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
    }
}
