// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SemanticSearch;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.CSharp;

/// <summary>
/// Controls the Semantic Search tool window.
/// </summary>
[Export(typeof(ISemanticSearchToolWindowController)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SemanticSearchToolWindowController(
    SemanticSearchToolWindowImpl toolWindowImpl,
    IThreadingContext threadingContext,
    IVsService<VisualStudioExtensibility, VisualStudioExtensibility> lazyExtensibilityService) : ISemanticSearchToolWindowController
{
    private readonly IVsService<VisualStudioExtensibility, VisualStudioExtensibility> _lazyExtensibilityService = lazyExtensibilityService;

    public async Task UpdateQueryAsync(string query, bool activateWindow, bool executeQuery, CancellationToken cancellationToken)
    {
        var extensibility = await _lazyExtensibilityService.GetValueAsync(cancellationToken).ConfigureAwait(false);

        await extensibility.Shell().ShowToolWindowAsync<SemanticSearchToolWindow>(activateWindow, cancellationToken).ConfigureAwait(false);

        // make sure the window has been initialized:
        _ = await toolWindowImpl.InitializeAsync(cancellationToken).ConfigureAwait(false);

        await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        toolWindowImpl.SetEditorText(query);

        if (executeQuery)
        {
            toolWindowImpl.RunQuery();
        }

        await TaskScheduler.Default;
    }
}
