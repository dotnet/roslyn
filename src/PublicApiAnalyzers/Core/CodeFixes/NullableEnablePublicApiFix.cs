// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using DiagnosticIds = Roslyn.Diagnostics.Analyzers.RoslynDiagnosticIds;

#nullable enable

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "NullableEnablePublicApiFix"), Shared]
    public sealed class NullableEnablePublicApiFix : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(DiagnosticIds.ShouldAnnotateApiFilesRuleId);

        public sealed override FixAllProvider GetFixAllProvider()
            => new PublicSurfaceAreaFixAllProvider();

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Project project = context.Document.Project;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                TextDocument? document = DeclarePublicApiFix.GetShippedDocument(project);

                if (document != null)
                {
                    context.RegisterCodeFix(
                            new DeclarePublicApiFix.AdditionalDocumentChangeAction(
                                $"Add '#nullable enable' to public API",
                                c => GetFix(document, c)),
                            diagnostic);
                }
            }

            return Task.CompletedTask;
        }

        private static async Task<Solution> GetFix(TextDocument publicSurfaceAreaDocument, CancellationToken cancellationToken)
        {
            SourceText sourceText = await publicSurfaceAreaDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            SourceText newSourceText = AddNullableEnable(sourceText);

            return publicSurfaceAreaDocument.Project.Solution.WithAdditionalDocumentText(publicSurfaceAreaDocument.Id, newSourceText);
        }

        private static SourceText AddNullableEnable(SourceText sourceText)
        {
            string extraLine = "#nullable enable" + Environment.NewLine;
            SourceText newSourceText = sourceText.WithChanges(new TextChange(new TextSpan(0, 0), extraLine));
            return newSourceText;
        }

        private class FixAllAdditionalDocumentChangeAction : CodeAction
        {
            private readonly List<KeyValuePair<Project, ImmutableArray<Diagnostic>>> _diagnosticsToFix;
            private readonly Solution _solution;

            public FixAllAdditionalDocumentChangeAction(string title, Solution solution, List<KeyValuePair<Project, ImmutableArray<Diagnostic>>> diagnosticsToFix)
            {
                this.Title = title;
                _solution = solution;
                _diagnosticsToFix = diagnosticsToFix;
            }

            public override string Title { get; }

            protected override async Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                var updatedPublicSurfaceAreaText = new List<KeyValuePair<DocumentId, SourceText>>();

                foreach (KeyValuePair<Project, ImmutableArray<Diagnostic>> pair in _diagnosticsToFix)
                {
                    Project project = pair.Key;
                    TextDocument? shippedDocument = DeclarePublicApiFix.GetShippedDocument(project);
                    SourceText? shippedSourceText = shippedDocument is null ? null : await shippedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    if (shippedSourceText is object)
                    {
                        SourceText newShippedSourceText = AddNullableEnable(shippedSourceText);
                        updatedPublicSurfaceAreaText.Add(new KeyValuePair<DocumentId, SourceText>(shippedDocument!.Id, newShippedSourceText));
                    }
                }

                Solution newSolution = _solution;
                foreach (KeyValuePair<DocumentId, SourceText> pair in updatedPublicSurfaceAreaText)
                {
                    newSolution = newSolution.WithAdditionalDocumentText(pair.Key, pair.Value);
                }

                return newSolution;
            }
        }

        private class PublicSurfaceAreaFixAllProvider : FixAllProvider
        {
            public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            {
                var diagnosticsToFix = new List<KeyValuePair<Project, ImmutableArray<Diagnostic>>>();
                string? title;
                switch (fixAllContext.Scope)
                {
                    case FixAllScope.Document:
                    case FixAllScope.Project:
                        {
                            Project project = fixAllContext.Project;
                            ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                            diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(fixAllContext.Project, diagnostics));
                            title = string.Format(CultureInfo.InvariantCulture, PublicApiAnalyzerResources.EnableNullableInProjectToThePublicApiTitle, fixAllContext.Project.Name);
                            break;
                        }

                    case FixAllScope.Solution:
                        {
                            foreach (Project project in fixAllContext.Solution.Projects)
                            {
                                ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                                diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(project, diagnostics));
                            }

                            title = PublicApiAnalyzerResources.EnableNullableInTheSolutionToThePublicApiTitle;
                            break;
                        }

                    case FixAllScope.Custom:
                        return null;

                    default:
                        Debug.Fail($"Unknown FixAllScope '{fixAllContext.Scope}'");
                        return null;
                }

                return new FixAllAdditionalDocumentChangeAction(title, fixAllContext.Solution, diagnosticsToFix);
            }
        }
    }
}
