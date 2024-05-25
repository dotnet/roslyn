// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Completion;

public class FileSystemCompletionHelperTests
{
    private static void AssertItemsEqual(ImmutableArray<CompletionItem> actual, params string[] expected)
    {
        AssertEx.Equal(
            expected,
            actual.Select(c => $"'{c.DisplayText}', {string.Join(", ", c.Tags)}, '{c.GetProperty(CommonCompletionItem.DescriptionProperty)}'"),
            itemInspector: c => $"@\"{c}\"");

        Assert.True(actual.All(i => i.Rules == TestFileSystemCompletionHelper.CompletionRules));
    }

    [ConditionalFact(typeof(WindowsOnly))]
    public void GetItems_Windows1()
    {
        var fsc = new TestFileSystemCompletionHelper(
            searchPaths: ImmutableArray.Create(@"X:\A", @"X:\B"),
            baseDirectoryOpt: @"Z:\C",
            allowableExtensions: ImmutableArray.Create(".abc", ".def"),
            drives: new[] { @"X:\", @"Z:\" },
            directories: new[]
            {
                @"X:",
                @"X:\A",
                @"X:\A\1",
                @"X:\A\2",
                @"X:\A\3",
                @"X:\B",
                @"Z:",
                @"Z:\C",
                @"Z:\D",
            },
            files: new[]
            {
                @"X:\A\1\file1.abc",
                @"X:\A\2\file2.abc",
                @"X:\B\file4.x",
                @"X:\B\file5.abc",
                @"X:\B\hidden.def",
                @"Z:\C\file6.def",
                @"Z:\C\file.7.def",
            });

        // Note backslashes in description are escaped
        AssertItemsEqual(fsc.GetTestAccessor().GetItems("", CancellationToken.None),
            @"'file6.def', File, C#, 'Text|Z:\5CC\5Cfile6.def'",
            @"'file.7.def', File, C#, 'Text|Z:\5CC\5Cfile.7.def'",
            @"'X:', Folder, 'Text|X:'",
            @"'Z:', Folder, 'Text|Z:'",
            @"'\\', , 'Text|\5C\5C'",
            @"'1', Folder, 'Text|X:\5CA\5C1'",
            @"'2', Folder, 'Text|X:\5CA\5C2'",
            @"'3', Folder, 'Text|X:\5CA\5C3'",
            @"'file5.abc', File, C#, 'Text|X:\5CB\5Cfile5.abc'");

        AssertItemsEqual(fsc.GetTestAccessor().GetItems(@"X:\A\", CancellationToken.None),
            @"'1', Folder, 'Text|X:\5CA\5C1'",
            @"'2', Folder, 'Text|X:\5CA\5C2'",
            @"'3', Folder, 'Text|X:\5CA\5C3'");

        AssertItemsEqual(fsc.GetTestAccessor().GetItems(@"X:\B\", CancellationToken.None),
            @"'file5.abc', File, C#, 'Text|X:\5CB\5Cfile5.abc'");

        AssertItemsEqual(fsc.GetTestAccessor().GetItems(@"Z:\", CancellationToken.None),
            @"'C', Folder, 'Text|Z:\5CC'",
            @"'D', Folder, 'Text|Z:\5CD'");

        AssertItemsEqual(fsc.GetTestAccessor().GetItems(@"Z:", CancellationToken.None),
            @"'Z:', Folder, 'Text|Z:'");

        AssertItemsEqual(fsc.GetTestAccessor().GetItems(@"\", CancellationToken.None),
            @"'\\', , 'Text|\5C\5C'");
    }

    [ConditionalFact(typeof(WindowsOnly))]
    public void GetItems_Windows_NoBaseDirectory()
    {
        var fsc = new TestFileSystemCompletionHelper(
            searchPaths: ImmutableArray.Create(@"X:\A", @"X:\B"),
            baseDirectoryOpt: null,
            allowableExtensions: ImmutableArray.Create(".abc", ".def"),
            drives: new[] { @"X:\" },
            directories: new[]
            {
                @"X:",
                @"X:\A",
                @"X:\A\1",
                @"X:\A\2",
                @"X:\A\3",
                @"X:\B",
            },
            files: new[]
            {
                @"X:\A\1\file1.abc",
                @"X:\A\2\file2.abc",
                @"X:\B\file4.x",
                @"X:\B\file5.abc",
                @"X:\B\hidden.def",
            });

        // Note backslashes in description are escaped
        AssertItemsEqual(fsc.GetTestAccessor().GetItems(@"", CancellationToken.None),
            @"'X:', Folder, 'Text|X:'",
            @"'\\', , 'Text|\5C\5C'",
            @"'1', Folder, 'Text|X:\5CA\5C1'",
            @"'2', Folder, 'Text|X:\5CA\5C2'",
            @"'3', Folder, 'Text|X:\5CA\5C3'",
            @"'file5.abc', File, C#, 'Text|X:\5CB\5Cfile5.abc'");
    }

    [ConditionalFact(typeof(WindowsOnly))]
    public void GetItems_Windows_NoSearchPaths()
    {
        var fsc = new TestFileSystemCompletionHelper(
            searchPaths: ImmutableArray<string>.Empty,
            baseDirectoryOpt: null,
            allowableExtensions: ImmutableArray.Create(".abc", ".def"),
            drives: new[] { @"X:\" },
            directories: new[]
            {
                @"X:",
                @"X:\A",
                @"X:\A\1",
                @"X:\A\2",
                @"X:\A\3",
                @"X:\B",
            },
            files: new[]
            {
                @"X:\A\1\file1.abc",
                @"X:\A\2\file2.abc",
                @"X:\B\file4.x",
                @"X:\B\file5.abc",
                @"X:\B\hidden.def",
            });

        // Note backslashes in description are escaped
        AssertItemsEqual(fsc.GetTestAccessor().GetItems(@"", CancellationToken.None),
            @"'X:', Folder, 'Text|X:'",
            @"'\\', , 'Text|\5C\5C'");
    }

    [ConditionalFact(typeof(WindowsOnly))]
    public void GetItems_Windows_Network()
    {
        var fsc = new TestFileSystemCompletionHelper(
            searchPaths: ImmutableArray<string>.Empty,
            baseDirectoryOpt: null,
            allowableExtensions: ImmutableArray.Create(".cs"),
            drives: Array.Empty<string>(),
            directories: new[]
            {
                @"\\server\share",
                @"\\server\share\C",
                @"\\server\share\D",
            },
            files: new[]
            {
                @"\\server\share\C\b.cs",
                @"\\server\share\C\c.cs",
                @"\\server\share\D\e.cs",
            });

        AssertItemsEqual(fsc.GetTestAccessor().GetItems(@"\\server\share\", CancellationToken.None),
            @"'C', Folder, 'Text|\5C\5Cserver\5Cshare\5CC'",
            @"'D', Folder, 'Text|\5C\5Cserver\5Cshare\5CD'");

        AssertItemsEqual(fsc.GetTestAccessor().GetItems(@"\\server\share\C\", CancellationToken.None),
            @"'b.cs', File, C#, 'Text|\5C\5Cserver\5Cshare\5CC\5Cb.cs'",
            @"'c.cs', File, C#, 'Text|\5C\5Cserver\5Cshare\5CC\5Cc.cs'");
    }

    [ConditionalFact(typeof(UnixLikeOnly))]
    public void GetItems_Unix1()
    {
        var fsc = new TestFileSystemCompletionHelper(
            searchPaths: ImmutableArray.Create(@"/A", @"/B"),
            baseDirectoryOpt: @"/C",
            allowableExtensions: ImmutableArray.Create(".abc", ".def"),
            drives: Array.Empty<string>(),
            directories: new[]
            {
                @"/A",
                @"/A/1",
                @"/A/2",
                @"/A/3",
                @"/B",
                @"/C",
                @"/D",
            },
            files: new[]
            {
                @"/A/1/file1.abc",
                @"/A/2/file2.abc",
                @"/B/file4.x",
                @"/B/file5.abc",
                @"/B/hidden.def",
                @"/C/file6.def",
                @"/C/file.7.def",
            });

        // Note backslashes in description are escaped
        AssertItemsEqual(fsc.GetTestAccessor().GetItems(@"", CancellationToken.None),
            @"'file6.def', File, C#, 'Text|/C/file6.def'",
            @"'file.7.def', File, C#, 'Text|/C/file.7.def'",
            @"'/', Folder, 'Text|/'",
            @"'1', Folder, 'Text|/A/1'",
            @"'2', Folder, 'Text|/A/2'",
            @"'3', Folder, 'Text|/A/3'",
            @"'file5.abc', File, C#, 'Text|/B/file5.abc'");

        AssertItemsEqual(fsc.GetTestAccessor().GetItems(@"/", CancellationToken.None),
            @"'A', Folder, 'Text|/A'",
            @"'B', Folder, 'Text|/B'",
            @"'C', Folder, 'Text|/C'",
            @"'D', Folder, 'Text|/D'");

        AssertItemsEqual(fsc.GetTestAccessor().GetItems(@"/B/", CancellationToken.None),
            @"'file5.abc', File, C#, 'Text|/B/file5.abc'");
    }
}
