using PersonaEditorLib.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Persona5Rus.Common
{
    class EbootImporter
    {
        private const string BMD1Name = "BMD1.PTP";
        private const int BMD1Pos = 0xD8B784;
        private const int BMD1MaxSize = 0x1C3;

        private const string BMD2Name = "BMD2.PTP";
        private const int BMD2Pos = 0xDE85EC;
        private const int BMD2MaxSize = 0x263;

        private const string BMD3Name = "BMD3.PTP";
        private const int BMD3Pos = 0xDE8954;
        private const int BMD3MaxSize = 0x17215;

        private const string BMD4Name = "BMD4.PTP";
        private const int BMD4Pos = 0xDFFB69;
        private const int BMD4MaxSize = 0x17292;

        private static List<(int Pos, int Size, string Str)> StringData = new List<(int Pos, int Size, string Str)>()
        {
            (0xB713DC, 0x10, "START"),
            (0xB713EC, 0x10, "SELECT"),
            (0xB7B6F4, 0x8,  "Защита?"),
            (0xB7B760, 0x10, "Полная свобода"),
            (0xB7B770, 0x10, "Массивная атака"),
            (0xB7B780, 0x10, "Сохранить ОД"),
            (0xB7B790, 0x10, "Лечение/Помощь"),
            (0xB7B7A0, 0x10, "Прямые команды"),
            (0xB7B7E8, 0x10, "Вся команда")
        };

        private Dictionary<string, Dictionary<(int, int), string>> import = new Dictionary<string, Dictionary<(int, int), string>>();

        private Encoding oldEncoding = Global.OldEncoding();
        private Encoding newEncoding = Global.NewEncoding();
        private Dictionary<char, int> charWidth = Global.NewFont().GetCharWidth(Global.NewEncoding());

        public EbootImporter(string textPTPPath)
        {
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

        public void Import(string ebootPath)
        {
            var ebootData = File.ReadAllBytes(ebootPath);

            using (var MS = new MemoryStream(ebootData))
            {
                PackPart(MS, BMD1Name, BMD1Pos, BMD1MaxSize);
                PackPart(MS, BMD2Name, BMD2Pos, BMD2MaxSize);
                PackPart(MS, BMD3Name, BMD3Pos, BMD3MaxSize);
                PackPart(MS, BMD4Name, BMD4Pos, BMD4MaxSize);

                foreach (var str in StringData)
                {
                    PackString(MS, str.Pos, str.Size, str.Str);
                }
            }

            File.WriteAllBytes(ebootPath, ebootData);
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

        private void PackString(MemoryStream MS, int pos, int size, string str)
        {
            var data = newEncoding.GetBytes(str);

            if (data.Length >= size)
            {
                throw new Exception(str);
            }

            MS.Position = pos;
            for (int i = 0; i < size; i++)
            {
                MS.WriteByte(0);
            }
            MS.Position = pos;
            MS.Write(data, 0, data.Length);
        }
    }
}