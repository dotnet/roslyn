// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline;

internal sealed class DocumentOutlineSearchTask(
    IThreadingContext threadingContext,
    DocumentOutlineViewModel viewModel,
    uint cookie,
    IVsSearchQuery searchQuery,
    IVsSearchCallback searchCallback) : VsSearchTask(cookie, searchQuery, searchCallback)
{
    private bool _searchRequested;

    protected override void OnStartSearch()
    {
        threadingContext.JoinableTaskFactory.RunAsync(async () =>
        {
            await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(threadingContext.DisposalToken);

            if (TaskStatus != VSConstants.VsSearchTaskStatus.Started)
                return;

            viewModel.SearchText = SearchQuery.SearchString;
            _searchRequested = true;
        }).Task.ReportNonFatalErrorAsync();
    }

    public void Complete(uint resultCount)
    {
        threadingContext.ThrowIfNotOnUIThread();

        // A previous model update can arrive before the UI-thread search request runs.
        if (!_searchRequested || !SetTaskStatus(VSConstants.VsSearchTaskStatus.Completed))
            return;

        SearchResults = resultCount;

        SearchCallback.ReportComplete(this, resultCount);
    }
}
