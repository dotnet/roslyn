// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports
{
    internal abstract class AbstractRemoveUnnecessaryImportsDiagnosticAnalyzer
        : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        // NOTE: This is a trigger diagnostic, which doesn't show up in the ruleset editor and hence doesn't need a conventional IDE Diagnostic ID string.
        internal const string DiagnosticFixableId = "RemoveUnnecessaryImportsFixable";

        // The NotConfigurable custom tag ensures that user can't turn this diagnostic into a warning / error via
        // ruleset editor or solution explorer. Setting messageFormat to empty string ensures that we won't display
        // this diagnostic in the preview pane header.
#pragma warning disable RS0030 // Do not used banned APIs - We cannot use AbstractBuiltInCodeStyleDiagnosticAnalyzer nor AbstractCodeQualityDiagnosticAnalyzer.
        // This analyzer is run against generated code while the abstract base classes mentioned doesn't.
        private static readonly DiagnosticDescriptor s_fixableIdDescriptor =
            new(DiagnosticFixableId,
                                     title: "", messageFormat: "", category: "",
                                     defaultSeverity: DiagnosticSeverity.Hidden,
                                     isEnabledByDefault: true,
                                     customTags: DiagnosticCustomTags.NotConfigurable);
#pragma warning restore RS0030 // Do not used banned APIs

        private readonly DiagnosticDescriptor _unnecessaryClassificationIdDescriptor;
        private readonly DiagnosticDescriptor _classificationIdDescriptor;
        private readonly DiagnosticDescriptor _unnecessaryGeneratedCodeClassificationIdDescriptor;
        private readonly DiagnosticDescriptor _generatedCodeClassificationIdDescriptor;

        protected AbstractRemoveUnnecessaryImportsDiagnosticAnalyzer()
        {
            var titleAndMessageFormat = GetTitleAndMessageFormatForClassificationIdDescriptor();

#pragma warning disable RS0030 // Do not used banned APIs
            _unnecessaryClassificationIdDescriptor =
                new DiagnosticDescriptor(IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId,
                                         titleAndMessageFormat,
                                         titleAndMessageFormat,
                                         DiagnosticCategory.Style,
                                         DiagnosticSeverity.Hidden,
                                         isEnabledByDefault: true,
                                         helpLinkUri: DiagnosticHelper.GetHelpLinkForDiagnosticId(IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId),
                                         customTags: DiagnosticCustomTags.Unnecessary.Concat(EnforceOnBuildValues.RemoveUnnecessaryImports.ToCustomTag()).ToArray());

            _classificationIdDescriptor =
                new DiagnosticDescriptor(IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId,
                                         titleAndMessageFormat,
                                         titleAndMessageFormat,
                                         DiagnosticCategory.Style,
                                         DiagnosticSeverity.Hidden,
                                         isEnabledByDefault: true,
                                         helpLinkUri: DiagnosticHelper.GetHelpLinkForDiagnosticId(IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId),
                                         customTags: EnforceOnBuildValues.RemoveUnnecessaryImports.ToCustomTag());

            _unnecessaryGeneratedCodeClassificationIdDescriptor =
                new DiagnosticDescriptor(IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId + "_gen",
                                         titleAndMessageFormat,
                                         titleAndMessageFormat,
                                         DiagnosticCategory.Style,
                                         DiagnosticSeverity.Hidden,
                                         isEnabledByDefault: true,
                                         helpLinkUri: DiagnosticHelper.GetHelpLinkForDiagnosticId(IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId),
                                         customTags: DiagnosticCustomTags.UnnecessaryAndNotConfigurable);

            _generatedCodeClassificationIdDescriptor =
                new DiagnosticDescriptor(IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId + "_gen",
                                         titleAndMessageFormat,
                                         titleAndMessageFormat,
                                         DiagnosticCategory.Style,
                                         DiagnosticSeverity.Hidden,
                                         isEnabledByDefault: true,
                                         helpLinkUri: DiagnosticHelper.GetHelpLinkForDiagnosticId(IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId),
                                         customTags: DiagnosticCustomTags.NotConfigurable);
#pragma warning restore RS0030 // Do not used banned APIs
        }

        protected abstract ISyntaxFacts SyntaxFacts { get; }
        protected abstract LocalizableString GetTitleAndMessageFormatForClassificationIdDescriptor();
        protected abstract ImmutableArray<SyntaxNode> MergeImports(ImmutableArray<SyntaxNode> unnecessaryImports);
        protected abstract bool IsRegularCommentOrDocComment(SyntaxTrivia trivia);
        protected abstract IUnnecessaryImportsProvider UnnecessaryImportsProvider { get; }

        protected abstract SyntaxToken? TryGetLastToken(SyntaxNode node);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(
                    s_fixableIdDescriptor,
                    _unnecessaryClassificationIdDescriptor,
                    _classificationIdDescriptor,
                    _unnecessaryGeneratedCodeClassificationIdDescriptor,
                    _generatedCodeClassificationIdDescriptor);
            }
        }

        public CodeActionRequestPriority RequestPriority => CodeActionRequestPriority.Normal;
        public bool OpenFileOnly(SimplifierOptions? options) => false;

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterSemanticModelAction(AnalyzeSemanticModel);
        }

        private void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            var tree = context.SemanticModel.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var language = context.SemanticModel.Language;

            var unnecessaryImports = UnnecessaryImportsProvider.GetUnnecessaryImports(context.SemanticModel, cancellationToken);
            if (unnecessaryImports.Any())
            {
                // The IUnnecessaryImportsService will return individual import pieces that
                // need to be removed.  For example, it will return individual import-clauses
                // from VB.  However, we want to mark the entire import statement if we are
                // going to remove all the clause.  Defer to our subclass to stitch this up
                // for us appropriately.
                unnecessaryImports = MergeImports(unnecessaryImports);

                var fadeOut = context.Options.GetIdeOptions().FadeOutUnusedImports;

                DiagnosticDescriptor descriptor;
                if (GeneratedCodeUtilities.IsGeneratedCode(tree, IsRegularCommentOrDocComment, cancellationToken))
                {
                    descriptor = fadeOut ? _unnecessaryGeneratedCodeClassificationIdDescriptor : _generatedCodeClassificationIdDescriptor;
                }
                else
                {
                    descriptor = fadeOut ? _unnecessaryClassificationIdDescriptor : _classificationIdDescriptor;
                }

                var contiguousSpans = GetContiguousSpans(unnecessaryImports);
                var diagnostics =
                    CreateClassificationDiagnostics(contiguousSpans, tree, descriptor, cancellationToken).Concat(
                    CreateFixableDiagnostics(unnecessaryImports, tree, cancellationToken));

                foreach (var diagnostic in diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private IEnumerable<TextSpan> GetContiguousSpans(ImmutableArray<SyntaxNode> nodes)
        {
            var syntaxFacts = this.SyntaxFacts;
            (SyntaxNode node, TextSpan textSpan)? previous = null;

            // Sort the nodes in source location order.
            foreach (var node in nodes.OrderBy(n => n.SpanStart))
            {
                TextSpan textSpan;
                var nodeEnd = GetEnd(node);
                if (previous == null)
                {
                    textSpan = TextSpan.FromBounds(node.Span.Start, nodeEnd);
                }
                else
                {
                    var lastToken = TryGetLastToken(previous.Value.node) ?? previous.Value.node.GetLastToken();
                    if (lastToken.GetNextToken(includeDirectives: true) == node.GetFirstToken())
                    {
                        // Expand the span
                        textSpan = TextSpan.FromBounds(previous.Value.textSpan.Start, nodeEnd);
                    }
                    else
                    {
                        // Return the last span, and start a new one
                        yield return previous.Value.textSpan;
                        textSpan = TextSpan.FromBounds(node.Span.Start, nodeEnd);
                    }
                }

                previous = (node, textSpan);
            }

            if (previous.HasValue)
                yield return previous.Value.textSpan;

            yield break;

            int GetEnd(SyntaxNode node)
            {
                var end = node.Span.End;
                foreach (var trivia in node.GetTrailingTrivia())
                {
                    if (syntaxFacts.IsRegularComment(trivia))
                        end = trivia.Span.End;
                }

                return end;
            }
        }

        // Create one diagnostic for each unnecessary span that will be classified as Unnecessary
        private static IEnumerable<Diagnostic> CreateClassificationDiagnostics(
            IEnumerable<TextSpan> contiguousSpans, SyntaxTree tree,
            DiagnosticDescriptor descriptor, CancellationToken cancellationToken)
        {
            foreach (var span in contiguousSpans)
            {
                if (tree.OverlapsHiddenPosition(span, cancellationToken))
                {
                    continue;
                }

                yield return Diagnostic.Create(descriptor, tree.GetLocation(span));
            }
        }

        protected abstract IEnumerable<TextSpan> GetFixableDiagnosticSpans(
            IEnumerable<SyntaxNode> nodes, SyntaxTree tree, CancellationToken cancellationToken);

        private IEnumerable<Diagnostic> CreateFixableDiagnostics(
            IEnumerable<SyntaxNode> nodes, SyntaxTree tree, CancellationToken cancellationToken)
        {
            var spans = GetFixableDiagnosticSpans(nodes, tree, cancellationToken);

            foreach (var span in spans)
            {
                yield return Diagnostic.Create(s_fixableIdDescriptor, tree.GetLocation(span));
            }
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
    }
}
