// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UsePatternMatchingAsAndMemberAccess), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CSharpAsAndMemberAccessCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UsePatternMatchingAsAndMemberAccessDiagnosticId];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Use_pattern_matching, nameof(CSharpAnalyzersResources.Use_pattern_matching));
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
            FixOne(editor, diagnostic, cancellationToken);
    }

    private static void FixOne(SyntaxEditor editor, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var node = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
        if (node is not BinaryExpressionSyntax asExpression)
            return;

        if (!UsePatternMatchingHelpers.TryGetPartsOfAsAndMemberAccessCheck(
                asExpression, out var conditionalAccessExpression, out var binaryExpression, out var isPatternExpression, out _))
        {
            return;
        }

        var parent = binaryExpression ?? (ExpressionSyntax?)isPatternExpression;
        Contract.ThrowIfNull(parent);

        var (notKeyword, initialPattern) = CreatePattern(binaryExpression, isPatternExpression);

        // { X.Y: pattern }
        var propertyPattern = PropertyPatternClause(
            OpenBraceToken.WithoutTrivia().WithAppendedTrailingTrivia(Space),
            [Subpattern(
                CreateExpressionColon(conditionalAccessExpression),
                initialPattern.WithTrailingTrivia(Space))],
            CloseBraceToken.WithoutTrivia());

        // T { X.Y: pattern }
        var recursivePattern = RecursivePattern(
            (TypeSyntax)asExpression.Right.WithAppendedTrailingTrivia(Space),
            positionalPatternClause: null,
            propertyPattern,
            designation: null);

        // is T { X.Y: pattern }
        var newIsExpression = IsPatternExpression(
            asExpression.Left,
            IsKeyword.WithTriviaFrom(asExpression.OperatorToken),
            notKeyword == default ? recursivePattern : UnaryPattern(notKeyword, recursivePattern));

        var toReplace = parent.WalkUpParentheses();
        editor.ReplaceNode(
            toReplace,
            newIsExpression.WithTriviaFrom(toReplace));

        return;

        static BaseExpressionColonSyntax CreateExpressionColon(ConditionalAccessExpressionSyntax conditionalAccessExpression)
        {
            var whenNotNull = conditionalAccessExpression.WhenNotNull;

            if (whenNotNull is MemberBindingExpressionSyntax { Name: IdentifierNameSyntax identifierName })
                return NameColon(identifierName);

            return ExpressionColon(RewriteMemberBindingToExpression(whenNotNull), ColonToken);
        }

        static ExpressionSyntax RewriteMemberBindingToExpression(ExpressionSyntax expression)
        {
            // .X => X
            if (expression is MemberBindingExpressionSyntax memberBinding)
                return memberBinding.Name;

            // .X.Y   recurse down left side to produce: X.Y
            if (expression is MemberAccessExpressionSyntax memberAccessExpression)
                return memberAccessExpression.WithExpression(RewriteMemberBindingToExpression(memberAccessExpression.Expression));

            return expression;
        }

        static (SyntaxToken notKeyword, PatternSyntax pattern) CreatePattern(
            BinaryExpressionSyntax? binaryExpression, IsPatternExpressionSyntax? isPatternExpression)
        {
            // if we had `.X.Y is some_pattern` we can just convert that to `X.Y: some_pattern`
            if (isPatternExpression != null)
            {
                // If this is a `not { ..  var name ... }` pattern, then we need to lift the 'not' outwards to ensure
                // that 'var name' is still in scope when the pattern is checked. The lang only allows this for
                // top-level 'not' pattern, not for an inner 'not' pattern.
                if (isPatternExpression.Pattern is UnaryPatternSyntax(kind: SyntaxKind.NotPattern) unaryPattern &&
                    unaryPattern.DescendantNodes().OfType<DeclarationPatternSyntax>().Any())
                {
                    return (unaryPattern.OperatorToken, unaryPattern.Pattern);
                }

                return (default, isPatternExpression.Pattern);
            }

            Contract.ThrowIfNull(binaryExpression);

            PatternSyntax pattern = binaryExpression.Kind() switch
            {
                // `.X.Y == expr` => `X.Y: expr`
                SyntaxKind.EqualsExpression => ConstantPattern(binaryExpression.Right),
                // `.X.Y != expr` => `X.Y: not expr`
                SyntaxKind.NotEqualsExpression => UnaryPattern(ConstantPattern(binaryExpression.Right)),
                // `.X.Y > expr` => `X.Y: > expr`
                // etc
                SyntaxKind.GreaterThanExpression or
                SyntaxKind.GreaterThanOrEqualExpression or
                SyntaxKind.LessThanExpression or
                SyntaxKind.LessThanOrEqualExpression => RelationalPattern(binaryExpression.OperatorToken, binaryExpression.Right),
                _ => throw ExceptionUtilities.Unreachable()
            };

            return (default, pattern);
        }
    }
}
