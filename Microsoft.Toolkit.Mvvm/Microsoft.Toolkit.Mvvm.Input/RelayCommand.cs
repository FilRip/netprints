using System;
using System.Runtime.CompilerServices;
using System.Windows.Input;

#nullable enable

namespace Microsoft.Toolkit.Mvvm.Input
{
    public sealed class RelayCommand : IRelayCommand, ICommand
    {
        private readonly Action execute;

        private readonly Func<bool>? canExecute;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action execute)
        {
            this.execute = execute;
        }

        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public void NotifyCanExecuteChanged()
        {
            this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanExecute(object? parameter)
        {
            return canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                execute();
            }
        }
    }
    public sealed class RelayCommand<T> : IRelayCommand<T>, IRelayCommand, ICommand
    {
        private readonly Action<T?> execute;

        private readonly Predicate<T?>? canExecute;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action<T?> execute)
        {
            this.execute = execute;
        }

        public RelayCommand(Action<T?> execute, Predicate<T?> canExecute)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public void NotifyCanExecuteChanged()
        {
            this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanExecute(T? parameter)
        {
            return canExecute?.Invoke(parameter) ?? true;
        }

        public bool CanExecute(object? parameter)
        {
            if (default(T) != null && parameter == null)
            {
                return false;
            }
            return CanExecute((T)parameter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(T? parameter)
        {
            if (CanExecute(parameter))
            {
                execute(parameter);
            }
        }

        public void Execute(object? parameter)
        {
            Execute((T)parameter);
        }
    }
}
