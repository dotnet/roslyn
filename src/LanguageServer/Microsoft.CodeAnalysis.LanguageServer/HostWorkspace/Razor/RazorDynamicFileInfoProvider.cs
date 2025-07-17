// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.Razor;

[Shared]
[Export(typeof(IDynamicFileInfoProvider))]
[ExportRazorStatelessLspService(typeof(RazorDynamicFileInfoProvider))]
[ExportMetadata("Extensions", new string[] { "cshtml", "razor", })]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class RazorDynamicFileInfoProvider(Lazy<LanguageServerWorkspaceFactory> workspaceFactory, ILoggerFactory loggerFactory) : IDynamicFileInfoProvider, ILspService, IOnInitialized, IDisposable
{
    private RazorWorkspaceService? _razorWorkspaceService;
    private RazorLspDynamicFileInfoProvider? _dynamicFileInfoProvider;

    public event EventHandler<string>? Updated;

    private readonly ILogger _logger = loggerFactory.CreateLogger<RazorDynamicFileInfoProvider>();

    public async Task<DynamicFileInfo?> GetDynamicFileInfoAsync(ProjectId projectId, string? projectFilePath, string filePath, CancellationToken cancellationToken)
    {
        if (_dynamicFileInfoProvider is null || _razorWorkspaceService is null)
        {
            return null;
        }

        _razorWorkspaceService.NotifyDynamicFile(projectId);

        var dynamicInfo = await _dynamicFileInfoProvider.GetDynamicFileInfoAsync(workspaceFactory.Value.HostWorkspace, projectId, projectFilePath, filePath, cancellationToken).ConfigureAwait(false);
        if (dynamicInfo is null)
        {
            return null;
        }

        return new DynamicFileInfo(
            dynamicInfo.FilePath,
            dynamicInfo.SourceCodeKind,
            dynamicInfo.TextLoader,
            designTimeOnly: true,
            documentServiceProvider: new RazorDocumentServiceProviderWrapper(dynamicInfo.DocumentServiceProvider));
    }

    public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
    {
        _razorWorkspaceService = context.GetService<RazorWorkspaceService>();
        _dynamicFileInfoProvider = context.GetService<RazorLspDynamicFileInfoProvider>();

        if (_razorWorkspaceService is null || _dynamicFileInfoProvider is null)
        {
            _logger.LogError("RazorDynamicFileInfoProvider not initialized. RazorWorkspaceService or RazorLspDynamicFileInfoProvider is null.");
            return Task.CompletedTask;
        }

        _dynamicFileInfoProvider.Updated += (s, uri) =>
        {
            var filePath = ProtocolConversions.GetDocumentFilePathFromUri(uri);
            Updated?.Invoke(this, filePath);
        };

        return Task.CompletedTask;
    }

    public async Task RemoveDynamicFileInfoAsync(ProjectId projectId, string? projectFilePath, string filePath, CancellationToken cancellationToken)
    {
        if (_dynamicFileInfoProvider is null)
        {
            return;
        }

        await _dynamicFileInfoProvider.RemoveDynamicFileInfoAsync(workspaceFactory.Value.HostWorkspace, projectId, projectFilePath, filePath, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        // Dispose is called when the LSP server is being shut down. Clear the dynamic file provider in case a workspace
        // event is raised after, as the actual provider will try to make LSP requests.
        _dynamicFileInfoProvider = null;
    }
}
