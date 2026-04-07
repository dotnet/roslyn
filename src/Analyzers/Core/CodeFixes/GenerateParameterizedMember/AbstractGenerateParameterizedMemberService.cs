// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember;

internal abstract partial class AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax> :
    AbstractGenerateMemberService<TSimpleNameSyntax, TExpressionSyntax>
    where TService : AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
    where TSimpleNameSyntax : TExpressionSyntax
    where TExpressionSyntax : SyntaxNode
    where TInvocationExpressionSyntax : TExpressionSyntax
{
    protected abstract AbstractInvocationInfo CreateInvocationMethodInfo(SemanticDocument document, State abstractState);

    protected abstract bool IsValidSymbol(ISymbol symbol, SemanticModel semanticModel);
    protected abstract bool AreSpecialOptionsActive(SemanticModel semanticModel);

    protected virtual bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType)
        => false;

    protected virtual string GetImplicitConversionDisplayText(State state)
        => string.Empty;

    protected virtual string GetExplicitConversionDisplayText(State state)
        => string.Empty;

    protected async ValueTask<ImmutableArray<CodeAction>> GetActionsAsync(Document document, State state, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<CodeAction>.GetInstance(out var result);
        result.Add(new GenerateParameterizedMemberCodeAction((TService)this, document, state, isAbstract: false, generateProperty: false));

        // If we're trying to generate an instance method into an abstract class (but not a
        // static class or an interface), then offer to generate it abstractly.
        var canGenerateAbstractly = state.TypeToGenerateIn.IsAbstract &&
            !state.TypeToGenerateIn.IsStatic &&
            state.TypeToGenerateIn.TypeKind != TypeKind.Interface &&
            !state.IsStatic;

        if (canGenerateAbstractly)
            result.Add(new GenerateParameterizedMemberCodeAction((TService)this, document, state, isAbstract: true, generateProperty: false));

        var semanticFacts = document.Project.Solution.GetRequiredLanguageService<ISemanticFactsService>(state.TypeToGenerateIn.Language);

        if (semanticFacts.SupportsParameterizedProperties &&
            state.InvocationExpressionOpt != null &&
            // Generate Method has the Razor-specific hidden/source-generated override; properties should still
            // only be offered when the destination passes the normal code-generation checks.
            CodeGenerator.CanAdd(document.Project.Solution, state.TypeToGenerateIn, cancellationToken))
        {
            var typeParameters = state.SignatureInfo.DetermineTypeParameters(cancellationToken);
            var returnType = await state.SignatureInfo.DetermineReturnTypeAsync(cancellationToken).ConfigureAwait(false);

            if (typeParameters.Length == 0 && returnType.SpecialType != SpecialType.System_Void)
            {
                result.Add(new GenerateParameterizedMemberCodeAction((TService)this, document, state, isAbstract: false, generateProperty: true));

                if (canGenerateAbstractly)
                    result.Add(new GenerateParameterizedMemberCodeAction((TService)this, document, state, isAbstract: true, generateProperty: true));
            }
        }

        return result.ToImmutableAndClear();
    }

    /// <summary>
    /// Checks if a document comes from the Razor source generator
    /// </summary>
    /// <returns>SourceGeneratedDocument.Identity is not available in the code style layer, so we can't use the existing extension method</returns>
    private static bool IsRazorSourceGeneratedDocument(Document document)
        => document is SourceGeneratedDocument &&
           document.FilePath is string filePath &&
           filePath.IndexOf("Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator", StringComparison.Ordinal) >= 0;
}
