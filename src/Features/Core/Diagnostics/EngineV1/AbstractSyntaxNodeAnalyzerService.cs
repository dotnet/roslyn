// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

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
            AnalyzerExecutor analyzerExecutor)
        {
            analyzerExecutor.ExecuteSyntaxNodeActions(actions, descendantNodes, semanticModel, this.GetKind);
        }

        public void ExecuteCodeBlockActions(
            AnalyzerActions actions,
            IEnumerable<DeclarationInfo> declarationsInNode,
            SemanticModel semanticModel,
            AnalyzerExecutor analyzerExecutor)
        {
            analyzerExecutor.ExecuteCodeBlockActions(actions, declarationsInNode, semanticModel, this.GetKind);
        }
    }
}
