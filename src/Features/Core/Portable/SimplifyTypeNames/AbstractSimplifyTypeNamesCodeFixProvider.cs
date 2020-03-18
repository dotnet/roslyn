﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SimplifyTypeNames
{
    internal abstract partial class AbstractSimplifyTypeNamesCodeFixProvider<TSyntaxKind>
        : SyntaxEditorBasedCodeFixProvider
        where TSyntaxKind : struct
    {
        private readonly SimplifyTypeNamesDiagnosticAnalyzerBase<TSyntaxKind> _analyzer;

        protected AbstractSimplifyTypeNamesCodeFixProvider(
            SimplifyTypeNamesDiagnosticAnalyzerBase<TSyntaxKind> analyzer)
        {
            _analyzer = analyzer;
        }

        protected abstract string GetTitle(string diagnosticId, string nodeText);
        protected abstract SyntaxNode AddSimplificationAnnotationTo(SyntaxNode node);

        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(
                IDEDiagnosticIds.SimplifyNamesDiagnosticId,
                IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId,
                IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        private (SyntaxNode, string diagnosticId) GetNodeToSimplify(
            SyntaxNode root, SemanticModel model, TextSpan span,
            OptionSet optionSet, CancellationToken cancellationToken)
        {
            var token = root.FindToken(span.Start, findInsideTrivia: true);
            if (!token.Span.IntersectsWith(span))
            {
                return default;
            }

            SyntaxNode topmostSimplifiableNode = null;
            string topmostDiagnosticId = null;
            foreach (var node in token.GetAncestors<SyntaxNode>())
            {
                if (node.Span.IntersectsWith(span) && CanSimplifyTypeNameExpression(model, node, optionSet, span, out var diagnosticId, cancellationToken))
                {
                    // keep overwriting the best simplifiable node as long as we keep finding them.
                    topmostSimplifiableNode = node;
                    topmostDiagnosticId = diagnosticId;
                }
                else if (topmostSimplifiableNode != null)
                {
                    // if we have found something simplifiable, but hit something that isn't, then
                    // return the best thing we've found.
                    break;
                }
            }

            return (topmostSimplifiableNode, topmostDiagnosticId);
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var (node, diagnosticId) = GetNodeToSimplify(
                root, model, span, documentOptions, cancellationToken);
            if (node == null)
                return;

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var title = GetTitle(diagnosticId, syntaxFacts.ConvertToSingleLine(node).ToString());

            context.RegisterCodeFix(new MyCodeAction(
                title,
                c => FixAsync(context.Document, context.Diagnostics[0], c),
                diagnosticId), context.Diagnostics);
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in diagnostics)
            {
                var (node, _) = GetNodeToSimplify(
                    root, model, diagnostic.Location.SourceSpan,
                    documentOptions, cancellationToken);

                if (node == null)
                    return;

                editor.ReplaceNode(
                    node,
                    (current, _) => AddSimplificationAnnotationTo(current));
            }
        }

        private bool CanSimplifyTypeNameExpression(SemanticModel model, SyntaxNode node, OptionSet optionSet, TextSpan span, out string diagnosticId, CancellationToken cancellationToken)
        {
            diagnosticId = null;
            if (!_analyzer.IsCandidate(node) ||
                !_analyzer.CanSimplifyTypeNameExpression(
                    model, node, optionSet, out var issueSpan, out diagnosticId, out _, cancellationToken))
            {
                return false;
            }

            return issueSpan.Equals(span);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(
                string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey)
                : base(title, createChangedDocument, equivalenceKey)
            {
            }
        }
    }
}
