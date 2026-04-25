// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Razor.LanguageClient;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Razor.ProjectSystem;

[Order(RazorConstants.AboveManagedProjectSystemOrder)]
[Export(typeof(IProjectTreeActionHandler))]
[AppliesTo(WellKnownProjectCapabilities.DotNetCoreRazor)]
[method: ImportingConstructor]
internal sealed partial class RenameProjectTreeHandler(
    [Import(ExportContractNames.Scopes.UnconfiguredProject)] IProjectAsynchronousTasksService projectAsynchronousTasksService,
    SVsServiceProvider serviceProvider,
    Lazy<LSPRequestInvokerWrapper> requestInvoker,
    ILoggerFactory loggerFactory) : ProjectTreeActionHandlerBase
{
    private readonly IProjectAsynchronousTasksService _projectAsynchronousTasksService = projectAsynchronousTasksService;
    private readonly SVsServiceProvider _serviceProvider = serviceProvider;
    private readonly Lazy<LSPRequestInvokerWrapper> _requestInvoker = requestInvoker;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<RenameProjectTreeHandler>();

    public override async Task RenameAsync(IProjectTreeActionHandlerContext context, IProjectTree node, string value)
    {
        ApplyRenameEditParams? request = null;
        try
        {
            if (node.FilePath is null || node.IsFolder)
            {
                return;
            }

            var oldFilePath = node.FilePath;
            var newFilePath = Path.Combine(Path.GetDirectoryName(oldFilePath), value);

            // We only do fancy renames for Razor component files, and only if they're not changing file extensions
            if (!FileUtilities.IsRazorComponentFilePath(oldFilePath, PathUtilities.OSSpecificPathComparison) ||
                !FileUtilities.IsRazorComponentFilePath(newFilePath, PathUtilities.OSSpecificPathComparison))
            {
                return;
            }

            var response = await _projectAsynchronousTasksService.LoadedProjectAsync(() => _requestInvoker.Value.ReinvokeRequestOnServerAsync<RenameFilesParams, WorkspaceEdit?>(
                Methods.WorkspaceWillRenameFilesName,
                RazorLSPConstants.RoslynLanguageServerName,
                new RenameFilesParams()
                {
                    Files =
                    [
                        new FileRename()
                    {
                        OldUri = new(RazorUri.CreateAbsoluteUri(oldFilePath)),
                        NewUri = new(RazorUri.CreateAbsoluteUri(newFilePath)),
                    }
                    ]
                },
                _projectAsynchronousTasksService.UnloadCancellationToken));

            if (response.Result is null)
            {
                return;
            }

            request = new ApplyRenameEditParams
            {
                Edit = response.Result,
                OldFilePath = oldFilePath,
                NewFilePath = newFilePath
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during rename operation.");
        }
        finally
        {
            // Always perform the default rename operation (renaming the file on disk)
            await base.RenameAsync(context, node, value);
        }

        if (request is null)
        {
            return;
        }

        ApplyWorkspaceEditAsync(request).Forget();
    }

    private async Task ApplyWorkspaceEditAsync(ApplyRenameEditParams request)
    {
        // We want to let the rename operation finish to avoid deadlocks
        await Task.Yield();

        await _projectAsynchronousTasksService.LoadedProjectAsync(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var fromComponentName = Path.GetFileNameWithoutExtension(request.OldFilePath);
            var toComponentName = Path.GetFileNameWithoutExtension(request.NewFilePath);

            var dialogFactory = (IVsThreadedWaitDialogFactory)_serviceProvider.GetService(typeof(SVsThreadedWaitDialogFactory));
            using var _ = new WaitIndicator(dialogFactory, SR.Renaming_Razor_Component, SR.FormatRenaming_0_to_1(fromComponentName, toComponentName));

            await _requestInvoker.Value.ReinvokeRequestOnServerAsync<ApplyRenameEditParams, VoidResult>(
                 RazorLSPConstants.ApplyRenameEditName,
                 RazorLSPConstants.RoslynLanguageServerName,
                 request,
                 _projectAsynchronousTasksService.UnloadCancellationToken);
        });
    }
}
