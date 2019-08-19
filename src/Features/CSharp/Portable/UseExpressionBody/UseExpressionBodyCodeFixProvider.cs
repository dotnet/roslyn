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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal partial class UseExpressionBodyCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; }

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        private static readonly ImmutableArray<UseExpressionBodyHelper> _helpers = UseExpressionBodyHelper.Helpers;

        [ImportingConstructor]
        public UseExpressionBodyCodeFixProvider()
        {
            FixableDiagnosticIds = _helpers.SelectAsArray(h => h.DiagnosticId);
        }

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => !diagnostic.IsSuppressed ||
               diagnostic.Properties.ContainsKey(UseExpressionBodyDiagnosticAnalyzer.FixesError);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var documentOptionSet = await context.Document.GetOptionsAsync(context.CancellationToken).ConfigureAwait(false);

            var priority = diagnostic.Severity == DiagnosticSeverity.Hidden
                ? CodeActionPriority.Low
                : CodeActionPriority.Medium;

            context.RegisterCodeFix(
                new MyCodeAction(diagnostic.GetMessage(), priority, c => FixAsync(context.Document, diagnostic, c)),
                diagnostic);
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var accessorLists = new HashSet<AccessorListSyntax>();
            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddEdits(semanticModel, editor, diagnostic, accessorLists, cancellationToken);
            }

            // Ensure that if we changed any accessors that the accessor lists they're contained
            // in are formatted properly as well.  Do this as a last pass so that we see all
            // individual changes made to the child accessors if we're doing a fix-all.
            foreach (var accessorList in accessorLists)
            {
                editor.ReplaceNode(accessorList, (current, _) => current.WithAdditionalAnnotations(Formatter.Annotation));
            }
        }

        private void AddEdits(
            SemanticModel semanticModel, SyntaxEditor editor, Diagnostic diagnostic,
            HashSet<AccessorListSyntax> accessorLists,
            CancellationToken cancellationToken)
        {
            var declarationLocation = diagnostic.AdditionalLocations[0];
            var helper = _helpers.Single(h => h.DiagnosticId == diagnostic.Id);
            var declaration = declarationLocation.FindNode(cancellationToken);
            var useExpressionBody = diagnostic.Properties.ContainsKey(nameof(UseExpressionBody));

            var updatedDeclaration = helper.Update(semanticModel, declaration, useExpressionBody)
                                           .WithAdditionalAnnotations(Formatter.Annotation);

            editor.ReplaceNode(declaration, updatedDeclaration);

            if (declaration.Parent is AccessorListSyntax accessorList)
            {
                accessorLists.Add(accessorList);
            }
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
