// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Logging;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

[UseExportProvider]
public sealed class WorkspaceStructureLoggerTests
{
    [Fact]
    public async Task EmptySolution_ProducesWorkspaceElement()
    {
        using var workspace = new AdhocWorkspace();

        var result = await new WorkspaceStructureLogger().BuildWorkspaceStructureAsync(
            workspace.CurrentSolution,
            workspace.Kind,
            progress: null,
            CancellationToken.None);

        var root = result.Root;
        Assert.NotNull(root);
        Assert.Equal("workspace", root!.Name.LocalName);
        Assert.Equal(workspace.Kind, root.Attribute("kind")?.Value);
        Assert.Empty(root.Elements("project"));
    }

    [Fact]
    public async Task SingleProject_IncludesProjectAttributes()
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);

        var result = await new WorkspaceStructureLogger().BuildWorkspaceStructureAsync(
            workspace.CurrentSolution,
            workspace.Kind,
            progress: null,
            CancellationToken.None);

        var projectElement = result.Root!.Element("project");
        Assert.NotNull(projectElement);
        Assert.Equal("TestProject", projectElement!.Attribute("name")?.Value);
        Assert.Equal(LanguageNames.CSharp, projectElement.Attribute("language")?.Value);
    }

    [Fact]
    public async Task ProjectWithDocument_IncludesDocumentElement()
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        workspace.AddDocument(project.Id, "Test.cs", SourceText.From("class C { }"));

        var result = await new WorkspaceStructureLogger().BuildWorkspaceStructureAsync(
            workspace.CurrentSolution,
            workspace.Kind,
            progress: null,
            CancellationToken.None);

        var projectElement = result.Root!.Element("project");
        var docsElement = projectElement!.Element("workspaceDocuments");
        Assert.NotNull(docsElement);
        Assert.Single(docsElement!.Elements("document"));
    }

    [Fact]
    public async Task ProjectWithMetadataReference_IncludesReferenceElements()
    {
        using var workspace = new AdhocWorkspace();
        var projectInfo = ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Default,
            "TestProject",
            "TestProject",
            LanguageNames.CSharp,
            metadataReferences: [NetFramework.mscorlib]);
        workspace.AddProject(projectInfo);

        var result = await new WorkspaceStructureLogger().BuildWorkspaceStructureAsync(
            workspace.CurrentSolution,
            workspace.Kind,
            progress: null,
            CancellationToken.None);

        var projectElement = result.Root!.Element("project");
        var workspaceRefs = projectElement!.Element("workspaceReferences");
        Assert.NotNull(workspaceRefs);
        Assert.NotEmpty(workspaceRefs!.Elements("peReference"));
    }

    [Fact]
    public async Task ProjectWithProjectReference_IncludesProjectReferenceElement()
    {
        using var workspace = new AdhocWorkspace();
        var referencedProject = workspace.AddProject("ReferencedProject", LanguageNames.CSharp);
        var projectInfo = ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Default,
            "TestProject",
            "TestProject",
            LanguageNames.CSharp,
            projectReferences: [new ProjectReference(referencedProject.Id)]);
        workspace.AddProject(projectInfo);

        var result = await new WorkspaceStructureLogger().BuildWorkspaceStructureAsync(
            workspace.CurrentSolution,
            workspace.Kind,
            progress: null,
            CancellationToken.None);

        var testProjectElement = result.Root!.Elements("project")
            .Single(e => e.Attribute("name")?.Value == "TestProject");
        var workspaceRefs = testProjectElement.Element("workspaceReferences");
        Assert.NotNull(workspaceRefs);
        Assert.Single(workspaceRefs!.Elements("projectReference"));
    }

    [Fact]
    public async Task ProgressIsReported()
    {
        using var workspace = new AdhocWorkspace();
        workspace.AddProject("Project1", LanguageNames.CSharp);
        workspace.AddProject("Project2", LanguageNames.CSharp);

        var reports = new List<(int current, int total)>();

        await new WorkspaceStructureLogger().BuildWorkspaceStructureAsync(
            workspace.CurrentSolution,
            workspace.Kind,
            new SynchronousProgress<(int current, int total)>(reports.Add),
            CancellationToken.None);

        Assert.Equal(2, reports.Count);
        Assert.Equal((1, 2), reports[0]);
        Assert.Equal((2, 2), reports[1]);
    }

    [Fact]
    public async Task AdditionalProjectElements_AreIncluded()
    {
        using var workspace = new AdhocWorkspace();
        workspace.AddProject("TestProject", LanguageNames.CSharp);

        var result = await new TestWorkspaceStructureLogger().BuildWorkspaceStructureAsync(
            workspace.CurrentSolution,
            workspace.Kind,
            progress: null,
            CancellationToken.None);

        var projectElement = result.Root!.Element("project");
        var customElement = projectElement!.Element("customElement");
        Assert.NotNull(customElement);
        Assert.Equal("value", customElement!.Attribute("key")?.Value);
    }

    [Fact]
    public async Task CompilationDiagnostics_AreIncluded()
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        workspace.AddDocument(project.Id, "Test.cs", SourceText.From("class C { invalid }"));

        var result = await new WorkspaceStructureLogger().BuildWorkspaceStructureAsync(
            workspace.CurrentSolution,
            workspace.Kind,
            progress: null,
            CancellationToken.None);

        var projectElement = result.Root!.Element("project");
        var diagnosticsElement = projectElement!.Element("diagnostics");
        Assert.NotNull(diagnosticsElement);
        Assert.NotEmpty(diagnosticsElement!.Elements("diagnostic"));
    }

    [Fact]
    public async Task Cancellation_ThrowsOperationCanceled()
    {
        using var workspace = new AdhocWorkspace();
        workspace.AddProject("TestProject", LanguageNames.CSharp);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new WorkspaceStructureLogger().BuildWorkspaceStructureAsync(
                workspace.CurrentSolution,
                workspace.Kind,
                progress: null,
                cts.Token));
    }

    private sealed class TestWorkspaceStructureLogger : WorkspaceStructureLogger
    {
        protected override Task<IEnumerable<XElement>> CreateAdditionalProjectElementsAsync(Project project, CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<XElement>>(
                [new XElement("customElement", new XAttribute("key", "value"))]);
        }
    }

    // We use this implementation because Progress<T> posts callbacks asynchronously on net472.
    private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }
}
