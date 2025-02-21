// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.Razor;

[Shared]
[Export(typeof(IDynamicFileInfoProvider))]
[Export(typeof(RazorDynamicFileInfoProvider))]
[ExportMetadata("Extensions", new string[] { "cshtml", "razor", })]
internal partial class RazorDynamicFileInfoProvider : IDynamicFileInfoProvider
{
    private readonly AsyncBatchingWorkQueue<string> _updateWorkQueue;
    private IRazorWorkspaceService? _razorWorkspaceService;
    private IRazorDynamicFileInfoProvider? _dynamicFileInfoProvider;

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

    public void Initialize(IRazorWorkspaceService razorWorkspaceService, IRazorDynamicFileInfoProvider dynamicFileInfoProvider)
    {
        _razorWorkspaceService = razorWorkspaceService;
        _dynamicFileInfoProvider = dynamicFileInfoProvider;
        _dynamicFileInfoProvider.Updated += (s, filePath) => _updateWorkQueue.AddWork(filePath);
    }

    public event EventHandler<string>? Updated;

    public async Task<DynamicFileInfo?> GetDynamicFileInfoAsync(ProjectId projectId, string? projectFilePath, string filePath, CancellationToken cancellationToken)
    {
        if (_razorWorkspaceService is not { IsInitialized: true } || _dynamicFileInfoProvider is null)
        {
            return null;
        }

        _razorWorkspaceService.NotifyDynamicFile(projectId);

        var dynamicInfo = await _dynamicFileInfoProvider.GetDynamicFileInfoAsync(projectId, projectFilePath, filePath, cancellationToken).ConfigureAwait(false);
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
        if (_dynamicFileInfoProvider is null)
        {
            return;
        }

        await _dynamicFileInfoProvider.RemoveDynamicFileInfoAsync(projectId, projectFilePath, filePath, cancellationToken).ConfigureAwait(false);
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
}
