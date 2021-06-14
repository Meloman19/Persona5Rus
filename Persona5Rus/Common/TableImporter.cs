using PersonaEditorLib;
using PersonaEditorLib.FileContainer;
using PersonaEditorLib.Other;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Persona5Rus.Common
{
    internal sealed class TableImporter
    {
        private readonly string tableText;
        private readonly Dictionary<string, Action<GameFile>> actions;

        private readonly Encoding oldEncoding;
        private readonly Encoding newEncoding;

        public TableImporter(string tableText, Encoding oldEncoding, Encoding newEncoding)
        {
            this.oldEncoding = oldEncoding ?? throw new ArgumentNullException(nameof(oldEncoding));
            this.newEncoding = newEncoding ?? throw new ArgumentNullException(nameof(newEncoding));
            this.tableText = tableText;
            actions = new Dictionary<string, Action<GameFile>>()
            {
                { @"battle\table.pac", Pack_TalbePac },
                { @"calendar\goodGauge.pac", PackFTD_goodGaugePac },
                { @"field\panel\mission_list\mission_list.tbl", PackFTD_MissionListTbl },
                { @"field\panel\roadmap\roadmap.tbl", Pack_Roadmap },
                { @"field\panel\fldPanelLmap.pac", PackFTD_fldPanelLmap },
                { @"field\panel\fldPanelMsg.pac", PackFTD_fldPanelMsg },
                { @"field\panel\fldPanelMsgDng.pac", PackFTD_fldPanelMsgDng },
                { @"field\fldResident.pac", PackFTD_fldResident },
                { @"field\panel.bin", PackFTD_PanelBin },
                { @"init\camp.pak", PackFTD_CampPak },
                { @"init\cmm.bin", Pack_CmmBin },
                { @"init\facility.pak", Pack_FacilityPak },
                { @"init\fclTable.bin", Pack_fclTableBin },
                { @"init\ttrTable.bin", Pack_ttrTableBin },
            };
        }

        public bool Import(GameFile file, string cpkPath)
        {
            if (!actions.TryGetValue(cpkPath, out Action<GameFile> action))
            {
                return false;
            }

            action(file);
            return true;
        }

        // battle\table.pac
        private void Pack_TalbePac(GameFile file)
        {
            const string s2 = @"NAME_TBL.tsv";

            var bin = file.GameData as BIN;

            string[][] source = null;
            {
                var listS = new List<string[]>();
                var list = new List<string>();
                bool started = false;

                var table_text_path = Path.Combine(tableText, s2);
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
        }

        // field\panel\roadmap\roadmap.tbl
        private void Pack_Roadmap(GameFile file)
        {
            const string s2 = @"ROADMAP_TBL.tsv";

            var bin = file.GameData as BIN;

            string[] ftdNames = new string[]
            {
                "fld_texpack_title.ftd"
            };

            var source = new Dictionary<string, string[]>();

            {
                var list = new List<string>();
                string currentName = null;

                var table_text_path = Path.Combine(tableText, s2);

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
        }

        // init\cmm.bin
        private void Pack_CmmBin(GameFile file)
        {
            const string s2 = @"CMM_BIN.tsv";

            var bin = file.GameData as BIN;

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

                var table_text_path = Path.Combine(tableText, s2);
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
        }

        // init\facility.pak
        private void Pack_FacilityPak(GameFile file)
        {
            var bin = file.GameData as BIN;

            var fcl = bin.SubFiles.Find(gd => gd.Name == "fclTable.bin");
            Pack_fclTableBin(fcl);
        }

        // init\fclTable.bin
        private void Pack_fclTableBin(GameFile file)
        {
            const string s2 = @"FACILITY_PAK.tsv";

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

                var table_text_path = Path.Combine(tableText, s2);
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

            var bin = file.GameData as BIN;

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
        }

        // init\ttrTable.bin"
        private void Pack_ttrTableBin(GameFile file)
        {
            const string s2 = @"TTRTABLE_BIN.tsv";

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

                var table_text_path = Path.Combine(tableText, s2);
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

            var bin = file.GameData as BIN;

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
        }

        // calendar\goodGauge.pac
        private void PackFTD_goodGaugePac(GameFile file)
        {
            const string s2 = @"GOODGAUGE_PAC.tsv";

            var bin = file.GameData as BIN;

            string[] ftdNames = new string[]
            {
                "cldCommentTable.ftd",
                "cldEvtCommentTable.ftd"
            };

            var source = new Dictionary<string, string[]>();
            {
                var list = new List<string>();
                string currentName = null;

                var table_text_path = Path.Combine(tableText, s2);
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
        }

        // field\panel\fldPanelLmap.pac
        private void PackFTD_fldPanelLmap(GameFile file)
        {
            const string s2 = @"FLDPANELLMAP_PAC.tsv";

            var source = new Dictionary<string, string[]>();
            {
                var list = new List<string>();
                string currentName = null;

                var table_text_path = Path.Combine(tableText, s2);
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

            const string ftd1Name = @"lmap/fldLMapStation.ftd";
            const string ftd2Name = @"lmap/fldLMapLockedCorpName.ftd";

            var bin = file.GameData as BIN;

            if (source.TryGetValue(ftd1Name, out string[] import))
            {
                var ftdGD = bin.SubFiles.Find(gd => gd.Name == ftd1Name);
                var ftd = ftdGD.GameData as FTD;

                ftd.ImportText_fldLMapStation(import, newEncoding);
            }

            if (source.TryGetValue(ftd2Name, out string[] import2))
            {
                var ftdGD = bin.SubFiles.Find(gd => gd.Name == ftd2Name);
                var ftd = ftdGD.GameData as FTD;

                for (int i = 0; i < import2.Length; i++)
                {
                    if (string.IsNullOrEmpty(import2[i]))
                    {
                        import2[i] = " ";
                    }
                }
                if (import2.Length < ftd.Entries[0].Length)
                {
                    import2 = import2.Concat(Enumerable.Repeat(" ", ftd.Entries[0].Length - import2.Length)).ToArray();
                }

                ftd.ImportText_1Entry_LineByLine(import2, newEncoding);
            }
        }

        // field\panel\fldPanelMsg.pac
        private void PackFTD_fldPanelMsg(GameFile file)
        {
            const string s2 = @"FLDPANELMSG_PAC.tsv";

            var source = new Dictionary<string, string[]>();
            {
                var list = new List<string>();
                string currentName = null;

                var table_text_path = Path.Combine(tableText, s2);
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

            var bin = file.GameData as BIN;

            var ftd = bin.SubFiles.Find(gd => gd.Name == "fldWholeMapTable.ftd");
            var ftdData = ftd.GameData as FTD;

            source.TryGetValue("fldWholeMapTable.ftd", out string[] import);

            ftdData.ImportText_fldPanelMsg(import, newEncoding);
        }

        // field\panel\fldPanelMsgDng.pac
        private void PackFTD_fldPanelMsgDng(GameFile file)
        {
            const string s2 = @"FLDPANELMSGDNG_PAC.tsv";

            var source = new Dictionary<string, string[]>();
            {
                var list = new List<string>();
                string currentName = null;

                var table_text_path = Path.Combine(tableText, s2);
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

            var bin = file.GameData as BIN;

            var ftd = bin.SubFiles.Find(gd => gd.Name == "fldWholeMapTableDng.ftd");
            var ftdData = ftd.GameData as FTD;

            source.TryGetValue("fldWholeMapTableDng.ftd", out string[] import);

            ftdData.ImportText_fldPanelMsg(import, newEncoding);
        }

        // field\fldResident.pac
        private void PackFTD_fldResident(GameFile file)
        {
            const string s2 = @"FLDRESIDENT_PAC.tsv";

            var bin = file.GameData as BIN;

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

                var table_text_path = Path.Combine(tableText, s2);
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
        }

        // field\panel.bin
        private void PackFTD_PanelBin(GameFile file)
        {
            var bin = file.GameData as BIN;
            var bin_ftd = bin.SubFiles[7];
            PackFTD_MissionListTbl(bin_ftd);
        }

        // field\panel\mission_list\mission_list.tbl
        private void PackFTD_MissionListTbl(GameFile file)
        {
            const string s2 = @"PANEL_BIN.tsv";

            var tbl = file.GameData as BIN;

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

                var table_text_path = Path.Combine(tableText, s2);
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
        }

        // init\camp.pak
        private void PackFTD_CampPak(GameFile file)
        {
            const string s2 = @"CAMP_PAK.tsv";

            var bin = file.GameData as BIN;

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

                var table_text_path = Path.Combine(tableText, s2);

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
        }
    }
}