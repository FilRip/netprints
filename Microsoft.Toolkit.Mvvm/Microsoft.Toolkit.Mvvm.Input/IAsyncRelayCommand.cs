using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

#nullable enable

namespace Microsoft.Toolkit.Mvvm.Input
{
    public interface IAsyncRelayCommand : IRelayCommand, ICommand, INotifyPropertyChanged
    {
        Task? ExecutionTask { get; }

        bool CanBeCanceled { get; }

        bool IsCancellationRequested { get; }

        bool IsRunning { get; }

        Task ExecuteAsync(object? parameter);

        void Cancel();
    }
    public interface IAsyncRelayCommand<in T> : IAsyncRelayCommand, IRelayCommand, ICommand, INotifyPropertyChanged, IRelayCommand<T>
    {
        Task ExecuteAsync(T? parameter);
    }
}
