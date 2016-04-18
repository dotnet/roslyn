// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// IDE-only document based diagnostic analyzer.
    /// </summary>
    internal abstract class DocumentDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        // REVIEW: why DocumentDiagnosticAnalyzer doesn't have span based analysis?
        public abstract Task AnalyzeSyntaxAsync(Document document, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken);
        public abstract Task AnalyzeSemanticsAsync(Document document, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken);

        /// <summary>
        /// it is not allowed one to implement both DocumentDiagnosticAnalzyer and DiagnosticAnalyzer
        /// </summary>
        public sealed override void Initialize(AnalysisContext context)
        {
        }
    }
}
