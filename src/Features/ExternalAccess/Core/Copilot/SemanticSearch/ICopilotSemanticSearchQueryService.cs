// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.SemanticSearch;

internal interface ICopilotSemanticSearchQueryService
{
    CompileQueryResult CompileQuery(
        SolutionServices services,
        string query,
        string? targetLanguage,
        string referenceAssembliesDir,
        TraceSource traceSource,
        CancellationToken cancellationToken);

    Task<ExecuteQueryResult> ExecuteQueryAsync(
        Solution solution,
        CompiledQueryId queryId,
        ICopilotSemanticSearchResultsObserver observer,
        QueryExecutionOptions options,
        TraceSource traceSource,
        CancellationToken cancellationToken);

    void DiscardQuery(CompiledQueryId queryId);

    internal readonly record struct QueryExecutionOptions(
        int? ResultCountLimit,
        bool KeepCompiledQuery);

    internal readonly record struct ExecuteQueryResult(
        string? ErrorMessage,
        string[]? ErrorMessageArgs = null,
        TimeSpan ExecutionTime = default);

    internal readonly record struct CompileQueryResult(
        CompiledQueryId QueryId,
        ImmutableArray<QueryCompilationError> CompilationErrors,
        TimeSpan EmitTime = default);

    internal readonly record struct QueryCompilationError(
        string Id,
        string Message,
        TextSpan Span);

    internal readonly record struct CompiledQueryId(
        int Id);
}
