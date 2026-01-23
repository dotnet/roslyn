// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
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
                    new TaskItem(@"c:\packages\SourcePackage1\"),
                    new TaskItem(@"/packages/SourcePackage2/"),
                    new TaskItem(@"c:\MyProjects\MyProject\", new Dictionary<string, string>
                    {
                        { "SourceControl", "Git" },
                    }),
                    new TaskItem(@"c:\MyProjects\MyProject\a\b\", new Dictionary<string, string>
                    {
                        { "SourceControl", "Git" },
                        { "NestedRoot", "a/b" },
                        { "ContainingRoot", @"c:\MyProjects\MyProject\" },
                        { "some metadata", "some value" },
                    }),
                },
                Deterministic = true
            };

            bool result = task.Execute();
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", engine.Log);

            RoslynDebug.Assert(task.MappedSourceRoots is object);
            Assert.Equal(4, task.MappedSourceRoots.Length);

            Assert.Equal(Utilities.FixFilePath(@"c:\packages\SourcePackage1\"), task.MappedSourceRoots[0].ItemSpec);
            Assert.Equal(@"/_1/", task.MappedSourceRoots[0].GetMetadata("MappedPath"));

            Assert.Equal(Utilities.FixFilePath(@"/packages/SourcePackage2/"), task.MappedSourceRoots[1].ItemSpec);
            Assert.Equal(@"/_2/", task.MappedSourceRoots[1].GetMetadata("MappedPath"));

            Assert.Equal(Utilities.FixFilePath(@"c:\MyProjects\MyProject\"), task.MappedSourceRoots[2].ItemSpec);
            Assert.Equal(@"/_/", task.MappedSourceRoots[2].GetMetadata("MappedPath"));
            Assert.Equal(@"Git", task.MappedSourceRoots[2].GetMetadata("SourceControl"));

            Assert.Equal(Utilities.FixFilePath(@"c:\MyProjects\MyProject\a\b\"), task.MappedSourceRoots[3].ItemSpec);
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

            Assert.Equal(Utilities.FixFilePath(@"!@#:;$%^&*()_+|{}\"), task.MappedSourceRoots[0].ItemSpec);
            Assert.Equal(@"/_1/", task.MappedSourceRoots[0].GetMetadata("MappedPath"));

            Assert.Equal(Utilities.FixFilePath("****/"), task.MappedSourceRoots[1].ItemSpec);
            Assert.Equal(@"/_/", task.MappedSourceRoots[1].GetMetadata("MappedPath"));
            Assert.Equal(@"Git", task.MappedSourceRoots[1].GetMetadata("SourceControl"));

            Assert.Equal(Utilities.FixFilePath(@"****\|||:;\"), task.MappedSourceRoots[2].ItemSpec);
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
                    new TaskItem(@"c:\MyProjects\MyProject\"),
                    new TaskItem(@"c:\MyProjects\MyProject\a\a\", new Dictionary<string, string>
                    {
                        { "NestedRoot", @"a/a/" },
                        { "ContainingRoot", @"c:\MyProjects\MyProject\" },
                    }),
                    new TaskItem(@"c:\MyProjects\MyProject\a\b\", new Dictionary<string, string>
                    {
                        { "NestedRoot", @"a/b\" },
                        { "ContainingRoot", @"c:\MyProjects\MyProject\" },
                    }),
                    new TaskItem(@"c:\MyProjects\MyProject\a\c\", new Dictionary<string, string>
                    {
                        { "NestedRoot", @"a\c" },
                        { "ContainingRoot", @"c:\MyProjects\MyProject\" },
                    }),
                },
                Deterministic = true
            };

            bool result = task.Execute();
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", engine.Log);

            RoslynDebug.Assert(task.MappedSourceRoots is object);
            Assert.Equal(4, task.MappedSourceRoots.Length);

            Assert.Equal(Utilities.FixFilePath(@"c:\MyProjects\MyProject\"), task.MappedSourceRoots[0].ItemSpec);
            Assert.Equal(@"/_/", task.MappedSourceRoots[0].GetMetadata("MappedPath"));

            Assert.Equal(Utilities.FixFilePath(@"c:\MyProjects\MyProject\a\a\"), task.MappedSourceRoots[1].ItemSpec);
            Assert.Equal(@"/_/a/a/", task.MappedSourceRoots[1].GetMetadata("MappedPath"));

            Assert.Equal(Utilities.FixFilePath(@"c:\MyProjects\MyProject\a\b\"), task.MappedSourceRoots[2].ItemSpec);
            Assert.Equal(@"/_/a/b/", task.MappedSourceRoots[2].GetMetadata("MappedPath"));

            Assert.Equal(Utilities.FixFilePath(@"c:\MyProjects\MyProject\a\c\"), task.MappedSourceRoots[3].ItemSpec);
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
                    new TaskItem(@"c:\packages\SourcePackage1\"),
                    new TaskItem(@"C:\packages\SourcePackage1\"),
                    new TaskItem(@"c:\packages\SourcePackage2\"),
                },
                Deterministic = true
            };

            bool result = task.Execute();
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", engine.Log);

            RoslynDebug.Assert(task.MappedSourceRoots is object);
            Assert.Equal(3, task.MappedSourceRoots.Length);

            Assert.Equal(Utilities.FixFilePath(@"c:\packages\SourcePackage1\"), task.MappedSourceRoots[0].ItemSpec);
            Assert.Equal(@"/_/", task.MappedSourceRoots[0].GetMetadata("MappedPath"));

            Assert.Equal(Utilities.FixFilePath(@"C:\packages\SourcePackage1\"), task.MappedSourceRoots[1].ItemSpec);
            Assert.Equal(@"/_1/", task.MappedSourceRoots[1].GetMetadata("MappedPath"));

            Assert.Equal(Utilities.FixFilePath(@"c:\packages\SourcePackage2\"), task.MappedSourceRoots[2].ItemSpec);
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
                    "MapSourceRoots.NoSuchTopLevelSourceRoot", "SourceRoot.ContainingRoot", "SourceRoot", path2)) + Environment.NewLine +
                "ERROR : " + string.Format(task.Log.FormatResourceString(
                    "MapSourceRoots.NoSuchTopLevelSourceRoot", "SourceRoot.ContainingRoot", "SourceRoot", path1)) + Environment.NewLine, engine.Log);

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
                    "MapSourceRoots.ContainsDuplicate", "SourceRoot", path1, "SourceControl", "git", "tfvc")) + Environment.NewLine +
                "WARNING : " + string.Format(task.Log.FormatResourceString(
                    "MapSourceRoots.ContainsDuplicate", "SourceRoot", path1, "RevisionId", "RevId1", "RevId2")) + Environment.NewLine +
                "WARNING : " + string.Format(task.Log.FormatResourceString(
                    "MapSourceRoots.ContainsDuplicate", "SourceRoot", path1, "NestedRoot", "NR1A", "NR1B")) + Environment.NewLine +
                "WARNING : " + string.Format(task.Log.FormatResourceString(
                    "MapSourceRoots.ContainsDuplicate", "SourceRoot", path1, "ContainingRoot", path3, "CR")) + Environment.NewLine +
                "WARNING : " + string.Format(task.Log.FormatResourceString(
                    "MapSourceRoots.ContainsDuplicate", "SourceRoot", path1, "MappedPath", "MP1", "MP2")) + Environment.NewLine +
                "WARNING : " + string.Format(task.Log.FormatResourceString(
                    "MapSourceRoots.ContainsDuplicate", "SourceRoot", path1, "SourceLinkUrl", "URL1", "URL2")) + Environment.NewLine,
                engine.Log);

            AssertEx.NotNull(task.MappedSourceRoots);
            AssertEx.Equal(new[]
            {
                $"'{path1}' SourceControl='git' RevisionId='RevId1' NestedRoot='NR1A' ContainingRoot='{path3}' MappedPath='{(deterministic ? "/_/NR1A/" : path1)}' SourceLinkUrl='URL1'",
                $"'{path2}' SourceControl='git' RevisionId='' NestedRoot='NR2' ContainingRoot='{path3}' MappedPath='{(deterministic ? "/_/NR2/" : path2)}' SourceLinkUrl=''",
                $"'{path3}' SourceControl='' RevisionId='' NestedRoot='' ContainingRoot='' MappedPath='{(deterministic ? "/_/" : path3)}' SourceLinkUrl=''",
            }, task.MappedSourceRoots.Select(InspectSourceRoot));

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
                    new TaskItem(@"c:\MyProjects\MYPROJECT\"),
                    new TaskItem(@"c:\MyProjects\MyProject\a\b\", new Dictionary<string, string>
                    {
                        { "SourceControl", "Git" },
                        { "NestedRoot", "a/b" },
                        { "ContainingRoot", @"c:\MyProjects\MyProject\" },
                    }),
                },
                Deterministic = true
            };

            bool result = task.Execute();

            AssertEx.AssertEqualToleratingWhitespaceDifferences("ERROR : " + string.Format(task.Log.FormatResourceString(
                "MapSourceRoots.NoSuchTopLevelSourceRoot", "SourceRoot.ContainingRoot", "SourceRoot", @"c:\MyProjects\MyProject\")) + Environment.NewLine, engine.Log);

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
                AssertEx.Equal(new[]
                {
                    $"'{path1}' SourceControl='' RevisionId='' NestedRoot='a/b' ContainingRoot='{path1}' MappedPath='{path1}' SourceLinkUrl=''",
                }, task.MappedSourceRoots.Select(InspectSourceRoot));

                Assert.True(result);
            }
        }

        [Fact, WorkItem(82112, "https://github.com/dotnet/roslyn/issues/82112")]
        public void PathCanonicalization()
        {
            var engine = new MockEngine();

            // Create paths with relative components that need canonicalization
            var baseDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "test"));
            var pathWithRelative = Path.Combine(baseDir, "subfolder", "..", "another") + Path.DirectorySeparatorChar;
            var expectedCanonical = Path.Combine(baseDir, "another") + Path.DirectorySeparatorChar;

            var task = new MapSourceRoots
            {
                BuildEngine = engine,
                SourceRoots = new[]
                {
                    new TaskItem(pathWithRelative, new Dictionary<string, string>
                    {
                        { "SourceControl", "Git" },
                    }),
                },
                Deterministic = false
            };

            bool result = task.Execute();
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", engine.Log);

            RoslynDebug.Assert(task.MappedSourceRoots is object);
            Assert.Equal(1, task.MappedSourceRoots.Length);

            // Verify the path was canonicalized (relative parts removed)
            Assert.Equal(Utilities.FixFilePath(expectedCanonical), task.MappedSourceRoots[0].ItemSpec);
            Assert.Equal(Utilities.FixFilePath(expectedCanonical), task.MappedSourceRoots[0].GetMetadata("MappedPath"));
            Assert.Equal(@"Git", task.MappedSourceRoots[0].GetMetadata("SourceControl"));

            Assert.True(result);
        }

        [Fact, WorkItem(82112, "https://github.com/dotnet/roslyn/issues/82112")]
        public void PathCanonicalization_PreservesTrailingSeparator()
        {
            var engine = new MockEngine();

            // Test with both forward slash and backslash
            var baseDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "test"));
            var pathWithBackslash = Path.Combine(baseDir, "subfolder", "..", "dir1") + "\\";
            var pathWithForwardSlash = Path.Combine(baseDir, "subfolder", "..", "dir2") + "/";
            var expectedDir1 = Path.Combine(baseDir, "dir1") + "\\";
            var expectedDir2 = Path.Combine(baseDir, "dir2") + "/";

            var task = new MapSourceRoots
            {
                BuildEngine = engine,
                SourceRoots = new[]
                {
                    new TaskItem(pathWithBackslash),
                    new TaskItem(pathWithForwardSlash),
                },
                Deterministic = false
            };

            bool result = task.Execute();
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", engine.Log);

            RoslynDebug.Assert(task.MappedSourceRoots is object);
            Assert.Equal(2, task.MappedSourceRoots.Length);

            // Verify trailing separators are preserved
            Assert.Equal(Utilities.FixFilePath(expectedDir1), task.MappedSourceRoots[0].ItemSpec);
            Assert.Equal(Utilities.FixFilePath(expectedDir2), task.MappedSourceRoots[1].ItemSpec);

            Assert.True(result);
        }

        [Theory, CombinatorialData]
        [WorkItem(82112, "https://github.com/dotnet/roslyn/issues/82112")]
        public void PathCanonicalization_Deterministic(bool deterministic)
        {
            var engine = new MockEngine();

            // Test that canonicalization works with both deterministic and non-deterministic modes
            var baseDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "project"));
            var pathWithRelative = Path.Combine(baseDir, "src", "..", "src") + Path.DirectorySeparatorChar;
            var expectedCanonical = Path.Combine(baseDir, "src") + Path.DirectorySeparatorChar;

            var task = new MapSourceRoots
            {
                BuildEngine = engine,
                SourceRoots = new[]
                {
                    new TaskItem(pathWithRelative, new Dictionary<string, string>
                    {
                        { "SourceControl", "Git" },
                    }),
                },
                Deterministic = deterministic
            };

            bool result = task.Execute();
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", engine.Log);

            RoslynDebug.Assert(task.MappedSourceRoots is object);
            Assert.Equal(1, task.MappedSourceRoots.Length);

            // Verify the ItemSpec is canonicalized in both modes
            Assert.Equal(Utilities.FixFilePath(expectedCanonical), task.MappedSourceRoots[0].ItemSpec);

            if (deterministic)
            {
                Assert.Equal(@"/_/", task.MappedSourceRoots[0].GetMetadata("MappedPath"));
            }
            else
            {
                Assert.Equal(Utilities.FixFilePath(expectedCanonical), task.MappedSourceRoots[0].GetMetadata("MappedPath"));
            }

            Assert.True(result);
        }

        [Fact, WorkItem(82112, "https://github.com/dotnet/roslyn/issues/82112")]
        public void PathCanonicalization_ContainingRootAndNestedRoot()
        {
            var engine = new MockEngine();

            // Create paths with relative components in ContainingRoot and NestedRoot
            var baseDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "project"));
            var parentPath = Path.Combine(baseDir, "subfolder", "..", "parent") + Path.DirectorySeparatorChar;
            var nestedPath = Path.Combine(baseDir, "parent", "nested") + Path.DirectorySeparatorChar;
            var nestedRootWithRelative = Path.Combine("subfolder", "..", "nested") + Path.DirectorySeparatorChar;
            
            var expectedParent = Path.Combine(baseDir, "parent") + Path.DirectorySeparatorChar;
            var expectedNested = Path.Combine(baseDir, "parent", "nested") + Path.DirectorySeparatorChar;
            var expectedNestedRoot = Path.Combine("nested") + Path.DirectorySeparatorChar;

            var task = new MapSourceRoots
            {
                BuildEngine = engine,
                SourceRoots = new[]
                {
                    new TaskItem(parentPath, new Dictionary<string, string>
                    {
                        { "SourceControl", "Git" },
                    }),
                    new TaskItem(nestedPath, new Dictionary<string, string>
                    {
                        { "SourceControl", "Git" },
                        { "NestedRoot", nestedRootWithRelative },
                        { "ContainingRoot", parentPath },
                    }),
                },
                Deterministic = false
            };

            bool result = task.Execute();
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", engine.Log);

            RoslynDebug.Assert(task.MappedSourceRoots is object);
            Assert.Equal(2, task.MappedSourceRoots.Length);

            // Verify ContainingRoot was canonicalized
            Assert.Equal(Utilities.FixFilePath(expectedParent), task.MappedSourceRoots[0].ItemSpec);
            Assert.Equal(Utilities.FixFilePath(expectedNested), task.MappedSourceRoots[1].ItemSpec);
            Assert.Equal(Utilities.FixFilePath(expectedParent), task.MappedSourceRoots[1].GetMetadata("ContainingRoot"));
            
            // Verify NestedRoot was canonicalized
            Assert.Equal(Utilities.FixFilePath(expectedNestedRoot), task.MappedSourceRoots[1].GetMetadata("NestedRoot"));

            Assert.True(result);
        }
    }
}

