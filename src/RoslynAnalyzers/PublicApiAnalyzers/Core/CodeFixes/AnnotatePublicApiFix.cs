// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

using DiagnosticIds = Roslyn.Diagnostics.Analyzers.RoslynDiagnosticIds;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "AnnotatePublicApiFix"), Shared]
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    public sealed class AnnotatePublicApiFix() : CodeFixProvider
    {
        private const char ObliviousMarker = '~';

        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(DiagnosticIds.AnnotatePublicApiRuleId, DiagnosticIds.AnnotateInternalApiRuleId);

        public sealed override FixAllProvider GetFixAllProvider()
            => new PublicSurfaceAreaFixAllProvider();

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Project project = context.Document.Project;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                string? minimalSymbolName = diagnostic.Properties[DeclarePublicApiAnalyzer.MinimalNamePropertyBagKey];
                string? publicSymbolName = diagnostic.Properties[DeclarePublicApiAnalyzer.ApiNamePropertyBagKey];
                string? publicSymbolNameWithNullability = diagnostic.Properties[DeclarePublicApiAnalyzer.ApiNameWithNullabilityPropertyBagKey];
                string? fileName = diagnostic.Properties[DeclarePublicApiAnalyzer.FileName];

                if (minimalSymbolName is null ||
                    publicSymbolName is null ||
                    publicSymbolNameWithNullability is null ||
                    fileName is null)
                {
                    // If any of the required properties are missing, we cannot register a fix.
                    continue;
                }

                TextDocument? document = project.GetPublicApiDocument(fileName);

                if (document != null)
                {
                    context.RegisterCodeFix(
                            new DeclarePublicApiFix.AdditionalDocumentChangeAction(
                                $"Annotate {minimalSymbolName} in public API",
                                document.Id,
                                isPublic: diagnostic.Id == DiagnosticIds.AnnotatePublicApiRuleId,
                                c => GetFixAsync(document, publicSymbolName, publicSymbolNameWithNullability, c)),
                            diagnostic);
                }
            }

            return Task.CompletedTask;

            static async Task<Solution?> GetFixAsync(TextDocument publicSurfaceAreaDocument, string oldSymbolName, string newSymbolName, CancellationToken cancellationToken)
            {
                SourceText sourceText = await publicSurfaceAreaDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                SourceText newSourceText = AnnotateSymbolNamesInSourceText(sourceText, new Dictionary<string, string> { [oldSymbolName] = newSymbolName });

                return publicSurfaceAreaDocument.Project.Solution.WithAdditionalDocumentText(publicSurfaceAreaDocument.Id, newSourceText);
            }
        }

        private static SourceText AnnotateSymbolNamesInSourceText(SourceText sourceText, Dictionary<string, string> changes)
        {
            if (changes.Count == 0)
            {
                return sourceText;
            }

            List<string> lines = DeclarePublicApiFix.GetLinesFromSourceText(sourceText);

            for (int i = 0; i < lines.Count; i++)
            {
                if (changes.TryGetValue(lines[i].Trim(ObliviousMarker), out string newLine))
                {
                    lines.Insert(i, newLine);
                    lines.RemoveAt(i + 1);
                }
            }

            var endOfLine = sourceText.GetEndOfLine();
            SourceText newSourceText = sourceText.Replace(new TextSpan(0, sourceText.Length), string.Join(endOfLine, lines) + sourceText.GetEndOfFileText(endOfLine));
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

            protected override async Task<Solution?> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                var updatedPublicSurfaceAreaText = new List<(DocumentId, SourceText)>();

                foreach (var (project, diagnostics) in _diagnosticsToFix)
                {
                    IEnumerable<IGrouping<SyntaxTree?, Diagnostic>> groupedDiagnostics =
                        diagnostics
                            .Where(d => d.Location.IsInSource)
                            .GroupBy(d => d.Location.SourceTree);

                    var allChanges = new Dictionary<string, Dictionary<string, string>>();

                    foreach (IGrouping<SyntaxTree?, Diagnostic> grouping in groupedDiagnostics)
                    {
                        Document? document = project.GetDocument(grouping.Key);

                        if (document is null)
                        {
                            continue;
                        }

                        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                        SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                        foreach (Diagnostic diagnostic in grouping)
                        {
                            switch (diagnostic.Id)
                            {
                                case DiagnosticIds.AnnotateInternalApiRuleId:
                                case DiagnosticIds.AnnotatePublicApiRuleId:
                                    break;
                                default:
                                    continue;
                            }

                            string? oldName = diagnostic.Properties[DeclarePublicApiAnalyzer.ApiNamePropertyBagKey];
                            string? newName = diagnostic.Properties[DeclarePublicApiAnalyzer.ApiNameWithNullabilityPropertyBagKey];
                            bool isShipped = diagnostic.Properties[DeclarePublicApiAnalyzer.ApiIsShippedPropertyBagKey] == "true";
                            string? fileName = diagnostic.Properties[DeclarePublicApiAnalyzer.FileName];

                            if (oldName is null ||
                                newName is null ||
                                fileName is null)
                            {
                                continue;
                            }

                            if (!allChanges.TryGetValue(fileName, out var mapToUpdate))
                            {
                                mapToUpdate = [];
                                allChanges.Add(fileName, mapToUpdate);
                            }

                            mapToUpdate[oldName] = newName;
                        }
                    }

                    foreach (var (path, changes) in allChanges)
                    {
                        var doc = project.GetPublicApiDocument(path);

                        if (doc is not null)
                        {
                            var text = await doc.GetTextAsync(cancellationToken).ConfigureAwait(false);
                            SourceText newShippedSourceText = AnnotateSymbolNamesInSourceText(text, changes);
                            updatedPublicSurfaceAreaText.Add((doc.Id, newShippedSourceText));
                        }
                    }
                }

                Solution newSolution = _solution;

                foreach (var (docId, text) in updatedPublicSurfaceAreaText)
                {
                    newSolution = newSolution.WithAdditionalDocumentText(docId, text);
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
                        {
                            Document document = fixAllContext.Document!;
                            ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
                            diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(fixAllContext.Project, diagnostics));
                            title = string.Format(CultureInfo.InvariantCulture, PublicApiAnalyzerResources.AddAllItemsInDocumentToTheApiTitle, document.Name);
                            break;
                        }

                    case FixAllScope.Project:
                        {
                            Project project = fixAllContext.Project;
                            ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                            diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(fixAllContext.Project, diagnostics));
                            title = string.Format(CultureInfo.InvariantCulture, PublicApiAnalyzerResources.AddAllItemsInProjectToTheApiTitle, fixAllContext.Project.Name);
                            break;
                        }

                    case FixAllScope.Solution:
                        {
                            foreach (Project project in fixAllContext.Solution.Projects)
                            {
                                ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                                diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(project, diagnostics));
                            }

                            title = PublicApiAnalyzerResources.AddAllItemsInTheSolutionToTheApiTitle;
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
