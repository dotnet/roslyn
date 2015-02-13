// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    internal abstract class AbstractSyntaxNodeAnalyzerService<TLanguageKindEnum> : ISyntaxNodeAnalyzerService where TLanguageKindEnum : struct
    {
        protected abstract IEqualityComparer<TLanguageKindEnum> GetSyntaxKindEqualityComparer();
        protected abstract TLanguageKindEnum GetKind(SyntaxNode node);

        private bool ShouldAnalyze(SyntaxNode node, ImmutableArray<TLanguageKindEnum> kindsToAnalyze)
        {
            var kind = GetKind(node);
            return kindsToAnalyze.Contains(kind);
        }

        public void ExecuteSyntaxNodeActions(
            AnalyzerActions actions,
            IEnumerable<SyntaxNode> descendantNodes,
            SemanticModel semanticModel,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> reportDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            AnalyzerDriverHelper.ExecuteSyntaxNodeActions(actions, descendantNodes, semanticModel,
                analyzerOptions, reportDiagnostic, continueOnAnalyzerException, this.GetKind, cancellationToken);
        }

        public void ExecuteCodeBlockActions(
            AnalyzerActions actions,
            IEnumerable<DeclarationInfo> declarationsInNode,
            SemanticModel semanticModel,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> reportDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            AnalyzerDriverHelper.ExecuteCodeBlockActions(actions, declarationsInNode,
                semanticModel, analyzerOptions, reportDiagnostic, continueOnAnalyzerException, this.GetKind, cancellationToken);
        }
    }
}
