using System;
using System.Linq;

namespace Microsoft.Toolkit.Mvvm.ComponentModel
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    public sealed class AlsoNotifyChangeForAttribute : Attribute
    {
        public string[] PropertyNames { get; }

        public AlsoNotifyChangeForAttribute(string propertyName)
        {
            PropertyNames = new string[1] { propertyName };
        }

        public AlsoNotifyChangeForAttribute(string propertyName, params string[] otherPropertyNames)
        {
            PropertyNames = new string[1] { propertyName }.Concat(otherPropertyNames).ToArray();
        }
    }
}
