// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Diagnostics.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = "DeclarePublicAPIFix"), Shared]
    public class DeclarePublicAPIFix : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(RoslynDiagnosticIds.DeclarePublicApiRuleId); }
        }

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
                string minimalSymbolName = diagnostic.Properties[DeclarePublicAPIAnalyzer.MinimalNamePropertyBagKey];
                string publicSurfaceAreaSymbolName = diagnostic.Properties[DeclarePublicAPIAnalyzer.PublicApiNamePropertyBagKey];

                context.RegisterCodeFix(
                        new AdditionalDocumentChangeAction(
                            $"Add {minimalSymbolName} to public API",
                            c => GetFix(publicSurfaceAreaDocument, publicSurfaceAreaSymbolName, c)),
                        diagnostic);
            }
        }

        private static TextDocument GetPublicSurfaceAreaDocument(Project project)
        {
            return project.AdditionalDocuments.FirstOrDefault(doc => doc.Name.Equals(DeclarePublicAPIAnalyzer.UnshippedFileName, StringComparison.Ordinal));
        }

        private async Task<Solution> GetFix(TextDocument publicSurfaceAreaDocument, string newSymbolName, CancellationToken cancellationToken)
        {
            SourceText sourceText = await publicSurfaceAreaDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            SourceText newSourceText = AddSymbolNamesToSourceText(sourceText, new[] { newSymbolName });

            return publicSurfaceAreaDocument.Project.Solution.WithAdditionalDocumentText(publicSurfaceAreaDocument.Id, newSourceText);
        }

        private static SourceText AddSymbolNamesToSourceText(SourceText sourceText, IEnumerable<string> newSymbolNames)
        {
            HashSet<string> lines = GetLinesFromSourceText(sourceText);

            foreach (string name in newSymbolNames)
            {
                lines.Add(name);
            }

            IOrderedEnumerable<string> sortedLines = lines.OrderBy(s => s, StringComparer.Ordinal);

            SourceText newSourceText = sourceText.Replace(new TextSpan(0, sourceText.Length), string.Join(Environment.NewLine, sortedLines));
            return newSourceText;
        }

        private static HashSet<string> GetLinesFromSourceText(SourceText sourceText)
        {
            var lines = new HashSet<string>();

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

        private static ISymbol FindDeclaration(SyntaxNode root, Location location, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            SyntaxNode node = root.FindNode(location.SourceSpan);
            ISymbol symbol = null;
            while (node != null)
            {
                symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);
                if (symbol != null)
                {
                    break;
                }

                node = node.Parent;
            }

            return symbol;
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
                            string publicSurfaceAreaSymbolName = diagnostic.Properties[DeclarePublicAPIAnalyzer.PublicApiNamePropertyBagKey];

                            newSymbolNames.Add(publicSurfaceAreaSymbolName);
                        }
                    }

                    SourceText newSourceText = AddSymbolNamesToSourceText(sourceText, newSymbolNames);

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
                string titleFormat = "Add all items in {0} {1} to the public API";
                string title = null;

                switch (fixAllContext.Scope)
                {
                    case FixAllScope.Document:
                        {
                            ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(fixAllContext.Document).ConfigureAwait(false);
                            diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(fixAllContext.Project, diagnostics));
                            title = string.Format(titleFormat, "document", fixAllContext.Document.Name);
                            break;
                        }

                    case FixAllScope.Project:
                        {
                            Project project = fixAllContext.Project;
                            ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                            diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(fixAllContext.Project, diagnostics));
                            title = string.Format(titleFormat, "project", fixAllContext.Project.Name);
                            break;
                        }

                    case FixAllScope.Solution:
                        {
                            foreach (Project project in fixAllContext.Solution.Projects)
                            {
                                ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                                diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(project, diagnostics));
                            }

                            title = "Add all items in the solution to the public API";
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
