// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.Razor;

[Shared]
[Export(typeof(IDynamicFileInfoProvider))]
[Export(typeof(RazorDynamicFileInfoProvider))]
[ExportMetadata("Extensions", new string[] { "cshtml", "razor", })]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal partial class RazorDynamicFileInfoProvider() : IDynamicFileInfoProvider
{
    private IRazorWorkspaceService? _razorWorkspaceService;
    private IRazorDynamicFileInfoProvider? _dynamicFileInfoProvider;

    public void Initialize(IRazorWorkspaceService razorWorkspaceService, IRazorDynamicFileInfoProvider dynamicFileInfoProvider)
    {
        _razorWorkspaceService = razorWorkspaceService;
        _dynamicFileInfoProvider = dynamicFileInfoProvider;
        _dynamicFileInfoProvider.Updated += (s, filePath) => Updated?.Invoke(this, filePath);
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
}
