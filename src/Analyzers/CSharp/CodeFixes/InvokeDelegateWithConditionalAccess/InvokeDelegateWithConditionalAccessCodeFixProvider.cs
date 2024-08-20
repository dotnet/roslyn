// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.InvokeDelegateWithConditionalAccess;

using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.InvokeDelegateWithConditionalAccess), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed partial class InvokeDelegateWithConditionalAccessCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [IDEDiagnosticIds.InvokeDelegateWithConditionalAccessId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Simplify_delegate_invocation, nameof(CSharpAnalyzersResources.Simplify_delegate_invocation));
        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddEdits(editor, diagnostic, cancellationToken);
        }

        return Task.CompletedTask;
    }

    private static void AddEdits(
        SyntaxEditor editor, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        if (diagnostic.Properties[Constants.Kind] == Constants.VariableAndIfStatementForm)
        {
            HandleVariableAndIfStatementForm(editor, diagnostic, cancellationToken);
        }
        else
        {
            Debug.Assert(diagnostic.Properties[Constants.Kind] == Constants.SingleIfStatementForm);
            HandleSingleIfStatementForm(editor, diagnostic, cancellationToken);
        }
    }

    private static void HandleSingleIfStatementForm(
        SyntaxEditor editor,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        var root = editor.OriginalRoot;

        var ifStatementLocation = diagnostic.AdditionalLocations[0];
        var expressionStatementLocation = diagnostic.AdditionalLocations[1];

        var ifStatement = (IfStatementSyntax)root.FindNode(ifStatementLocation.SourceSpan);
        cancellationToken.ThrowIfCancellationRequested();

        var expressionStatement = (ExpressionStatementSyntax)root.FindNode(expressionStatementLocation.SourceSpan);
        cancellationToken.ThrowIfCancellationRequested();

        var invocationExpression = (InvocationExpressionSyntax)expressionStatement.Expression;
        cancellationToken.ThrowIfCancellationRequested();

        var (invokedExpression, invokeName) =
            invocationExpression.Expression is MemberAccessExpressionSyntax { Name: IdentifierNameSyntax { Identifier.ValueText: nameof(Action.Invoke) } } memberAccessExpression
                ? (memberAccessExpression.Expression, memberAccessExpression.Name)
                : (invocationExpression.Expression, IdentifierName(nameof(Action.Invoke)));

        StatementSyntax newStatement = expressionStatement.WithExpression(
            ConditionalAccessExpression(
                invokedExpression,
                InvocationExpression(
                    MemberBindingExpression(invokeName), invocationExpression.ArgumentList)));
        newStatement = newStatement.WithPrependedLeadingTrivia(ifStatement.GetLeadingTrivia());

        if (ifStatement.Parent.IsKind(SyntaxKind.ElseClause) &&
            ifStatement.Statement is BlockSyntax block)
        {
            newStatement = block.WithStatements([newStatement]);
        }

        newStatement = newStatement.WithAdditionalAnnotations(Formatter.Annotation);
        newStatement = AppendTriviaWithoutEndOfLines(newStatement, ifStatement);

        cancellationToken.ThrowIfCancellationRequested();

        editor.ReplaceNode(ifStatement, newStatement);
    }

    private static void HandleVariableAndIfStatementForm(
        SyntaxEditor editor, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var root = editor.OriginalRoot;

        var localDeclarationLocation = diagnostic.AdditionalLocations[0];
        var ifStatementLocation = diagnostic.AdditionalLocations[1];
        var expressionStatementLocation = diagnostic.AdditionalLocations[2];

        var localDeclarationStatement = (LocalDeclarationStatementSyntax)root.FindNode(localDeclarationLocation.SourceSpan);
        cancellationToken.ThrowIfCancellationRequested();

        var ifStatement = (IfStatementSyntax)root.FindNode(ifStatementLocation.SourceSpan);
        cancellationToken.ThrowIfCancellationRequested();

        var expressionStatement = (ExpressionStatementSyntax)root.FindNode(expressionStatementLocation.SourceSpan);
        cancellationToken.ThrowIfCancellationRequested();

        var invocationExpression = (InvocationExpressionSyntax)expressionStatement.Expression;
        var parentBlock = (BlockSyntax)localDeclarationStatement.GetRequiredParent();

        var invokeName =
            invocationExpression.Expression is MemberAccessExpressionSyntax { Name: IdentifierNameSyntax { Identifier.ValueText: nameof(Action.Invoke) } } memberAccessExpression
                ? memberAccessExpression.Name
                : IdentifierName(nameof(Action.Invoke));

        var newStatement = expressionStatement.WithExpression(
            ConditionalAccessExpression(
                localDeclarationStatement.Declaration.Variables[0].Initializer!.Value.Parenthesize(),
                InvocationExpression(
                    MemberBindingExpression(invokeName), invocationExpression.ArgumentList)));

        newStatement = newStatement.WithAdditionalAnnotations(Formatter.Annotation);
        newStatement = AppendTriviaWithoutEndOfLines(newStatement, ifStatement);

        editor.ReplaceNode(ifStatement, newStatement);
        editor.RemoveNode(localDeclarationStatement, SyntaxRemoveOptions.KeepLeadingTrivia | SyntaxRemoveOptions.AddElasticMarker);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static T AppendTriviaWithoutEndOfLines<T>(T newStatement, IfStatementSyntax ifStatement) where T : SyntaxNode
    {
        // We're combining trivia from the delegate invocation and the end of the if statement
        // but we don't want two EndOfLines so we ignore the one on the invocation (if it exists)
        var expressionTrivia = newStatement.GetTrailingTrivia();
        var expressionTriviaWithoutEndOfLine = expressionTrivia.Where(t => !t.IsKind(SyntaxKind.EndOfLineTrivia));
        var ifStatementTrivia = ifStatement.GetTrailingTrivia();

        return newStatement.WithTrailingTrivia(expressionTriviaWithoutEndOfLine.Concat(ifStatementTrivia));
    }
}
