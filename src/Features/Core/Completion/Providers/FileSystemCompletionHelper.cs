// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal sealed class FileSystemCompletionHelper
    {
        private readonly ICurrentWorkingDirectoryDiscoveryService fileSystemDiscoveryService;
        private readonly Func<string, bool> exclude;
        private readonly Glyph folderGlyph;
        private readonly Glyph fileGlyph;

        // absolute paths
        private readonly ImmutableArray<string> searchPaths;

        private readonly ISet<string> allowableExtensions;

        private readonly Lazy<string[]> lazyGetDrives;
        private readonly ICompletionProvider completionProvider;
        private readonly TextSpan textChangeSpan;

        public FileSystemCompletionHelper(
            ICompletionProvider completionProvider,
            TextSpan textChangeSpan,
            ICurrentWorkingDirectoryDiscoveryService fileSystemDiscoveryService,
            Glyph folderGlyph,
            Glyph fileGlyph,
            ImmutableArray<string> searchPaths,
            IEnumerable<string> allowableExtensions,
            Func<string, bool> exclude = null)
        {
            Debug.Assert(searchPaths.All(path => PathUtilities.IsAbsolute(path)));

            this.completionProvider = completionProvider;
            this.textChangeSpan = textChangeSpan;
            this.searchPaths = searchPaths;
            this.allowableExtensions = allowableExtensions.Select(e => e.ToLowerInvariant()).ToSet();
            this.fileSystemDiscoveryService = fileSystemDiscoveryService;
            this.folderGlyph = folderGlyph;
            this.fileGlyph = fileGlyph;
            this.exclude = exclude;

            this.lazyGetDrives = new Lazy<string[]>(() =>
                IOUtilities.PerformIO(Directory.GetLogicalDrives, SpecializedCollections.EmptyArray<string>()));
        }

        public IEnumerable<CompletionItem> GetItems(string pathSoFar, string documentPath)
        {
            if (exclude != null && exclude(pathSoFar))
            {
                return SpecializedCollections.EmptyEnumerable<CompletionItem>();
            }

            return GetFilesAndDirectories(pathSoFar, documentPath).ToList();
        }

        private CompletionItem CreateCurrentDirectoryItem()
        {
            return new CompletionItem(completionProvider, ".", textChangeSpan);
        }

        private CompletionItem CreateParentDirectoryItem()
        {
            return new CompletionItem(completionProvider, "..", textChangeSpan);
        }

        private CompletionItem CreateNetworkRoot(TextSpan textChangeSpan)
        {
            return new CompletionItem(completionProvider, "\\\\", textChangeSpan);
        }

        private IList<CompletionItem> GetFilesAndDirectories(string path, string basePath)
        {
            var result = new List<CompletionItem>();
            var pathKind = PathUtilities.GetPathKind(path);
            switch (pathKind)
            {
                case PathKind.Empty:
                    result.Add(CreateCurrentDirectoryItem());

                    if (!IsDriveRoot(fileSystemDiscoveryService.CurrentDirectory))
                    {
                        result.Add(CreateParentDirectoryItem());
                    }

                    result.Add(CreateNetworkRoot(textChangeSpan));
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
                            fileSystemDiscoveryService.CurrentDirectory);

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
                                result.Add(CreateNetworkRoot(TextSpan.FromBounds(textChangeSpan.Start - 1, textChangeSpan.End)));
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

                    foreach (var searchPath in searchPaths)
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

            return result;
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
                completionProvider,
                child.Name,
                textChangeSpan,
                glyph: child is DirectoryInfo ? folderGlyph : fileGlyph,
                description: child.FullName.ToSymbolDisplayParts());
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
                    allowableExtensions.Count == 0 ||
                    allowableExtensions.Contains(Path.GetExtension(child.Name).ToLowerInvariant());
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
            return searchPaths.SelectMany(GetFilesAndDirectoriesInDirectory);
        }

        private IEnumerable<CompletionItem> GetLogicalDrives()
        {
            // First, we may have a filename, so let's include all drives
            return from d in lazyGetDrives.Value
                   where d.Length > 0 && (d.Last() == Path.DirectorySeparatorChar || d.Last() == Path.AltDirectorySeparatorChar)
                   let text = d.Substring(0, d.Length - 1)
                   select new CompletionItem(completionProvider, text, textChangeSpan, glyph: folderGlyph);
        }

        private static FileSystemInfo[] GetFileSystemInfos(DirectoryInfo directoryInfo)
        {
            return IOUtilities.PerformIO(directoryInfo.GetFileSystemInfos, SpecializedCollections.EmptyArray<FileSystemInfo>());
        }
    }
}
