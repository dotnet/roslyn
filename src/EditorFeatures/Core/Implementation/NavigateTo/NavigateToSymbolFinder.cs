// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.GeneratedCodeRecognition;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal static class NavigateToSymbolFinder
    {
        internal static async Task<IEnumerable<ValueTuple<DeclaredSymbolInfo, Document, IEnumerable<PatternMatch>>>> FindNavigableDeclaredSymbolInfos(Project project, string pattern, CancellationToken cancellationToken)
        {
            var patternMatcher = new PatternMatcher(pattern);

            var result = new List<ValueTuple<DeclaredSymbolInfo, Document, IEnumerable<PatternMatch>>>();
            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var declaredSymbolInfos = await document.GetDeclaredSymbolInfosAsync(cancellationToken).ConfigureAwait(false);
                foreach (var declaredSymbolInfo in declaredSymbolInfos)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var patternMatches = patternMatcher.GetMatches(
                        GetSearchName(declaredSymbolInfo),
                        declaredSymbolInfo.FullyQualifiedContainerName,
                        includeMatchSpans: false);

                    if (patternMatches != null)
                    {
                        result.Add(ValueTuple.Create(declaredSymbolInfo, document, patternMatches));
                    }
                }
            }

            return result;
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
