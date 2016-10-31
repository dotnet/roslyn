// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
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

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpIsAndCastCheckCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.InlineIsTypeCheckId);

        public override FixAllProvider GetFixAllProvider() => new InlineTypeCheckFixAllProvider(this);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return SpecializedTasks.EmptyTask;
        }

        private Task<Document> FixAsync(
            Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            return FixAllAsync(document, ImmutableArray.Create(diagnostic), cancellationToken);
        }

        private async Task<Document> FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);

            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddEdits(root, editor, diagnostic, cancellationToken);
            }

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private void AddEdits(
            SyntaxNode root,
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
                    Extensions.SyntaxTokenExtensions.WithoutTrivia(
                        localDeclaration.Declaration.Variables[0].Identifier)));

            var trivia = localDeclaration.GetLeadingTrivia().Concat(localDeclaration.GetTrailingTrivia())
                                         .Where(t => t.IsSingleOrMultiLineComment())
                                         .SelectMany(t => ImmutableArray.Create(t, SyntaxFactory.ElasticCarriageReturnLineFeed))
                                         .ToImmutableArray();

            var updatedIfStatement = ifStatement.ReplaceNode(ifStatement.Condition, updatedCondition)
                                                .WithPrependedLeadingTrivia(trivia)
                                                .WithAdditionalAnnotations(Formatter.Annotation);
            
            editor.RemoveNode(localDeclaration);
            editor.ReplaceNode(ifStatement,
                (i, g) =>
                {
                    // Because the local declaration is *inside* the 'if', we need to get the 'if' 
                    // statement after it was already modified and *then* update the condition
                    // portion of it.
                    var currentIf = (IfStatementSyntax)i;
                    return currentIf.ReplaceNode(currentIf.Condition, updatedCondition)
                                    .WithPrependedLeadingTrivia(trivia)
                                    .WithAdditionalAnnotations(Formatter.Annotation);
                });
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpFeaturesResources.Inline_temporary_variable, createChangedDocument)
            {
            }
        }
    }
}