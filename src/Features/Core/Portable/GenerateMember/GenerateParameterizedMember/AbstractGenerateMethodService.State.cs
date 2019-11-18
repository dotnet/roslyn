// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
{
    internal partial class AbstractGenerateMethodService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
    {
        internal new class State : AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>.State
        {
            public static async Task<State> GenerateMethodStateAsync(
                TService service,
                SemanticDocument document,
                SyntaxNode interfaceNode,
                CancellationToken cancellationToken)
            {
                var state = new State();
                if (!await state.TryInitializeMethodAsync(service, document, interfaceNode, cancellationToken).ConfigureAwait(false))
                {
                    return null;
                }

                return state;
            }

            private Task<bool> TryInitializeMethodAsync(
                TService service,
                SemanticDocument document,
                SyntaxNode node,
                CancellationToken cancellationToken)
            {
                // Cases that we deal with currently:
                //
                // 1) expr.Goo
                // 2) expr->Goo
                // 3) Goo
                // 4) expr.Goo()
                // 5) expr->Goo()
                // 6) Goo()
                // 7) ReturnType Explicit.Interface.Goo()
                //
                // In the first 3 invocationExpressionOpt will be null and we'll have to infer a
                // delegate type in order to figure out the right method signature to generate. In
                // the next 3 invocationExpressionOpt will be non null and will be used to figure
                // out the types/name of the parameters to generate. In the last one, we're going to
                // generate into an interface.
                if (service.IsExplicitInterfaceGeneration(node))
                {
                    if (!TryInitializeExplicitInterface(service, document, node, cancellationToken))
                    {
                        return SpecializedTasks.False;
                    }
                }
                else if (service.IsSimpleNameGeneration(node))
                {
                    if (!TryInitializeSimpleName(service, document, (TSimpleNameSyntax)node, cancellationToken))
                    {
                        return SpecializedTasks.False;
                    }
                }

                return TryFinishInitializingStateAsync(service, document, cancellationToken);
            }

            private bool TryInitializeExplicitInterface(
                TService service,
                SemanticDocument document,
                SyntaxNode methodDeclaration,
                CancellationToken cancellationToken)
            {
                MethodKind = MethodKind.Ordinary;
                if (!service.TryInitializeExplicitInterfaceState(
                    document, methodDeclaration, cancellationToken,
                    out var identifierToken, out var methodSymbol, out var typeToGenerateIn))
                {
                    return false;
                }

                if (methodSymbol.ExplicitInterfaceImplementations.Any())
                {
                    return false;
                }

                IdentifierToken = identifierToken;
                TypeToGenerateIn = typeToGenerateIn;

                cancellationToken.ThrowIfCancellationRequested();
                var semanticModel = document.SemanticModel;
                ContainingType = semanticModel.GetEnclosingNamedType(methodDeclaration.SpanStart, cancellationToken);
                if (ContainingType == null)
                {
                    return false;
                }

                if (!ContainingType.Interfaces.Contains(TypeToGenerateIn))
                {
                    return false;
                }

                SignatureInfo = new MethodSignatureInfo(document, this, methodSymbol);
                return true;
            }

            private bool TryInitializeSimpleName(
                TService service,
                SemanticDocument semanticDocument,
                TSimpleNameSyntax simpleName,
                CancellationToken cancellationToken)
            {
                MethodKind = MethodKind.Ordinary;
                SimpleNameOpt = simpleName;
                if (!service.TryInitializeSimpleNameState(
                        semanticDocument, simpleName, cancellationToken,
                        out var identifierToken, out var simpleNameOrMemberAccessExpression,
                        out var invocationExpressionOpt, out var isInConditionalExpression))
                {
                    return false;
                }

                IdentifierToken = identifierToken;
                SimpleNameOrMemberAccessExpression = simpleNameOrMemberAccessExpression;
                InvocationExpressionOpt = invocationExpressionOpt;
                IsInConditionalAccessExpression = isInConditionalExpression;

                if (string.IsNullOrWhiteSpace(IdentifierToken.ValueText))
                {
                    return false;
                }

                // If we're not in a type, don't even bother.  NOTE(cyrusn): We'll have to rethink this
                // for C# Script.
                cancellationToken.ThrowIfCancellationRequested();
                var semanticModel = semanticDocument.SemanticModel;
                ContainingType = semanticModel.GetEnclosingNamedType(SimpleNameOpt.SpanStart, cancellationToken);
                if (ContainingType == null)
                {
                    return false;
                }

                if (InvocationExpressionOpt != null)
                {
                    SignatureInfo = service.CreateInvocationMethodInfo(semanticDocument, this);
                }
                else
                {
                    var typeInference = semanticDocument.Document.GetLanguageService<ITypeInferenceService>();
                    var delegateType = typeInference.InferDelegateType(semanticModel, SimpleNameOrMemberAccessExpression, cancellationToken);
                    if (delegateType is { DelegateInvokeMethod: { } })
                    {
                        SignatureInfo = new MethodSignatureInfo(semanticDocument, this, delegateType.DelegateInvokeMethod);
                    }
                    else
                    {
                        // We don't have and invocation expression or a delegate, but we may have a special expression without parenthesis.  Lets see
                        // if the type inference service can directly infer the type for our expression.
                        var expressionType = service.DetermineReturnTypeForSimpleNameOrMemberAccessExpression(typeInference, semanticModel, SimpleNameOrMemberAccessExpression, cancellationToken);
                        if (expressionType == null)
                        {
                            return false;
                        }

                        SignatureInfo = new MethodSignatureInfo(semanticDocument, this, CreateMethodSymbolWithReturnType(expressionType));
                    }
                }

                // Now, try to bind the invocation and see if it succeeds or not.  if it succeeds and
                // binds uniquely, then we don't need to offer this quick fix.
                cancellationToken.ThrowIfCancellationRequested();

                // If the name bound with errors, then this is a candidate for generate method.
                var semanticInfo = semanticModel.GetSymbolInfo(SimpleNameOrMemberAccessExpression, cancellationToken);
                if (semanticInfo.GetAllSymbols().Any(s => s.Kind == SymbolKind.Local || s.Kind == SymbolKind.Parameter) &&
                    !service.AreSpecialOptionsActive(semanticModel))
                {
                    // if the name bound to something in scope then we don't want to generate the
                    // method because it will be shadowed by what's in scope. Unless we are in a 
                    // special state such as Option Strict On where we want to generate fixes even
                    // if we shadow types.
                    return false;
                }

                // Check if the symbol is on the list of valid symbols for this language. 
                cancellationToken.ThrowIfCancellationRequested();
                if (semanticInfo.Symbol != null && !service.IsValidSymbol(semanticInfo.Symbol, semanticModel))
                {
                    return false;
                }

                // Either we found no matches, or this was ambiguous. Either way, we might be able
                // to generate a method here.  Determine where the user wants to generate the method
                // into, and if it's valid then proceed.
                cancellationToken.ThrowIfCancellationRequested();
                if (!service.TryDetermineTypeToGenerateIn(
                        semanticDocument, ContainingType, SimpleNameOrMemberAccessExpression, cancellationToken,
                        out var typeToGenerateIn, out var isStatic))
                {
                    return false;
                }

                var semanticFacts = semanticDocument.Document.GetLanguageService<ISemanticFactsService>();
                IsWrittenTo = semanticFacts.IsWrittenTo(semanticModel, InvocationExpressionOpt ?? SimpleNameOrMemberAccessExpression, cancellationToken);
                TypeToGenerateIn = typeToGenerateIn;
                IsStatic = isStatic;
                MethodGenerationKind = MethodGenerationKind.Member;
                return true;
            }

            private static IMethodSymbol CreateMethodSymbolWithReturnType(
                ITypeSymbol expressionType)
            {
                return CodeGenerationSymbolFactory.CreateMethodSymbol(
                    attributes: ImmutableArray<AttributeData>.Empty,
                    accessibility: default,
                    modifiers: default,
                    returnType: expressionType,
                    refKind: RefKind.None,
                    explicitInterfaceImplementations: default,
                    name: null,
                    typeParameters: ImmutableArray<ITypeParameterSymbol>.Empty,
                    parameters: ImmutableArray<IParameterSymbol>.Empty);
            }
        }
    }
}
