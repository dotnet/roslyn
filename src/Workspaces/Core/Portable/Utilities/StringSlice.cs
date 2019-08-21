// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Utilities
{
    internal struct StringSlice : IEquatable<StringSlice>
    {
        private readonly string _underlyingString;
        private readonly TextSpan _span;

        public StringSlice(string underlyingString, TextSpan span)
        {
            _underlyingString = underlyingString;
            _span = span;

            Debug.Assert(span.Start >= 0);
            Debug.Assert(span.End <= underlyingString.Length);
        }

        public StringSlice(string value) : this(value, new TextSpan(0, value.Length))
        {
        }

        public int Length => _span.Length;

        public char this[int index] => _underlyingString[_span.Start + index];

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public override bool Equals(object obj) => Equals((StringSlice)obj);

        public bool Equals(StringSlice other) => EqualsOrdinal(other);

        internal bool EqualsOrdinal(StringSlice other)
        {
            if (this._span.Length != other._span.Length)
            {
                return false;
            }

            var end = this._span.End;
            for (int i = this._span.Start, j = other._span.Start; i < end; i++, j++)
            {
                if (this._underlyingString[i] != other._underlyingString[j])
                {
                    return false;
                }
            }

            return true;
        }

        internal bool EqualsOrdinalIgnoreCase(StringSlice other)
        {
            if (this._span.Length != other._span.Length)
            {
                return false;
            }

            var end = this._span.End;
            for (int i = this._span.Start, j = other._span.Start; i < end; i++, j++)
            {
                var thisChar = this._underlyingString[i];
                var otherChar = other._underlyingString[j];

                if (!EqualsOrdinalIgnoreCase(thisChar, otherChar))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool EqualsOrdinalIgnoreCase(char thisChar, char otherChar)
        {
            // Do a fast check first before converting to lowercase characters.
            return
                thisChar == otherChar ||
                CaseInsensitiveComparison.ToLower(thisChar) == CaseInsensitiveComparison.ToLower(otherChar);
        }

        public override int GetHashCode() => GetHashCodeOrdinal();

        internal int GetHashCodeOrdinal()
        {
            return Hash.GetFNVHashCode(this._underlyingString, this._span.Start, this._span.Length);
        }

        internal int GetHashCodeOrdinalIgnoreCase()
        {
            return Hash.GetCaseInsensitiveFNVHashCode(this._underlyingString, this._span.Start, this._span.Length);
        }

        internal int CompareToOrdinal(StringSlice other)
            => IComparableHelper.CompareTo(this, other, GetComparablesToOrdinal);

        private static IEnumerable<IComparable> GetComparablesToOrdinal(StringSlice slice)
        {
            for (int i = slice._span.Start; i < slice._span.End; i++)
            {
                yield return true;
                yield return slice._underlyingString[i];
            }

            // Choose the one that is shorter if their prefixes match so far.
            yield return false;
        }

        internal int CompareToOrdinalIgnoreCase(StringSlice other)
            => IComparableHelper.CompareTo(this, other, GetComparablesToOrdinalIgnoreCase);

        private static IEnumerable<IComparable> GetComparablesToOrdinalIgnoreCase(StringSlice slice)
        {
            for (int i = slice._span.Start; i < slice._span.End; i++)
            {
                yield return true;
                yield return CaseInsensitiveComparison.ToLower(slice._underlyingString[i]);
            }

            // Choose the one that is shorter if their prefixes match so far.
            yield return false;
        }

        public struct Enumerator
        {
            private readonly StringSlice _stringSlice;
            private int index;

            public Enumerator(StringSlice stringSlice)
            {
                _stringSlice = stringSlice;
                index = -1;
            }

            public bool MoveNext()
            {
                index++;
                return index < _stringSlice.Length;
            }

            public char Current => _stringSlice[index];
        }
    }

    internal abstract class StringSliceComparer : IComparer<StringSlice>, IEqualityComparer<StringSlice>
    {
        public static readonly StringSliceComparer Ordinal = new OrdinalComparer();
        public static readonly StringSliceComparer OrdinalIgnoreCase = new OrdinalIgnoreCaseComparer();

        private class OrdinalComparer : StringSliceComparer
        {
            public override int Compare(StringSlice x, StringSlice y)
                => x.CompareToOrdinal(y);

            public override bool Equals(StringSlice x, StringSlice y)
                => x.EqualsOrdinal(y);

            public override int GetHashCode(StringSlice obj)
                => obj.GetHashCodeOrdinal();
        }

        private class OrdinalIgnoreCaseComparer : StringSliceComparer
        {
            public override int Compare(StringSlice x, StringSlice y)
                => x.CompareToOrdinalIgnoreCase(y);

            public override bool Equals(StringSlice x, StringSlice y)
                => x.EqualsOrdinalIgnoreCase(y);

            public override int GetHashCode(StringSlice obj)
                => obj.GetHashCodeOrdinalIgnoreCase();
        }

        public abstract int Compare(StringSlice x, StringSlice y);
        public abstract bool Equals(StringSlice x, StringSlice y);
        public abstract int GetHashCode(StringSlice obj);
    }
}
