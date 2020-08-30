using PersonaEditorLib.FileContainer;
using PersonaEditorLib.Other;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persona5Rus.Common
{
    internal static class ImportSteps_Tables
    {
        public static void PackTBLtoSource(string sourceDir, string newTableDir, IProgress<double> progress)
        {
            const double count = 2;

            PackTBL_TablePac(sourceDir, newTableDir);
            progress.Report(1 / count);
            PackFTD_Camp(sourceDir, newTableDir);
            progress.Report(2 / count);
        }

        private static void PackTBL_TablePac(string sourceDir, string newTableDir)
        {
            const string s1 = @"data\battle\table.pac";
            const string s2 = @"NAME_TBL.tsv";
            var table_pac_path = Path.Combine(sourceDir, s1);

            if (!File.Exists(table_pac_path))
            {
                throw new Exception($"Отсутствует файл: {s1}");
            }

            var table_text_path = Path.Combine(newTableDir, s2);

            if (!File.Exists(table_text_path))
            {
                throw new Exception($"Отсутствует файл: {s2}");
            }

            Encoding newEncoding = Static.NewEncoding();

            var bin = new BIN(File.ReadAllBytes(table_pac_path));

            string[][] source = null;

            {
                var listS = new List<string[]>();
                var list = new List<string>();
                bool started = false;

                foreach (var line in File.ReadAllLines(table_text_path))
                {
                    if (line.StartsWith("NAME.TBL"))
                    {
                        if (started)
                        {
                            listS.Add(list.ToArray());
                            list.Clear();
                        }
                        else
                        {
                            started = true;
                        }
                    }
                    else
                    {
                        if (!started)
                        {
                            continue;
                        }

                        var split = line.Split('\t');
                        if (string.IsNullOrEmpty(split[1]))
                        {
                            list.Add(split[0]);
                        }
                        else
                        {
                            list.Add(split[1]);
                        }
                    }
                }

                if (started)
                {
                    listS.Add(list.ToArray());
                }

                source = listS.ToArray();
            }

            if (bin.SubFiles.Find(x => x.Name == "table/NAME.TBL")?.GameData is TBL tbl)
            {
                for (int i = 0; i < tbl.SubFiles.Count; i += 2)
                {
                    var array = source[i / 2];

                    var output = new List<byte[]>();

                    foreach (var item in array)
                    {
                        var newItem = newEncoding.GetBytes(item).Concat(new byte[] { 0 }).ToArray();
                        output.Add(newItem);
                    }

                    byte[] buffer = new byte[2];
                    List<int> pos = new List<int>();

                    int offset = 0;
                    foreach (var a in output)
                    {
                        pos.Add(offset);
                        offset += a.Length;
                    }

                    using (MemoryStream MS = new MemoryStream())
                    {
                        foreach (var a in pos)
                        {
                            buffer = BitConverter.GetBytes((ushort)a).Reverse().ToArray();
                            MS.Write(buffer, 0, 2);
                        }
                        tbl.SubFiles[i].GameData = new DAT(MS.ToArray());
                    }

                    using (MemoryStream MS = new MemoryStream())
                    {
                        foreach (var a in output)
                            MS.Write(a, 0, a.Length);

                        tbl.SubFiles[i + 1].GameData = new DAT(MS.ToArray());
                    }
                }
            }

            File.WriteAllBytes(table_pac_path, bin.GetData());
        }

        private static void PackFTD_Camp(string sourceDir, string newTableDir)
        {
            const string s1 = @"ps3\init\camp.pak";
            const string s2 = @"CAMP_PAK.tsv";
            var camp_pak_path = Path.Combine(sourceDir, s1);

            if (!File.Exists(camp_pak_path))
            {
                throw new Exception($"Отсутствует файл: {s1}");
            }

            var table_text_path = Path.Combine(newTableDir, s2);

            if (!File.Exists(table_text_path))
            {
                throw new Exception($"Отсутствует файл: {s2}");
            }

            Encoding oldEncoding = Static.OldEncoding();
            Encoding newEncoding = Static.NewEncoding();

            var bin = new BIN(File.ReadAllBytes(camp_pak_path));

            var bin_ftd = bin.SubFiles[4].GameData as BIN;

            string[] ftdNames = new string[]
            {
                "cmpQuestName.ctd",
                "cmpQuestTargetName.ctd",
                "cmpPersonaParam.ctd",
                "cmpSystemMenu.ctd",
                "cmpSystemHelp.ctd",
                "cmpConfigHelp.ctd",
                "cmpDifficultName.ctd",
                "cmpConfigItem.ctd",
                "cmpCalName.ctd",
                "cmpArbeitName.ctd",
                "chatTitleName.ctd"
            };

            var source = new Dictionary<string, string[]>();

            {
                var list = new List<string>();
                string currentName = null;

                foreach (var line in File.ReadAllLines(table_text_path))
                {
                    var split = line.Split('\t');

                    if (split[0].EndsWith(".ctd"))
                    {
                        if(currentName != null)
                        {
                            source.Add(currentName, list.ToArray());
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

                        if (string.IsNullOrEmpty(split[1]))
                        {
                            list.Add(split[0]);
                        }
                        else
                        {
                            list.Add(split[1]);
                        }
                    }
                }

                if (currentName != null)
                {
                    source.Add(currentName, list.ToArray());
                }
            }

            foreach (var a in bin_ftd.SubFiles)
                if (ftdNames.Contains(a.Name))
                    if (a.GameData is FTD ftd)
                    {
                        if (source.TryGetValue(a.Name, out string[] import))
                        {
                            ftd.ImportText_1Entry_LineByLine(import, newEncoding);
                        }
                        else
                        {
                            ftd.ImportText_1Entry_Reimport(oldEncoding, newEncoding);
                        }
                    }
            
            File.WriteAllBytes(camp_pak_path, bin.GetData());
        }
    }
}