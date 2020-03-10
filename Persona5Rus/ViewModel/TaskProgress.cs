﻿using System;
using System.Threading.Tasks;

namespace Persona5Rus.ViewModel
{
    class TaskProgress : BindableBase, IProgress<double>
    {
        private bool? _success;
        private string _title;
        private double _progress;

        public TaskProgress()
        {
        }

        public Action<IProgress<double>> Action { get; set; }

        public bool? Success
        {
            get { return _success; }
            set { SetProperty(ref _success, value); }
        }

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public double Progress
        {
            get { return _progress; }
            set { SetProperty(ref _progress, value); }
        }

        public void Report(double value)
        {
            RunInDispatcher(() => Progress = value);
        }

        public async Task RunAsync()
        {
            try
            {
                await Task.Run(() => Action?.Invoke(this));
                Success = true;
                Progress = 100;
            }
            catch (Exception ex)
            {
                Success = false;
            }
        }
    }
}