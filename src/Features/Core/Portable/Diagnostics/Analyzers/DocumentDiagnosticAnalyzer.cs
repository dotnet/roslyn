// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// IDE-only document based diagnostic analyzer.
    /// </summary>
    internal abstract class DocumentDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public abstract Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken);

        public abstract Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken);

        /// <summary>
        /// it is not allowed one to implement both DocumentDiagnosticAnalzyer and DiagnosticAnalyzer
        /// </summary>
        public sealed override void Initialize(AnalysisContext context)
        {
        }
    }
}
