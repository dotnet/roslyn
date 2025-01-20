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
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.GenerateMember.GenerateMethod;

[ExportLanguageService(typeof(IGenerateDeconstructMemberService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpGenerateDeconstructMethodService() :
    AbstractGenerateDeconstructMethodService<CSharpGenerateDeconstructMethodService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax>
{
    protected override bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType)
        => containingType.ContainingTypesOrSelfHasUnsafeKeyword();

    protected override AbstractInvocationInfo CreateInvocationMethodInfo(SemanticDocument document, AbstractGenerateParameterizedMemberService<CSharpGenerateDeconstructMethodService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax>.State state)
        => new CSharpGenerateParameterizedMemberService<CSharpGenerateDeconstructMethodService>.InvocationExpressionInfo(document, state);

    protected override bool AreSpecialOptionsActive(SemanticModel semanticModel)
        => CSharpCommonGenerationServiceMethods.AreSpecialOptionsActive();

    protected override bool IsValidSymbol(ISymbol symbol, SemanticModel semanticModel)
        => CSharpCommonGenerationServiceMethods.IsValidSymbol();

    public override ImmutableArray<IParameterSymbol> TryMakeParameters(SemanticModel semanticModel, SyntaxNode target, CancellationToken cancellationToken)
    {
        // For `if (this is C(0, 0))`, we 'll generate `Deconstruct(out int v1, out int v2)`
        if (target is PositionalPatternClauseSyntax positionalPattern)
        {
            // Code in GenerateDeconstructMethodCodeFixProvider has already checked that all subpatterns are ConstantPatternSyntax.
            var namesBuilder = positionalPattern.Subpatterns.SelectAsArray(sub =>
                semanticModel.GenerateNameForExpression(((ConstantPatternSyntax)sub.Pattern).Expression, capitalize: false, cancellationToken));

            var names = NameGenerator.EnsureUniqueness(namesBuilder);

            return names.SelectAsArray((name, i) => CodeGenerationSymbolFactory.CreateParameterSymbol(
                attributes: default,
                refKind: RefKind.Out,
                isParams: false,
                type: semanticModel.GetTypeInfo(((ConstantPatternSyntax)positionalPattern.Subpatterns[i].Pattern).Expression, cancellationToken).Type ?? semanticModel.Compilation.GetSpecialType(SpecialType.System_Object),
                name: name));
        }
        else
        {
            var targetType = semanticModel.GetTypeInfo(target, cancellationToken: cancellationToken).Type;
            if (targetType is not INamedTypeSymbol { IsTupleType: true, TupleElements: var tupleElements })
                return default;

            return tupleElements.SelectAsArray(element => CodeGenerationSymbolFactory.CreateParameterSymbol(
                    attributes: default, RefKind.Out, isParams: false, element.Type, element.Name));
        }
    }
}
