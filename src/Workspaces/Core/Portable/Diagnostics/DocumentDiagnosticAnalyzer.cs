// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        public const int DefaultPriority = 50;

        public abstract Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken);

        public abstract Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken);

        /// <summary>
        /// it is not allowed one to implement both DocumentDiagnosticAnalzyer and DiagnosticAnalyzer
        /// </summary>
#pragma warning disable RS1026 // Enable concurrent execution
#pragma warning disable RS1025 // Configure generated code analysis
        public sealed override void Initialize(AnalysisContext context)
#pragma warning restore RS1025 // Configure generated code analysis
#pragma warning restore RS1026 // Enable concurrent execution
        {
        }

        /// <summary>
        /// This lets vsix installed <see cref="DocumentDiagnosticAnalyzer"/> or <see cref="ProjectDiagnosticAnalyzer"/> to
        /// specify priority of the analyzer. Regular <see cref="DiagnosticAnalyzer"/> always comes before those 2 different types.
        /// Priority is ascending order and this only works on HostDiagnosticAnalyzer meaning Vsix installed analyzers in VS.
        /// This is to support partner teams (such as typescript and F#) who want to order their analyzer's execution order.
        /// </summary>
        public virtual int Priority => DefaultPriority;
    }
}
