// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.FixAllOccurrences;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseCoalesceExpression
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    internal class UseCoalesceExpressionCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseCoalesceExpressionDiagnosticId);

        public override FixAllProvider GetFixAllProvider() => new UseCoalesceExpressionFixAllProvider(this);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);
            return SpecializedTasks.EmptyTask;
        }

        private Task<Document> FixAsync(
            Document document,
            Diagnostic diagnostic,
            CancellationToken cancellationToken)
        {
            return FixAllAsync(document, ImmutableArray.Create(diagnostic), cancellationToken);
        }

        private async Task<Document> FixAllAsync(
            Document document,
            ImmutableArray<Diagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);
            var generator = editor.Generator;

            foreach (var diagnostic in diagnostics)
            {
                var conditionalExpression = root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan);
                var conditionalPart = root.FindNode(diagnostic.AdditionalLocations[1].SourceSpan);
                var whenPart = root.FindNode(diagnostic.AdditionalLocations[2].SourceSpan);

                editor.ReplaceNode(conditionalExpression,
                    generator.CoalesceExpression(conditionalPart, whenPart));
            }

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private class UseCoalesceExpressionFixAllProvider : DocumentBasedFixAllProvider
        {
            private readonly UseCoalesceExpressionCodeFixProvider _provider;

            public UseCoalesceExpressionFixAllProvider(UseCoalesceExpressionCodeFixProvider provider)
            {
                _provider = provider;
            }

            protected override Task<Document> FixDocumentAsync(
                Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
            {
                var filteredDiagnostics = diagnostics.WhereAsArray(
                    d => !d.Descriptor.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary));
                return _provider.FixAllAsync(document, filteredDiagnostics, cancellationToken);
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Use_coalesce_expression, createChangedDocument)
            {

            }
        }
    }
}