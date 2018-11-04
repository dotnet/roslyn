// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses
{
    internal abstract class AbstractRemoveUnnecessaryParenthesesCodeFixProvider<TConstruct>
        : SyntaxEditorBasedCodeFixProvider
        where TConstruct : SyntaxNode
    {
        private readonly string _kind;

        protected AbstractRemoveUnnecessaryParenthesesCodeFixProvider(string kind)
        {
            _kind = kind;
        }

        protected abstract ISyntaxFactsService GetSyntaxFactsService();

        protected abstract SyntaxNode Unparenthesize(TConstruct current);

        protected abstract bool CanRemoveParentheses(TConstruct current, SemanticModel semanticModel);

        public sealed override ImmutableArray<string> FixableDiagnosticIds
           => ImmutableArray.Create(IDEDiagnosticIds.RemoveUnnecessaryParenthesesDiagnosticId);

        protected sealed override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => diagnostic.Properties["Kind"] == _kind;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(
                    c => FixAsync(context.Document, context.Diagnostics[0], c)),
                    context.Diagnostics);
            return Task.CompletedTask;
        }

        protected sealed override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var originalNodes = diagnostics.SelectAsArray(
                d => (TConstruct)d.AdditionalLocations[0].FindNode(
                    findInsideTrivia: true, getInnermostNodeForTie: true, cancellationToken));

            return editor.ApplyExpressionLevelSemanticEditsAsync(
                document, originalNodes,
                (semanticModel, current) => current != null && CanRemoveParentheses(current, semanticModel),
                (_, currentRoot, current) => currentRoot.ReplaceNode(current, Unparenthesize(current)),
                cancellationToken);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Remove_unnecessary_parentheses, createChangedDocument, FeaturesResources.Remove_unnecessary_parentheses)
            {
            }
        }
    }
}
