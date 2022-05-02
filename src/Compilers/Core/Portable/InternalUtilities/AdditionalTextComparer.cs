// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class AdditionalTextComparer : IEqualityComparer<AdditionalText>
    {
        public static readonly AdditionalTextComparer Instance = new AdditionalTextComparer();

        public bool Equals(AdditionalText? x, AdditionalText? y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            if (!PathUtilities.Comparer.Equals(x.Path, y.Path))
            {
                return false;
            }

            var xText = x.GetText();
            var yText = y.GetText();

            if (xText is null || yText is null || xText.Length != yText.Length)
            {
                return false;
            }

            return ByteSequenceComparer.Equals(xText.GetChecksum(), yText.GetChecksum());
        }

        public int GetHashCode(AdditionalText obj)
        {
            return Hash.Combine(PathUtilities.Comparer.GetHashCode(obj.Path),
                                ByteSequenceComparer.GetHashCode(obj.GetText()?.GetChecksum() ?? ImmutableArray<byte>.Empty));
        }
    }
}
