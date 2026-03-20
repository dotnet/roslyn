// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

[Shared]
[ExportLspServiceFactory(typeof(CsprojInConeChecker), ProtocolConstants.RoslynLspLanguagesContract)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CsprojInConeCheckerFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        return new CsprojInConeChecker();
    }
}

internal sealed class CsprojInConeChecker : ILspService, IOnInitialized
{
    private ImmutableArray<string> _workspaceFolders;

    public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
    {
        var initializeManager = context.GetRequiredService<IInitializeManager>();
        if (initializeManager.TryGetInitializeParams() is { WorkspaceFolders: [_, ..] nonEmptyWorkspaceFolders })
        {
            _workspaceFolders = GetFolderPaths(nonEmptyWorkspaceFolders);
        }

        return Task.CompletedTask;

        static ImmutableArray<string> GetFolderPaths(WorkspaceFolder[] workspaceFolders)
        {
            var builder = ArrayBuilder<string>.GetInstance(workspaceFolders.Length);
            foreach (var workspaceFolder in workspaceFolders)
            {
                if (workspaceFolder.DocumentUri.ParsedUri is not { } parsedUri)
                    continue;

                var workspaceFolderPath = ProtocolConversions.GetDocumentFilePathFromUri(parsedUri);
                builder.Add(workspaceFolderPath);
            }

            return builder.ToImmutableAndFree();
        }
    }

    public bool IsContainedInCsprojCone(string csFilePath)
    {
        // Note: manual perf testing of this check on Windows, in a reasonably complex case,
        // showed an overhead on the order of 100s of microseconds for this check.
        // If this overhead becomes problematic, we may want to put a cache in front of it.

        if (_workspaceFolders.IsDefaultOrEmpty)
            return false;

        if (!PathUtilities.IsAbsolute(csFilePath))
            return false;

        foreach (var workspaceFolder in _workspaceFolders)
        {
            var directoryName = PathUtilities.GetDirectoryName(csFilePath);
            while (PathUtilities.IsSameDirectoryOrChildOf(child: directoryName, parent: workspaceFolder))
            {
                var containsCsproj = Directory.EnumerateFiles(directoryName, "*.csproj").Any();
                if (containsCsproj)
                    return true;

                directoryName = PathUtilities.GetDirectoryName(directoryName);
            }
        }

        return false;
    }
}