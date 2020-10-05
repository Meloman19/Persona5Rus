﻿using Persona5Rus.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Persona5Rus.ViewModel
{
    internal sealed class CreationViewModel : BindableBase
    {
        private static readonly string SourcePath = Path.Combine(Global.BasePath, "Source");
        private static readonly string SourceBMDPath = Path.Combine(SourcePath, "PTP");
        private static readonly string SourceUSMPath = Path.Combine(SourcePath, "USM");
        private static readonly string SourceTablePath = Path.Combine(SourcePath, "TABLE");
        private static readonly string SourceOtherPath = Path.Combine(SourcePath, "OTHER");
        private static readonly string SourceEBOOTPath = Path.Combine(SourcePath, "EBOOT.BIN");

        private static readonly string TextPath = Path.Combine(Global.BasePath, "Text");
        private static readonly string DuplicatesFilePath = Path.Combine(TextPath, "PTP_DUPLICATE.txt");
        private static readonly string TextMovieFilePath = Path.Combine(TextPath, "subtitles.tsv");
        private static readonly string TextPTPPath = Path.Combine(TextPath, "PTP");
        private static readonly string TextTablePath = Path.Combine(TextPath, "TABLE");

        private static readonly string TempPath = Path.Combine(Global.BasePath, "Temp");
        private static readonly string TempSource = Path.Combine(TempPath, "TEMP_SOURCE");
        private static readonly string TempUSM = Path.Combine(TempPath, "TEMP_USM");
        private static readonly string TempEBOOT = Path.Combine(TempPath, "EBOOT.BIN");

        private static readonly string CPKTool = Path.Combine(Global.BasePath, "Tools", "cpk", "cpkmakec.exe");

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
                    if (Directory.Exists(Global.OutputFolderPath))
                    {
                        Directory.Delete(Global.OutputFolderPath, true);
                    }

                    if (Directory.Exists(TempPath))
                    {
                        Directory.Delete(TempPath, true);
                    }

                    Thread.Sleep(1000);

                    progress.Report(1);
                }
            };

            yield return new TaskProgress()
            {
                Title = "Копируем оригинальные файлы во временную папку...",
                Action = progress =>
                {
                    ImportSteps.CopySourceFiles(TempSource, progress, SourceBMDPath, SourceTablePath, SourceOtherPath);
                    File.Copy(SourceEBOOTPath, TempEBOOT, true);
                }
            };

            if (!settings.DevSkipTextImport)
            {
                yield return new TaskProgress()
                {
                    Title = "Импортируем перевод в оригинальные файлы...",
                    Action = progress =>
                    {
                        TextImporter textImporter = new TextImporter(TextPTPPath, DuplicatesFilePath);
                        EbootImporter ebootImporter = new EbootImporter(TextPTPPath);

                        var sourceDir = TempSource;

                        var files = Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories).ToArray();
                        var ind = 0;

                        foreach (var file in files)
                        {
                            var progressValue = (double)ind++ / (double)files.Length * 100;
                            progress.Report(progressValue);

                            textImporter.Import(file, sourceDir);
                        }

                        ebootImporter.Import(TempEBOOT);
                    }
                };
            }

            if (!settings.DevSkipTableImport)
            {
                yield return new TaskProgress()
                {
                    Title = "Импортируем перевод (таблицы) в оригинальные файлы...",
                    Action = progress =>
                    {
                        ImportSteps_Tables.PackTBLtoSource(TempSource, TextTablePath, progress);
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
                        UsmImporter usmImporter = new UsmImporter(TempUSM, TextMovieFilePath);

                        var files = Directory.EnumerateFiles(SourceUSMPath, "*", SearchOption.AllDirectories).ToArray();
                        var ind = 0;

                        var usmOutput = Path.Combine(TempSource, "ps3", "movie");

                        foreach (var file in files)
                        {
                            var progressValue = (double)ind++ / (double)files.Length * 100;
                            progress.Report(progressValue);

                            usmImporter.Import(file, usmOutput);
                        }
                    }
                };
            }

            yield return new TaskProgress()
            {
                Title = "Собираем все файлы вместе...",
                Action = progress =>
                {
                    Directory.CreateDirectory(Global.OutputFolderPath);
                    File.Copy(TempEBOOT, Path.Combine(Global.OutputFolderPath, "EBOOT.BIN"), true);

                    if (settings.CreateModCPK)
                    {
                        var tempComplete = Path.Combine(TempPath, "TEMP_COMBINE");
                        ImportSteps.MoveFiles(tempComplete, progress, Path.Combine(TempSource, "data"), Path.Combine(TempSource, "ps3"));
                        var mod = Path.Combine(Global.OutputFolderPath, "mod.cpk");
                        ImportSteps.MakeCPK(CPKTool, tempComplete, mod);
                    }
                    else
                    {
                        var completePath = Path.Combine(Global.OutputFolderPath, "mod");
                        ImportSteps.MoveFiles(completePath, progress, Path.Combine(TempSource, "data"), Path.Combine(TempSource, "ps3"));
                    }

                    if (Directory.Exists(TempPath))
                    {
                        Directory.Delete(TempPath, true);
                    }
                }
            };
        }
    }
}