// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Words = System.Collections.Generic.IEnumerable<string>;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class DeclarationNameCompletionProvider
    {
        internal class NameGenerator
        {
            internal static ImmutableArray<IEnumerable<string>> GetBaseNames(ITypeSymbol type)
            {
                var baseName = TryRemoveInterfacePrefix(type);
                using (var breaks = StringBreaker.BreakIntoWordParts(baseName))
                {
                    return GetInterleavedPatterns(breaks, baseName);
                }
            }

            private static ImmutableArray<IEnumerable<string>> GetInterleavedPatterns(StringBreaks breaks, string baseName)
            {
                var result = ArrayBuilder<IEnumerable<string>>.GetInstance();
                var breakCount = breaks.GetCount();
                result.Add(GetWords(0, breakCount, breaks, baseName));

                for (int length = breakCount - 1; length > 0; length--)
                {
                    // going forward
                    result.Add(GetLongestForwardSubsequence(length, breaks, baseName));

                    // going backward
                    result.Add(GetLongestBackwardSubsequence(length, breaks, baseName));
                }

                return result.ToImmutable();
            }

            private static Words GetLongestBackwardSubsequence(int length, StringBreaks breaks, string baseName)
            {
                var breakCount = breaks.GetCount();
                var start = breakCount - length;
                return GetWords(start, breakCount, breaks, baseName);
            }

            private static Words GetLongestForwardSubsequence(int length, StringBreaks breaks, string baseName)
            {
                var end = length;
                return GetWords(0, end, breaks, baseName);
            }

            private static Words GetWords(int start, int end, StringBreaks breaks, string baseName)
            {
                var result = ImmutableArray.Create<string>();
                for (; start < end; start++)
                {
                    var @break = breaks[start];
                    result = result.Add(baseName.Substring(@break.Start, @break.Length));
                }

                return result;
            }

            private static string TryRemoveInterfacePrefix(ITypeSymbol type)
            {
                var name = type.Name;
                if (type.TypeKind == TypeKind.Interface && name.Length > 1)
                {
                    if (name[0] == 'I' && char.IsLower(name[1]))
                    {
                        return name.Substring(1);
                    }
                }
                return type.CreateParameterName();
            }
        }
    }
}