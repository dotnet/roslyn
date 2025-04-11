// Licensed to the .NET Foundation under one or more agreements.
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
    private sealed class ResultsObserver(CancellationTokenSource cancellationSource, int resultCountLimit) : ISemanticSearchResultsObserver
    {
        private ImmutableList<string> _results = [];
        public string? RuntimeException { get; private set; }
        public bool LimitReached { get; private set; }

        public ImmutableList<string> Results => _results;

        public ValueTask AddItemsAsync(int itemCount, CancellationToken cancellationToken)
            => ValueTaskFactory.CompletedTask;

        public ValueTask ItemsCompletedAsync(int itemCount, CancellationToken cancellationToken)
            => ValueTaskFactory.CompletedTask;

        public ValueTask OnUserCodeExceptionAsync(UserCodeExceptionInfo exception, CancellationToken cancellationToken)
        {
            RuntimeException ??= $"{exception.TypeName.ToVisibleDisplayString(includeLeftToRightMarker: false)}: {exception.Message}{Environment.NewLine}{exception.StackTrace.ToVisibleDisplayString(includeLeftToRightMarker: false)}";
            cancellationSource.Cancel();
            return ValueTaskFactory.CompletedTask;
        }

        public ValueTask OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken)
        {
            if (!ImmutableInterlocked.Update(ref _results,
                list => list.Count == resultCountLimit ? list : list.Add(definition.NameDisplayParts.ToVisibleDisplayString(includeLeftToRightMarker: false))))
            {
                LimitReached = true;
                cancellationSource.Cancel();
            }

            return ValueTaskFactory.CompletedTask;
        }
    }

    /// <summary>
    /// We only use symbol display names, classification is not relevant.
    /// </summary>
    private sealed class DefaultClassificationOptionsProvider : OptionsProvider<ClassificationOptions>
    {
        public static readonly DefaultClassificationOptionsProvider Instance = new();

        public ValueTask<ClassificationOptions> GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => new(ClassificationOptions.Default);
    }

    private readonly Workspace _workspace = workspaceProvider.Workspace;

    public async Task<CopilotSemanticSearchQueryResults> ExecuteAsync(string query, int resultCountLimit, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(resultCountLimit > 0);

        using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var observer = new ResultsObserver(cancellationSource, resultCountLimit);

        try
        {
            var result = await RemoteSemanticSearchServiceProxy.ExecuteQueryAsync(
                _workspace.CurrentSolution,
                LanguageNames.CSharp,
                query,
                SemanticSearchUtilities.ReferenceAssembliesDirectory,
                observer,
                DefaultClassificationOptionsProvider.Instance,
                cancellationSource.Token).ConfigureAwait(false);

            return new CopilotSemanticSearchQueryResults()
            {
                Symbols = observer.Results,
                CompilationErrors = result.compilationErrors.SelectAsArray(e => (e.Id, e.Message)),
                Error = (result.ErrorMessage != null) ? string.Format(result.ErrorMessage, result.ErrorMessageArgs ?? []) : null,
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
