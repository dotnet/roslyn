// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.SemanticSearch;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SemanticSearch;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.SemanticSearch;

[Export(typeof(ISemanticSearchQueryService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CopilotSemanticSearchQueryService(
    [Import(AllowDefault = true)] ICopilotSemanticSearchQueryService? impl) : ISemanticSearchQueryService
{
    private sealed class CopilotObserver(ISemanticSearchResultsObserver observer) : ICopilotSemanticSearchResultsObserver
    {
        public ValueTask AddItemsAsync(int itemCount, CancellationToken cancellationToken)
            => observer.AddItemsAsync(itemCount, cancellationToken);

        public ValueTask ItemsCompletedAsync(int itemCount, CancellationToken cancellationToken)
            => observer.ItemsCompletedAsync(itemCount, cancellationToken);

        public ValueTask OnSymbolFoundAsync(Solution solution, ISymbol symbol, CancellationToken cancellationToken)
            => observer.OnSymbolFoundAsync(solution, symbol, cancellationToken);

        public ValueTask OnUserCodeExceptionAsync(ICopilotSemanticSearchResultsObserver.UserCodeExceptionInfo info, CancellationToken cancellationToken)
            => observer.OnUserCodeExceptionAsync(
                new UserCodeExceptionInfo(
                    info.ProjectDisplayName,
                    info.Message,
                    info.TypeName,
                    info.StackTrace,
                    info.Span),
                cancellationToken);
    }

    public CompileQueryResult CompileQuery(SolutionServices services, string query, string? targetLanguage, string referenceAssembliesDir, TraceSource traceSource, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(impl);

        var result = impl.CompileQuery(services, query, targetLanguage, referenceAssembliesDir, traceSource, cancellationToken);
        return new(
            new(result.QueryId.Id),
            result.CompilationErrors.SelectAsArray(static e => new QueryCompilationError(e.Id, e.Message, e.Span)),
            result.EmitTime);
    }

    public void DiscardQuery(CompiledQueryId queryId)
    {
        Contract.ThrowIfNull(impl);
        impl.DiscardQuery(new(queryId.Id));
    }

    public async Task<ExecuteQueryResult> ExecuteQueryAsync(Solution solution, CompiledQueryId queryId, ISemanticSearchResultsObserver observer, QueryExecutionOptions options, TraceSource traceSource, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(impl);

        var result = await impl.ExecuteQueryAsync(
            solution,
            new(queryId.Id),
            new CopilotObserver(observer),
            new(options.ResultCountLimit, options.KeepCompiledQuery),
            traceSource,
            cancellationToken).ConfigureAwait(false);

        return new ExecuteQueryResult(
            result.ErrorMessage,
            result.ErrorMessageArgs,
            result.ExecutionTime);
    }
}
