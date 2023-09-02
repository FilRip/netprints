using System;

namespace Microsoft.Toolkit.Mvvm.ComponentModel
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ObservablePropertyAttribute : Attribute
    {
    }
}
