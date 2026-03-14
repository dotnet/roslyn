// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

internal sealed class CanonicalMiscellaneousFilesProjectProvider
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly AsyncLazy<ImmutableArray<ProjectFileInfo>> _canonicalBuildResult;

    public CanonicalMiscellaneousFilesProjectProvider(ILoggerFactory loggerFactory)
    {
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

        var tempDirectory = Path.Combine(Path.GetTempPath(), "roslyn-canonical-misc", Guid.NewGuid().ToString());

        try
        {
            Directory.CreateDirectory(tempDirectory);
            var virtualProjectPath = Path.Combine(tempDirectory, "Canonical.csproj");

            const BuildHostProcessKind buildHostKind = BuildHostProcessKind.NetCore;
            await using var buildHostProcessManager = new BuildHostProcessManager([LanguageNames.CSharp], [], null, _loggerFactory);
            var buildHost = await buildHostProcessManager.GetBuildHostAsync(buildHostKind, virtualProjectPath, dotnetPath: null, cancellationToken);
            var loadedFile = await buildHost.LoadProjectAsync(virtualProjectPath, virtualProjectXml, languageName: LanguageNames.CSharp, cancellationToken);

            return await loadedFile.GetProjectFileInfosAsync(cancellationToken);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
