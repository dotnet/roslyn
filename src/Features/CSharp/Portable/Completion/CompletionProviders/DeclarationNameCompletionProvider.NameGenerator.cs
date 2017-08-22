// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Humanizer;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Words = System.Collections.Immutable.ImmutableArray<string>;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class DeclarationNameCompletionProvider
    {
        internal class NameGenerator
        {
            internal static ImmutableArray<Words> GetBaseNames(ITypeSymbol type, bool pluralize)
            {
                var baseName = TryRemoveInterfacePrefix(type);
                var parts = StringBreaker.GetWordParts(baseName);
                var result = GetInterleavedPatterns(parts, baseName);
                if (pluralize)
                {
                    result = Pluralize(result);
                }

                parts.Free();
                return result;
            }

            private static ImmutableArray<Words> Pluralize(ImmutableArray<Words> baseNames)
            {
                var result = ArrayBuilder<Words>.GetInstance();
                foreach (var baseName in baseNames)
                {
                    var lastWord = baseName[baseName.Length - 1];
                    var pluralizedLastWord = lastWord.Pluralize(inputIsKnownToBeSingular: false);
                    if (lastWord != pluralizedLastWord)
                    {
                        result.Add(baseName.RemoveAt(baseName.Length -1).Add(pluralizedLastWord));
                    }
                    else
                    {
                        result.Add(baseName);
                    }
                }

                return result.ToImmutableAndFree();
            }

            internal static ImmutableArray<Words> GetBaseNames(IAliasSymbol alias)
            {
                var name = alias.Name;
                if (alias.Target.IsType && (((INamedTypeSymbol)alias.Target).IsInterfaceType()
                    && CanRemoveInterfacePrefix(name)))
                {
                    name = name.Substring(1);
                }

                var breaks = StringBreaker.GetWordParts(name);
                var result = GetInterleavedPatterns(breaks, name);
                breaks.Free();
                return result;
            }

            private static ImmutableArray<Words> GetInterleavedPatterns(ArrayBuilder<TextSpan> breaks, string baseName)
            {
                var result = ArrayBuilder<Words>.GetInstance();
                var breakCount = breaks.Count;
                result.Add(GetWords(0, breakCount, breaks, baseName));

                for (var length = breakCount - 1; length > 0; length--)
                {
                    // going forward
                    result.Add(GetLongestForwardSubsequence(length, breaks, baseName));

                    // going backward
                    result.Add(GetLongestBackwardSubsequence(length, breaks, baseName));
                }

                return result.ToImmutable();
            }

            private static Words GetLongestBackwardSubsequence(int length, ArrayBuilder<TextSpan> breaks, string baseName)
            {
                var breakCount = breaks.Count;
                var start = breakCount - length;
                return GetWords(start, breakCount, breaks, baseName);
            }

            private static Words GetLongestForwardSubsequence(int length, ArrayBuilder<TextSpan> breaks, string baseName)
            {
                return GetWords(0, length, breaks, baseName);
            }

            private static Words GetWords(int start, int end, ArrayBuilder<TextSpan> breaks, string baseName)
            {
                var result = ArrayBuilder<string>.GetInstance();
                for (; start < end; start++)
                {
                    var @break = breaks[start];
                    result.Add(baseName.Substring(@break.Start, @break.Length));
                }

                return result.ToImmutableAndFree();
            }

            private static string TryRemoveInterfacePrefix(ITypeSymbol type)
            {
                var name = type.Name;
                if (type.TypeKind == TypeKind.Interface && name.Length > 1)
                {
                    if (CanRemoveInterfacePrefix(name))
                    {
                        return name.Substring(1);
                    }
                }
                return type.CreateParameterName();
            }
        }

        private static bool CanRemoveInterfacePrefix(string name) => name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]);
    }
}
