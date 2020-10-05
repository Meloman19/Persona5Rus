using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using VGAudio.Containers.Wave;
using VGAudio.Formats.CriAdx;
using VGMToolbox.format;

namespace Persona5Rus.Common
{
    internal sealed class UsmImporter
    {
        private static readonly string USMEncoderTool = Path.Combine(Global.BasePath, "Tools", "usm", "medianoche.exe");

        private readonly string _temp;

        private readonly Dictionary<string, string[][]> import = new Dictionary<string, string[][]>();

        public UsmImporter(string temp, string translateFile)
        {
            _temp = temp;

            {
                var list = new List<string[]>();
                string currentName = null;

                foreach (var line in File.ReadAllLines(translateFile))
                {
                    var split = line.Split('\t');

                    if (split[0].EndsWith(".usm"))
                    {
                        if (currentName != null)
                        {
                            import.Add(currentName, list.ToArray());
                            list.Clear();
                        }

                        currentName = split[0];
                    }
                    else
                    {
                        if (currentName == null)
                        {
                            continue;
                        }

                        list.Add(split);
                    }
                }

                if (currentName != null)
                {
                    import.Add(currentName, list.ToArray());
                }
            }
        }

        public void Import(string usmPath, string output)
        {
            if (Directory.Exists(_temp))
            {
                Directory.Delete(_temp, true);
            }

            Directory.CreateDirectory(_temp);

            var usmName = Path.GetFileName(usmPath);
            var usmNameWOExt = Path.GetFileNameWithoutExtension(usmName);

            if (import.TryGetValue(usmName, out string[][] translate))
            {
                var oldEncoding = Global.OldEncoding();
                var newEncoding = Global.NewEncoding();

                var outtrslt = new List<string>();
                outtrslt.Add("1000");
                foreach (var sub in translate)
                {
                    var tr = sub[2];
                    tr = string.Join("\\n", tr.Split(new string[] { "\\n" }, StringSplitOptions.None).Select(l => oldEncoding.GetString(newEncoding.GetBytes(l))).ToArray());
                    outtrslt.Add($"{sub[0]}, {sub[1]}, {tr}");
                }
                File.WriteAllLines(Path.Combine(_temp, usmNameWOExt + ".txt"), outtrslt);
            }
            else
            {
                return;
            }

            if (!File.Exists(USMEncoderTool))
            {
                throw new Exception("Не найден инструмент для кодирования usm.");
            }

            var tempUsmPath = Path.Combine(_temp, usmName);
            File.Copy(usmPath, tempUsmPath);

            RunDemux(tempUsmPath);
            File.Delete(tempUsmPath);

            var adxs = Directory.EnumerateFiles(_temp, "*.adx").OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f).Split('_')[1])).ToArray();
            {
                if (adxs.Length > 2)
                {
                    throw new Exception("ADX more than 2");
                }
                if (adxs.Length > 1)
                {
                    var japAdx = adxs[1];
                    var newName = Path.Combine(Path.GetDirectoryName(japAdx), Path.GetFileNameWithoutExtension(usmPath) + "_jap.adx");
                    File.Move(japAdx, newName);
                    adxs[1] = newName;
                }
                if (adxs.Length > 0)
                {
                    var engAdx = adxs[0];
                    var newName = Path.Combine(Path.GetDirectoryName(engAdx), Path.GetFileNameWithoutExtension(usmPath) + "_eng.adx");
                    File.Move(engAdx, newName);
                    adxs[0] = newName;
                }

                foreach (var adx in adxs)
                {
                    ExtractAdx(adx);
                    File.Delete(adx);
                }
            }

            var m2v = Directory.EnumerateFiles(_temp, "*.m2v").ToArray()[0];
            {
                var newName = Path.Combine(Path.GetDirectoryName(m2v), Path.GetFileNameWithoutExtension(usmPath) + ".m2v");
                File.Move(m2v, newName);
            }

            EncodeUsm(_temp);

            var outputPath = Path.Combine(output, Path.GetFileName(tempUsmPath));
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
            Directory.CreateDirectory(output);
            File.Move(tempUsmPath, outputPath);
        }

        private void RunDemux(string usmPath)
        {
            try
            {
                CriUsmStream usmStream = new CriUsmStream(usmPath);
                usmStream.DemultiplexStreams(new MpegStream.DemuxOptionsStruct()
                {
                    ExtractAudio = true,
                    ExtractVideo = true,
                    SplitAudioStreams = false,
                    AddHeader = true,
                    AddPlaybackHacks = false
                });
            }
            catch
            {
                throw new Exception("Usm Demux Error");
            }
        }

        private static void ExtractAdx(string filePath)
        {
            CriAdxFormat adxFormat;
            using (var adxFS = File.OpenRead(filePath))
            {
                var adxData = new VGAudio.Containers.Adx.AdxReader().Read(adxFS);
                adxFormat = adxData.GetFormat<CriAdxFormat>();
            }

            var wavWriter = new WaveWriter();
            for (int i = 0; i < adxFormat.ChannelCount; i++)
            {
                var chWavPath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + $"_ch{i + 1}.wav");

                var channel = adxFormat.GetChannels(i);

                using (var chFS = File.Create(chWavPath))
                {
                    wavWriter.WriteToStream(channel, chFS);
                }
            }
        }

        private void EncodeUsm(string dir)
        {
            string m2vPath;
            {
                var m2v = Directory.EnumerateFiles(dir, "*.m2v").ToArray();
                if (m2v.Length != 1)
                {
                    throw new Exception("USM Encode - 1");
                }
                m2vPath = m2v[0];
            }

            var usmName = Path.GetFileNameWithoutExtension(m2vPath);

            var waves = Directory.EnumerateFiles(dir, "*.wav").ToArray();

            var hasEng = waves.Any(w => Path.GetFileNameWithoutExtension(w).StartsWith(usmName + "_eng"));
            var hasJap = waves.Any(w => Path.GetFileNameWithoutExtension(w).StartsWith(usmName + "_jap"));

            var hasSub = File.Exists(Path.Combine(dir, usmName + ".txt"));

            string args = $"-gop_closed=on -gop_i=1 -gop_p=4 -gop_b=2 -video00={usmName}.m2v -output={usmName}.usm -bitrate=12000000";

            if (hasEng)
            {
                for (int ch = 0; ch < 6; ch++)
                {
                    args += $" -mca00_0{ch}={usmName}_eng_ch{ch + 1}.wav";
                }
            }
            if (hasJap)
            {
                for (int ch = 0; ch < 6; ch++)
                {
                    args += $" -mca01_0{ch}={usmName}_jap_ch{ch + 1}.wav";
                }
            }
            if (hasSub)
            {
                args += $" -subtitle00={usmName}.txt";
            }

            var process = new Process();
            process.StartInfo.FileName = USMEncoderTool;
            process.StartInfo.Arguments = args;
            process.StartInfo.WorkingDirectory = dir;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Thread.Sleep(100);
            process.Start();
            process.WaitForExit();
        }
    }
}