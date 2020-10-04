using Persona5Rus.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Persona5Rus.ViewModel
{
    class MainWindowViewModel : BindableBase
    {
        private static readonly string BasePath = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);

        private static readonly string SourcePath = Path.Combine(BasePath, "Source");
        private static readonly string SourceBMDPath = Path.Combine(SourcePath, "PTP");
        private static readonly string SourceUSMPath = Path.Combine(SourcePath, "USM");
        private static readonly string SourceTablePath = Path.Combine(SourcePath, "TABLE");
        private static readonly string SourceOtherPath = Path.Combine(SourcePath, "OTHER");
        private static readonly string SourceEBOOTPath = Path.Combine(SourcePath, "EBOOT.BIN");

        private static readonly string TextPath = Path.Combine(BasePath, "Text");
        private static readonly string DuplicatesFilePath = Path.Combine(TextPath, "PTP_DUPLICATE.txt");
        private static readonly string TextMovieFilePath = Path.Combine(TextPath, "subtitles.tsv");
        private static readonly string TextPTPPath = Path.Combine(TextPath, "PTP");
        private static readonly string TextTablePath = Path.Combine(TextPath, "TABLE");

        private static readonly string OutputPath = Path.Combine(BasePath, "Output");

        private static readonly string TempPath = Path.Combine(BasePath, "Temp");
        private static readonly string TempSource = Path.Combine(TempPath, "TEMP_SOURCE");
        private static readonly string TempUSM = Path.Combine(TempPath, "TEMP_USM");
        private static readonly string TempComplete = Path.Combine(TempPath, "TEMP_COMBINE");
        private static readonly string TempEBOOT = Path.Combine(TempPath, "EBOOT.BIN");

        private static readonly string CPKTool = Path.Combine(BasePath, "Tools", "cpkmakec.exe");
        private static readonly string USMEncoderTool = Path.Combine(BasePath, "Tools", "medianoche.exe");

        private bool _onWork;
        private bool _onProcess;
        private string _outputText;

        public MainWindowViewModel()
        {
            MakeGoodCommand = new RelayCommand(MakeGood);
        }

        public ICommand MakeGoodCommand { get; }

        public string OutputText
        {
            get { return _outputText; }
            set { SetProperty(ref _outputText, value); }
        }

        public bool OnProcess
        {
            get { return _onProcess; }
            set { SetProperty(ref _onProcess, value); }
        }

        public ObservableCollection<TaskProgress> Tasks { get; } = new ObservableCollection<TaskProgress>();

        private async void MakeGood()
        {
            if (_onWork)
            {
                return;
            }

            if (OnProcess)
            {
                OnProcess = false;
                return;
            }

            Tasks.Clear();
            OnProcess = true;
            _onWork = true;

            foreach (var task in GetTasks())
            {
                Tasks.Add(task);
            }

            if (Directory.Exists(OutputPath))
            {
                Directory.Delete(OutputPath, true);
            }

            if (Directory.Exists(TempPath))
            {
                Directory.Delete(TempPath, true);
            }

            await Task.Delay(1000);

            foreach (var task in Tasks)
            {
                await task.RunAsync();

                if (task.Success == false)
                {
                    break;
                }
            }

            if (Directory.Exists(TempPath))
            {
                Directory.Delete(TempPath, true);
            }

            _onWork = false;
        }

        private IEnumerable<TaskProgress> GetTasks()
        {
            yield return new TaskProgress()
            {
                Title = "Копируем оригинальные файлы во временную папку...",
                Action = progress =>
                {
                    ImportSteps.CopySourceFiles(TempSource, progress, SourceBMDPath, SourceTablePath, SourceOtherPath);
                    File.Copy(SourceEBOOTPath, TempEBOOT, true);
                }
            };

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

            yield return new TaskProgress()
            {
                Title = "Импортируем перевод (таблицы) в оригинальные файлы...",
                Action = progress =>
                {
                    ImportSteps_Tables.PackTBLtoSource(TempSource, TextTablePath, progress);
                }
            };

            yield return new TaskProgress()
            {
                Title = "Импорт субтитров в видео...",
                Action = progress =>
                {
                    UsmImporter usmImporter = new UsmImporter(TempUSM, USMEncoderTool, TextMovieFilePath);

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

            yield return new TaskProgress()
            {
                Title = "Собираем все файлы вместе...",
                Action = progress =>
                {
                    ImportSteps.MoveFiles(TempComplete, progress, Path.Combine(TempSource, "data"), Path.Combine(TempSource, "ps3"));
                    var mod = Path.Combine(OutputPath, "mod.cpk");
                    ImportSteps.MakeCPK(CPKTool, TempComplete, mod);

                    File.Copy(TempEBOOT, Path.Combine(OutputPath, "EBOOT.BIN"), true);
                }
            };
        }
    }
}