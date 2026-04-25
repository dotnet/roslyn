// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.LiveShare;

namespace Microsoft.VisualStudio.Razor.LiveShare.Guest;

[ExportCollaborationService(typeof(SessionActiveDetector), Scope = SessionScope.Guest)]
[method: ImportingConstructor]
internal class RazorGuestInitializationService(
[Import(typeof(ILiveShareSessionAccessor))] LiveShareSessionAccessor sessionAccessor) : ICollaborationServiceFactory
{
    private const string ViewImportsFileName = "_ViewImports.cshtml";
    private readonly LiveShareSessionAccessor _sessionAccessor = sessionAccessor;

    private Task? _viewImportsCopyTask;

    public Task<ICollaborationService> CreateServiceAsync(CollaborationSession sessionContext, CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _viewImportsCopyTask = EnsureViewImportsCopiedAsync(sessionContext, cts.Token);

        _sessionAccessor.SetSession(sessionContext);
        var sessionDetector = new SessionActiveDetector(() =>
        {
            cts.Cancel();
            _sessionAccessor.SetSession(session: null);
        });
        return Task.FromResult<ICollaborationService>(sessionDetector);
    }

    // Today we ensure that all _ViewImports in the shared project exist on the guest because we don't currently track import documents
    // in a manner that would allow us to retrieve/monitor that data across the wire. Once the Razor sub-system is moved to use
    // DocumentSnapshots we'll be able to rely on that API to more properly manage files that impact parsing of Razor documents.
    private static async Task EnsureViewImportsCopiedAsync(CollaborationSession sessionContext, CancellationToken cancellationToken)
    {
        var listDirectoryOptions = new ListDirectoryOptions()
        {
            Recursive = true,
            IncludePatterns = new[] { "*.cshtml" }
        };

        var copyTasks = new List<Task>();

        try
        {
            var roots = await sessionContext.ListRootsAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            foreach (var root in roots)
            {
                var fileUris = await sessionContext.ListDirectoryAsync(root, listDirectoryOptions, cancellationToken);
                StartViewImportsCopy(fileUris, copyTasks, sessionContext, cancellationToken);
            }

            await Task.WhenAll(copyTasks);
        }
        catch (OperationCanceledException)
        {
            // Swallow task cancellations
        }
    }

    private static void StartViewImportsCopy(Uri[] fileUris, List<Task> copyTasks, CollaborationSession sessionContext, CancellationToken cancellationToken)
    {
        foreach (var fileUri in fileUris)
        {
            if (fileUri.GetAbsoluteOrUNCPath().EndsWith(ViewImportsFileName, StringComparison.Ordinal))
            {
                var copyTask = sessionContext.DownloadFileAsync(fileUri, cancellationToken);
                copyTasks.Add(copyTask);
            }
        }
    }

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal sealed class TestAccessor(RazorGuestInitializationService instance)
    {
        public Task? ViewImportsCopyTask => instance._viewImportsCopyTask;
    }
}

internal class SessionActiveDetector(Action onDispose) : ICollaborationService, IDisposable
{
    private readonly Action _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));

    [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "https://github.com/dotnet/roslyn-analyzers/issues/4801")]
    public virtual void Dispose()
    {
        _onDispose();
    }
}
