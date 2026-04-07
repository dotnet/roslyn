// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using DiagnosticIds = Roslyn.Diagnostics.Analyzers.RoslynDiagnosticIds;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "NullableEnablePublicApiFix"), Shared]
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    public sealed class NullableEnablePublicApiFix() : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(DiagnosticIds.ShouldAnnotatePublicApiFilesRuleId, DiagnosticIds.ShouldAnnotateInternalApiFilesRuleId);

        public sealed override FixAllProvider GetFixAllProvider()
            => new PublicSurfaceAreaFixAllProvider();

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Project project = context.Document.Project;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                var isPublic = diagnostic.Id == DiagnosticIds.ShouldAnnotatePublicApiFilesRuleId;
                TextDocument? document = project.GetShippedDocument(isPublic);

                if (document != null)
                {
                    context.RegisterCodeFix(
                            new DeclarePublicApiFix.AdditionalDocumentChangeAction(
                                $"Add '#nullable enable' to {(isPublic ? "public" : "internal")} API",
                                document.Id,
                                isPublic,
                                c => GetFixAsync(document, c)),
                            diagnostic);
                }
            }

            return Task.CompletedTask;
        }

        private static async Task<Solution?> GetFixAsync(TextDocument surfaceAreaDocument, CancellationToken cancellationToken)
        {
            SourceText sourceText = await surfaceAreaDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            SourceText newSourceText = AddNullableEnable(sourceText);

            return surfaceAreaDocument.Project.Solution.WithAdditionalDocumentText(surfaceAreaDocument.Id, newSourceText);
        }

        private static SourceText AddNullableEnable(SourceText sourceText)
        {
            string extraLine = "#nullable enable" + sourceText.GetEndOfLine();
            SourceText newSourceText = sourceText.WithChanges(new TextChange(new TextSpan(0, 0), extraLine));
            return newSourceText;
        }

        private class FixAllAdditionalDocumentChangeAction : CodeAction
        {
            private readonly List<Project> _projectsToFix;
            private readonly Solution _solution;

            public FixAllAdditionalDocumentChangeAction(string title, Solution solution, List<Project> projectsToFix)
            {
                this.Title = title;
                _solution = solution;
                _projectsToFix = projectsToFix;
            }

            public override string Title { get; }

            protected override async Task<Solution?> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                var updatedSurfaceAreaText = new List<(DocumentId, SourceText)>();

                using var _ = PooledHashSet<string>.GetInstance(out var uniqueShippedDocuments);
                foreach (var project in _projectsToFix)
                {
                    foreach (var isPublic in new[] { true, false })
                    {
                        TextDocument? shippedDocument = project.GetShippedDocument(isPublic);
                        if (shippedDocument == null ||
                            shippedDocument.FilePath != null && !uniqueShippedDocuments.Add(shippedDocument.FilePath))
                        {
                            // Skip past duplicate shipped documents.
                            // Multi-tfm projects can likely share the same api files, and we want to avoid duplicate code fix application.
                            continue;
                        }

                        var shippedSourceText = await shippedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                        SourceText newShippedSourceText = AddNullableEnable(shippedSourceText);
                        updatedSurfaceAreaText.Add((shippedDocument.Id, newShippedSourceText));
                    }
                }

                Solution newSolution = _solution;
                foreach (var (document, text) in updatedSurfaceAreaText)
                {
                    newSolution = newSolution.WithAdditionalDocumentText(document, text);
                }

                return newSolution;
            }
        }

        private class PublicSurfaceAreaFixAllProvider : FixAllProvider
        {
            public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            {
                var projectsToFix = new List<Project>();
                string? title;
                switch (fixAllContext.Scope)
                {
                    case FixAllScope.Document:
                    case FixAllScope.Project:
                        {
                            projectsToFix.Add(fixAllContext.Project);
                            title = string.Format(CultureInfo.InvariantCulture, PublicApiAnalyzerResources.EnableNullableInProjectToTheApiTitle, fixAllContext.Project.Name);
                            break;
                        }

                    case FixAllScope.Solution:
                        {
                            foreach (Project project in fixAllContext.Solution.Projects)
                            {
                                ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                                if (!diagnostics.IsEmpty)
                                {
                                    projectsToFix.Add(project);
                                }
                            }

                            title = PublicApiAnalyzerResources.EnableNullableInTheSolutionToTheApiTitle;
                            break;
                        }

                    case FixAllScope.Custom:
                        return null;

                    default:
                        Debug.Fail($"Unknown FixAllScope '{fixAllContext.Scope}'");
                        return null;
                }

                return new FixAllAdditionalDocumentChangeAction(title, fixAllContext.Solution, projectsToFix);
            }
        }
    }
}
