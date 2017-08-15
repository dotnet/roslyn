// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnusedVariable
{
    internal abstract class AbstractRemoveUnusedVariableCodeFixProvider<TLocalDeclarationStatement, TVariableDeclarator, TVariableDeclaration> : SyntaxEditorBasedCodeFixProvider
        where TLocalDeclarationStatement : SyntaxNode
        where TVariableDeclarator : SyntaxNode
        where TVariableDeclaration : SyntaxNode
    {
        protected abstract bool IsCatchDeclarationIdentifier(SyntaxToken token);

        public async override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
                var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
                if (!IsCatchDeclarationIdentifier(token))
                {
                    var ancestor = token.GetAncestor<TLocalDeclarationStatement>();

                    if (ancestor == null)
                    {
                        return;
                    }
                }
            }

            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
        }

        protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var root = editor.OriginalRoot;
            foreach (var diagnostic in diagnostics)
            {
                var token = diagnostic.Location.FindToken(cancellationToken);
                if (IsCatchDeclarationIdentifier(token))
                {
                    editor.ReplaceNode(
                        token.Parent,
                        token.Parent.ReplaceToken(token, default(SyntaxToken)).WithAdditionalAnnotations(Formatter.Annotation));
                }
                else
                {
                    var variableDeclarator = token.GetAncestor<TVariableDeclarator>();
                    var variableDeclarators = token.GetAncestor<TVariableDeclaration>().ChildNodes().Where(x => x is TVariableDeclarator);

                    if (variableDeclarators.Count() == 1)
                    {
                        var localDeclaration = token.GetAncestor<TLocalDeclarationStatement>();
                        var removeOptions = SyntaxGenerator.DefaultRemoveOptions;

                        if (localDeclaration.GetLeadingTrivia().Contains(t => t.IsDirective))
                        {
                            removeOptions |= SyntaxRemoveOptions.KeepLeadingTrivia;
                        }
                        else
                        {
                            var statementParent = localDeclaration.Parent;
                            if (syntaxFacts.IsExecutableBlock(statementParent))
                            {
                                var siblings = syntaxFacts.GetExecutableBlockStatements(statementParent);
                                var localDeclarationIndex = siblings.IndexOf(localDeclaration);
                                if (localDeclarationIndex != 0)
                                {
                                    // if we're removing hte first statement in a block, then we
                                    // want to have the elastic marker on it so that the next statement
                                    // properly formats with the space left behind.  But if it's
                                    // not the first statement then just keep the trivia as is
                                    // so that the statement before and after it stay appropriately
                                    // spaced apart.
                                    removeOptions &= ~SyntaxRemoveOptions.AddElasticMarker;
                                }
                            }
                        }

                        editor.RemoveNode(localDeclaration, removeOptions);
                    }
                    else if (variableDeclarators.Count() > 1)
                    {
                        editor.RemoveNode(variableDeclarator);
                    }
                }
            }

            return SpecializedTasks.EmptyTask;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(FeaturesResources.Remove_unused_variable, createChangedDocument, FeaturesResources.Remove_unused_variable)
            {
            }
        }
    }
}
