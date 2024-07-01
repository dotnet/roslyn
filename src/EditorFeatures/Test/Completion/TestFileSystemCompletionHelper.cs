// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Completion;

internal sealed class TestFileSystemCompletionHelper : FileSystemCompletionHelper
{
    internal static readonly CompletionItemRules CompletionRules = CompletionItemRules.Default;

    private readonly ImmutableArray<string> _directories;
    private readonly ImmutableArray<string> _files;
    private readonly ImmutableArray<string> _drives;

    public TestFileSystemCompletionHelper(
        ImmutableArray<string> searchPaths,
        string baseDirectoryOpt,
        ImmutableArray<string> allowableExtensions,
        IEnumerable<string> drives,
        IEnumerable<string> directories,
        IEnumerable<string> files)
        : base(Glyph.OpenFolder, Glyph.CSharpFile, searchPaths, baseDirectoryOpt, allowableExtensions, CompletionRules)
    {
        Assert.True(drives.All(d => d.EndsWith(PathUtilities.DirectorySeparatorStr)));
        Assert.True(directories.All(d => !d.EndsWith(PathUtilities.DirectorySeparatorStr)));

        _drives = ImmutableArray.CreateRange(drives);
        _directories = ImmutableArray.CreateRange(directories);
        _files = ImmutableArray.CreateRange(files);
    }

    protected override string[] GetLogicalDrives()
        => _drives.ToArray();

    protected override bool IsVisibleFileSystemEntry(string fullPath)
        => !fullPath.Contains("hidden");

    protected override bool DirectoryExists(string fullPath)
        => _directories.Contains(fullPath.TrimEnd(PathUtilities.DirectorySeparatorChar));

    protected override IEnumerable<string> EnumerateDirectories(string fullDirectoryPath)
        => Enumerate(_directories, fullDirectoryPath);

    protected override IEnumerable<string> EnumerateFiles(string fullDirectoryPath)
        => Enumerate(_files, fullDirectoryPath);

    private static IEnumerable<string> Enumerate(ImmutableArray<string> entries, string fullDirectoryPath)
    {
        var withTrailingSeparator = fullDirectoryPath.TrimEnd(PathUtilities.DirectorySeparatorChar) + PathUtilities.DirectorySeparatorChar;
        return from d in entries
               where d.StartsWith(withTrailingSeparator)
               let nextSeparator = d.IndexOf(PathUtilities.DirectorySeparatorChar, withTrailingSeparator.Length)
               select d[..((nextSeparator >= 0) ? nextSeparator : d.Length)];
    }
}
