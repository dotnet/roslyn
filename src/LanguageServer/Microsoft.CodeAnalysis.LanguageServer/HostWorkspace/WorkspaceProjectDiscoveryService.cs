// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.IO.Enumeration;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportCSharpVisualBasicLspServiceFactory(typeof(WorkspaceProjectDiscoveryService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class WorkspaceProjectDiscoveryServiceFactory(
    ILoggerFactory loggerFactory) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new WorkspaceProjectDiscoveryService(
            lspServices.GetRequiredService<IWorkspaceFolderTracker>(),
            loggerFactory,
            lspServices.GetRequiredService<LanguageServerProjectSystem>().GetSupportedProjectFileExtensions());
}

internal readonly record struct ProjectDiscoveryResult(
    string? WorkspaceFolder,
    ImmutableArray<string> Projects);

internal sealed class WorkspaceProjectDiscoveryService : ILspService
{
    private const StringComparison s_pathComparison = StringComparison.OrdinalIgnoreCase;

    private readonly IWorkspaceFolderTracker _workspaceFolderTracker;
    private readonly ILogger _logger;
    private readonly ImmutableArray<string> _supportedProjectFileExtensions;
    private readonly Func<string, ImmutableArray<string>> _enumerateFiles;

    internal WorkspaceProjectDiscoveryService(
        IWorkspaceFolderTracker workspaceFolderTracker,
        ILoggerFactory loggerFactory,
        ImmutableArray<string> supportedProjectFileExtensions,
        Func<string, ImmutableArray<string>>? enumerateFiles = null)
    {
        _workspaceFolderTracker = workspaceFolderTracker;
        _logger = loggerFactory.CreateLogger<WorkspaceProjectDiscoveryService>();
        _supportedProjectFileExtensions = supportedProjectFileExtensions;
        _enumerateFiles = enumerateFiles ?? EnumerateFiles;
    }

    internal ProjectDiscoveryResult DiscoverProjects(string filePath, CancellationToken cancellationToken)
    {
        if (!PathUtilities.IsAbsolute(filePath))
            return new(null, []);

        filePath = NormalizePath(filePath);
        var workspaceFolders = _workspaceFolderTracker.GetRequiredWorkspaceFolderPaths();
        var workspaceFolder = GetDeepestContainingWorkspaceFolder(filePath, workspaceFolders);
        if (workspaceFolder is null)
            return new(null, []);

        var directory = PathUtilities.GetDirectoryName(filePath);
        while (PathUtilities.IsSameDirectoryOrChildOf(directory, workspaceFolder, s_pathComparison))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var projects = GetProjectsInDirectory(directory);
            if (!projects.IsEmpty)
                return new(workspaceFolder, projects);

            if (StringComparer.OrdinalIgnoreCase.Equals(directory, workspaceFolder))
                break;

            directory = PathUtilities.GetDirectoryName(directory);
        }

        return new(workspaceFolder, []);
    }

    private static string? GetDeepestContainingWorkspaceFolder(string path, ImmutableArray<string> workspaceFolders)
    {
        string? deepestWorkspaceFolder = null;
        foreach (var workspaceFolder in workspaceFolders)
        {
            if (PathUtilities.IsSameDirectoryOrChildOf(path, workspaceFolder, s_pathComparison) &&
                (deepestWorkspaceFolder is null || workspaceFolder.Length > deepestWorkspaceFolder.Length))
            {
                deepestWorkspaceFolder = workspaceFolder;
            }
        }

        return deepestWorkspaceFolder;
    }

    private ImmutableArray<string> GetProjectsInDirectory(string directory)
    {
        try
        {
            return [.. _enumerateFiles(directory)
                .Where(IsExistingSupportedProject)
                .Order(StringComparer.Ordinal)];
        }
        catch (Exception ex) when (IOUtilities.IsNormalIOException(ex))
        {
            _logger.LogWarning(
                "Could not enumerate project files in '{Directory}' while resolving workspace projects: {ExceptionMessage}",
                directory,
                ex.Message);
            return [];
        }
    }

    private bool IsExistingSupportedProject(string projectPath)
        => File.Exists(projectPath) && IsSupportedProjectExtension(projectPath);

    private bool IsSupportedProjectExtension(string path)
    {
        var extension = PathUtilities.GetExtension(path);
        if (extension is not ['.', .. var extensionWithoutDot])
            return false;

        foreach (var supported in _supportedProjectFileExtensions)
        {
            if (extensionWithoutDot.AsSpan().Equals(supported, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private ImmutableArray<string> EnumerateFiles(string directory)
    {
        // Filter by extension during enumeration itself so files that can't be projects never get a path string allocated.
        using var enumerator = new ProjectFileEnumerator(directory, _supportedProjectFileExtensions);
        var builder = ImmutableArray.CreateBuilder<string>();
        while (enumerator.MoveNext())
            builder.Add(enumerator.Current);

        return builder.ToImmutable();
    }

    private static string NormalizePath(string path)
        => Path.GetFullPath(path);

    private sealed class ProjectFileEnumerator(string directory, ImmutableArray<string> supportedExtensions)
        : FileSystemEnumerator<string>(directory, new EnumerationOptions { RecurseSubdirectories = false, IgnoreInaccessible = true })
    {
        protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
            => !entry.IsDirectory && IsSupportedExtension(Path.GetExtension(entry.FileName));

        protected override string TransformEntry(ref FileSystemEntry entry)
            => entry.ToFullPath();

        private bool IsSupportedExtension(ReadOnlySpan<char> extension)
        {
            if (extension is not ['.', .. var extensionWithoutDot])
                return false;

            foreach (var supported in supportedExtensions)
            {
                if (extensionWithoutDot.Equals(supported, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
