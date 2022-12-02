// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

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

                var semanticFacts = document.Document.GetRequiredLanguageService<ISemanticFactsService>();

                cancellationToken.ThrowIfCancellationRequested();

                var semanticModel = document.SemanticModel;
                ContainingType = semanticModel.GetEnclosingNamedType(targetVariables.SpanStart, cancellationToken);
                if (ContainingType == null)
                {
                    return false;
                }

                var parameters = TryMakeParameters(semanticModel, targetVariables, semanticFacts, cancellationToken);
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

            private static ImmutableArray<IParameterSymbol> TryMakeParameters(SemanticModel semanticModel, SyntaxNode target, ISemanticFactsService semanticFacts, CancellationToken cancellationToken)
            {
                ITypeSymbol targetType;
                if (target is PositionalPatternClauseSyntax positionalPattern)
                {
                    var namesBuilder = ImmutableArray.CreateBuilder<string>();
                    using var _ = ArrayBuilder<IParameterSymbol>.GetInstance(positionalPattern.Subpatterns.Count, out var builder);
                    for (var i = 0; i < positionalPattern.Subpatterns.Count - 1; i++)
                    {
                        namesBuilder.Add(semanticFacts.GenerateNameForExpression(semanticModel, positionalPattern.Subpatterns[i], false, cancellationToken));
                    }
                    var names = NameGenerator.EnsureUniqueness(namesBuilder.ToImmutable());
                    for (var i = 0; i < positionalPattern.Subpatterns.Count - 1; i++)
                    {
                        targetType = semanticModel.GetTypeInfo(positionalPattern.Subpatterns[i].Pattern, cancellationToken: cancellationToken).Type;
                        builder.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                        RefKind.Out, targetType, names[i]));
                    }

                    return builder.ToImmutableAndClear();
                }
                else
                {
                    targetType = semanticModel.GetTypeInfo(target, cancellationToken: cancellationToken).Type;
                    if (targetType?.IsTupleType != true)
                    {
                        return default;
                    }

                    var tupleElements = ((INamedTypeSymbol)targetType).TupleElements;
                    using var _ = ArrayBuilder<IParameterSymbol>.GetInstance(tupleElements.Length, out var builder);
                    foreach (var element in tupleElements)
                    {
                        builder.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                            attributes: default, RefKind.Out, isParams: false, element.Type, element.Name));
                    }

                    return builder.ToImmutableAndClear();
                }
            }
        }
    }
}
