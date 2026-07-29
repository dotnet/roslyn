// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public class GenerateMSBuildEditorConfigTests
    {
        [Fact]
        public void GlobalPropertyIsGeneratedIfEmpty()
        {
            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig();
            configTask.Execute();

            var result = configTask.ConfigFileContents;
            Assert.Equal(@"is_global = true
", result);
        }

        [Fact]
        public void PropertiesAreGeneratedInGlobalSection()
        {
            ITaskItem property1 = MSBuildUtil.CreateTaskItem("Property1", new Dictionary<string, string> { { "Value", "abc123" } });
            ITaskItem property2 = MSBuildUtil.CreateTaskItem("Property2", new Dictionary<string, string> { { "Value", "def456" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                PropertyItems = new[] { property1, property2 }
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true
build_property.Property1 = abc123
build_property.Property2 = def456
", result);
        }

        [Fact]
        public void ItemMetaDataCreatesSection()
        {
            ITaskItem item1 = MSBuildUtil.CreateTaskItem("c:/file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                MetadataItems = new[] { item1 }
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true

[c:/file1.cs]
build_metadata.Compile.ToRetrieve = abc123
", result);
        }

        [Fact]
        public void MultipleItemMetaDataCreatesSections()
        {
            ITaskItem item1 = MSBuildUtil.CreateTaskItem("c:/file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });
            ITaskItem item2 = MSBuildUtil.CreateTaskItem("c:/file2.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "def456" } });
            ITaskItem item3 = MSBuildUtil.CreateTaskItem("c:/file3.cs", new Dictionary<string, string> { { "ItemType", "AdditionalFiles" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "ghi789" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                MetadataItems = new[] { item1, item2, item3 }
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true

[c:/file1.cs]
build_metadata.Compile.ToRetrieve = abc123

[c:/file2.cs]
build_metadata.Compile.ToRetrieve = def456

[c:/file3.cs]
build_metadata.AdditionalFiles.ToRetrieve = ghi789
", result);
        }

        [Fact]
        [WorkItem(52469, "https://github.com/dotnet/roslyn/issues/52469")]
        public void MultipleSpecialCharacterItemMetaDataCreatesSections()
        {
            ITaskItem item1 = MSBuildUtil.CreateTaskItem("c:/{f*i?le1}.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });
            ITaskItem item2 = MSBuildUtil.CreateTaskItem("c:/f,ile#2.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "def456" } });
            ITaskItem item3 = MSBuildUtil.CreateTaskItem("c:/f;i!le[3].cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "ghi789" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                MetadataItems = new[] { item1, item2, item3 }
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true

[c:/\{f\*i\?le1\}.cs]
build_metadata.Compile.ToRetrieve = abc123

[c:/f\,ile\#2.cs]
build_metadata.Compile.ToRetrieve = def456

[c:/f\;i\!le\[3\].cs]
build_metadata.Compile.ToRetrieve = ghi789
", result);
        }

        [Fact]
        public void DuplicateItemSpecsAreCombinedInSections()
        {
            ITaskItem item1 = MSBuildUtil.CreateTaskItem("c:/file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });
            ITaskItem item2 = MSBuildUtil.CreateTaskItem("c:/file1.cs", new Dictionary<string, string> { { "ItemType", "AdditionalFile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "def456" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                MetadataItems = new[] { item1, item2 }
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true

[c:/file1.cs]
build_metadata.Compile.ToRetrieve = abc123
build_metadata.AdditionalFile.ToRetrieve = def456
", result);
        }

        [Fact]
        public void ItemIsMissingRequestedMetadata()
        {
            ITaskItem item1 = MSBuildUtil.CreateTaskItem("c:/file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                MetadataItems = new[] { item1 }
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true

[c:/file1.cs]
build_metadata.Compile.ToRetrieve = 
", result);
        }

        [Fact]
        public void ItemIsMissingRequiredMetadata()
        {
            ITaskItem item1 = MSBuildUtil.CreateTaskItem("c:/file1.cs", new Dictionary<string, string> { });
            ITaskItem item2 = MSBuildUtil.CreateTaskItem("c:/file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" } });
            ITaskItem item3 = MSBuildUtil.CreateTaskItem("c:/file1.cs", new Dictionary<string, string> { { "MetadataName", "ToRetrieve" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                MetadataItems = new[] { item1, item2, item3 }
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true

[c:/file1.cs]
", result);
        }

        [Fact]
        public void PropertiesAreGeneratedBeforeItems()
        {
            ITaskItem item1 = MSBuildUtil.CreateTaskItem("c:/file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });
            ITaskItem item2 = MSBuildUtil.CreateTaskItem("c:/file2.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "def456" } });
            ITaskItem item3 = MSBuildUtil.CreateTaskItem("c:/file3.cs", new Dictionary<string, string> { { "ItemType", "AdditionalFiles" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "ghi789" } });
            ITaskItem item4 = MSBuildUtil.CreateTaskItem("c:/file1.cs", new Dictionary<string, string> { { "ItemType", "AdditionalFiles" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "jkl012" } });

            ITaskItem property1 = MSBuildUtil.CreateTaskItem("Property1", new Dictionary<string, string> { { "Value", "abc123" } });
            ITaskItem property2 = MSBuildUtil.CreateTaskItem("Property2", new Dictionary<string, string> { { "Value", "def456" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                MetadataItems = new[] { item1, item2, item3, item4 },
                PropertyItems = new[] { property1, property2 }
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true
build_property.Property1 = abc123
build_property.Property2 = def456

[c:/file1.cs]
build_metadata.Compile.ToRetrieve = abc123
build_metadata.AdditionalFiles.ToRetrieve = jkl012

[c:/file2.cs]
build_metadata.Compile.ToRetrieve = def456

[c:/file3.cs]
build_metadata.AdditionalFiles.ToRetrieve = ghi789
", result);
        }

        [Fact]
        public void ItemIsNotFullyQualifiedPath()
        {
            TaskItem item1 = new TaskItem("file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });
            TaskItem item2 = new TaskItem("subDir\\file2.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });
            TaskItem item3 = new TaskItem("someDir\\otherDir\\thirdDir\\..\\file3.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                MetadataItems = new[] { item1, item2, item3 }
            };
            configTask.Execute();
            var result = configTask.ConfigFileContents;

            // MSBuild will convert the above relative paths to absolute paths based on the current location.
            // We replicate that behavior here to test we get the expected full paths 
            string executingLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)?.Replace('\\', '/') ?? string.Empty;
            string expectedPath1 = $"{executingLocation}/file1.cs";
            string expectedPath2 = $"{executingLocation}/subDir/file2.cs";
            string expectedPath3 = $"{executingLocation}/someDir/otherDir/file3.cs";

            Assert.Equal($@"is_global = true

[{expectedPath1}]
build_metadata.Compile.ToRetrieve = abc123

[{expectedPath2}]
build_metadata.Compile.ToRetrieve = abc123

[{expectedPath3}]
build_metadata.Compile.ToRetrieve = abc123
", result);
        }

        [Fact]
        public void ItemsWithDifferentRelativeButSameFullPathAreCombined()
        {
            TaskItem item1 = new TaskItem("file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });
            TaskItem item2 = new TaskItem("someDir\\..\\file1.cs", new Dictionary<string, string> { { "ItemType", "AdditionalFile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "def456" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                MetadataItems = new[] { item1, item2 }
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            // MSBuild will convert the above relative paths to absolute paths based on the current location.
            // We replicate that behavior here to test we get the expected full paths 
            string executingLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)?.Replace('\\', '/') ?? string.Empty;
            string expectedPath = $"{executingLocation}/file1.cs";

            Assert.Equal($@"is_global = true

[{expectedPath}]
build_metadata.Compile.ToRetrieve = abc123
build_metadata.AdditionalFile.ToRetrieve = def456
", result);
        }

        [Fact]
        [WorkItem(43970, "https://github.com/dotnet/roslyn/issues/43970")]
        public void PropertiesWithNewLines()
        {
            // Currently new lines transfer from MSBuild through to the resulting configuration
            // which can break downstream parsing. This tests tracks issue #43970 and should
            // be adjusted when we address that.

            string longPropertyValue = @"this is 
a 
property
with  
linebreaks
"" quotation "" marks
and 
property = looking
values
";

            TaskItem property1 = new TaskItem("Property1", new Dictionary<string, string> { { "Value", longPropertyValue } });
            TaskItem property2 = new TaskItem("Property2", new Dictionary<string, string> { { "Value", "def456" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                PropertyItems = new[] { property1, property2 }
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true
build_property.Property1 = this is 
a 
property
with  
linebreaks
"" quotation "" marks
and 
property = looking
values

build_property.Property2 = def456
", result);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void ItemMetadataPathIsAdjustedOnWindows()
        {
            ITaskItem item1 = MSBuildUtil.CreateTaskItem("c:\\file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                MetadataItems = new[] { item1 }
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true

[c:/file1.cs]
build_metadata.Compile.ToRetrieve = abc123
", result);
        }

        [Fact]
        public void ConfigFileCanBeWrittenToDisk()
        {
            ITaskItem property1 = MSBuildUtil.CreateTaskItem("Property1", new Dictionary<string, string> { { "Value", "abc123" } });
            ITaskItem property2 = MSBuildUtil.CreateTaskItem("Property2", new Dictionary<string, string> { { "Value", "def456" } });

            var fileName = Path.Combine(TempRoot.Root, "ConfigFileCanBeWrittenToDisk.GenerateMSBuildEditorConfig.editorconfig");

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                PropertyItems = new[] { property1, property2 },
                FileName = new TaskItem(fileName)
            };
            configTask.Execute();

            var expectedContents = @"is_global = true
build_property.Property1 = abc123
build_property.Property2 = def456
";

            Assert.True(File.Exists(fileName));
            Assert.True(configTask.WriteMSBuildEditorConfig());
            Assert.Equal(expectedContents, File.ReadAllText(fileName));
        }

        [Fact]
        public void PathMapRewritesSectionPaths()
        {
            ITaskItem item1 = MSBuildUtil.CreateTaskItem("c:/repo/src/file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                MetadataItems = new[] { item1 },
                PathMap = "c:/repo/=/_/",
                MapSectionHeaderPaths = true
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true

[/_/src/file1.cs]
build_metadata.Compile.ToRetrieve = abc123
", result);
        }

        [Fact]
        public void PathMapNormalizesBackslashesInMappedPath()
        {
            // A backslash local prefix mapped to a forward-slash deterministic prefix should
            // produce a fully forward-slashed section path, matching what the compiler computes.
            ITaskItem item1 = MSBuildUtil.CreateTaskItem(@"c:\repo\src\file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                MetadataItems = new[] { item1 },
                PathMap = @"c:\repo\=/_/",
                MapSectionHeaderPaths = true
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true

[/_/src/file1.cs]
build_metadata.Compile.ToRetrieve = abc123
", result);
        }

        [Fact]
        public void PathMapLeavesNonMatchingPathsUnchanged()
        {
            ITaskItem item1 = MSBuildUtil.CreateTaskItem("c:/other/file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                MetadataItems = new[] { item1 },
                PathMap = "c:/repo/=/_/",
                MapSectionHeaderPaths = true
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true

[c:/other/file1.cs]
build_metadata.Compile.ToRetrieve = abc123
", result);
        }

        [Fact]
        public void EmptyPathMapLeavesSectionPathsUnchanged()
        {
            ITaskItem item1 = MSBuildUtil.CreateTaskItem("c:/repo/src/file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                MetadataItems = new[] { item1 },
                PathMap = string.Empty,
                MapSectionHeaderPaths = true
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true

[c:/repo/src/file1.cs]
build_metadata.Compile.ToRetrieve = abc123
", result);
        }

        [Fact]
        public void PathMapAppliesLongestMatchingPrefixFirst()
        {
            // The compiler applies the longest (most specific) matching prefix; the order of the
            // entries in the PathMap string must not matter.
            ITaskItem item1 = MSBuildUtil.CreateTaskItem("c:/repo/sub/file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });
            ITaskItem item2 = MSBuildUtil.CreateTaskItem("c:/repo/file2.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "def456" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                MetadataItems = new[] { item1, item2 },
                PathMap = "c:/repo/=/_/,c:/repo/sub/=/_sub/",
                MapSectionHeaderPaths = true
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true

[/_sub/file1.cs]
build_metadata.Compile.ToRetrieve = abc123

[/_/file2.cs]
build_metadata.Compile.ToRetrieve = def456
", result);
        }

        [Fact]
        public void PathMapSupportsMultipleRoots()
        {
            ITaskItem item1 = MSBuildUtil.CreateTaskItem("c:/a/file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });
            ITaskItem item2 = MSBuildUtil.CreateTaskItem("c:/b/file2.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "def456" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                MetadataItems = new[] { item1, item2 },
                PathMap = "c:/a/=/rootA/,c:/b/=/rootB/",
                MapSectionHeaderPaths = true
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true

[/rootA/file1.cs]
build_metadata.Compile.ToRetrieve = abc123

[/rootB/file2.cs]
build_metadata.Compile.ToRetrieve = def456
", result);
        }

        [Fact]
        public void PathMapSupportsDoubledSeparatorEscaping()
        {
            // A ',' inside a path is escaped by doubling it in the PathMap string, mirroring the
            // compiler's /pathmap parsing.
            ITaskItem item1 = MSBuildUtil.CreateTaskItem("c:/a,b/file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                MetadataItems = new[] { item1 },
                PathMap = "c:/a,,b/=/_/",
                MapSectionHeaderPaths = true
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true

[/_/file1.cs]
build_metadata.Compile.ToRetrieve = abc123
", result);
        }

        [Fact]
        public void PathMapRewritesPathValuedProperties()
        {
            // A property whose value is an absolute path under a mapped root (e.g. ProjectDir) is
            // rewritten so the generated config is independent of the checkout location.
            ITaskItem property1 = MSBuildUtil.CreateTaskItem("Property1", new Dictionary<string, string> { { "Value", "c:/repo/src" } });
            ITaskItem item1 = MSBuildUtil.CreateTaskItem("c:/repo/src/file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                PropertyItems = new[] { property1 },
                MetadataItems = new[] { item1 },
                PathMap = "c:/repo/=/_/",
                MapSectionHeaderPaths = true,
                MapPropertyValues = true
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true
build_property.Property1 = /_/src

[/_/src/file1.cs]
build_metadata.Compile.ToRetrieve = abc123
", result);
        }

        [Fact]
        public void PathMapNormalizesBackslashesInPropertyValues()
        {
            // The real ProjectDir value is a backslash absolute path; it must map to a fully
            // forward-slashed deterministic path just like the section headers.
            ITaskItem property1 = MSBuildUtil.CreateTaskItem("ProjectDir", new Dictionary<string, string> { { "Value", @"c:\repo\proj\" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                PropertyItems = new[] { property1 },
                PathMap = @"c:\repo\=/_/",
                MapPropertyValues = true
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true
build_property.ProjectDir = /_/proj/
", result);
        }

        [Fact]
        public void PathMapLeavesNonMatchingPropertyValuesUnchanged()
        {
            // Non-path values and paths outside any mapped root are prefix-anchored no-ops.
            ITaskItem property1 = MSBuildUtil.CreateTaskItem("TargetFramework", new Dictionary<string, string> { { "Value", "net11.0" } });
            ITaskItem property2 = MSBuildUtil.CreateTaskItem("OtherDir", new Dictionary<string, string> { { "Value", "c:/other/x" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                PropertyItems = new[] { property1, property2 },
                PathMap = "c:/repo/=/_/",
                MapPropertyValues = true
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true
build_property.TargetFramework = net11.0
build_property.OtherDir = c:/other/x
", result);
        }

        [Fact]
        public void EmptyPathMapLeavesPropertyValuesUnchanged()
        {
            ITaskItem property1 = MSBuildUtil.CreateTaskItem("ProjectDir", new Dictionary<string, string> { { "Value", "c:/repo/src" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                PropertyItems = new[] { property1 },
                PathMap = string.Empty,
                MapPropertyValues = true
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true
build_property.ProjectDir = c:/repo/src
", result);
        }

        [Fact]
        public void MappingIsOptInAndOffByDefault()
        {
            // With a PathMap supplied but neither opt-in flag set, nothing is mapped: section
            // headers and property values are emitted verbatim (the default, backwards-compatible
            // behavior).
            ITaskItem property1 = MSBuildUtil.CreateTaskItem("ProjectDir", new Dictionary<string, string> { { "Value", "c:/repo/proj" } });
            ITaskItem item1 = MSBuildUtil.CreateTaskItem("c:/repo/src/file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                PropertyItems = new[] { property1 },
                MetadataItems = new[] { item1 },
                PathMap = "c:/repo/=/_/"
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true
build_property.ProjectDir = c:/repo/proj

[c:/repo/src/file1.cs]
build_metadata.Compile.ToRetrieve = abc123
", result);
        }

        [Fact]
        public void MapSectionHeaderPathsDoesNotMapPropertyValues()
        {
            // Opting in to section-header mapping only: the header is mapped, the property value
            // (e.g. ProjectDir) is left absolute.
            ITaskItem property1 = MSBuildUtil.CreateTaskItem("ProjectDir", new Dictionary<string, string> { { "Value", "c:/repo/proj" } });
            ITaskItem item1 = MSBuildUtil.CreateTaskItem("c:/repo/src/file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                PropertyItems = new[] { property1 },
                MetadataItems = new[] { item1 },
                PathMap = "c:/repo/=/_/",
                MapSectionHeaderPaths = true
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true
build_property.ProjectDir = c:/repo/proj

[/_/src/file1.cs]
build_metadata.Compile.ToRetrieve = abc123
", result);
        }

        [Fact]
        public void MapPropertyValuesDoesNotMapSectionHeaders()
        {
            // Opting in to property-value mapping only: the value is mapped, the section header is
            // left absolute.
            ITaskItem property1 = MSBuildUtil.CreateTaskItem("ProjectDir", new Dictionary<string, string> { { "Value", "c:/repo/proj" } });
            ITaskItem item1 = MSBuildUtil.CreateTaskItem("c:/repo/src/file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });

            GenerateMSBuildEditorConfig configTask = new GenerateMSBuildEditorConfig()
            {
                PropertyItems = new[] { property1 },
                MetadataItems = new[] { item1 },
                PathMap = "c:/repo/=/_/",
                MapPropertyValues = true
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true
build_property.ProjectDir = /_/proj

[c:/repo/src/file1.cs]
build_metadata.Compile.ToRetrieve = abc123
", result);
        }
    }
}
