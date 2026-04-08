using LspVGrepTool.Models;

namespace LspVGrepTool.Execution;

internal interface IQueryAlgorithm
{
    string Name { get; }

    string QueryType { get; }

    Task<AlgorithmExecutionResult> ExecuteAsync(QueryRequest query, QueryExecutionContext context, CancellationToken cancellationToken);
}

internal abstract class QueryAlgorithm<TQuery> : IQueryAlgorithm
    where TQuery : QueryRequest
{
    public abstract string Name { get; }

    public abstract string QueryType { get; }

    public async Task<AlgorithmExecutionResult> ExecuteAsync(QueryRequest query, QueryExecutionContext context, CancellationToken cancellationToken)
    {
        if (query is not TQuery typedQuery)
        {
            throw new InvalidOperationException(
                $"Algorithm '{Name}' cannot handle query type '{query.Type}'.");
        }

        return await ExecuteTypedAsync(typedQuery, context, cancellationToken);
    }

    protected abstract Task<AlgorithmExecutionResult> ExecuteTypedAsync(
        TQuery query,
        QueryExecutionContext context,
        CancellationToken cancellationToken);
}
