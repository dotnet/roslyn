// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

[ExportLspService(typeof(CsprojInConeChecker), ProtocolConstants.RoslynLspLanguagesContract), Shared(LspServiceComposition.SharingBoundary)]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification = "Used directly by tests")]
internal sealed class CsprojInConeChecker(IWorkspaceFolderTracker workspaceFolderTracker) : ILspService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CsprojInConeChecker(LspService<IWorkspaceFolderTracker> workspaceFolderTracker)
        : this(workspaceFolderTracker.Value)
    {
    }

    public bool IsContainedInCsprojCone(string csFilePath)
    {
        // Note: manual perf testing of this check on Windows, in a reasonably complex case,
        // showed an overhead on the order of 100s of microseconds for this check.
        // If this overhead becomes problematic, we may want to put a cache in front of it.

        // Bound the search to workspace folders so opening an arbitrary file outside the workspace does not
        // discover and load an unrelated project from one of its ancestor directories.
        var workspaceFolders = workspaceFolderTracker.GetRequiredWorkspaceFolderPaths();
        if (workspaceFolders.IsEmpty)
            return false;

        if (!PathUtilities.IsAbsolute(csFilePath))
            return false;

        foreach (var workspaceFolder in workspaceFolders)
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
