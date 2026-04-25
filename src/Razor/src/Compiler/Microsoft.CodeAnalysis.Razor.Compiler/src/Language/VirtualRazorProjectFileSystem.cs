// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language;

internal class VirtualRazorProjectFileSystem : RazorProjectFileSystem
{
    private readonly DirectoryNode _root = new DirectoryNode("/");

    public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
    {
        basePath = NormalizeAndEnsureValidPath(basePath);
        var directory = _root.GetDirectory(basePath);
        return directory?.EnumerateItems() ?? [];
    }

    public override RazorProjectItem GetItem(string path, RazorFileKind? fileKind)
    {
        path = NormalizeAndEnsureValidPath(path);
        return _root.GetItem(path) ?? new NotFoundProjectItem(path, fileKind);
    }

    public void Add(RazorProjectItem projectItem)
    {
        ArgHelper.ThrowIfNull(projectItem);

        var filePath = NormalizeAndEnsureValidPath(projectItem.FilePath);
        _root.AddFile(new FileNode(filePath, projectItem));
    }

    // Internal for testing
    [DebuggerDisplay("{Path}")]
    internal sealed class DirectoryNode(string path)
    {
        public string Path { get; } = path;

        public List<DirectoryNode> Directories { get; } = [];
        public List<FileNode> Files { get; } = [];

        public void AddFile(FileNode fileNode)
        {
            var filePath = fileNode.Path;
            if (!filePath.StartsWith(Path, StringComparison.OrdinalIgnoreCase))
            {
                var message = Resources.FormatVirtualFileSystem_FileDoesNotBelongToDirectory(fileNode.Path, Path);
                throw new InvalidOperationException(message);
            }

            // Look for the first / that appears in the path after the current directory path.
            var directoryPath = GetDirectoryPath(filePath);
            var directory = GetOrAddDirectory(this, directoryPath, createIfNotExists: true);
            Debug.Assert(directory != null);
            directory.Files.Add(fileNode);
        }

        public DirectoryNode? GetDirectory(string path)
        {
            if (!path.StartsWith(Path, StringComparison.OrdinalIgnoreCase))
            {
                var message = Resources.FormatVirtualFileSystem_FileDoesNotBelongToDirectory(path, Path);
                throw new InvalidOperationException(message);
            }

            return GetOrAddDirectory(this, path);
        }

        public IEnumerable<RazorProjectItem> EnumerateItems()
        {
            foreach (var file in Files)
            {
                yield return file.ProjectItem;
            }

            foreach (var directory in Directories)
            {
                foreach (var file in directory.EnumerateItems())
                {
                    yield return file;
                }
            }
        }

        public RazorProjectItem? GetItem(string path)
        {
            if (!path.StartsWith(Path, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(Resources.FormatVirtualFileSystem_FileDoesNotBelongToDirectory(path, Path));
            }

            var directoryPath = GetDirectoryPath(path);
            var directory = GetOrAddDirectory(this, directoryPath);
            if (directory == null)
            {
                return null;
            }

            foreach (var file in directory.Files)
            {
                var filePath = file.Path;
                var directoryLength = directory.Path.Length;

                // path, filePath -> /Views/Home/Index.cshtml
                // directory.Path -> /Views/Home/
                // We only need to match the file name portion since we've already matched the directory segment.
                if (string.Compare(path, directoryLength, filePath, directoryLength, path.Length - directoryLength, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return file.ProjectItem;
                }
            }

            return null;
        }

        private static string GetDirectoryPath(string path)
        {
            // /dir1/dir2/file.cshtml -> /dir1/dir2/
            var fileNameIndex = path.LastIndexOf('/');

            return fileNameIndex >= 0
                ? path[..(fileNameIndex + 1)]
                : path;
        }

        private static DirectoryNode? GetOrAddDirectory(
            DirectoryNode directory,
            string path,
            bool createIfNotExists = false)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));
            if (path[^1] != '/')
            {
                path += '/';
            }

            int index;
            while ((index = path.IndexOf('/', directory.Path.Length)) != -1 && index != path.Length)
            {
                var subDirectory = FindSubDirectory(directory, path);

                if (subDirectory == null)
                {
                    if (createIfNotExists)
                    {
                        var directoryPath = path.Substring(0, index + 1); // + 1 to include trailing slash
                        subDirectory = new DirectoryNode(directoryPath);
                        directory.Directories.Add(subDirectory);
                    }
                    else
                    {
                        return null;
                    }
                }

                directory = subDirectory;
            }

            return directory;
        }

        private static DirectoryNode? FindSubDirectory(DirectoryNode parentDirectory, string path)
        {
            for (var i = 0; i < parentDirectory.Directories.Count; i++)
            {
                // ParentDirectory.Path -> /Views/Home/
                // CurrentDirectory.Path -> /Views/Home/SubDir/
                // Path -> /Views/Home/SubDir/MorePath/File.cshtml
                // Each invocation of FindSubDirectory returns the immediate subdirectory along the path to the file.

                var currentDirectory = parentDirectory.Directories[i];
                var directoryPath = currentDirectory.Path;
                var startIndex = parentDirectory.Path.Length;
                var directoryNameLength = directoryPath.Length - startIndex;

                if (string.Compare(path, startIndex, directoryPath, startIndex, directoryNameLength, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return currentDirectory;
                }
            }

            return null;
        }
    }

    // Internal for testing
    [DebuggerDisplay("{Path}")]
    internal readonly struct FileNode(string path, RazorProjectItem projectItem)
    {
        public string Path => path;
        public RazorProjectItem ProjectItem => projectItem;
    }
}
