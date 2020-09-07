using PersonaEditorLib.Text;
using System;
using System.IO;
using System.Text;

namespace Persona5Rus.Common
{
    internal static class ImportSteps_EBOOT
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

        private const int STARTPos = 0xB713DC;
        private const int STARTSize = 0xF;

        private const int SELECTPos = 0xB713EC;
        private const int SELECTSize = 0xF;

        public static void PackEBOOT(string ebootPath, string ptp)
        {
            const string s1 = @"eboot";

            var ebootPTPDir = Path.Combine(ptp, s1);

            if (!Directory.Exists(ebootPTPDir))
            {
                throw new Exception($"{s1}");
            }

            var ebootData = File.ReadAllBytes(ebootPath);

            using (var MS = new MemoryStream(ebootData))
            {
                var newEncoding = Static.NewEncoding();

                PackPart(MS, newEncoding, ebootPTPDir, BMD1Name, BMD1Pos, BMD1MaxSize);
                PackPart(MS, newEncoding, ebootPTPDir, BMD2Name, BMD2Pos, BMD2MaxSize);
                PackPart(MS, newEncoding, ebootPTPDir, BMD3Name, BMD3Pos, BMD3MaxSize);
                PackPart(MS, newEncoding, ebootPTPDir, BMD4Name, BMD4Pos, BMD4MaxSize);

                PackString(MS, newEncoding, STARTPos, STARTSize, "START");
                PackString(MS, newEncoding, SELECTPos, SELECTSize, "SELECT");
            }

            File.WriteAllBytes(ebootPath, ebootData);
        }

        private static void PackPart(MemoryStream MS, Encoding newEncoding, string ebootPTPDir, string BMDName, int pos, int size)
        {
            var ptpPath = Path.Combine(ebootPTPDir, BMDName);
            var ptp = new PTP(File.ReadAllBytes(ptpPath));
            var bmd = new BMD(ptp, newEncoding)
            {
                IsLittleEndian = false
            };
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

        private static void PackString(MemoryStream MS, Encoding newEncoding, int pos, int size, string str)
        {
            var data = newEncoding.GetBytes(str);

            if(data.Length > size)
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
