// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Services.Razor;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.Razor;

[Shared]
[Export(typeof(IDynamicFileInfoProvider))]
[Export(typeof(RazorDynamicFileInfoProvider))]
[ExportMetadata("Extensions", new string[] { "cshtml", "razor", })]
internal partial class RazorDynamicFileInfoProvider : IDynamicFileInfoProvider
{
    private readonly AsyncBatchingWorkQueue<string> _updateWorkQueue;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RazorDynamicFileInfoProvider(
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _updateWorkQueue = new AsyncBatchingWorkQueue<string>(
            TimeSpan.FromMilliseconds(200),
            UpdateAsync,
            listenerProvider.GetListener(nameof(RazorDynamicFileInfoProvider)),
            CancellationToken.None);
    }

    public event EventHandler<string>? Updated;

    public void Update(string filePath)
    {
        _updateWorkQueue.AddWork(filePath);
    }

    public async Task<DynamicFileInfo?> GetDynamicFileInfoAsync(ProjectId projectId, string? projectFilePath, string filePath, CancellationToken cancellationToken)
    {
        var razorWorkspaceService = await RazorLSPServiceProvider.TryGetServiceAsync<IRazorWorkspaceService>(cancellationToken);

        if (razorWorkspaceService is not { IsInitialized: true })
        {
            return null;
        }

        razorWorkspaceService.NotifyDynamicFile(projectId);
        var dynamicFileInfoProvider = await GetFileProviderAsync(cancellationToken);

        var dynamicInfo = await dynamicFileInfoProvider.GetDynamicFileInfoAsync(projectId, projectFilePath, filePath, cancellationToken).ConfigureAwait(false);
        if (dynamicInfo is null)
        {
            return null;
        }

        return new DynamicFileInfo(
            dynamicInfo.FilePath,
            dynamicInfo.SourceCodeKind,
            dynamicInfo.TextLoader,
            designTimeOnly: true,
            new RazorDocumentServiceProviderWrapper(dynamicInfo.DocumentServiceProvider));
    }

    public async Task RemoveDynamicFileInfoAsync(ProjectId projectId, string? projectFilePath, string filePath, CancellationToken cancellationToken)
    {
        var dynamicFileInfoProvider = await GetFileProviderAsync(cancellationToken).ConfigureAwait(false);
        await dynamicFileInfoProvider.RemoveDynamicFileInfoAsync(projectId, projectFilePath, filePath, cancellationToken).ConfigureAwait(false);
    }

    private ValueTask UpdateAsync(ImmutableSegmentedList<string> paths, CancellationToken token)
    {
        foreach (var path in paths)
        {
            token.ThrowIfCancellationRequested();
            Updated?.Invoke(this, path);
        }

        return ValueTask.CompletedTask;
    }

    private async Task<IRazorLSPDynamicFileInfoProvider> GetFileProviderAsync(CancellationToken cancellationToken)
    {
        var dynamicFileInfoProvider = await RazorLSPServiceProvider.GetRequiredServiceAsync<IRazorLSPDynamicFileInfoProvider>(cancellationToken);

        await dynamicFileInfoProvider.EnsureInitializedAsync(
            static ct =>
            {
                return RazorLSPServiceProvider.GetRequiredServiceAsync<IRazorClientLanguageServerManager>(ct);
            },
            cancellationToken);

        return dynamicFileInfoProvider;
    }
}
