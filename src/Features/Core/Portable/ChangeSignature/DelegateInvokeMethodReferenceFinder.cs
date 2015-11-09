// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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

        protected override async Task<IEnumerable<ISymbol>> DetermineCascadedSymbolsAsync(
            IMethodSymbol symbol,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            var result = new List<ISymbol>();

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
                    result.AddRange(await changeSignatureService.DetermineCascadedSymbolsFromDelegateInvoke(symbol, document, cancellationToken).ConfigureAwait(false));
                }
            }

            return result;
        }

        protected override Task<IEnumerable<Document>> DetermineDocumentsToSearchAsync(
            IMethodSymbol symbol,
            Project project,
            IImmutableSet<Document> documents,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(project.Documents);
        }

        protected override async Task<IEnumerable<ReferenceLocation>> FindReferencesInDocumentAsync(
            IMethodSymbol methodSymbol,
            Document document,
            CancellationToken cancellationToken)
        {
            // FAR on the Delegate type and use those results to find Invoke calls

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var nodes = root.DescendantNodes();

            var convertedAnonymousFunctions = nodes.Where(n => syntaxFactsService.IsAnonymousFunction(n))
                .Where(n =>
                    {
                        ISymbol convertedType = semanticModel.GetTypeInfo(n, cancellationToken).ConvertedType;

                        if (convertedType != null)
                        {
                            convertedType =
                                SymbolFinder.FindSourceDefinitionAsync(convertedType, document.Project.Solution, cancellationToken).WaitAndGetResult(cancellationToken)
                                    ?? convertedType;
                        }

                        return convertedType == methodSymbol.ContainingType;
                    });

            var invocations = nodes.Where(n => syntaxFactsService.IsInvocationExpression(n))
                .Where(e => semanticModel.GetSymbolInfo(e, cancellationToken).Symbol.OriginalDefinition == methodSymbol);

            return invocations.Concat(convertedAnonymousFunctions).Select(
                e => new ReferenceLocation(document, null, e.GetLocation(), isImplicit: false, isWrittenTo: false, candidateReason: CandidateReason.None));
        }
    }
}
