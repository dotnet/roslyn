// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// IDE-only document based diagnostic analyzer.
/// </summary>
internal abstract class DocumentDiagnosticAnalyzer : DiagnosticAnalyzer
{
    public const int DefaultPriority = 50;

    public virtual async Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(TextDocument textDocument, SyntaxTree? tree, CancellationToken cancellationToken)
        => [];

    public virtual async Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(TextDocument textDocument, SyntaxTree? tree, CancellationToken cancellationToken)
        => [];

    /// <summary>
    /// it is not allowed one to implement both DocumentDiagnosticAnalyzer and DiagnosticAnalyzer
    /// </summary>
#pragma warning disable RS1026 // Enable concurrent execution
#pragma warning disable RS1025 // Configure generated code analysis
    public sealed override void Initialize(AnalysisContext context)
#pragma warning restore RS1025 // Configure generated code analysis
#pragma warning restore RS1026 // Enable concurrent execution
    {
    }

    /// <summary>
    /// This lets vsix installed <see cref="DocumentDiagnosticAnalyzer"/> to specify priority of the analyzer. Regular
    /// <see cref="DiagnosticAnalyzer"/> always comes before those 2 different types. Priority is ascending order and
    /// this only works on HostDiagnosticAnalyzer meaning Vsix installed analyzers in VS. This is to support partner
    /// teams (such as typescript and F#) who want to order their analyzer's execution order.
    /// </summary>
    public virtual int Priority => DefaultPriority;
}
