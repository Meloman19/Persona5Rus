using AuxiliaryLibraries.Extensions;
using PersonaEditorLib.Other;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Persona5Rus.Common
{
    public static class Extensions
    {
        public static void ImportText_1Entry_LineByLine(this FTD ftd, string[] import, Encoding newEncoding, int leftPadding = 0, int rightPadding = 0)
        {
            var a = ftd.Entries[0];

            var index = 0;

            foreach (var entry in a)
            {
                byte[] oldArray = entry.SubArray(leftPadding, entry.Length - rightPadding - leftPadding);
                if (Encoding.ASCII.GetString(oldArray) == "NULL" || oldArray.Length <= 1)
                {
                    index++;
                    continue;
                }

                string value = import[index];
                index++;

                byte[] buffer = TrimLength(value, newEncoding, entry.Length - leftPadding - rightPadding - 1);

                for (int i = leftPadding; i < entry.Length - rightPadding; i++)
                    entry[i] = 0;

                if (buffer.Length >= entry.Length - leftPadding - rightPadding)
                    Buffer.BlockCopy(buffer, 0, entry, leftPadding, entry.Length - leftPadding - rightPadding - 1);
                else
                    Buffer.BlockCopy(buffer, 0, entry, leftPadding, buffer.Length);
            }
        }

        public static void ImportText_1Entry_Reimport(this FTD ftd, Encoding oldEncoding, Encoding newEncoding, int leftPadding = 0, int rightPadding = 0)
        {
            var a = ftd.Entries[0];

            foreach (var entry in a)
            {
                byte[] oldArray = entry.SubArray(leftPadding, entry.Length - rightPadding - leftPadding);
                if (Encoding.ASCII.GetString(oldArray) == "NULL" || oldArray.Length <= 1)
                    continue;

                string value = oldEncoding.GetString(oldArray).TrimEnd('\0');

                byte[] buffer = TrimLength(value, newEncoding, entry.Length - leftPadding - rightPadding - 1);

                for (int i = leftPadding; i < entry.Length - rightPadding; i++)
                    entry[i] = 0;

                if (buffer.Length >= entry.Length - leftPadding - rightPadding)
                    Buffer.BlockCopy(buffer, 0, entry, leftPadding, entry.Length - leftPadding - rightPadding - 1);
                else
                    Buffer.BlockCopy(buffer, 0, entry, leftPadding, buffer.Length);
            }
        }

        public static void ImportText_MultiEntry_LineByLine(this FTD ftd, string[] import, Encoding newEncoding, int leftPadding = 0, int rightPadding = 0)
        {
            var index = 0;

            foreach (var entries in ftd.Entries)
            {
                var entry = entries[0];

                byte[] oldArray = entry.SubArray(leftPadding, entry.Length - rightPadding - leftPadding);
                if (Encoding.ASCII.GetString(oldArray).Trim('\0') == "NULL" || oldArray.Length <= 1)
                {
                    index++;
                    continue;
                }

                byte[] buffer = null;
                string value = import[index];
                index++;

                buffer = newEncoding.GetBytes(value).Concat(new byte[] { 0 }).ToArray();

                entries[0] = buffer;
            }
        }

        public static void ImportText_MultiEntry_Reimport(this FTD ftd, Encoding oldEncoding, Encoding newEncoding, int leftPadding = 0, int rightPadding = 0)
        {
            var index = 0;

            foreach (var entries in ftd.Entries)
            {
                var entry = entries[0];

                byte[] oldArray = entry.SubArray(leftPadding, entry.Length - rightPadding - leftPadding);
                if (Encoding.ASCII.GetString(oldArray) == "NULL")
                    continue;

                byte[] buffer = null;
                string value = oldEncoding.GetString(oldArray).TrimEnd('\0');
                index++;

                buffer = newEncoding.GetBytes(value).Concat(new byte[] { 0 }).ToArray();

                entries[0] = buffer;
            }
        }

        public static void ImportText(this FTD ftd, List<string[]> text, Encoding oldEncoding, Encoding newEcoding, int leftPadding = 0, int rightPadding = 0)
        {
            foreach (var a in ftd.Entries)
            {
                if (a.Length == 1)
                {
                    string old = oldEncoding.GetString(a[0]).TrimEnd('\0');
                    var findLine = text.Find(x => x[0] == old && x[1] != "");
                    if (findLine != null)
                        a[0] = newEcoding.GetBytes(findLine[1]).Concat(new byte[] { 0 }).ToArray();
                    else
                        a[0] = newEcoding.GetBytes(old).Concat(new byte[] { 0 }).ToArray();
                }
                else
                {
                    foreach (var entry in a)
                    {
                        byte[] oldArray = entry.SubArray(leftPadding, entry.Length - rightPadding - leftPadding);
                        if (Encoding.ASCII.GetString(oldArray) == "NULL")
                            continue;

                        string old = oldEncoding.GetString(oldArray).TrimEnd('\0');

                        var findLine = text.Find(x => x[0] == old && x[1] != "");

                        byte[] buffer = null;
                        if (findLine != null)
                            buffer = newEcoding.GetBytes(findLine[1]);
                        else
                            buffer = newEcoding.GetBytes(old);

                        for (int i = leftPadding; i < entry.Length - rightPadding; i++)
                            entry[i] = 0;

                        if (buffer.Length >= entry.Length - leftPadding - rightPadding)
                            Buffer.BlockCopy(buffer, 0, entry, leftPadding, entry.Length - leftPadding - rightPadding - 1);
                        else
                            Buffer.BlockCopy(buffer, 0, entry, leftPadding, buffer.Length);
                    }
                }
            }
        }

        public static void Import_cmmPC_PARAM_Help(this FTD ftd, string[] import, Encoding newEncoding)
        {
            if (import.Length != 25)
            {
                throw new Exception("Не то количество элементов");
            }

            var index = 0;

            for(int entryIndex = 0; entryIndex < 5; entryIndex++)
            {
                IEnumerable<byte> entryEnum = Enumerable.Empty<byte>();

                for(int i = 0; i< 5; i++)
                {
                    var str = import[index];

                    var data = TrimLength(str, newEncoding, 19);

                    entryEnum = entryEnum.Concat(data);

                    var add = 20 - data.Length;

                    entryEnum = entryEnum.Concat(Enumerable.Repeat((byte)0, add));

                    index++;
                }

                ftd.Entries[0][entryIndex] = entryEnum.ToArray();
            }
        }

        public static void ImportText_fldPanelMsg(this FTD ftd, string[] import, Encoding newEncoding)
        {
            var index = 0;
            foreach (var entry in ftd.Entries[0])
            {
                using (var MS = new MemoryStream(entry))
                {
                    while (true)
                    {
                        var buffer = new byte[0x20];
                        var curPos = MS.Position;
                        var readed = MS.Read(buffer, 0, 0x20);

                        if (readed == 0x20)
                        {
                            var value = import[index];
                            index++;

                            if (!Encoding.ASCII.GetString(buffer).StartsWith("NULL"))
                            {
                                MS.Position = curPos;

                                for (int i = 0; i < 20; i++)
                                {
                                    MS.WriteByte(0);
                                }

                                MS.Position = curPos;

                                var data = TrimLength(value, newEncoding, 19);
                                for (int i = 0; i < data.Length; i++)
                                {
                                    MS.WriteByte(data[i]);
                                }
                            }

                            MS.Position = curPos + 0x30;
                        }
                        else break;
                    }
                }
            }
        }

        private static byte[] TrimLength(string value, Encoding encoding, int maxLength)
        {
            byte[] buffer;
            while (true)
            {
                buffer = encoding.GetBytes(value);

                if (buffer.Length <= maxLength)
                    break;
                else
                {
                    value = value.Substring(0, value.Length - 1);
                }
            }

            return buffer;
        }
    }
}
