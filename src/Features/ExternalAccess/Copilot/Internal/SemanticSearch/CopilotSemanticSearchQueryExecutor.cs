﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.SemanticSearch;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SemanticSearch;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.SemanticSearch;

[Export(typeof(ICopilotSemanticSearchQueryExecutor)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CopilotSemanticSearchQueryExecutor(IHostWorkspaceProvider workspaceProvider) : ICopilotSemanticSearchQueryExecutor
{
    private sealed class ResultsObserver(CancellationTokenSource cancellationSource, int resultCountLimit) : ISemanticSearchResultsDefinitionObserver
    {
        private ImmutableList<string> _results = [];
        public string? RuntimeException { get; private set; }
        public bool LimitReached { get; private set; }

        public ImmutableList<string> Results => _results;

        /// <summary>
        /// We only use symbol display names, classification is not relevant.
        /// </summary>
        public ValueTask<ClassificationOptions> GetClassificationOptionsAsync(LanguageServices language, CancellationToken cancellationToken)
            => new(ClassificationOptions.Default);

        public ValueTask AddItemsAsync(int itemCount, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask ItemsCompletedAsync(int itemCount, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask OnUserCodeExceptionAsync(UserCodeExceptionInfo exception, CancellationToken cancellationToken)
        {
            RuntimeException ??= $"{exception.TypeName.ToVisibleDisplayString(includeLeftToRightMarker: false)}: {exception.Message}{Environment.NewLine}{exception.StackTrace.ToVisibleDisplayString(includeLeftToRightMarker: false)}";
            cancellationSource.Cancel();
            return ValueTask.CompletedTask;
        }

        public ValueTask OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken)
        {
            if (!ImmutableInterlocked.Update(ref _results,
                list => list.Count == resultCountLimit ? list : list.Add(definition.NameDisplayParts.ToVisibleDisplayString(includeLeftToRightMarker: false))))
            {
                LimitReached = true;
                cancellationSource.Cancel();
            }

            return ValueTask.CompletedTask;
        }
    }

    private readonly Workspace _workspace = workspaceProvider.Workspace;

    public async Task<CopilotSemanticSearchQueryResults> ExecuteAsync(string query, int resultCountLimit, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(resultCountLimit > 0);

        using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var observer = new ResultsObserver(cancellationSource, resultCountLimit);

        try
        {
            var compileResult = await RemoteSemanticSearchServiceProxy.CompileQueryAsync(
                _workspace.CurrentSolution.Services,
                query,
                cancellationSource.Token).ConfigureAwait(false);

            if (compileResult == null)
            {
                return new CopilotSemanticSearchQueryResults()
                {
                    Symbols = observer.Results,
                    CompilationErrors = [],
                    Error = FeaturesResources.Semantic_search_only_supported_on_net_core,
                    LimitReached = false,
                };
            }

            if (!compileResult.Value.CompilationErrors.IsEmpty)
            {
                return new CopilotSemanticSearchQueryResults()
                {
                    Symbols = observer.Results,
                    CompilationErrors = compileResult.Value.CompilationErrors.SelectAsArray(e => (e.Id, e.Message)),
                    Error = null,
                    LimitReached = false,
                };
            }

            var executeResult = await RemoteSemanticSearchServiceProxy.ExecuteQueryAsync(
                _workspace.CurrentSolution,
                compileResult.Value.QueryId,
                observer,
                cancellationSource.Token).ConfigureAwait(false);

            return new CopilotSemanticSearchQueryResults()
            {
                Symbols = observer.Results,
                CompilationErrors = [],
                Error = (executeResult.ErrorMessage != null) ? string.Format(executeResult.ErrorMessage, executeResult.ErrorMessageArgs ?? []) : null,
                LimitReached = false,
            };
        }
        catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
        {
            return new CopilotSemanticSearchQueryResults()
            {
                Symbols = observer.Results,
                CompilationErrors = [],
                Error = observer.RuntimeException != null ? $"The query failed with an exception: {observer.RuntimeException}" : null,
                LimitReached = observer.LimitReached,
            };
        }
    }
}
