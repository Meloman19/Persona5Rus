using Persona5Rus.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Persona5Rus.ViewModel
{
    class MainWindowViewModel : BindableBase
    {
        private static readonly string BasePath = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
        private static readonly string SourcePath = "Source";
        private static readonly string TextPath = "Text";
        private static readonly string PTPPath = "PTP";
        private static readonly string OutputPath = "Output";
        private static readonly string DuplicatesPath = "PTP_DUPLICATE.txt";

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

            foreach (var task in Tasks)
            {
                await task.RunAsync();

                if (task.Success == false)
                {
                    break;
                }
            }

            _onWork = false;
        }

        private IEnumerable<TaskProgress> GetTasks()
        {
            var ptpSourcePath = Path.Combine(BasePath, SourcePath, PTPPath);
            var ptpTextPath = Path.Combine(BasePath, TextPath, PTPPath);
            var ptpPath = Path.Combine(BasePath, PTPPath);
            var outputPath = Path.Combine(BasePath, OutputPath);
            var duplPath = Path.Combine(BasePath, TextPath, DuplicatesPath);

            yield return new TaskProgress()
            {
                Title = "Импортируем перевод в PTP файлы...",
                Action = progress =>
                {
                    ImportSteps.ImportTextToPTP(ptpPath, ptpTextPath, progress);
                }
            };

            yield return new TaskProgress()
            {
                Title = "Копируем оригинальные файлы в выходную папку для дальнейшей обработки...",
                Action = progress =>
                {
                    ImportSteps.CopySourceFiles(ptpSourcePath, outputPath, progress);
                }
            };

            yield return new TaskProgress()
            {
                Title = "Импортируем PTP файлы в оригинальные файлы...",
                Action = progress =>
                {
                    ImportSteps.PackPTPtoSource(outputPath, ptpPath, duplPath, progress);
                }
            };
        }
    }
}