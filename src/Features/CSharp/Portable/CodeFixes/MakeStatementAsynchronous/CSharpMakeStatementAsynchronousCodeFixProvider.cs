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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.MakeStatementAsynchronous
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakeStatementAsynchronous), Shared]
    internal class CSharpMakeStatementAsynchronousCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        public CSharpMakeStatementAsynchronousCodeFixProvider()
        {
        }

        // error CS8414: foreach statement cannot operate on variables of type 'IAsyncEnumerable<int>' because 'IAsyncEnumerable<int>' does not contain a public instance definition for 'GetEnumerator'. Did you mean 'await foreach'?
        // error CS8418: 'IAsyncDisposable': type used in a using statement must be implicitly convertible to 'System.IDisposable'. Did you mean 'await using' rather than 'using'?
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("CS8414", "CS8418");

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.Compile;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

            var constructToFix = TryGetStatementToFix(node);
            if (constructToFix == null)
            {
                return;
            }

            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, diagnostic, c)),
                context.Diagnostics);
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;

            foreach (var diagnostic in diagnostics)
            {
                var node = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
                var statementToFix = TryGetStatementToFix(node);
                if (statementToFix != null)
                {
                    MakeStatementAsynchronous(editor, statementToFix);
                }
            }

            return Task.CompletedTask;
        }

        private static void MakeStatementAsynchronous(SyntaxEditor editor, SyntaxNode statementToFix)
        {
            SyntaxNode newStatement;
            switch (statementToFix)
            {
                case ForEachStatementSyntax forEach:
                    newStatement = forEach
                        .WithForEachKeyword(forEach.ForEachKeyword.WithLeadingTrivia())
                        .WithAwaitKeyword(SyntaxFactory.Token(SyntaxKind.AwaitKeyword).WithLeadingTrivia(forEach.GetLeadingTrivia()));
                    break;
                case ForEachVariableStatementSyntax forEachDeconstruction:
                    newStatement = forEachDeconstruction
                        .WithForEachKeyword(forEachDeconstruction.ForEachKeyword.WithLeadingTrivia())
                        .WithAwaitKeyword(SyntaxFactory.Token(SyntaxKind.AwaitKeyword).WithLeadingTrivia(forEachDeconstruction.GetLeadingTrivia()));
                    break;
                case UsingStatementSyntax usingStatement:
                    newStatement = usingStatement
                        .WithUsingKeyword(usingStatement.UsingKeyword.WithLeadingTrivia())
                        .WithAwaitKeyword(SyntaxFactory.Token(SyntaxKind.AwaitKeyword).WithLeadingTrivia(usingStatement.GetLeadingTrivia()));
                    break;
                case LocalDeclarationStatementSyntax localDeclaration:
                    newStatement = localDeclaration
                        .WithUsingKeyword(localDeclaration.UsingKeyword.WithLeadingTrivia())
                        .WithAwaitKeyword(SyntaxFactory.Token(SyntaxKind.AwaitKeyword).WithLeadingTrivia(localDeclaration.GetLeadingTrivia()));
                    break;
                default:
                    return;
            }

            editor.ReplaceNode(statementToFix, newStatement);
        }

        private static SyntaxNode TryGetStatementToFix(SyntaxNode node)
        {
            if (node.IsParentKind(SyntaxKind.ForEachStatement, SyntaxKind.ForEachVariableStatement, SyntaxKind.UsingStatement))
            {
                return node.Parent;
            }

            if (node is LocalDeclarationStatementSyntax localDeclaration && localDeclaration.UsingKeyword != default)
            {
                return node;
            }

            return null;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpFeaturesResources.Add_await,
                       createChangedDocument,
                       CSharpFeaturesResources.Add_await)
            {
            }
        }
    }
}
