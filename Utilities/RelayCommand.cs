using System;
using System.Windows.Input;

namespace AIHomeStudio.Utilities
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _executeWithParam;
        private readonly Action? _executeWithoutParam;
        private readonly Func<object?, bool>? _canExecuteWithParam;
        private readonly Func<bool>? _canExecuteWithoutParam;
        private readonly bool _hasParameter;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _executeWithoutParam = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecuteWithoutParam = canExecute;
            _hasParameter = false;
        }

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _executeWithParam = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecuteWithParam = canExecute;
            _hasParameter = true;
        }

        public bool CanExecute(object? parameter)
        {
            if (_hasParameter)
                return _canExecuteWithParam?.Invoke(parameter) ?? true;
            else
                return _canExecuteWithoutParam?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            if (_hasParameter)
                _executeWithParam(parameter);
            else
                _executeWithoutParam?.Invoke();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
