// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Humanizer;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Words = System.Collections.Immutable.ImmutableArray<string>;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers.DeclarationName
{
    internal partial class DeclarationNameRecommender
    {
        internal class NameGenerator
        {
            private const char DefaultInterfacePrefix = 'I';
            private const char DefaultGenericParameterPrefix = 'T';

            internal static ImmutableArray<Words> GetBaseNames(ITypeSymbol type, bool pluralize)
            {
                var baseName = TryRemoveKnownPrefixes(type);
                using var parts = TemporaryArray<TextSpan>.Empty;
                StringBreaker.AddWordParts(baseName, ref parts.AsRef());
                var result = GetInterleavedPatterns(parts, baseName, pluralize);

                return result;
            }

            internal static ImmutableArray<Words> GetBaseNames(IAliasSymbol alias)
            {
                var name = alias.Name;
                if (alias.Target.IsType &&
                    ((INamedTypeSymbol)alias.Target).IsInterfaceType() &&
                    CanRemovePrefix(name, DefaultInterfacePrefix))
                {
                    name = name[1..];
                }

                using var breaks = TemporaryArray<TextSpan>.Empty;
                StringBreaker.AddWordParts(name, ref breaks.AsRef());
                var result = GetInterleavedPatterns(breaks, name, pluralize: false);
                return result;
            }

            private static ImmutableArray<Words> GetInterleavedPatterns(
                in TemporaryArray<TextSpan> breaks, string baseName, bool pluralize)
            {
                using var result = TemporaryArray<Words>.Empty;
                var breakCount = breaks.Count;
                result.Add(GetWords(0, breakCount, breaks, baseName, pluralize));

                for (var length = breakCount - 1; length > 0; length--)
                {
                    // going forward
                    result.Add(GetLongestForwardSubsequence(length, breaks, baseName, pluralize));

                    // going backward
                    result.Add(GetLongestBackwardSubsequence(length, breaks, baseName, pluralize));
                }

                return result.ToImmutableAndClear();
            }

            private static Words GetLongestBackwardSubsequence(int length, in TemporaryArray<TextSpan> breaks, string baseName, bool pluralize)
            {
                var breakCount = breaks.Count;
                var start = breakCount - length;
                return GetWords(start, breakCount, breaks, baseName, pluralize);
            }

            private static Words GetLongestForwardSubsequence(int length, in TemporaryArray<TextSpan> breaks, string baseName, bool pluralize)
                => GetWords(0, length, breaks, baseName, pluralize);

            private static Words GetWords(int start, int end, in TemporaryArray<TextSpan> breaks, string baseName, bool pluralize)
            {
                using var result = TemporaryArray<string>.Empty;
                // Add all the words but the last one
                for (; start < end; start++)
                {
                    var @break = breaks[start];
                    var text = baseName.Substring(@break.Start, @break.Length);
                    if (pluralize && start == end - 1)
                    {
                        // Pluralize the last word if necessary
                        result.Add(text.Pluralize());
                    }
                    else
                    {
                        result.Add(text);
                    }
                }

                return result.ToImmutableAndClear();
            }

            //Tries to remove "I" prefix from interfaces and "T" prefix from generic parameter names
            private static string TryRemoveKnownPrefixes(ITypeSymbol type)
            {
                var name = type.Name;

                if (type.TypeKind == TypeKind.Interface)
                {
                    if (CanRemovePrefix(name, DefaultInterfacePrefix))
                    {
                        return name[1..];
                    }
                }

                if (type.TypeKind == TypeKind.TypeParameter)
                {
                    if (CanRemovePrefix(name, DefaultGenericParameterPrefix))
                    {
                        return name[1..];
                    }
                    else if (name is [DefaultGenericParameterPrefix])
                    {
                        return ITypeSymbolExtensions.DefaultParameterName;
                    }
                }

                return type.CreateParameterName();
            }
        }

        private static bool CanRemovePrefix(string name, char prefix) => name.Length > 1 && name[0] == prefix && char.IsUpper(name[1]);
    }
}
