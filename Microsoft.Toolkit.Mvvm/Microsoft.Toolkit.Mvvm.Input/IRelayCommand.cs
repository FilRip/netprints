using System.Windows.Input;

#nullable enable

namespace Microsoft.Toolkit.Mvvm.Input
{
    public interface IRelayCommand : ICommand
    {
        void NotifyCanExecuteChanged();
    }
    public interface IRelayCommand<in T> : IRelayCommand, ICommand
    {
        bool CanExecute(T? parameter);

        void Execute(T? parameter);
    }
}
