// Licensed to the .NET Foundation under one or more agreements.
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
    /// Executes given <paramref name="query"/> query against <paramref name="solution"/>.
    /// </summary>
    /// <param name="solution">The solution snapshot.</param>
    /// <param name="query">Query (top-level code).</param>
    /// <param name="referenceAssembliesDir">Directory that contains refernece assemblies to be used for compilation of the query.</param>
    /// <param name="observer">Observer of the found symbols.</param>
    /// <param name="classificationOptions">Options to use to classify the textual representation of the found symbols.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Error message on failure.</returns>
    Task<ExecuteQueryResult> ExecuteQueryAsync(
        Solution solution,
        string query,
        string referenceAssembliesDir,
        ISemanticSearchResultsObserver observer,
        OptionsProvider<ClassificationOptions> classificationOptions,
        TraceSource traceSource,
        CancellationToken cancellationToken);
}
