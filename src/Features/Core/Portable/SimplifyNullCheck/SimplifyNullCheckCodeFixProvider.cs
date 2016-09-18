// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

namespace Microsoft.CodeAnalysis.SimplifyNullCheck
{
    [ExportCodeFixProvider(LanguageNames.CSharp,
        Name = PredefinedCodeFixProviderNames.SimplifyNullCheck), Shared]
    internal class SimplifyNullCheckCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.SimplifyNullCheckDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, diagnostic, c)),
                diagnostic);

            return SpecializedTasks.EmptyTask;
        }

        private async Task<Document> FixAsync(
            Document document, 
            Diagnostic diagnostic, 
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var ifStatement = root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan);
            var throwStatementExpression = root.FindNode(diagnostic.AdditionalLocations[1].SourceSpan);
            var assignmentValue = root.FindNode(diagnostic.AdditionalLocations[2].SourceSpan);

            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);
            var generator = editor.Generator;

            // First, remote the if-statement entirely.
            editor.RemoveNode(ifStatement);

            // Now, update the assignemnt value to go from 'a' to 'a ?? throw ...'.
            editor.ReplaceNode(assignmentValue,
                generator.CoalesceExpression(assignmentValue,
                generator.ThrowExpression(throwStatementExpression)));

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(
                Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(FeaturesResources.Simplify_null_check, createChangedDocument, FeaturesResources.Simplify_null_check)
            {
            }
        }
    }
}
