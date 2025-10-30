// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.LinkedFileDiffMerging;

[Trait(Traits.Feature, Traits.Features.LinkedFileDiffMerging)]
public partial class LinkedFileDiffMergingTests
{
    [Fact]
    public void TestIdenticalChanges()
        => TestLinkedFileSet(
            "x",
            ["y", "y"],
            @"y",
            LanguageNames.CSharp);

    [Fact]
    public void TestChangesInOnlyOneFile()
        => TestLinkedFileSet(
            "a b c d e",
            ["a b c d e", "a z c z e"],
            @"a z c z e",
            LanguageNames.CSharp);

    [Fact]
    public void TestIsolatedChangesInBothFiles()
        => TestLinkedFileSet(
            "a b c d e",
            ["a z c d e", "a b c z e"],
            @"a z c z e",
            LanguageNames.CSharp);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
    public void TestIdenticalEditAfterIsolatedChanges()
        => TestLinkedFileSet(
            "a; b; c; d; e;",
            ["a; zzz; c; xx; e;", "a; b; c; xx; e;"],
            @"a; zzz; c; xx; e;",
            LanguageNames.CSharp);

    [Fact]
    public void TestOneConflict()
        => TestLinkedFileSet(
            "a b c d e",
            ["a b y d e", "a b z d e"],
            $"""

            <<<<<<< {string.Format(WorkspacesResources.TODO_Unmerged_change_from_project_0, "ProjectName1")}, {WorkspacesResources.Before_colon}
            a b c d e
            =======
            a b z d e
            >>>>>>> {WorkspacesResources.After}
            a b y d e
            """,
            LanguageNames.CSharp);

    [Fact]
    public void TestTwoConflictsOnSameLine()
        => TestLinkedFileSet(
            "a b c d e",
            ["a q1 c z1 e", "a q2 c z2 e"],
            $"""

            <<<<<<< {string.Format(WorkspacesResources.TODO_Unmerged_change_from_project_0, "ProjectName1")}, {WorkspacesResources.Before_colon}
            a b c d e
            =======
            a q2 c z2 e
            >>>>>>> {WorkspacesResources.After}
            a q1 c z1 e
            """,
            LanguageNames.CSharp);

    [Fact]
    public void TestTwoConflictsOnAdjacentLines()
        => TestLinkedFileSet(
            """
            One
            Two
            Three
            Four
            """,
            [
                """
                One
                TwoY
                ThreeY
                Four
                """,
                """
                One
                TwoZ
                ThreeZ
                Four
                """
            ],
            $"""
            One

            <<<<<<< {string.Format(WorkspacesResources.TODO_Unmerged_change_from_project_0, "ProjectName1")}, {WorkspacesResources.Before_colon}
            Two
            Three
            =======
            TwoZ
            ThreeZ
            >>>>>>> {WorkspacesResources.After}
            TwoY
            ThreeY
            Four
            """,
            LanguageNames.CSharp);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
    public void TestTwoConflictsOnSeparatedLines()
        => TestLinkedFileSet(
            """
            One;
            Two;
            Three;
            Four;
            Five;
            """,
            [
                """
                One;
                TwoY;
                Three;
                FourY;
                Five;
                """,
                """
                One;
                TwoZ;
                Three;
                FourZ;
                Five;
                """
            ],
            $"""
            One;

            <<<<<<< {string.Format(WorkspacesResources.TODO_Unmerged_change_from_project_0, "ProjectName1")}, {WorkspacesResources.Before_colon}
            Two;
            =======
            TwoZ;
            >>>>>>> {WorkspacesResources.After}
            TwoY;
            Three;

            <<<<<<< {string.Format(WorkspacesResources.TODO_Unmerged_change_from_project_0, "ProjectName1")}, {WorkspacesResources.Before_colon}
            Four;
            =======
            FourZ;
            >>>>>>> {WorkspacesResources.After}
            FourY;
            Five;
            """,
            LanguageNames.CSharp);

    [Fact]
    public void TestManyLinkedFilesWithOverlappingChange()
        => TestLinkedFileSet(
            @"A",
            [
                @"A",
                @"B",
                @"C",
                @"",
            ],
            $"""

            <<<<<<< {string.Format(WorkspacesResources.TODO_Unmerged_change_from_project_0, "ProjectName2")}, {WorkspacesResources.Before_colon}
            A
            =======
            C
            >>>>>>> {WorkspacesResources.After}

            <<<<<<< {string.Format(WorkspacesResources.TODO_Unmerged_change_from_project_0, "ProjectName3")}, {WorkspacesResources.Before_colon}
            A
            =======
            >>>>>>> {WorkspacesResources.After}
            B
            """,
            LanguageNames.CSharp);

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public void TestCommentsAddedCode(string language)
        => TestLinkedFileSet(
            @"",
            [
                @"A",
                @"B",
            ],
            $"""

            <<<<<<< {string.Format(WorkspacesResources.TODO_Unmerged_change_from_project_0, "ProjectName1")}, {WorkspacesResources.Before_colon}
            =======
            B
            >>>>>>> {WorkspacesResources.After}
            A
            """,
            language);

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public void TestCommentsRemovedCode(string language)
        => TestLinkedFileSet(
            @"A",
            [
                @"B",
                @"",
            ],
            $"""

            <<<<<<< {string.Format(WorkspacesResources.TODO_Unmerged_change_from_project_0, "ProjectName1")}, {WorkspacesResources.Before_colon}
            A
            =======
            >>>>>>> {WorkspacesResources.After}
            B
            """,
            language);
}
