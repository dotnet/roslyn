// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal sealed class ParameterSymbolReferenceFinder : AbstractReferenceFinder<IParameterSymbol>
    {
        protected override bool CanFind(IParameterSymbol symbol)
            => true;

        protected override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IParameterSymbol symbol,
            HashSet<string>? globalAliases,
            Project project,
            IImmutableSet<Document>? documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            // TODO(cyrusn): We can be smarter with parameters.  They will either be found
            // within the method that they were declared on, or they will referenced
            // elsewhere as "paramName:" or "paramName:=".  We can narrow the search by
            // filtering down to matches of that form.  For now we just return any document
            // that references something with this name.
            return FindDocumentsAsync(project, documents, cancellationToken, symbol.Name);
        }

        protected override ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IParameterSymbol symbol,
            HashSet<string>? globalAliases,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var symbolsMatchAsync = GetParameterSymbolsMatchFunction(symbol, document.Project.Solution, cancellationToken);

            return FindReferencesInDocumentUsingIdentifierAsync(
                symbol, symbol.Name, document, semanticModel, symbolsMatchAsync, cancellationToken);
        }

        private static Func<SyntaxToken, SemanticModel, ValueTask<(bool matched, CandidateReason reason)>> GetParameterSymbolsMatchFunction(
            IParameterSymbol parameter, Solution solution, CancellationToken cancellationToken)
        {
            // Get the standard function for comparing parameters.  This function will just 
            // directly compare the parameter symbols for SymbolEquivalence.
            var standardFunction = GetStandardSymbolsMatchFunction(parameter, findParentNode: null, solution, cancellationToken);

            // HOwever, we also want to consider parameter symbols them same if they unify across
            // VB's synthesized AnonymousDelegate parameters. 
            var containingMethod = parameter.ContainingSymbol as IMethodSymbol;
            if (containingMethod?.AssociatedAnonymousDelegate == null)
            {
                // This was a normal parameter, so just use the normal comparison function.
                return standardFunction;
            }

            var invokeMethod = containingMethod.AssociatedAnonymousDelegate.DelegateInvokeMethod;
            var ordinal = parameter.Ordinal;
            if (invokeMethod == null || ordinal >= invokeMethod.Parameters.Length)
            {
                return standardFunction;
            }

            // This was parameter of a method that had an associated synthesized anonyomous-delegate.
            // IN that case, we want it to match references to the corresponding parameter in that
            // anonymous-delegate's invoke method.  So get he symbol match function that will chec
            // for equivalence with that parameter.
            var anonymousDelegateParameter = invokeMethod.Parameters[ordinal];
            var anonParameterFunc = GetStandardSymbolsMatchFunction(anonymousDelegateParameter, findParentNode: null, solution, cancellationToken);

            // Return a new function which is a compound of the two functions we have.
            return async (token, model) =>
            {
                // First try the standard function.
                var result = await standardFunction(token, model).ConfigureAwait(false);
                if (!result.matched)
                {
                    // If it fails, fall back to the anon-delegate function.
                    result = await anonParameterFunc(token, model).ConfigureAwait(false);
                }

                return result;
            };
        }

        protected override async Task<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
            IParameterSymbol parameter,
            Solution solution,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            if (parameter.IsThis)
                return ImmutableArray<ISymbol>.Empty;

            using var _ = ArrayBuilder<ISymbol>.GetInstance(out var symbols);

            await CascadeBetweenAnonymousFunctionParametersAsync(solution, parameter, symbols, cancellationToken).ConfigureAwait(false);
            CascadeBetweenPropertyAndAccessorParameters(parameter, symbols);
            CascadeBetweenDelegateMethodParameters(parameter, symbols);
            CascadeBetweenPartialMethodParameters(parameter, symbols);
            CascadeBetweenPrimaryConstructorParameterAndProperties(parameter, symbols, cancellationToken);

            return symbols.ToImmutable();
        }

        private static void CascadeBetweenPrimaryConstructorParameterAndProperties(
            IParameterSymbol parameter, ArrayBuilder<ISymbol> symbols, CancellationToken cancellationToken)
        {
            symbols.AddIfNotNull(parameter.GetAssociatedSynthesizedRecordProperty(cancellationToken));
        }

        private static async Task CascadeBetweenAnonymousFunctionParametersAsync(
            Solution solution,
            IParameterSymbol parameter,
            ArrayBuilder<ISymbol> results,
            CancellationToken cancellationToken)
        {
            if (parameter.ContainingSymbol.IsAnonymousFunction())
            {
                var parameterNode = parameter.DeclaringSyntaxReferences.Select(r => r.GetSyntax(cancellationToken)).FirstOrDefault();
                if (parameterNode != null)
                {
                    var document = solution.GetDocument(parameterNode.SyntaxTree);
                    if (document != null)
                    {
                        var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
                        if (semanticFacts.ExposesAnonymousFunctionParameterNames)
                        {
                            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                            var lambdaNode = parameter.ContainingSymbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax(cancellationToken)).First();
                            var convertedType = semanticModel.GetTypeInfo(lambdaNode, cancellationToken).ConvertedType;

                            if (convertedType != null)
                            {
                                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                                var container = GetContainer(semanticModel, parameterNode, syntaxFacts);
                                if (container != null)
                                {
                                    CascadeBetweenAnonymousFunctionParameters(
                                        document, semanticModel, container, parameter,
                                        convertedType, results, cancellationToken);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void CascadeBetweenAnonymousFunctionParameters(
            Document document,
            SemanticModel semanticModel,
            SyntaxNode container,
            IParameterSymbol parameter,
            ITypeSymbol convertedType1,
            ArrayBuilder<ISymbol> results,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            foreach (var token in container.DescendantTokens())
            {
                if (IdentifiersMatch(syntaxFacts, parameter.Name, token))
                {
                    var symbol = semanticModel.GetDeclaredSymbol(token.GetRequiredParent(), cancellationToken);
                    if (symbol is IParameterSymbol &&
                        symbol.ContainingSymbol.IsAnonymousFunction() &&
                        SignatureComparer.Instance.HaveSameSignatureAndConstraintsAndReturnTypeAndAccessors(parameter.ContainingSymbol, symbol.ContainingSymbol, syntaxFacts.IsCaseSensitive) &&
                        ParameterNamesMatch(syntaxFacts, (IMethodSymbol)parameter.ContainingSymbol, (IMethodSymbol)symbol.ContainingSymbol))
                    {
                        var lambdaNode = symbol.ContainingSymbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax(cancellationToken)).First();
                        var convertedType2 = semanticModel.GetTypeInfo(lambdaNode, cancellationToken).ConvertedType;

                        if (convertedType1.Equals(convertedType2))
                        {
                            results.Add(symbol);
                        }
                    }
                }
            }
        }

        private static bool ParameterNamesMatch(ISyntaxFactsService syntaxFacts, IMethodSymbol methodSymbol1, IMethodSymbol methodSymbol2)
        {
            for (var i = 0; i < methodSymbol1.Parameters.Length; i++)
            {
                if (!syntaxFacts.TextMatch(methodSymbol1.Parameters[i].Name, methodSymbol2.Parameters[i].Name))
                {
                    return false;
                }
            }

            return true;
        }

        private static SyntaxNode? GetContainer(SemanticModel semanticModel, SyntaxNode parameterNode, ISyntaxFactsService syntaxFactsService)
        {
            for (var current = parameterNode; current != null; current = current.Parent)
            {
                var declaredSymbol = semanticModel.GetDeclaredSymbol(current);

                if (declaredSymbol is IMethodSymbol method && method.MethodKind != MethodKind.AnonymousFunction)
                {
                    return current;
                }
            }

            return syntaxFactsService.GetContainingVariableDeclaratorOfFieldDeclaration(parameterNode);
        }

        private static void CascadeBetweenPropertyAndAccessorParameters(
            IParameterSymbol parameter,
            ArrayBuilder<ISymbol> results)
        {
            var ordinal = parameter.Ordinal;
            var containingSymbol = parameter.ContainingSymbol;
            if (containingSymbol is IMethodSymbol containingMethod)
            {
                if (containingMethod.AssociatedSymbol is IPropertySymbol property)
                {
                    AddParameterAtIndex(results, ordinal, property.Parameters);
                }
            }
            else if (containingSymbol is IPropertySymbol containingProperty)
            {
                if (containingProperty.GetMethod != null && ordinal < containingProperty.GetMethod.Parameters.Length)
                {
                    results.Add(containingProperty.GetMethod.Parameters[ordinal]);
                }

                if (containingProperty.SetMethod != null && ordinal < containingProperty.SetMethod.Parameters.Length)
                {
                    results.Add(containingProperty.SetMethod.Parameters[ordinal]);
                }
            }
        }

        private static void CascadeBetweenDelegateMethodParameters(
            IParameterSymbol parameter,
            ArrayBuilder<ISymbol> results)
        {
            var ordinal = parameter.Ordinal;
            if (parameter.ContainingSymbol is IMethodSymbol containingMethod)
            {
                var containingType = containingMethod.ContainingType;
                if (containingType.IsDelegateType())
                {
                    if (containingMethod.MethodKind == MethodKind.DelegateInvoke)
                    {
                        // cascade to the corresponding parameter in the BeginInvoke method.
                        var beginInvokeMethod = containingType.GetMembers(WellKnownMemberNames.DelegateBeginInvokeName)
                                                              .OfType<IMethodSymbol>()
                                                              .FirstOrDefault();
                        AddParameterAtIndex(results, ordinal, beginInvokeMethod?.Parameters);
                    }
                    else if (containingMethod.ContainingType.IsDelegateType() &&
                             containingMethod.Name == WellKnownMemberNames.DelegateBeginInvokeName)
                    {
                        // cascade to the corresponding parameter in the Invoke method.
                        AddParameterAtIndex(results, ordinal, containingType.DelegateInvokeMethod?.Parameters);
                    }
                }
            }
        }

        private static void AddParameterAtIndex(
            ArrayBuilder<ISymbol> results,
            int ordinal,
            ImmutableArray<IParameterSymbol>? parameters)
        {
            if (parameters != null && ordinal < parameters.Value.Length)
            {
                results.Add(parameters.Value[ordinal]);
            }
        }

        private static void CascadeBetweenPartialMethodParameters(
            IParameterSymbol parameter,
            ArrayBuilder<ISymbol> results)
        {
            if (parameter.ContainingSymbol is IMethodSymbol)
            {
                var ordinal = parameter.Ordinal;
                var method = (IMethodSymbol)parameter.ContainingSymbol;
                if (method.PartialDefinitionPart != null && ordinal < method.PartialDefinitionPart.Parameters.Length)
                {
                    results.Add(method.PartialDefinitionPart.Parameters[ordinal]);
                }

                if (method.PartialImplementationPart != null && ordinal < method.PartialImplementationPart.Parameters.Length)
                {
                    results.Add(method.PartialImplementationPart.Parameters[ordinal]);
                }
            }
        }
    }
}
