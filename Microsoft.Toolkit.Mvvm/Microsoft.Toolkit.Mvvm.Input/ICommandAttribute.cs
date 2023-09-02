using System;

namespace Microsoft.Toolkit.Mvvm.Input
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class ICommandAttribute : Attribute
    {
    }
}
