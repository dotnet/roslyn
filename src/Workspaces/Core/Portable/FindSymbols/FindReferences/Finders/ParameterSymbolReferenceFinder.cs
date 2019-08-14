// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class ParameterSymbolReferenceFinder : AbstractReferenceFinder<IParameterSymbol>
    {
        protected override bool CanFind(IParameterSymbol symbol)
        {
            return true;
        }

        protected override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IParameterSymbol symbol,
            Project project,
            IImmutableSet<Document> documents,
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

        protected override Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            IParameterSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var symbolsMatch = GetParameterSymbolsMatchFunction(
                symbol, document.Project.Solution, cancellationToken);

            return FindReferencesInDocumentUsingIdentifierAsync(
                symbol.Name, document, semanticModel, symbolsMatch, cancellationToken);
        }

        private Func<SyntaxToken, SemanticModel, (bool matched, CandidateReason reason)> GetParameterSymbolsMatchFunction(
            IParameterSymbol parameter, Solution solution, CancellationToken cancellationToken)
        {
            // Get the standard function for comparing parameters.  This function will just 
            // directly compare the parameter symbols for SymbolEquivalence.
            var standardFunction = GetStandardSymbolsMatchFunction(
                parameter, findParentNode: null, solution: solution, cancellationToken: cancellationToken);

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
            var anonParameterFunc = GetStandardSymbolsMatchFunction(
                anonymousDelegateParameter, findParentNode: null, solution: solution, cancellationToken: cancellationToken);

            // Return a new function which is a compound of the two functions we have.
            return (token, model) =>
            {
                // First try the standard function.
                var result = standardFunction(token, model);
                if (!result.matched)
                {
                    // If it fails, fall back to the anon-delegate function.
                    result = anonParameterFunc(token, model);
                }

                return result;
            };
        }

        protected override async Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId<IParameterSymbol> parameterAndProjectId,
            Solution solution,
            IImmutableSet<Project> projects,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var parameter = parameterAndProjectId.Symbol;
            if (parameter.IsThis)
            {
                return ImmutableArray<SymbolAndProjectId>.Empty;
            }

            var result = ArrayBuilder<SymbolAndProjectId>.GetInstance();

            await CascadeBetweenAnonymousFunctionParametersAsync(solution, parameterAndProjectId, result, cancellationToken).ConfigureAwait(false);
            CascadeBetweenPropertyAndAccessorParameters(solution, parameterAndProjectId, result);
            CascadeBetweenDelegateMethodParameters(solution, parameterAndProjectId, result);
            CascadeBetweenPartialMethodParameters(parameterAndProjectId, result);

            return result.ToImmutableAndFree();
        }

        private async Task CascadeBetweenAnonymousFunctionParametersAsync(
            Solution solution,
            SymbolAndProjectId<IParameterSymbol> parameterAndProjectId,
            ArrayBuilder<SymbolAndProjectId> results,
            CancellationToken cancellationToken)
        {
            var parameter = parameterAndProjectId.Symbol;
            if (parameter.ContainingSymbol.IsAnonymousFunction())
            {
                var parameterNode = parameter.DeclaringSyntaxReferences.Select(r => r.GetSyntax(cancellationToken)).FirstOrDefault();
                if (parameterNode != null)
                {
                    var document = solution.GetDocument(parameterNode.SyntaxTree);
                    if (document != null)
                    {
                        var semanticFacts = document.GetLanguageService<ISemanticFactsService>();
                        if (semanticFacts.ExposesAnonymousFunctionParameterNames)
                        {
                            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                            var lambdaNode = parameter.ContainingSymbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax(cancellationToken)).FirstOrDefault();
                            var convertedType = semanticModel.GetTypeInfo(lambdaNode, cancellationToken).ConvertedType;

                            if (convertedType != null)
                            {
                                var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
                                var container = GetContainer(semanticModel, parameterNode, syntaxFactsService);
                                if (container != null)
                                {
                                    CascadeBetweenAnonymousFunctionParameters(
                                        document, semanticModel, container, parameterAndProjectId,
                                        convertedType, results, cancellationToken);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void CascadeBetweenAnonymousFunctionParameters(
            Document document,
            SemanticModel semanticModel,
            SyntaxNode container,
            SymbolAndProjectId<IParameterSymbol> parameterAndProjectId,
            ITypeSymbol convertedType1,
            ArrayBuilder<SymbolAndProjectId> results,
            CancellationToken cancellationToken)
        {
            var parameter = parameterAndProjectId.Symbol;
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            foreach (var token in container.DescendantTokens())
            {
                if (IdentifiersMatch(syntaxFacts, parameter.Name, token))
                {
                    var symbol = semanticModel.GetDeclaredSymbol(token.Parent, cancellationToken);
                    if (symbol is IParameterSymbol &&
                        symbol.ContainingSymbol.IsAnonymousFunction() &&
                        SignatureComparer.Instance.HaveSameSignatureAndConstraintsAndReturnTypeAndAccessors(parameter.ContainingSymbol, symbol.ContainingSymbol, syntaxFacts.IsCaseSensitive) &&
                        ParameterNamesMatch(syntaxFacts, (IMethodSymbol)parameter.ContainingSymbol, (IMethodSymbol)symbol.ContainingSymbol))
                    {
                        var lambdaNode = symbol.ContainingSymbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax(cancellationToken)).FirstOrDefault();
                        var convertedType2 = semanticModel.GetTypeInfo(lambdaNode, cancellationToken).ConvertedType;

                        if (convertedType1.Equals(convertedType2))
                        {
                            results.Add(parameterAndProjectId.WithSymbol(symbol));
                        }
                    }
                }
            }
        }

        private bool ParameterNamesMatch(ISyntaxFactsService syntaxFacts, IMethodSymbol methodSymbol1, IMethodSymbol methodSymbol2)
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

        private SyntaxNode GetContainer(SemanticModel semanticModel, SyntaxNode parameterNode, ISyntaxFactsService syntaxFactsService)
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

        private void CascadeBetweenPropertyAndAccessorParameters(
            Solution solution,
            SymbolAndProjectId<IParameterSymbol> parameterAndProjectId,
            ArrayBuilder<SymbolAndProjectId> results)
        {
            var parameter = parameterAndProjectId.Symbol;
            var ordinal = parameter.Ordinal;
            var containingSymbol = parameter.ContainingSymbol;
            if (containingSymbol is IMethodSymbol containingMethod)
            {
                if (containingMethod.AssociatedSymbol is IPropertySymbol property)
                {
                    AddParameterAtIndex(
                        parameterAndProjectId, results,
                        ordinal, property.Parameters);
                }
            }
            else if (containingSymbol is IPropertySymbol containingProperty)
            {
                if (containingProperty.GetMethod != null && ordinal < containingProperty.GetMethod.Parameters.Length)
                {
                    results.Add(parameterAndProjectId.WithSymbol(containingProperty.GetMethod.Parameters[ordinal]));
                }

                if (containingProperty.SetMethod != null && ordinal < containingProperty.SetMethod.Parameters.Length)
                {
                    results.Add(parameterAndProjectId.WithSymbol(containingProperty.SetMethod.Parameters[ordinal]));
                }
            }
        }

        private void CascadeBetweenDelegateMethodParameters(
            Solution solution,
            SymbolAndProjectId<IParameterSymbol> parameterAndProjectId,
            ArrayBuilder<SymbolAndProjectId> results)
        {
            var parameter = parameterAndProjectId.Symbol;
            var ordinal = parameter.Ordinal;
            if (parameter.ContainingSymbol is IMethodSymbol containingMethod)
            {
                var containingType = containingMethod.ContainingType as INamedTypeSymbol;
                if (containingType.IsDelegateType())
                {
                    if (containingMethod.MethodKind == MethodKind.DelegateInvoke)
                    {
                        // cascade to the corresponding parameter in the BeginInvoke method.
                        var beginInvokeMethod = containingType.GetMembers(WellKnownMemberNames.DelegateBeginInvokeName)
                                                              .OfType<IMethodSymbol>()
                                                              .FirstOrDefault();
                        AddParameterAtIndex(
                            parameterAndProjectId, results,
                            ordinal, beginInvokeMethod?.Parameters);
                    }
                    else if (containingMethod.ContainingType.IsDelegateType() &&
                             containingMethod.Name == WellKnownMemberNames.DelegateBeginInvokeName)
                    {
                        // cascade to the corresponding parameter in the Invoke method.
                        AddParameterAtIndex(
                            parameterAndProjectId, results,
                            ordinal, containingType.DelegateInvokeMethod?.Parameters);
                    }
                }
            }
        }

        private static void AddParameterAtIndex(
            SymbolAndProjectId<IParameterSymbol> parameterAndProjectId,
            ArrayBuilder<SymbolAndProjectId> results,
            int ordinal,
            ImmutableArray<IParameterSymbol>? parameters)
        {
            if (parameters != null && ordinal < parameters.Value.Length)
            {
                results.Add(parameterAndProjectId.WithSymbol(parameters.Value[ordinal]));
            }
        }

        private void CascadeBetweenPartialMethodParameters(
            SymbolAndProjectId<IParameterSymbol> parameterAndProjectId,
            ArrayBuilder<SymbolAndProjectId> results)
        {
            var parameter = parameterAndProjectId.Symbol;
            if (parameter.ContainingSymbol is IMethodSymbol)
            {
                var ordinal = parameter.Ordinal;
                var method = (IMethodSymbol)parameter.ContainingSymbol;
                if (method.PartialDefinitionPart != null && ordinal < method.PartialDefinitionPart.Parameters.Length)
                {
                    results.Add(
                        parameterAndProjectId.WithSymbol(method.PartialDefinitionPart.Parameters[ordinal]));
                }

                if (method.PartialImplementationPart != null && ordinal < method.PartialImplementationPart.Parameters.Length)
                {
                    results.Add(
                        parameterAndProjectId.WithSymbol(method.PartialImplementationPart.Parameters[ordinal]));
                }
            }
        }
    }
}
