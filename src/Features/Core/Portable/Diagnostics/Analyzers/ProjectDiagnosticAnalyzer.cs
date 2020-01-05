// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// IDE-only project based diagnostic analyzer.
    /// </summary>
    internal abstract class ProjectDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public const int DefaultPriority = 50;

        public abstract Task<ImmutableArray<Diagnostic>> AnalyzeProjectAsync(Project project, CancellationToken cancellationToken);

        /// <summary>
        /// it is not allowed one to implement both ProjectDiagnosticAnalzyer and DiagnosticAnalyzer
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
