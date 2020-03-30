// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

#nullable enable

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "AnnotatePublicApiFix"), Shared]
    public sealed class AnnotatePublicApiFix : CodeFixProvider
    {
        private const char ObliviousMarker = '~';

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(DiagnosticIds.AnnotatePublicApiRuleId);

        public sealed override FixAllProvider GetFixAllProvider()
            => new PublicSurfaceAreaFixAllProvider();

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Project project = context.Document.Project;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                string minimalSymbolName = diagnostic.Properties[DeclarePublicApiAnalyzer.MinimalNamePropertyBagKey];
                string publicSymbolName = diagnostic.Properties[DeclarePublicApiAnalyzer.PublicApiNamePropertyBagKey];
                string publicSymbolNameWithNullability = diagnostic.Properties[DeclarePublicApiAnalyzer.PublicApiNameWithNullabilityPropertyBagKey];
                bool isShippedDocument = diagnostic.Properties[DeclarePublicApiAnalyzer.PublicApiIsShippedPropertyBagKey] == "true";

                TextDocument? document = isShippedDocument ? DeclarePublicApiFix.GetShippedDocument(project) : DeclarePublicApiFix.GetUnshippedDocument(project);

                if (document != null)
                {
                    context.RegisterCodeFix(
                            new DeclarePublicApiFix.AdditionalDocumentChangeAction(
                                $"Annotate {minimalSymbolName} in public API",
                                c => GetFix(document, publicSymbolName, publicSymbolNameWithNullability, c)),
                            diagnostic);
                }
            }

            return Task.CompletedTask;

            static async Task<Solution> GetFix(TextDocument publicSurfaceAreaDocument, string oldSymbolName, string newSymbolName, CancellationToken cancellationToken)
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

            SourceText newSourceText = sourceText.Replace(new TextSpan(0, sourceText.Length), string.Join(Environment.NewLine, lines) + DeclarePublicApiFix.GetEndOfFileText(sourceText));
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
                    ImmutableArray<Diagnostic> diagnostics = pair.Value;

                    TextDocument? unshippedDocument = DeclarePublicApiFix.GetUnshippedDocument(project);
                    TextDocument? shippedDocument = DeclarePublicApiFix.GetShippedDocument(project);

                    SourceText? unshippedSourceText = unshippedDocument is null ? null : await unshippedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    SourceText? shippedSourceText = shippedDocument is null ? null : await shippedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    IEnumerable<IGrouping<SyntaxTree, Diagnostic>> groupedDiagnostics =
                        diagnostics
                            .Where(d => d.Location.IsInSource)
                            .GroupBy(d => d.Location.SourceTree);

                    var shippedChanges = new Dictionary<string, string>();
                    var unshippedChanges = new Dictionary<string, string>();

                    foreach (IGrouping<SyntaxTree, Diagnostic> grouping in groupedDiagnostics)
                    {
                        Document document = project.GetDocument(grouping.Key);

                        if (document == null)
                        {
                            continue;
                        }

                        SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                        SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                        foreach (Diagnostic diagnostic in grouping)
                        {
                            if (diagnostic.Id != DeclarePublicApiAnalyzer.AnnotateApiRule.Id)
                            {
                                continue;
                            }

                            string oldName = diagnostic.Properties[DeclarePublicApiAnalyzer.PublicApiNamePropertyBagKey];
                            string newName = diagnostic.Properties[DeclarePublicApiAnalyzer.PublicApiNameWithNullabilityPropertyBagKey];
                            bool isShipped = diagnostic.Properties[DeclarePublicApiAnalyzer.PublicApiIsShippedPropertyBagKey] == "true";
                            (isShipped ? shippedChanges : unshippedChanges).Add(oldName, newName);
                        }
                    }

                    if (shippedSourceText is object)
                    {
                        SourceText newShippedSourceText = AnnotateSymbolNamesInSourceText(shippedSourceText, shippedChanges);
                        updatedPublicSurfaceAreaText.Add(new KeyValuePair<DocumentId, SourceText>(shippedDocument!.Id, newShippedSourceText));
                    }

                    if (unshippedSourceText is object)
                    {
                        SourceText newUnshippedSourceText = AnnotateSymbolNamesInSourceText(unshippedSourceText, unshippedChanges);
                        updatedPublicSurfaceAreaText.Add(new KeyValuePair<DocumentId, SourceText>(unshippedDocument!.Id, newUnshippedSourceText));
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
                        {
                            ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(fixAllContext.Document).ConfigureAwait(false);
                            diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(fixAllContext.Project, diagnostics));
                            title = string.Format(CultureInfo.InvariantCulture, PublicApiAnalyzerResources.AddAllItemsInDocumentToThePublicApiTitle, fixAllContext.Document.Name);
                            break;
                        }

                    case FixAllScope.Project:
                        {
                            Project project = fixAllContext.Project;
                            ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                            diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(fixAllContext.Project, diagnostics));
                            title = string.Format(CultureInfo.InvariantCulture, PublicApiAnalyzerResources.AddAllItemsInProjectToThePublicApiTitle, fixAllContext.Project.Name);
                            break;
                        }

                    case FixAllScope.Solution:
                        {
                            foreach (Project project in fixAllContext.Solution.Projects)
                            {
                                ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                                diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(project, diagnostics));
                            }

                            title = PublicApiAnalyzerResources.AddAllItemsInTheSolutionToThePublicApiTitle;
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
