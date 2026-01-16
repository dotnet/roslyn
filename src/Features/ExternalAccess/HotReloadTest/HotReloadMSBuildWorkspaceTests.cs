// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.CodeAnalysis.ExternalAccess.HotReload.Api.UnitTests;

public class HotReloadMSBuildWorkspaceTests
{
    private static HotReloadMSBuildWorkspace CreateWorkspace()
        => new(NullLogger.Instance, getBuildProjects: _ => throw ExceptionUtilities.Unreachable());

    [Fact]
    public void UpdateSolution()
    {
        using var workspace = CreateWorkspace();

        var projectId1 = ProjectId.CreateNewId();
        var documentAId1 = DocumentId.CreateNewId(projectId1);

        var projectInfo1 = ProjectInfo.Create(
            projectId1,
            VersionStamp.Create(),
            name: "P1",
            assemblyName: "A1",
            language: LanguageNames.CSharp,
            filePath: Path.Combine(TempRoot.Root, "P1.csproj"),
            outputFilePath: Path.Combine(TempRoot.Root, "P1.dll"),
            compilationOptions: TestOptions.DebugDll,
            parseOptions: TestOptions.Regular14,
            documents:
            [
                DocumentInfo.Create(
                    documentAId1,
                    name: "A",
                    filePath: Path.Combine(TempRoot.Root, "A.cs"),
                    loader: TextLoader.From(TextAndVersion.Create(SourceText.From("class A;", Encoding.UTF8, SourceHashAlgorithm.Sha256), VersionStamp.Create())))
            ],
            projectReferences: [],
            metadataReferences: [],
            analyzerReferences: [],
            additionalDocuments: [],
            isSubmission: false,
            hostObjectType: null,
            outputRefFilePath: Path.Combine(TempRoot.Root, "ref", "P1.dll"))
            .WithChecksumAlgorithm(SourceHashAlgorithm.Sha256)
            .WithAnalyzerConfigDocuments([])
            .WithCompilationOutputInfo(new CompilationOutputInfo(
                assemblyPath: Path.Combine(TempRoot.Root, "obj", "P1.dll"),
                generatedFilesOutputDirectory: Path.Combine(TempRoot.Root, "obj")));

        var solution1 = workspace.UpdateSolution([projectInfo1]);
        Assert.Equal(projectId1, solution1.Projects.Single().Id);

        var projectId2 = ProjectId.CreateNewId();
        var documentBId2 = DocumentId.CreateNewId(projectId2);

        var projectInfo2 = ProjectInfo.Create(
            projectId2,
            VersionStamp.Create(),
            name: "P1",
            assemblyName: "A1",
            language: LanguageNames.CSharp,
            filePath: Path.Combine(TempRoot.Root, "P1.csproj"),
            outputFilePath: Path.Combine(TempRoot.Root, "P1.dll"),
            compilationOptions: TestOptions.DebugDll,
            parseOptions: TestOptions.Regular14,
            documents:
            [
                DocumentInfo.Create(
                    DocumentId.CreateNewId(projectId2),
                    name: "A",
                    filePath: Path.Combine(TempRoot.Root, "A.cs"),
                    loader: TextLoader.From(TextAndVersion.Create(SourceText.From("class C;", Encoding.UTF8, SourceHashAlgorithm.Sha256), VersionStamp.Create()))),
                DocumentInfo.Create(
                    documentBId2,
                    name: "B",
                    filePath: Path.Combine(TempRoot.Root, "B.cs"),
                    loader: TextLoader.From(TextAndVersion.Create(SourceText.From("class C;", Encoding.UTF8, SourceHashAlgorithm.Sha256), VersionStamp.Create())))
            ],
            projectReferences: [],
            metadataReferences: [],
            analyzerReferences: [],
            additionalDocuments: [],
            isSubmission: false,
            hostObjectType: null,
            outputRefFilePath: Path.Combine(TempRoot.Root, "ref", "P1.dll"))
            .WithChecksumAlgorithm(SourceHashAlgorithm.Sha256)
            .WithAnalyzerConfigDocuments([])
            .WithCompilationOutputInfo(new CompilationOutputInfo(
                assemblyPath: Path.Combine(TempRoot.Root, "obj", "P1.dll"),
                generatedFilesOutputDirectory: Path.Combine(TempRoot.Root, "obj")));

        var solution2 = workspace.UpdateSolution([projectInfo2]);
        var project2 = solution2.Projects.Single();

        // ids have been mapped:
        Assert.Equal(projectId1, project2.Id);
        Assert.Equal(["A", "B"], project2.Documents.Select(d => d.Name));
        Assert.Equal(documentAId1, project2.DocumentIds[0]);

        // project properties preserved:
        Assert.Equal("P1", project2.Name);
        Assert.Equal("A1", project2.AssemblyName);
        Assert.Equal(LanguageNames.CSharp, project2.Language);
        Assert.Equal(Path.Combine(TempRoot.Root, "P1.csproj"), project2.FilePath);
        Assert.Equal(Path.Combine(TempRoot.Root, "P1.dll"), project2.OutputFilePath);
        Assert.Equal(Path.Combine(TempRoot.Root, "ref", "P1.dll"), project2.OutputRefFilePath);
        Assert.False(project2.IsSubmission);
        Assert.Equal(SourceHashAlgorithm.Sha256, project2.State.ChecksumAlgorithm);
        Assert.Equal(Path.Combine(TempRoot.Root, "obj", "P1.dll"), project2.CompilationOutputInfo.AssemblyPath);
        Assert.Equal(Path.Combine(TempRoot.Root, "obj"), project2.CompilationOutputInfo.GeneratedFilesOutputDirectory);
    }
}
