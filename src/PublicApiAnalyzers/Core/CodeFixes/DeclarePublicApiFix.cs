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
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using DiagnosticIds = Roslyn.Diagnostics.Analyzers.RoslynDiagnosticIds;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = "DeclarePublicApiFix"), Shared]
    public sealed class DeclarePublicApiFix : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.DeclarePublicApiRuleId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return new PublicSurfaceAreaFixAllProvider();
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Project project = context.Document.Project;
            TextDocument publicSurfaceAreaDocument = GetPublicSurfaceAreaDocument(project);
            if (publicSurfaceAreaDocument == null)
            {
                return;
            }

            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            SemanticModel semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                string minimalSymbolName = diagnostic.Properties[DeclarePublicApiAnalyzer.MinimalNamePropertyBagKey];
                string publicSurfaceAreaSymbolName = diagnostic.Properties[DeclarePublicApiAnalyzer.PublicApiNamePropertyBagKey];
                ImmutableHashSet<string> siblingSymbolNamesToRemove = diagnostic.Properties[DeclarePublicApiAnalyzer.PublicApiNamesOfSiblingsToRemovePropertyBagKey]
                    .Split(DeclarePublicApiAnalyzer.PublicApiNamesOfSiblingsToRemovePropertyBagValueSeparator.ToCharArray())
                    .ToImmutableHashSet();

                context.RegisterCodeFix(
                        new AdditionalDocumentChangeAction(
                            $"Add {minimalSymbolName} to public API",
                            c => GetFix(publicSurfaceAreaDocument, publicSurfaceAreaSymbolName, siblingSymbolNamesToRemove, c)),
                        diagnostic);
            }
        }

        private static TextDocument GetPublicSurfaceAreaDocument(Project project)
        {
            return project.AdditionalDocuments.FirstOrDefault(doc => doc.Name.Equals(DeclarePublicApiAnalyzer.UnshippedFileName, StringComparison.Ordinal));
        }

        private static async Task<Solution> GetFix(TextDocument publicSurfaceAreaDocument, string newSymbolName, ImmutableHashSet<string> siblingSymbolNamesToRemove, CancellationToken cancellationToken)
        {
            SourceText sourceText = await publicSurfaceAreaDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            SourceText newSourceText = AddSymbolNamesToSourceText(sourceText, new[] { newSymbolName });
            newSourceText = RemoveSymbolNamesFromSourceText(newSourceText, siblingSymbolNamesToRemove);

            return publicSurfaceAreaDocument.Project.Solution.WithAdditionalDocumentText(publicSurfaceAreaDocument.Id, newSourceText);
        }

        private static SourceText AddSymbolNamesToSourceText(SourceText sourceText, IEnumerable<string> newSymbolNames)
        {
            List<string> lines = GetLinesFromSourceText(sourceText);

            foreach (string name in newSymbolNames)
            {
                InsertInList(lines, name);
            }

            SourceText newSourceText = sourceText.Replace(new TextSpan(0, sourceText.Length), string.Join(Environment.NewLine, lines) + GetEndOfFileText(sourceText));
            return newSourceText;
        }

        private static SourceText RemoveSymbolNamesFromSourceText(SourceText sourceText, ImmutableHashSet<string> linesToRemove)
        {
            if (linesToRemove.IsEmpty)
            {
                return sourceText;
            }

            List<string> lines = GetLinesFromSourceText(sourceText);
            IEnumerable<string> newLines = lines.Where(line => !linesToRemove.Contains(line));

            SourceText newSourceText = sourceText.Replace(new TextSpan(0, sourceText.Length), string.Join(Environment.NewLine, newLines) + GetEndOfFileText(sourceText));
            return newSourceText;
        }

        private static List<string> GetLinesFromSourceText(SourceText sourceText)
        {
            var lines = new List<string>();

            foreach (TextLine textLine in sourceText.Lines)
            {
                string text = textLine.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    lines.Add(text);
                }
            }

            return lines;
        }

        /// <summary>
        /// Insert name at the first suitable position
        /// </summary>
        private static void InsertInList(List<string> list, string name)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Compare(name, list[i], StringComparison.Ordinal) < 0)
                {
                    list.Insert(i, name);
                    return;
                }
            }

            list.Add(name);
        }

        /// <summary>
        /// Returns the trailing newline from the end of <paramref name="sourceText"/>, if one exists.
        /// </summary>
        /// <param name="sourceText">The source text.</param>
        /// <returns><see cref="Environment.NewLine"/> if <paramref name="sourceText"/> ends with a trailing newline;
        /// otherwise, <see cref="string.Empty"/>.</returns>
        public static string GetEndOfFileText(SourceText sourceText)
        {
            if (sourceText.Length == 0)
                return string.Empty;

            var lastLine = sourceText.Lines[sourceText.Lines.Count - 1];
            return lastLine.Span.IsEmpty ? Environment.NewLine : string.Empty;
        }

        private class AdditionalDocumentChangeAction : CodeAction
        {
            private readonly Func<CancellationToken, Task<Solution>> _createChangedAdditionalDocument;

            public AdditionalDocumentChangeAction(string title, Func<CancellationToken, Task<Solution>> createChangedAdditionalDocument)
            {
                this.Title = title;
                _createChangedAdditionalDocument = createChangedAdditionalDocument;
            }

            public override string Title { get; }

            public override string EquivalenceKey => Title;

            protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                return _createChangedAdditionalDocument(cancellationToken);
            }
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

                    TextDocument publicSurfaceAreaAdditionalDocument = GetPublicSurfaceAreaDocument(project);

                    if (publicSurfaceAreaAdditionalDocument == null)
                    {
                        continue;
                    }

                    SourceText sourceText = await publicSurfaceAreaAdditionalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    IEnumerable<IGrouping<SyntaxTree, Diagnostic>> groupedDiagnostics =
                        diagnostics
                            .Where(d => d.Location.IsInSource)
                            .GroupBy(d => d.Location.SourceTree);

                    var newSymbolNames = new List<string>();
                    var symbolNamesToRemoveBuilder = PooledHashSet<string>.GetInstance();

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
                            string publicSurfaceAreaSymbolName = diagnostic.Properties[DeclarePublicApiAnalyzer.PublicApiNamePropertyBagKey];

                            newSymbolNames.Add(publicSurfaceAreaSymbolName);

                            string siblingNamesToRemove = diagnostic.Properties[DeclarePublicApiAnalyzer.PublicApiNamesOfSiblingsToRemovePropertyBagKey];
                            if (siblingNamesToRemove.Length > 0)
                            {
                                var namesToRemove = siblingNamesToRemove.Split(DeclarePublicApiAnalyzer.PublicApiNamesOfSiblingsToRemovePropertyBagValueSeparator.ToCharArray());
                                foreach (var nameToRemove in namesToRemove)
                                {
                                    symbolNamesToRemoveBuilder.Add(nameToRemove);
                                }
                            }
                        }
                    }

                    var symbolNamesToRemove = symbolNamesToRemoveBuilder.ToImmutableAndFree();

                    // We shouldn't be attempting to remove any symbol name, while also adding it.
                    Debug.Assert(newSymbolNames.All(newSymbolName => !symbolNamesToRemove.Contains(newSymbolName)));

                    SourceText newSourceText = AddSymbolNamesToSourceText(sourceText, newSymbolNames);
                    newSourceText = RemoveSymbolNamesFromSourceText(newSourceText, symbolNamesToRemove);

                    updatedPublicSurfaceAreaText.Add(new KeyValuePair<DocumentId, SourceText>(publicSurfaceAreaAdditionalDocument.Id, newSourceText));
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
            public override async Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
            {
                var diagnosticsToFix = new List<KeyValuePair<Project, ImmutableArray<Diagnostic>>>();
                string title = null;

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
                        break;
                }

                return new FixAllAdditionalDocumentChangeAction(title, fixAllContext.Solution, diagnosticsToFix);
            }
        }
    }
}
