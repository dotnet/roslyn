// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.GenerateMember.GenerateParameterizedMember;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.GenerateMember.GenerateMethod
{
    [ExportLanguageService(typeof(IGenerateDeconstructMemberService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpGenerateDeconstructMethodService :
        AbstractGenerateDeconstructMethodService<CSharpGenerateDeconstructMethodService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpGenerateDeconstructMethodService()
        {
        }

        protected override bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType)
            => containingType.ContainingTypesOrSelfHasUnsafeKeyword();

        protected override AbstractInvocationInfo CreateInvocationMethodInfo(SemanticDocument document, AbstractGenerateParameterizedMemberService<CSharpGenerateDeconstructMethodService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax>.State state)
            => new CSharpGenerateParameterizedMemberService<CSharpGenerateDeconstructMethodService>.InvocationExpressionInfo(document, state);

        protected override bool AreSpecialOptionsActive(SemanticModel semanticModel)
            => CSharpCommonGenerationServiceMethods.AreSpecialOptionsActive();

        protected override bool IsValidSymbol(ISymbol symbol, SemanticModel semanticModel)
            => CSharpCommonGenerationServiceMethods.IsValidSymbol();

        public override ImmutableArray<IParameterSymbol> TryMakeParameters(SemanticModel semanticModel, SyntaxNode target, ISemanticFactsService semanticFacts, CancellationToken cancellationToken)
        {
            ITypeSymbol targetType;
            if (target is PositionalPatternClauseSyntax positionalPattern)
            {
                var namesBuilder = ImmutableArray.CreateBuilder<string>();
                using var _ = ArrayBuilder<IParameterSymbol>.GetInstance(positionalPattern.Subpatterns.Count, out var builder);
                for (var i = 0; i < positionalPattern.Subpatterns.Count; i++)
                {
                    namesBuilder.Add(semanticFacts.GenerateNameForExpression(semanticModel, ((ConstantPatternSyntax)positionalPattern.Subpatterns[i].Pattern).Expression, false, cancellationToken));
                }
                var names = NameGenerator.EnsureUniqueness(namesBuilder.ToImmutable());
                for (var i = 0; i < positionalPattern.Subpatterns.Count; i++)
                {
                    targetType = semanticModel.GetTypeInfo(((ConstantPatternSyntax)positionalPattern.Subpatterns[i].Pattern).Expression, cancellationToken: cancellationToken).Type;
                    builder.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                    attributes: default, RefKind.Out, isParams: false, targetType, names[i]));
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
