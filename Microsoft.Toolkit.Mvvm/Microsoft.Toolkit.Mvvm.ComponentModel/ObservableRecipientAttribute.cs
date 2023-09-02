using System;

namespace Microsoft.Toolkit.Mvvm.ComponentModel
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ObservableRecipientAttribute : Attribute
    {
    }
}
