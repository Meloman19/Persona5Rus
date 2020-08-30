using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using AuxiliaryLibraries.Extensions;
using AuxiliaryLibraries.Tools;
using PersonaEditorLib;
using PersonaEditorLib.FileContainer;
using PersonaEditorLib.Other;
using PersonaEditorLib.Text;

namespace Persona5Rus.Common
{
    static class ImportSteps
    {
        public static void ImportTextToPTP(string PTPDir, string NewTextDir, IProgress<double> progress)
        {
            //Logging.Write("ImportTextToPTP started");
            string NAMEfile = Path.Combine(NewTextDir, "names.tsv");
            int width = 0;

            if (!Directory.Exists(PTPDir))
            {
                //Logging.Write("PTP Directory does not exist.");
                return;
            }

            //Console.WriteLine("Text will be overwritten.Overwrite anyway ? Input \"YES\"");
            //if (Console.ReadLine() != "YES")
            //    return;

            Encoding oldEncoding = Static.OldEncoding();
            Encoding newEncoding = Static.NewEncoding();
            var charWidth = Static.NewFont().GetCharWidth(Static.NewEncoding());

            Dictionary<string, string> NameDic = new Dictionary<string, string>();
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

            string[] DIRS = Directory.GetDirectories(PTPDir, "*", SearchOption.AllDirectories);
            string DIRSlength = DIRS.Length.ToString();
            int temp = DIRSlength.Length;
            int DIRSindex = 0;

            foreach (var ptpDir in DIRS)
            {
                var progressValue = (double)DIRSindex / (double)DIRS.Length * 100;
                progress.Report(progressValue);

                DIRSindex++;
                string DIRSconsole = $"Папка: {DIRSindex.ToString().PadLeft(temp)}\\{DIRSlength}";
                //output(DIRSconsole);

                string importTXT = Path.Combine(NewTextDir, Path.GetFileName(ptpDir) + ".tsv");
                if (!File.Exists(importTXT))
                {
                    importTXT = AuxiliaryLibraries.Tools.IOTools.RelativePath(ptpDir, PTPDir);
                    importTXT = String.Join("-", importTXT.Split('\\'));
                    importTXT = Path.Combine(NewTextDir, importTXT + ".tsv");
                    if (!File.Exists(importTXT))
                        continue;
                }

                var import = File.ReadAllLines(importTXT)
                    .Select(x => x.Split('\t'))
                    .Where(x => x.Length > 5 && !x[5].Equals(""))
                    .GroupBy(x => x[0])
                    .ToDictionary(x => x.Key.ToLower(), x => x.Select(y => new string[] { y[1], y[2], y[5] }).ToArray());

                string[] ptpFiles = Directory.GetFiles(ptpDir, "*.PTP", SearchOption.TopDirectoryOnly);

                for (int k = 0; k < ptpFiles.Length; k++)
                {
                    string FILESconsole = DIRSconsole + $" - Файл: {(k + 1).ToString().PadLeft(3)}\\{ptpFiles.Length.ToString().PadLeft(3)}";
                    //Console.Write(FILESconsole);

                    string current = Path.GetFileName(ptpFiles[k]).ToLower();
                    if (import.ContainsKey(current))
                    {
                        var PTP = new PTP(File.ReadAllBytes(ptpFiles[k]));

                        PTP.ImportNames(NameDic, oldEncoding);

                        if (width == 0)
                            PTP.ImportText(import[current], charWidth);
                        else
                            PTP.ImportText(import[current], charWidth, width);

                        File.WriteAllBytes(ptpFiles[k], PTP.GetData());
                    }
                }
            }

            //Console.WriteLine("\rИмпорт перевода в PTP файлы завершён.");
        }

        public static void CopySourceFiles(string src, string dst, IProgress<double> progress)
        {
            Directory.CreateDirectory(dst);
            Thread.Sleep(1000);
            var totalFiles = Directory.GetFiles(src, "*", SearchOption.AllDirectories).Length;
            int processedFiles = 0;
            CopyTree(src, dst, progress, totalFiles, ref processedFiles);
        }

        private static void CopyTree(string src, string dst, IProgress<double> progress, int totalFiles, ref int processedFiles)
        {
            DirectoryInfo dir = new DirectoryInfo(src);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException("Папки не существует: " + src);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(dst))
            {
                Directory.CreateDirectory(dst);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(dst, file.Name);
                file.CopyTo(temppath, true);
                processedFiles++;
                progress.Report((double)processedFiles / (double)totalFiles * 100);
            }

            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(dst, subdir.Name);
                CopyTree(subdir.FullName, temppath, progress, totalFiles, ref processedFiles);
            }
        }

        public static void PackPTPtoSource(string sourceDir, string ptpDir, string ptpDuplicates, IProgress<double> progress)
        {
            #region Read Duplicate

            Dictionary<string, string> DUPLICATES = new Dictionary<string, string>();
            if (File.Exists(ptpDuplicates))
            {
                string[] dupl = File.ReadAllLines(ptpDuplicates);
                string last = "";

                foreach (var line in dupl)
                {
                    if (line.EndsWith(":"))
                        last = line.Substring(0, line.Length - 1);
                    else if (!line.Equals(""))
                        DUPLICATES.Add(line, last);
                }
            }

            #endregion Read Duplicate

            string[] FILES = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);

            string length = FILES.Length.ToString();
            for (int i = 0; i < FILES.Length; i++)
            {
                var progressValue = (double)i / (double)FILES.Length * 100;
                progress.Report(progressValue);

                Console.Write($"\rFile: {i.ToString().PadLeft(length.Length)}\\{length}");

                var file = GameFormatHelper.OpenFile(Path.GetFileName(FILES[i]), File.ReadAllBytes(FILES[i]));

                if (file != null)
                {
                    var typeFile = file.GetAllObjectFiles(FormatEnum.BMD);
                    foreach (var a in typeFile)
                    {
                        string path = Path.Combine(Path.GetDirectoryName(FILES[i]), Path.GetFileNameWithoutExtension(a.Name.Replace('/', '+')) + ".PTP");
                        string relPath = IOTools.RelativePath(path, sourceDir);
                        path = Path.Combine(ptpDir, relPath);

                        if (File.Exists(path))
                        {
                            PTP PTP;
                            try
                            {
                                PTP = new PTP(File.ReadAllBytes(path));
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Ошибка в открытии PTP файла: {path}", ex);
                            }
                            a.GameData = new BMD(PTP, Static.NewEncoding())
                            {
                                IsLittleEndian = false
                            };
                        }
                        else if (DUPLICATES.ContainsKey(relPath) && File.Exists(Path.Combine(ptpDir, DUPLICATES[relPath])))
                        {
                            PTP PTP = new PTP(File.ReadAllBytes(Path.Combine(ptpDir, DUPLICATES[relPath])));
                            a.GameData = new BMD(PTP, Static.NewEncoding())
                            {
                                IsLittleEndian = false
                            };
                        }
                    }

                    File.WriteAllBytes(FILES[i], file.GameData.GetData());
                }
            }
            Console.WriteLine();
        }
    }
}