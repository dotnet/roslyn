// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseSimpleUsingStatement
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseSimpleUsingStatementCodeFixProvider)), Shared]
    internal class UseSimpleUsingStatementCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.UseSimpleUsingStatementDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var topmostUsingStatements = diagnostics.SelectAsArray(d => (UsingStatementSyntax)d.AdditionalLocations[0].FindNode(cancellationToken));

            // Grab all the convertible usings in a block of nested usings.  i.e.
            // 
            //      using (...)
            //      using (...)
            //      using (...)
            //      {
            //      }
            //
            // If invoked on any of these , we'll convert in all in teh group that we can.

            // Importantly, this is a set, so if we're doing a fix-all, we may add the items
            // multiple times, but that's ok as it will still only be in the set once.
            var allUsingStatements = GetUsingStatementsToUpdate(topmostUsingStatements);

            var rewriter = new Rewriter(allUsingStatements);
            var root = editor.OriginalRoot;
            var updatedRoot = rewriter.Visit(root);

            editor.ReplaceNode(root, updatedRoot);

            return Task.CompletedTask;
        }

        private static HashSet<UsingStatementSyntax> GetUsingStatementsToUpdate(ImmutableArray<UsingStatementSyntax> topmostUsingStatements)
        {
            var allUsingStatements = new HashSet<UsingStatementSyntax>();

            foreach (var topmostUsingStatement in topmostUsingStatements)
            {
                // Walk inwards adding all using statements that are convertible.
                for (var current = topmostUsingStatement;
                     current?.Declaration != null;
                     current = current.Statement as UsingStatementSyntax)
                {
                    allUsingStatements.Add(current);
                }
            }

            return allUsingStatements;
        }

        private class Rewriter : CSharpSyntaxRewriter
        {
            private readonly ISet<UsingStatementSyntax> _usingStatements;

            public Rewriter(ISet<UsingStatementSyntax> usingStatements)
            {
                _usingStatements = usingStatements;
            }

            private static SyntaxList<StatementSyntax> Expand(UsingStatementSyntax usingStatement)
            {
                var builder = ArrayBuilder<StatementSyntax>.GetInstance();
                builder.Add(Convert(usingStatement));
                if (usingStatement.Statement is BlockSyntax block)
                {
                    builder.AddRange(block.Statements);
                }
                else
                {
                    builder.Add(usingStatement.Statement);
                }

                var statements = new SyntaxList<StatementSyntax>(builder);
                builder.Free();
                return statements;
            }

            private bool ShouldExpand(StatementSyntax beforeStatement, StatementSyntax afterStatement)
                => _usingStatements.Contains(beforeStatement) &&
                   afterStatement is UsingStatementSyntax usingStatement &&
                   usingStatement.Declaration != null;

            private static LocalDeclarationStatementSyntax Convert(UsingStatementSyntax usingStatement)
            {
                return SyntaxFactory.LocalDeclarationStatement(
                    usingStatement.AwaitKeyword,
                    usingStatement.UsingKeyword,
                    modifiers: default,
                    usingStatement.Declaration,
                    SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }

            private bool ExpandAppropriateStatements(
                SyntaxList<StatementSyntax> originalStatements,
                SyntaxList<StatementSyntax> rewrittenStatements,
                ArrayBuilder<StatementSyntax> finalStatements)
            {
                var changed = false;
                if (originalStatements.Count == rewrittenStatements.Count)
                {
                    for (int i = 0, n = rewrittenStatements.Count; i < n; i++)
                    {
                        var rewrittenStatement = rewrittenStatements[i];
                        if (ShouldExpand(originalStatements[i], rewrittenStatement))
                        {
                            var expanded = Expand((UsingStatementSyntax)rewrittenStatement);
                            finalStatements.AddRange(expanded);
                            changed = true;
                        }
                        else
                        {
                            finalStatements.Add(rewrittenStatement);
                        }
                    }
                }

                return changed;
            }

            public override SyntaxNode VisitBlock(BlockSyntax node)
            {
                var finalStatements = ArrayBuilder<StatementSyntax>.GetInstance();
                var result = VisitBlockWorker(node, finalStatements);
                finalStatements.Free();

                return result;
            }

            private SyntaxNode VisitBlockWorker(BlockSyntax node, ArrayBuilder<StatementSyntax> finalStatements)
            {
                var rewrittenBlock = (BlockSyntax)base.VisitBlock(node);

                var changed = ExpandAppropriateStatements(node.Statements, rewrittenBlock.Statements, finalStatements);

                if (!changed)
                {
                    // Didn't update any children.  Just return as is.
                    return rewrittenBlock;
                }

                return rewrittenBlock.WithStatements(new SyntaxList<StatementSyntax>(finalStatements))
                                     .WithAdditionalAnnotations(Formatter.Annotation);
            }

            public override SyntaxNode VisitUsingStatement(UsingStatementSyntax node)
            {
                // First, descend into the using-statement so we fixup any using statements contained
                // inside of it.
                var rewrittenUsingStatement = (UsingStatementSyntax)base.VisitUsingStatement(node);

                // Now, if the else-clause previous pointed at a using statement we wanted to
                // rewrite, and it still points at a using statement, then expand that using
                // statemnet into the else-clause.
                var rewrittenStatement = rewrittenUsingStatement.Statement;
                if (ShouldExpand(node.Statement, rewrittenStatement))
                {
                    // Can't expand a using-statement directly inside an else-clause.
                    // have to add a block around it to make sure scoping is preserved
                    // properly.
                    var expanded = Expand((UsingStatementSyntax)rewrittenStatement);
                    return rewrittenUsingStatement.WithStatement(SyntaxFactory.Block(expanded))
                                                  .WithAdditionalAnnotations(Formatter.Annotation);
                }

                return rewrittenUsingStatement;
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Use_simple_using_statement, createChangedDocument, FeaturesResources.Use_simple_using_statement)
            {
            }
        }
    }
}
