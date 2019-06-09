// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SyntaxTreeIndex
    {
        private struct LiteralInfo
        {
            private readonly BloomFilter _literalsFilter;

            public LiteralInfo(BloomFilter literalsFilter)
            {
                _literalsFilter = literalsFilter ?? throw new ArgumentNullException(nameof(literalsFilter));
            }

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
