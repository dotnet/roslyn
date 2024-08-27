﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ProjectSystem;

internal sealed class FileWatchedReferenceFactory<TReference>
    where TReference : class
{
    private readonly object _gate = new();

    /// <summary>
    /// A file change context used to watch metadata references. This is lazy to avoid creating this immediately during
    /// our LSP process startup, when we don't yet know the LSP client's capabilities.
    /// </summary>
    private readonly Lazy<IFileChangeContext> _fileReferenceChangeContext;

    /// <summary>
    /// File watching tokens from <see cref="_fileReferenceChangeContext"/> that are watching metadata references. These
    /// are only created once we are actually applying a batch because we don't determine until the batch is applied if
    /// the file reference will actually be a file reference or it'll be a converted project reference.
    /// </summary>
    private readonly Dictionary<string, (IWatchedFile Token, int RefCount)> _referenceFileWatchingTokens = [];

    /// <summary>
    /// Stores the caller for a previous disposal of a reference produced by this class, to track down a double-dispose
    /// issue.
    /// </summary>
    /// <remarks>
    /// This can be removed once https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1843611 is fixed.
    /// </remarks>
    private readonly ConditionalWeakTable<TReference, string> _previousDisposalLocations = new();

    private readonly AsyncBatchingWorkQueue<string> _workQueue;

    private readonly Func<string, CancellationToken, Task> _callback;

    public FileWatchedReferenceFactory(
        IFileChangeWatcher fileChangeWatcher,
        IAsynchronousOperationListener asyncListener,
        Func<string, CancellationToken, Task> callback,
        CancellationToken cancellationToken)
    {
        _callback = callback;
        _workQueue = new AsyncBatchingWorkQueue<string>(
            TimeSpan.FromSeconds(5),
            ProcessWorkAsync,
            // Dedupe notifications for the same file path
            EqualityComparer<string>.Default,
            asyncListener,
            cancellationToken);

        _fileReferenceChangeContext = new Lazy<IFileChangeContext>(() =>
        {
            var fileReferenceChangeContext = fileChangeWatcher.CreateContext(GetAdditionalWatchedDirectories());
            fileReferenceChangeContext.FileChanged += (s, e) => _workQueue.AddWork(e);
            return fileReferenceChangeContext;
        });

        static ImmutableArray<WatchedDirectory> GetAdditionalWatchedDirectories()
        {
            using var _ = PooledHashSet<string>.GetInstance(out var referenceDirectories);

            // On each platform, there is a place that reference assemblies for the framework are installed. These are
            // rarely going to be changed but are the most common places that we're going to create file watches. Rather
            // than either creating a huge number of file watchers for every single file, or eventually realizing we
            // should just watch these directories, we just create the single directory watchers now. We'll collect this
            // from two places: constructing it from known environment variables, and also for the defaults where those
            // environment variables would usually point, as a fallback.

            if (Environment.GetEnvironmentVariable("DOTNET_ROOT") is string dotnetRoot && !string.IsNullOrEmpty(dotnetRoot))
            {
                referenceDirectories.Add(Path.Combine(dotnetRoot, "packs"));
            }

            if (PlatformInformation.IsWindows)
            {
                referenceDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Reference Assemblies", "Microsoft", "Framework"));
                referenceDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "packs"));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                referenceDirectories.Add("/usr/lib/dotnet/packs");
                referenceDirectories.Add("/usr/share/dotnet/packs");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                referenceDirectories.Add("/usr/local/share/dotnet/packs");
            }

            // Also watch the NuGet restore path; we don't do this (yet) on Windows due to potential concerns about
            // whether this creates additional overhead responding to changes during a restore. TODO: remove this
            // condition
            if (!PlatformInformation.IsWindows)
            {
                referenceDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages"));
            }

            return referenceDirectories.SelectAsArray(static d => new WatchedDirectory(d, ".dll"));
        }
    }

    /// <summary>
    /// Starts watching a particular <typeparamref name="TReference"/> for changes to the file. If this is already being
    /// watched , the reference count will be incremented. This is *not* safe to attempt to call multiple times for the
    /// same project and reference (e.g. in applying workspace updates)
    /// </summary>
    public void StartWatchingReference(string fullFilePath)
    {
        lock (_gate)
        {
            var (token, count) = _referenceFileWatchingTokens.GetOrAdd(fullFilePath, _ =>
            {
                var fileToken = _fileReferenceChangeContext.Value.EnqueueWatchingFile(fullFilePath);
                return (fileToken, RefCount: 0);
            });

            _referenceFileWatchingTokens[fullFilePath] = (token, RefCount: count + 1);
        }
    }

    /// <summary>
    /// Decrements the reference count for the given <typeparamref name="TReference"/>. When the reference count reaches
    /// 0, the file watcher will be stopped. This is *not* safe to attempt to call multiple times for the same project
    /// and reference (e.g. in applying workspace updates)
    /// </summary>
    public void StopWatchingReference(string fullFilePath, TReference? referenceToTrack, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
    {
        lock (_gate)
        {
            var disposalLocation = callerFilePath + ", line " + callerLineNumber;
            if (!_referenceFileWatchingTokens.TryGetValue(fullFilePath, out var watchedFileReference))
            {
                if (referenceToTrack != null)
                {
                    // We're attempting to stop watching a file that we never started watching. This is a bug.
                    var existingDisposalStackTrace = _previousDisposalLocations.TryGetValue(referenceToTrack, out var previousDisposalLocation);
                    throw new ArgumentException("The reference was already disposed at " + previousDisposalLocation);
                }
                else
                {
                    throw new ArgumentException("Attempting to stop watching a file that we never started watching. This is a bug.");
                }
            }

            var newRefCount = watchedFileReference.RefCount - 1;
            Contract.ThrowIfFalse(newRefCount >= 0, "Ref count cannot be negative");
            if (newRefCount == 0)
            {
                // No one else is watching this file, so stop watching it and remove from our map.
                watchedFileReference.Token.Dispose();
                _referenceFileWatchingTokens.Remove(fullFilePath);

                if (referenceToTrack != null)
                {
                    _previousDisposalLocations.Remove(referenceToTrack);
                    _previousDisposalLocations.Add(referenceToTrack, disposalLocation);
                }
            }
            else
            {
                _referenceFileWatchingTokens[fullFilePath] = (watchedFileReference.Token, newRefCount);
            }

            // Note we still potentially have an outstanding change that we haven't raised a notification for due to the
            // delay we use. We could cancel the notification for that file path, but we may still have another
            // outstanding PortableExecutableReference that isn't this one that does want that notification. We're OK
            // just leaving the delay still running for two reasons:
            //
            // 1. Technically, we did see a file change before the call to StopWatchingReference, so arguably we should
            //    still raise it.
            // 2. Since we raise the notification for a file path, it's up to the consumer of this to still track down
            //    which actual reference needs to be changed. That'll automatically handle any race where the event
            //    comes late, which is a scenario this must always deal with no matter what -- another thread might
            //    already be gearing up to notify the caller of this reference and we can't stop it.
        }
    }

    private async ValueTask ProcessWorkAsync(ImmutableSegmentedList<string> list, CancellationToken cancellationToken)
    {
        foreach (var filePath in list)
            await _callback(filePath, cancellationToken).ConfigureAwait(false);
    }
}
