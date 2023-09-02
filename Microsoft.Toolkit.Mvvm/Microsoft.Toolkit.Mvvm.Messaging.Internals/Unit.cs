using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#nullable enable

namespace Microsoft.Toolkit.Mvvm.Messaging.Internals
{
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    internal readonly struct Unit : IEquatable<Unit>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Unit other)
        {
            return true;
        }

        public override bool Equals(object? obj)
        {
            return obj is Unit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return 0;
        }
    }
}
