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
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseThrowExpression
{
    [ExportCodeFixProvider(LanguageNames.CSharp,
        Name = PredefinedCodeFixProviderNames.UseThrowExpression), Shared]
    internal partial class UseThrowExpressionCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseThrowExpressionDiagnosticId);

        public override FixAllProvider GetFixAllProvider()
            => new UseThrowExpressionFixAllProvider(this);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, diagnostic, c)),
                diagnostic);

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
                var ifStatement = root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan);
                var throwStatementExpression = root.FindNode(diagnostic.AdditionalLocations[1].SourceSpan);
                var assignmentValue = root.FindNode(diagnostic.AdditionalLocations[2].SourceSpan);

                // First, remote the if-statement entirely.
                editor.RemoveNode(ifStatement);

                // Now, update the assignemnt value to go from 'a' to 'a ?? throw ...'.
                editor.ReplaceNode(assignmentValue,
                    generator.CoalesceExpression(assignmentValue,
                    generator.ThrowExpression(throwStatementExpression)));
            }

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(
                Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(FeaturesResources.Use_throw_expression, createChangedDocument)
            {
            }
        }
    }
}