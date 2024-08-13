// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ProjectSystem;

internal sealed class FileWatchedPortableExecutableReferenceFactory(IFileChangeWatcher fileChangeWatcher)
    : AbstractFileWatchedReferenceFactory<PortableExecutableReference>(fileChangeWatcher)
{
    protected override ImmutableArray<WatchedDirectory> GetAdditionalWatchedDirectories()
    {
        using var _ = PooledHashSet<string>.GetInstance(out var referenceDirectories);

        // On each platform, there is a place that reference assemblies for the framework are installed. These are rarely going to be changed
        // but are the most common places that we're going to create file watches. Rather than either creating a huge number of file watchers
        // for every single file, or eventually realizing we should just watch these directories, we just create the single directory watchers now.
        // We'll collect this from two places: constructing it from known environment variables, and also for the defaults where those environment
        // variables would usually point, as a fallback.

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

        // Also watch the NuGet restore path; we don't do this (yet) on Windows due to potential concerns about whether
        // this creates additional overhead responding to changes during a restore.
        // TODO: remove this condition
        if (!PlatformInformation.IsWindows)
        {
            referenceDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages"));
        }

        return referenceDirectories.SelectAsArray(static d => new WatchedDirectory(d, ".dll"));
    }
}
