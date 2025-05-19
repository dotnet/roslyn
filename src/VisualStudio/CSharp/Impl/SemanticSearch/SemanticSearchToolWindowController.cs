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
using Microsoft.VisualStudio.Shell.Interop;
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
    SVsServiceProvider serviceProvider) : ISemanticSearchToolWindowController
{
    private readonly AsyncLazy<VisualStudioExtensibility> _visualStudioExtensibility = new(
        () =>
        {
            var shell = (IVsShell)serviceProvider.GetService(typeof(SVsShell));
            ErrorHandler.ThrowOnFailure(shell.LoadPackage(Guids.CSharpPackageId, out var package));
            return RoslynServiceExtensions.GetServiceAsync<VisualStudioExtensibility, VisualStudioExtensibility>((AsyncPackage)package, threadingContext.JoinableTaskFactory);
        },
        threadingContext.JoinableTaskFactory);

    public async Task UpdateQueryAsync(string query, bool activateWindow, bool executeQuery, CancellationToken cancellationToken)
    {
        var extensibility = await _visualStudioExtensibility.GetValueAsync(cancellationToken).ConfigureAwait(false);

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
