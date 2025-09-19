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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Diagnostics.Analyzers;
using DiagnosticIds = Roslyn.Diagnostics.Analyzers.RoslynDiagnosticIds;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = "DeclarePublicApiFix"), Shared]
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    public sealed class DeclarePublicApiFix() : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(DiagnosticIds.DeclarePublicApiRuleId, DiagnosticIds.DeclareInternalApiRuleId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return new PublicSurfaceAreaFixAllProvider();
        }

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var project = context.Document.Project;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                bool isPublic = diagnostic.Id == DiagnosticIds.DeclarePublicApiRuleId;
                string? minimalSymbolName = diagnostic.Properties[DeclarePublicApiAnalyzer.MinimalNamePropertyBagKey];
                string? publicSurfaceAreaSymbolName = diagnostic.Properties[DeclarePublicApiAnalyzer.ApiNamePropertyBagKey];
                string? siblingsToRemoveSymbolNames = diagnostic.Properties[DeclarePublicApiAnalyzer.ApiNamesOfSiblingsToRemovePropertyBagKey];
                if (minimalSymbolName == null || publicSurfaceAreaSymbolName == null || siblingsToRemoveSymbolNames == null)
                {
                    continue;
                }

                ImmutableHashSet<string> siblingSymbolNamesToRemove = siblingsToRemoveSymbolNames
                    .Split(DeclarePublicApiAnalyzer.ApiNamesOfSiblingsToRemovePropertyBagValueSeparator.ToCharArray())
                    .ToImmutableHashSet();

                foreach (var file in GetUnshippedPublicApiFiles(context.Document.Project, isPublic))
                {
                    context.RegisterCodeFix(
                            new AdditionalDocumentChangeAction(
                                $"Add {minimalSymbolName} to API file {file?.Name}",
                                file?.Id,
                                isPublic,
                                c => GetFixAsync(file, isPublic, project, publicSurfaceAreaSymbolName, siblingSymbolNamesToRemove, c)),
                            diagnostic);
                }
            }

            return Task.CompletedTask;
        }

        private static IEnumerable<TextDocument?> GetUnshippedPublicApiFiles(Project project, bool isPublic)
        {
            var count = 0;

            foreach (var additional in project.AdditionalDocuments)
            {
                if (additional.FilePath == null)
                {
                    // Skip documents without a file path, as they cannot be public API files.
                    continue;
                }

                var file = new PublicApiFile(additional.FilePath, isPublic);

                if (file.IsApiFile && !file.IsShipping)
                {
                    yield return additional;
                    count++;
                }
            }

            if (count == 0)
            {
                yield return null;
            }
        }

        private static async Task<Solution?> GetFixAsync(TextDocument? surfaceAreaDocument, bool isPublic, Project project, string newSymbolName, ImmutableHashSet<string> siblingSymbolNamesToRemove, CancellationToken cancellationToken)
        {
            if (surfaceAreaDocument == null)
            {
                var newSourceText = AddSymbolNamesToSourceText(sourceText: null, new[] { newSymbolName });
                return AddPublicApiFiles(project, newSourceText, isPublic);
            }
            else
            {
                var sourceText = await surfaceAreaDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var newSourceText = AddSymbolNamesToSourceText(sourceText, new[] { newSymbolName });
                newSourceText = RemoveSymbolNamesFromSourceText(newSourceText, siblingSymbolNamesToRemove);

                return surfaceAreaDocument.Project.Solution.WithAdditionalDocumentText(surfaceAreaDocument.Id, newSourceText);
            }
        }

        private static Solution AddPublicApiFiles(Project project, SourceText unshippedText, bool isPublic)
        {
            Debug.Assert(unshippedText.Length > 0);
            project = AddAdditionalDocument(project, isPublic ? DeclarePublicApiAnalyzer.PublicShippedFileName : DeclarePublicApiAnalyzer.InternalShippedFileName, SourceText.From(string.Empty));
            project = AddAdditionalDocument(project, isPublic ? DeclarePublicApiAnalyzer.PublicUnshippedFileName : DeclarePublicApiAnalyzer.InternalUnshippedFileName, unshippedText);
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

            var endOfLine = sourceText.GetEndOfLine();

            var newText = string.Join(endOfLine, lines) + sourceText.GetEndOfFileText(endOfLine);
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

            var endOfLine = sourceText.GetEndOfLine();
            SourceText newSourceText = sourceText.Replace(new TextSpan(0, sourceText.Length), string.Join(endOfLine, newLines) + sourceText.GetEndOfFileText(endOfLine));
            return newSourceText;
        }

        internal static List<string> GetLinesFromSourceText(SourceText? sourceText)
        {
            if (sourceText == null)
            {
                return [];
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

        internal class AdditionalDocumentChangeAction : CodeAction
        {
            private readonly Func<CancellationToken, Task<Solution?>> _createChangedAdditionalDocument;

            public AdditionalDocumentChangeAction(string title, DocumentId? apiDocId, bool isPublic, Func<CancellationToken, Task<Solution?>> createChangedAdditionalDocument)
            {
                this.Title = title;
                EquivalenceKey = apiDocId.CreateEquivalenceKey(isPublic);
                _createChangedAdditionalDocument = createChangedAdditionalDocument;
            }

            public override string Title { get; }

            public override string EquivalenceKey { get; }

            protected override Task<Solution?> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                return _createChangedAdditionalDocument(cancellationToken);
            }
        }

        private class FixAllAdditionalDocumentChangeAction : CodeAction
        {
            private readonly List<KeyValuePair<Project, ImmutableArray<Diagnostic>>> _diagnosticsToFix;
            private readonly bool _isPublic;
            private readonly DocumentId? _apiDocId;
            private readonly Solution _solution;

            public FixAllAdditionalDocumentChangeAction(string title, DocumentId? apiDocId, Solution solution, List<KeyValuePair<Project, ImmutableArray<Diagnostic>>> diagnosticsToFix, bool isPublic)
            {
                this.Title = title;
                _apiDocId = apiDocId;
                _solution = solution;
                _diagnosticsToFix = diagnosticsToFix;
                this._isPublic = isPublic;
            }

            public override string Title { get; }

            protected override async Task<Solution?> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                var updatedPublicSurfaceAreaText = new List<KeyValuePair<DocumentId, SourceText>>();
                var addedPublicSurfaceAreaText = new List<KeyValuePair<ProjectId, SourceText>>();

                foreach (KeyValuePair<Project, ImmutableArray<Diagnostic>> pair in _diagnosticsToFix)
                {
                    Project project = pair.Key;
                    ImmutableArray<Diagnostic> diagnostics = pair.Value;

                    var publicSurfaceAreaAdditionalDocument = _apiDocId is not null ? project.GetAdditionalDocument(_apiDocId) : null;
                    var sourceText = publicSurfaceAreaAdditionalDocument != null ?
                        await publicSurfaceAreaAdditionalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false) :
                        null;

                    IEnumerable<IGrouping<SyntaxTree?, Diagnostic>> groupedDiagnostics =
                        diagnostics
                            .Where(d => d.Location.IsInSource)
                            .GroupBy(d => d.Location.SourceTree);

                    var newSymbolNames = new SortedSet<string>(IgnoreCaseWhenPossibleComparer.Instance);
                    var symbolNamesToRemoveBuilder = PooledHashSet<string>.GetInstance();

                    foreach (IGrouping<SyntaxTree?, Diagnostic> grouping in groupedDiagnostics)
                    {
                        Document? document = project.GetDocument(grouping.Key);

                        if (document == null)
                        {
                            continue;
                        }

                        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                        SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                        foreach (Diagnostic diagnostic in grouping)
                        {
                            if (diagnostic.Id is RoslynDiagnosticIds.ShouldAnnotatePublicApiFilesRuleId
                                              or RoslynDiagnosticIds.ShouldAnnotateInternalApiFilesRuleId
                                              or RoslynDiagnosticIds.ObliviousPublicApiRuleId
                                              or RoslynDiagnosticIds.ObliviousInternalApiRuleId)
                            {
                                continue;
                            }

                            string? publicSurfaceAreaSymbolName = diagnostic.Properties[DeclarePublicApiAnalyzer.ApiNamePropertyBagKey];
                            if (publicSurfaceAreaSymbolName == null)
                            {
                                continue;
                            }

                            newSymbolNames.Add(publicSurfaceAreaSymbolName);

                            string? siblingNamesToRemove = diagnostic.Properties[DeclarePublicApiAnalyzer.ApiNamesOfSiblingsToRemovePropertyBagKey];
                            if (siblingNamesToRemove is { Length: > 0 })
                            {
                                var namesToRemove = siblingNamesToRemove.Split(DeclarePublicApiAnalyzer.ApiNamesOfSiblingsToRemovePropertyBagValueSeparator.ToCharArray());
                                foreach (var nameToRemove in namesToRemove)
                                {
                                    symbolNamesToRemoveBuilder.Add(nameToRemove);
                                }
                            }
                        }
                    }

                    var symbolNamesToRemove = symbolNamesToRemoveBuilder.ToImmutableHashSet();
                    symbolNamesToRemoveBuilder.Free();

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
                using var _ = PooledHashSet<string>.GetInstance(out var uniqueProjectPaths);
                foreach (KeyValuePair<ProjectId, SourceText> pair in addedPublicSurfaceAreaText)
                {
                    var project = newSolution.GetProject(pair.Key);
                    if (project == null)
                    {
                        continue;
                    }

                    if (uniqueProjectPaths.Add(project.FilePath ?? project.Name))
                    {
                        newSolution = AddPublicApiFiles(project, pair.Value, _isPublic);
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

                return new FixAllAdditionalDocumentChangeAction(title, fixAllContext.CreateDocIdFromEquivalenceKey(out bool isPublic), fixAllContext.Solution, diagnosticsToFix, isPublic);
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
