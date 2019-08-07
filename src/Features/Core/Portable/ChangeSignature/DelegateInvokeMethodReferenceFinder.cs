// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.LanguageServices;
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
        {
            return symbol.MethodKind == MethodKind.DelegateInvoke;
        }

        protected override async Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId<IMethodSymbol> symbolAndProjectId,
            Solution solution,
            IImmutableSet<Project> projects,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var result = ImmutableArray.CreateBuilder<SymbolAndProjectId>();

            var symbol = symbolAndProjectId.Symbol;
            var beginInvoke = symbol.ContainingType.GetMembers(WellKnownMemberNames.DelegateBeginInvokeName).FirstOrDefault();
            if (beginInvoke != null)
            {
                result.Add(symbolAndProjectId.WithSymbol(beginInvoke));
            }

            // All method group references
            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    var changeSignatureService = document.GetLanguageService<AbstractChangeSignatureService>();
                    result.AddRange(await changeSignatureService.DetermineCascadedSymbolsFromDelegateInvoke(
                        symbolAndProjectId, document, cancellationToken).ConfigureAwait(false));
                }
            }

            return result.ToImmutable();
        }

        protected override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IMethodSymbol symbol,
            Project project,
            IImmutableSet<Document> documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(project.Documents.ToImmutableArray());
        }

        protected override async Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IMethodSymbol methodSymbol,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            // FAR on the Delegate type and use those results to find Invoke calls

            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            var semanticFactsService = document.GetLanguageService<ISemanticFactsService>();

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var nodes = root.DescendantNodes();

            var convertedAnonymousFunctions = nodes.Where(n => syntaxFactsService.IsAnonymousFunction(n))
                .Where(n =>
                    {
                        ISymbol convertedType = semanticModel.GetTypeInfo(n, cancellationToken).ConvertedType;

                        if (convertedType != null)
                        {
                            convertedType =
                                SymbolFinder.FindSourceDefinitionAsync(convertedType, document.Project.Solution, cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken)
                                    ?? convertedType;
                        }

                        return convertedType == methodSymbol.ContainingType;
                    });

            var invocations = nodes.Where(n => syntaxFactsService.IsInvocationExpression(n))
                .Where(e => semanticModel.GetSymbolInfo(e, cancellationToken).Symbol.OriginalDefinition == methodSymbol);

            return invocations.Concat(convertedAnonymousFunctions).SelectAsArray(
                  n => new FinderLocation(
                      n,
                      new ReferenceLocation(
                          document,
                          null,
                          n.GetLocation(),
                          isImplicit: false,
                          symbolUsageInfo: GetSymbolUsageInfo(
                              n,
                              semanticModel,
                              syntaxFactsService,
                              semanticFactsService,
                              cancellationToken),
                          containingTypeInfo: GetContainingTypeInfo(n, syntaxFactsService),
                          containingMemberInfo: GetContainingMemberInfo(n, syntaxFactsService),
                          candidateReason: CandidateReason.None)));
        }
    }
}
