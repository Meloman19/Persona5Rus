using AuxiliaryLibraries.Tools;
using PersonaEditorLib;
using PersonaEditorLib.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Persona5Rus.Common
{
    class TextImporter
    {
        private Dictionary<string, string> NameDic = new Dictionary<string, string>();
        private Dictionary<string, string> DUPLICATES = new Dictionary<string, string>();
        private Dictionary<string, Dictionary<(int, int), string>> import = new Dictionary<string, Dictionary<(int, int), string>>();

        private Encoding oldEncoding = Static.OldEncoding();
        private Encoding newEncoding = Static.NewEncoding();
        private Dictionary<char, int> charWidth = Static.NewFont().GetCharWidth(Static.NewEncoding());

        public TextImporter(string textPTPPath, string duplicatesFilePath)
        {
            string NAMEfile = Path.Combine(textPTPPath, "names.tsv");
            if (File.Exists(NAMEfile))
            {
                foreach (var line in File.ReadAllLines(NAMEfile).Select(x => x.Split('\t')).Where(x => x.Length > 1 && x[1] != ""))
                {
                    if (!NameDic.ContainsKey(line[0]))
                    {
                        NameDic.Add(line[0], line[1]);
                    };
                }
            }

            if (File.Exists(duplicatesFilePath))
            {
                string[] dupl = File.ReadAllLines(duplicatesFilePath);
                string last = "";

                foreach (var line in dupl)
                {
                    if (line.EndsWith(":"))
                        last = line.Substring(0, line.Length - 1);
                    else if (!line.Equals(""))
                        DUPLICATES.Add(line.ToLower(), last.ToLower());
                }
            }

            foreach (var translate in Directory.EnumerateFiles(textPTPPath))
            {
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
        }

        public void Import(string file, string sourceDir)
        {
            var fileData = PersonaEditorLib.GameFormatHelper.OpenFile(Path.GetFileName(file), File.ReadAllBytes(file));
            var bmds = fileData.GetAllObjectFiles(FormatEnum.BMD).ToArray();

            if (bmds.Length == 0)
            {
                return;
            }

            var dirName = Path.GetDirectoryName(file);
            var rel = IOTools.RelativePath(dirName, sourceDir);

            foreach (var bmd in bmds)
            {
                var bmdGM = bmd.GameData as BMD;

                Dictionary<(int, int), string> trans;
                var fullName = Path.Combine(rel, Path.GetFileNameWithoutExtension(bmd.Name.Replace('/', '+')) + ".ptp").ToLower();
                if (!import.TryGetValue(fullName, out trans))
                {
                    if (DUPLICATES.TryGetValue(fullName, out fullName))
                    {
                        import.TryGetValue(fullName, out trans);
                    }
                }

                for (int msgInd = 0; msgInd < bmdGM.Msg.Count; msgInd++)
                {
                    var msgData = bmdGM.Msg[msgInd];

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

                if (rel == @"data\camp\chat")
                {
                    // Исключительный случай. Для отображения иконок персонажей в чатах (месседжере)
                    // игра используется в качетсве ключа имя персонажа зашитое в файл BMD.
                    // Искать, где находится аналогичная таблица для самих иконок мне лень, поэтому проще во всех чатах
                    // не менять исходные данные в именах.
                    // Всё равно имена в чатах не отображаются.
                    continue;
                }

                foreach (var name in bmdGM.Name)
                {
                    var oldName = oldEncoding.GetString(name.NameBytes);
                    string newName;
                    if (!NameDic.TryGetValue(oldName, out newName))
                    {
                        newName = oldName;
                    }

                    name.NameBytes = newEncoding.GetBytes(newName);
                }
            }

            File.WriteAllBytes(file, fileData.GameData.GetData());
        }
    }
}