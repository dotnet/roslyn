// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
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

        protected abstract SyntaxNode GetNodeToRemoveOrReplace(SyntaxNode node);

        protected abstract void RemoveOrReplaceNode(SyntaxEditor editor, SyntaxNode node, ISyntaxFactsService syntaxFacts);

        protected abstract SeparatedSyntaxList<SyntaxNode> GetVariables(TLocalDeclarationStatement localDeclarationStatement);

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(new MyCodeAction(c => FixAsync(context.Document, diagnostic, c)), diagnostic);
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor syntaxEditor, CancellationToken cancellationToken)
        {
            var nodesToRemove = new HashSet<SyntaxNode>();

            // Create actions and keep their SpanStart. 
            // Execute actions ordered descending by SpanStart to avoid conflicts.
            var actionsToPerform = new List<(int, Action)>();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var documentsToBeSearched = ImmutableHashSet.Create(document);

            foreach (var diagnostic in diagnostics)
            {
                var token = diagnostic.Location.FindToken(cancellationToken);
                var node = root.FindNode(diagnostic.Location.SourceSpan);
                if (IsCatchDeclarationIdentifier(token))
                {
                    (int, Action) pair = (token.Parent.SpanStart,
                        () => syntaxEditor.ReplaceNode(
                            token.Parent,
                            token.Parent.ReplaceToken(token, default(SyntaxToken)).WithAdditionalAnnotations(Formatter.Annotation)));
                    actionsToPerform.Add(pair);
                }
                else
                {
                    nodesToRemove.Add(node);
                }

                var symbol = documentEditor.SemanticModel.GetDeclaredSymbol(node);
                var referencedSymbols = await SymbolFinder.FindReferencesAsync(symbol, document.Project.Solution, documentsToBeSearched, cancellationToken).ConfigureAwait(false);

                foreach (var referencedSymbol in referencedSymbols)
                {
                    if (referencedSymbol?.Locations != null)
                    {
                        foreach (var location in referencedSymbol.Locations)
                        {
                            var referencedSymbolNode = root.FindNode(location.Location.SourceSpan);
                            if (referencedSymbolNode != null)
                            {
                                var nodeToRemoveOrReplace = GetNodeToRemoveOrReplace(referencedSymbolNode);
                                if (nodeToRemoveOrReplace != null)
                                {
                                    nodesToRemove.Add(nodeToRemoveOrReplace);
                                }
                            }
                        }
                    }
                }
            }

            MergeNodesToRemove(nodesToRemove);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            foreach (var node in nodesToRemove)
            {
                actionsToPerform.Add((node.SpanStart, () => RemoveOrReplaceNode(syntaxEditor, node, syntaxFacts)));
            }

            // Process nodes in reverse order 
            // to complete with nested declarations before processing the outer ones.
            foreach (var node in actionsToPerform.OrderByDescending(n => n.Item1))
            {
                node.Item2();
            }
        }

        protected void RemoveNode(SyntaxEditor editor, SyntaxNode node, ISyntaxFactsService syntaxFacts)
        {
            var localDeclaration = node.GetAncestorOrThis<TLocalDeclarationStatement>();
            var removeOptions = CreateSyntaxRemoveOptions(localDeclaration, syntaxFacts);
            editor.RemoveNode(node, removeOptions);
        }

        private SyntaxRemoveOptions CreateSyntaxRemoveOptions(TLocalDeclarationStatement localDeclaration, ISyntaxFactsService syntaxFacts)
        {
            var removeOptions = SyntaxGenerator.DefaultRemoveOptions;

            if (localDeclaration != null)
            {
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
                            // if we're removing the first statement in a block, then we
                            // want to have the elastic marker on it so that the next statement
                            // properly formats with the space left behind.  But if it's
                            // not the first statement then just keep the trivia as is
                            // so that the statement before and after it stay appropriately
                            // spaced apart.
                            removeOptions &= ~SyntaxRemoveOptions.AddElasticMarker;
                        }
                    }
                }
            }

            return removeOptions;
        }

        // Merges node like
        // var unused1 = 0, unused2 = 0;
        // to remove the whole line.
        private void MergeNodesToRemove(HashSet<SyntaxNode> nodesToRemove)
        {
            var candidateLocalDeclarationsToRemove = new HashSet<TLocalDeclarationStatement>();
            foreach (var variableDeclarator in nodesToRemove.OfType<TVariableDeclarator>())
            {
                var localDeclaration = (TLocalDeclarationStatement)variableDeclarator.Parent.Parent;
                candidateLocalDeclarationsToRemove.Add(localDeclaration);
            }

            foreach (var candidate in candidateLocalDeclarationsToRemove)
            {
                var hasUsedLocal = false;
                foreach (var variable in GetVariables(candidate))
                {
                    if (!nodesToRemove.Contains(variable))
                    {
                        hasUsedLocal = true;
                        break;
                    }
                }

                if (!hasUsedLocal)
                {
                    nodesToRemove.Add(candidate);
                    foreach (var variable in GetVariables(candidate))
                    {
                        nodesToRemove.Remove(variable);
                    }
                }
            }
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
