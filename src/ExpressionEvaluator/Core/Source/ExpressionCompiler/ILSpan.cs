// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    // TODO: use TextSpan?
    internal readonly struct ILSpan : IEquatable<ILSpan>
    {
        public static readonly ILSpan MaxValue = new ILSpan(0, uint.MaxValue);

        public readonly uint StartOffset;
        public readonly uint EndOffsetExclusive;

        public ILSpan(uint start, uint end)
        {
            Debug.Assert(start <= end);

            StartOffset = start;
            EndOffsetExclusive = end;
        }

        public bool Contains(int offset) => offset >= StartOffset && offset < EndOffsetExclusive;
        public bool Equals(ILSpan other) => StartOffset == other.StartOffset && EndOffsetExclusive == other.EndOffsetExclusive;
        public override bool Equals(object obj) => obj is ILSpan && Equals((ILSpan)obj);
        public override int GetHashCode() => Hash.Combine(StartOffset.GetHashCode(), EndOffsetExclusive.GetHashCode());
        public override string ToString() => $"[{StartOffset}, {EndOffsetExclusive})";
    }
}
