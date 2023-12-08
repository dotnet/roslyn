// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    internal class FileSystemCompletionHelper
    {
        private static readonly char[] s_windowsDirectorySeparator = ['\\'];

        private readonly Glyph _folderGlyph;
        private readonly Glyph _fileGlyph;

        // absolute paths
        private readonly ImmutableArray<string> _searchPaths;
        private readonly string? _baseDirectory;

        private readonly ImmutableArray<string> _allowableExtensions;
        private readonly CompletionItemRules _itemRules;

        public FileSystemCompletionHelper(
            Glyph folderGlyph,
            Glyph fileGlyph,
            ImmutableArray<string> searchPaths,
            string? baseDirectory,
            ImmutableArray<string> allowableExtensions,
            CompletionItemRules itemRules)
        {
            Debug.Assert(searchPaths.All(PathUtilities.IsAbsolute));
            Debug.Assert(baseDirectory == null || PathUtilities.IsAbsolute(baseDirectory));

            _searchPaths = searchPaths;
            _baseDirectory = baseDirectory;
            _allowableExtensions = allowableExtensions;
            _folderGlyph = folderGlyph;
            _fileGlyph = fileGlyph;
            _itemRules = itemRules;
        }

        // virtual for testing
        protected virtual string[] GetLogicalDrives()
            => IOUtilities.PerformIO(Directory.GetLogicalDrives, Array.Empty<string>());

        // virtual for testing
        protected virtual bool DirectoryExists(string fullPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));
            return Directory.Exists(fullPath);
        }

        // virtual for testing
        protected virtual IEnumerable<string> EnumerateDirectories(string fullDirectoryPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullDirectoryPath));
            return IOUtilities.PerformIO(() => Directory.EnumerateDirectories(fullDirectoryPath), Array.Empty<string>());
        }

        // virtual for testing
        protected virtual IEnumerable<string> EnumerateFiles(string fullDirectoryPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullDirectoryPath));
            return IOUtilities.PerformIO(() => Directory.EnumerateFiles(fullDirectoryPath), Array.Empty<string>());
        }

        // virtual for testing
        protected virtual bool IsVisibleFileSystemEntry(string fullPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));
            return IOUtilities.PerformIO(() => (File.GetAttributes(fullPath) & (FileAttributes.Hidden | FileAttributes.System)) == 0, false);
        }

        private CompletionItem CreateNetworkRoot()
            => CommonCompletionItem.Create(
                "\\\\",
                displayTextSuffix: "",
                glyph: null,
                description: "\\\\".ToSymbolDisplayParts(),
                rules: _itemRules);

        private CompletionItem CreateUnixRoot()
            => CommonCompletionItem.Create(
                "/",
                displayTextSuffix: "",
                glyph: _folderGlyph,
                description: "/".ToSymbolDisplayParts(),
                rules: _itemRules);

        private CompletionItem CreateFileSystemEntryItem(string fullPath, bool isDirectory)
            => CommonCompletionItem.Create(
                PathUtilities.GetFileName(fullPath),
                displayTextSuffix: "",
                glyph: isDirectory ? _folderGlyph : _fileGlyph,
                description: fullPath.ToSymbolDisplayParts(),
                rules: _itemRules);

        private CompletionItem CreateLogicalDriveItem(string drive)
            => CommonCompletionItem.Create(
                drive,
                displayTextSuffix: "",
                glyph: _folderGlyph,
                description: drive.ToSymbolDisplayParts(),
                rules: _itemRules);

        public Task<ImmutableArray<CompletionItem>> GetItemsAsync(string directoryPath, CancellationToken cancellationToken)
            => Task.Run(() => GetItems(directoryPath, cancellationToken), cancellationToken);

        private ImmutableArray<CompletionItem> GetItems(string directoryPath, CancellationToken cancellationToken)
        {
            if (!PathUtilities.IsUnixLikePlatform && directoryPath == "\\")
            {
                // The user has typed only "\".  In this case, we want to add "\\" to the list.  
                return ImmutableArray.Create(CreateNetworkRoot());
            }

            var result = ArrayBuilder<CompletionItem>.GetInstance();

            var pathKind = PathUtilities.GetPathKind(directoryPath);
            switch (pathKind)
            {
                case PathKind.Empty:
                    // base directory
                    if (_baseDirectory != null)
                    {
                        result.AddRange(GetItemsInDirectory(_baseDirectory, cancellationToken));
                    }

                    // roots
                    if (PathUtilities.IsUnixLikePlatform)
                    {
                        result.AddRange(CreateUnixRoot());
                    }
                    else
                    {
                        foreach (var drive in GetLogicalDrives())
                        {
                            result.Add(CreateLogicalDriveItem(drive.TrimEnd(s_windowsDirectorySeparator)));
                        }

                        result.Add(CreateNetworkRoot());
                    }

                    // entries on search paths
                    foreach (var searchPath in _searchPaths)
                    {
                        result.AddRange(GetItemsInDirectory(searchPath, cancellationToken));
                    }

                    break;

                case PathKind.Absolute:
                case PathKind.RelativeToCurrentDirectory:
                case PathKind.RelativeToCurrentParent:
                case PathKind.RelativeToCurrentRoot:
                    var fullDirectoryPath = FileUtilities.ResolveRelativePath(directoryPath, basePath: null, baseDirectory: _baseDirectory);
                    if (fullDirectoryPath != null)
                    {
                        result.AddRange(GetItemsInDirectory(fullDirectoryPath, cancellationToken));
                    }
                    else
                    {
                        // invalid path
                        result.Clear();
                    }

                    break;

                case PathKind.Relative:

                    // base directory:
                    if (_baseDirectory != null)
                    {
                        result.AddRange(GetItemsInDirectory(PathUtilities.CombineAbsoluteAndRelativePaths(_baseDirectory, directoryPath)!, cancellationToken));
                    }

                    // search paths:
                    foreach (var searchPath in _searchPaths)
                    {
                        result.AddRange(GetItemsInDirectory(PathUtilities.CombineAbsoluteAndRelativePaths(searchPath, directoryPath)!, cancellationToken));
                    }

                    break;

                case PathKind.RelativeToDriveDirectory:
                    // Paths "C:dir" are not supported, but when the path doesn't include any directory, i.e. "C:",
                    // we return the drive itself.
                    if (directoryPath.Length == 2)
                    {
                        result.Add(CreateLogicalDriveItem(directoryPath));
                    }

                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(pathKind);
            }

            return result.ToImmutableAndFree();
        }

        private IEnumerable<CompletionItem> GetItemsInDirectory(string fullDirectoryPath, CancellationToken cancellationToken)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullDirectoryPath));

            cancellationToken.ThrowIfCancellationRequested();

            if (!DirectoryExists(fullDirectoryPath))
            {
                yield break;
            }

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var directory in EnumerateDirectories(fullDirectoryPath))
            {
                if (IsVisibleFileSystemEntry(directory))
                {
                    yield return CreateFileSystemEntryItem(directory, isDirectory: true);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var file in EnumerateFiles(fullDirectoryPath))
            {
                if (_allowableExtensions.Length != 0 &&
                    !_allowableExtensions.Contains(
                        PathUtilities.GetExtension(file),
                        PathUtilities.IsUnixLikePlatform ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (IsVisibleFileSystemEntry(file))
                {
                    yield return CreateFileSystemEntryItem(file, isDirectory: false);
                }
            }
        }

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor(FileSystemCompletionHelper fileSystemCompletionHelper)
        {
            private readonly FileSystemCompletionHelper _fileSystemCompletionHelper = fileSystemCompletionHelper;

            internal ImmutableArray<CompletionItem> GetItems(string directoryPath, CancellationToken cancellationToken)
                => _fileSystemCompletionHelper.GetItems(directoryPath, cancellationToken);
        }
    }
}
