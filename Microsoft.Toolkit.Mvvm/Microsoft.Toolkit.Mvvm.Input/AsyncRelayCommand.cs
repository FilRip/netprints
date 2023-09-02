using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Microsoft.Toolkit.Mvvm.ComponentModel;

#nullable enable

namespace Microsoft.Toolkit.Mvvm.Input
{
    public sealed class AsyncRelayCommand : ObservableObject, IAsyncRelayCommand, IRelayCommand, ICommand, INotifyPropertyChanged
    {
        internal static readonly PropertyChangedEventArgs CanBeCanceledChangedEventArgs = new(nameof(CanBeCanceled));

        internal static readonly PropertyChangedEventArgs IsCancellationRequestedChangedEventArgs = new(nameof(IsCancellationRequested));

        internal static readonly PropertyChangedEventArgs IsRunningChangedEventArgs = new(nameof(IsRunning));

        private readonly Func<Task>? execute;

        private readonly Func<CancellationToken, Task>? cancelableExecute;

        private readonly Func<bool>? canExecute;

        private CancellationTokenSource? cancellationTokenSource;

        private TaskNotifier? executionTask;

        public Task? ExecutionTask
        {
            get
            {
                return executionTask;
            }
            private set
            {
                if (SetPropertyAndNotifyOnCompletion(ref executionTask, value, delegate
                {
                    OnPropertyChanged(IsRunningChangedEventArgs);
                    OnPropertyChanged(CanBeCanceledChangedEventArgs);
                }, nameof(ExecutionTask)))
                {
                    OnPropertyChanged(IsRunningChangedEventArgs);
                    OnPropertyChanged(CanBeCanceledChangedEventArgs);
                }
            }
        }

        public bool CanBeCanceled
        {
            get
            {
                if (cancelableExecute != null)
                {
                    return IsRunning;
                }
                return false;
            }
        }

        public bool IsCancellationRequested => cancellationTokenSource?.IsCancellationRequested ?? false;

        public bool IsRunning
        {
            get
            {
                Task? task = ExecutionTask;
                if (task == null)
                {
                    return false;
                }
                return !task!.IsCompleted;
            }
        }

        public event EventHandler? CanExecuteChanged;

        public AsyncRelayCommand(Func<Task> execute)
        {
            this.execute = execute;
        }

        public AsyncRelayCommand(Func<CancellationToken, Task> cancelableExecute)
        {
            this.cancelableExecute = cancelableExecute;
        }

        public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public AsyncRelayCommand(Func<CancellationToken, Task> cancelableExecute, Func<bool> canExecute)
        {
            this.cancelableExecute = cancelableExecute;
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
            ExecuteAsync(parameter);
        }

        public Task ExecuteAsync(object? parameter)
        {
            if (CanExecute(parameter))
            {
                if (execute != null)
                {
                    return ExecutionTask = execute!();
                }
                this.cancellationTokenSource?.Cancel();
                CancellationTokenSource cancellationTokenSource = (this.cancellationTokenSource = new CancellationTokenSource());
                OnPropertyChanged(IsCancellationRequestedChangedEventArgs);
                return ExecutionTask = cancelableExecute!(cancellationTokenSource.Token);
            }
            return Task.CompletedTask;
        }

        public void Cancel()
        {
            cancellationTokenSource?.Cancel();
            OnPropertyChanged(IsCancellationRequestedChangedEventArgs);
            OnPropertyChanged(CanBeCanceledChangedEventArgs);
        }
    }
    public sealed class AsyncRelayCommand<T> : ObservableObject, IAsyncRelayCommand<T>, IAsyncRelayCommand, IRelayCommand, ICommand, INotifyPropertyChanged, IRelayCommand<T>
    {
        private readonly Func<T?, Task>? execute;

        private readonly Func<T?, CancellationToken, Task>? cancelableExecute;

        private readonly Predicate<T?>? canExecute;

        private CancellationTokenSource? cancellationTokenSource;

        private TaskNotifier? executionTask;

        public Task? ExecutionTask
        {
            get
            {
                return executionTask;
            }
            private set
            {
                if (SetPropertyAndNotifyOnCompletion(ref executionTask, value, delegate
                {
                    OnPropertyChanged(AsyncRelayCommand.IsRunningChangedEventArgs);
                    OnPropertyChanged(AsyncRelayCommand.CanBeCanceledChangedEventArgs);
                }, nameof(ExecutionTask)))
                {
                    OnPropertyChanged(AsyncRelayCommand.IsRunningChangedEventArgs);
                    OnPropertyChanged(AsyncRelayCommand.CanBeCanceledChangedEventArgs);
                }
            }
        }

        public bool CanBeCanceled
        {
            get
            {
                if (cancelableExecute != null)
                {
                    return IsRunning;
                }
                return false;
            }
        }

        public bool IsCancellationRequested => cancellationTokenSource?.IsCancellationRequested ?? false;

        public bool IsRunning
        {
            get
            {
                Task? task = ExecutionTask;
                if (task == null)
                {
                    return false;
                }
                return !task!.IsCompleted;
            }
        }

        public event EventHandler? CanExecuteChanged;

        public AsyncRelayCommand(Func<T?, Task> execute)
        {
            this.execute = execute;
        }

        public AsyncRelayCommand(Func<T?, CancellationToken, Task> cancelableExecute)
        {
            this.cancelableExecute = cancelableExecute;
        }

        public AsyncRelayCommand(Func<T?, Task> execute, Predicate<T?> canExecute)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public AsyncRelayCommand(Func<T?, CancellationToken, Task> cancelableExecute, Predicate<T?> canExecute)
        {
            this.cancelableExecute = cancelableExecute;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            ExecuteAsync(parameter);
        }

        public void Execute(object? parameter)
        {
            ExecuteAsync((T)parameter);
        }

        public Task ExecuteAsync(T? parameter)
        {
            if (CanExecute(parameter))
            {
                if (execute != null)
                {
                    return ExecutionTask = execute!(parameter);
                }
                this.cancellationTokenSource?.Cancel();
                CancellationTokenSource cancellationTokenSource = (this.cancellationTokenSource = new CancellationTokenSource());
                OnPropertyChanged(AsyncRelayCommand.IsCancellationRequestedChangedEventArgs);
                return ExecutionTask = cancelableExecute!(parameter, cancellationTokenSource.Token);
            }
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(object? parameter)
        {
            return ExecuteAsync((T)parameter);
        }

        public void Cancel()
        {
            cancellationTokenSource?.Cancel();
            OnPropertyChanged(AsyncRelayCommand.IsCancellationRequestedChangedEventArgs);
            OnPropertyChanged(AsyncRelayCommand.CanBeCanceledChangedEventArgs);
        }
    }
}
