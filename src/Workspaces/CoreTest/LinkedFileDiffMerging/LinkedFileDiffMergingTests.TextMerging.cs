// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.LinkedFileDiffMerging
{
    public partial class LinkedFileDiffMergingTests
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.LinkedFileDiffMerging)]
        public void TestIdenticalChanges()
        {
            TestLinkedFileSet(
                "x",
                new List<string> { "y", "y" },
                @"y",
                LanguageNames.CSharp);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.LinkedFileDiffMerging)]
        public void TestChangesInOnlyOneFile()
        {
            TestLinkedFileSet(
                "a b c d e",
                new List<string> { "a b c d e", "a z c z e" },
                @"a z c z e",
                LanguageNames.CSharp);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.LinkedFileDiffMerging)]
        public void TestIsolatedChangesInBothFiles()
        {
            TestLinkedFileSet(
                "a b c d e",
                new List<string> { "a z c d e", "a b c z e" },
                @"a z c z e",
                LanguageNames.CSharp);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.LinkedFileDiffMerging)]
        public void TestIdenticalEditAfterIsolatedChanges()
        {
            TestLinkedFileSet(
                "a b c d e",
                new List<string> { "a zzz c xx e", "a b c xx e" },
                @"a zzz c xx e",
                LanguageNames.CSharp);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.LinkedFileDiffMerging)]
        public void TestOneConflict()
        {
            TestLinkedFileSet(
                "a b c d e",
                new List<string> { "a b y d e", "a b z d e" },
                @"
/* " + string.Format(WorkspacesResources.UnmergedChangeFromProject, "ProjectName1") + @"
" + WorkspacesResources.BeforeHeader + @"
a b c d e
" + WorkspacesResources.AfterHeader + @"
a b z d e
*/
a b y d e",
                LanguageNames.CSharp);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.LinkedFileDiffMerging)]
        public void TestTwoConflictsOnSameLine()
        {
            TestLinkedFileSet(
                "a b c d e",
                new List<string> { "a q1 c z1 e", "a q2 c z2 e" },
                @"
/* " + string.Format(WorkspacesResources.UnmergedChangeFromProject, "ProjectName1") + @"
" + WorkspacesResources.BeforeHeader + @"
a b c d e
" + WorkspacesResources.AfterHeader + @"
a q2 c z2 e
*/
a q1 c z1 e",
                LanguageNames.CSharp);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.LinkedFileDiffMerging)]
        public void TestTwoConflictsOnAdjacentLines()
        {
            TestLinkedFileSet(
                @"One
Two
Three
Four",
                new List<string>
                {
                    @"One
TwoY
ThreeY
Four",
                    @"One
TwoZ
ThreeZ
Four"
                },
                @"One

/* " + string.Format(WorkspacesResources.UnmergedChangeFromProject, "ProjectName1") + @"
" + WorkspacesResources.BeforeHeader + @"
Two
Three
" + WorkspacesResources.AfterHeader + @"
TwoZ
ThreeZ
*/
TwoY
ThreeY
Four",
                LanguageNames.CSharp);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.LinkedFileDiffMerging)]
        public void TestTwoConflictsOnSeparatedLines()
        {
            TestLinkedFileSet(
                @"One
Two
Three
Four
Five",
                new List<string>
                {
                    @"One
TwoY
Three
FourY
Five",
                    @"One
TwoZ
Three
FourZ
Five"
                },
                @"One

/* " + string.Format(WorkspacesResources.UnmergedChangeFromProject, "ProjectName1") + @"
" + WorkspacesResources.BeforeHeader + @"
Two
" + WorkspacesResources.AfterHeader + @"
TwoZ
*/
TwoY
Three

/* " + string.Format(WorkspacesResources.UnmergedChangeFromProject, "ProjectName1") + @"
" + WorkspacesResources.BeforeHeader + @"
Four
" + WorkspacesResources.AfterHeader + @"
FourZ
*/
FourY
Five",
                LanguageNames.CSharp);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.LinkedFileDiffMerging)]
        public void TestManyLinkedFilesWithOverlappingChange()
        {
            TestLinkedFileSet(
                @"A",
                new List<string>
                {
                    @"A",
                    @"B",
                    @"C",
                    @"",
                },
                @"
/* " + string.Format(WorkspacesResources.UnmergedChangeFromProject, "ProjectName2") + @"
" + WorkspacesResources.BeforeHeader + @"
A
" + WorkspacesResources.AfterHeader + @"
C
*/

/* " + string.Format(WorkspacesResources.UnmergedChangeFromProject, "ProjectName3") + @"
" + WorkspacesResources.RemovedHeader + @"
A
*/
B",
                LanguageNames.CSharp);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.LinkedFileDiffMerging)]
        public void TestCommentsAddedCodeCSharp()
        {
            TestLinkedFileSet(
                @"",
                new List<string>
                {
                    @"A",
                    @"B",
                },
                @"
/* " + string.Format(WorkspacesResources.UnmergedChangeFromProject, "ProjectName1") + @"
" + WorkspacesResources.AddedHeader + @"
B
*/
A",
                LanguageNames.CSharp);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.LinkedFileDiffMerging)]
        public void TestCommentsAddedCodeVB()
        {
            TestLinkedFileSet(
                @"",
                new List<string>
                {
                    @"A",
                    @"B",
                },
                @"
' " + string.Format(WorkspacesResources.UnmergedChangeFromProject, "ProjectName1") + @" 
' " + WorkspacesResources.AddedHeader + @"
' B
A",
                LanguageNames.VisualBasic);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.LinkedFileDiffMerging)]
        public void TestCommentsRemovedCodeCSharp()
        {
            TestLinkedFileSet(
                @"A",
                new List<string>
                {
                    @"B",
                    @"",
                },
                @"
/* " + string.Format(WorkspacesResources.UnmergedChangeFromProject, "ProjectName1") + @"
" + WorkspacesResources.RemovedHeader + @"
A
*/
B",
                LanguageNames.CSharp);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.LinkedFileDiffMerging)]
        public void TestCommentsRemovedCodeVB()
        {
            TestLinkedFileSet(
                @"A",
                new List<string>
                {
                    @"B",
                    @"",
                },
                @"
' " + string.Format(WorkspacesResources.UnmergedChangeFromProject, "ProjectName1") + @" 
' " + WorkspacesResources.RemovedHeader + @"
' A
B",
                LanguageNames.VisualBasic);
        }
    }
}
