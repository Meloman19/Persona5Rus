using PersonaEditorLib.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Persona5Rus.Common
{
    class MSGSplitter
    {
        private static readonly byte[] DefaultZero = new byte[] { 0x00 };
        private static readonly byte[] DefaultNewLine = new byte[] { 0x0A };

        private static readonly byte[] DefaultMsgStart1 = new byte[] { 0xF2, 0x05, 0xFF, 0xFF };
        private static readonly byte[] DefaultMsgStart2 = new byte[] { 0xF1, 0x41 };

        private static readonly byte[] DefaultMsgEnd1 = new byte[] { 0xF1, 0x21 };

        private static readonly byte[] Start_Chat1 = new byte[] { 0xF1, 0xAE };
        private static readonly byte[] Start_Chat2 = new byte[] { 0xF1, 0xAF };

        private static readonly byte[][] StartMasks = new[]
        {
            new byte[] { 0xF2, 0x87 },
            new byte[] { 0xF2, 0x9D },
            new byte[] { 0xF3, 0x06 },
            new byte[] { 0xF4, 0x8A },
            new byte[] { 0xF4, 0x96 },
            new byte[] { 0xF4, 0xAD },
            new byte[] { 0xF5, 0x8E },
            new byte[] { 0xF5, 0x99 },
            new byte[] { 0xF6, 0x86 },
            new byte[] { 0xF6, 0x90 },
            new byte[] { 0xF7, 0x61 },
            new byte[] { 0xF7, 0x97 },
            new byte[] { 0xF7, 0x98 },
            new byte[] { 0xFD, 0x91 }
        };

        private static readonly byte[][] EndMasks = new[]
        {
            new byte[] { 0xF2, 0x22 },
            new byte[] { 0xF2, 0x23 },
            new byte[] { 0xF2, 0x26 },
            new byte[] { 0xF5, 0x47 }
        };

        private static readonly byte[] StartEndMask1 = new byte[] { 0xF7, 0x61 };

        public MSGSplitter(byte[] data, bool last)
        {
            Body = data.GetTextBases().ToList();

            if (last)
            {
                var lst = Body[Body.Count - 1];
                if (lst.IsText)
                {
                    throw new Exception("0");
                }
                if (!lst.Data.SequenceEqual(DefaultZero))
                {
                    throw new Exception("0-1");
                }

                Body.Remove(lst);
                Postfix.Add(lst);
            }

            if (Body.Count != 0)
            {
                FillPrefix();
                FillPostfix();
            }
        }

        private void FillPrefix()
        {
            {
                var start1 = Body[0];
                if (start1.IsText)
                {
                    throw new Exception("1");
                }
                if (!start1.Data.SequenceEqual(DefaultMsgStart1))
                {
                    throw new Exception("1-1");
                }

                Body.Remove(start1);
                Prefix.Add(start1);
            }

            {
                var start2 = Body[0];
                if (start2.IsText)
                {
                    throw new Exception("2");
                }
                if (!start2.Data.SequenceEqual(DefaultMsgStart2))
                {
                    throw new Exception("2-1");
                }

                Body.Remove(start2);
                Prefix.Add(start2);
            }

            while (true)
            {
                if (Body.Count == 0)
                {
                    break;
                }

                var fst = Body[0];

                if (fst.IsText)
                {
                    break;
                }

                if (fst.Data.SequenceEqual(Start_Chat1)
                    || fst.Data.SequenceEqual(Start_Chat2)
                    || StartMasks.Any(mask => fst.Data.SequenceMaskEqual(mask))
                    || fst.Data.SequenceMaskEqual(StartEndMask1))
                {
                    Body.Remove(fst);
                    Prefix.Add(fst);
                }
                else
                {
                    break;
                }
            }
        }

        private void FillPostfix()
        {
            while (true)
            {
                if (Body.Count == 0)
                {
                    break;
                }

                var lst = Body[Body.Count - 1];

                if (lst.IsText)
                {
                    break;
                }

                if (lst.Data.SequenceEqual(DefaultZero)
                    || lst.Data.SequenceEqual(DefaultMsgEnd1)
                    || lst.Data.SequenceEqual(Start_Chat2)
                    || EndMasks.Any(mask => lst.Data.SequenceMaskEqual(mask))
                    || lst.Data.SequenceMaskEqual(StartEndMask1))
                {
                    Body.Remove(lst);
                    Postfix.Insert(0, lst);
                }
                else if (lst.Data.SequenceEqual(DefaultNewLine))
                {
                    Body.Remove(lst);
                    Postfix.Insert(0, lst);
                    break;
                }
                else
                {
                    break;
                }
            }
        }

        public List<TextBaseElement> Prefix { get; } = new List<TextBaseElement>();
        public List<TextBaseElement> Body { get; private set; }
        public List<TextBaseElement> Postfix { get; } = new List<TextBaseElement>();

        public void ChangeBody(string str, Encoding newEncoding, Dictionary<char, int> charWidth)
        {
            string splittedStr;
            if (str.StartsWith("{F1 25}") && str.IndexOf("{0A}") != -1)
            {
                // Случай, если имя вынесено в текст

                var endIndex = str.IndexOf("{0A}");

                var name = str.Substring(0, endIndex + 4);
                var str2 = str.Substring(endIndex + 4, str.Length - (endIndex + 4));

                var lineCount = Body.Count(e => e.Data.SequenceEqual(DefaultNewLine));
                splittedStr = name + str2.SplitByLineCount(charWidth, lineCount);
            }
            else
            {
                var lineCount = Body.Count(e => e.Data.SequenceEqual(DefaultNewLine)) + 1;
                splittedStr = str.SplitByLineCount(charWidth, lineCount);
            }

            Body = splittedStr.GetTextBases(newEncoding).ToList();
        }

        public void ChangeEncoding(Encoding oldEncoding, Encoding newEncoding)
        {
            List<TextBaseElement> newBody = new List<TextBaseElement>();
            foreach(var el in Body)
            {
                if (el.IsText)
                {
                    var str = oldEncoding.GetString(el.Data);
                    var newData = newEncoding.GetBytes(str);
                    newBody.Add(new TextBaseElement(true, newData));
                }
                else
                {
                    newBody.Add(el);
                }
            }
            Body = newBody;
        }

        public byte[] GetData()
        {
            return Prefix.Concat(Body).Concat(Postfix).GetByteArray();
        }
    }

    public static class Exten
    {
        public static bool SequenceMaskEqual<T>(this IEnumerable<T> first, IEnumerable<T> second)
        {
            var comparer = EqualityComparer<T>.Default;

            using (var firstEnumerator = first.GetEnumerator())
            {
                using (var secondEnumerator = second.GetEnumerator())
                {
                    while (true)
                    {
                        if (secondEnumerator.MoveNext())
                        {
                            if (firstEnumerator.MoveNext())
                            {
                                if (!comparer.Equals(firstEnumerator.Current, secondEnumerator.Current))
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
            }
        }
    }
}