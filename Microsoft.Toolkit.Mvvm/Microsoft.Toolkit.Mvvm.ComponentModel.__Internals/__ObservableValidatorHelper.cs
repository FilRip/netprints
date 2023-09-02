using System;
using System.ComponentModel;

#nullable enable

namespace Microsoft.Toolkit.Mvvm.ComponentModel.__Internals
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This type is not intended to be used directly by user code")]
#pragma warning disable IDE1006
    public static class __ObservableValidatorHelper
#pragma warning restore IDE1006
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is not intended to be called directly by user code")]
        public static void ValidateProperty(ObservableValidator instance, object? value, string propertyName)
        {
            instance.ValidateProperty(value, propertyName);
        }
    }
}
