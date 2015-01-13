// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

namespace Roslyn.Diagnostics.Analyzers.ApiDesign
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = "PublicSurfaceAreaFix"), Shared]
    public class DeclarePublicAPIFix : CodeFixProvider
    {
        private static readonly SymbolDisplayFormat titleFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:
                    SymbolDisplayMemberOptions.None,
                parameterOptions:
                    SymbolDisplayParameterOptions.None,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.None);

        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(RoslynDiagnosticIds.DeclarePublicApiRuleId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return new PublicSurfaceAreaFixAllProvider();
        }

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var project = context.Document.Project;
            TextDocument publicSurfaceAreaDocument = GetPublicSurfaceAreaDocument(project);
            if (publicSurfaceAreaDocument == null)
            {
                return;
            }

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan);
                if (node != null)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(node, context.CancellationToken);
                    var minimalSymbolName = symbol.ToMinimalDisplayString(semanticModel, node.SpanStart, titleFormat);
                    var publicSurfaceAreaSymbolName = symbol.ToDisplayString(DeclarePublicAPIAnalyzer.PublicApiFormat);

                    if (symbol != null)
                    {
                        context.RegisterFix(
                            new AdditionalDocumentChangeAction(
                                $"Add {minimalSymbolName} to public API",
                                c => GetFix(publicSurfaceAreaDocument, publicSurfaceAreaSymbolName, c)),
                            diagnostic);
                    }
                }
            }
        }

        private static TextDocument GetPublicSurfaceAreaDocument(Project project)
        {
            return project.AdditionalDocuments.FirstOrDefault(doc => doc.Name.Equals(DeclarePublicAPIAnalyzer.PublicApiFileName, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<Solution> GetFix(TextDocument publicSurfaceAreaDocument, string newSymbolName, CancellationToken cancellationToken)
        {
            var sourceText = await publicSurfaceAreaDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newSourceText = AddSymbolNamesToSourceText(sourceText, new[] { newSymbolName });

            return publicSurfaceAreaDocument.Project.Solution.WithAdditionalDocumentText(publicSurfaceAreaDocument.Id, newSourceText);
        }

        private static SourceText AddSymbolNamesToSourceText(SourceText sourceText, IEnumerable<string> newSymbolNames)
        {
            HashSet<string> lines = GetLinesFromSourceText(sourceText);

            foreach (var name in newSymbolNames)
            {
                lines.Add(name);
            }

            var sortedLines = lines.OrderBy(s => s, StringComparer.Ordinal);

            var newSourceText = sourceText.Replace(new TextSpan(0, sourceText.Length), string.Join(Environment.NewLine, sortedLines));
            return newSourceText;
        }

        private static HashSet<string> GetLinesFromSourceText(SourceText sourceText)
        {
            var lines = new HashSet<string>();

            foreach (var textLine in sourceText.Lines)
            {
                var text = textLine.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    lines.Add(text);
                }
            }

            return lines;
        }

        private class AdditionalDocumentChangeAction : CodeAction
        {
            private readonly Func<CancellationToken, Task<Solution>> createChangedAdditionalDocument;

            public AdditionalDocumentChangeAction(string title, Func<CancellationToken, Task<Solution>> createChangedAdditionalDocument)
            {
                this.Title = title;
                this.createChangedAdditionalDocument = createChangedAdditionalDocument;
            }

            public override string Title { get; }

            protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                return this.createChangedAdditionalDocument(cancellationToken);
            }
        }

        private class FixAllAdditionalDocumentChangeAction : CodeAction
        {
            private readonly List<KeyValuePair<Project, ImmutableArray<Diagnostic>>> diagnosticsToFix;
            private readonly Solution solution;

            public FixAllAdditionalDocumentChangeAction(string title, Solution solution, List<KeyValuePair<Project, ImmutableArray<Diagnostic>>> diagnosticsToFix)
            {
                this.Title = title;
                this.solution = solution;
                this.diagnosticsToFix = diagnosticsToFix;
            }

            public override string Title { get; }

            protected override async Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                var updatedPublicSurfaceAreaText = new List<KeyValuePair<DocumentId, SourceText>>();

                foreach (var pair in diagnosticsToFix)
                {
                    var project = pair.Key;
                    var diagnostics = pair.Value;

                    var publicSurfaceAreaAdditionalDocument = GetPublicSurfaceAreaDocument(project);

                    if (publicSurfaceAreaAdditionalDocument == null)
                    {
                        continue;
                    }

                    var sourceText = await publicSurfaceAreaAdditionalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    var groupedDiagnostics =
                        diagnostics
                            .Where(d => d.Location.IsInSource)
                            .GroupBy(d => d.Location.SourceTree);

                    var newSymbolNames = new List<string>();

                    foreach (var grouping in groupedDiagnostics)
                    {
                        var document = project.GetDocument(grouping.Key);

                        if (document == null)
                        {
                            continue;
                        }

                        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                        foreach (var diagnostic in grouping)
                        {
                            var node = root.FindNode(diagnostic.Location.SourceSpan);
                            if (node != null)
                            {
                                var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);
                                var publicSurfaceAreaSymbolName = symbol.ToDisplayString(DeclarePublicAPIAnalyzer.PublicApiFormat);

                                if (symbol != null)
                                {
                                    newSymbolNames.Add(publicSurfaceAreaSymbolName);
                                }
                            }
                        }
                    }

                    var newSourceText = AddSymbolNamesToSourceText(sourceText, newSymbolNames);

                    updatedPublicSurfaceAreaText.Add(new KeyValuePair<DocumentId, SourceText>(publicSurfaceAreaAdditionalDocument.Id, newSourceText));
                }

                var newSolution = this.solution;

                foreach (var pair in updatedPublicSurfaceAreaText)
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
                        var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(fixAllContext.Document).ConfigureAwait(false);
                        diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(fixAllContext.Project, diagnostics));
                        title = string.Format(titleFormat, "document", fixAllContext.Document.Name);
                        break;
                    }

                case FixAllScope.Project:
                    {
                        var project = fixAllContext.Project;
                        ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                        diagnosticsToFix.Add(new KeyValuePair<Project, ImmutableArray<Diagnostic>>(fixAllContext.Project, diagnostics));
                        title = string.Format(titleFormat, "project", fixAllContext.Project.Name);
                        break;
                    }

                case FixAllScope.Solution:
                    {
                        foreach (var project in fixAllContext.Solution.Projects)
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