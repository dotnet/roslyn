// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.RemoveUnnecessaryImports
{
    internal abstract class RemoveUnnecessaryImportsDiagnosticAnalyzerBase : DiagnosticAnalyzer, IBuiltInAnalyzer
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

        private DiagnosticDescriptor _classificationIdDescriptor;
        private DiagnosticDescriptor GetClassificationIdDescriptor()
        {
            if (_classificationIdDescriptor == null)
            {
                var titleAndMessageFormat = GetTitleAndMessageFormatForClassificationIdDescriptor();
                _classificationIdDescriptor =
                    new DiagnosticDescriptor(IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId,
                                             titleAndMessageFormat,
                                             titleAndMessageFormat,
                                             DiagnosticCategory.Style,
                                             DiagnosticSeverity.Hidden,
                                             isEnabledByDefault: true,
                                             customTags: DiagnosticCustomTags.Unnecessary);
            }

            return _classificationIdDescriptor;
        }

        private ImmutableArray<DiagnosticDescriptor> _descriptors;
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                if (_descriptors == null)
                {
                    _descriptors = ImmutableArray.Create(s_fixableIdDescriptor, GetClassificationIdDescriptor());
                }

                return _descriptors;
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSemanticModelAction(this.AnalyzeSemanticModel);
        }

        private void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            var tree = context.SemanticModel.SyntaxTree;
            var root = tree.GetRoot();
            var unnecessaryImports = GetUnnecessaryImports(context.SemanticModel, root);
            if (unnecessaryImports != null && unnecessaryImports.Any())
            {
                Func<SyntaxNode, SyntaxToken> getLastTokenFunc = GetLastTokenDelegateForContiguousSpans();
                var contiguousSpans = unnecessaryImports.GetContiguousSpans(getLastTokenFunc);
                var diagnostics = CreateClassificationDiagnostics(contiguousSpans, tree).Concat(
                        CreateFixableDiagnostics(unnecessaryImports, tree));

                foreach (var diagnostic in diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        protected abstract IEnumerable<SyntaxNode> GetUnnecessaryImports(SemanticModel semanticModel, SyntaxNode root, CancellationToken cancellationToken = default(CancellationToken));
        protected virtual Func<SyntaxNode, SyntaxToken> GetLastTokenDelegateForContiguousSpans()
        {
            return null;
        }

        // Create one diagnostic for each unnecessary span that will be classified as Unnecessary
        private IEnumerable<Diagnostic> CreateClassificationDiagnostics(IEnumerable<TextSpan> contiguousSpans, SyntaxTree tree, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var span in contiguousSpans)
            {
                if (tree.OverlapsHiddenPosition(span, cancellationToken))
                {
                    continue;
                }

                yield return Diagnostic.Create(GetClassificationIdDescriptor(), tree.GetLocation(span));
            }
        }

        protected abstract IEnumerable<TextSpan> GetFixableDiagnosticSpans(IEnumerable<SyntaxNode> nodes, SyntaxTree tree, CancellationToken cancellationToken = default(CancellationToken));

        private IEnumerable<Diagnostic> CreateFixableDiagnostics(IEnumerable<SyntaxNode> nodes, SyntaxTree tree, CancellationToken cancellationToken = default(CancellationToken))
        {
            var spans = GetFixableDiagnosticSpans(nodes, tree, cancellationToken);

            foreach (var span in spans)
            {
                yield return Diagnostic.Create(s_fixableIdDescriptor, tree.GetLocation(span));
            }
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
        }
    }
}
