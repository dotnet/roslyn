// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Testing
{
    internal readonly struct MatchQuality : IComparable<MatchQuality>, IEquatable<MatchQuality>
    {
        public static readonly MatchQuality Full = new MatchQuality(0);
        public static readonly MatchQuality None = new MatchQuality(4);

        private readonly int _value;

        public MatchQuality(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _value = value;
        }

        public static MatchQuality operator +(MatchQuality left, MatchQuality right)
            => new MatchQuality(left._value + right._value);

        public static MatchQuality operator -(MatchQuality left, MatchQuality right)
            => new MatchQuality(left._value - right._value);

        public static bool operator ==(MatchQuality left, MatchQuality right)
            => left.Equals(right);

        public static bool operator !=(MatchQuality left, MatchQuality right)
            => !left.Equals(right);

        public static bool operator <(MatchQuality left, MatchQuality right)
            => left._value < right._value;

        public static bool operator <=(MatchQuality left, MatchQuality right)
            => left._value <= right._value;

        public static bool operator >(MatchQuality left, MatchQuality right)
            => left._value > right._value;

        public static bool operator >=(MatchQuality left, MatchQuality right)
            => left._value >= right._value;

        public static MatchQuality RemainingUnmatched(int count)
            => new MatchQuality(None._value * count);

        public int CompareTo(MatchQuality other)
            => _value.CompareTo(other._value);

        public override bool Equals(object? obj)
            => obj is MatchQuality quality && Equals(quality);

        public bool Equals(MatchQuality other)
            => _value == other._value;

        public override int GetHashCode()
            => _value;
    }
}
