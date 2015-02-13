// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    internal interface ISyntaxNodeAnalyzerService : ILanguageService
    {
        void ExecuteSyntaxNodeActions(
            AnalyzerActions actions,
            IEnumerable<SyntaxNode> descendantNodes,
            SemanticModel semanticModel,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> reportDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken);

        void ExecuteCodeBlockActions(
            AnalyzerActions actions,
            IEnumerable<DeclarationInfo> declarationsInNode,
            SemanticModel semanticModel,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> reportDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken);
    }
}
