// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Utilities
{
    internal abstract class StringSliceComparer : IComparer<ReadOnlyMemory<char>>, IEqualityComparer<ReadOnlyMemory<char>>
    {
        public static readonly StringSliceComparer Ordinal = new OrdinalComparer();
        public static readonly StringSliceComparer OrdinalIgnoreCase = new OrdinalIgnoreCaseComparer();

        private class OrdinalComparer : StringSliceComparer
        {
            public override int Compare(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
                => x.Span.CompareTo(y.Span, StringComparison.Ordinal);

            public override bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
                => x.Span.Equals(y.Span, StringComparison.Ordinal);

            public override int GetHashCode(ReadOnlyMemory<char> obj)
                => Hash.GetFNVHashCode(obj.Span);
        }

        private class OrdinalIgnoreCaseComparer : StringSliceComparer
        {
            public override int Compare(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
                => CaseInsensitiveComparison.Compare(x.Span, y.Span);

            public override bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
                => CaseInsensitiveComparison.Equals(x.Span, y.Span);

            public override int GetHashCode(ReadOnlyMemory<char> obj)
                => Hash.GetCaseInsensitiveFNVHashCode(obj.Span);
        }

        public abstract int Compare(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y);
        public abstract bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y);
        public abstract int GetHashCode(ReadOnlyMemory<char> obj);
    }
}
