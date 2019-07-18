// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
{
    internal partial class AbstractGenerateDeconstructMethodService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
    {
        internal new class State :
            AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>.State
        {
            /// <summary>
            /// Make a State instance representing the Deconstruct method we want to generate.
            /// The method will be called "Deconstruct". It will be a member of `typeToGenerateIn`.
            /// Its arguments will be based on `targetVariables`.
            /// </summary>
            public static async Task<State> GenerateDeconstructMethodStateAsync(
                TService service,
                SemanticDocument document,
                SyntaxNode targetVariables,
                INamedTypeSymbol typeToGenerateIn,
                CancellationToken cancellationToken)
            {
                var state = new State();
                if (!await state.TryInitializeMethodAsync(service, document, targetVariables, typeToGenerateIn, cancellationToken).ConfigureAwait(false))
                {
                    return null;
                }

                return state;
            }

            private async Task<bool> TryInitializeMethodAsync(
                TService service,
                SemanticDocument document,
                SyntaxNode targetVariables,
                INamedTypeSymbol typeToGenerateIn,
                CancellationToken cancellationToken)
            {
                TypeToGenerateIn = typeToGenerateIn;
                IsStatic = false;
                var generator = SyntaxGenerator.GetGenerator(document.Document);
                IdentifierToken = generator.Identifier(WellKnownMemberNames.DeconstructMethodName);
                MethodGenerationKind = MethodGenerationKind.Member;
                MethodKind = MethodKind.Ordinary;

                cancellationToken.ThrowIfCancellationRequested();

                var semanticModel = document.SemanticModel;
                ContainingType = semanticModel.GetEnclosingNamedType(targetVariables.SpanStart, cancellationToken);
                if (ContainingType == null)
                {
                    return false;
                }

                var parameters = TryMakeParameters(semanticModel, targetVariables);
                if (parameters.IsDefault)
                {
                    return false;
                }

                var methodSymbol = CodeGenerationSymbolFactory.CreateMethodSymbol(
                    attributes: default,
                    accessibility: default,
                    modifiers: default,
                    returnType: semanticModel.Compilation.GetSpecialType(SpecialType.System_Void),
                    refKind: RefKind.None,
                    explicitInterfaceImplementations: default,
                    name: null,
                    typeParameters: default,
                    parameters);

                SignatureInfo = new MethodSignatureInfo(document, this, methodSymbol);

                return await TryFinishInitializingStateAsync(service, document, cancellationToken).ConfigureAwait(false);
            }

            private static ImmutableArray<IParameterSymbol> TryMakeParameters(SemanticModel semanticModel, SyntaxNode target)
            {
                var targetType = semanticModel.GetTypeInfo(target).Type;
                if (targetType?.IsTupleType != true)
                {
                    return default;
                }

                var tupleElements = ((INamedTypeSymbol)targetType).TupleElements;
                var builder = ArrayBuilder<IParameterSymbol>.GetInstance(tupleElements.Length);
                foreach (var element in tupleElements)
                {
                    builder.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                        attributes: default, RefKind.Out, isParams: false, element.Type, element.Name));
                }

                return builder.ToImmutableAndFree();
            }
        }
    }
}
