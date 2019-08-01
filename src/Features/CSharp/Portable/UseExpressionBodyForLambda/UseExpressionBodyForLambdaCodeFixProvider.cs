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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda
{
    using static UseExpressionBodyForLambdaHelpers;

    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal sealed partial class UseExpressionBodyForLambdaCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        public UseExpressionBodyForLambdaCodeFixProvider()
        {
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.UseExpressionBodyForLambdaExpressionsDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => !diagnostic.IsSuppressed ||
               diagnostic.Properties.ContainsKey(UseExpressionBodyForLambdaDiagnosticAnalyzer.FixesError);

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();

            var priority = diagnostic.Severity == DiagnosticSeverity.Hidden
                ? CodeActionPriority.Low
                : CodeActionPriority.Medium;

            context.RegisterCodeFix(
                new MyCodeAction(diagnostic.GetMessage(), priority, c => FixAsync(context.Document, diagnostic, c)),
                diagnostic);

            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddEdits(editor, semanticModel, diagnostic, cancellationToken);
            }
        }

        private void AddEdits(
            SyntaxEditor editor, SemanticModel semanticModel,
            Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var declarationLocation = diagnostic.AdditionalLocations[0];
            var originalDeclaration = (LambdaExpressionSyntax)declarationLocation.FindNode(getInnermostNodeForTie: true, cancellationToken);
            var useExpressionBody = diagnostic.Properties.ContainsKey(nameof(UseExpressionBody));

            editor.ReplaceNode(
                originalDeclaration,
                (currentDeclaration, g) => Update(semanticModel, useExpressionBody, originalDeclaration, (LambdaExpressionSyntax)currentDeclaration));
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            internal override CodeActionPriority Priority { get; }

            public MyCodeAction(string title, CodeActionPriority priority, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
                this.Priority = priority;
            }
        }
    }
}
