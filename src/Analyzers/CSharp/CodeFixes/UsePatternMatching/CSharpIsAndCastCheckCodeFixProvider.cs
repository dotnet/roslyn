// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UsePatternMatchingIsAndCastCheck), Shared]
internal partial class CSharpIsAndCastCheckCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpIsAndCastCheckCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.InlineIsTypeCheckId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Use_pattern_matching, nameof(CSharpAnalyzersResources.Use_pattern_matching));
        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddEdits(editor, diagnostic, cancellationToken);
        }

        return Task.CompletedTask;
    }

    private static void AddEdits(
        SyntaxEditor editor,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        var ifStatementLocation = diagnostic.AdditionalLocations[0];
        var localDeclarationLocation = diagnostic.AdditionalLocations[1];

        var ifStatement = (IfStatementSyntax)ifStatementLocation.FindNode(cancellationToken);
        var localDeclaration = (LocalDeclarationStatementSyntax)localDeclarationLocation.FindNode(cancellationToken);
        var isExpression = (BinaryExpressionSyntax)ifStatement.Condition;

        var updatedCondition = SyntaxFactory.IsPatternExpression(
            isExpression.Left, SyntaxFactory.DeclarationPattern(
                ((TypeSyntax)isExpression.Right).WithoutTrivia(),
                SyntaxFactory.SingleVariableDesignation(
                    localDeclaration.Declaration.Variables[0].Identifier.WithoutTrivia())));

        var trivia = localDeclaration.GetLeadingTrivia().Concat(localDeclaration.GetTrailingTrivia())
                                     .Where(t => t.IsSingleOrMultiLineComment())
                                     .SelectMany(t => ImmutableArray.Create(SyntaxFactory.Space, t, SyntaxFactory.ElasticCarriageReturnLineFeed))
                                     .ToImmutableArray();

        editor.RemoveNode(localDeclaration);
        editor.ReplaceNode(ifStatement,
            (i, g) =>
            {
                // Because the local declaration is *inside* the 'if', we need to get the 'if' 
                // statement after it was already modified and *then* update the condition
                // portion of it.
                var currentIf = (IfStatementSyntax)i;
                return GetUpdatedIfStatement(updatedCondition, trivia, ifStatement, currentIf);
            });
    }

    private static IfStatementSyntax GetUpdatedIfStatement(
        IsPatternExpressionSyntax updatedCondition,
        ImmutableArray<SyntaxTrivia> trivia,
        IfStatementSyntax originalIf,
        IfStatementSyntax currentIf)
    {
        var newIf = currentIf.ReplaceNode(currentIf.Condition, updatedCondition);
        newIf = originalIf.IsParentKind(SyntaxKind.ElseClause)
            ? newIf.ReplaceToken(newIf.CloseParenToken, newIf.CloseParenToken.WithTrailingTrivia(trivia))
            : newIf.WithPrependedLeadingTrivia(trivia);

        return newIf.WithAdditionalAnnotations(Formatter.Annotation);
    }
}
