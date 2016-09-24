// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

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
            CancellationToken cancellationToken)
        {
            // TODO(cyrusn): We can be smarter with parameters.  They will either be found
            // within the method that they were declared on, or they will referenced
            // elsewhere as "paramName:" or "paramName:=".  We can narrow the search by
            // filtering down to matches of that form.  For now we just return any document
            // that references something with this name.
            return FindDocumentsAsync(project, documents, cancellationToken, symbol.Name);
        }

        protected override Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentAsync(
            IParameterSymbol symbol,
            Document document,
            CancellationToken cancellationToken)
        {
            return FindReferencesInDocumentUsingSymbolNameAsync(symbol, document, cancellationToken);
        }

        protected override async Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId<IParameterSymbol> parameterAndProjectId,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            var parameter = parameterAndProjectId.Symbol;
            if (parameter.IsThis)
            {
                return ImmutableArray<SymbolAndProjectId>.Empty;
            }

            var anonymousFunctionParameterCascades = await CascadeBetweenAnonymousFunctionParametersAsync(
                solution, parameterAndProjectId, cancellationToken).ConfigureAwait(false);

            var propertyOrEventOrAccessorParameterCascades = await CascadeBetweenPropertyOrEventAndAccessorParameterAsync(
                solution, parameterAndProjectId, cancellationToken).ConfigureAwait(false);

            return anonymousFunctionParameterCascades.Concat(
                propertyOrEventOrAccessorParameterCascades).Concat(
                CascadeBetweenPartialMethodParameters(parameterAndProjectId));
        }

        private async Task<ImmutableArray<SymbolAndProjectId>> CascadeBetweenAnonymousFunctionParametersAsync(
            Solution solution,
            SymbolAndProjectId<IParameterSymbol> parameterAndProjectId,
            CancellationToken cancellationToken)
        {
            var results = ArrayBuilder<SymbolAndProjectId>.GetInstance();

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
                                    results.AddRange(CascadeBetweenAnonymousFunctionParameters(
                                        document, semanticModel, container, parameterAndProjectId, convertedType, cancellationToken));
                                }
                            }
                        }
                    }
                }

                var containingMethod = (IMethodSymbol)parameter.ContainingSymbol;
                if (containingMethod.AssociatedAnonymousDelegate != null)
                {
                    var invokeMethod = containingMethod.AssociatedAnonymousDelegate.DelegateInvokeMethod;
                    int ordinal = parameter.Ordinal;
                    if (invokeMethod != null && ordinal < invokeMethod.Parameters.Length)
                    {
                        results.Add(parameterAndProjectId.WithSymbol(
                            invokeMethod.Parameters[ordinal]));
                    }
                }
            }

            return results.ToImmutableAndFree();
        }

        private ImmutableArray<SymbolAndProjectId> CascadeBetweenAnonymousFunctionParameters(
            Document document,
            SemanticModel semanticModel,
            SyntaxNode container,
            SymbolAndProjectId<IParameterSymbol> parameterAndProjectId,
            ITypeSymbol convertedType1,
            CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<SymbolAndProjectId>.GetInstance();

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
                            result.Add(parameterAndProjectId.WithSymbol(symbol));
                        }
                    }
                }
            }

            return result.ToImmutableAndFree();
        }

        private bool ParameterNamesMatch(ISyntaxFactsService syntaxFacts, IMethodSymbol methodSymbol1, IMethodSymbol methodSymbol2)
        {
            for (int i = 0; i < methodSymbol1.Parameters.Length; i++)
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

                if (declaredSymbol is IMethodSymbol && ((IMethodSymbol)declaredSymbol).MethodKind != MethodKind.AnonymousFunction)
                {
                    return current;
                }
            }

            return syntaxFactsService.GetContainingVariableDeclaratorOfFieldDeclaration(parameterNode);
        }

        private async Task<ImmutableArray<SymbolAndProjectId>> CascadeBetweenPropertyOrEventAndAccessorParameterAsync(
            Solution solution,
            SymbolAndProjectId<IParameterSymbol> parameterAndProjectId,
            CancellationToken cancellationToken)
        {
            var results = ArrayBuilder<SymbolAndProjectId>.GetInstance();

            var parameter = parameterAndProjectId.Symbol;
            var ordinal = parameter.Ordinal;
            var containingSymbol = parameter.ContainingSymbol;
            if (containingSymbol is IMethodSymbol)
            {
                var containingMethod = (IMethodSymbol)containingSymbol;
                if (containingMethod.AssociatedSymbol is IPropertySymbol)
                {
                    var property = (IPropertySymbol)containingMethod.AssociatedSymbol;
                    if (ordinal < property.Parameters.Length)
                    {
                        results.Add(parameterAndProjectId.WithSymbol(property.Parameters[ordinal]));
                    }
                }
                else
                {
                    var namedType = containingMethod.ContainingType as INamedTypeSymbol;
                    if (namedType != null && namedType.IsDelegateType() && namedType.AssociatedSymbol != null)
                    {
                        var eventNode = namedType.AssociatedSymbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax(cancellationToken)).FirstOrDefault();
                        if (eventNode != null)
                        {
                            var document = solution.GetDocument(eventNode.SyntaxTree);
                            if (document != null)
                            {
                                var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
                                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                                foreach (var token in eventNode.DescendantTokens())
                                {
                                    if (IdentifiersMatch(syntaxFacts, parameter.Name, token))
                                    {
                                        var eventParam = semanticModel.GetDeclaredSymbol(token.Parent, cancellationToken) as IParameterSymbol;
                                        if (eventParam != null && eventParam.Type != null && eventParam.Type.Equals(parameter.Type))
                                        {
                                            results.Add(parameterAndProjectId.WithSymbol(eventParam));
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (containingSymbol is IPropertySymbol)
            {
                var containingProperty = (IPropertySymbol)containingSymbol;
                if (containingProperty.GetMethod != null && ordinal < containingProperty.GetMethod.Parameters.Length)
                {
                    results.Add(parameterAndProjectId.WithSymbol(containingProperty.GetMethod.Parameters[ordinal]));
                }

                if (containingProperty.SetMethod != null && ordinal < containingProperty.SetMethod.Parameters.Length)
                {
                    results.Add(parameterAndProjectId.WithSymbol(containingProperty.SetMethod.Parameters[ordinal]));
                }
            }
            else if (containingSymbol is IEventSymbol)
            {
                var containingEvent = (IEventSymbol)containingSymbol;
                var namedType = containingEvent.Type as INamedTypeSymbol;
                if (namedType != null && namedType.IsDelegateType())
                {
                    foreach (var member in namedType.GetMembers())
                    {
                        if (member.Kind == SymbolKind.Method)
                        {
                            foreach (var memberParam in member.GetParameters())
                            {
                                if (memberParam.Name.Equals(parameter.Name) && memberParam.Type != null && memberParam.Type.Equals(parameter.Type))
                                {
                                    results.Add(parameterAndProjectId.WithSymbol(memberParam));
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return results.ToImmutableAndFree();
        }

        private ImmutableArray<SymbolAndProjectId> CascadeBetweenPartialMethodParameters(
            SymbolAndProjectId<IParameterSymbol> parameterAndProjectId)
        {
            var result = ArrayBuilder<SymbolAndProjectId>.GetInstance();

            var parameter = parameterAndProjectId.Symbol;
            if (parameter.ContainingSymbol is IMethodSymbol)
            {
                var ordinal = parameter.Ordinal;
                var method = (IMethodSymbol)parameter.ContainingSymbol;
                if (method.PartialDefinitionPart != null && ordinal < method.PartialDefinitionPart.Parameters.Length)
                {
                    result.Add(
                        parameterAndProjectId.WithSymbol(method.PartialDefinitionPart.Parameters[ordinal]));
                }

                if (method.PartialImplementationPart != null && ordinal < method.PartialImplementationPart.Parameters.Length)
                {
                    result.Add(
                        parameterAndProjectId.WithSymbol(method.PartialImplementationPart.Parameters[ordinal]));
                }
            }

            return result.ToImmutableAndFree();
        }
    }
}