using AuxiliaryLibraries.Extensions;
using PersonaEditorLib.Other;
using System;
using System.Collections.Generic;
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
                if (Encoding.ASCII.GetString(oldArray) == "NULL")
                    continue;

                byte[] buffer = null;
                string value = import[index];
                index++;

                while (true)
                {
                    buffer = newEncoding.GetBytes(value);

                    if (buffer.Length < entry.Length - leftPadding - rightPadding - 1)
                        break;
                    else
                    {
                        value = value.Substring(0, value.Length - 1);
                    }
                }

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
                if (Encoding.ASCII.GetString(oldArray) == "NULL")
                    continue;

                byte[] buffer = null;
                string value = oldEncoding.GetString(oldArray).TrimEnd('\0');

                while (true)
                {
                    buffer = newEncoding.GetBytes(value);

                    if (buffer.Length < entry.Length - leftPadding - rightPadding - 1)
                        break;
                    else
                    {
                        value = value.Substring(0, value.Length - 1);
                    }
                }

                for (int i = leftPadding; i < entry.Length - rightPadding; i++)
                    entry[i] = 0;

                if (buffer.Length >= entry.Length - leftPadding - rightPadding)
                    Buffer.BlockCopy(buffer, 0, entry, leftPadding, entry.Length - leftPadding - rightPadding - 1);
                else
                    Buffer.BlockCopy(buffer, 0, entry, leftPadding, buffer.Length);
            }
        }

        public static void ImportTextOneEntry(this FTD ftd, Dictionary<string, string> import, Encoding oldEncoding, Encoding newEncoding, int leftPadding = 0, int rightPadding = 0)
        {
            var a = ftd.Entries[0];

            foreach (var entry in a)
            {
                byte[] oldArray = entry.SubArray(leftPadding, entry.Length - rightPadding - leftPadding);
                if (Encoding.ASCII.GetString(oldArray) == "NULL")
                    continue;

                string old = oldEncoding.GetString(oldArray).TrimEnd('\0');

                byte[] buffer = null;
                if (import.TryGetValue(old, out string value)
                    && string.IsNullOrEmpty(value))
                    buffer = newEncoding.GetBytes(value);
                else
                    buffer = newEncoding.GetBytes(old);

                for (int i = leftPadding; i < entry.Length - rightPadding; i++)
                    entry[i] = 0;

                if (buffer.Length >= entry.Length - leftPadding - rightPadding)
                    Buffer.BlockCopy(buffer, 0, entry, leftPadding, entry.Length - leftPadding - rightPadding - 1);
                else
                    Buffer.BlockCopy(buffer, 0, entry, leftPadding, buffer.Length);
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
    }
}
