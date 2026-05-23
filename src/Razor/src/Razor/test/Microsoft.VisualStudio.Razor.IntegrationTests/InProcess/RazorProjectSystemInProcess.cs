// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Razor.IntegrationTests.InProcess;
using Microsoft.VisualStudio.Razor.LanguageClient;

namespace Microsoft.VisualStudio.Extensibility.Testing;

[TestService]
internal partial class RazorProjectSystemInProcess
{
    public async Task WaitForLSPServerActivatedAsync(CancellationToken cancellationToken)
    {
        await WaitForLSPServerActivationStatusAsync(active: true, cancellationToken);
    }

    public async Task WaitForLSPServerDeactivatedAsync(CancellationToken cancellationToken)
    {
        await WaitForLSPServerActivationStatusAsync(active: false, cancellationToken);
    }

    private async Task WaitForLSPServerActivationStatusAsync(bool active, CancellationToken cancellationToken)
    {
        var tracker = await TestServices.Shell.GetComponentModelServiceAsync<ILspServerActivationTracker>(cancellationToken);
        await Helper.RetryAsync(ct =>
        {
            return Task.FromResult(tracker.IsActive == active);
        }, TimeSpan.FromMilliseconds(50), cancellationToken);
    }

    public async Task WaitForHtmlVirtualDocumentAsync(string razorFilePath, CancellationToken cancellationToken)
    {
        var documentManager = await TestServices.Shell.GetComponentModelServiceAsync<LSPDocumentManager>(cancellationToken);

        var uri = new Uri(razorFilePath, UriKind.Absolute);
        await Helper.RetryAsync(ct =>
        {
            if (documentManager.TryGetDocument(uri, out var snapshot))
            {
                if (snapshot.TryGetVirtualDocument<HtmlVirtualDocumentSnapshot>(out var virtualDocument))
                {
                    return Task.FromResult(true);
                }
            }

            return SpecializedTasks.False;

        }, TimeSpan.FromMilliseconds(100), cancellationToken);
    }

    public async Task WaitForHtmlVirtualDocumentUpdateAsync(string projectName, string relativeFilePath, Func<Task> updater, CancellationToken cancellationToken)
    {
        var filePath = await TestServices.SolutionExplorer.GetAbsolutePathForProjectRelativeFilePathAsync(projectName, relativeFilePath, cancellationToken);

        var documentManager = await TestServices.Shell.GetComponentModelServiceAsync<LSPDocumentManager>(cancellationToken);

        var uri = new Uri(filePath, UriKind.Absolute);

        long? desiredVersion = null;

        await Helper.RetryAsync(async ct =>
        {
            if (documentManager.TryGetDocument(uri, out var snapshot))
            {
                if (snapshot.TryGetVirtualDocument<HtmlVirtualDocumentSnapshot>(out var virtualDocument))
                {
                    if (virtualDocument.Snapshot.Length > 0)
                    {
                        if (desiredVersion is null)
                        {
                            desiredVersion = virtualDocument.HostDocumentSyncVersion + 1;
                            await updater();
                        }
                        else if (virtualDocument.HostDocumentSyncVersion == desiredVersion)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            return false;

        }, TimeSpan.FromMilliseconds(100), cancellationToken);
    }
}
