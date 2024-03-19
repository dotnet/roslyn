// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody;

/// <summary>
/// Helper class that allows us to share lots of logic between the diagnostic analyzer and the
/// code refactoring provider.  Those can't share a common base class due to their own inheritance
/// requirements with <see cref="DiagnosticAnalyzer"/> and "CodeRefactoringProvider".
/// </summary>
internal abstract class UseExpressionBodyHelper<TDeclaration> : UseExpressionBodyHelper
    where TDeclaration : SyntaxNode
{
    public override Option2<CodeStyleOption2<ExpressionBodyPreference>> Option { get; }
    public override LocalizableString UseExpressionBodyTitle { get; }
    public override LocalizableString UseBlockBodyTitle { get; }
    public override string DiagnosticId { get; }
    public override EnforceOnBuild EnforceOnBuild { get; }
    public override ImmutableArray<SyntaxKind> SyntaxKinds { get; }

    protected UseExpressionBodyHelper(
        string diagnosticId,
        EnforceOnBuild enforceOnBuild,
        LocalizableString useExpressionBodyTitle,
        LocalizableString useBlockBodyTitle,
        Option2<CodeStyleOption2<ExpressionBodyPreference>> option,
        ImmutableArray<SyntaxKind> syntaxKinds)
    {
        DiagnosticId = diagnosticId;
        EnforceOnBuild = enforceOnBuild;
        Option = option;
        UseExpressionBodyTitle = useExpressionBodyTitle;
        UseBlockBodyTitle = useBlockBodyTitle;
        SyntaxKinds = syntaxKinds;
    }

    protected static AccessorDeclarationSyntax? GetSingleGetAccessor(AccessorListSyntax? accessorList)
    {
        return accessorList is { Accessors: [{ AttributeLists.Count: 0, RawKind: (int)SyntaxKind.GetAccessorDeclaration } accessor] }
            ? accessor
            : null;
    }

    protected static BlockSyntax? GetBodyFromSingleGetAccessor(AccessorListSyntax accessorList)
        => GetSingleGetAccessor(accessorList)?.Body;

    public override BlockSyntax? GetBody(SyntaxNode declaration)
        => GetBody((TDeclaration)declaration);

    public override ArrowExpressionClauseSyntax? GetExpressionBody(SyntaxNode declaration)
        => GetExpressionBody((TDeclaration)declaration);

    public override bool IsRelevantDeclarationNode(SyntaxNode node)
        => node is TDeclaration;

    public override bool CanOfferUseExpressionBody(CodeStyleOption2<ExpressionBodyPreference> preference, SyntaxNode declaration, bool forAnalyzer, CancellationToken cancellationToken)
        => CanOfferUseExpressionBody(preference, (TDeclaration)declaration, forAnalyzer, cancellationToken);

    public override bool CanOfferUseBlockBody(CodeStyleOption2<ExpressionBodyPreference> preference, SyntaxNode declaration, bool forAnalyzer, out bool fixesError, [NotNullWhen(true)] out ArrowExpressionClauseSyntax? expressionBody)
        => CanOfferUseBlockBody(preference, (TDeclaration)declaration, forAnalyzer, out fixesError, out expressionBody);

    public sealed override SyntaxNode Update(SemanticModel semanticModel, SyntaxNode declaration, bool useExpressionBody, CancellationToken cancellationToken)
        => Update(semanticModel, (TDeclaration)declaration, useExpressionBody, cancellationToken);

    public override Location GetDiagnosticLocation(SyntaxNode declaration)
        => GetDiagnosticLocation((TDeclaration)declaration);

    protected virtual Location GetDiagnosticLocation(TDeclaration declaration)
    {
        var body = GetBody(declaration);
        Contract.ThrowIfNull(body);
        return body.Statements[0].GetLocation();
    }

    public bool CanOfferUseExpressionBody(
        CodeStyleOption2<ExpressionBodyPreference> preference, TDeclaration declaration, bool forAnalyzer, CancellationToken cancellationToken)
    {
        var userPrefersExpressionBodies = preference.Value != ExpressionBodyPreference.Never;
        var analyzerDisabled = preference.Notification.Severity == ReportDiagnostic.Suppress;

        // If the user likes expression bodies, then we offer expression bodies from the diagnostic analyzer.
        // If the user does not like expression bodies then we offer expression bodies from the refactoring provider.
        // If the analyzer is disabled completely, the refactoring is enabled in both directions.
        if (userPrefersExpressionBodies == forAnalyzer || (!forAnalyzer && analyzerDisabled))
        {
            var expressionBody = GetExpressionBody(declaration);
            if (expressionBody == null)
            {
                // They don't have an expression body.  See if we could convert the block they
                // have into one.

                var conversionPreference = forAnalyzer ? preference.Value : ExpressionBodyPreference.WhenPossible;

                return TryConvertToExpressionBody(declaration, conversionPreference, cancellationToken,
                    expressionWhenOnSingleLine: out _, semicolonWhenOnSingleLine: out _);
            }
        }

        return false;
    }

    protected virtual bool TryConvertToExpressionBody(
        TDeclaration declaration,
        ExpressionBodyPreference conversionPreference,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out ArrowExpressionClauseSyntax? expressionWhenOnSingleLine,
        out SyntaxToken semicolonWhenOnSingleLine)
    {
        return TryConvertToExpressionBodyWorker(
            declaration, conversionPreference, cancellationToken,
            out expressionWhenOnSingleLine, out semicolonWhenOnSingleLine);
    }

    private bool TryConvertToExpressionBodyWorker(
        SyntaxNode declaration, ExpressionBodyPreference conversionPreference, CancellationToken cancellationToken,
        [NotNullWhen(true)] out ArrowExpressionClauseSyntax? expressionWhenOnSingleLine, out SyntaxToken semicolonWhenOnSingleLine)
    {
        var body = GetBody(declaration);
        if (body is null)
        {
            expressionWhenOnSingleLine = null;
            semicolonWhenOnSingleLine = default;
            return false;
        }

        var languageVersion = body.SyntaxTree.Options.LanguageVersion();

        return body.TryConvertToArrowExpressionBody(
            declaration.Kind(), languageVersion, conversionPreference, cancellationToken,
            out expressionWhenOnSingleLine, out semicolonWhenOnSingleLine);
    }

    protected bool TryConvertToExpressionBodyForBaseProperty(
        BasePropertyDeclarationSyntax declaration,
        ExpressionBodyPreference conversionPreference,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out ArrowExpressionClauseSyntax? arrowExpression,
        out SyntaxToken semicolonToken)
    {
        if (TryConvertToExpressionBodyWorker(declaration, conversionPreference, cancellationToken, out arrowExpression, out semicolonToken))
        {
            return true;
        }

        var getAccessor = GetSingleGetAccessor(declaration.AccessorList);
        if (getAccessor?.ExpressionBody != null &&
            BlockSyntaxExtensions.MatchesPreference(getAccessor.ExpressionBody.Expression, conversionPreference))
        {
            arrowExpression = SyntaxFactory.ArrowExpressionClause(getAccessor.ExpressionBody.Expression);
            semicolonToken = getAccessor.SemicolonToken;
            return true;
        }

        return false;
    }

    public bool CanOfferUseBlockBody(
        CodeStyleOption2<ExpressionBodyPreference> preference,
        TDeclaration declaration,
        bool forAnalyzer,
        out bool fixesError,
        [NotNullWhen(true)] out ArrowExpressionClauseSyntax? expressionBody)
    {
        var userPrefersBlockBodies = preference.Value == ExpressionBodyPreference.Never;
        var analyzerDisabled = preference.Notification.Severity == ReportDiagnostic.Suppress;

        expressionBody = GetExpressionBody(declaration);
        if (expressionBody?.TryConvertToBlock(
            SyntaxFactory.Token(SyntaxKind.SemicolonToken), false, block: out _) != true)
        {
            fixesError = false;
            return false;
        }

        var languageVersion = declaration.GetLanguageVersion();
        if (languageVersion < LanguageVersion.CSharp7)
        {
            if (expressionBody!.Expression.IsKind(SyntaxKind.ThrowExpression))
            {
                // If they're using a throw expression in a declaration and it's prior to C# 7
                // then always mark this as something that can be fixed by the analyzer.  This way
                // we'll also get 'fix all' working to fix all these cases.
                fixesError = true;
                return true;
            }

            if (declaration is AccessorDeclarationSyntax or ConstructorDeclarationSyntax)
            {
                // If they're using expression bodies for accessors/constructors and it's prior to C# 7
                // then always mark this as something that can be fixed by the analyzer.  This way
                // we'll also get 'fix all' working to fix all these cases.
                fixesError = true;
                return true;
            }
        }

        if (languageVersion < LanguageVersion.CSharp6)
        {
            // If they're using expression bodies prior to C# 6, then always mark this as something
            // that can be fixed by the analyzer.  This way we'll also get 'fix all' working to fix
            // all these cases.
            fixesError = true;
            return true;
        }

        // If the user likes block bodies, then we offer block bodies from the diagnostic analyzer.
        // If the user does not like block bodies then we offer block bodies from the refactoring provider.
        // If the analyzer is disabled completely, the refactoring is enabled in both directions.
        fixesError = false;
        return userPrefersBlockBodies == forAnalyzer || (!forAnalyzer && analyzerDisabled);
    }

    public TDeclaration Update(SemanticModel semanticModel, TDeclaration declaration, bool useExpressionBody, CancellationToken cancellationToken)
    {
        if (useExpressionBody)
        {
            TryConvertToExpressionBody(declaration, ExpressionBodyPreference.WhenPossible, cancellationToken, out var expressionBody, out var semicolonToken);

            var trailingTrivia = semicolonToken.TrailingTrivia
                                               .Where(t => t.Kind() != SyntaxKind.EndOfLineTrivia)
                                               .Concat(declaration.GetTrailingTrivia());
            semicolonToken = semicolonToken.WithTrailingTrivia(trailingTrivia);

            return WithSemicolonToken(
                       WithExpressionBody(
                           WithBody(declaration, body: null),
                           expressionBody),
                       semicolonToken);
        }
        else
        {
            return WithSemicolonToken(
                       WithExpressionBody(
                           WithGenerateBody(semanticModel, declaration),
                           expressionBody: null),
                       default);
        }
    }

    protected abstract BlockSyntax? GetBody(TDeclaration declaration);

    protected abstract ArrowExpressionClauseSyntax? GetExpressionBody(TDeclaration declaration);

    protected abstract bool CreateReturnStatementForExpression(SemanticModel semanticModel, TDeclaration declaration);

    protected abstract SyntaxToken GetSemicolonToken(TDeclaration declaration);

    protected abstract TDeclaration WithSemicolonToken(TDeclaration declaration, SyntaxToken token);
    protected abstract TDeclaration WithExpressionBody(TDeclaration declaration, ArrowExpressionClauseSyntax? expressionBody);
    protected abstract TDeclaration WithBody(TDeclaration declaration, BlockSyntax? body);

    protected virtual TDeclaration WithGenerateBody(SemanticModel semanticModel, TDeclaration declaration)
    {
        var expressionBody = GetExpressionBody(declaration);

        if (expressionBody.TryConvertToBlock(
                GetSemicolonToken(declaration),
                CreateReturnStatementForExpression(semanticModel, declaration),
                out var block))
        {
            return WithBody(declaration, block);
        }

        return declaration;
    }

    protected TDeclaration WithAccessorList(SemanticModel semanticModel, TDeclaration declaration)
    {
        var expressionBody = GetExpressionBody(declaration);
        var semicolonToken = GetSemicolonToken(declaration);

        // When converting an expression-bodied property to a block body, always attempt to
        // create an accessor with a block body (even if the user likes expression bodied
        // accessors.  While this technically doesn't match their preferences, it fits with
        // the far more likely scenario that the user wants to convert this property into
        // a full property so that they can flesh out the body contents.  If we keep around
        // an expression bodied accessor they'll just have to convert that to a block as well
        // and that means two steps to take instead of one.

        expressionBody.TryConvertToBlock(
            GetSemicolonToken(declaration),
            CreateReturnStatementForExpression(semanticModel, declaration),
            out var block);

        var accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration);
        accessor = block != null
            ? accessor.WithBody(block)
            : accessor.WithExpressionBody(expressionBody)
                      .WithSemicolonToken(semicolonToken);

        return WithAccessorList(declaration, SyntaxFactory.AccessorList([accessor]));
    }

    protected virtual TDeclaration WithAccessorList(TDeclaration declaration, AccessorListSyntax accessorListSyntax)
        => throw new NotImplementedException();
}
