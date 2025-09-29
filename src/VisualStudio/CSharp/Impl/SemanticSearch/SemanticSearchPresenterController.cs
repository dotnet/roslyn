// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SemanticSearch;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.CSharp;

/// <summary>
/// Executes Semantic Search query and streams results to Find Results tool window.
/// </summary>
[Export(typeof(ISemanticSearchPresenterController)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SemanticSearchPresenterController(
    IStreamingFindUsagesPresenter resultsPresenter,
    VisualStudioWorkspace workspace,
    IGlobalOptionService globalOptions,
    IThreadingContext threadingContext) : ISemanticSearchPresenterController
{
    public async Task ExecuteQueryAsync(string query, CancellationToken cancellationToken)
    {
        await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var (presenterContext, presenterCancellationToken) = resultsPresenter.StartSearch(ServicesVSResources.Semantic_search_results, StreamingFindUsagesPresenterOptions.Default);

        await TaskScheduler.Default;

        using var queryCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(presenterCancellationToken, cancellationToken);

        // TODO: logger
        var executor = new SemanticSearchQueryExecutor(presenterContext, logMessage: static _ => { }, globalOptions);
        await executor.ExecuteAsync(query, queryDocument: null, workspace.CurrentSolution, queryCancellationSource.Token).ConfigureAwait(false);
    }
}
