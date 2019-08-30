// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpIsAndCastCheckWithoutNameCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        public CSharpIsAndCastCheckWithoutNameCodeFixProvider()
            : base(supportsFixAll: false)
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.InlineIsTypeWithoutNameCheckDiagnosticsId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            Debug.Assert(diagnostics.Length == 1);
            var location = diagnostics[0].Location;
            var isExpression = (BinaryExpressionSyntax)location.FindNode(
                getInnermostNodeForTie: true, cancellationToken: cancellationToken);

            var workspace = document.Project.Solution.Workspace;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var (matches, localName) = CSharpIsAndCastCheckWithoutNameDiagnosticAnalyzer.Instance.AnalyzeExpression(
                workspace, semanticModel, isExpression, cancellationToken);

            var updatedSemanticModel = CSharpIsAndCastCheckWithoutNameDiagnosticAnalyzer.Instance.ReplaceMatches(
                workspace, semanticModel, isExpression, localName, matches, cancellationToken);

            var updatedRoot = updatedSemanticModel.SyntaxTree.GetRoot(cancellationToken);
            editor.ReplaceNode(editor.OriginalRoot, updatedRoot);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Use_pattern_matching, createChangedDocument)
            {
            }

            internal override CodeActionPriority Priority => CodeActionPriority.Low;
        }
    }
}
