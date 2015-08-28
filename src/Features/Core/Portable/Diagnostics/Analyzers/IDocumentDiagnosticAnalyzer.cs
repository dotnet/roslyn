// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// IDE-only document based diagnostic analyzer.
    /// </summary>
    internal abstract class DocumentDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public abstract Task AnalyzeSyntaxAsync(Document document, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken);
        public abstract Task AnalyzeSemanticsAsync(Document document, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken);

        public override void Initialize(AnalysisContext context)
        {
        }
    }
}
