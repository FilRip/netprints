using System;
using System.Runtime.CompilerServices;

#nullable enable

namespace Microsoft.Toolkit.Mvvm.Messaging.Internals
{
    internal readonly struct Type2 : IEquatable<Type2>
    {
        public readonly Type TMessage;

        public readonly Type TToken;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Type2(Type tMessage, Type tToken)
        {
            TMessage = tMessage;
            TToken = tToken;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Type2 other)
        {
            if (TMessage == other.TMessage)
            {
                return TToken == other.TToken;
            }
            return false;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Type2 other)
            {
                return Equals(other);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            int hashCode = TMessage.GetHashCode();
            hashCode = (hashCode << 5) + hashCode;
            return hashCode + TToken.GetHashCode();
        }
    }
}
