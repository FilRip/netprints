using System;

namespace Microsoft.Toolkit.Mvvm.ComponentModel
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class INotifyPropertyChangedAttribute : Attribute
    {
        public bool IncludeAdditionalHelperMethods { get; set; } = true;

    }
}
