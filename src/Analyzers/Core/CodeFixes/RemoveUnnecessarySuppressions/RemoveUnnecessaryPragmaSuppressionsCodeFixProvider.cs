// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryPragmaSuppressions), Shared]
    internal sealed class RemoveUnnecessaryPragmaSuppressionsCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public RemoveUnnecessaryPragmaSuppressionsCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId);

        internal override CodeFixCategory CodeFixCategory
            => CodeFixCategory.CodeQuality;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (root.FindTrivia(diagnostic.Location.SourceSpan.Start).HasStructure)
                {
                    context.RegisterCodeFix(
                        new MyCodeAction(c => FixAsync(context.Document, diagnostic, c)),
                        diagnostic);
                }
            }
        }

        protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var seenNodes = new HashSet<SyntaxNode>();
            foreach (var diagnostic in diagnostics)
            {
                RemoveNode(diagnostic.Location, editor, seenNodes);

                foreach (var location in diagnostic.AdditionalLocations)
                {
                    RemoveNode(location, editor, seenNodes);
                }
            }

            return Task.CompletedTask;

            static void RemoveNode(Location location, SyntaxEditor editor, HashSet<SyntaxNode> seenNodes)
            {
                var node = editor.OriginalRoot.FindTrivia(location.SourceSpan.Start).GetStructure()!;
                if (seenNodes.Add(node))
                {
                    editor.RemoveNode(node);
                }
            }
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(AnalyzersResources.Remove_unnecessary_suppression, createChangedDocument, nameof(RemoveUnnecessaryPragmaSuppressionsCodeFixProvider))
            {
            }
        }
    }
}
