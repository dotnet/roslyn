// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.VSCode.API;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
internal class HostWorkspace : Workspace
{
    /// <summary>
    /// Set of assemblies to look for host installed analyzers.
    /// Similar to https://github.com/dotnet/roslyn/blob/9fee6f5461baae5152c956c3c3024ca15b85feb9/src/VisualStudio/Setup/source.extension.vsixmanifest#L51
    /// except only include dlls applicable to VSCode.
    /// </summary>
    private static readonly string[] _hostAnalyzerDlls = new string[]
    {
        "Microsoft.CodeAnalysis.CSharp.dll",
        "Microsoft.CodeAnalysis.VisualBasic.dll",
        "Microsoft.CodeAnalysis.Features.dll",
        "Microsoft.CodeAnalysis.Workspaces.dll",
        "Microsoft.CodeAnalysis.CSharp.Workspaces.dll",
        "Microsoft.CodeAnalysis.VisualBasic.Workspaces.dll",
        "Microsoft.CodeAnalysis.CSharp.Features.dll",
        "Microsoft.CodeAnalysis.VisualBasic.Features.dll",
    };

    public HostWorkspace(HostServices host, string? workspaceKind) : base(host, workspaceKind)
    {
    }

    internal static async Task<Workspace> CreateWorkspaceAsync(string solutionPath, ExportProvider exportProvider, HostServices hostServices, ILogger logger)
    {
        try
        {
            // This is weird.  Really we don't need a workspace other than it is a useful tool to keep track of LSP changes.
            // But no changes should ever be applied from the LSP host (instead the client should be applying them).
            //
            // So we use the MSBuildWorkspace type to create the solution.  But we can't use the MSBuild workspace itself
            // because it doesn't support adding analyzers to the solution (and generallly we shouldn't be calling TryApplyChanges).
            // Instead we just take the solution and it put in this workspace type where we can call SetCurrentSolution.
            //
            // This is all going to get refactored anyway when we do more project system stuff.
            using var msbuildWorkspace = MSBuildWorkspace.Create(hostServices);
            var solution = await msbuildWorkspace.OpenSolutionAsync(solutionPath);

            var hostWorkspace = new HostWorkspace(hostServices, WorkspaceKind.Host);
            hostWorkspace.SetCurrentSolution(solution);
            // SetCurrentSolution does raise workspace events.  For now manually register until we figure out how workspaces will work.
            exportProvider.GetExportedValue<LspWorkspaceRegistrationService>().Register(hostWorkspace);

            InitializeDiagnostics(hostWorkspace, exportProvider, logger);

            return hostWorkspace;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to load workspace for {solutionPath}");
            throw;
        }
    }

    internal static void InitializeDiagnostics(HostWorkspace hostWorkspace, ExportProvider exportProvider, ILogger logger)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var references = new List<AnalyzerFileReference>();
        var analyzerLoader = VSCodeAnalyzerLoader.CreateAnalyzerAssemblyLoader();
        foreach (var assemblyName in _hostAnalyzerDlls)
        {
            var path = Path.Combine(baseDirectory, assemblyName);
            if (!File.Exists(path))
                continue;

            references.Add(new AnalyzerFileReference(path, analyzerLoader));
        }

        var newSolution = hostWorkspace.CurrentSolution.WithAnalyzerReferences(references);
        hostWorkspace.SetCurrentSolution(newSolution);
        logger.LogDebug($"Loaded host analyzers:{Environment.NewLine}{string.Join(Environment.NewLine, references.Select(r => r.FullPath))}");

        exportProvider.GetExportedValue<VSCodeAnalyzerLoader>().InitializeDiagnosticsServices(hostWorkspace);
    }
}
