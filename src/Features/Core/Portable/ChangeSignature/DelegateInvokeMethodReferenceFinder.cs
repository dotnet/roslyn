﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    /// <summary>
    /// For ChangeSignature, FAR on a delegate invoke method must cascade to BeginInvoke, 
    /// cascade through method group conversions, and discover implicit invocations that do not
    /// mention the string "Invoke" or the delegate type itself. This implementation finds these
    /// symbols by binding most identifiers and invocation expressions in the solution. 
    /// </summary>
    /// <remarks>
    /// TODO: Rewrite this to track backward through references instead of binding everything
    /// </remarks>
    internal class DelegateInvokeMethodReferenceFinder : AbstractReferenceFinder<IMethodSymbol>
    {
        public static readonly IReferenceFinder DelegateInvokeMethod = new DelegateInvokeMethodReferenceFinder();

        protected override bool CanFind(IMethodSymbol symbol)
            => symbol.MethodKind == MethodKind.DelegateInvoke;

        protected override async Task<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
            IMethodSymbol symbol,
            Solution solution,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<ISymbol>.GetInstance(out var result);

            var beginInvoke = symbol.ContainingType.GetMembers(WellKnownMemberNames.DelegateBeginInvokeName).FirstOrDefault();
            if (beginInvoke != null)
                result.Add(beginInvoke);

            // All method group references
            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    var changeSignatureService = document.GetRequiredLanguageService<AbstractChangeSignatureService>();
                    var cascaded = await changeSignatureService.DetermineCascadedSymbolsFromDelegateInvokeAsync(
                        symbol, document, cancellationToken).ConfigureAwait(false);
                    result.AddRange(cascaded);
                }
            }

            return result.ToImmutable();
        }

        protected override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IMethodSymbol symbol,
            HashSet<string>? globalAliases,
            Project project,
            IImmutableSet<Document>? documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(project.Documents.ToImmutableArray());
        }

        protected override async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IMethodSymbol methodSymbol,
            HashSet<string>? globalAliases,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            // FAR on the Delegate type and use those results to find Invoke calls

            var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticFactsService = document.GetRequiredLanguageService<ISemanticFactsService>();

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var nodes = root.DescendantNodes();

            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var convertedAnonymousFunctions);
            foreach (var node in nodes)
            {
                if (!syntaxFactsService.IsAnonymousFunctionExpression(node))
                    continue;

                var convertedType = (ISymbol?)semanticModel.GetTypeInfo(node, cancellationToken).ConvertedType;
                if (convertedType != null)
                {
                    convertedType = await SymbolFinder.FindSourceDefinitionAsync(convertedType, document.Project.Solution, cancellationToken).ConfigureAwait(false)
                        ?? convertedType;
                }

                if (convertedType == methodSymbol.ContainingType)
                    convertedAnonymousFunctions.Add(node);
            }

            var invocations = nodes.Where(n => syntaxFactsService.IsInvocationExpression(n))
                .Where(e => semanticModel.GetSymbolInfo(e, cancellationToken).Symbol?.OriginalDefinition == methodSymbol);

            return invocations.Concat(convertedAnonymousFunctions).SelectAsArray(
                  node => new FinderLocation(
                      node,
                      new ReferenceLocation(
                          document,
                          alias: null,
                          node.GetLocation(),
                          isImplicit: false,
                          symbolUsageInfo: GetSymbolUsageInfo(
                              node,
                              semanticModel,
                              syntaxFactsService,
                              semanticFactsService,
                              cancellationToken),
                          additionalProperties: GetAdditionalFindUsagesProperties(
                              node, semanticModel, syntaxFactsService),
                          candidateReason: CandidateReason.None)));
        }
    }
}
