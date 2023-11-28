// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class ConstantValue
    {
        /// <summary>
        /// The IEEE floating-point spec doesn't specify which bit pattern an implementation
        /// is required to use when producing NaN values.  Indeed, the spec does recommend
        /// "diagnostic" information "left to the implementer’s discretion" be placed in the
        /// undefined bits. It is therefore likely that NaNs produced on different platforms
        /// will differ even for the same arithmetic such as 0.0 / 0.0.  To ensure that the
        /// compiler behaves in a deterministic way, we force NaN values to use the
        /// IEEE "canonical" form with the diagnostic bits set to zero and the sign bit set
        /// to one.  Conversion of this value to float produces the corresponding
        /// canonical NaN of the float type (IEEE Std 754-2008 section 6.2.3).
        /// </summary>
        private static readonly double _s_IEEE_canonical_NaN = BitConverter.Int64BitsToDouble(unchecked((long)0xFFF8000000000000UL));

        private sealed class ConstantValueBad : ConstantValue
        {
            private ConstantValueBad() { }

            public static readonly ConstantValueBad Instance = new ConstantValueBad();

            public override ConstantValueTypeDiscriminator Discriminator
            {
                get
                {
                    return ConstantValueTypeDiscriminator.Bad;
                }
            }

            internal override SpecialType SpecialType
            {
                get { return SpecialType.None; }
            }

            // all instances of this class are singletons
            public override bool Equals(ConstantValue? other)
            {
                return ReferenceEquals(this, other);
            }

            public override int GetHashCode()
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
            }

            internal override string GetValueToDisplay()
            {
                return "bad";
            }
            public override string ToString(string? format, IFormatProvider? provider)
            {
                return GetValueToDisplay();
            }
        }

        private sealed class ConstantValueNull : ConstantValue
        {
            private ConstantValueNull() { }

            public static readonly ConstantValueNull Instance = new ConstantValueNull();
            public static readonly ConstantValueNull Uninitialized = new ConstantValueNull();

            public override ConstantValueTypeDiscriminator Discriminator
            {
                get
                {
                    return ConstantValueTypeDiscriminator.Null;
                }
            }

            internal override SpecialType SpecialType
            {
                get { return SpecialType.None; }
            }

            public override string? StringValue
            {
                get
                {
                    return null;
                }
            }

            internal override Rope? RopeValue
            {
                get
                {
                    return null;
                }
            }

            // all instances of this class are singletons
            public override bool Equals(ConstantValue? other)
            {
                return ReferenceEquals(this, other);
            }

            public override int GetHashCode()
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
            }

            public override bool IsDefaultValue
            {
                get
                {
                    return true;
                }
            }

            internal override string GetValueToDisplay()
            {
                return ((object)this == (object)Uninitialized) ? "unset" : "null";
            }

            public override string ToString(string? format, IFormatProvider? provider)
            {
                return GetValueToDisplay();
            }
        }

        private sealed class ConstantValueString : ConstantValue
        {
            private readonly Rope _value;
            /// <summary>
            /// Some string constant values can have large costs to realize. To compensate, we realize
            /// constant values lazily, and hold onto a weak reference. If the next time we're asked for the constant
            /// value the previous one still exists, we can avoid rerealizing it. But we don't want to root the constant
            /// value if it's not being used.
            /// </summary>
            private WeakReference<string>? _constantValueReference;

            public ConstantValueString(string value)
            {
                // we should have just one Null regardless string or object.
                RoslynDebug.Assert(value != null, "null strings should be represented as Null constant.");
                _value = Rope.ForString(value);
            }

            public ConstantValueString(Rope value)
            {
                // we should have just one Null regardless string or object.
                RoslynDebug.Assert(value != null, "null strings should be represented as Null constant.");
                _value = value;
            }

            public override ConstantValueTypeDiscriminator Discriminator
            {
                get
                {
                    return ConstantValueTypeDiscriminator.String;
                }
            }

            internal override SpecialType SpecialType
            {
                get { return SpecialType.System_String; }
            }

            public override string StringValue
            {
                get
                {
                    string? constantValue = null;
                    if (_constantValueReference?.TryGetTarget(out constantValue) != true)
                    {
                        // Note: we could end up realizing the constant value multiple times if there's
                        // a race here. Currently, this isn't believed to be an issue, as the assignment
                        // to _constantValueReference is atomic so the worst that will happen is we return
                        // different instances of a string constant.
                        constantValue = _value.ToString();
                        _constantValueReference = new WeakReference<string>(constantValue);
                    }

                    Debug.Assert(constantValue != null);
                    return constantValue;
                }
            }

            internal override Rope RopeValue
            {
                get
                {
                    return _value;
                }
            }

            public override int GetHashCode()
            {
                return Hash.Combine(base.GetHashCode(), _value.GetHashCode());
            }

            public override bool Equals(ConstantValue? other)
            {
                return base.Equals(other) && _value.Equals(other.RopeValue);
            }

            internal override string GetValueToDisplay()
            {
                return (_value == null) ? "null" : string.Format("\"{0}\"", _value);
            }

            public override string ToString(string? format, IFormatProvider? provider)
            {
                int formatLength = RopeValue.Length;
                if (format is not null && int.TryParse(format, out var len))
                {
                    formatLength = len;
                }

                return formatLength < RopeValue.Length ?
                    @$"""{RopeValue.ToString(Math.Max(formatLength - 3, 0))}...""" :
                    @$"""{RopeValue}""";
            }
        }

        private sealed class ConstantValueDecimal : ConstantValue
        {
            private readonly decimal _value;

            public ConstantValueDecimal(decimal value)
            {
                _value = value;
            }

            public override ConstantValueTypeDiscriminator Discriminator
            {
                get
                {
                    return ConstantValueTypeDiscriminator.Decimal;
                }
            }

            internal override SpecialType SpecialType
            {
                get { return SpecialType.System_Decimal; }
            }

            public override decimal DecimalValue
            {
                get
                {
                    return _value;
                }
            }

            public override int GetHashCode()
            {
                return Hash.Combine(base.GetHashCode(), _value.GetHashCode());
            }

            public override bool Equals(ConstantValue? other)
            {
                return base.Equals(other) && _value == other.DecimalValue;
            }
        }

        private sealed class ConstantValueDateTime : ConstantValue
        {
            private readonly DateTime _value;

            public ConstantValueDateTime(DateTime value)
            {
                _value = value;
            }

            public override ConstantValueTypeDiscriminator Discriminator
            {
                get
                {
                    return ConstantValueTypeDiscriminator.DateTime;
                }
            }

            internal override SpecialType SpecialType
            {
                get { return SpecialType.System_DateTime; }
            }

            public override DateTime DateTimeValue
            {
                get
                {
                    return _value;
                }
            }

            public override int GetHashCode()
            {
                return Hash.Combine(base.GetHashCode(), _value.GetHashCode());
            }

            public override bool Equals(ConstantValue? other)
            {
                return base.Equals(other) && _value == other.DateTimeValue;
            }
        }

        // base for constant classes that may represent more than one 
        // constant type
        private abstract class ConstantValueDiscriminated : ConstantValue
        {
            private readonly ConstantValueTypeDiscriminator _discriminator;

            public ConstantValueDiscriminated(ConstantValueTypeDiscriminator discriminator)
            {
                _discriminator = discriminator;
            }

            public override ConstantValueTypeDiscriminator Discriminator
            {
                get
                {
                    return _discriminator;
                }
            }

            internal override SpecialType SpecialType
            {
                get { return GetSpecialType(_discriminator); }
            }
        }

        // default value of a value type constant. (reference type constants use Null as default)
        private class ConstantValueDefault : ConstantValueDiscriminated
        {
            public static readonly ConstantValueDefault SByte = new ConstantValueDefault(ConstantValueTypeDiscriminator.SByte);
            public static readonly ConstantValueDefault Byte = new ConstantValueDefault(ConstantValueTypeDiscriminator.Byte);
            public static readonly ConstantValueDefault Int16 = new ConstantValueDefault(ConstantValueTypeDiscriminator.Int16);
            public static readonly ConstantValueDefault UInt16 = new ConstantValueDefault(ConstantValueTypeDiscriminator.UInt16);
            public static readonly ConstantValueDefault Int32 = new ConstantValueDefault(ConstantValueTypeDiscriminator.Int32);
            public static readonly ConstantValueDefault UInt32 = new ConstantValueDefault(ConstantValueTypeDiscriminator.UInt32);
            public static readonly ConstantValueDefault Int64 = new ConstantValueDefault(ConstantValueTypeDiscriminator.Int64);
            public static readonly ConstantValueDefault UInt64 = new ConstantValueDefault(ConstantValueTypeDiscriminator.UInt64);
            public static readonly ConstantValueDefault NInt = new ConstantValueDefault(ConstantValueTypeDiscriminator.NInt);
            public static readonly ConstantValueDefault NUInt = new ConstantValueDefault(ConstantValueTypeDiscriminator.NUInt);
            public static readonly ConstantValueDefault Char = new ConstantValueDefault(ConstantValueTypeDiscriminator.Char);
            public static readonly ConstantValueDefault Single = new ConstantValueSingleZero();
            public static readonly ConstantValueDefault Double = new ConstantValueDoubleZero();
            public static readonly ConstantValueDefault Decimal = new ConstantValueDecimalZero();
            public static readonly ConstantValueDefault DateTime = new ConstantValueDefault(ConstantValueTypeDiscriminator.DateTime);
            public static readonly ConstantValueDefault Boolean = new ConstantValueDefault(ConstantValueTypeDiscriminator.Boolean);

            protected ConstantValueDefault(ConstantValueTypeDiscriminator discriminator)
                : base(discriminator)
            {
            }

            public override byte ByteValue
            {
                get
                {
                    return 0;
                }
            }

            public override sbyte SByteValue
            {
                get
                {
                    return 0;
                }
            }

            public override bool BooleanValue
            {
                get
                {
                    return false;
                }
            }

            public override double DoubleValue
            {
                get
                {
                    return 0;
                }
            }

            public override float SingleValue
            {
                get
                {
                    return 0;
                }
            }

            public override decimal DecimalValue
            {
                get
                {
                    return 0;
                }
            }

            public override char CharValue
            {
                get
                {
                    return default(char);
                }
            }

            public override DateTime DateTimeValue
            {
                get
                {
                    return default(DateTime);
                }
            }

            // all instances of this class are singletons
            public override bool Equals(ConstantValue? other)
            {
                return ReferenceEquals(this, other);
            }

            public override int GetHashCode()
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
            }

            public override bool IsDefaultValue
            {
                get { return true; }
            }

            public override string ToString(string? format, IFormatProvider? provider)
            {
                return $"default({GetPrimitiveTypeName()})";
            }
        }

        private sealed class ConstantValueDecimalZero : ConstantValueDefault
        {
            internal ConstantValueDecimalZero()
                : base(ConstantValueTypeDiscriminator.Decimal)
            {
            }

            public override bool Equals(ConstantValue? other)
            {
                if (ReferenceEquals(other, this))
                {
                    return true;
                }

                if (ReferenceEquals(other, null))
                {
                    return false;
                }

                return this.Discriminator == other.Discriminator && other.DecimalValue == 0m;
            }
        }

        private sealed class ConstantValueDoubleZero : ConstantValueDefault
        {
            internal ConstantValueDoubleZero()
                : base(ConstantValueTypeDiscriminator.Double)
            {
            }

            public override bool Equals(ConstantValue? other)
            {
                if (ReferenceEquals(other, this))
                {
                    return true;
                }

                if (ReferenceEquals(other, null))
                {
                    return false;
                }

                return this.Discriminator == other.Discriminator && other.DoubleValue == 0;
            }
        }

        private sealed class ConstantValueSingleZero : ConstantValueDefault
        {
            internal ConstantValueSingleZero()
                : base(ConstantValueTypeDiscriminator.Single)
            {
            }

            public override bool Equals(ConstantValue? other)
            {
                if (ReferenceEquals(other, this))
                {
                    return true;
                }

                if (ReferenceEquals(other, null))
                {
                    return false;
                }

                return this.Discriminator == other.Discriminator && other.SingleValue == 0;
            }
        }

        private class ConstantValueOne : ConstantValueDiscriminated
        {
            public static readonly ConstantValueOne SByte = new ConstantValueOne(ConstantValueTypeDiscriminator.SByte);
            public static readonly ConstantValueOne Byte = new ConstantValueOne(ConstantValueTypeDiscriminator.Byte);
            public static readonly ConstantValueOne Int16 = new ConstantValueOne(ConstantValueTypeDiscriminator.Int16);
            public static readonly ConstantValueOne UInt16 = new ConstantValueOne(ConstantValueTypeDiscriminator.UInt16);
            public static readonly ConstantValueOne Int32 = new ConstantValueOne(ConstantValueTypeDiscriminator.Int32);
            public static readonly ConstantValueOne UInt32 = new ConstantValueOne(ConstantValueTypeDiscriminator.UInt32);
            public static readonly ConstantValueOne Int64 = new ConstantValueOne(ConstantValueTypeDiscriminator.Int64);
            public static readonly ConstantValueOne UInt64 = new ConstantValueOne(ConstantValueTypeDiscriminator.UInt64);
            public static readonly ConstantValueOne NInt = new ConstantValueOne(ConstantValueTypeDiscriminator.NInt);
            public static readonly ConstantValueOne NUInt = new ConstantValueOne(ConstantValueTypeDiscriminator.NUInt);
            public static readonly ConstantValueOne Single = new ConstantValueOne(ConstantValueTypeDiscriminator.Single);
            public static readonly ConstantValueOne Double = new ConstantValueOne(ConstantValueTypeDiscriminator.Double);
            public static readonly ConstantValueOne Decimal = new ConstantValueDecimalOne();
            public static readonly ConstantValueOne Boolean = new ConstantValueOne(ConstantValueTypeDiscriminator.Boolean);
            public static readonly ConstantValueOne Char = new ConstantValueOne(ConstantValueTypeDiscriminator.Char);

            protected ConstantValueOne(ConstantValueTypeDiscriminator discriminator)
                : base(discriminator)
            {
            }

            public override byte ByteValue
            {
                get
                {
                    return 1;
                }
            }

            public override sbyte SByteValue
            {
                get
                {
                    return 1;
                }
            }

            public override bool BooleanValue
            {
                get
                {
                    return true;
                }
            }

            public override double DoubleValue
            {
                get
                {
                    return 1;
                }
            }

            public override float SingleValue
            {
                get
                {
                    return 1;
                }
            }

            public override decimal DecimalValue
            {
                get
                {
                    return 1;
                }
            }

            public override int Int32Value
            {
                get
                {
                    return 1;
                }
            }

            public override uint UInt32Value
            {
                get
                {
                    return 1;
                }
            }

            public override long Int64Value
            {
                get
                {
                    return 1;
                }
            }

            public override ulong UInt64Value
            {
                get
                {
                    return 1;
                }
            }

            public override short Int16Value
            {
                get
                {
                    return 1;
                }
            }

            public override ushort UInt16Value
            {
                get
                {
                    return 1;
                }
            }

            public override char CharValue
            {
                get
                {
                    return (char)1;
                }
            }

            public override bool IsOne
            {
                get
                {
                    return true;
                }
            }

            // all instances of this class are singletons
            public override bool Equals(ConstantValue? other)
            {
                return ReferenceEquals(this, other);
            }

            public override int GetHashCode()
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
            }
        }

        private sealed class ConstantValueDecimalOne : ConstantValueOne
        {
            internal ConstantValueDecimalOne()
                : base(ConstantValueTypeDiscriminator.Decimal)
            {
            }

            public override bool Equals(ConstantValue? other)
            {
                if (ReferenceEquals(other, this))
                {
                    return true;
                }

                if (ReferenceEquals(other, null))
                {
                    return false;
                }

                return this.Discriminator == other.Discriminator && other.DecimalValue == 1m;
            }
        }

        private sealed class ConstantValueI8 : ConstantValueDiscriminated
        {
            private readonly byte _value;

            public ConstantValueI8(sbyte value)
                : base(ConstantValueTypeDiscriminator.SByte)
            {
                _value = unchecked((byte)value);
            }

            public ConstantValueI8(byte value)
                : base(ConstantValueTypeDiscriminator.Byte)
            {
                _value = value;
            }

            public override byte ByteValue
            {
                get
                {
                    return _value;
                }
            }

            public override sbyte SByteValue
            {
                get
                {
                    return unchecked((sbyte)(_value));
                }
            }

            public override int GetHashCode()
            {
                return Hash.Combine(base.GetHashCode(), _value.GetHashCode());
            }

            public override bool Equals(ConstantValue? other)
            {
                return base.Equals(other) && _value == other.ByteValue;
            }
        }

        private sealed class ConstantValueI16 : ConstantValueDiscriminated
        {
            private readonly short _value;

            public ConstantValueI16(short value)
                : base(ConstantValueTypeDiscriminator.Int16)
            {
                _value = value;
            }

            public ConstantValueI16(ushort value)
                : base(ConstantValueTypeDiscriminator.UInt16)
            {
                _value = unchecked((short)value);
            }

            public ConstantValueI16(char value)
                : base(ConstantValueTypeDiscriminator.Char)
            {
                _value = unchecked((short)value);
            }

            public override short Int16Value
            {
                get
                {
                    return _value;
                }
            }

            public override ushort UInt16Value
            {
                get
                {
                    return unchecked((ushort)_value);
                }
            }

            public override char CharValue
            {
                get
                {
                    return unchecked((char)_value);
                }
            }

            public override int GetHashCode()
            {
                return Hash.Combine(base.GetHashCode(), _value.GetHashCode());
            }

            public override bool Equals(ConstantValue? other)
            {
                return base.Equals(other) && _value == other.Int16Value;
            }
        }

        private sealed class ConstantValueI32 : ConstantValueDiscriminated
        {
            private readonly int _value;

            public ConstantValueI32(int value)
                : base(ConstantValueTypeDiscriminator.Int32)
            {
                _value = value;
            }

            public ConstantValueI32(uint value)
                : base(ConstantValueTypeDiscriminator.UInt32)
            {
                _value = unchecked((int)value);
            }

            public override int Int32Value
            {
                get
                {
                    return _value;
                }
            }

            public override uint UInt32Value
            {
                get
                {
                    return unchecked((uint)_value);
                }
            }

            public override int GetHashCode()
            {
                return Hash.Combine(base.GetHashCode(), _value.GetHashCode());
            }

            public override bool Equals(ConstantValue? other)
            {
                return base.Equals(other) && _value == other.Int32Value;
            }
        }

        private sealed class ConstantValueI64 : ConstantValueDiscriminated
        {
            private readonly long _value;

            public ConstantValueI64(long value)
                : base(ConstantValueTypeDiscriminator.Int64)
            {
                _value = value;
            }

            public ConstantValueI64(ulong value)
                : base(ConstantValueTypeDiscriminator.UInt64)
            {
                _value = unchecked((long)value);
            }

            public override long Int64Value
            {
                get
                {
                    return _value;
                }
            }

            public override ulong UInt64Value
            {
                get
                {
                    return unchecked((ulong)_value);
                }
            }

            public override int GetHashCode()
            {
                return Hash.Combine(base.GetHashCode(), _value.GetHashCode());
            }

            public override bool Equals(ConstantValue? other)
            {
                return base.Equals(other) && _value == other.Int64Value;
            }
        }

        private sealed class ConstantValueNativeInt : ConstantValueDiscriminated
        {
            // Constants are limited to 32-bit for portability.
            private readonly int _value;

            public ConstantValueNativeInt(int value)
                : base(ConstantValueTypeDiscriminator.NInt)
            {
                _value = value;
            }

            public ConstantValueNativeInt(uint value)
                : base(ConstantValueTypeDiscriminator.NUInt)
            {
                _value = unchecked((int)value);
            }

            public override int Int32Value
            {
                get
                {
                    return _value;
                }
            }

            public override uint UInt32Value
            {
                get
                {
                    return unchecked((uint)_value);
                }
            }

            public override int GetHashCode()
            {
                return Hash.Combine(base.GetHashCode(), _value.GetHashCode());
            }

            public override bool Equals(ConstantValue? other)
            {
                return base.Equals(other) && _value == other.Int32Value;
            }
        }

        private sealed class ConstantValueDouble : ConstantValueDiscriminated
        {
            private readonly double _value;

            public ConstantValueDouble(double value)
                : base(ConstantValueTypeDiscriminator.Double)
            {
                if (double.IsNaN(value))
                {
                    value = _s_IEEE_canonical_NaN;
                }

                _value = value;
            }

            public override double DoubleValue
            {
                get
                {
                    return _value;
                }
            }

            public override int GetHashCode()
            {
                return Hash.Combine(base.GetHashCode(), _value.GetHashCode());
            }

            public override bool Equals(ConstantValue? other)
            {
                return base.Equals(other) && _value.Equals(other.DoubleValue);
            }

        }

        private sealed class ConstantValueSingle : ConstantValueDiscriminated
        {
            // C# performs constant folding on floating point values in full precision
            // so this class stores values in double precision
            // DoubleValue can be used to get unclipped value
            private readonly double _value;

            public ConstantValueSingle(double value)
                : base(ConstantValueTypeDiscriminator.Single)
            {
                if (double.IsNaN(value))
                {
                    value = _s_IEEE_canonical_NaN;
                }

                _value = value;
            }

            public override double DoubleValue
            {
                get
                {
                    return _value;
                }
            }

            public override float SingleValue
            {
                get
                {
                    return (float)_value;
                }
            }

            public override int GetHashCode()
            {
                return Hash.Combine(base.GetHashCode(), _value.GetHashCode());
            }

            public override bool Equals(ConstantValue? other)
            {
                return base.Equals(other) && _value.Equals(other.DoubleValue);
            }
        }
    }
}
