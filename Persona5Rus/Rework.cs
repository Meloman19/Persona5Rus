using Persona5Rus.Common;
using PersonaEditorLib.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Persona5Rus
{
    class Rework
    {
        public static void DoNormal()
        {
            var source = @"d:\Visual Studio 2019\rework\source";
            var dest = @"d:\Visual Studio 2019\rework\dest";
            var dir = @"d:\Persona 5\DATA_PS3_ENG";

            foreach (var file in Directory.EnumerateFiles(source))
            {
                ExtractNormal(file, dir, dest);
            }
        }

        public static void ExtractNormal(string source, string dir, string dest)
        {
            var sourceLines = File.ReadAllLines(source).Select(s => s.Split('\t')).ToList();

            Dictionary<string, Dictionary<int, Dictionary<int, int>>> indexes = new Dictionary<string, Dictionary<int, Dictionary<int, int>>>();
            {
                for (int i = 0; i < sourceLines.Count; i++)
                {
                    var name = Path.GetFileNameWithoutExtension(sourceLines[i][0]);
                    if (int.TryParse(sourceLines[i][1], out int msgInd)
                        && int.TryParse(sourceLines[i][2], out int strInd))
                    {
                        if (!indexes.TryGetValue(name, out Dictionary<int, Dictionary<int, int>> dicByName))
                        {
                            dicByName = new Dictionary<int, Dictionary<int, int>>();
                            indexes.Add(name, dicByName);
                        }

                        if (!dicByName.TryGetValue(msgInd, out Dictionary<int, int> dicByInd))
                        {
                            dicByInd = new Dictionary<int, int>();
                            dicByName.Add(msgInd, dicByInd);
                        }

                        dicByInd.Add(strInd, i);
                    }
                }
            }

            var output = Enumerable.Repeat("", sourceLines.Count).ToArray();

            string dirPath;
            {
                var name = Path.GetFileNameWithoutExtension(source);
                var splitName = name.Split('-');

                var path = (new string[] { dir }).Concat(splitName).ToArray();

                dirPath = Path.Combine(path);
            }

            foreach (var file in Directory.EnumerateFiles(dirPath, "*", SearchOption.TopDirectoryOnly))
            {
                var dat = PersonaEditorLib.GameFormatHelper.OpenFile(Path.GetFileName(file), File.ReadAllBytes(file));

                if (dat != null)
                {
                    foreach (var bmd in dat.GetAllObjectFiles(PersonaEditorLib.FormatEnum.BMD))
                    {
                        var name = Path.GetFileNameWithoutExtension(bmd.Name.Replace('/', '+'));

                        if (indexes.TryGetValue(name, out Dictionary<int, Dictionary<int, int>> dicByName))
                        {
                            var bmdGD = bmd.GameData as BMD;
                            for (int msgInd = 0; msgInd < bmdGD.Msg.Count; msgInd++)
                            {
                                var msgs = bmdGD.Msg[msgInd];

                                var len = msgs.MsgStrings.GetLength(0);
                                for (int msgStrInd = 0; msgStrInd < len; msgStrInd++)
                                {
                                    var msgS = msgs.MsgStrings[msgStrInd];

                                    var splitResult = new MSGSplitter(msgS, msgStrInd + 1 == len);
                                    var text = splitResult.Body.GetString(Static.P5EngEncoding(), true).Replace("\n", " ");

                                    if (dicByName.TryGetValue(msgInd, out Dictionary<int, int> dicByMsg)
                                        && dicByMsg.TryGetValue(msgStrInd, out int sourceLineInd))
                                    {
                                        output[sourceLineInd] = text;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            File.WriteAllLines(Path.Combine(dest, Path.GetFileName(source)), output);
        }
    }
}