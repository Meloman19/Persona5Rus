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

        private static readonly string SourcePath = Path.Combine(BasePath, "Source");
        private static readonly string SourcePTPPath = Path.Combine(SourcePath, "PTP");

        private static readonly string TextPath = Path.Combine(BasePath, "Text");
        private static readonly string DuplicatesFilePath = Path.Combine(TextPath, "PTP_DUPLICATE.txt");
        private static readonly string TextPTPPath = Path.Combine(TextPath, "PTP");

        private static readonly string PTPPath = Path.Combine(BasePath, "PTP");

        private static readonly string OutputPath = Path.Combine(BasePath, "Output");

        private static readonly string TempPTP = Path.Combine(OutputPath, "TEMP_PTP");
        private static readonly string TempSource = Path.Combine(OutputPath, "TEMP_SOURCE");

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
            yield return new TaskProgress()
            {
                Title = "Копируем PTP файлы в выходную папку...",
                Action = progress =>
                {
                    ImportSteps.CopySourceFiles(PTPPath, TempPTP, progress);
                }
            };

            yield return new TaskProgress()
            {
                Title = "Импортируем перевод в PTP файлы...",
                Action = progress =>
                {
                    ImportSteps.ImportTextToPTP(TempPTP, TextPTPPath, progress);
                }
            };

            yield return new TaskProgress()
            {
                Title = "Копируем оригинальные файлы в выходную папку...",
                Action = progress =>
                {
                    ImportSteps.CopySourceFiles(SourcePTPPath, TempSource, progress);
                }
            };

            yield return new TaskProgress()
            {
                Title = "Импортируем PTP файлы в оригинальные файлы...",
                Action = progress =>
                {
                    ImportSteps.PackPTPtoSource(TempSource, TempPTP, DuplicatesFilePath, progress);
                }
            };
        }
    }
}