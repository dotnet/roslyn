// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;
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

            var xText = GetTextOrNullIfBinary(x);
            var yText = GetTextOrNullIfBinary(y);

            // If xText and yText are both null, then the additional text is observably not changed
            // and can be treated as equal.
            if (xText is null && yText is null)
            {
                return true;
            }

            if (xText is null || yText is null || xText.Length != yText.Length)
            {
                return false;
            }

            return ByteSequenceComparer.Equals(xText.GetChecksum(), yText.GetChecksum());
        }

        public int GetHashCode(AdditionalText obj)
        {
            return Hash.Combine(PathUtilities.Comparer.GetHashCode(obj.Path),
                                ByteSequenceComparer.GetHashCode(GetTextOrNullIfBinary(obj)?.GetChecksum() ?? ImmutableArray<byte>.Empty));
        }

        private static SourceText? GetTextOrNullIfBinary(AdditionalText text)
        {
            try
            {
                return text.GetText();
            }
            catch (InvalidDataException)
            {
                // InvalidDataException is thrown when the underlying text is binary
                return null;
            }
        }
    }
}
