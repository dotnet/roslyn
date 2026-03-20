// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

internal sealed class CanonicalMiscellaneousFilesProjectProvider : IDisposable
{
    private readonly LanguageServerWorkspaceFactory _workspaceFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly AsyncLazy<ImmutableArray<ProjectFileInfo>> _canonicalBuildResult;
    private string? _tempDirectory;

    public CanonicalMiscellaneousFilesProjectProvider(LanguageServerWorkspaceFactory workspaceFactory, ILoggerFactory loggerFactory)
    {
        _workspaceFactory = workspaceFactory;
        _loggerFactory = loggerFactory;
        _canonicalBuildResult = AsyncLazy.Create(LoadCanonicalProjectAsync);
    }

    public async Task<ImmutableArray<ProjectFileInfo>> GetProjectInfoAsync(string miscDocumentPath, CancellationToken cancellationToken)
    {
        var canonicalInfos = await _canonicalBuildResult.GetValueAsync(cancellationToken);
        var miscDocFileInfo = new DocumentFileInfo(
            filePath: miscDocumentPath,
            logicalPath: Path.GetFileName(miscDocumentPath),
            isLinked: false,
            isGenerated: false,
            folders: []);

        var forkedInfos = canonicalInfos.SelectAsArray(info => info with
        {
            FilePath = miscDocumentPath,
            Documents = info.Documents.Add(miscDocFileInfo),
            FileGlobs = [],
        });

        return forkedInfos;
    }

    private async Task<ImmutableArray<ProjectFileInfo>> LoadCanonicalProjectAsync(CancellationToken cancellationToken)
    {
        // Set the FileBasedProgram feature flag so that '#:' is permitted without errors in rich misc files.
        // This allows us to avoid spurious errors for files which contain '#:' directives yet are not treated
        // as file-based programs (due to not being saved to disk, for example.)
        var virtualProjectXml = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net$(BundledNETCoreAppTargetFrameworkVersion)</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <Features>$(Features);FileBasedProgram</Features>
              </PropertyGroup>
            </Project>
            """;

        _tempDirectory = Path.Combine(Path.GetTempPath(), "roslyn-canonical-misc", Guid.NewGuid().ToString());

        Directory.CreateDirectory(_tempDirectory);
        var virtualProjectPath = Path.Combine(_tempDirectory, "Canonical.csproj");

        await using var buildHostProcessManager = new BuildHostProcessManager(
            _workspaceFactory.HostWorkspace.Services.SolutionServices.GetSupportedLanguages<ICommandLineParserService>(),
            globalMSBuildProperties: [],
            binaryLogPathProvider: null,
            _loggerFactory);
        var buildHost = await buildHostProcessManager.GetBuildHostAsync(BuildHostProcessKind.NetCore, virtualProjectPath, dotnetPath: null, cancellationToken);
        var loadedFile = await buildHost.LoadProjectAsync(virtualProjectPath, virtualProjectXml, languageName: LanguageNames.CSharp, cancellationToken);
        return await loadedFile.GetProjectFileInfosAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (_tempDirectory is not null)
        {
            IOUtilities.PerformIO(() => Directory.Delete(_tempDirectory, recursive: true));
        }
    }
}
