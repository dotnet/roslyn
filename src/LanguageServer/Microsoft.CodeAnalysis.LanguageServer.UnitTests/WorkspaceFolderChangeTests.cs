// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

/// <summary>
/// Tests for project-unload behavior driven by workspace/didChangeWorkspaceFolders notifications.
/// These tests verify the graph-based retention logic implemented in
/// <see cref="LanguageServerProjectLoader.UnloadProjectsNotReachableFromWorkspaceFoldersAsync"/>.
/// </summary>
public sealed class WorkspaceFolderChangeTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerHostTests(testOutputHelper)
{
    /// <summary>
    /// A project whose path is directly inside the active workspace folder must remain loaded.
    /// </summary>
    [Fact]
    public async Task ProjectInsideWorkspaceFolder_Retained()
    {
        var (exportProvider, _) = await LanguageServerTestComposition.CreateExportProviderAsync(
            LoggerFactory, includeDevKitComponents: false, MefCacheDirectory.Path, []);
        using var __ = exportProvider;

        var (projectSystem, projectFactory) = GetTestComponents(exportProvider);

        var workspaceRoot = MakeAbsolutePath("workspaceRoot");
        var insideProjectPath = Path.Combine(workspaceRoot, "src", "App.csproj");

        var insideProjectId = await AddProjectToWorkspaceAsync(projectFactory, insideProjectPath);
        var accessor = projectSystem.GetTestAccessor();
        accessor.AddPrimordialEntry(insideProjectPath, projectFactory, insideProjectId);

        // The workspace folder contains the project.
        await projectSystem.OnWorkspaceFoldersChangedAsync(
            addedFolderPaths: [workspaceRoot],
            removedFolderPaths: [],
            CancellationToken.None);

        Assert.True(accessor.IsTracked(insideProjectPath));
    }

    /// <summary>
    /// A project whose path is outside all active workspace folders must be unloaded.
    /// </summary>
    [Fact]
    public async Task ProjectOutsideWorkspaceFolder_Unloaded()
    {
        var (exportProvider, _) = await LanguageServerTestComposition.CreateExportProviderAsync(
            LoggerFactory, includeDevKitComponents: false, MefCacheDirectory.Path, []);
        using var __ = exportProvider;

        var (projectSystem, projectFactory) = GetTestComponents(exportProvider);

        var workspaceRoot = MakeAbsolutePath("workspaceRoot");
        var outsideProjectPath = MakeAbsolutePath(Path.Combine("otherFolder", "Lib.csproj"));

        var outsideProjectId = await AddProjectToWorkspaceAsync(projectFactory, outsideProjectPath);
        var accessor = projectSystem.GetTestAccessor();
        accessor.AddPrimordialEntry(outsideProjectPath, projectFactory, outsideProjectId);

        // Add a workspace folder that does NOT contain the project.
        await projectSystem.OnWorkspaceFoldersChangedAsync(
            addedFolderPaths: [workspaceRoot],
            removedFolderPaths: [],
            CancellationToken.None);

        Assert.False(accessor.IsTracked(outsideProjectPath));
    }

    /// <summary>
    /// A project outside the workspace folders must NOT be unloaded if it is directly
    /// referenced by a project that is inside the workspace folders.
    /// </summary>
    [Fact]
    public async Task ProjectOutsideWorkspaceFolder_ReferencedByInScopeProject_Retained()
    {
        var (exportProvider, _) = await LanguageServerTestComposition.CreateExportProviderAsync(
            LoggerFactory, includeDevKitComponents: false, MefCacheDirectory.Path, []);
        using var __ = exportProvider;

        var (projectSystem, projectFactory) = GetTestComponents(exportProvider);

        var workspaceRoot = MakeAbsolutePath("workspaceRoot");
        var appProjectPath = Path.Combine(workspaceRoot, "App.csproj");
        var libProjectPath = MakeAbsolutePath(Path.Combine("shared", "Lib.csproj"));

        // Create both projects; App references Lib.
        var libProjectId = ProjectId.CreateNewId("Lib");
        var appProjectId = ProjectId.CreateNewId("App");

        projectFactory.ApplyChangeToWorkspace(ws =>
        {
            ws.OnProjectAdded(CreateProjectInfo(libProjectId, "Lib", libProjectPath));
            ws.OnProjectAdded(CreateProjectInfo(appProjectId, "App", appProjectPath));
        });
        projectFactory.ApplyChangeToWorkspace(ws =>
            ws.OnProjectReferenceAdded(appProjectId, new ProjectReference(libProjectId)));

        var accessor = projectSystem.GetTestAccessor();
        accessor.AddPrimordialEntry(libProjectPath, projectFactory, libProjectId);
        accessor.AddPrimordialEntry(appProjectPath, projectFactory, appProjectId);

        // workspace folder contains App but not Lib.
        await projectSystem.OnWorkspaceFoldersChangedAsync(
            addedFolderPaths: [workspaceRoot],
            removedFolderPaths: [],
            CancellationToken.None);

        // App (root) must be retained; Lib (referenced by root) must also be retained.
        Assert.True(accessor.IsTracked(appProjectPath));
        Assert.True(accessor.IsTracked(libProjectPath));
    }

    /// <summary>
    /// Transitive project references must also be retained.
    /// e.g. App → Core → Util, with only App inside the workspace folder.
    /// </summary>
    [Fact]
    public async Task TransitivelyReferencedProjectsOutsideFolder_Retained()
    {
        var (exportProvider, _) = await LanguageServerTestComposition.CreateExportProviderAsync(
            LoggerFactory, includeDevKitComponents: false, MefCacheDirectory.Path, []);
        using var __ = exportProvider;

        var (projectSystem, projectFactory) = GetTestComponents(exportProvider);

        var workspaceRoot = MakeAbsolutePath("workspaceRoot");
        var appProjectPath = Path.Combine(workspaceRoot, "App.csproj");
        var coreProjectPath = MakeAbsolutePath(Path.Combine("shared", "Core.csproj"));
        var utilProjectPath = MakeAbsolutePath(Path.Combine("shared", "Util.csproj"));

        var appProjectId = ProjectId.CreateNewId("App");
        var coreProjectId = ProjectId.CreateNewId("Core");
        var utilProjectId = ProjectId.CreateNewId("Util");

        projectFactory.ApplyChangeToWorkspace(ws =>
        {
            ws.OnProjectAdded(CreateProjectInfo(utilProjectId, "Util", utilProjectPath));
            ws.OnProjectAdded(CreateProjectInfo(coreProjectId, "Core", coreProjectPath));
            ws.OnProjectAdded(CreateProjectInfo(appProjectId, "App", appProjectPath));
        });
        // App → Core → Util
        projectFactory.ApplyChangeToWorkspace(ws =>
        {
            ws.OnProjectReferenceAdded(appProjectId, new ProjectReference(coreProjectId));
            ws.OnProjectReferenceAdded(coreProjectId, new ProjectReference(utilProjectId));
        });

        var accessor = projectSystem.GetTestAccessor();
        accessor.AddPrimordialEntry(appProjectPath, projectFactory, appProjectId);
        accessor.AddPrimordialEntry(coreProjectPath, projectFactory, coreProjectId);
        accessor.AddPrimordialEntry(utilProjectPath, projectFactory, utilProjectId);

        await projectSystem.OnWorkspaceFoldersChangedAsync(
            addedFolderPaths: [workspaceRoot],
            removedFolderPaths: [],
            CancellationToken.None);

        Assert.True(accessor.IsTracked(appProjectPath));
        Assert.True(accessor.IsTracked(coreProjectPath));
        Assert.True(accessor.IsTracked(utilProjectPath));
    }

    /// <summary>
    /// An external project that has no path into the active workspace folders AND is not
    /// referenced by any in-scope project must be unloaded.
    /// </summary>
    [Fact]
    public async Task UnreferencedProjectOutsideFolder_Unloaded()
    {
        var (exportProvider, _) = await LanguageServerTestComposition.CreateExportProviderAsync(
            LoggerFactory, includeDevKitComponents: false, MefCacheDirectory.Path, []);
        using var __ = exportProvider;

        var (projectSystem, projectFactory) = GetTestComponents(exportProvider);

        var workspaceRoot = MakeAbsolutePath("workspaceRoot");
        var appProjectPath = Path.Combine(workspaceRoot, "App.csproj");
        var oldLibProjectPath = MakeAbsolutePath(Path.Combine("old", "OldLib.csproj"));

        var appProjectId = ProjectId.CreateNewId("App");
        var oldLibProjectId = ProjectId.CreateNewId("OldLib");

        projectFactory.ApplyChangeToWorkspace(ws =>
        {
            ws.OnProjectAdded(CreateProjectInfo(oldLibProjectId, "OldLib", oldLibProjectPath));
            ws.OnProjectAdded(CreateProjectInfo(appProjectId, "App", appProjectPath));
            // No reference from App to OldLib
        });

        var accessor = projectSystem.GetTestAccessor();
        accessor.AddPrimordialEntry(appProjectPath, projectFactory, appProjectId);
        accessor.AddPrimordialEntry(oldLibProjectPath, projectFactory, oldLibProjectId);

        await projectSystem.OnWorkspaceFoldersChangedAsync(
            addedFolderPaths: [workspaceRoot],
            removedFolderPaths: [],
            CancellationToken.None);

        Assert.True(accessor.IsTracked(appProjectPath));
        Assert.False(accessor.IsTracked(oldLibProjectPath));
    }

    /// <summary>
    /// Verifies prefix/path boundary safety: a workspace folder "/repo" must not match
    /// a project at "/repo2/proj.csproj" (which shares the prefix but is in a different directory).
    /// </summary>
    [Fact]
    public async Task PathPrefixSafety_SimilarNamedFolderDoesNotMatch()
    {
        var (exportProvider, _) = await LanguageServerTestComposition.CreateExportProviderAsync(
            LoggerFactory, includeDevKitComponents: false, MefCacheDirectory.Path, []);
        using var __ = exportProvider;

        var (projectSystem, projectFactory) = GetTestComponents(exportProvider);

        var workspaceRoot = MakeAbsolutePath("repo");
        var prefixTrapProjectPath = MakeAbsolutePath(Path.Combine("repo2", "Proj.csproj"));

        var projectId = await AddProjectToWorkspaceAsync(projectFactory, prefixTrapProjectPath);
        var accessor = projectSystem.GetTestAccessor();
        accessor.AddPrimordialEntry(prefixTrapProjectPath, projectFactory, projectId);

        // workspace folder is "/repo"; project is in "/repo2" — must NOT be retained.
        await projectSystem.OnWorkspaceFoldersChangedAsync(
            addedFolderPaths: [workspaceRoot],
            removedFolderPaths: [],
            CancellationToken.None);

        Assert.False(accessor.IsTracked(prefixTrapProjectPath));
    }

    /// <summary>
    /// When all workspace folders are removed, all tracked projects should be unloaded.
    /// </summary>
    [Fact]
    public async Task RemoveAllWorkspaceFolders_AllProjectsUnloaded()
    {
        var (exportProvider, _) = await LanguageServerTestComposition.CreateExportProviderAsync(
            LoggerFactory, includeDevKitComponents: false, MefCacheDirectory.Path, []);
        using var __ = exportProvider;

        var (projectSystem, projectFactory) = GetTestComponents(exportProvider);

        var workspaceRoot = MakeAbsolutePath("workspaceRoot");
        var proj1Path = Path.Combine(workspaceRoot, "Proj1.csproj");
        var proj2Path = Path.Combine(workspaceRoot, "sub", "Proj2.csproj");

        var proj1Id = await AddProjectToWorkspaceAsync(projectFactory, proj1Path);
        var proj2Id = await AddProjectToWorkspaceAsync(projectFactory, proj2Path);

        projectSystem.SetInitialWorkspaceFolderPaths([workspaceRoot]);
        var accessor = projectSystem.GetTestAccessor();
        accessor.AddPrimordialEntry(proj1Path, projectFactory, proj1Id);
        accessor.AddPrimordialEntry(proj2Path, projectFactory, proj2Id);

        // Remove the only workspace folder — both projects should be unloaded.
        await projectSystem.OnWorkspaceFoldersChangedAsync(
            addedFolderPaths: [],
            removedFolderPaths: [workspaceRoot],
            CancellationToken.None);

        Assert.False(accessor.IsTracked(proj1Path));
        Assert.False(accessor.IsTracked(proj2Path));
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static (LanguageServerProjectSystem projectSystem, ProjectSystemProjectFactory projectFactory)
        GetTestComponents(Microsoft.VisualStudio.Composition.ExportProvider exportProvider)
    {
        var projectSystem = exportProvider.GetExportedValue<LanguageServerProjectSystem>();
        var workspaceFactory = exportProvider.GetExportedValue<LanguageServerWorkspaceFactory>();
        return (projectSystem, workspaceFactory.HostProjectFactory);
    }

    private static async Task<ProjectId> AddProjectToWorkspaceAsync(
        ProjectSystemProjectFactory projectFactory, string projectPath)
    {
        var projectId = ProjectId.CreateNewId(Path.GetFileNameWithoutExtension(projectPath));
        await projectFactory.ApplyChangeToWorkspaceAsync(
            ws => ws.OnProjectAdded(CreateProjectInfo(projectId, Path.GetFileNameWithoutExtension(projectPath), projectPath)));
        return projectId;
    }

    private static ProjectInfo CreateProjectInfo(ProjectId projectId, string name, string filePath)
        => ProjectInfo.Create(
            projectId,
            VersionStamp.Default,
            name,
            name,
            LanguageNames.CSharp,
            filePath: filePath);

    private static string MakeAbsolutePath(string relativePath)
    {
        if (OperatingSystem.IsWindows())
            return Path.Combine(@"Z:\", relativePath);
        else
            return Path.Combine("/", relativePath);
    }
}
