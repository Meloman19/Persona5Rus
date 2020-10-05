using PersonaEditorLib.FileContainer;
using PersonaEditorLib.Other;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Persona5Rus.Common
{
    internal static class ImportSteps_Tables
    {
        public static void PackTBLtoSource(string sourceDir, string newTableDir, IProgress<double> progress)
        {
            var actions = new List<Action<string, string>>()
            {
                Pack_TablePac,
                Pack_Roadmap,
                PackFTD_CmmBin,
                PackFTD_FacilityPak,
                PackFTD_ttrTableBin,
                PackFTD_goodGaugePac,
                PackFTD_fldPanelMsg,
                PackFTD_fldPanelMsgDng,
                PackFTD_fldResident,
                PackFTD_CampPak,
                PackFTD_PanelBin
            };

            for (int i = 0; i < actions.Count; i++)
            {
                actions[i](sourceDir, newTableDir);
                progress.Report((i + 1 / actions.Count) * 100);
            }
        }

        // data\battle\table.pac
        private static void Pack_TablePac(string sourceDir, string newTableDir)
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

            Encoding newEncoding = Global.NewEncoding();

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

        // data\field\panel\roadmap\roadmap.tbl
        private static void Pack_Roadmap(string sourceDir, string newTableDir)
        {
            const string s1 = @"data\field\panel\roadmap\roadmap.tbl";
            const string s2 = @"ROADMAP_TBL.tsv";
            var source_path = Path.Combine(sourceDir, s1);

            if (!File.Exists(source_path))
            {
                throw new Exception($"Отсутствует файл: {s1}");
            }

            var table_text_path = Path.Combine(newTableDir, s2);

            if (!File.Exists(table_text_path))
            {
                throw new Exception($"Отсутствует файл: {s2}");
            }

            Encoding oldEncoding = Global.OldEncoding();
            Encoding newEncoding = Global.NewEncoding();

            var bin = new BIN(File.ReadAllBytes(source_path));

            string[] ftdNames = new string[]
            {
                "fld_texpack_title.ftd"
            };

            var source = new Dictionary<string, string[]>();

            {
                var list = new List<string>();
                string currentName = null;

                foreach (var line in File.ReadAllLines(table_text_path))
                {
                    var split = line.Split('\t');

                    if (split[0].EndsWith(".ftd"))
                    {
                        if (currentName != null)
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

            foreach (var a in bin.SubFiles)
                if (ftdNames.Contains(a.Name))
                    if (a.GameData is FTD ftd)
                    {
                        if (source.TryGetValue(a.Name, out string[] import))
                        {
                            ftd.ImportText_MultiEntry_LineByLine(import, newEncoding);
                        }
                        else
                        {
                            ftd.ImportText_MultiEntry_Reimport(oldEncoding, newEncoding);
                        }
                    }

            File.WriteAllBytes(source_path, bin.GetData());
        }

        // data\init\cmm.bin
        private static void PackFTD_CmmBin(string sourceDir, string newTableDir)
        {
            const string s1 = @"data\init\cmm.bin";
            const string s2 = @"CMM_BIN.tsv";
            var source_path = Path.Combine(sourceDir, s1);

            if (!File.Exists(source_path))
            {
                throw new Exception($"Отсутствует файл: {s1}");
            }

            var table_text_path = Path.Combine(newTableDir, s2);

            if (!File.Exists(table_text_path))
            {
                throw new Exception($"Отсутствует файл: {s2}");
            }

            Encoding oldEncoding = Global.OldEncoding();
            Encoding newEncoding = Global.NewEncoding();

            var bin = new BIN(File.ReadAllBytes(source_path));

            string[] ftdNames = new string[]
            {
                "cmmName.ctd",
                "cmmArcanaSPHelp.ctd",
                "cmmClubName.ctd",
                "cmmMailOrder_Text.ctd",
                "cmmMailOrder_Name.ctd",
                "cmmMemberName.ctd",
                "cmmPC_PARAM_Name.ctd",
                "cmmPhantomThiefName.ctd",
                "cmmFixString.ctd",
                "cmmFunctionName.ctd",
                "cmmAreaName.ctd"
            };

            string[] ftdNames2 = new string[]
            {
                "cmmNetReportTable.ctd"
            };

            string[] ftdNames3 = new string[]
            {
                "cmmPC_PARAM_Help.ctd"
            };

            var translate = new Dictionary<string, string[]>();

            {
                var list = new List<string>();
                string currentName = null;

                foreach (var line in File.ReadAllLines(table_text_path))
                {
                    var split = line.Split('\t');

                    if (split[0].EndsWith(".ctd"))
                    {
                        if (currentName != null)
                        {
                            translate.Add(currentName, list.ToArray());
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
                    translate.Add(currentName, list.ToArray());
                }
            }

            foreach (var a in bin.SubFiles)
            {
                if (ftdNames.Contains(a.Name))
                {
                    if (a.GameData is FTD ftd)
                    {
                        if (translate.TryGetValue(a.Name, out string[] import))
                        {
                            ftd.ImportText_1Entry_LineByLine(import, newEncoding);
                        }
                        else
                        {
                            ftd.ImportText_1Entry_Reimport(oldEncoding, newEncoding);
                        }
                    }
                }
                else if (ftdNames2.Contains(a.Name))
                {
                    if (a.GameData is FTD ftd)
                    {
                        if (translate.TryGetValue(a.Name, out string[] import))
                        {
                            ftd.ImportText_1Entry_LineByLine(import, newEncoding, 4);
                        }
                        else
                        {
                            ftd.ImportText_1Entry_Reimport(oldEncoding, newEncoding, 4);
                        }
                    }
                }
                else if (ftdNames3.Contains(a.Name))
                {
                    if (a.GameData is FTD ftd)
                    {
                        translate.TryGetValue(a.Name, out string[] import);
                        ftd.Import_cmmPC_PARAM_Help(import, newEncoding);
                    }
                }
            }

            File.WriteAllBytes(source_path, bin.GetData());
        }

        // data\init\facility.pak & data\init\facility.pak
        private static void PackFTD_FacilityPak(string sourceDir, string newTableDir)
        {
            const string s1 = @"data\init\fclTable.bin";
            const string s12 = @"data\init\facility.pak";
            const string s2 = @"FACILITY_PAK.tsv";

            var source_path = Path.Combine(sourceDir, s1);
            var source_path2 = Path.Combine(sourceDir, s12);
            var table_text_path = Path.Combine(newTableDir, s2);

            if (!File.Exists(source_path))
            {
                throw new Exception($"Отсутствует файл: {s1}");
            }

            if (!File.Exists(source_path2))
            {
                throw new Exception($"Отсутствует файл: {s12}");
            }

            if (!File.Exists(table_text_path))
            {
                throw new Exception($"Отсутствует файл: {s2}");
            }

            Encoding oldEncoding = Global.OldEncoding();
            Encoding newEncoding = Global.NewEncoding();

            string[] ftdNames = new string[]
            {
                "fclSpreadText.ftd",
                "fclSimpleHelp.ftd",
                "fclCmbComText.ftd",
                "fclSuggestTypeName.ftd",
                "fclSearchText.ftd",
                "fclViewTypeText.ftd",
                "fclLogFormatText.ftd",
                "fclLogConjunctionText.ftd",
                "fclInjectionName.ftd",
                "fclSetItemName.ftd",
                "fclPublicShopName.ftd"
            };

            string[] ftdNames2 = new string[]
            {
                "fclHelpTable_COMBINE_ROOT.ftd",
                "fclHelpTable_COMBINE_SUB.ftd",
                "fclHelpTable_COMBINE_G.ftd",
                "fclHelpTable_COMBINE_G_HELP.ftd",
                "fclHelpTable_COMBINE_HELP.ftd",
                "fclHelpTable_COMPEND.ftd",
                "fclHelpTable_COMPEND_HELP.ftd",
                "fclHelpTable_CELL.ftd",
                "fclHelpTable_CELL_HELP.ftd"
            };

            var translate = new Dictionary<string, string[]>();

            {
                var list = new List<string>();
                string currentName = null;

                foreach (var line in File.ReadAllLines(table_text_path))
                {
                    var split = line.Split('\t');

                    if (split[0].EndsWith(".ftd"))
                    {
                        if (currentName != null)
                        {
                            translate.Add(currentName, list.ToArray());
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
                    translate.Add(currentName, list.ToArray());
                }
            }

            var bin = new BIN(File.ReadAllBytes(source_path));

            foreach (var a in bin.SubFiles)
            {
                if (ftdNames.Contains(a.Name))
                {
                    if (a.GameData is FTD ftd)
                    {
                        if (translate.TryGetValue(a.Name, out string[] import))
                        {
                            ftd.ImportText_1Entry_LineByLine(import, newEncoding);
                        }
                        else
                        {
                            ftd.ImportText_1Entry_Reimport(oldEncoding, newEncoding);
                        }
                    }
                }
                else if (ftdNames2.Contains(a.Name))
                {
                    if (a.GameData is FTD ftd)
                    {
                        if (translate.TryGetValue(a.Name, out string[] import))
                        {
                            ftd.ImportText_1Entry_LineByLine(import, newEncoding, 8, 4);
                        }
                        else
                        {
                            ftd.ImportText_1Entry_Reimport(oldEncoding, newEncoding, 8, 4);
                        }
                    }
                }
            }

            File.WriteAllBytes(source_path, bin.GetData());

            var bin2 = new BIN(File.ReadAllBytes(source_path2));

            var fcl = bin2.SubFiles.Find(gd => gd.Name == "fclTable.bin");
            fcl.GameData = bin;

            File.WriteAllBytes(source_path2, bin2.GetData());
        }

        // data\init\ttrTable.bin"
        private static void PackFTD_ttrTableBin(string sourceDir, string newTableDir)
        {
            const string s1 = @"data\init\ttrTable.bin";
            const string s2 = @"TTRTABLE_BIN.tsv";

            var source_path = Path.Combine(sourceDir, s1);
            var table_text_path = Path.Combine(newTableDir, s2);

            if (!File.Exists(source_path))
            {
                throw new Exception($"Отсутствует файл: {s1}");
            }

            if (!File.Exists(table_text_path))
            {
                throw new Exception($"Отсутствует файл: {s2}");
            }

            Encoding oldEncoding = Global.OldEncoding();
            Encoding newEncoding = Global.NewEncoding();

            string[] ftdNames = new string[]
            {
                "ttrTitleName_BATTLE.ttd",
                "ttrTitleName_COMBINE.ttd",
                "ttrTitleName_DAILY.ttd",
                "ttrTitleName_DUNGEON.ttd",
                "ttrTitleName_SYSTEM.ttd",
                "ttrTitleName_STORY.ttd"
            };

            var translate = new Dictionary<string, string[]>();

            {
                var list = new List<string>();
                string currentName = null;

                foreach (var line in File.ReadAllLines(table_text_path))
                {
                    var split = line.Split('\t');

                    if (split[0].EndsWith(".ttd"))
                    {
                        if (currentName != null)
                        {
                            translate.Add(currentName, list.ToArray());
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
                    translate.Add(currentName, list.ToArray());
                }
            }

            var bin = new BIN(File.ReadAllBytes(source_path));

            foreach (var a in bin.SubFiles)
            {
                if (ftdNames.Contains(a.Name))
                {
                    if (a.GameData is FTD ftd)
                    {
                        if (translate.TryGetValue(a.Name, out string[] import))
                        {
                            ftd.ImportText_1Entry_LineByLine(import, newEncoding);
                        }
                        else
                        {
                            ftd.ImportText_1Entry_Reimport(oldEncoding, newEncoding);
                        }
                    }
                }
            }

            File.WriteAllBytes(source_path, bin.GetData());
        }

        // ps3\calendar\goodGauge.pac
        private static void PackFTD_goodGaugePac(string sourceDir, string newTableDir)
        {
            const string s1 = @"ps3\calendar\goodGauge.pac";
            const string s2 = @"GOODGAUGE_PAC.tsv";
            var source_path = Path.Combine(sourceDir, s1);

            if (!File.Exists(source_path))
            {
                throw new Exception($"Отсутствует файл: {s1}");
            }

            var table_text_path = Path.Combine(newTableDir, s2);

            if (!File.Exists(table_text_path))
            {
                throw new Exception($"Отсутствует файл: {s2}");
            }

            Encoding oldEncoding = Global.OldEncoding();
            Encoding newEncoding = Global.NewEncoding();

            var bin = new BIN(File.ReadAllBytes(source_path));

            string[] ftdNames = new string[]
            {
                "cldCommentTable.ftd",
                "cldEvtCommentTable.ftd"
            };

            var source = new Dictionary<string, string[]>();

            {
                var list = new List<string>();
                string currentName = null;

                foreach (var line in File.ReadAllLines(table_text_path))
                {
                    var split = line.Split('\t');

                    if (split[0].EndsWith(".ftd"))
                    {
                        if (currentName != null)
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

            foreach (var a in bin.SubFiles)
                if (ftdNames.Contains(a.Name))
                    if (a.GameData is FTD ftd)
                    {
                        if (source.TryGetValue(a.Name, out string[] import))
                        {
                            ftd.ImportText_MultiEntry_LineByLine(import, newEncoding);
                        }
                        else
                        {
                            ftd.ImportText_MultiEntry_Reimport(oldEncoding, newEncoding);
                        }
                    }

            File.WriteAllBytes(source_path, bin.GetData());
        }

        // ps3\field\panel\fldPanelMsg.pac
        private static void PackFTD_fldPanelMsg(string sourceDir, string newTableDir)
        {
            const string s1 = @"ps3\field\panel\fldPanelMsg.pac";
            const string s2 = @"FLDPANELMSG_PAC.tsv";
            var source_path = Path.Combine(sourceDir, s1);

            if (!File.Exists(source_path))
            {
                throw new Exception($"Отсутствует файл: {s1}");
            }

            var table_text_path = Path.Combine(newTableDir, s2);

            if (!File.Exists(table_text_path))
            {
                throw new Exception($"Отсутствует файл: {s2}");
            }

            Encoding oldEncoding = Global.OldEncoding();
            Encoding newEncoding = Global.NewEncoding();

            var source = new Dictionary<string, string[]>();

            {
                var list = new List<string>();
                string currentName = null;

                foreach (var line in File.ReadAllLines(table_text_path))
                {
                    var split = line.Split('\t');

                    if (split[0].EndsWith(".ftd"))
                    {
                        if (currentName != null)
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

            var bin = new BIN(File.ReadAllBytes(source_path));

            var ftd = bin.SubFiles.Find(gd => gd.Name == "fldWholeMapTable.ftd");
            var ftdData = ftd.GameData as FTD;

            source.TryGetValue("fldWholeMapTable.ftd", out string[] import);

            ftdData.ImportText_fldPanelMsg(import, newEncoding);

            File.WriteAllBytes(source_path, bin.GetData());
        }

        // ps3\field\panel\fldPanelMsgDng.pac
        private static void PackFTD_fldPanelMsgDng(string sourceDir, string newTableDir)
        {
            const string s1 = @"ps3\field\panel\fldPanelMsgDng.pac";
            const string s2 = @"FLDPANELMSGDNG_PAC.tsv";
            var source_path = Path.Combine(sourceDir, s1);

            if (!File.Exists(source_path))
            {
                throw new Exception($"Отсутствует файл: {s1}");
            }

            var table_text_path = Path.Combine(newTableDir, s2);

            if (!File.Exists(table_text_path))
            {
                throw new Exception($"Отсутствует файл: {s2}");
            }

            Encoding oldEncoding = Global.OldEncoding();
            Encoding newEncoding = Global.NewEncoding();

            var source = new Dictionary<string, string[]>();

            {
                var list = new List<string>();
                string currentName = null;

                foreach (var line in File.ReadAllLines(table_text_path))
                {
                    var split = line.Split('\t');

                    if (split[0].EndsWith(".ftd"))
                    {
                        if (currentName != null)
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

            var bin = new BIN(File.ReadAllBytes(source_path));

            var ftd = bin.SubFiles.Find(gd => gd.Name == "fldWholeMapTableDng.ftd");
            var ftdData = ftd.GameData as FTD;

            source.TryGetValue("fldWholeMapTableDng.ftd", out string[] import);

            ftdData.ImportText_fldPanelMsg(import, newEncoding);

            File.WriteAllBytes(source_path, bin.GetData());
        }

        // ps3\field\fldResident.pac
        private static void PackFTD_fldResident(string sourceDir, string newTableDir)
        {
            const string s1 = @"ps3\field\fldResident.pac";
            const string s2 = @"FLDRESIDENT_PAC.tsv";
            var source_path = Path.Combine(sourceDir, s1);

            if (!File.Exists(source_path))
            {
                throw new Exception($"Отсутствует файл: {s1}");
            }

            var table_text_path = Path.Combine(newTableDir, s2);

            if (!File.Exists(table_text_path))
            {
                throw new Exception($"Отсутствует файл: {s2}");
            }

            Encoding oldEncoding = Global.OldEncoding();
            Encoding newEncoding = Global.NewEncoding();

            var bin = new BIN(File.ReadAllBytes(source_path));

            string[] ftdNames = new string[]
            {
                "ftd/fldActionName.ftd",
                "ftd/fldArcanaName.ftd",
                "ftd/fldCheckName.ftd",
                "ftd/fldDngCheckName.ftd",
                "ftd/fldKFECheckName.ftd",
                "ftd/fldNPCName.ftd",
                "ftd/fldPlaceName.ftd",
                "ftd/fldScriptName.ftd"
            };

            string[] ftdNames2 = new string[]
             {
                "ftd/fldSaveDataPlace.ftd"
             };

            var source = new Dictionary<string, string[]>();

            {
                var list = new List<string>();
                string currentName = null;

                foreach (var line in File.ReadAllLines(table_text_path))
                {
                    var split = line.Split('\t');

                    if (split[0].EndsWith(".ftd"))
                    {
                        if (currentName != null)
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

            foreach (var a in bin.SubFiles)
                if (a.GameData is FTD ftd)
                {
                    if (ftdNames.Contains(a.Name))
                    {
                        if (source.TryGetValue(a.Name, out string[] import))
                        {
                            ftd.ImportText_MultiEntry_LineByLine(import, newEncoding);
                        }
                        else
                        {
                            ftd.ImportText_MultiEntry_Reimport(oldEncoding, newEncoding);
                        }
                    }
                    else if (ftdNames2.Contains(a.Name))
                    {
                        if (source.TryGetValue(a.Name, out string[] import))
                        {
                            ftd.ImportText_1Entry_LineByLine(import, newEncoding, 4);
                        }
                        else
                        {
                            ftd.ImportText_1Entry_Reimport(oldEncoding, newEncoding, 4);
                        }
                    }
                }

            File.WriteAllBytes(source_path, bin.GetData());
        }

        // ps3\field\panel.bin
        private static void PackFTD_PanelBin(string sourceDir, string newTableDir)
        {
            const string s1 = @"ps3\field\panel.bin";
            const string s12 = @"data\field\panel\mission_list\mission_list.tbl";
            const string s2 = @"PANEL_BIN.tsv";
            var source_path = Path.Combine(sourceDir, s1);
            var source_path2 = Path.Combine(sourceDir, s12);
            var table_text_path = Path.Combine(newTableDir, s2);

            if (!File.Exists(source_path))
            {
                throw new Exception($"Отсутствует файл: {s1}");
            }

            if (!File.Exists(source_path2))
            {
                throw new Exception($"Отсутствует файл: {s12}");
            }

            if (!File.Exists(table_text_path))
            {
                throw new Exception($"Отсутствует файл: {s2}");
            }

            Encoding oldEncoding = Global.OldEncoding();
            Encoding newEncoding = Global.NewEncoding();

            var tbl = new BIN(File.ReadAllBytes(source_path2));

            string[] ftdNames = new string[]
            {
                "btl_mission_title.ftd",
                "dng_mission_title.ftd",
                "fld_mission_title.ftd",
                "kfe_mission_title.ftd",
                "main_mission_title.ftd"
            };

            var source = new Dictionary<string, string[]>();

            {
                var list = new List<string>();
                string currentName = null;

                foreach (var line in File.ReadAllLines(table_text_path))
                {
                    var split = line.Split('\t');

                    if (split[0].EndsWith(".ftd"))
                    {
                        if (currentName != null)
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

            foreach (var a in tbl.SubFiles)
                if (ftdNames.Contains(a.Name))
                    if (a.GameData is FTD ftd)
                    {
                        if (source.TryGetValue(a.Name, out string[] import))
                        {
                            ftd.ImportText_MultiEntry_LineByLine(import, newEncoding);
                        }
                        else
                        {
                            ftd.ImportText_MultiEntry_Reimport(oldEncoding, newEncoding);
                        }
                    }

            File.WriteAllBytes(source_path2, tbl.GetData());

            var bin = new BIN(File.ReadAllBytes(source_path));
            var bin_ftd = bin.SubFiles[7];
            bin_ftd.GameData = tbl;

            File.WriteAllBytes(source_path, bin.GetData());
        }

        // ps3\init\camp.pak
        private static void PackFTD_CampPak(string sourceDir, string newTableDir)
        {
            const string s1 = @"ps3\init\camp.pak";
            const string s2 = @"CAMP_PAK.tsv";
            var source_path = Path.Combine(sourceDir, s1);

            if (!File.Exists(source_path))
            {
                throw new Exception($"Отсутствует файл: {s1}");
            }

            var table_text_path = Path.Combine(newTableDir, s2);

            if (!File.Exists(table_text_path))
            {
                throw new Exception($"Отсутствует файл: {s2}");
            }

            Encoding oldEncoding = Global.OldEncoding();
            Encoding newEncoding = Global.NewEncoding();

            var bin = new BIN(File.ReadAllBytes(source_path));

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
                        if (currentName != null)
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

            File.WriteAllBytes(source_path, bin.GetData());
        }
    }
}