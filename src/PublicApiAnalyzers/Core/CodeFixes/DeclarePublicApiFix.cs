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

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var project = context.Document.Project;
            var publicSurfaceAreaDocument = GetUnshippedDocument(project);

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
                            c => GetFixAsync(publicSurfaceAreaDocument, project, publicSurfaceAreaSymbolName, siblingSymbolNamesToRemove, c)),
                        diagnostic);
            }

            return Task.CompletedTask;
        }

        internal static TextDocument? GetUnshippedDocument(Project project)
        {
            return project.AdditionalDocuments.FirstOrDefault(doc => doc.Name.Equals(DeclarePublicApiAnalyzer.UnshippedFileName, StringComparison.Ordinal));
        }

        internal static TextDocument? GetShippedDocument(Project project)
        {
            return project.AdditionalDocuments.FirstOrDefault(doc => doc.Name.Equals(DeclarePublicApiAnalyzer.ShippedFileName, StringComparison.Ordinal));
        }

        private static async Task<Solution> GetFixAsync(TextDocument? publicSurfaceAreaDocument, Project project, string newSymbolName, ImmutableHashSet<string> siblingSymbolNamesToRemove, CancellationToken cancellationToken)
        {
            if (publicSurfaceAreaDocument == null)
            {
                var newSourceText = AddSymbolNamesToSourceText(sourceText: null, new[] { newSymbolName });
                return AddPublicApiFiles(project, newSourceText);
            }
            else
            {
                var sourceText = await publicSurfaceAreaDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var newSourceText = AddSymbolNamesToSourceText(sourceText, new[] { newSymbolName });
                newSourceText = RemoveSymbolNamesFromSourceText(newSourceText, siblingSymbolNamesToRemove);

                return publicSurfaceAreaDocument.Project.Solution.WithAdditionalDocumentText(publicSurfaceAreaDocument.Id, newSourceText);
            }
        }

        private static Solution AddPublicApiFiles(Project project, SourceText unshippedText)
        {
            Debug.Assert(unshippedText.Length > 0);
            project = AddAdditionalDocument(project, DeclarePublicApiAnalyzer.ShippedFileName, SourceText.From(string.Empty));
            project = AddAdditionalDocument(project, DeclarePublicApiAnalyzer.UnshippedFileName, unshippedText);
            return project.Solution;

            // Local functions.
            static Project AddAdditionalDocument(Project project, string name, SourceText text)
            {
                TextDocument? additionalDocument = project.AdditionalDocuments.FirstOrDefault(doc => string.Equals(doc.Name, name, StringComparison.OrdinalIgnoreCase));
                if (additionalDocument == null)
                {
                    project = project.AddAdditionalDocument(name, text).Project;
                }

                return project;
            }
        }

        private static SourceText AddSymbolNamesToSourceText(SourceText? sourceText, IEnumerable<string> newSymbolNames)
        {
            List<string> lines = GetLinesFromSourceText(sourceText);

            foreach (string name in newSymbolNames)
            {
                insertInList(lines, name);
            }

            var endOfLine = Environment.NewLine;
            if (sourceText is not null && sourceText.Lines.Count > 1)
            {
                var firstLine = sourceText.Lines[0];
                endOfLine = sourceText.ToString(new TextSpan(firstLine.End, firstLine.EndIncludingLineBreak - firstLine.End));
            }

            var newText = string.Join(endOfLine, lines) + GetEndOfFileText(sourceText, endOfLine);
            return sourceText?.Replace(new TextSpan(0, sourceText.Length), newText) ?? SourceText.From(newText);

            // Insert name at the first suitable position
            static void insertInList(List<string> list, string name)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (IgnoreCaseWhenPossibleComparer.Instance.Compare(name, list[i]) < 0)
                    {
                        list.Insert(i, name);
                        return;
                    }
                }

                list.Add(name);
            }
        }

        private static SourceText RemoveSymbolNamesFromSourceText(SourceText sourceText, ImmutableHashSet<string> linesToRemove)
        {
            if (linesToRemove.IsEmpty)
            {
                return sourceText;
            }

            List<string> lines = GetLinesFromSourceText(sourceText);
            IEnumerable<string> newLines = lines.Where(line => !linesToRemove.Contains(line));

            var endOfLine = Environment.NewLine;
            if (sourceText.Lines.Count > 1)
            {
                var firstLine = sourceText.Lines[0];
                endOfLine = sourceText.ToString(new TextSpan(firstLine.End, firstLine.EndIncludingLineBreak - firstLine.End));
            }

            SourceText newSourceText = sourceText.Replace(new TextSpan(0, sourceText.Length), string.Join(endOfLine, newLines) + GetEndOfFileText(sourceText, endOfLine));
            return newSourceText;
        }

        internal static List<string> GetLinesFromSourceText(SourceText? sourceText)
        {
            if (sourceText == null)
            {
                return new List<string>();
            }

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
        /// Returns the trailing newline from the end of <paramref name="sourceText"/>, if one exists.
        /// </summary>
        /// <param name="sourceText">The source text.</param>
        /// <returns><see cref="Environment.NewLine"/> if <paramref name="sourceText"/> ends with a trailing newline;
        /// otherwise, <see cref="string.Empty"/>.</returns>
        public static string GetEndOfFileText(SourceText? sourceText, string endOfLine)
        {
            if (sourceText == null || sourceText.Length == 0)
                return string.Empty;

            var lastLine = sourceText.Lines[^1];
            return lastLine.Span.IsEmpty ? endOfLine : string.Empty;
        }

        internal class AdditionalDocumentChangeAction : CodeAction
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
                var addedPublicSurfaceAreaText = new List<KeyValuePair<ProjectId, SourceText>>();

                foreach (KeyValuePair<Project, ImmutableArray<Diagnostic>> pair in _diagnosticsToFix)
                {
                    Project project = pair.Key;
                    ImmutableArray<Diagnostic> diagnostics = pair.Value;

                    var publicSurfaceAreaAdditionalDocument = GetUnshippedDocument(project);

                    var sourceText = publicSurfaceAreaAdditionalDocument != null ?
                        await publicSurfaceAreaAdditionalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false) :
                        null;

                    IEnumerable<IGrouping<SyntaxTree, Diagnostic>> groupedDiagnostics =
                        diagnostics
                            .Where(d => d.Location.IsInSource)
                            .GroupBy(d => d.Location.SourceTree);

                    var newSymbolNames = new SortedSet<string>(IgnoreCaseWhenPossibleComparer.Instance);
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
                            if (diagnostic.Id == DeclarePublicApiAnalyzer.ShouldAnnotateApiFilesRule.Id ||
                                diagnostic.Id == DeclarePublicApiAnalyzer.ObliviousApiRule.Id)
                            {
                                continue;
                            }

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

                    if (publicSurfaceAreaAdditionalDocument != null)
                    {
                        updatedPublicSurfaceAreaText.Add(new KeyValuePair<DocumentId, SourceText>(publicSurfaceAreaAdditionalDocument.Id, newSourceText));
                    }
                    else if (newSourceText.Length > 0)
                    {
                        addedPublicSurfaceAreaText.Add(new KeyValuePair<ProjectId, SourceText>(project.Id, newSourceText));
                    }
                }

                Solution newSolution = _solution;

                foreach (KeyValuePair<DocumentId, SourceText> pair in updatedPublicSurfaceAreaText)
                {
                    newSolution = newSolution.WithAdditionalDocumentText(pair.Key, pair.Value);
                }

                // NOTE: We need to avoid creating duplicate files for multi-tfm projects. See https://github.com/dotnet/roslyn-analyzers/issues/3952.
                using var uniqueProjectPaths = PooledHashSet<string>.GetInstance();
                foreach (KeyValuePair<ProjectId, SourceText> pair in addedPublicSurfaceAreaText)
                {
                    var project = newSolution.GetProject(pair.Key);
                    if (uniqueProjectPaths.Add(project.FilePath ?? project.Name))
                    {
                        newSolution = AddPublicApiFiles(project, pair.Value);
                    }
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

        private sealed class IgnoreCaseWhenPossibleComparer : IComparer<string>
        {
            public static readonly IgnoreCaseWhenPossibleComparer Instance = new();

            private IgnoreCaseWhenPossibleComparer()
            {
            }

            public int Compare(string x, string y)
            {
                var result = StringComparer.OrdinalIgnoreCase.Compare(x, y);
                if (result == 0)
                    result = StringComparer.Ordinal.Compare(x, y);

                return result;
            }
        }
    }
}
