// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportCSharpVisualBasicStatelessLspService(typeof(DidChangeWorkspaceFoldersHandler)), Shared]
[Method(Methods.WorkspaceDidChangeWorkspaceFoldersName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DidChangeWorkspaceFoldersHandler(
    LanguageServerProjectSystem projectSystem,
    ILoggerFactory loggerFactory) : ILspServiceNotificationHandler<DidChangeWorkspaceFoldersParams>
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<DidChangeWorkspaceFoldersHandler>();

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => false;

    async Task INotificationHandler<DidChangeWorkspaceFoldersParams, RequestContext>.HandleNotificationAsync(
        DidChangeWorkspaceFoldersParams request,
        RequestContext requestContext,
        CancellationToken cancellationToken)
    {
        var added = GetFolderPaths(request.Event.Added);
        var removed = GetFolderPaths(request.Event.Removed);

        await projectSystem.OnWorkspaceFoldersChangedAsync(added, removed, cancellationToken);
    }

    private ImmutableArray<string> GetFolderPaths(WorkspaceFolder[] folders)
    {
        using var _ = ArrayBuilder<string>.GetInstance(out var builder);
        foreach (var folder in folders)
        {
            if (TryGetFolderFilePath(folder, out var folderPath))
                builder.Add(folderPath);
            else
                _logger.LogWarning("Workspace folder '{folderUri}' is not a file URI; skipping.", folder.DocumentUri);
        }

        return builder.ToImmutable();
    }

    private static bool TryGetFolderFilePath(WorkspaceFolder folder, [NotNullWhen(true)] out string? folderPath)
    {
        if (folder.DocumentUri.ParsedUri is { } uri && uri.IsFile)
        {
            folderPath = ProtocolConversions.GetDocumentFilePathFromUri(uri);
            return true;
        }

        folderPath = null;
        return false;
    }
}
