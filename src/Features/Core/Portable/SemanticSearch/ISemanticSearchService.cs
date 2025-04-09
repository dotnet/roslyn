﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal interface ISemanticSearchService : ILanguageService
{
    /// <summary>
    /// Compiles a query. The query has to be executed or discarded.
    /// </summary>
    /// <param name="query">Query (top-level code).</param>
    /// <param name="referenceAssembliesDir">Directory that contains refernece assemblies to be used for compilation of the query.</param>
    CompileQueryResult CompileQuery(
        SolutionServices services,
        string query,
        string referenceAssembliesDir,
        TraceSource traceSource,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes given query against <paramref name="solution"/> and discards it.
    /// </summary>
    /// <param name="solution">The solution snapshot.</param>
    /// <param name="queryId">Id of a compiled query.</param>
    /// <param name="observer">Observer of the found symbols.</param>
    /// <param name="classificationOptions">Options to use to classify the textual representation of the found symbols.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ExecuteQueryResult> ExecuteQueryAsync(
        Solution solution,
        CompiledQueryId queryId,
        ISemanticSearchResultsObserver observer,
        OptionsProvider<ClassificationOptions> classificationOptions,
        TraceSource traceSource,
        CancellationToken cancellationToken);

    /// <summary>
    /// Discards resources associated with compiled query.
    /// Only call if the query is not executed.
    /// </summary>
    void DiscardQuery(CompiledQueryId queryId);
}
