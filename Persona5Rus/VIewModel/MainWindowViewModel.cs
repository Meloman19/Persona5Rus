using Persona5Rus.Common;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Persona5Rus.VIewModel
{
    class MainWindowViewModel : BindableBase
    {
        private static readonly string BasePath = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
        private static readonly string SourcePath = "Source";
        private static readonly string TextPath = "Text";
        private static readonly string PTPPath = "PTP";
        private static readonly string OutputPath = "Output";
        private static readonly string DuplicatesPath = "PTP_DUPLICATE.txt";


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

        private async void MakeGood()
        {
            if (OnProcess)
            {
                return;
            }

            OnProcess = true;

            try
            {
                await Task.Run(Rus);
            }
            catch(Exception ex)
            {
                AddText(ex.Message);
                AddText(ex.StackTrace);
            }

            OnProcess = false;
        }

        private void Rus()
        {
            RunInDispatcher(() => OutputText = string.Empty);

            var ptpSourcePath = Path.Combine(BasePath, SourcePath, PTPPath);
            var ptpTextPath = Path.Combine(BasePath, TextPath, PTPPath);
            var ptpPath = Path.Combine(BasePath, PTPPath);
            var outputPath = Path.Combine(BasePath, OutputPath);
            var duplPath = Path.Combine(BasePath, TextPath, DuplicatesPath);

            AddText("Импортируем перевод в PTP файлы...");

            try
            {
                TextHelper.ImportTextToPTP(ptpPath, ptpTextPath);
            }
            catch(Exception ex)
            {
                AddText(ex.Message);
                return;
            }

            AddText("Копируем оригинальные файлы в выходную папку для дальнейшей обработки...");

            try
            {
                Directory.Delete(outputPath, true);
                Thread.Sleep(1000);
                CopyTree(ptpSourcePath, outputPath);
            }
            catch(Exception ex)
            {
                AddText(ex.Message);
                return;
            }

            AddText("Импортируем PTP файлы в оригинальные файлы");

            try
            {
                TextHelper.PackPTPtoSource(outputPath, ptpPath, duplPath);
            }
            catch(Exception ex)
            {
                AddText(ex.Message);
                return;
            }

            AddText("Готово!");
        }

        private void AddText(string text)
        {
            RunInDispatcher(() => OutputText += text + '\n');
        }

        private void CopyTree(string src, string dst)
        {
            DirectoryInfo dir = new DirectoryInfo(src);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException("Папки не существует: "+ src);
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
            }

            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(dst, subdir.Name);
                CopyTree(subdir.FullName, temppath);
            }
        }
    }
}