// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.IO.Enumeration;
using System.Runtime.CompilerServices;
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
using Microsoft.CodeAnalysis.Shared.TestHooks;
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
internal sealed class FileBasedProgramsEntryPointDiscoveryFactory(IGlobalOptionService globalOptionService, IAsynchronousOperationListenerProvider listenerProvider, ILoggerFactory loggerFactory) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        return new FileBasedProgramsEntryPointDiscovery(globalOptionService, listenerProvider.GetListener(FeatureAttribute.Workspace), loggerFactory, lspServices);
    }
}

internal sealed partial class FileBasedProgramsEntryPointDiscovery(
    IGlobalOptionService globalOptionService, IAsynchronousOperationListener listener, ILoggerFactory loggerFactory, LspServices lspServices) : ILspService, IOnInitialized
{
    private static readonly StringComparer s_pathComparer = StringComparer.OrdinalIgnoreCase;

    /// <summary>Directories which are ignored per convention.</summary>
    /// <remarks>Some conventional directories like '.git' and '.vs' are expected to be marked hidden and will be automatically ignored by discovery.</remarks>
    private static readonly SearchValues<string> s_ignoredDirectories = SearchValues.Create([
        "artifacts",
        "bin",
        "obj",
        "node_modules"
    ], StringComparison.OrdinalIgnoreCase);

    private readonly ILogger _logger = loggerFactory.CreateLogger<FileBasedProgramsEntryPointDiscovery>();
    private ImmutableArray<string> _workspaceFolders;

    public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
    {
        var initializeManager = context.GetRequiredService<IInitializeManager>();
        _workspaceFolders = initializeManager.GetRequiredWorkspaceFolderPaths();
        Task.Run(async () =>
        {
            try
            {
                using var token = listener.BeginAsyncOperation(nameof(FindAndLoadEntryPointsAsync));
                await FindAndLoadEntryPointsAsync();
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    internal async Task FindAndLoadEntryPointsAsync()
    {
        Contract.ThrowIfTrue(_workspaceFolders.IsDefault, $"{nameof(OnInitializedAsync)} must be called before {nameof(FindAndLoadEntryPointsAsync)}.");

        if (_workspaceFolders.IsEmpty)
        {
            _logger.LogTrace("No workspace folders to search for file-based apps.");
            return;
        }

        if (!globalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms))
        {
            _logger.LogTrace(@"""dotnet.projects.enableFileBasedPrograms"" is false. Not discovering entry points.");
            return;
        }

        if (!globalOptionService.GetOption(FileBasedAppsOptionsStorage.EnableAutomaticDiscovery))
        {
            _logger.LogTrace(@"""dotnet.fileBasedApps.enableAutomaticDiscovery"" is false. Not discovering entry points.");
            return;
        }

        var fileBasedProgramsProjectSystem = (FileBasedProgramsProjectSystem?)lspServices.GetService<ILspMiscellaneousFilesWorkspaceProvider>();
        Contract.ThrowIfNull(fileBasedProgramsProjectSystem);

        // Note: the overwhelmingly common case is when there is just one workspace folder.
        // For simplicity we orient our search around one workspace folder at a time.
        foreach (var workspaceFolder in _workspaceFolders)
        {
            foreach (var fileBasedAppPath in FindEntryPoints(workspaceFolder))
            {
                await fileBasedProgramsProjectSystem.TryBeginLoadingFileBasedAppAsync(fileBasedAppPath);
            }
        }

        // Discovery pass done. Find and delete old caches.
        IOUtilities.PerformIO(() =>
        {
            using var enumerator = new OldCacheEnumerator();
            while (enumerator.MoveNext())
            {
                IOUtilities.PerformIO(() => Directory.Delete(enumerator.Current, recursive: true));
            }
        });
    }

    private sealed class OldCacheEnumerator() : FileSystemEnumerator<string>(
        directory: VirtualProjectXmlProvider.GetDiscoveryCacheRootDirectory(),
        options: new() { RecurseSubdirectories = false })
    {
        // Yield cache directories that have not been modified in 30 days (indicates they are stale and should be deleted)
        private readonly DateTimeOffset _includeCachesEarlierThanUtc = DateTimeOffset.UtcNow - TimeSpan.FromDays(30);

        protected override string TransformEntry(ref FileSystemEntry entry) => entry.ToFullPath();

        protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
        {
            return entry.IsDirectory && entry.LastWriteTimeUtc < _includeCachesEarlierThanUtc;
        }
    }

    internal ImmutableArray<string> FindEntryPoints(string workspaceFolder)
    {
        var stopwatch = SharedStopwatch.StartNew();
        var cacheDirectory = VirtualProjectXmlProvider.GetDiscoveryCacheDirectory(workspaceFolder);
        var cacheFilePath = Path.Join(cacheDirectory, "cache.json");
        Cache? cache = null;
        try
        {
            if (File.Exists(cacheFilePath))
            {
                using var cacheFile = File.OpenRead(cacheFilePath);
                cache = JsonSerializer.Deserialize(cacheFile, CacheSerializerContext.Default.Cache);
            }

            // Drop malformed caches
            if (cache != null
                && (!cache.WorkspacePath.Equals(workspaceFolder, StringComparison.OrdinalIgnoreCase)
                    || cache.FileBasedAppFullPaths.IsDefault
                    || cache.DirectoriesContainingCsproj.IsDefault))
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
        // We assume that file writes in the workspace folder which occur after this write, will have equal or later timestamps.
        // Timestamps we encounter which compare equal to the walkStartTimeUtc timestamp, must be treated as possibly being newer than the walkStartTimeUtc timestamp.
        var walkStartTimeUtc = IOUtilities.PerformIO(() =>
        {
            var sentinelPath = Path.Join(cacheDirectory, $".walk-timestamp-{Guid.NewGuid()}");
            File.WriteAllBytes(sentinelPath, []);
            var lastWriteTime = File.GetLastWriteTimeUtc(sentinelPath);
            File.Delete(sentinelPath);
            return lastWriteTime;
        }, defaultValue: cache.LastWalkTimeUtc);

        var newFileBasedAppsBuilder = ArrayBuilder<string>.GetInstance(cache.FileBasedAppFullPaths.Length);
        var directoriesContainingCsprojBuilder = ArrayBuilder<string>.GetInstance(cache.DirectoriesContainingCsproj.Length);
        var visitor = new WorkspaceFolderVisitor(cache, newFileBasedAppsBuilder, directoriesContainingCsprojBuilder, _logger);
        visitor.Visit();
        var elapsedMilliseconds = Math.Round(stopwatch.Elapsed.TotalMilliseconds);
        _logger.LogInformation("Finished discovery in '{workspaceFolder}' in {elapsedMilliseconds} milliseconds", workspaceFolder, elapsedMilliseconds);

        // Ensure items go into the cache file in a stable order.
        // This is useful for manual inspection and allows use of 'BinarySearch' to match directories against the cache.
        newFileBasedAppsBuilder.Sort();
        directoriesContainingCsprojBuilder.Sort();
        var newCache = new Cache(workspaceFolder, walkStartTimeUtc, newFileBasedAppsBuilder.ToImmutableAndFree(), directoriesContainingCsprojBuilder.ToImmutableAndFree());
        try
        {
            Directory.CreateDirectory(cacheDirectory);
            var cacheStagingFilePath = Path.Join(cacheDirectory, "cache.staging.json");
            using (var stagingFile = File.Create(cacheStagingFilePath))
            {
                JsonSerializer.Serialize(stagingFile, newCache, CacheSerializerContext.Default.Cache);
            }
            File.Replace(cacheStagingFilePath, cacheFilePath, destinationBackupFileName: null);
        }
        catch (Exception ex) when (FatalError.ReportAndCatch(ex))
        {
        }

        return newCache.FileBasedAppFullPaths;
    }

    /// <summary>Check if discovery should consider this a file-based app.</summary>
    private static bool IsFileBasedApp(string fullPath)
    {
        using var fileStream = File.OpenRead(fullPath);
        var toRead = (int)Math.Min(5, fileStream.Length);
        InlineArray5<byte> bytes = default;
        Span<byte> bytesSpan = bytes;
        fileStream.ReadExactly(bytesSpan[..toRead]);

        // Discovery only considers a file to be file-based app, if it starts with either "#!", or UTF-8 BOM followed by "#!".
        return bytesSpan is [(byte)'#', (byte)'!', ..] or [0xEF, 0xBB, 0xBF, (byte)'#', (byte)'!'];
    }

    private enum CsFileKind
    {
        None, // Denotes a file that is irrelevant for discovery. Shouldn't appear on a valid 'CsFileInfo' instance.
        Directory,
        Cs,
        Csproj,
    }

    private readonly struct CsFileInfo(CsFileKind kind, string path, DateTimeOffset createdOrModifiedTimeUtc)
    {
        public CsFileKind Kind { get; } = kind;
        public string Path { get; } = path;
        public DateTimeOffset CreatedOrModifiedTimeUtc { get; } = createdOrModifiedTimeUtc;
    }

    private class DirectoryEnumerator(string directory) : FileSystemEnumerator<CsFileInfo>(directory)
    {
        private CsFileKind GetKind(ref FileSystemEntry entry)
        {
            if (entry.IsDirectory)
                return CsFileKind.Directory;

            var extension = Path.GetExtension(entry.FileName);
            if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
                return CsFileKind.Cs;

            if (extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
                return CsFileKind.Csproj;

            return CsFileKind.None;
        }

        protected override CsFileInfo TransformEntry(ref FileSystemEntry entry)
        {
            var kind = GetKind(ref entry);
            Contract.ThrowIfTrue(kind == CsFileKind.None);
            return new CsFileInfo(kind, entry.ToFullPath(), Max(entry.CreationTimeUtc, entry.LastWriteTimeUtc));
        }

        protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
        {
            return GetKind(ref entry) != CsFileKind.None;
        }

        protected override bool ShouldRecurseIntoEntry(ref FileSystemEntry entry)
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    private class WorkspaceFolderVisitor(Cache cache, ArrayBuilder<string> entryPointsBuilder, ArrayBuilder<string> directoriesContainingCsprojBuilder, ILogger logger)
    {
        internal void Visit()
            // Note: passing `DateTimeOffset.MinValue` here will force `VisitDirectory` to stat the directory again to get its created/modified times out.
            => VisitDirectory(cache.WorkspacePath, DateTimeOffset.MinValue);

        private void VisitDirectory(string directory, DateTimeOffset createdOrModifiedTimeUtc)
        {
            if (Path.GetFileName(directory.AsSpan()).ContainsAny(s_ignoredDirectories))
                return;

            if (createdOrModifiedTimeUtc < cache.LastWalkTimeUtc)
            {
                // On NTFS, the directory timestamps we observe when enumerating can be stale when files are added/deleted from a directory.
                // If we find the timestamps were old enough (i.e. we entered this block),
                // we still need to `new DirectoryInfo()` again and force the timestamps to update if needed.
                var directoryInfo = new DirectoryInfo(directory);
                var newCreatedOrModifiedTimeUtc = Max(directoryInfo.CreationTimeUtc, directoryInfo.LastWriteTimeUtc);
                if (newCreatedOrModifiedTimeUtc < cache.LastWalkTimeUtc && cache.DirectoriesContainingCsproj.BinarySearch(directory, s_pathComparer) >= 0)
                {
                    // Our info about this directory is up to date, and we know it contains a csproj, so bail out before enumerating its files.
                    directoriesContainingCsprojBuilder.Add(directory);
                    return;
                }

                createdOrModifiedTimeUtc = Max(createdOrModifiedTimeUtc, newCreatedOrModifiedTimeUtc);
            }

            using var currentDirectoryItems = TemporaryArray<CsFileInfo>.Empty;
            using var enumerator = new DirectoryEnumerator(directory);
            while (enumerator.MoveNext())
            {
                var fileInfo = enumerator.Current;
                if (fileInfo.Kind == CsFileKind.Csproj)
                {
                    // Found a csproj. Return without visiting any of the files.
                    directoriesContainingCsprojBuilder.Add(directory);
                    return;
                }

                currentDirectoryItems.Add(fileInfo);
            }

            // Did not find a csproj. Continue searching this subtree for entry points.
            foreach (var fileInfo in currentDirectoryItems)
            {
                // When a subdirectory is moved in to a parent directory between two discovery passes, the timestamps of the subdirectory's files are not updated.
                // Only the "modified" timestamp of the parent directory, and the "created" timestamp of the subdirectory, are updated.
                // This means: even if a .cs file we encounter within a "new" subdirectory has old timestamps, we don't know whether we've seen it before or not, so we need to crack it.
                if (fileInfo.Kind == CsFileKind.Directory)
                    VisitDirectory(fileInfo.Path, Max(createdOrModifiedTimeUtc, fileInfo.CreatedOrModifiedTimeUtc));
                else if (fileInfo.Kind == CsFileKind.Cs)
                    VisitCsFile(fileInfo.Path, Max(createdOrModifiedTimeUtc, fileInfo.CreatedOrModifiedTimeUtc));
                else
                    throw ExceptionUtilities.Unreachable();
            }
        }

        private void VisitCsFile(string file, DateTimeOffset createdOrModifiedTimeUtc)
        {
            if (createdOrModifiedTimeUtc < cache.LastWalkTimeUtc)
            {
                if (cache.FileBasedAppFullPaths.BinarySearch(file) >= 0)
                {
                    logger.LogInformation("Discovered file-based app (cache hit): {csFilePath}", file);
                    entryPointsBuilder.Add(file);
                }

                return;
            }

            if (IOUtilities.PerformIO(() => IsFileBasedApp(file)))
            {
                logger.LogInformation("Discovered file-based app (cache miss): {csFilePath}", file);
                entryPointsBuilder.Add(file);
            }
        }
    }

    /// <summary>Get the later of two DateTimeOffsets.</summary>
    private static DateTimeOffset Max(DateTimeOffset lhs, DateTimeOffset rhs)
        => lhs < rhs ? rhs : lhs;

    internal sealed record Cache(string WorkspacePath, DateTimeOffset LastWalkTimeUtc, ImmutableArray<string> FileBasedAppFullPaths, ImmutableArray<string> DirectoriesContainingCsproj);

    [JsonSerializable(typeof(Cache))]
    internal sealed partial class CacheSerializerContext : JsonSerializerContext;
}