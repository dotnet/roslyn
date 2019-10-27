// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Fading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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
        // Setting category to "Compiler" ensures we always run this analyzer even when user has turned off analyzer execution for the project.
        private static readonly DiagnosticDescriptor s_fixableIdDescriptor =
            new DiagnosticDescriptor(DiagnosticFixableId,
                                     title: "", messageFormat: "", category: DiagnosticCategory.Compiler,
                                     defaultSeverity: DiagnosticSeverity.Hidden,
                                     isEnabledByDefault: true,
                                     customTags: WellKnownDiagnosticTags.NotConfigurable);

        protected abstract LocalizableString GetTitleAndMessageFormatForClassificationIdDescriptor();

        private DiagnosticDescriptor _unnecessaryClassificationIdDescriptor;
        private DiagnosticDescriptor _classificationIdDescriptor;
        private DiagnosticDescriptor _unnecessaryGeneratedCodeClassificationIdDescriptor;
        private DiagnosticDescriptor _generatedCodeClassificationIdDescriptor;

        private void EnsureClassificationIdDescriptors()
        {
            if (_unnecessaryClassificationIdDescriptor == null)
            {
                var titleAndMessageFormat = GetTitleAndMessageFormatForClassificationIdDescriptor();

                _unnecessaryClassificationIdDescriptor =
                    new DiagnosticDescriptor(IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId,
                                             titleAndMessageFormat,
                                             titleAndMessageFormat,
                                             DiagnosticCategory.Style,
                                             DiagnosticSeverity.Hidden,
                                             isEnabledByDefault: true,
                                             customTags: DiagnosticCustomTags.Unnecessary);

                _classificationIdDescriptor =
                    new DiagnosticDescriptor(IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId,
                                             titleAndMessageFormat,
                                             titleAndMessageFormat,
                                             DiagnosticCategory.Style,
                                             DiagnosticSeverity.Hidden,
                                             isEnabledByDefault: true);

                _unnecessaryGeneratedCodeClassificationIdDescriptor =
                    new DiagnosticDescriptor(IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId + "_gen",
                                             titleAndMessageFormat,
                                             titleAndMessageFormat,
                                             DiagnosticCategory.Style,
                                             DiagnosticSeverity.Hidden,
                                             isEnabledByDefault: true,
                                             customTags: new[] { WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.NotConfigurable });

                _generatedCodeClassificationIdDescriptor =
                    new DiagnosticDescriptor(IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId + "_gen",
                                             titleAndMessageFormat,
                                             titleAndMessageFormat,
                                             DiagnosticCategory.Style,
                                             DiagnosticSeverity.Hidden,
                                             isEnabledByDefault: true,
                                             customTags: new[] { WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTags.NotConfigurable });
            }
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                EnsureClassificationIdDescriptors();
                return ImmutableArray.Create(
                    s_fixableIdDescriptor,
                    _unnecessaryClassificationIdDescriptor,
                    _classificationIdDescriptor,
                    _unnecessaryGeneratedCodeClassificationIdDescriptor,
                    _generatedCodeClassificationIdDescriptor);
            }
        }

        public bool OpenFileOnly(Workspace workspace) => false;

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

            if (!(context.Options is WorkspaceAnalyzerOptions workspaceOptions))
            {
                return;
            }

            var language = context.SemanticModel.Compilation.Language;
            var service = workspaceOptions.Services.GetLanguageServices(language)
                                                   .GetService<IUnnecessaryImportsService>();

            var unnecessaryImports = service.GetUnnecessaryImports(context.SemanticModel, cancellationToken);
            if (unnecessaryImports.Any())
            {
                // The IUnnecessaryImportsService will return individual import pieces that
                // need to be removed.  For example, it will return individual import-clauses
                // from VB.  However, we want to mark the entire import statement if we are
                // going to remove all the clause.  Defer to our subclass to stitch this up
                // for us appropriately.
                unnecessaryImports = MergeImports(unnecessaryImports);

                EnsureClassificationIdDescriptors();
                var fadeOut = workspaceOptions.Services.Workspace.Options.GetOption(FadingOptions.FadeOutUnusedImports, language);

                DiagnosticDescriptor descriptor;
                var syntaxFacts = workspaceOptions.Services.GetLanguageServices(language).GetRequiredService<ISyntaxFactsService>();
                if (GeneratedCodeUtilities.IsGeneratedCode(tree, t => syntaxFacts.IsRegularComment(t) || syntaxFacts.IsDocumentationComment(t), context.CancellationToken))
                {
                    descriptor = fadeOut ? _unnecessaryGeneratedCodeClassificationIdDescriptor : _generatedCodeClassificationIdDescriptor;
                }
                else
                {
                    descriptor = fadeOut ? _unnecessaryClassificationIdDescriptor : _classificationIdDescriptor;
                }

                var getLastTokenFunc = GetLastTokenDelegateForContiguousSpans();
                var contiguousSpans = unnecessaryImports.GetContiguousSpans(getLastTokenFunc);
                var diagnostics =
                    CreateClassificationDiagnostics(contiguousSpans, tree, descriptor, cancellationToken).Concat(
                    CreateFixableDiagnostics(unnecessaryImports, tree, cancellationToken));

                foreach (var diagnostic in diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        protected abstract ImmutableArray<SyntaxNode> MergeImports(ImmutableArray<SyntaxNode> unnecessaryImports);

        protected virtual Func<SyntaxNode, SyntaxToken> GetLastTokenDelegateForContiguousSpans()
        {
            return null;
        }

        // Create one diagnostic for each unnecessary span that will be classified as Unnecessary
        private IEnumerable<Diagnostic> CreateClassificationDiagnostics(
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
