﻿using AuxiliaryLibraries.Tools;
using Persona5Rus.Common;
using PersonaEditorLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Persona5Rus.ViewModel
{
    internal sealed class CreationViewModel : BindableBase
    {
        private static readonly string SourceEBOOTPath = Path.Combine(Global.DataDirectory, "EBOOT.BIN");
        private static readonly string SourceFontPath = Path.Combine(Global.DataDirectory, "font0.fnt");
        private static readonly string SourceWorkFilesPath = Path.Combine(Global.DataDirectory, "work_files.txt");
        private static readonly string SourceWorkFilesPath_BMD = Path.Combine(Global.DataDirectory, "BMD.txt");
        private static readonly string SourceWorkFilesPath_TABLE = Path.Combine(Global.DataDirectory, "TABLE.txt");
        private static readonly string SourceWorkFilesPath_TEXTURE = Path.Combine(Global.DataDirectory, "TEX.txt");
        private static readonly string SourceWorkFilesPath_USM = Path.Combine(Global.DataDirectory, "USM.txt");

        private static readonly string BMDTextPath = Path.Combine(Global.DataDirectory, "BMD");
        private static readonly string BMDDuplicatesPath = Path.Combine(Global.DataDirectory, "PTP_DUPLICATE.txt");
        private static readonly string TableTextPath = Path.Combine(Global.DataDirectory, "TABLE");
        private static readonly string MovieSubtitlesFilePath = Path.Combine(Global.DataDirectory, "subtitles.tsv");

        private static readonly string TexturePath = Path.Combine(Global.DataDirectory, "TEX");

        private static readonly string TempMod = Path.Combine(Global.TempDirectory, "mod");
        private static readonly string TempSource = Path.Combine(Global.TempDirectory, "TEMP_SOURCE");
        private static readonly string TempUSM = Path.Combine(Global.TempDirectory, "TEMP_USM");
        private static readonly string TempEBOOT = Path.Combine(Global.TempDirectory, "EBOOT.BIN");

        private static readonly string CPKTool = Path.Combine(Global.ApplicationDirectory, "Tools", "cpk", "cpkmakec.exe");

        private bool _onProcess;

        private int _totalTasks;
        private int _currentTaskInd;

        private TaskProgress _currentTask;

        public bool OnProcess
        {
            get { return _onProcess; }
            set { SetProperty(ref _onProcess, value); }
        }

        public int TotalTasks
        {
            get => _totalTasks;
            set => SetProperty(ref _totalTasks, value);
        }

        public int CurrentTaskInd
        {
            get => _currentTaskInd;
            set => SetProperty(ref _currentTaskInd, value);
        }

        public TaskProgress CurrentTask
        {
            get => _currentTask;
            set => SetProperty(ref _currentTask, value);
        }

        public async void MakeGood(Settings settings)
        {
            if (OnProcess)
            {
                return;
            }

            OnProcess = true;

            var currentSettings = settings.Copy();
            var tasks = GetTasks(currentSettings).ToArray();

            TotalTasks = tasks.Length;
            CurrentTaskInd = 0;

            foreach (var task in tasks)
            {
                CurrentTaskInd++;
                CurrentTask = task;

                await task.RunAsync();

                if (task.Success == false)
                {
                    var error = new ErrorWindow();
                    error.ErrorText = task.Error;
                    error.ShowDialog();
                    break;
                }
            }

            OnProcess = false;
        }

        private IEnumerable<TaskProgress> GetTasks(Settings settings)
        {
            yield return new TaskProgress()
            {
                Title = "Предварительная подготовка...",
                Action = progress =>
                {
                    if (Directory.Exists(Global.OutputDirectory))
                    {
                        Directory.Delete(Global.OutputDirectory, true);
                    }

                    if (Directory.Exists(Global.TempDirectory))
                    {
                        Directory.Delete(Global.TempDirectory, true);
                    }

                    Thread.Sleep(1000);

                    progress.Report(1);
                }
            };

            if (!settings.DevSkipTextImport && !settings.DevSkipTextureImport)
            {
                yield return new TaskProgress()
                {
                    Title = "Импортируем перевод...",
                    Action = progress =>
                    {
                        TextImporter textImporter = new TextImporter(BMDTextPath, BMDDuplicatesPath);
                        TableImporter tableImporter = new TableImporter(TableTextPath);
                        EbootImporter ebootImporter = new EbootImporter(BMDTextPath);
                        TextureImporter textureImporter = new TextureImporter(TexturePath);

                        var sourceFiles = GetSourceFiles_TextTextures(settings);

                        var completed = 0;

                        Action<int> action = i =>
                        {
                            var filePath = sourceFiles[i].Item1;
                            var fileGD = GameFormatHelper.OpenFile(Path.GetFileName(filePath), File.ReadAllBytes(filePath));

                            var newPath = Path.Combine(TempMod, sourceFiles[i].Item2);
                            bool updated = false;

                            if (!settings.DevSkipTextImport)
                            {
                                updated |= textImporter.Import(fileGD, sourceFiles[i].Item2);
                                updated |= tableImporter.Import(fileGD, sourceFiles[i].Item2);
                            }

                            if (!settings.DevSkipTextureImport)
                            {
                                updated |= textureImporter.Import(fileGD, sourceFiles[i].Item2);
                            }

                            if (updated)
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                                File.WriteAllBytes(newPath, fileGD.GameData.GetData());
                            }

                            Interlocked.Increment(ref completed);

                            var progressValue = (double)completed / sourceFiles.Length * 100;
                            progress.Report(progressValue);
                        };

                        Parallel.For(0, sourceFiles.Length, action);

                        var fontNewPath = Path.Combine(TempMod, "font", "font0.fnt");
                        Directory.CreateDirectory(Path.GetDirectoryName(fontNewPath));
                        File.Copy(SourceFontPath, fontNewPath, true);

                        if (!settings.DevSkipTextImport)
                        {
                            File.Copy(SourceEBOOTPath, TempEBOOT, true);
                            ebootImporter.Import(TempEBOOT);
                        }
                    }
                };
            }

            if (!settings.DevSkipMovieImport)
            {
                yield return new TaskProgress()
                {
                    Title = "Импорт субтитров в видео...",
                    Action = progress =>
                    {
                        UsmImporter usmImporter = new UsmImporter(TempUSM, MovieSubtitlesFilePath);

                        var sourceFiles = GetSourceFiles_Movie(settings);

                        for (int i = 0; i < sourceFiles.Length; i++)
                        {
                            var progressValue = (double)i / sourceFiles.Length * 100;
                            progress.Report(progressValue);

                            var outputPath = Path.Combine(TempMod, sourceFiles[i].Item2);

                            usmImporter.Import(sourceFiles[i].Item1, outputPath);
                        }
                    }
                };
            }

            yield return new TaskProgress()
            {
                Title = "Собираем все файлы вместе...",
                Action = progress =>
                {
                    Directory.CreateDirectory(Global.OutputDirectory);
                    if (File.Exists(TempEBOOT))
                    {
                        File.Copy(TempEBOOT, Path.Combine(Global.OutputDirectory, "EBOOT.BIN"), true);
                    }

                    if (Directory.Exists(TempMod))
                    {
                        if (settings.CreateModCPK)
                        {
                            var mod = Path.Combine(Global.OutputDirectory, "mod.cpk");
                            ImportSteps.MakeCPK(CPKTool, TempMod, mod);
                        }
                        else
                        {
                            var completePath = Path.Combine(Global.OutputDirectory, "mod");
                            Directory.Move(TempMod, completePath);
                        }
                    }

                    if (Directory.Exists(Global.TempDirectory))
                    {
                        Directory.Delete(Global.TempDirectory, true);
                    }
                }
            };
        }

        private static (string, string)[] GetSourceFiles_TextTextures(Settings settings)
        {
            var workFiles = new HashSet<string>();

            if (!settings.DevSkipTextImport)
            {
                workFiles.UnionWith(File.ReadAllLines(SourceWorkFilesPath_BMD));
                workFiles.UnionWith(File.ReadAllLines(SourceWorkFilesPath_TABLE));
            }

            if (!settings.DevSkipTextureImport)
            {
                workFiles.UnionWith(File.ReadAllLines(SourceWorkFilesPath_TEXTURE));
            }

            return GetSourceFiles(settings, workFiles);
        }

        private static (string, string)[] GetSourceFiles_Movie(Settings settings)
        {
            var workFiles = File.ReadAllLines(SourceWorkFilesPath_USM).ToHashSet();

            return GetSourceFiles(settings, workFiles);
        }

        private static (string, string)[] GetSourceFiles(Settings settings, HashSet<string> workFiles)
        {
            var fi = new HashSet<string>();
            List<(string, string)> sourceFiles = new List<(string, string)>();

            var cpkDir = settings.PsCPKPath;
            foreach (var file in Directory.EnumerateFiles(cpkDir, "*", SearchOption.AllDirectories))
            {
                var relPath = IOTools.RelativePath(file, cpkDir);

                if (workFiles.Contains(relPath))
                {
                    fi.Add(relPath);
                    sourceFiles.Add(new ValueTuple<string, string>(file, relPath));
                }
            }

            cpkDir = settings.DataCPKPath;
            foreach (var file in Directory.EnumerateFiles(cpkDir, "*", SearchOption.AllDirectories))
            {
                var relPath = IOTools.RelativePath(file, cpkDir);

                if (workFiles.Contains(relPath) && !fi.Contains(relPath))
                {
                    sourceFiles.Add(new ValueTuple<string, string>(file, relPath));
                }
            }

            return sourceFiles.ToArray();
        }
    }
}