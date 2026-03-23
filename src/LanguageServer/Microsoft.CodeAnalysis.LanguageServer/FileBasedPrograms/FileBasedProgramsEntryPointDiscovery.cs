// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.IO.Enumeration;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Features.Workspaces;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

[Shared]
[ExportLspServiceFactory(typeof(FileBasedProgramsEntryPointDiscovery), ProtocolConstants.RoslynLspLanguagesContract)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class FileBasedProgramsEntryPointDiscoveryFactory(IGlobalOptionService globalOptionService, ILoggerFactory loggerFactory) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        return new FileBasedProgramsEntryPointDiscovery(globalOptionService, loggerFactory, lspServices);
    }
}

internal sealed partial class FileBasedProgramsEntryPointDiscovery(
    IGlobalOptionService globalOptionService, ILoggerFactory loggerFactory, LspServices lspServices) : ILspService, IOnInitialized
{
    private static readonly StringComparer s_pathComparer = StringComparer.OrdinalIgnoreCase;

    /// <summary>Directories which are ignored per convention.</summary>
    /// <remarks>Some conventional directories like '.git' and '.vs' are expected to be marked hidden and will be automatically ignored by discovery.</remarks>
    private static readonly ImmutableArray<string> s_ignoredDirectories = [
        "artifacts",
        "bin",
        "obj",
        "node_modules"
    ];

    private readonly ILogger _logger = loggerFactory.CreateLogger<FileBasedProgramsEntryPointDiscovery>();
    private ImmutableArray<string> _workspaceFolders;

    public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
    {
        var initializeManager = context.GetRequiredService<IInitializeManager>();
        var initializeParams = initializeManager.TryGetInitializeParams();
        Contract.ThrowIfNull(initializeParams);
        _workspaceFolders = initializeParams.WorkspaceFolders is [_, ..] workspaceFolders ? GetFolderPaths(workspaceFolders) : [];
        Task.Run(async () =>
        {
            try
            {
                await FindAndLoadEntryPointsAsync();
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }, cancellationToken);

        return Task.CompletedTask;

        static ImmutableArray<string> GetFolderPaths(WorkspaceFolder[] workspaceFolders)
        {
            var builder = ArrayBuilder<string>.GetInstance(workspaceFolders.Length);
            foreach (var workspaceFolder in workspaceFolders)
            {
                if (workspaceFolder.DocumentUri.ParsedUri is not { } parsedUri)
                    continue;

                var workspaceFolderPath = ProtocolConversions.GetDocumentFilePathFromUri(parsedUri);
                builder.Add(workspaceFolderPath);
            }

            return builder.ToImmutableAndFree();
        }
    }

    internal async Task FindAndLoadEntryPointsAsync()
    {
        Contract.ThrowIfTrue(_workspaceFolders.IsDefault, $"{nameof(OnInitializedAsync)} must be called before {nameof(FindAndLoadEntryPointsAsync)}.");

        if (_workspaceFolders.IsEmpty)
        {
            _logger.LogDebug("No workspace folders to search for file-based apps.");
            return;
        }

        if (!globalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms))
        {
            _logger.LogDebug(@"""enableFileBasedPrograms"" is false. Not discovering entry points.");
            return;
        }

        if (!globalOptionService.GetOption(FileBasedAppsOptionsStorage.EnableAutomaticDiscovery))
        {
            _logger.LogDebug(@"""dotnet.fileBasedApps.enableAutomaticDiscovery"" is false. Not discovering entry points.");
            return;
        }

        if (lspServices.GetService<ILspMiscellaneousFilesWorkspaceProvider>()
            is not FileBasedProgramsProjectSystem fileBasedProgramsProjectSystem)
        {
            _logger.LogWarning("Did not find FileBasedProgramsProjectSystem. Not discovering entry points.");
            return;
        }

        // Note: the overwhelmingly common case is when there is just one workspace folder.
        // For simplicity we orient our search around one workspace folder at a time.
        foreach (var workspaceFolder in _workspaceFolders)
        {
            await FindAndLoadEntryPointsAsync(workspaceFolder);
        }

        async Task FindAndLoadEntryPointsAsync(string workspaceFolder)
        {
            foreach (var fileBasedAppPath in FindEntryPoints(workspaceFolder))
            {
                await fileBasedProgramsProjectSystem.TryBeginLoadingFileBasedAppAsync(fileBasedAppPath);
            }
        }
    }

    internal IEnumerable<string> FindEntryPoints(string workspaceFolder)
    {
        var stopwatch = Stopwatch.StartNew();
        var cacheDirectory = VirtualProjectXmlProvider.GetDiscoveryCacheDirectory(workspaceFolder);
        var cacheFilePath = Path.Join(cacheDirectory, "cache.json");
        Cache? cache = null;
        try
        {
            using var cacheFile = File.OpenRead(cacheFilePath);
            cache = JsonSerializer.Deserialize(cacheFile, CacheSerializerContext.Default.Cache);

            // Drop malformed caches
            if (cache?.WorkspacePath.Equals(workspaceFolder, StringComparison.OrdinalIgnoreCase) == false
                || cache is { FileBasedAppFullPaths.IsDefault: true } or { DirectoriesContainingCsproj.IsDefault: true })
            {
                cache = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Could not read cache file: {ex.Message}", ex.Message);
        }

        cache ??= new Cache(workspaceFolder, DateTimeOffset.MinValue, FileBasedAppFullPaths: [], DirectoriesContainingCsproj: []);

        // Note: file system timestamps can have a coarser resolution than DateTimeOffset.
        // This means that using APIs like `DateTimeOffset.UtcNow`, then writing a file, then sampling its LastWriteTimeUtc,
        // can result in the UtcNow that we accessed earlier, having a "later" value than the LastWriteTimeUtc.
        //
        // To deal with this accurately, we write a timestamp file to the filesystem, then get a walkStartTimeUtc from its LastWriteTimeUtc.
        // Timestamps we encounter which compare equal to the walkStartTimeUtc timestamp, must be treated as possibly being newer than the walkStartTimeUtc timestamp.
        var walkStartTimeUtc = IOUtilities.PerformIO(() =>
        {
            var sentinelPath = Path.Join(cacheDirectory, $".walk-timestamp-{Guid.NewGuid()}");
            File.WriteAllBytes(sentinelPath, []);
            var lastWriteTime = File.GetLastWriteTimeUtc(sentinelPath);
            File.Delete(sentinelPath);
            return lastWriteTime;
        }, defaultValue: cache.LastWalkTimeUtc);

        // Initial cache loop: load known file-based apps
        var csprojInConeChecker = lspServices.GetRequiredService<CsprojInConeChecker>();
        var newFileBasedAppsBuilder = ArrayBuilder<string>.GetInstance(cache.FileBasedAppFullPaths.Length);
        foreach (var fileBasedAppPath in cache.FileBasedAppFullPaths)
        {
            var fileInfo = new FileInfo(fileBasedAppPath);
            if (!fileInfo.Exists)
            {
                // Deleted since our last walk.
                continue;
            }

            if (csprojInConeChecker.IsContainedInCsprojCone(fileBasedAppPath))
            {
                // A csproj has appeared in the file's directory cone since our last walk.
                continue;
            }

            if ((fileInfo.CreationTimeUtc >= cache.LastWalkTimeUtc || fileInfo.LastWriteTimeUtc >= cache.LastWalkTimeUtc)
                && !IsFileBasedApp(fileInfo.FullName))
            {
                // Changed to stop being a file-based app since our last walk.
                continue;
            }

            newFileBasedAppsBuilder.Add(fileBasedAppPath);
            _logger.LogInformation("MAGIC Discovered file-based app (cache hit): {fileBasedAppPath}", fileBasedAppPath);
            yield return fileBasedAppPath;
        }

        // Search for changes since our last walk.
        // Note: if the workspace root itself contains a csproj (rare case), we don't even want to create an enumerator.
        var directoriesContainingCsprojBuilder = ArrayBuilder<string>.GetInstance(cache.DirectoriesContainingCsproj.Length);
        if (!Directory.EnumerateFiles(cache.WorkspacePath, "*.csproj").Any())
        {
            var enumerator = new IncrementalEntryPointEnumerator(cache, directoriesContainingCsprojBuilder);
            while (enumerator.MoveNext())
            {
                var fileBasedAppPath = enumerator.Current;
                newFileBasedAppsBuilder.Add(fileBasedAppPath);
                _logger.LogInformation("MAGIC Discovered file-based app (cache miss): {csFilePath}", fileBasedAppPath);
                yield return fileBasedAppPath;
            }

            stopwatch.Stop();
            _logger.LogInformation("MAGIC Finished discovery in {workspaceFolder} in {stopwatch.ElapsedMilliseconds} milliseconds", workspaceFolder, stopwatch.ElapsedMilliseconds);
            newFileBasedAppsBuilder.Sort(s_pathComparer);
            directoriesContainingCsprojBuilder.Sort(s_pathComparer);
        }

        var newCache = new Cache(workspaceFolder, walkStartTimeUtc, newFileBasedAppsBuilder.ToImmutableAndFree(), directoriesContainingCsprojBuilder.ToImmutableAndFree());
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(cacheFilePath)!);
            using var file = File.Create(cacheFilePath);
            JsonSerializer.Serialize(file, newCache, CacheSerializerContext.Default.Cache);
        }
        catch (Exception ex) when (FatalError.ReportAndCatch(ex))
        {
        }
    }

    /// <summary>Check if discovery should consider this a file-based app.</summary>
    private static bool IsFileBasedApp(string fullPath)
    {
        using var fileStream = File.OpenRead(fullPath);
        var isFileBasedApp = VirtualProjectXmlProvider.HasFileBasedAppDirectives(SourceText.From(fileStream));
        return isFileBasedApp;
    }

    private class IncrementalEntryPointEnumerator : FileSystemEnumerator<string>
    {
        private readonly Cache _cache;
        private readonly ArrayBuilder<string> _directoriesContainingCsprojBuilder;

        /// <summary>
        /// Directories under the workspace folder which have a newer create/modify timestamp than the last walk time, and their subdirectories, as they are encountered.
        /// In this case, items may have been moved into the directory since the last walk.
        /// Therefore we need to crack '.cs' files under these directories, even if the '.cs' files themselves are older than the last walk.
        /// </summary>
        private readonly HashSet<string> _newerDirectories = new HashSet<string>(s_pathComparer);

        public IncrementalEntryPointEnumerator(Cache cache, ArrayBuilder<string> directoriesContainingCsprojBuilder)
            : base(cache.WorkspacePath, options: new EnumerationOptions { RecurseSubdirectories = true })
        {
            _cache = cache;
            _directoriesContainingCsprojBuilder = directoriesContainingCsprojBuilder;

            // Note: a creation time can be newer than the last write time when a file is copied or moved.
            var workspaceDirectoryInfo = new DirectoryInfo(_cache.WorkspacePath);
            if (workspaceDirectoryInfo.CreationTimeUtc >= cache.LastWalkTimeUtc
                || workspaceDirectoryInfo.LastWriteTimeUtc >= cache.LastWalkTimeUtc)
            {
                _newerDirectories.Add(workspaceDirectoryInfo.FullName);
            }
        }

        protected override string TransformEntry(ref FileSystemEntry entry)
            => entry.ToFullPath();

        private bool IsCacheUpToDate(ref FileSystemEntry entry)
        {
            if (_newerDirectories.GetAlternateLookup<ReadOnlySpan<char>>().Contains(entry.Directory))
                return false;

            if (entry.CreationTimeUtc >= _cache.LastWalkTimeUtc
                || entry.LastWriteTimeUtc >= _cache.LastWalkTimeUtc)
            {
                return false;
            }

            // NTFS shenanigans.
            if (entry.IsDirectory)
            {
                var directoryInfo = new DirectoryInfo(entry.ToFullPath());
                if (directoryInfo.CreationTimeUtc >= _cache.LastWalkTimeUtc
                    || directoryInfo.LastWriteTimeUtc >= _cache.LastWalkTimeUtc)
                {
                    return false;
                }
            }

            return true;
        }

        protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
        {
            if (entry.IsDirectory || !Path.GetExtension(entry.FileName).Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                // Cheap check indicates this is not a file-based app.
                return false;
            }

            if (IsCacheUpToDate(ref entry))
            {
                // Already up to date. If it is an FBA, it was visited by the initial cache loop.
                return false;
            }

            var fullPath = entry.ToFullPath();
            if (_cache.FileBasedAppFullPaths.BinarySearch(fullPath, s_pathComparer) >= 0)
            {
                // File has changed since our last walk, but it's under a cached file-based app path.
                // The initial cache loop already handled it.
                return false;
            }

            return IsFileBasedApp(fullPath);
        }

        protected override bool ShouldRecurseIntoEntry(ref FileSystemEntry entry)
        {
            foreach (var ignored in s_ignoredDirectories)
            {
                if (entry.FileName.Equals(ignored, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            var fullPath = entry.ToFullPath();
            if (IsCacheUpToDate(ref entry))
            {
                if (_cache.DirectoriesContainingCsproj.BinarySearch(fullPath, s_pathComparer) >= 0)
                {
                    // Still contains a csproj. Do not recurse.
                    _directoriesContainingCsprojBuilder.Add(fullPath);
                    return false;
                }

                return true;
            }

            // Directory contents changed since last walk.
            // Check again if it contains a csproj file.
            var containsCsproj = Directory.EnumerateFiles(fullPath, "*.csproj").Any();
            if (containsCsproj)
            {
                _directoriesContainingCsprojBuilder.Add(fullPath);
                return false;
            }

            // Changed since last walk, and doesn't contain a csproj file.
            // User may have moved new folders or files into this directory since last walk.
            _newerDirectories.Add(fullPath);
            return true;
        }
    }

    internal record Cache(string WorkspacePath, DateTimeOffset LastWalkTimeUtc, ImmutableArray<string> FileBasedAppFullPaths, ImmutableArray<string> DirectoriesContainingCsproj)
    {
        public ImmutableArray<string> FileBasedAppFullPaths { get; init; } = FileBasedAppFullPaths;
        public ImmutableArray<string> DirectoriesContainingCsproj { get; init; } = DirectoriesContainingCsproj;
    }

    [JsonSerializable(typeof(Cache))]
    internal partial class CacheSerializerContext : JsonSerializerContext;
}
