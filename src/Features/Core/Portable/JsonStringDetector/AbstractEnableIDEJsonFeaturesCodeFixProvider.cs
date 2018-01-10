// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.JsonStringDetector
{
    internal abstract class AbstractEnableIDEJsonFeaturesCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(IDEDiagnosticIds.JsonDetectionDiagnosticId);

        protected abstract void AddComment(SyntaxEditor editor, SyntaxToken stringLiteral, bool strict);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
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
                var stringLiteral = diagnostic.Location.FindToken(cancellationToken);
                Debug.Assert(syntaxFacts.IsStringLiteral(stringLiteral));

                var strict = diagnostic.Properties.ContainsKey(
                    AbstractJsonStringDetectorDiagnosticAnalyzer.StrictKey);
                AddComment(editor, stringLiteral, strict);
            }

            return SpecializedTasks.EmptyTask;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(FeaturesResources.Enable_JSON_editor_features, createChangedDocument)
            {
            }
        }
    }
}
