// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddRequiredParentheses
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    internal class AddRequiredParenthesesCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.AddRequiredParenthesesDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(
                    c => FixAsync(context.Document, context.Diagnostics[0], c)),
                    context.Diagnostics);
            return SpecializedTasks.EmptyTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            foreach (var diagnostic in diagnostics)
            {
                var location = diagnostic.AdditionalLocations[0];
                var binaryExpression = location.FindNode(findInsideTrivia: true, getInnermostNodeForTie: true, cancellationToken: cancellationToken);
                editor.ReplaceNode(binaryExpression,
                    (current, _) => syntaxFacts.Parenthesize(current, includeElasticTrivia: false, addSimplifierAnnotation: false));
            }

            return SpecializedTasks.EmptyTask;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Add_parentheses_for_clarity, createChangedDocument, FeaturesResources.Add_parentheses_for_clarity)
            {
            }
        }
    }
}
