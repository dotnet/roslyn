// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities.PooledObjects;
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
            private readonly List<Project> _projectsToFix;
            private readonly Solution _solution;

            public FixAllAdditionalDocumentChangeAction(string title, Solution solution, List<Project> projectsToFix)
            {
                this.Title = title;
                _solution = solution;
                _projectsToFix = projectsToFix;
            }

            public override string Title { get; }

            protected override async Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                var updatedPublicSurfaceAreaText = new List<KeyValuePair<DocumentId, SourceText>>();

                using var uniqueShippedDocuments = PooledHashSet<string>.GetInstance();
                foreach (var project in _projectsToFix)
                {
                    TextDocument? shippedDocument = DeclarePublicApiFix.GetShippedDocument(project);
                    if (shippedDocument == null ||
                        shippedDocument.FilePath != null && !uniqueShippedDocuments.Add(shippedDocument.FilePath))
                    {
                        // Skip past duplicate shipped documents.
                        // Multi-tfm projects can likely share the same api files, and we want to avoid duplicate code fix application.
                        continue;
                    }

                    var shippedSourceText = await shippedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    SourceText newShippedSourceText = AddNullableEnable(shippedSourceText);
                    updatedPublicSurfaceAreaText.Add(new KeyValuePair<DocumentId, SourceText>(shippedDocument!.Id, newShippedSourceText));
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
                var projectsToFix = new List<Project>();
                string? title;
                switch (fixAllContext.Scope)
                {
                    case FixAllScope.Document:
                    case FixAllScope.Project:
                        {
                            projectsToFix.Add(fixAllContext.Project);
                            title = string.Format(CultureInfo.InvariantCulture, PublicApiAnalyzerResources.EnableNullableInProjectToThePublicApiTitle, fixAllContext.Project.Name);
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

                            title = PublicApiAnalyzerResources.EnableNullableInTheSolutionToThePublicApiTitle;
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
