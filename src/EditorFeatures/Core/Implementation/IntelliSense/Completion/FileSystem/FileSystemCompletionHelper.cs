// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.FileSystem
{
    internal sealed class FileSystemCompletionHelper
    {
        private readonly ICurrentWorkingDirectoryDiscoveryService _fileSystemDiscoveryService;
        private readonly Func<string, bool> _exclude;
        private readonly Glyph _folderGlyph;
        private readonly Glyph _fileGlyph;

        // absolute paths
        private readonly ImmutableArray<string> _searchPaths;

        private readonly ISet<string> _allowableExtensions;

        private readonly Lazy<string[]> _lazyGetDrives;
        private readonly CompletionListProvider _completionProvider;
        private readonly TextSpan _textChangeSpan;
        private readonly CompletionItemRules _itemRules;

        public FileSystemCompletionHelper(
            CompletionListProvider completionProvider,
            TextSpan textChangeSpan,
            ICurrentWorkingDirectoryDiscoveryService fileSystemDiscoveryService,
            Glyph folderGlyph,
            Glyph fileGlyph,
            ImmutableArray<string> searchPaths,
            IEnumerable<string> allowableExtensions,
            Func<string, bool> exclude = null,
            CompletionItemRules itemRules = null)
        {
            Debug.Assert(searchPaths.All(path => PathUtilities.IsAbsolute(path)));

            _completionProvider = completionProvider;
            _textChangeSpan = textChangeSpan;
            _searchPaths = searchPaths;
            _allowableExtensions = allowableExtensions.Select(e => e.ToLowerInvariant()).ToSet();
            _fileSystemDiscoveryService = fileSystemDiscoveryService;
            _folderGlyph = folderGlyph;
            _fileGlyph = fileGlyph;
            _exclude = exclude;
            _itemRules = itemRules;

            _lazyGetDrives = new Lazy<string[]>(() =>
                IOUtilities.PerformIO(Directory.GetLogicalDrives, SpecializedCollections.EmptyArray<string>()));
        }

        public ImmutableArray<CompletionItem> GetItems(string pathSoFar, string documentPath)
        {
            if (_exclude != null && _exclude(pathSoFar))
            {
                return ImmutableArray<CompletionItem>.Empty;
            }

            return GetFilesAndDirectories(pathSoFar, documentPath);
        }

        private CompletionItem CreateCurrentDirectoryItem()
        {
            return new CompletionItem(_completionProvider, ".", _textChangeSpan, rules: _itemRules);
        }

        private CompletionItem CreateParentDirectoryItem()
        {
            return new CompletionItem(_completionProvider, "..", _textChangeSpan, rules: _itemRules);
        }

        private CompletionItem CreateNetworkRoot(TextSpan textChangeSpan)
        {
            return new CompletionItem(_completionProvider, "\\\\", textChangeSpan, rules: _itemRules);
        }

        private ImmutableArray<CompletionItem> GetFilesAndDirectories(string path, string basePath)
        {
            var result = ImmutableArray.CreateBuilder<CompletionItem>();
            var pathKind = PathUtilities.GetPathKind(path);
            switch (pathKind)
            {
                case PathKind.Empty:
                    result.Add(CreateCurrentDirectoryItem());

                    if (!IsDriveRoot(_fileSystemDiscoveryService.WorkingDirectory))
                    {
                        result.Add(CreateParentDirectoryItem());
                    }

                    result.Add(CreateNetworkRoot(_textChangeSpan));
                    result.AddRange(GetLogicalDrives());
                    result.AddRange(GetFilesAndDirectoriesInSearchPaths());
                    break;

                case PathKind.Absolute:
                case PathKind.RelativeToCurrentDirectory:
                case PathKind.RelativeToCurrentParent:
                case PathKind.RelativeToCurrentRoot:
                    {
                        var fullPath = FileUtilities.ResolveRelativePath(
                            path,
                            basePath,
                            _fileSystemDiscoveryService.WorkingDirectory);

                        if (fullPath != null)
                        {
                            result.AddRange(GetFilesAndDirectoriesInDirectory(fullPath));

                            // although it is possible to type "." here, it doesn't make any sense to do so:
                            if (!IsDriveRoot(fullPath) && pathKind != PathKind.Absolute)
                            {
                                result.Add(CreateParentDirectoryItem());
                            }

                            if (path == "\\" && pathKind == PathKind.RelativeToCurrentRoot)
                            {
                                // The user has typed only "\".  In this case, we want to add "\\" to
                                // the list.  Also, the textChangeSpan needs to be backed up by one
                                // so that it will consume the "\" when "\\" is inserted.
                                result.Add(CreateNetworkRoot(TextSpan.FromBounds(_textChangeSpan.Start - 1, _textChangeSpan.End)));
                            }
                        }
                        else
                        {
                            // invalid path
                            result.Clear();
                        }
                    }

                    break;

                case PathKind.Relative:

                    // although it is possible to type "." here, it doesn't make any sense to do so:
                    result.Add(CreateParentDirectoryItem());

                    foreach (var searchPath in _searchPaths)
                    {
                        var fullPath = PathUtilities.CombineAbsoluteAndRelativePaths(searchPath, path);

                        // search paths are always absolute:
                        Debug.Assert(PathUtilities.IsAbsolute(fullPath));
                        result.AddRange(GetFilesAndDirectoriesInDirectory(fullPath));
                    }

                    break;

                case PathKind.RelativeToDriveDirectory:
                    // these paths are not supported
                    break;

                default:
                    throw ExceptionUtilities.Unreachable;
            }

            return result.AsImmutable();
        }

        private static bool IsDriveRoot(string fullPath)
        {
            return IOUtilities.PerformIO(() => new DirectoryInfo(fullPath).Parent == null);
        }

        private IEnumerable<CompletionItem> GetFilesAndDirectoriesInDirectory(string fullDirectoryPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullDirectoryPath));
            if (IOUtilities.PerformIO(() => Directory.Exists(fullDirectoryPath)))
            {
                var directoryInfo = IOUtilities.PerformIO(() => new DirectoryInfo(fullDirectoryPath));
                if (directoryInfo != null)
                {
                    return from child in GetFileSystemInfos(directoryInfo)
                           where ShouldShow(child)
                           where CanAccess(child)
                           select this.CreateCompletion(child);
                }
            }

            return SpecializedCollections.EmptyEnumerable<CompletionItem>();
        }

        private CompletionItem CreateCompletion(FileSystemInfo child)
        {
            return new CompletionItem(
                _completionProvider,
                child.Name,
                _textChangeSpan,
                glyph: child is DirectoryInfo ? _folderGlyph : _fileGlyph,
                description: child.FullName.ToSymbolDisplayParts(),
                rules: _itemRules);
        }

        private bool ShouldShow(FileSystemInfo child)
        {
            // Get the attributes.  If we can't, assume it's hidden.
            var attributes = IOUtilities.PerformIO(() => child.Attributes, FileAttributes.Hidden);

            // Don't show hidden/system files.
            if ((attributes & FileAttributes.Hidden) != 0 ||
                (attributes & FileAttributes.System) != 0)
            {
                return false;
            }

            if (child is DirectoryInfo)
            {
                return true;
            }

            if (child is FileInfo)
            {
                return
                    _allowableExtensions.Count == 0 ||
                    _allowableExtensions.Contains(Path.GetExtension(child.Name).ToLowerInvariant());
            }

            return false;
        }

        private bool CanAccess(FileSystemInfo info)
        {
            return info.TypeSwitch(
                (DirectoryInfo d) => CanAccessDirectory(d),
                (FileInfo f) => CanAccessFile(f));
        }

        private bool CanAccessFile(FileInfo file)
        {
            var accessControl = IOUtilities.PerformIO(file.GetAccessControl);

            // Quick and dirty check.  If we can't even get the access control object, then we
            // can't access the file.
            if (accessControl == null)
            {
                return false;
            }

            // TODO(cyrusn): Actually add checks here.
            return true;
        }

        private bool CanAccessDirectory(DirectoryInfo directory)
        {
            var accessControl = IOUtilities.PerformIO(directory.GetAccessControl);

            // Quick and dirty check.  If we can't even get the access control object, then we
            // can't access the file.
            if (accessControl == null)
            {
                return false;
            }

            // TODO(cyrusn): Do more checks here.
            return true;
        }

        private IEnumerable<CompletionItem> GetFilesAndDirectoriesInSearchPaths()
        {
            return _searchPaths.SelectMany(GetFilesAndDirectoriesInDirectory);
        }

        private IEnumerable<CompletionItem> GetLogicalDrives()
        {
            // First, we may have a filename, so let's include all drives
            return from d in _lazyGetDrives.Value
                   where d.Length > 0 && (d.Last() == Path.DirectorySeparatorChar || d.Last() == Path.AltDirectorySeparatorChar)
                   let text = d.Substring(0, d.Length - 1)
                   select new CompletionItem(_completionProvider, text, _textChangeSpan, glyph: _folderGlyph, rules: _itemRules);
        }

        private static FileSystemInfo[] GetFileSystemInfos(DirectoryInfo directoryInfo)
        {
            return IOUtilities.PerformIO(directoryInfo.GetFileSystemInfos, SpecializedCollections.EmptyArray<FileSystemInfo>());
        }
    }
}
