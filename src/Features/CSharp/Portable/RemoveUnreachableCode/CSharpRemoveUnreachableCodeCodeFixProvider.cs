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

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnreachableCode
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnreachableCode), Shared]
    internal class CSharpRemoveUnreachableCodeCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        public CSharpRemoveUnreachableCodeCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.RemoveUnreachableCodeDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeQuality;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];

            // Only the first reported unreacha ble line will have a squiggle.  On that line, make the
            // code action normal priority as the user is likely bringing up the lightbulb to fix the
            // squiggle.  On all the other lines make the code action low priority as it's definitely
            // helpful, but shouldn't interfere with anything else the uesr is doing.
            var priority = IsSubsequentSection(diagnostic)
                ? CodeActionPriority.Low
                : CodeActionPriority.Medium;

            context.RegisterCodeFix(new MyCodeAction(
                FeaturesResources.Remove_unreachable_code,
                c => FixAsync(context.Document, diagnostic, c),
                priority), diagnostic);

            return Task.CompletedTask;
        }

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => !IsSubsequentSection(diagnostic);

        private static bool IsSubsequentSection(Diagnostic diagnostic)
            => diagnostic.Properties.ContainsKey(CSharpRemoveUnreachableCodeDiagnosticAnalyzer.IsSubsequentSection);

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

                editor.RemoveNode(firstUnreachableStatement, SyntaxRemoveOptions.KeepUnbalancedDirectives);

                var sections = RemoveUnreachableCodeHelpers.GetSubsequentUnreachableSections(firstUnreachableStatement);
                foreach (var section in sections)
                {
                    foreach (var statement in section)
                    {
                        editor.RemoveNode(statement, SyntaxRemoveOptions.KeepUnbalancedDirectives);
                    }
                }
            }

            return Task.CompletedTask;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(
                string title,
                Func<CancellationToken, Task<Document>> createChangedDocument,
                CodeActionPriority priority)
                : base(title, createChangedDocument, title)
            {
                this.Priority = priority;
            }

            internal override CodeActionPriority Priority { get; }
        }
    }
}
