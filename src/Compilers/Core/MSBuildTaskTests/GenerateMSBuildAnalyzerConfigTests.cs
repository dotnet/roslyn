// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
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

[c:\file1.cs]
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

[c:\file1.cs]
msbuild_item.Compile.ToRetrieve = abc123

[c:\file2.cs]
msbuild_item.Compile.ToRetrieve = def456

[c:\file3.cs]
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

[c:\file1.cs]
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

[c:\file1.cs]
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

[c:\file1.cs]
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

[c:\file1.cs]
msbuild_item.Compile.ToRetrieve = abc123
msbuild_item.AdditionalFiles.ToRetrieve = jkl012

[c:\file2.cs]
msbuild_item.Compile.ToRetrieve = def456

[c:\file3.cs]
msbuild_item.AdditionalFiles.ToRetrieve = ghi789
", result);
        }

    }
}
