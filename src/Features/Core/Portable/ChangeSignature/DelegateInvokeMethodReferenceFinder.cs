// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            => symbol.MethodKind == MethodKind.DelegateInvoke;

        protected override async Task<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
            IMethodSymbol symbol,
            Solution solution,
            IImmutableSet<Project> projects,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var result = ImmutableArray.CreateBuilder<ISymbol>();

            var beginInvoke = symbol.ContainingType.GetMembers(WellKnownMemberNames.DelegateBeginInvokeName).FirstOrDefault();
            if (beginInvoke != null)
            {
                result.Add(beginInvoke);
            }

            // All method group references
            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    var changeSignatureService = document.GetLanguageService<AbstractChangeSignatureService>();
                    result.AddRange(await changeSignatureService.DetermineCascadedSymbolsFromDelegateInvokeAsync(
                        symbol, document, cancellationToken).ConfigureAwait(false));
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
