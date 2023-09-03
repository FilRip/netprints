using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;

#nullable enable

namespace Microsoft.Toolkit.Mvvm.ComponentModel
{
    [DataContract()]
    public abstract class ObservableRecipientSerializable : ObservableObjectSerializable
    {
        private bool isActive;

        protected IMessenger Messenger { get; }

        public bool IsActive
        {
            get
            {
                return isActive;
            }
            set
            {
                if (SetProperty(ref isActive, value, broadcast: true, nameof(IsActive)))
                {
                    if (value)
                    {
                        OnActivated();
                    }
                    else
                    {
                        OnDeactivated();
                    }
                }
            }
        }

        protected ObservableRecipientSerializable()
            : this(WeakReferenceMessenger.Default)
        {
        }

        protected ObservableRecipientSerializable(IMessenger messenger)
        {
            Messenger = messenger;
        }

        protected virtual void OnActivated()
        {
            Messenger.RegisterAll(this);
        }

        protected virtual void OnDeactivated()
        {
            Messenger.UnregisterAll(this);
        }

        protected virtual void Broadcast<T>(T oldValue, T newValue, string? propertyName)
        {
            PropertyChangedMessage<T> message = new(this, propertyName, oldValue, newValue);
            Messenger.Send(message);
        }

        protected bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, bool broadcast, [CallerMemberName()] string? propertyName = null)
        {
            T oldValue = field;
            bool num = SetProperty(ref field, newValue, propertyName);
            if (num && broadcast)
            {
                Broadcast(oldValue, newValue, propertyName);
            }
            return num;
        }

        protected bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, IEqualityComparer<T> comparer, bool broadcast, [CallerMemberName()] string? propertyName = null)
        {
            T oldValue = field;
            bool num = SetProperty(ref field, newValue, comparer, propertyName);
            if (num && broadcast)
            {
                Broadcast(oldValue, newValue, propertyName);
            }
            return num;
        }

        protected bool SetProperty<T>(T oldValue, T newValue, Action<T> callback, bool broadcast, [CallerMemberName()] string? propertyName = null)
        {
            bool num = SetProperty(oldValue, newValue, callback, propertyName);
            if (num && broadcast)
            {
                Broadcast(oldValue, newValue, propertyName);
            }
            return num;
        }

        protected bool SetProperty<T>(T oldValue, T newValue, IEqualityComparer<T> comparer, Action<T> callback, bool broadcast, [CallerMemberName()] string? propertyName = null)
        {
            bool num = SetProperty(oldValue, newValue, comparer, callback, propertyName);
            if (num && broadcast)
            {
                Broadcast(oldValue, newValue, propertyName);
            }
            return num;
        }

        protected bool SetProperty<TModel, T>(T oldValue, T newValue, TModel model, Action<TModel, T> callback, bool broadcast, [CallerMemberName()] string? propertyName = null) where TModel : class
        {
            bool num = SetProperty(oldValue, newValue, model, callback, propertyName);
            if (num && broadcast)
            {
                Broadcast(oldValue, newValue, propertyName);
            }
            return num;
        }

        protected bool SetProperty<TModel, T>(T oldValue, T newValue, IEqualityComparer<T> comparer, TModel model, Action<TModel, T> callback, bool broadcast, [CallerMemberName()] string? propertyName = null) where TModel : class
        {
            bool num = SetProperty(oldValue, newValue, comparer, model, callback, propertyName);
            if (num && broadcast)
            {
                Broadcast(oldValue, newValue, propertyName);
            }
            return num;
        }
    }
}
