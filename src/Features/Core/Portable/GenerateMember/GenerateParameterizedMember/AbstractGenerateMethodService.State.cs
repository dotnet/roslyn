// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember;

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

            var syntaxFacts = semanticDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>();
            if (syntaxFacts.IsLeftSideOfAnyAssignment(simpleNameOrMemberAccessExpression))
                return false;

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
                var delegateInvokeMethod = typeInference.InferDelegateType(semanticModel, SimpleNameOrMemberAccessExpression, cancellationToken)?.DelegateInvokeMethod;
                if (delegateInvokeMethod != null)
                {
                    // If we inferred Func/Action here, attempt to create better parameter names than the default
                    // 'arg1/arg2/arg3' form that the delegate specifies.
                    var parameterNames = delegateInvokeMethod.ContainingType is { Name: nameof(Action) or nameof(Func<int>), ContainingNamespace.Name: nameof(System) }
                        ? GenerateParameterNamesBasedOnParameterTypes(delegateInvokeMethod.Parameters)
                        : delegateInvokeMethod.Parameters.SelectAsArray(p => p.Name);

                    SignatureInfo = new MethodSignatureInfo(semanticDocument, this, delegateInvokeMethod, parameterNames);
                }
                else
                {
                    // We don't have and invocation expression or a delegate, but we may have a special expression without parenthesis.  Lets see
                    // if the type inference service can directly infer the type for our expression.
                    var expressionType = service.DetermineReturnTypeForSimpleNameOrMemberAccessExpression(typeInference, semanticModel, SimpleNameOrMemberAccessExpression, cancellationToken);
                    if (expressionType == null)
                        return false;

                    SignatureInfo = new MethodSignatureInfo(semanticDocument, this, CreateMethodSymbolWithReturnType(expressionType));
                }
            }

            // Now, try to bind the invocation and see if it succeeds or not.  if it succeeds and
            // binds uniquely, then we don't need to offer this quick fix.
            cancellationToken.ThrowIfCancellationRequested();

            // If the name bound with errors, then this is a candidate for generate method.
            var semanticInfo = semanticModel.GetSymbolInfo(SimpleNameOrMemberAccessExpression, cancellationToken);
            if (semanticInfo.GetAllSymbols().Any(static s => s.Kind is SymbolKind.Local or SymbolKind.Parameter) &&
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
            if (!TryDetermineTypeToGenerateIn(
                    semanticDocument, ContainingType, SimpleNameOrMemberAccessExpression, cancellationToken,
                    out var typeToGenerateIn, out var isStatic, out _))
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

        private static ImmutableArray<string> GenerateParameterNamesBasedOnParameterTypes(ImmutableArray<IParameterSymbol> parameters)
        {
            using var _1 = ArrayBuilder<string>.GetInstance(out var names);
            using var _2 = ArrayBuilder<bool>.GetInstance(out var isFixed);

            foreach (var parameter in parameters)
            {
                var typeLocalName = parameter.Type.GetLocalName(fallback: parameter.Name);
                names.Add(new ParameterName(typeLocalName, isFixed: false).BestNameForParameter);
                isFixed.Add(false);
            }

            NameGenerator.EnsureUniquenessInPlace(names, isFixed);
            return names.ToImmutableAndClear();
        }

        private static IMethodSymbol CreateMethodSymbolWithReturnType(
            ITypeSymbol expressionType)
        {
            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes: [],
                accessibility: default,
                modifiers: default,
                returnType: expressionType,
                refKind: RefKind.None,
                explicitInterfaceImplementations: default,
                name: null,
                typeParameters: [],
                parameters: []);
        }
    }
}
