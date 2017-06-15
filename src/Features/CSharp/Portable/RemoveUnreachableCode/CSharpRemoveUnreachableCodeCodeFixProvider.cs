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

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnreachableCode
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnreachableCode), Shared]
    internal class CSharpRemoveUnreachableCodeCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.RemoveUnreachableCodeDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            context.RegisterCodeFix(new MyCodeAction(
                FeaturesResources.Remove_unreachable_code,
                c => FixAsync(context.Document, diagnostic, c)), diagnostic);

            return SpecializedTasks.EmptyTask;
        }
        

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => !diagnostic.Properties.ContainsKey(CSharpRemoveUnreachableCodeDiagnosticAnalyzer.IsCascadedSection);

        protected override Task FixAllAsync(
            Document document, 
            ImmutableArray<Diagnostic> diagnostics, 
            SyntaxEditor editor, 
            CancellationToken cancellationToken)
        {
            var syntaxRoot = editor.OriginalRoot;

            foreach (var diagnostic in diagnostics)
            {
                var firstUnreachableStatementLocation = diagnostic.AdditionalLocations.Single();
                var firstUnreachableStatement = (StatementSyntax)firstUnreachableStatementLocation.FindNode(cancellationToken);

                var sections = RemoveUnreachableCodeHelpers.GetUnreachableSections(firstUnreachableStatement);
                foreach (var section in sections)
                {
                    foreach (var statement in section)
                    {
                        editor.RemoveNode(statement, SyntaxRemoveOptions.KeepUnbalancedDirectives);
                    }
                }
            }

            return SpecializedTasks.EmptyTask;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(title, createChangedDocument, title)
            {
            }
        }
    }
}