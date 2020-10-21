// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.Utilities
{
    public static class RenameHelpers
    {
        public static async Task AssertRenameAnnotationsAsync(Solution originalSolution, Solution newSolution, IReadOnlyDictionary<string, string> symbolPairs, CancellationToken cancellationToken = default)
        {
            var allDocuments = newSolution.Projects.SelectMany(p => p.Documents);
            var remainingSymbols = symbolPairs.ToHashSet();

            foreach (var document in allDocuments)
            {
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken);
                var annotatedNodesAndTokens = root.GetAnnotatedNodesAndTokens(RenameSymbolAnnotation.RenameSymbolKind);

                var annotatedNodes = annotatedNodesAndTokens.Select(nodeOrToken => nodeOrToken.IsNode ? nodeOrToken.AsNode() : nodeOrToken.AsToken().Parent);

                var originalDocument = originalSolution.GetRequiredDocument(document.Id);
                var originalSemanticModel = await originalDocument.GetRequiredSemanticModelAsync(cancellationToken);
                var originalCompilation = originalSemanticModel.Compilation;

                var newSemanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken);

                foreach (var node in annotatedNodes)
                {
                    var annotation = node!.GetAnnotations(RenameSymbolAnnotation.RenameSymbolKind).Single();
                    var originalSymbol = RenameSymbolAnnotation.ResolveSymbol(annotation, originalCompilation);
                    var newSymbol = newSemanticModel.GetDeclaredSymbol(node, cancellationToken);

                    Assert.NotNull(originalSymbol);
                    Assert.NotNull(newSymbol);

                    var pair = (originalSymbol!.ToDisplayString(), newSymbol!.ToDisplayString());

                    var isRemoved = remainingSymbols.Remove(pair.ToKeyValuePair());
                    if (!isRemoved)
                    {
                        Assert.True(symbolPairs.Contains(pair.ToKeyValuePair()), $"Rename annotation for {pair} not expected");
                    }
                }
            }

            Assert.Empty(remainingSymbols);
        }

        public static ImmutableDictionary<string, string> MakeSymbolPairs(params (string, string)[] pairs)
            => pairs.ToImmutableDictionary(p => p.Item1, p => p.Item2);

        public static ImmutableDictionary<string, string> MakeSymbolPairs(params string[] items)
        {
            Assert.Equal(0, items.Length % 2);

            var tuples = new List<(string, string)>(items.Length / 2);

            for (var i = 0; i < items.Length; i += 2)
            {
                tuples.Add((items[i], items[i + 1]));
            }

            return MakeSymbolPairs(tuples.ToArray());
        }
    }
}
