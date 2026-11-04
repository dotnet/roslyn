// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Extensibility.Testing;

internal partial class EditorInProcess
{
    public async Task<ICollapsible[]> GetOutlineRegionsAsync(Text.Editor.IWpfTextView textView, CancellationToken cancellationToken)
    {
        var span = new SnapshotSpan(textView.TextSnapshot, 0, textView.TextSnapshot.Length);
        var manager = await GetOutlineManagerAsync(textView, cancellationToken);

        var outlines = manager.GetAllRegions(span);

        return outlines
            .OrderBy(s => s.Extent.GetStartPoint(textView.TextSnapshot))
            .ToArray();
    }

    public async Task WaitForOutlineRegionsAsync(CancellationToken cancellationToken)
    {
        var textView = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
        var manager = await GetOutlineManagerAsync(textView, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var span = new SnapshotSpan(textView.TextSnapshot, 0, textView.TextSnapshot.Length);

        manager.RegionsChanged += On_RegionsChanged;

        try
        {
            CompleteIfResolved();
            await completionSource.Task.WithCancellation(cancellationToken);
        }
        finally
        {
            manager.RegionsChanged -= On_RegionsChanged;
        }

        void On_RegionsChanged(object sender, RegionsChangedEventArgs e)
            => CompleteIfResolved();

        void CompleteIfResolved()
        {
            var regions = manager.GetAllRegions(span);

            if (regions.Any())
            {
                completionSource.TrySetResult(true);
            }
        }
    }

    private async Task<IOutliningManager> GetOutlineManagerAsync(Text.Editor.IWpfTextView textView, CancellationToken cancellationToken)
    {
        await TestServices.JoinableTaskFactory.SwitchToMainThreadAsync();
        var outliningService = await TestServices.Shell.GetComponentModelServiceAsync<IOutliningManagerService>(cancellationToken);
        var manager = outliningService.GetOutliningManager(textView);
        Assumes.NotNull(manager);
        return manager;
    }
}
