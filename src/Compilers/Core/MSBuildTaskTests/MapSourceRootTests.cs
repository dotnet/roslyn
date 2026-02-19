// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public sealed class MapSourceRootsTests
    {
        private string InspectSourceRoot(ITaskItem sourceRoot)
            => $"'{sourceRoot.ItemSpec}'" +
               $" SourceControl='{sourceRoot.GetMetadata("SourceControl")}'" +
               $" RevisionId='{sourceRoot.GetMetadata("RevisionId")}'" +
               $" NestedRoot='{sourceRoot.GetMetadata("NestedRoot")}'" +
               $" ContainingRoot='{sourceRoot.GetMetadata("ContainingRoot")}'" +
               $" MappedPath='{sourceRoot.GetMetadata("MappedPath")}'" +
               $" SourceLinkUrl='{sourceRoot.GetMetadata("SourceLinkUrl")}'";

        [Fact]
        public void BasicMapping()
        {
            var engine = new MockEngine();

            var task = new MapSourceRoots
            {
                BuildEngine = engine,
                SourceRoots = new[]
                {
                    new TaskItem(Utilities.FixFilePath(@"c:\packages\SourcePackage1\")),
                    new TaskItem(@"/packages/SourcePackage2/"),
                    new TaskItem(Utilities.FixFilePath(@"c:\MyProjects\MyProject\"), new Dictionary<string, string>
                    {
                        { "SourceControl", "Git" },
                    }),
                    new TaskItem(Utilities.FixFilePath(@"c:\MyProjects\MyProject\a\b\"), new Dictionary<string, string>
                    {
                        { "SourceControl", "Git" },
                        { "NestedRoot", "a/b" },
                        { "ContainingRoot", Utilities.FixFilePath(@"c:\MyProjects\MyProject\") },
                        { "some metadata", "some value" },
                    }),
                },
                Deterministic = true
            };

            bool result = task.Execute();
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", engine.Log);

            RoslynDebug.Assert(task.MappedSourceRoots is object);
            Assert.Equal(4, task.MappedSourceRoots.Length);

            Assert.Equal(Utilities.GetFullPathNoThrow(Utilities.FixFilePath(@"c:\packages\SourcePackage1\")), task.MappedSourceRoots[0].ItemSpec);
            Assert.Equal(@"/_1/", task.MappedSourceRoots[0].GetMetadata("MappedPath"));

            Assert.Equal(Utilities.GetFullPathNoThrow(Utilities.FixFilePath("/packages/SourcePackage2/")), task.MappedSourceRoots[1].ItemSpec);
            Assert.Equal(@"/_2/", task.MappedSourceRoots[1].GetMetadata("MappedPath"));

            Assert.Equal(Utilities.GetFullPathNoThrow(Utilities.FixFilePath(@"c:\MyProjects\MyProject\")), task.MappedSourceRoots[2].ItemSpec);
            Assert.Equal(@"/_/", task.MappedSourceRoots[2].GetMetadata("MappedPath"));
            Assert.Equal(@"Git", task.MappedSourceRoots[2].GetMetadata("SourceControl"));

            Assert.Equal(Utilities.GetFullPathNoThrow(Utilities.FixFilePath(@"c:\MyProjects\MyProject\a\b\")), task.MappedSourceRoots[3].ItemSpec);
            Assert.Equal(@"/_/a/b/", task.MappedSourceRoots[3].GetMetadata("MappedPath"));
            Assert.Equal(@"Git", task.MappedSourceRoots[3].GetMetadata("SourceControl"));
            Assert.Equal(@"some value", task.MappedSourceRoots[3].GetMetadata("some metadata"));

            Assert.True(result);
        }

        [Fact]
        public void InvalidChars()
        {
            var engine = new MockEngine();

            var task = new MapSourceRoots
            {
                BuildEngine = engine,
                SourceRoots = new[]
                {
                    new TaskItem(@"!@#:;$%^&*()_+|{}\"),
                    new TaskItem(@"****/", new Dictionary<string, string>
                    {
                        { "SourceControl", "Git" },
                    }),
                    new TaskItem(@"****\|||:;\", new Dictionary<string, string>
                    {
                        { "SourceControl", "Git" },
                        { "NestedRoot", "|||:;" },
                        { "ContainingRoot", @"****/" },
                    }),
                },
                Deterministic = true
            };

            bool result = task.Execute();
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", engine.Log);

            RoslynDebug.Assert(task.MappedSourceRoots is object);
            Assert.Equal(3, task.MappedSourceRoots.Length);

            Assert.Equal(Utilities.FixFilePath(Utilities.GetFullPathNoThrow(@"!@#:;$%^&*()_+|{}\")), task.MappedSourceRoots[0].ItemSpec);
            Assert.Equal(@"/_1/", task.MappedSourceRoots[0].GetMetadata("MappedPath"));

            Assert.Equal(Utilities.FixFilePath(Utilities.GetFullPathNoThrow("****/")), task.MappedSourceRoots[1].ItemSpec);
            Assert.Equal(@"/_/", task.MappedSourceRoots[1].GetMetadata("MappedPath"));
            Assert.Equal(@"Git", task.MappedSourceRoots[1].GetMetadata("SourceControl"));

            Assert.Equal(Utilities.FixFilePath(Utilities.GetFullPathNoThrow(@"****\|||:;\")), task.MappedSourceRoots[2].ItemSpec);
            Assert.Equal(@"/_/|||:;/", task.MappedSourceRoots[2].GetMetadata("MappedPath"));
            Assert.Equal(@"Git", task.MappedSourceRoots[2].GetMetadata("SourceControl"));

            Assert.True(result);
        }

        [Fact]
        public void SourceRootPaths_EndWithSeparator()
        {
            var engine = new MockEngine();

            var task = new MapSourceRoots
            {
                BuildEngine = engine,
                SourceRoots = new[]
                {
                    new TaskItem(@"C:\"),
                    new TaskItem(@"C:/"),
                    new TaskItem(@"C:"),
                    new TaskItem(@"C"),
                },
                Deterministic = true
            };

            bool result = task.Execute();
            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
ERROR : {string.Format(ErrorString.MapSourceRoots_PathMustEndWithSlashOrBackslash, "SourceRoot", "C:")}
ERROR : {string.Format(ErrorString.MapSourceRoots_PathMustEndWithSlashOrBackslash, "SourceRoot", "C")}
", engine.Log);

            Assert.False(result);
        }

        [Fact]
        public void NestedRoots_Separators()
        {
            var engine = new MockEngine();

            var task = new MapSourceRoots
            {
                BuildEngine = engine,
                SourceRoots = new[]
                {
                    new TaskItem(Utilities.FixFilePath(@"c:\MyProjects\MyProject\")),
                    new TaskItem(Utilities.FixFilePath(@"c:\MyProjects\MyProject\a\a\"), new Dictionary<string, string>
                    {
                        { "NestedRoot", @"a/a/" },
                        { "ContainingRoot", Utilities.FixFilePath(@"c:\MyProjects\MyProject\") },
                    }),
                    new TaskItem(Utilities.FixFilePath(@"c:\MyProjects\MyProject\a\b\"), new Dictionary<string, string>
                    {
                        { "NestedRoot", @"a/b\" },
                        { "ContainingRoot", Utilities.FixFilePath(@"c:\MyProjects\MyProject\") },
                    }),
                    new TaskItem(Utilities.FixFilePath(@"c:\MyProjects\MyProject\a\c\"), new Dictionary<string, string>
                    {
                        { "NestedRoot", @"a\c" },
                        { "ContainingRoot", Utilities.FixFilePath(@"c:\MyProjects\MyProject\") },
                    }),
                },
                Deterministic = true
            };

            bool result = task.Execute();
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", engine.Log);

            RoslynDebug.Assert(task.MappedSourceRoots is object);
            Assert.Equal(4, task.MappedSourceRoots.Length);

            Assert.Equal(Utilities.GetFullPathNoThrow(Utilities.FixFilePath(@"c:\MyProjects\MyProject\")), task.MappedSourceRoots[0].ItemSpec);
            Assert.Equal(@"/_/", task.MappedSourceRoots[0].GetMetadata("MappedPath"));

            Assert.Equal(Utilities.GetFullPathNoThrow(Utilities.FixFilePath(@"c:\MyProjects\MyProject\a\a\")), task.MappedSourceRoots[1].ItemSpec);
            Assert.Equal(@"/_/a/a/", task.MappedSourceRoots[1].GetMetadata("MappedPath"));

            Assert.Equal(Utilities.GetFullPathNoThrow(Utilities.FixFilePath(@"c:\MyProjects\MyProject\a\b\")), task.MappedSourceRoots[2].ItemSpec);
            Assert.Equal(@"/_/a/b/", task.MappedSourceRoots[2].GetMetadata("MappedPath"));

            Assert.Equal(Utilities.GetFullPathNoThrow(Utilities.FixFilePath(@"c:\MyProjects\MyProject\a\c\")), task.MappedSourceRoots[3].ItemSpec);
            Assert.Equal(@"/_/a/c/", task.MappedSourceRoots[3].GetMetadata("MappedPath"));

            Assert.True(result);
        }

        [Fact]
        public void SourceRootCaseSensitive()
        {
            var engine = new MockEngine();

            var task = new MapSourceRoots
            {
                BuildEngine = engine,
                SourceRoots = new[]
                {
                    new TaskItem(Utilities.FixFilePath(@"c:\packages\SourcePackage1\")),
                    new TaskItem(Utilities.FixFilePath(@"C:\packages\SourcePackage1\")),
                    new TaskItem(Utilities.FixFilePath(@"c:\packages\SourcePackage2\")),
                },
                Deterministic = true
            };

            bool result = task.Execute();
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", engine.Log);

            RoslynDebug.Assert(task.MappedSourceRoots is object);
            Assert.Equal(3, task.MappedSourceRoots.Length);

            Assert.Equal(Utilities.GetFullPathNoThrow(Utilities.FixFilePath(@"c:\packages\SourcePackage1\")), task.MappedSourceRoots[0].ItemSpec);
            Assert.Equal(@"/_/", task.MappedSourceRoots[0].GetMetadata("MappedPath"));

            Assert.Equal(Utilities.GetFullPathNoThrow(Utilities.FixFilePath(@"C:\packages\SourcePackage1\")), task.MappedSourceRoots[1].ItemSpec);
            Assert.Equal(@"/_1/", task.MappedSourceRoots[1].GetMetadata("MappedPath"));

            Assert.Equal(Utilities.GetFullPathNoThrow(Utilities.FixFilePath(@"c:\packages\SourcePackage2\")), task.MappedSourceRoots[2].ItemSpec);
            Assert.Equal(@"/_2/", task.MappedSourceRoots[2].GetMetadata("MappedPath"));

            Assert.True(result);
        }

        [Fact]
        public void Error_Recursion()
        {
            var engine = new MockEngine();

            var path1 = Utilities.FixFilePath(@"c:\MyProjects\MyProject\a\1\");
            var path2 = Utilities.FixFilePath(@"c:\MyProjects\MyProject\a\2\");
            var path3 = Utilities.FixFilePath(@"c:\MyProjects\MyProject\");

            var task = new MapSourceRoots
            {
                BuildEngine = engine,
                SourceRoots = new[]
                {
                    new TaskItem(path1, new Dictionary<string, string>
                    {
                        { "ContainingRoot", path2 },
                        { "NestedRoot", "a/1" },
                    }),
                    new TaskItem(path2, new Dictionary<string, string>
                    {
                        { "ContainingRoot", path1 },
                        { "NestedRoot", "a/2" },
                    }),
                    new TaskItem(path3),
                },
                Deterministic = true
            };

            bool result = task.Execute();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                "ERROR : " + string.Format(task.Log.FormatResourceString(
                    "MapSourceRoots.NoSuchTopLevelSourceRoot", "SourceRoot.ContainingRoot", "SourceRoot", Utilities.GetFullPathNoThrow(path2))) + Environment.NewLine +
                "ERROR : " + string.Format(task.Log.FormatResourceString(
                    "MapSourceRoots.NoSuchTopLevelSourceRoot", "SourceRoot.ContainingRoot", "SourceRoot", Utilities.GetFullPathNoThrow(path1))) + Environment.NewLine, engine.Log);

            Assert.Null(task.MappedSourceRoots);
            Assert.False(result);
        }

        [Theory]
        [InlineData(new object[] { true })]
        [InlineData(new object[] { false })]
        public void MetadataMerge1(bool deterministic)
        {
            var engine = new MockEngine();

            var path1 = Utilities.FixFilePath(@"c:\packages\SourcePackage1\");
            var path2 = Utilities.FixFilePath(@"c:\packages\SourcePackage2\");
            var path3 = Utilities.FixFilePath(@"c:\packages\SourcePackage3\");

            var task = new MapSourceRoots
            {
                BuildEngine = engine,
                SourceRoots = new[]
                {
                    new TaskItem(path1, new Dictionary<string, string>
                    {
                        { "NestedRoot", @"NR1A" },
                        { "ContainingRoot", path3 },
                        { "RevisionId", "RevId1" },
                        { "SourceControl", "git" },
                        { "MappedPath", "MP1" },
                        { "SourceLinkUrl", "URL1" },
                    }),
                    new TaskItem(path1, new Dictionary<string, string>
                    {
                        { "NestedRoot", @"NR1B" },
                        { "ContainingRoot", @"CR" },
                        { "RevisionId", "RevId2" },
                        { "SourceControl", "tfvc" },
                        { "MappedPath", "MP2" },
                        { "SourceLinkUrl", "URL2" },
                    }),
                    new TaskItem(path2, new Dictionary<string, string>
                    {
                        { "NestedRoot", @"NR2" },
                        { "SourceControl", "git" },
                    }),
                    new TaskItem(path2, new Dictionary<string, string>
                    {
                        { "ContainingRoot", path3 },
                        { "SourceControl", "git" },
                    }),
                    new TaskItem(path3),
                },
                Deterministic = deterministic
            };

            bool result = task.Execute();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                "WARNING : " + string.Format(task.Log.FormatResourceString(
                    "MapSourceRoots.ContainsDuplicate", "SourceRoot", Utilities.GetFullPathNoThrow(path1), "SourceControl", "git", "tfvc")) + Environment.NewLine +
                "WARNING : " + string.Format(task.Log.FormatResourceString(
                    "MapSourceRoots.ContainsDuplicate", "SourceRoot", Utilities.GetFullPathNoThrow(path1), "RevisionId", "RevId1", "RevId2")) + Environment.NewLine +
                "WARNING : " + string.Format(task.Log.FormatResourceString(
                    "MapSourceRoots.ContainsDuplicate", "SourceRoot", Utilities.GetFullPathNoThrow(path1), "NestedRoot", "NR1A", "NR1B")) + Environment.NewLine +
                "WARNING : " + string.Format(task.Log.FormatResourceString(
                    "MapSourceRoots.ContainsDuplicate", "SourceRoot", Utilities.GetFullPathNoThrow(path1), "ContainingRoot", path3, "CR")) + Environment.NewLine +
                "WARNING : " + string.Format(task.Log.FormatResourceString(
                    "MapSourceRoots.ContainsDuplicate", "SourceRoot", Utilities.GetFullPathNoThrow(path1), "MappedPath", "MP1", "MP2")) + Environment.NewLine +
                "WARNING : " + string.Format(task.Log.FormatResourceString(
                    "MapSourceRoots.ContainsDuplicate", "SourceRoot", Utilities.GetFullPathNoThrow(path1), "SourceLinkUrl", "URL1", "URL2")) + Environment.NewLine,
                engine.Log);

            AssertEx.NotNull(task.MappedSourceRoots);
            AssertEx.Equal(string.Join("\n",
            [
                $"'{Utilities.GetFullPathNoThrow(path1)}' SourceControl='git' RevisionId='RevId1' NestedRoot='NR1A' ContainingRoot='{(deterministic ? Utilities.GetFullPathNoThrow(path3) : path3)}' MappedPath='{(deterministic ? "/_/NR1A/" : Utilities.GetFullPathNoThrow(path1))}' SourceLinkUrl='URL1'",
                $"'{Utilities.GetFullPathNoThrow(path2)}' SourceControl='git' RevisionId='' NestedRoot='NR2' ContainingRoot='{(deterministic ? Utilities.GetFullPathNoThrow(path3) : path3)}' MappedPath='{(deterministic ? "/_/NR2/" : Utilities.GetFullPathNoThrow(path2))}' SourceLinkUrl=''",
                $"'{Utilities.GetFullPathNoThrow(path3)}' SourceControl='' RevisionId='' NestedRoot='' ContainingRoot='' MappedPath='{(deterministic ? "/_/" : Utilities.GetFullPathNoThrow(path3))}' SourceLinkUrl=''",
            ]), string.Join("\n", task.MappedSourceRoots.Select(InspectSourceRoot)));

            Assert.True(result);
        }

        [Fact]
        public void Error_MissingContainingRoot()
        {
            var engine = new MockEngine();

            var task = new MapSourceRoots
            {
                BuildEngine = engine,
                SourceRoots = new[]
                {
                    new TaskItem(Utilities.FixFilePath(@"c:\MyProjects\MYPROJECT\")),
                    new TaskItem(Utilities.FixFilePath(@"c:\MyProjects\MyProject\a\b\"), new Dictionary<string, string>
                    {
                        { "SourceControl", "Git" },
                        { "NestedRoot", "a/b" },
                        { "ContainingRoot", Utilities.FixFilePath(@"c:\MyProjects\MyProject\") },
                    }),
                },
                Deterministic = true
            };

            bool result = task.Execute();

            AssertEx.AssertEqualToleratingWhitespaceDifferences("ERROR : " + string.Format(task.Log.FormatResourceString(
                "MapSourceRoots.NoSuchTopLevelSourceRoot", "SourceRoot.ContainingRoot", "SourceRoot", Utilities.GetFullPathNoThrow(Utilities.FixFilePath(@"c:\MyProjects\MyProject\")))) + Environment.NewLine, engine.Log);

            Assert.Null(task.MappedSourceRoots);
            Assert.False(result);
        }

        [Fact]
        public void Error_NoContainingRootSpecified()
        {
            var engine = new MockEngine();

            var task = new MapSourceRoots
            {
                BuildEngine = engine,
                SourceRoots = new[]
                {
                    new TaskItem(@"c:\MyProjects\MyProject\"),
                    new TaskItem(@"c:\MyProjects\MyProject\a\b\", new Dictionary<string, string>
                    {
                        { "SourceControl", "Git" },
                        { "NestedRoot", "a/b" },
                    }),
                },
                Deterministic = true
            };

            bool result = task.Execute();

            AssertEx.AssertEqualToleratingWhitespaceDifferences("ERROR : " + string.Format(task.Log.FormatResourceString(
                "MapSourceRoots.NoSuchTopLevelSourceRoot", "SourceRoot.ContainingRoot", "SourceRoot", @"")) + Environment.NewLine, engine.Log);

            Assert.Null(task.MappedSourceRoots);
            Assert.False(result);
        }

        [Theory]
        [InlineData(new object[] { true })]
        [InlineData(new object[] { false })]
        public void Error_NoTopLevelSourceRoot(bool deterministic)
        {
            var engine = new MockEngine();

            var path1 = Utilities.FixFilePath(@"c:\MyProjects\MyProject\a\b\");

            var task = new MapSourceRoots
            {
                BuildEngine = engine,
                SourceRoots = new[]
                {
                    new TaskItem(path1, new Dictionary<string, string>
                    {
                        { "ContainingRoot", path1 },
                        { "NestedRoot", "a/b" },
                    }),
                },
                Deterministic = deterministic
            };

            bool result = task.Execute();

            if (deterministic)
            {
                AssertEx.AssertEqualToleratingWhitespaceDifferences("ERROR : " + string.Format(task.Log.FormatResourceString(
                    "MapSourceRoots.NoTopLevelSourceRoot", "SourceRoot", "DeterministicSourcePaths")) + Environment.NewLine, engine.Log);

                Assert.Null(task.MappedSourceRoots);
                Assert.False(result);
            }
            else
            {
                AssertEx.NotNull(task.MappedSourceRoots);
                AssertEx.Equal(string.Join("\n",
                [
                    $"'{Utilities.GetFullPathNoThrow(path1)}' SourceControl='' RevisionId='' NestedRoot='a/b' ContainingRoot='{(deterministic ? Utilities.GetFullPathNoThrow(path1) : path1)}' MappedPath='{Utilities.GetFullPathNoThrow(path1)}' SourceLinkUrl=''",
                ]), string.Join("\n", task.MappedSourceRoots.Select(InspectSourceRoot)));

                Assert.True(result);
            }
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/82112")]
        public void NormalizePaths(bool deterministic, [CombinatorialValues("e", "d/../e")] string nestedRoot)
        {
            var engine = new MockEngine();

            var originalPath1 = Utilities.FixFilePath(@"c:\MyProjects\MyProject\a\b\..\c\");
            var normalizedPath1 = Utilities.FixFilePath(@"c:\MyProjects\MyProject\a\c\");
            var originalPath2 = Utilities.FixFilePath(@"c:\MyProjects\MyProject\a\b\..\c\d\..\e\");
            var normalizedPath2 = Utilities.FixFilePath(@"c:\MyProjects\MyProject\a\c\e\");

            var task = new MapSourceRoots
            {
                BuildEngine = engine,
                SourceRoots =
                [
                    new TaskItem(originalPath1),
                    new TaskItem(originalPath2, new Dictionary<string, string>
                    {
                        { "ContainingRoot", originalPath1 },
                        { "NestedRoot", nestedRoot },
                    }),
                ],
                Deterministic = deterministic,
            };

            bool result = task.Execute();
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", engine.Log);

            var expectedMappedPath1 = deterministic ? "/_/" : Utilities.GetFullPathNoThrow(normalizedPath1);
            var expectedNestedRoot = deterministic ? "e" : nestedRoot;
            var expectedContainingRoot = deterministic ? Utilities.GetFullPathNoThrow(normalizedPath1) : originalPath1;
            var expectedMappedPath2 = deterministic ? "/_/e/" : Utilities.GetFullPathNoThrow(normalizedPath2);

            AssertEx.NotNull(task.MappedSourceRoots);
            AssertEx.Equal(
                $"""
                '{Utilities.GetFullPathNoThrow(normalizedPath1)}' SourceControl='' RevisionId='' NestedRoot='' ContainingRoot='' MappedPath='{expectedMappedPath1}' SourceLinkUrl=''
                '{Utilities.GetFullPathNoThrow(normalizedPath2)}' SourceControl='' RevisionId='' NestedRoot='{expectedNestedRoot}' ContainingRoot='{expectedContainingRoot}' MappedPath='{expectedMappedPath2}' SourceLinkUrl=''
                """,
                string.Join(Environment.NewLine, task.MappedSourceRoots.Select(InspectSourceRoot)));

            Assert.True(result);
        }
    }
}

