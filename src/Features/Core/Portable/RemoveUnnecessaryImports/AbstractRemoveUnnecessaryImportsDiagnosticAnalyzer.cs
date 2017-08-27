﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Fading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports
{
    internal abstract class AbstractRemoveUnnecessaryImportsDiagnosticAnalyzer :
        DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        // NOTE: This is a trigger diagnostic, which doesn't show up in the ruleset editor and hence doesn't need a conventional IDE Diagnostic ID string.
        internal const string DiagnosticFixableId = "RemoveUnnecessaryImportsFixable";

        // The NotConfigurable custom tag ensures that user can't turn this diagnostic into a warning / error via
        // ruleset editor or solution explorer. Setting messageFormat to empty string ensures that we won't display
        // this diagnostic in the preview pane header.
        private static readonly DiagnosticDescriptor s_fixableIdDescriptor =
            new DiagnosticDescriptor(DiagnosticFixableId,
                                     title: "", messageFormat: "", category: "",
                                     defaultSeverity: DiagnosticSeverity.Hidden,
                                     isEnabledByDefault: true,
                                     customTags: WellKnownDiagnosticTags.NotConfigurable);

        protected abstract LocalizableString GetTitleAndMessageFormatForClassificationIdDescriptor();

        private DiagnosticDescriptor _unnecessaryClassificationIdDescriptor;
        private DiagnosticDescriptor _classificationIdDescriptor;

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
            }
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                EnsureClassificationIdDescriptors();
                return ImmutableArray.Create(s_fixableIdDescriptor, _unnecessaryClassificationIdDescriptor, _classificationIdDescriptor);
            }
        }

        public bool OpenFileOnly(Workspace workspace) => true;

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSemanticModelAction(this.AnalyzeSemanticModel);
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
                var descriptor = fadeOut ? _unnecessaryClassificationIdDescriptor : _classificationIdDescriptor;

                Func<SyntaxNode, SyntaxToken> getLastTokenFunc = GetLastTokenDelegateForContiguousSpans();
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
