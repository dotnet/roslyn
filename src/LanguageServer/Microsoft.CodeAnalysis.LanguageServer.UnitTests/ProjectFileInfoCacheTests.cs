// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias MSBuildWorkspacesContracts;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Extensions.Logging;
using Xunit;
using ProjectFileInfo = MSBuildWorkspacesContracts::Microsoft.CodeAnalysis.MSBuild.ProjectFileInfo;
using DocumentFileInfo = MSBuildWorkspacesContracts::Microsoft.CodeAnalysis.MSBuild.DocumentFileInfo;
using PackageReferenceItem = MSBuildWorkspacesContracts::Microsoft.CodeAnalysis.MSBuild.PackageReferenceItem;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class ProjectFileInfoCacheTests : IDisposable
{
    private readonly TempRoot _tempRoot = new();
    private readonly ILoggerFactory _loggerFactory = new LoggerFactory();

    public void Dispose()
    {
        _tempRoot.Dispose();
        _loggerFactory.Dispose();
    }

    [Fact]
    public void RoundTrip_SingleProjectFileInfo()
    {
        var tempDir = _tempRoot.CreateDirectory();
        var entryPointFile = tempDir.CreateFile("Program.cs").WriteAllText("Console.WriteLine();");
        var logger = _loggerFactory.CreateLogger("test");

        var original = new ProjectFileInfo
        {
            Language = LanguageNames.CSharp,
            FilePath = "/path/to/project.csproj",
            OutputFilePath = "/path/to/output.dll",
            OutputRefFilePath = "/path/to/ref/output.dll",
            IntermediateOutputFilePath = "/path/to/obj/output.dll",
            DefaultNamespace = "MyNamespace",
            TargetFramework = "net10.0",
            TargetFrameworkIdentifier = ".NETCoreApp",
            CommandLineArgs = ["/reference:System.dll", "/out:output.dll", "Program.cs"],
            Documents = [new DocumentFileInfo("Program.cs", "Program.cs", isLinked: false, isGenerated: false, [])],
            AdditionalDocuments = [],
            AnalyzerConfigDocuments = [],
            ProjectReferences = [],
            ProjectCapabilities = ["CSharp"],
            ContentFilePaths = [],
            PackageReferences = [new PackageReferenceItem("Newtonsoft.Json", "[13.0.1, )")],
            MetadataReferences = [],
            FileGlobs = [],
        };

        var infos = ImmutableArray.Create(original);
        ProjectFileInfoCache.WriteToCache(entryPointFile.Path, infos, logger);

        var cached = ProjectFileInfoCache.TryReadFromCache(entryPointFile.Path, logger);
        Assert.False(cached.IsDefault);
        Assert.Single(cached);

        var roundTripped = cached[0];
        Assert.Equal(original.Language, roundTripped.Language);
        Assert.Equal(original.FilePath, roundTripped.FilePath);
        Assert.Equal(original.OutputFilePath, roundTripped.OutputFilePath);
        Assert.Equal(original.OutputRefFilePath, roundTripped.OutputRefFilePath);
        Assert.Equal(original.IntermediateOutputFilePath, roundTripped.IntermediateOutputFilePath);
        Assert.Equal(original.DefaultNamespace, roundTripped.DefaultNamespace);
        Assert.Equal(original.TargetFramework, roundTripped.TargetFramework);
        Assert.Equal(original.TargetFrameworkIdentifier, roundTripped.TargetFrameworkIdentifier);
        Assert.Equal<string>(original.CommandLineArgs, roundTripped.CommandLineArgs);
        Assert.Equal(original.Documents.Length, roundTripped.Documents.Length);
        Assert.Equal(original.Documents[0].FilePath, roundTripped.Documents[0].FilePath);
        Assert.Equal(original.PackageReferences.Length, roundTripped.PackageReferences.Length);
        Assert.Equal(original.PackageReferences[0].Name, roundTripped.PackageReferences[0].Name);
        Assert.Equal<string>(original.ProjectCapabilities, roundTripped.ProjectCapabilities);
    }

    [Fact]
    public void ReadFromCache_NoCacheFile_ReturnsDefault()
    {
        var tempDir = _tempRoot.CreateDirectory();
        var entryPointFile = Path.Combine(tempDir.Path, "DoesNotExist.cs");
        var logger = _loggerFactory.CreateLogger("test");

        var result = ProjectFileInfoCache.TryReadFromCache(entryPointFile, logger);
        Assert.True(result.IsDefault);
    }
}
