// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Handler that allows the client to retrieve recognized file-based program entry points.
/// </summary>
[ExportCSharpVisualBasicStatelessLspService(typeof(FileBasedProgramEntryPointsHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class FileBasedProgramEntryPointsHandler() : ILspServiceRequestHandler<string[]>
{
    internal const string MethodName = "workspace/_ms_fileBasedProgramEntryPoints";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => false;

    public Task<string[]> HandleRequestAsync(RequestContext context, CancellationToken cancellationToken)
    {
        var miscellaneousFilesWorkspaceProvider = context.GetRequiredService<ILspMiscellaneousFilesWorkspaceProvider>();
        if (miscellaneousFilesWorkspaceProvider is not FileBasedProgramsProjectSystem fileBasedProgramsProjectSystem)
            return Task.FromResult(Array.Empty<string>());

        return Task.FromResult(fileBasedProgramsProjectSystem.GetFileBasedProgramEntryPoints());
    }
}
