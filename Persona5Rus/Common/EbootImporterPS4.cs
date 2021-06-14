using PersonaEditorLib.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Persona5Rus.Common
{
    internal sealed class EbootImporterPS4
    {
        private static List<(int Pos, int MaxSize, string Name)> BMDData = new List<(int Pos, int MaxSize, string Name)>()
        {
            (0x12DF5E0, 0x1C3,   "BMD1.PTP"),
            (0x12DF2B0, 0x263,   "BMD2.PTP"),
            (0x12DF7B0, 0x17215, "BMD3.PTP"),
            (0x12F69D0, 0x17292, "BMD4.PTP"),
        };

        private Dictionary<string, Dictionary<(int, int), string>> import = new Dictionary<string, Dictionary<(int, int), string>>();

        private readonly Encoding oldEncoding;
        private readonly Encoding newEncoding;
        private readonly Dictionary<char, int> charWidth;

        public EbootImporterPS4(string textPTPPath, Encoding oldEncoding, Encoding newEncoding, Dictionary<char, int> charWidth)
        {
            this.oldEncoding = oldEncoding ?? throw new ArgumentNullException(nameof(oldEncoding));
            this.newEncoding = newEncoding ?? throw new ArgumentNullException(nameof(newEncoding));
            this.charWidth = charWidth ?? throw new ArgumentNullException(nameof(charWidth));

            var translate = Path.Combine(textPTPPath, "eboot.tsv");

            var tranlateLines = File.ReadAllLines(translate)
                .Select(l => l.Split('\t'))
                .GroupBy(l => l[0]).ToArray();

            var pathSplit = Path.GetFileNameWithoutExtension(translate).Split('-');

            foreach (var group in tranlateLines)
            {
                var spl = pathSplit.Concat(new string[] { group.Key }).ToArray();
                var fullPath = Path.Combine(spl).ToLower();

                if (import.ContainsKey(fullPath))
                {
                    continue;
                }

                Dictionary<(int, int), string> dic = new Dictionary<(int, int), string>();

                foreach (var line in group)
                {
                    if (int.TryParse(line[1], out int msgInd)
                        && int.TryParse(line[2], out int strInd))
                    {
                        var intTuple = new ValueTuple<int, int>(msgInd, strInd);
                        if (dic.ContainsKey(intTuple))
                        {
                            continue;
                        }
                        dic.Add(intTuple, line[5]);
                    }
                }

                if (dic.Count == 0)
                {
                    continue;
                }

                import.Add(fullPath, dic);
            }
        }

        public void ImportBIN(string binPath)
        {
            Import(binPath);
        }

        private void Import(string elfPath)
        {
            var ebootData = File.ReadAllBytes(elfPath);

            using (var MS = new MemoryStream(ebootData))
            {
                foreach (var bmd in BMDData)
                {
                    PackPart(MS, bmd.Name, bmd.Pos, bmd.MaxSize);
                }
            }

            File.WriteAllBytes(elfPath, ebootData);
        }

        private void PackPart(MemoryStream MS, string BMDName, int pos, int size)
        {
            MS.Position = pos;
            var buffer = new byte[size];
            MS.Read(buffer, 0, size);

            var bmd = new BMD(buffer);

            var fullName = Path.Combine("eboot", BMDName).ToLower();
            import.TryGetValue(fullName, out Dictionary<(int, int), string> trans);

            if (trans == null)
            {
                return;
            }

            for (int msgInd = 0; msgInd < bmd.Msg.Count; msgInd++)
            {
                var msgData = bmd.Msg[msgInd];

                for (int strInd = 0; strInd < msgData.MsgStrings.Length; strInd++)
                {
                    var strData = msgData.MsgStrings[strInd];

                    var split = new MSGSplitter(strData, strInd + 1 == msgData.MsgStrings.Length);

                    if (trans != null
                        && trans.TryGetValue(new ValueTuple<int, int>(msgInd, strInd), out string newstr))
                    {
                        split.ChangeBody(newstr, newEncoding, charWidth);
                    }
                    else
                    {
                        split.ChangeEncoding(oldEncoding, newEncoding);
                    }

                    msgData.MsgStrings[strInd] = split.GetData();
                }
            }

            var bmdData = bmd.GetData();

            if (bmdData.Length > size)
            {
                throw new Exception(BMDName);
            }

            MS.Position = pos;
            for (int i = 0; i < size; i++)
            {
                MS.WriteByte(0);
            }
            MS.Position = pos;
            MS.Write(bmdData, 0, bmdData.Length);
        }
    }
}
