using PersonaEditorLib.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Persona5Rus.Common
{
    internal sealed class EbootImporter
    {
        private static readonly string scetoolPath = Path.Combine(Global.ApplicationDirectory, "Tools", "eboot", "scetool.exe");
        private static readonly string scetoolEssePath = Path.Combine(Global.ApplicationDirectory, "Tools", "eboot", "scetool_esse.exe");
        private static readonly string fixelfPath = Path.Combine(Global.ApplicationDirectory, "Tools", "eboot", "FixELF.exe");

        private const string klic = "72F990788F9CFF745725F08E4C128387";

        private const int ElfOffset = 0x10000;

        private static List<(int Pos, int MaxSize, string Name)> BMDData = new List<(int Pos, int MaxSize, string Name)>()
        {
            (0xD8AE04, 0x1C3,   "BMD1.PTP"),
            (0xDE7C6C, 0x263,   "BMD2.PTP"),
            (0xDE7FD4, 0x17215, "BMD3.PTP"),
            (0xDFF1E9, 0x17292, "BMD4.PTP"),
        };

        private static List<(int Pos, int Size, string Str)> UTF8StringData = new List<(int Pos, int Size, string Str)>()
        {
            (0xB70A5C, 0x10, "START"),
            (0xB70A6C, 0x10, "SELECT"),
        };

        private static List<(int Pos, int Size, string Str)> StringData = new List<(int Pos, int Size, string Str)>()
        {
            (0xB65AD4, 0x10, "Полная свобода"), // Act Freely
            (0xB65AE4, 0x10, "Массивная атака"), // Full Assault
            (0xB65AF4, 0x10, "Сохранить ОД"), // Conserve SP
            (0xB65B04, 0x10, "Лечение/Помощь"), // Heal/Support
            (0xB65B14, 0x10, "Прямые команды"), // Direct 
            (0xB65B24, 0x0C, "Замена всем"), // Change All
            (0xB65B30, 0x04, "Нет"), // None

            (0xB7AD74, 0x08, "Защита?"), // Guard?
            (0xB7AD8C, 0x10, "Поменять?"), // Switch out?
            (0xB7ADE0, 0x10, "Полная свобода"), // Act Freely
            (0xB7ADF0, 0x10, "Массивная атака"), // Full Assault
            (0xB7AE00, 0x10, "Сохранить ОД"), // Conserve SP
            (0xB7AE10, 0x10, "Лечение/Помощь"), // Heal/Support
            (0xB7AE20, 0x10, "Прямые команды"), // Direct Commands
            (0xB7AE68, 0x10, "Вся команда"), // All Members
            

            //(0xB3FCB0, 0x08, "Безоп."), // Safe
            //(0xB3FCA0, 0x08, "Легко"), // Easy
            //(0xB3FCB8, 0x08, "Норм."), // Normal
            //(0xB3FCC0, 0x08, "Тяжело"), // Hard
            //(0xB3FCC8, 0x10, "Беспощадно"), // Merciless

            //(0xB40108, 0x08, "Безоп."), // Safe
            //(0xB40110, 0x08, "Легко"), // Easy
            //(0xB40118, 0x08, "Норм."), // Normal
            //(0xB40120, 0x08, "Тяжело"), // Hard
            //(0xB40128, 0x10, "Беспощадно"), // Merciless
            
            (0xB67064, 0x08, "Безоп."), // Safe
            (0xB6706C, 0x08, "Легко"), // Easy
            (0xB67074, 0x08, "Норм."), // Normal
            (0xB6707C, 0x08, "Тяжело"), // Hard
            (0xB67084, 0x0C, "Беспощадно"), // Merciless

            //(0xB3FC68, 0x08, "Вс"), // Sun
            //(0xB3FC70, 0x08, "Пн"), // Mon
            //(0xB3FC78, 0x08, "Вт"), // Tue
            //(0xB3FC80, 0x08, "Ср"), // Wed
            //(0xB3FC88, 0x08, "Чт"), // Thu
            //(0xB3FC90, 0x08, "Пт"), // Fri
            //(0xB3FC98, 0x08, "Сб"), // Sat 
            
            //(0xB40068, 0x08, "Вс"), // Sun
            //(0xB40070, 0x08, "Пн"), // Mon
            //(0xB40078, 0x08, "Вт"), // Tue
            //(0xB40080, 0x08, "Ср"), // Wed
            //(0xB40088, 0x08, "Чт"), // Thu
            //(0xB40090, 0x08, "Пт"), // Fri
            //(0xB40098, 0x08, "Сб"), // Sat
            
            (0xB66B88, 0x08, "Вс"), // Sun
            (0xB66B90, 0x08, "Пн"), // Mon
            (0xB66B98, 0x08, "Вт"), // Tue
            (0xB66BA0, 0x08, "Ср"), // Wed
            (0xB66BA8, 0x08, "Чт"), // Thu
            (0xB66BB0, 0x08, "Пт"), // Fri
            (0xB66BB8, 0x08, "Сб"), // Sat
            
            //(0xB68C24, 0x08, "Вс"), // Sun
            //(0xB68C2C, 0x08, "Пн"), // Mon
            //(0xB68C34, 0x08, "Вт"), // Tue
            //(0xB68C3C, 0x08, "Ср"), // Wed
            //(0xB68C44, 0x08, "Чт"), // Thu
            //(0xB68C4C, 0x08, "Пт"), // Fri
            //(0xB68C54, 0x08, "Сб"), // Sat

            (0xD8F31C, 0x30, "Вещи & Инструменты"), // Items & Tools
            (0xD8F41C, 0x30, "Карты навыков"), // Skill Cards
            (0xD8F51C, 0x30, "Материалы"), // Materials
            (0xD8F61C, 0x30, "Сокровища"), // Treasure
            (0xD8F71C, 0x30, "Важное"), // Essentials
            (0xD8F81C, 0x30, "Ключевые предметы"), // Key Items

            (0xB3E850, 0x08, "Сл"), // St
            (0xB3E858, 0x08, "Мг"), // Ma
            (0xB3E860, 0x08, "Вн"), // En
            (0xB3E868, 0x08, "Лв"), // Ag
            (0xB3E870, 0x08, "Уд"), // Lu
            (0xB3E878, 0x10, "Все статы"), // All stats
            (0xB3E888, 0x08, "+"), // +

            (0xB67000, 0x24, "Нет доступных руководств."), // There are no tutorials available.
            (0xB67024, 0x10, "Вакансии"), // Job Listings
            (0xB67034, 0x0C, "История"), // Story Title
            //(0xB67040, 0x24, "Нет доступных руководств."), // There are no tutorials available.
            
            (0xD40314, 0x40, "Сокровища"), // Treasure
            (0xD40413, 0x40, "Холодное оружие"), // Melee Weapons
            (0xD40512, 0x40, "Оружие дального боя"), // Ranged Weapons
            (0xD40611, 0x40, "Броня"), // Protectors
            (0xD40710, 0x40, "Аксессуары"), // Accessories
            (0xD4080F, 0x08, "Вещи"), // Items

            (0xB7395C, 0x08, "Защита"), // Guard
            
            (0xB65ED8, 0x14, "Кто же это?.."), // Who could it be...?
            (0xB65DEC, 0x14, "--Не используется--"), // --Unequippable--
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

        public void ImportBIN(string binPath)
        {
            var elfPath = Path.Combine(Path.GetDirectoryName(binPath), Path.GetFileNameWithoutExtension(binPath) + ".ELF");

            string CID = string.Empty;

            // Получаем ContentID из оригинального BIN
            {
                var scetoolProcess = new Process();
                scetoolProcess.StartInfo.FileName = scetoolPath;
                scetoolProcess.StartInfo.Arguments = $"-i \"{binPath}\"";
                scetoolProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(scetoolPath);
                scetoolProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                scetoolProcess.StartInfo.RedirectStandardOutput = true;
                scetoolProcess.StartInfo.UseShellExecute = false;

                scetoolProcess.Start();
                var result = scetoolProcess.StandardOutput.ReadToEnd();
                scetoolProcess.WaitForExit();

                var content = Regex.Match(result, "ContentID {1,}(.)*", RegexOptions.Multiline);
                if (content.Success)
                {
                    CID = content.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1].TrimEnd(new char[] { '\r', '\n' });
                }
            }


            // Извелкаем ELF из оригинального BIN
            {
                var scetoolProcess = new Process();
                scetoolProcess.StartInfo.FileName = scetoolPath;
                scetoolProcess.StartInfo.Arguments = $"-d \"{binPath}\" \"{elfPath}\"";
                scetoolProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(scetoolPath);
                scetoolProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                scetoolProcess.Start();
                scetoolProcess.WaitForExit();
            }

            File.Delete(binPath);
            Import(elfPath);

            // Накатываем фикс на ELF
            {
                var fixelfProcess = new Process();
                fixelfProcess.StartInfo.FileName = fixelfPath;
                fixelfProcess.StartInfo.Arguments = $"\"{elfPath}\" \"24 13 BC C5 F6 00 33 00 00 00 36\" \"24 13 BC C5 F6 00 33 00 00 00 34\"";
                fixelfProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(fixelfPath);
                fixelfProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                fixelfProcess.Start();
                fixelfProcess.WaitForExit();
            }

            // Обартно подписываем ELF в BIN

            if (string.IsNullOrEmpty(CID))
            {
                var scetoolEsseProcess = new Process();
                scetoolEsseProcess.StartInfo.FileName = scetoolEssePath;
                scetoolEsseProcess.StartInfo.Arguments = $"-v --sce-type=SELF --compress-data=FALSE --skip-sections=FALSE --key-revision=01 --self-auth-id=1010000001000003 --self-vendor-id=01000002 --self-type=APP --self-app-version=0001000000000000 --self-fw-version=0003003000000000 --self-add-shdrs=TRUE --self-ctrl-flags=0000000000000000000000000000000000000000000000000000000000000000 --self-cap-flags=00000000000000000000000000000000000000000000003B0000000100004000 " +
                    $"--encrypt \"{elfPath}\" \"{binPath}\"";
                scetoolEsseProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(scetoolEssePath);
                scetoolEsseProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                scetoolEsseProcess.Start();
                scetoolEsseProcess.WaitForExit();
            }
            else
            {
                var scetoolEsseProcess = new Process();
                scetoolEsseProcess.StartInfo.FileName = scetoolEssePath;
                scetoolEsseProcess.StartInfo.Arguments = $"-v --sce-type=SELF --compress-data=FALSE --skip-sections=FALSE --key-revision=01 --self-auth-id=1010000001000003 --self-vendor-id=01000002 --self-type=NPDRM --self-app-version=0001000000000000 --self-fw-version=0003003000000000 --self-add-shdrs=TRUE --self-ctrl-flags=0000000000000000000000000000000000000000000000000000000000000000 --self-cap-flags=00000000000000000000000000000000000000000000003B0000000100002000 " +
                    $"--np-license-type=FREE --np-app-type=EXEC --np-content-id={CID} --np-klicensee={klic} --np-real-fname=EBOOT.BIN " +
                    $"--encrypt \"{elfPath}\" \"{binPath}\"";
                scetoolEsseProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(scetoolEssePath);
                scetoolEsseProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                scetoolEsseProcess.Start();
                scetoolEsseProcess.WaitForExit();
            }
        }

        private void Import(string elfPath)
        {
            var ebootData = File.ReadAllBytes(elfPath);

            using (var MS = new MemoryStream(ebootData))
            {
                PatchModCPK(MS);
                PatchCalendar(MS);

                foreach (var bmd in BMDData)
                {
                    PackPart(MS, bmd.Name, bmd.Pos, bmd.MaxSize);
                }

                foreach (var str in UTF8StringData)
                {
                    PackUTF8String(MS, str.Pos, str.Size, str.Str);
                }

                foreach (var str in StringData)
                {
                    PackString(MS, str.Pos, str.Size, str.Str);
                }
            }

            File.WriteAllBytes(elfPath, ebootData);
        }

        #region Patches

        private static List<(int, int)> Patch = new List<(int, int)>()
        {
            // make %s/hdd.cpk -> %s%s/mod.cpk
            (0x00B4D638, 0x25732573),
            (0x00B4D63C, 0x2F6D6F64),
            (0x00B4D640, 0x2E63706B),

            // make mod.cpk file path
            (0x00114CA4, 0x3C6000B5),
            (0x00114CA8, 0x33E3D638),
            (0x00114CAC, 0x48968BEB),
            (0x00114CB0, 0x60000000),
            (0x00114CB4, 0x7C7E1B78),
            (0x00114CB8, 0x48968BF7),
            (0x00114CBC, 0x60000000),
            (0x00114CC0, 0x33A10070),
            (0x00114CC4, 0x7C661B78),
            (0x00114CC8, 0x7FA3EB78),
            (0x00114CCC, 0x7FE4FB78),
            (0x00114CD0, 0x7FC5F378),
            (0x00114CD4, 0x48AD567F),
            (0x00114CD8, 0x60000000),
            (0x00114CDC, 0x48B44A9E),
            (0x00114CE0, 0x60000000),

            // trampoline
            (0x00B44A9C, 0x7FA3EB78),
            (0x00B44AA0, 0x48114B77),
            (0x00B44AA4, 0x60000000),
            (0x00B44AA8, 0x3880000A),
            (0x00B44AAC, 0x48AB8ED7),
            (0x00B44AB0, 0x60000000),
            (0x00B44AB4, 0x48114CE6),
            (0x00B44AB8, 0x60000000),
        };

        private void PatchModCPK(MemoryStream MS)
        {
            foreach (var patch in Patch)
            {
                MS.Position = patch.Item1 - ElfOffset;
                var bytes = BitConverter.GetBytes(patch.Item2).Reverse().ToArray();
                MS.Write(bytes, 0, 4);
            }
        }

        private const int CalendarPatchPos = 0xB66BD0;
        private static readonly byte[] CalendarPatch = new byte[] { 0xEF, 0xBC, 0x80, 0x25, 0x73, 0xEF, 0xBC, 0x80 };

        // Убираем скобки в календаре у дней недели
        private void PatchCalendar(MemoryStream MS)
        {
            MS.Position = CalendarPatchPos;
            MS.Write(CalendarPatch, 0, CalendarPatch.Length);
        }

        #endregion

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

        private void PackUTF8String(MemoryStream MS, int pos, int size, string str)
        {
            var data1 = newEncoding.GetBytes(str);
            var str1 = oldEncoding.GetString(data1);
            var data = Encoding.UTF8.GetBytes(str1);

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