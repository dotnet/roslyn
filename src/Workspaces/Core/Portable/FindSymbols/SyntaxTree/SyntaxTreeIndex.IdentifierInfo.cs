// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SyntaxTreeIndex
    {
        private struct IdentifierInfo
        {
            private readonly BloomFilter _identifierFilter;
            private readonly BloomFilter _escapedIdentifierFilter;

            public IdentifierInfo(
                BloomFilter identifierFilter,
                BloomFilter escapedIdentifierFilter)
            {
                _identifierFilter = identifierFilter ?? throw new ArgumentNullException(nameof(identifierFilter));
                _escapedIdentifierFilter = escapedIdentifierFilter ?? throw new ArgumentNullException(nameof(escapedIdentifierFilter));
            }

            /// <summary>
            /// Returns true when the identifier is probably (but not guaranteed) to be within the
            /// syntax tree.  Returns false when the identifier is guaranteed to not be within the
            /// syntax tree.
            /// </summary>
            public bool ProbablyContainsIdentifier(string identifier)
                => _identifierFilter.ProbablyContains(identifier);

            /// <summary>
            /// Returns true when the identifier is probably (but not guaranteed) escaped within the
            /// text of the syntax tree.  Returns false when the identifier is guaranteed to not be
            /// escaped within the text of the syntax tree.  An identifier that is not escaped within
            /// the text can be found by searching the text directly.  An identifier that is escaped can
            /// only be found by parsing the text and syntactically interpreting any escaping
            /// mechanisms found in the language ("\uXXXX" or "@XXXX" in C# or "[XXXX]" in Visual
            /// Basic).
            /// </summary>
            public bool ProbablyContainsEscapedIdentifier(string identifier)
                => _escapedIdentifierFilter.ProbablyContains(identifier);

            public void WriteTo(ObjectWriter writer)
            {
                _identifierFilter.WriteTo(writer);
                _escapedIdentifierFilter.WriteTo(writer);
            }

            public static IdentifierInfo? TryReadFrom(ObjectReader reader)
            {
                try
                {
                    var identifierFilter = BloomFilter.ReadFrom(reader);
                    var escapedIdentifierFilter = BloomFilter.ReadFrom(reader);

                    return new IdentifierInfo(identifierFilter, escapedIdentifierFilter);
                }
                catch (Exception)
                {
                }

                return null;
            }
        }
    }
}
