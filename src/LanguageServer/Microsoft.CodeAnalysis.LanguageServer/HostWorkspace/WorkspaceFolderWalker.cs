// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Immutable;
using System.IO.Enumeration;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

/// <summary>
/// Shared directory walker for workspace folder discovery.
/// Provides the common traversal primitives used by both project discovery and
/// file-based-app entry-point discovery:
/// <list type="bullet">
///   <item><see cref="IgnoredDirectoryNames"/> / <see cref="ShouldIgnoreDirectory"/> — shared ignore rules.</item>
///   <item><see cref="CsFileKind"/> / <see cref="CsFileInfo"/> / <see cref="DirectoryEnumerator"/> — shared file-system primitives.</item>
///   <item><see cref="Walk"/> — uncached walk that reports .csproj-containing directories and, optionally, .cs files.</item>
/// </list>
/// </summary>
internal static class WorkspaceFolderWalker
{
    /// <summary>
    /// Well-known output and tool directories that are always excluded from discovery.
    /// Directories whose name begins with <c>'.'</c> (e.g. <c>.git</c>, <c>.vs</c>) are also excluded.
    /// </summary>
    internal static readonly SearchValues<string> IgnoredDirectoryNames = SearchValues.Create([
        "artifacts",
        "bin",
        "obj",
        "node_modules"
    ], StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns <see langword="true"/> when the directory should be excluded from discovery.</summary>
    /// <param name="directoryName">The final component of the directory path (not a full path).</param>
    internal static bool ShouldIgnoreDirectory(ReadOnlySpan<char> directoryName)
        => directoryName.StartsWith('.') || directoryName.ContainsAny(IgnoredDirectoryNames);

    /// <summary>Returns the later of two <see cref="DateTimeOffset"/> values.</summary>
    internal static DateTimeOffset Max(DateTimeOffset lhs, DateTimeOffset rhs)
        => lhs < rhs ? rhs : lhs;

    /// <summary>
    /// Walks the tree rooted at <paramref name="workspaceFolder"/>.
    /// <para>
    /// For each directory found to contain at least one .csproj file, <paramref name="onCsprojDirectory"/>
    /// is invoked with the directory path and a sorted list of the .csproj paths it contains.
    /// The walk does <em>not</em> descend into those directories.
    /// </para>
    /// <para>
    /// For directories that do not contain a .csproj, <paramref name="onCsFile"/> (if provided) is invoked
    /// for each .cs file, passing the file path and the maximum of its creation and last-write timestamps.
    /// </para>
    /// </summary>
    internal static void Walk(
        string workspaceFolder,
        Action<string, ImmutableArray<string>> onCsprojDirectory,
        Action<string, DateTimeOffset>? onCsFile = null)
    {
        WalkDirectory(workspaceFolder);
        return;

        void WalkDirectory(string directory)
        {
            var name = Path.GetFileName(directory.AsSpan());
            if (ShouldIgnoreDirectory(name))
                return;

            ImmutableArray<string>.Builder? csprojPaths = null;
            using var buffer = TemporaryArray<CsFileInfo>.Empty;

            using var enumerator = new DirectoryEnumerator(directory);
            while (enumerator.MoveNext())
            {
                var item = enumerator.Current;
                if (item.Kind == CsFileKind.Csproj)
                {
                    csprojPaths ??= ImmutableArray.CreateBuilder<string>();
                    csprojPaths.Add(item.Path);
                }
                else
                {
                    buffer.Add(item);
                }
            }

            if (csprojPaths is not null)
            {
                // Stable order for deterministic results across runs.
                csprojPaths.Sort();
                onCsprojDirectory(directory, csprojPaths.ToImmutable());
                return; // do not descend into this subtree
            }

            // No .csproj in this directory — report .cs files and recurse.
            foreach (var item in buffer)
            {
                if (item.Kind == CsFileKind.Directory)
                    WalkDirectory(item.Path);
                else if (item.Kind == CsFileKind.Cs)
                    onCsFile?.Invoke(item.Path, item.CreatedOrModifiedTimeUtc);
            }
        }
    }

    internal enum CsFileKind
    {
        /// <summary>A file that is not relevant for discovery. Not returned by <see cref="DirectoryEnumerator"/>.</summary>
        None,
        Directory,
        Cs,
        Csproj,
    }

    internal readonly struct CsFileInfo(CsFileKind kind, string path, DateTimeOffset createdOrModifiedTimeUtc)
    {
        public CsFileKind Kind { get; } = kind;
        public string Path { get; } = path;
        public DateTimeOffset CreatedOrModifiedTimeUtc { get; } = createdOrModifiedTimeUtc;
    }

    /// <summary>
    /// A single-level <see cref="FileSystemEnumerator{TResult}"/> that yields directories, .cs files,
    /// and .csproj files inside a given directory, without recursing automatically.
    /// </summary>
    internal sealed class DirectoryEnumerator(string directory) : FileSystemEnumerator<CsFileInfo>(directory, new EnumerationOptions { RecurseSubdirectories = false, IgnoreInaccessible = true })
    {
        private static CsFileKind GetKind(ref FileSystemEntry entry)
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
            => GetKind(ref entry) != CsFileKind.None;

        protected override bool ShouldRecurseIntoEntry(ref FileSystemEntry entry)
            => throw ExceptionUtilities.Unreachable();
    }
}
