// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.AddBraces
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddBraces), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddAwait)]
    internal class CSharpAddBracesCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds 
            => ImmutableArray.Create(IDEDiagnosticIds.AddBracesDiagnosticId);

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);

            return SpecializedTasks.EmptyTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, 
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;
            foreach (var diagnostic in diagnostics)
            {
                var statement = root.FindNode(diagnostic.Location.SourceSpan);

                // Use the callback version of ReplaceNode so that we see the effects
                // of other replace calls.  i.e. we may have statements nested in statements,
                // we need to make sure that any inner edits are seen when we make the outer
                // replacement.
                editor.ReplaceNode(statement, (s, g) => GetReplacementNode(s));
            }

            return SpecializedTasks.EmptyTask;
        }

        private SyntaxNode GetReplacementNode(SyntaxNode statement)
        {
            switch (statement.Kind())
            {
                case SyntaxKind.IfStatement:
                    var ifSyntax = (IfStatementSyntax)statement;
                    return GetNewBlock(statement, ifSyntax.Statement);

                case SyntaxKind.ElseClause:
                    var elseClause = (ElseClauseSyntax)statement;
                    return GetNewBlock(statement, elseClause.Statement);

                case SyntaxKind.ForStatement:
                    var forSyntax = (ForStatementSyntax)statement;
                    return GetNewBlock(statement, forSyntax.Statement);

                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachVariableStatement:
                    var forEachSyntax = (CommonForEachStatementSyntax)statement;
                    return GetNewBlock(statement, forEachSyntax.Statement);

                case SyntaxKind.WhileStatement:
                    var whileSyntax = (WhileStatementSyntax)statement;
                    return GetNewBlock(statement, whileSyntax.Statement);

                case SyntaxKind.DoStatement:
                    var doSyntax = (DoStatementSyntax)statement;
                    return GetNewBlock(statement, doSyntax.Statement);

                case SyntaxKind.UsingStatement:
                    var usingSyntax = (UsingStatementSyntax)statement;
                    return GetNewBlock(statement, usingSyntax.Statement);

                case SyntaxKind.LockStatement:
                    var lockSyntax = (LockStatementSyntax)statement;
                    return GetNewBlock(statement, lockSyntax.Statement);
            }

            return default(SyntaxNode);
        }

        private SyntaxNode GetNewBlock(SyntaxNode statement, StatementSyntax statementBody) 
            => statement.ReplaceNode(statementBody, SyntaxFactory.Block(statementBody));

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(FeaturesResources.Add_braces, createChangedDocument, FeaturesResources.Add_braces)
            {
            }
        }
    }
}
