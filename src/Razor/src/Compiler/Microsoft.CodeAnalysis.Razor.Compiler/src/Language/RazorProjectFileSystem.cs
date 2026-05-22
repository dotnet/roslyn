// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// An abstraction for working with a project containing Razor files.
/// </summary>
public abstract partial class RazorProjectFileSystem
{
    internal const string DefaultBasePath = "/";

    public static readonly RazorProjectFileSystem Empty = new EmptyFileSystem();

    /// <summary>
    /// Gets a sequence of <see cref="RazorProjectItem"/> under the specific path in the project.
    /// </summary>
    /// <param name="basePath">The base path.</param>
    /// <returns>The sequence of <see cref="RazorProjectItem"/>.</returns>
    /// <remarks>
    /// Project items returned by this method have inferred FileKinds from their corresponding file paths.
    /// </remarks>
    public abstract IEnumerable<RazorProjectItem> EnumerateItems(string basePath);

    /// <summary>
    /// Gets a <see cref="RazorProjectItem"/> for the specified path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The <see cref="RazorProjectItem"/>.</returns>
    public RazorProjectItem GetItem(string path)
        => GetItem(path, fileKind: null);

    /// <summary>
    /// Gets a <see cref="RazorProjectItem"/> for the specified path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="fileKind">The file kind</param>
    /// <returns>The <see cref="RazorProjectItem"/>.</returns>
    public abstract RazorProjectItem GetItem(string path, RazorFileKind? fileKind);

    /// <summary>
    /// Gets the sequence of files named <paramref name="fileName"/> that are applicable to the specified path.
    /// </summary>
    /// <param name="path">The path of a project item.</param>
    /// <param name="fileName">The file name to seek.</param>
    /// <returns>A sequence of applicable <see cref="RazorProjectItem"/> instances.</returns>
    /// <remarks>
    /// This method returns paths starting from the project root and traverses to the directory of
    /// <paramref name="path"/>.
    /// e.g.
    /// /Views/Home/View.cshtml -> [ /FileName.cshtml, /Views/FileName.cshtml, /Views/Home/FileName.cshtml ]
    ///
    /// Project items returned by this method have inferred FileKinds from their corresponding file paths.
    /// </remarks>
    internal ImmutableArray<RazorProjectItem> FindHierarchicalItems(string path, string fileName)
    {
        return FindHierarchicalItems(basePath: DefaultBasePath, path, fileName);
    }

    /// <summary>
    /// Gets the sequence of files named <paramref name="fileName"/> that are applicable to the specified path.
    /// </summary>
    /// <param name="basePath">The base path.</param>
    /// <param name="path">The path of a project item.</param>
    /// <param name="fileName">The file name to seek.</param>
    /// <returns>A sequence of applicable <see cref="RazorProjectItem"/> instances.</returns>
    /// <remarks>
    /// This method returns paths starting from <paramref name="basePath"/> and traverses to the directory of
    /// <paramref name="path"/>.
    /// e.g.
    /// (/Views, /Views/Home/View.cshtml) -> [ /Views/FileName.cshtml, /Views/Home/FileName.cshtml ]
    ///
    /// Project items returned by this method have inferred FileKinds from their corresponding file paths.
    /// </remarks>
    internal ImmutableArray<RazorProjectItem> FindHierarchicalItems(string basePath, string path, string fileName)
    {
        ArgHelper.ThrowIfNullOrEmpty(fileName);

        basePath = NormalizeAndEnsureValidPath(basePath);
        path = NormalizeAndEnsureValidPath(path);

        Debug.Assert(!string.IsNullOrEmpty(path));

        if (path.Length == 1)
        {
            return [];
        }

        if (!path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var fileNameIndex = path.LastIndexOf('/');

        if (fileNameIndex == -1)
        {
            throw new InvalidOperationException($"Cannot find file name in path '{path}'");
        }

        var length = fileNameIndex + 1;
        var pathMemory = path.AsMemory();

        if (pathMemory.Span[(fileNameIndex + 1)..].Equals(fileName.AsSpan(), StringComparison.Ordinal))
        {
            pathMemory = pathMemory[..fileNameIndex];
        }

        using var result = new PooledArrayBuilder<RazorProjectItem>();

        var index = pathMemory.Length;

        while (index > basePath.Length && (index = pathMemory.Span.LastIndexOf('/')) >= 0)
        {
            pathMemory = pathMemory[..(index + 1)];

            var itemPath = string.Create(
                length: pathMemory.Length + fileName.Length,
                state: (pathMemory, fileName),
                static (span, state) =>
                {
                    var (memory, fileName) = state;

                    memory.Span.CopyTo(span);
                    span = span[memory.Length..];

                    fileName.AsSpan().CopyTo(span);
                    Debug.Assert(span[fileName.Length..].IsEmpty);
                });

            var item = GetItem(itemPath, fileKind: null);
            result.Add(item);

            // Slice to exclude the trailing '/' for the next pass.
            pathMemory = pathMemory[..^1];
        }

        return result.ToImmutableReversed();
    }

    /// <summary>
    /// Performs validation for paths passed to methods of <see cref="RazorProjectFileSystem"/>.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    protected virtual string NormalizeAndEnsureValidPath(string path)
    {
        ArgHelper.ThrowIfNullOrEmpty(path);

        if (path[0] != '/')
        {
            throw new ArgumentException(Resources.RazorProjectFileSystem_PathMustStartWithForwardSlash, nameof(path));
        }

        return path;
    }

    /// <summary>
    /// Create a Razor project file system based off of a root directory.
    /// </summary>
    /// <param name="rootDirectoryPath">The directory to root the file system at.</param>
    /// <returns>A <see cref="RazorProjectFileSystem"/></returns>
    public static RazorProjectFileSystem Create(string rootDirectoryPath)
    {
        ArgHelper.ThrowIfNullOrEmpty(rootDirectoryPath);

        return new DefaultRazorProjectFileSystem(rootDirectoryPath);
    }
}
