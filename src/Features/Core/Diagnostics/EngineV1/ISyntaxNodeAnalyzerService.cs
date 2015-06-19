// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    internal interface ISyntaxNodeAnalyzerService : ILanguageService
    {
        void ExecuteSyntaxNodeActions(
            AnalyzerActions actions,
            IEnumerable<SyntaxNode> descendantNodes,
            SemanticModel semanticModel,
            AnalyzerExecutor analyzerExecutor);

        void ExecuteCodeBlockActions(
            AnalyzerActions actions,
            IEnumerable<DeclarationInfo> declarationsInNode,
            SemanticModel semanticModel,
            AnalyzerExecutor analyzerExecutor);
    }
}
