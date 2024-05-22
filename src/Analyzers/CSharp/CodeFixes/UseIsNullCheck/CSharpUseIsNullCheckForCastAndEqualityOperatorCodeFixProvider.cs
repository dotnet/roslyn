// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseIsNullCheck;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseIsNullCheck;

using static SyntaxFactory;
using static UseIsNullCheckHelpers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseIsNullCheckForCastAndEqualityOperator), Shared]
internal class CSharpUseIsNullCheckForCastAndEqualityOperatorCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpUseIsNullCheckForCastAndEqualityOperatorCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseIsNullCheckDiagnosticId];

    private static bool IsSupportedDiagnostic(Diagnostic diagnostic)
        => diagnostic.Properties[UseIsNullConstants.Kind] == UseIsNullConstants.CastAndEqualityKey;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        if (IsSupportedDiagnostic(diagnostic))
        {
            var negated = diagnostic.Properties.ContainsKey(UseIsNullConstants.Negated);
            var title = GetTitle(negated, diagnostic.Location.SourceTree!.Options);

            context.RegisterCodeFix(
                CodeAction.Create(title, GetDocumentUpdater(context), title),
                context.Diagnostics);
        }

        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics)
        {
            if (!IsSupportedDiagnostic(diagnostic))
                continue;

            var binary = (BinaryExpressionSyntax)diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken: cancellationToken);

            editor.ReplaceNode(
                binary,
                (current, g) => Rewrite((BinaryExpressionSyntax)current));
        }

        return Task.CompletedTask;
    }

    private static ExpressionSyntax Rewrite(BinaryExpressionSyntax binary)
    {
        var isPattern = RewriteWorker(binary);
        if (binary.IsKind(SyntaxKind.EqualsExpression))
            return isPattern;

        if (SupportsIsNotPattern(binary.SyntaxTree.Options))
        {
            // convert:  (object)expr != null   to    expr is not null
            return isPattern.WithPattern(
                UnaryPattern(isPattern.Pattern));
        }
        else
        {
            // convert:  (object)expr != null   to    expr is object
            return BinaryExpression(
                SyntaxKind.IsExpression,
                isPattern.Expression,
                PredefinedType(Token(SyntaxKind.ObjectKeyword))).WithTriviaFrom(isPattern);
        }
    }

    private static IsPatternExpressionSyntax RewriteWorker(BinaryExpressionSyntax binary)
        => binary.Right.IsKind(SyntaxKind.NullLiteralExpression)
            ? Rewrite(binary, binary.Left, binary.Right)
            : Rewrite(binary, binary.Right, binary.Left);

    private static IsPatternExpressionSyntax Rewrite(
        BinaryExpressionSyntax binary, ExpressionSyntax expr, ExpressionSyntax nullLiteral)
    {
        var castExpr = (CastExpressionSyntax)expr;
        return IsPatternExpression(
            castExpr.Expression.WithTriviaFrom(binary.Left),
            Token(SyntaxKind.IsKeyword).WithTriviaFrom(binary.OperatorToken),
            ConstantPattern(nullLiteral).WithTriviaFrom(binary.Right));
    }
}
