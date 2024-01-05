// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SyntaxTreeIndex
    {
        private readonly struct LiteralInfo(BloomFilter literalsFilter)
        {
            private readonly BloomFilter _literalsFilter = literalsFilter ?? throw new ArgumentNullException(nameof(literalsFilter));

            /// <summary>
            /// Returns true when the identifier is probably (but not guaranteed) to be within the
            /// syntax tree.  Returns false when the identifier is guaranteed to not be within the
            /// syntax tree.
            /// </summary>
            public bool ProbablyContainsStringValue(string value)
                => _literalsFilter.ProbablyContains(value);

            public bool ProbablyContainsInt64Value(long value)
                => _literalsFilter.ProbablyContains(value);

            public void WriteTo(ObjectWriter writer)
                => _literalsFilter.WriteTo(writer);

            public static LiteralInfo? TryReadFrom(ObjectReader reader)
            {
                try
                {
                    var literalsFilter = BloomFilter.ReadFrom(reader);

                    return new LiteralInfo(literalsFilter);
                }
                catch (Exception)
                {
                }

                return null;
            }
        }
    }
}
