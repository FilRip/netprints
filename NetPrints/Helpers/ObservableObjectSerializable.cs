using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Toolkit.Mvvm.ComponentModel
{
    [DataContract()]
    public abstract class ObservableObjectSerializable : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private interface ITaskNotifier<TTask> where TTask : Task
        {
            TTask? Task { get; set; }
        }

        protected sealed class TaskNotifier : ITaskNotifier<Task>
        {
            private Task? task;

            Task? ITaskNotifier<Task>.Task
            {
                get
                {
                    return task;
                }
                set
                {
                    task = value;
                }
            }

            internal TaskNotifier()
            {
            }

            public static implicit operator Task?(TaskNotifier? notifier)
            {
                return notifier?.task;
            }
        }

        protected sealed class TaskNotifier<T> : ITaskNotifier<Task<T>>
        {
            private Task<T>? task;

            Task<T>? ITaskNotifier<Task<T>>.Task
            {
                get
                {
                    return task;
                }
                set
                {
                    task = value;
                }
            }

            internal TaskNotifier()
            {
            }

            public static implicit operator Task<T>?(TaskNotifier<T>? notifier)
            {
                return notifier?.task;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public event PropertyChangingEventHandler? PropertyChanging;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected virtual void OnPropertyChanging(PropertyChangingEventArgs e)
        {
            PropertyChanging?.Invoke(this, e);
        }

        protected void OnPropertyChanged([CallerMemberName()] string? propertyName = null)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected void OnPropertyChanging([CallerMemberName()] string? propertyName = null)
        {
            OnPropertyChanging(new PropertyChangingEventArgs(propertyName));
        }

        protected bool SetProperty<T>([NotNullIfNotNull("newValue")] ref T field, T newValue, [CallerMemberName()] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return false;
            }
            OnPropertyChanging(propertyName);
            field = newValue;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected bool SetProperty<T>([NotNullIfNotNull("newValue")] ref T field, T newValue, IEqualityComparer<T> comparer, [CallerMemberName()] string? propertyName = null)
        {
            if (comparer.Equals(field, newValue))
            {
                return false;
            }
            OnPropertyChanging(propertyName);
            field = newValue;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected bool SetProperty<T>(T oldValue, T newValue, Action<T> callback, [CallerMemberName()] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
            {
                return false;
            }
            OnPropertyChanging(propertyName);
            callback(newValue);
            OnPropertyChanged(propertyName);
            return true;
        }

        protected bool SetProperty<T>(T oldValue, T newValue, IEqualityComparer<T> comparer, Action<T> callback, [CallerMemberName()] string? propertyName = null)
        {
            if (comparer.Equals(oldValue, newValue))
            {
                return false;
            }
            OnPropertyChanging(propertyName);
            callback(newValue);
            OnPropertyChanged(propertyName);
            return true;
        }

        protected bool SetProperty<TModel, T>(T oldValue, T newValue, TModel model, Action<TModel, T> callback, [CallerMemberName()] string? propertyName = null) where TModel : class
        {
            if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
            {
                return false;
            }
            OnPropertyChanging(propertyName);
            callback(model, newValue);
            OnPropertyChanged(propertyName);
            return true;
        }

        protected bool SetProperty<TModel, T>(T oldValue, T newValue, IEqualityComparer<T> comparer, TModel model, Action<TModel, T> callback, [CallerMemberName()] string? propertyName = null) where TModel : class
        {
            if (comparer.Equals(oldValue, newValue))
            {
                return false;
            }
            OnPropertyChanging(propertyName);
            callback(model, newValue);
            OnPropertyChanged(propertyName);
            return true;
        }

        protected bool SetPropertyAndNotifyOnCompletion([NotNull] ref TaskNotifier? taskNotifier, Task? newValue, [CallerMemberName()] string? propertyName = null)
        {
#pragma warning disable CS8620, CS8631 // Impossible d'utiliser le type en tant que paramètre de type dans le type ou la méthode générique. La nullabilité de l'argument de type ne correspond pas au type de contrainte.
            return SetPropertyAndNotifyOnCompletion(taskNotifier ??= new TaskNotifier(), newValue, delegate
#pragma warning restore CS8620, CS8631 // Impossible d'utiliser le type en tant que paramètre de type dans le type ou la méthode générique. La nullabilité de l'argument de type ne correspond pas au type de contrainte.
            {
            }, propertyName);
        }

        protected bool SetPropertyAndNotifyOnCompletion([NotNull] ref TaskNotifier? taskNotifier, Task? newValue, Action<Task?> callback, [CallerMemberName()] string? propertyName = null)
        {
#pragma warning disable CS8620, CS8631 // Impossible d'utiliser le type en tant que paramètre de type dans le type ou la méthode générique. La nullabilité de l'argument de type ne correspond pas au type de contrainte.
            return SetPropertyAndNotifyOnCompletion(taskNotifier ??= new TaskNotifier(), newValue, callback, propertyName);
#pragma warning restore CS8620, CS8631 // Impossible d'utiliser le type en tant que paramètre de type dans le type ou la méthode générique. La nullabilité de l'argument de type ne correspond pas au type de contrainte.
        }

        protected bool SetPropertyAndNotifyOnCompletion<T>([NotNull] ref TaskNotifier<T>? taskNotifier, Task<T>? newValue, [CallerMemberName()] string? propertyName = null)
        {
#pragma warning disable CS8620, CS8631 // Impossible d'utiliser le type en tant que paramètre de type dans le type ou la méthode générique. La nullabilité de l'argument de type ne correspond pas au type de contrainte.
            return SetPropertyAndNotifyOnCompletion(taskNotifier ??= new TaskNotifier<T>(), newValue, delegate
#pragma warning restore CS8620, CS8631 // Impossible d'utiliser le type en tant que paramètre de type dans le type ou la méthode générique. La nullabilité de l'argument de type ne correspond pas au type de contrainte.
            {
            }, propertyName);
        }

        protected bool SetPropertyAndNotifyOnCompletion<T>([NotNull] ref TaskNotifier<T>? taskNotifier, Task<T>? newValue, Action<Task<T>?> callback, [CallerMemberName()] string? propertyName = null)
        {
#pragma warning disable CS8620, CS8631 // Impossible d'utiliser le type en tant que paramètre de type dans le type ou la méthode générique. La nullabilité de l'argument de type ne correspond pas au type de contrainte.
            return SetPropertyAndNotifyOnCompletion(taskNotifier ??= new TaskNotifier<T>(), newValue, callback, propertyName);
#pragma warning restore CS8620, CS8631 // Impossible d'utiliser le type en tant que paramètre de type dans le type ou la méthode générique. La nullabilité de l'argument de type ne correspond pas au type de contrainte.
        }

        private bool SetPropertyAndNotifyOnCompletion<TTask>(ITaskNotifier<TTask> taskNotifier, TTask newValue, Action<TTask?> callback, [CallerMemberName()] string? propertyName = null) where TTask : Task
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            TTask newValue2 = newValue;
            ITaskNotifier<TTask> taskNotifier2 = taskNotifier;
            string propertyName2 = propertyName;
            Action<TTask?> callback2 = callback;
            if (taskNotifier2.Task == newValue2)
            {
                return false;
            }
            bool num = newValue2?.IsCompleted ?? true;
            OnPropertyChanging(propertyName2);
            taskNotifier2.Task = newValue2;
            OnPropertyChanged(propertyName2);
            if (num)
            {
                callback2(newValue2);
                return true;
            }
            MonitorTask();
            return true;
            async void MonitorTask()
            {
                try
                {
                    if (newValue2 != null)
                        await newValue2;
                }
                catch
                {
                }
                if (taskNotifier2.Task == newValue2)
                {
                    OnPropertyChanged(propertyName2);
                }
                callback2(newValue2);
            }
        }
    }
}
