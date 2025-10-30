// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A structure meant to represent a union of <see cref="SpecialType"/> and <see cref="InternalSpecialType"/> values
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 1)]
    internal readonly struct ExtendedSpecialType
    {
        [FieldOffset(0)]
        private readonly sbyte _value;

        private ExtendedSpecialType(int value)
        {
            Debug.Assert(value >= sbyte.MinValue && value <= sbyte.MaxValue);
            _value = (sbyte)value;
        }

        public static implicit operator ExtendedSpecialType(SpecialType value) => new ExtendedSpecialType((int)value);
        public static explicit operator SpecialType(ExtendedSpecialType value) => value._value < (int)InternalSpecialType.First ? (SpecialType)value._value : SpecialType.None;

        public static implicit operator ExtendedSpecialType(InternalSpecialType value) => new ExtendedSpecialType((int)value);
        public static explicit operator InternalSpecialType(ExtendedSpecialType value) => value._value is < (int)InternalSpecialType.First or >= (int)InternalSpecialType.NextAvailable ? InternalSpecialType.Unknown : (InternalSpecialType)value._value;

        public static explicit operator ExtendedSpecialType(int value) => new ExtendedSpecialType(value);
        public static explicit operator int(ExtendedSpecialType value) => value._value;

        public static bool operator ==(ExtendedSpecialType left, ExtendedSpecialType right) => left._value == right._value;
        public static bool operator !=(ExtendedSpecialType left, ExtendedSpecialType right) => !(left == right);

        public override bool Equals(object? obj)
        {
            switch (obj)
            {
                case ExtendedSpecialType other:
                    return this == other;

                case SpecialType other:
                    return this == other;

                case InternalSpecialType other:
                    return this == other;

                default:
                    return false;
            }
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of the ExtendedSpecialType. This exists only for debugging purposes.
        /// </summary>
        public override string ToString()
        {
            if (_value > (int)SpecialType.None && _value <= (int)SpecialType.Count)
            {
                return ((SpecialType)_value).ToString();
            }

            // When there are overlapping labels for a value in an enum, it is undefined which label is returned.
            // ReadOnlySpan is the one we care about most from a debugging perspective.
            Debug.Assert(InternalSpecialType.First == InternalSpecialType.System_ReadOnlySpan_T);
            if (_value == (int)InternalSpecialType.System_ReadOnlySpan_T)
            {
                return nameof(InternalSpecialType.System_ReadOnlySpan_T);
            }

            if (_value > (int)InternalSpecialType.First && _value < (int)InternalSpecialType.NextAvailable)
            {
                return ((InternalSpecialType)_value).ToString();
            }

            return _value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
