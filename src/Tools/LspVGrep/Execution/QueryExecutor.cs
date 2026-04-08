using System.Diagnostics;
using LspVGrepTool.Models;

namespace LspVGrepTool.Execution;

internal sealed class QueryExecutor
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<IQueryAlgorithm>> _algorithmsByQueryType;

    public QueryExecutor(IEnumerable<IQueryAlgorithm> algorithms)
    {
        _algorithmsByQueryType = algorithms
            .GroupBy(algorithm => algorithm.QueryType, StringComparer.Ordinal)
            .ToDictionary(
                grouping => grouping.Key,
                grouping => (IReadOnlyList<IQueryAlgorithm>)grouping.ToList(),
                StringComparer.Ordinal);
    }

    public async Task<ToolReport> ExecuteAsync(
        IReadOnlyList<QueryRequest> queries,
        QueryExecutionContext context,
        TimeSpan? workspaceLoadTime,
        CancellationToken cancellationToken)
    {
        var queryReports = new List<QueryExecutionReport>(queries.Count);

        foreach (var query in queries)
        {
            if (!_algorithmsByQueryType.TryGetValue(query.Type, out var algorithms) || algorithms.Count == 0)
            {
                throw new InvalidOperationException($"No algorithms are registered for query type '{query.Type}'.");
            }

            var results = new List<AlgorithmExecutionResult>(algorithms.Count);
            foreach (var algorithm in algorithms)
            {
                var result = await ExecuteAlgorithmAsync(algorithm, query, context, cancellationToken);
                results.Add(result);
            }

            queryReports.Add(new QueryExecutionReport(query.Type, query.GetDisplayFields(), results));
        }

        var workspace = context.TryGetLoadedWorkspace();
        return new ToolReport(
            context.DirectoryPath,
            workspace?.TargetPath,
            workspace?.TargetKind.ToString(),
            workspaceLoadTime,
            queryReports);
    }

    private static async Task<AlgorithmExecutionResult> ExecuteAlgorithmAsync(
        IQueryAlgorithm algorithm,
        QueryRequest query,
        QueryExecutionContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await algorithm.ExecuteAsync(query, context, cancellationToken);
            stopwatch.Stop();
            return result with { ElapsedTime = stopwatch.Elapsed };
        }
        catch (Exception exception)
        {
            return new AlgorithmExecutionResult(
                algorithm.Name,
                AlgorithmOutcome.Failed,
                exception.ToString());
        }
    }
}
