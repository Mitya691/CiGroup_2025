using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopClient.Helpers
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Input;

    public sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;
        private bool _isRunning;

        public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool IsRunning => _isRunning;

        public bool CanExecute(object parameter) =>
            !_isRunning && (_canExecute?.Invoke() ?? true);

        public event EventHandler CanExecuteChanged;

        public async void Execute(object parameter) => await ExecuteAsync();

        public async Task ExecuteAsync()
        {
            if (!CanExecute(null)) return;

            _isRunning = true;
            RaiseCanExecuteChanged();
            try
            {
                await _execute().ConfigureAwait(true); // UI-поток нужен по завершении
            }
            finally
            {
                _isRunning = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

}
