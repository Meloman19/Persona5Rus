using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Persona5Rus.Common
{
    static class ImportSteps
    {
        public static void CopySourceFiles(string src, string dst, IProgress<double> progress)
        {
            Directory.CreateDirectory(dst);
            Thread.Sleep(1000);
            var totalFiles = Directory.GetFiles(src, "*", SearchOption.AllDirectories).Length;
            int processedFiles = 0;
            CopyTree(src, dst, progress, totalFiles, ref processedFiles);
        }

        public static void CopySourceFiles(string dst, IProgress<double> progress, params string[] src)
        {
            Directory.CreateDirectory(dst);
            Thread.Sleep(1000);

            int totalFiles = 0;
            foreach (var sr in src)
            {
                totalFiles += Directory.GetFiles(sr, "*", SearchOption.AllDirectories).Length;
            }

            int processedFiles = 0;
            foreach (var sr in src)
            {
                CopyTree(sr, dst, progress, totalFiles, ref processedFiles);
            }
        }

        public static void MoveFiles(string dst, IProgress<double> progress, params string[] src)
        {
            Directory.CreateDirectory(dst);
            Thread.Sleep(1000);

            int totalFiles = 0;
            foreach (var sr in src)
            {
                totalFiles += Directory.GetFiles(sr, "*", SearchOption.AllDirectories).Length;
            }

            int processedFiles = 0;
            foreach (var sr in src)
            {
                MoveTree(sr, dst, progress, totalFiles, ref processedFiles);
            }
        }

        private static void MoveTree(string src, string dst, IProgress<double> progress, int totalFiles, ref int processedFiles)
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

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(dst, file.Name);
                if (!File.Exists(temppath))
                {
                    file.MoveTo(temppath);
                }
                processedFiles++;
                progress.Report((double)processedFiles / (double)totalFiles * 100);
            }

            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(dst, subdir.Name);
                MoveTree(subdir.FullName, temppath, progress, totalFiles, ref processedFiles);
            }
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

        public static void MakeCPK(string cpkTool, string src, string dst)
        {
            var process = new Process();
            process.StartInfo.FileName = cpkTool;
            process.StartInfo.Arguments = $"\"{src}\" \"{dst}\" -code=UTF-8 -mode=FILENAME";
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(cpkTool);
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.Start();
            process.WaitForExit();
        }
    }
}