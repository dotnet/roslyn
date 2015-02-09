// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal static class NavigateToSymbolFinder
    {
        private static readonly char[] DotArray = new char[] { '.' };

        internal static async Task<IEnumerable<ValueTuple<DeclaredSymbolInfo, Document, IEnumerable<PatternMatch>>>> FindNavigableDeclaredSymbolInfos(Project project, string pattern, CancellationToken cancellationToken)
        {
            var patternMatcher = new PatternMatcher();
            var dotSeparatedPatternComponents = pattern.Contains(".") ? pattern.Split(DotArray, StringSplitOptions.RemoveEmptyEntries) : null;

            var result = new List<ValueTuple<DeclaredSymbolInfo, Document, IEnumerable<PatternMatch>>>();
            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var declaredSymbolInfos = await document.GetDeclaredSymbolInfosAsync(cancellationToken).ConfigureAwait(false);
                foreach (var declaredSymbolInfo in declaredSymbolInfos)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var patternMatches = TryMatch(pattern, dotSeparatedPatternComponents, patternMatcher, declaredSymbolInfo);
                    if (patternMatches != null)
                    {
                        result.Add(ValueTuple.Create(declaredSymbolInfo, document, patternMatches));
                    }
                }
            }

            return result;
        }

        private static IEnumerable<PatternMatch> TryMatch(
            string pattern,
            string[] dotSeparatedPatternComponents,
            PatternMatcher patternMatcher,
            DeclaredSymbolInfo declaredSymbolInfo)
        {
            var matches1 = TryGetDotSeparatedPatternMatches(patternMatcher, dotSeparatedPatternComponents, declaredSymbolInfo);
            var matches2 = patternMatcher.MatchPattern(GetSearchName(declaredSymbolInfo), pattern);

            if (matches1 == null)
            {
                return matches2;
            }

            if (matches2 == null)
            {
                return matches1;
            }

            return matches1.Concat(matches2);
        }

        private static IEnumerable<PatternMatch> TryGetDotSeparatedPatternMatches(
            PatternMatcher patternMatcher,
            string[] dotSeparatedPatternComponents,
            DeclaredSymbolInfo declaredSymbolInfo)
        {
            if (dotSeparatedPatternComponents == null || declaredSymbolInfo.FullyQualifiedContainerName == null || declaredSymbolInfo.Name == null)
            {
                return null;
            }

            // First, check that the last part of the dot separated pattern matches the name of the
            // declared symbol.  If not, then there's no point in proceeding and doing the more
            // expensive work.
            var symbolNameMatch = patternMatcher.MatchPattern(GetSearchName(declaredSymbolInfo), dotSeparatedPatternComponents.Last());
            if (symbolNameMatch == null)
            {
                return null;
            }

            // So far so good.  Now break up the container for the symbol and check if all
            // the dotted parts match up correctly.
            var totalMatch = symbolNameMatch.ToList();

            var containerParts = declaredSymbolInfo.FullyQualifiedContainerName
                .Split(DotArray, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            // -1 because the last part was checked against hte name, and only the rest
            // of the parts are checked against the container.
            if (dotSeparatedPatternComponents.Length - 1 > containerParts.Count)
            {
                // There weren't enough container parts to match against the pattern parts.
                // So this definitely doesn't match.
                return null;
            }

            for (int i = dotSeparatedPatternComponents.Length - 2, j = containerParts.Count - 1;
                    i >= 0;
                    i--, j--)
            {
                var dotPattern = dotSeparatedPatternComponents[i];
                var containerName = containerParts[j];
                var containerMatch = patternMatcher.MatchPattern(containerName, dotPattern);
                if (containerMatch == null)
                {
                    // This container didn't match the pattern piece.  So there's no match at all.
                    return null;
                }

                totalMatch.AddRange(containerMatch);
            }

            // Success, this symbol's full name matched against the dotted name the user was asking
            // about.
            return totalMatch;
        }

        private static string GetSearchName(DeclaredSymbolInfo declaredSymbolInfo)
        {
            if (declaredSymbolInfo.Kind == DeclaredSymbolInfoKind.Indexer && declaredSymbolInfo.Name == WellKnownMemberNames.Indexer)
            {
                return "this";
            }
            else
            {
                return declaredSymbolInfo.Name;
            }
        }
    }
}
