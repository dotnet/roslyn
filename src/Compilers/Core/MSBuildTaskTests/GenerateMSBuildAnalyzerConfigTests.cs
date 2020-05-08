// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public class GenerateMSBuildAnalyzerConfigTests
    {
        [Fact]
        public void GlobalPropertyIsGeneratedIfEmpty()
        {
            GenerateMSBuildAnalyzerConfig configTask = new GenerateMSBuildAnalyzerConfig();
            configTask.Execute();

            var result = configTask.ConfigFileContents;
            Assert.Equal(@"is_global = true
", result);
        }

        [Fact]
        public void PropertiesAreGeneratedInGlobalSection()
        {
            TaskItem property1 = new TaskItem("Property1", new Dictionary<string, string> { { "Value", "abc123" } });
            TaskItem property2 = new TaskItem("Property2", new Dictionary<string, string> { { "Value", "def456" } });

            GenerateMSBuildAnalyzerConfig configTask = new GenerateMSBuildAnalyzerConfig()
            {
                PropertyItems = new[] { property1, property2 }
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true
msbuild_property.Property1 = abc123
msbuild_property.Property2 = def456
", result);
        }

        [Fact]
        public void ItemMetaDataCreatesSection()
        {
            TaskItem item1 = new TaskItem("c:\\file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });

            GenerateMSBuildAnalyzerConfig configTask = new GenerateMSBuildAnalyzerConfig()
            {
                MetadataItems = new[] { item1 }
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true

[c:/file1.cs]
msbuild_item.Compile.ToRetrieve = abc123
", result);
        }

        [Fact]
        public void MutlipleItemMetaDataCreatesSections()
        {
            TaskItem item1 = new TaskItem("c:\\file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });
            TaskItem item2 = new TaskItem("c:\\file2.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "def456" } });
            TaskItem item3 = new TaskItem("c:\\file3.cs", new Dictionary<string, string> { { "ItemType", "AdditionalFiles" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "ghi789" } });

            GenerateMSBuildAnalyzerConfig configTask = new GenerateMSBuildAnalyzerConfig()
            {
                MetadataItems = new[] { item1, item2, item3 }
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true

[c:/file1.cs]
msbuild_item.Compile.ToRetrieve = abc123

[c:/file2.cs]
msbuild_item.Compile.ToRetrieve = def456

[c:/file3.cs]
msbuild_item.AdditionalFiles.ToRetrieve = ghi789
", result);
        }

        [Fact]
        public void DuplicateItemSpecsAreCombinedInSections()
        {
            TaskItem item1 = new TaskItem("c:\\file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });
            TaskItem item2 = new TaskItem("c:\\file1.cs", new Dictionary<string, string> { { "ItemType", "AdditionalFile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "def456" } });

            GenerateMSBuildAnalyzerConfig configTask = new GenerateMSBuildAnalyzerConfig()
            {
                MetadataItems = new[] { item1, item2 }
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true

[c:/file1.cs]
msbuild_item.Compile.ToRetrieve = abc123
msbuild_item.AdditionalFile.ToRetrieve = def456
", result);
        }

        [Fact]
        public void ItemIsMissingRequestedMetadata()
        {
            TaskItem item1 = new TaskItem("c:\\file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" } });

            GenerateMSBuildAnalyzerConfig configTask = new GenerateMSBuildAnalyzerConfig()
            {
                MetadataItems = new[] { item1 }
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true

[c:/file1.cs]
msbuild_item.Compile.ToRetrieve = 
", result);
        }

        [Fact]
        public void ItemIsMissingRequiredMetadata()
        {
            TaskItem item1 = new TaskItem("c:\\file1.cs", new Dictionary<string, string> { });
            TaskItem item2 = new TaskItem("c:\\file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" } });
            TaskItem item3 = new TaskItem("c:\\file1.cs", new Dictionary<string, string> { { "MetadataName", "ToRetrieve" } });

            GenerateMSBuildAnalyzerConfig configTask = new GenerateMSBuildAnalyzerConfig()
            {
                MetadataItems = new[] { item1, item2, item3 }
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true

[c:/file1.cs]
msbuild_item.. = 
msbuild_item.Compile. = 
msbuild_item..ToRetrieve = 
", result);
        }

        [Fact]
        public void PropertiesAreGeneratedBeforeItems()
        {
            TaskItem item1 = new TaskItem("c:\\file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });
            TaskItem item2 = new TaskItem("c:\\file2.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "def456" } });
            TaskItem item3 = new TaskItem("c:\\file3.cs", new Dictionary<string, string> { { "ItemType", "AdditionalFiles" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "ghi789" } });
            TaskItem item4 = new TaskItem("c:\\file1.cs", new Dictionary<string, string> { { "ItemType", "AdditionalFiles" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "jkl012" } });

            TaskItem property1 = new TaskItem("Property1", new Dictionary<string, string> { { "Value", "abc123" } });
            TaskItem property2 = new TaskItem("Property2", new Dictionary<string, string> { { "Value", "def456" } });

            GenerateMSBuildAnalyzerConfig configTask = new GenerateMSBuildAnalyzerConfig()
            {
                MetadataItems = new[] { item1, item2, item3, item4 },
                PropertyItems = new[] { property1, property2 }
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true
msbuild_property.Property1 = abc123
msbuild_property.Property2 = def456

[c:/file1.cs]
msbuild_item.Compile.ToRetrieve = abc123
msbuild_item.AdditionalFiles.ToRetrieve = jkl012

[c:/file2.cs]
msbuild_item.Compile.ToRetrieve = def456

[c:/file3.cs]
msbuild_item.AdditionalFiles.ToRetrieve = ghi789
", result);
        }

        [Fact]
        public void ItemIsNotFullyQualifiedPath()
        {
            TaskItem item1 = new TaskItem("file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });
            TaskItem item2 = new TaskItem("subDir\\file2.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });
            TaskItem item3 = new TaskItem("someDir\\otherDir\\thirdDir\\..\\file3.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });

            GenerateMSBuildAnalyzerConfig configTask = new GenerateMSBuildAnalyzerConfig()
            {
                MetadataItems = new[] { item1, item2, item3 }
            };
            configTask.Execute();
            var result = configTask.ConfigFileContents;


            // MSBuild will convert the above relative paths to absolute paths based on the current location.
            // We replicate that behavior here to test we get the expected full paths 
            string executingLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Replace('\\', '/');
            string expectedPath1 = $"{executingLocation}/file1.cs";
            string expectedPath2 = $"{executingLocation}/subDir/file2.cs";
            string expectedPath3 = $"{executingLocation}/someDir/otherDir/file3.cs";

            Assert.Equal($@"is_global = true

[{expectedPath1}]
msbuild_item.Compile.ToRetrieve = abc123

[{expectedPath2}]
msbuild_item.Compile.ToRetrieve = abc123

[{expectedPath3}]
msbuild_item.Compile.ToRetrieve = abc123
", result);
        }

        [Fact]
        public void ItemsWithDifferentRelativeButSameFullPathAreCombined()
        {
            TaskItem item1 = new TaskItem("c:\\file1.cs", new Dictionary<string, string> { { "ItemType", "Compile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "abc123" } });
            TaskItem item2 = new TaskItem("c:\\someDir\\..\\file1.cs", new Dictionary<string, string> { { "ItemType", "AdditionalFile" }, { "MetadataName", "ToRetrieve" }, { "ToRetrieve", "def456" } });

            GenerateMSBuildAnalyzerConfig configTask = new GenerateMSBuildAnalyzerConfig()
            {
                MetadataItems = new[] { item1, item2 }
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal($@"is_global = true

[c:/file1.cs]
msbuild_item.Compile.ToRetrieve = abc123
msbuild_item.AdditionalFile.ToRetrieve = def456
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

            GenerateMSBuildAnalyzerConfig configTask = new GenerateMSBuildAnalyzerConfig()
            {
                PropertyItems = new[] { property1, property2 }
            };
            configTask.Execute();

            var result = configTask.ConfigFileContents;

            Assert.Equal(@"is_global = true
msbuild_property.Property1 = this is 
a 
property
with  
linebreaks
"" quotation "" marks
and 
property = looking
values

msbuild_property.Property2 = def456
", result);
        }
    }
}
