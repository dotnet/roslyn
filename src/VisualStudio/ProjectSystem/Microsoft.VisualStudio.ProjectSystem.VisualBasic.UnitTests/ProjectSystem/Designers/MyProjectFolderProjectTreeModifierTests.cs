// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.Designers.Imaging;
using Microsoft.VisualStudio.Testing;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Designers
{
    [UnitTestTrait]
    public class MyProjectFolderProjectTreeModifierTests
    {
        [Fact]
        public void Constructor_NullAsImageProvider_ThrowsArgumentNull()
        {
            var projectServices = IUnconfiguredProjectCommonServicesFactory.Create();

            Assert.Throws<ArgumentNullException>("imageProvider", () => {

                new MyProjectFolderProjectTreeModifier((IProjectImageProvider)null, projectServices);
            });
        }

        [Fact]
        public void Constructor_NullAsProjectServices_ThrowsArgumentNull()
        {
            var imageProvider = IProjectImageProviderFactory.Create();

            Assert.Throws<ArgumentNullException>("projectServices", () => {

                new MyProjectFolderProjectTreeModifier(imageProvider, (IUnconfiguredProjectCommonServices)null);
            });
        }

        [Fact]
        public void ApplyModifications1_NullAsTree_ThrowsArgumentNull()
        {
            var modifier = CreateInstance();
            var projectTreeProvider = IProjectTreeProviderFactory.Create();

            Assert.Throws<ArgumentNullException>("tree", () => {

                modifier.ApplyModifications((IProjectTree)null, projectTreeProvider);
            });
        }

        [Fact]
        public void ApplyModifications2_NullAsTree_ThrowsArgumentNull()
        {
            var modifier = CreateInstance();
            var projectTreeProvider = IProjectTreeProviderFactory.Create();

            Assert.Throws<ArgumentNullException>("tree", () => {

                modifier.ApplyModifications((IProjectTree)null, (IProjectTree)null, projectTreeProvider);
            });
        }

        [Fact]
        public void ApplyModifications1_NullAsTreeProvider_ThrowsArgumentNull()
        {
            var modifier = CreateInstance();
            var tree = ProjectTreeParser.Parse("Root");

            Assert.Throws<ArgumentNullException>("projectTreeProvider", () => {

                modifier.ApplyModifications(tree, (IProjectTreeProvider)null);
            });
        }

        [Fact]
        public void ApplyModifications2_NullAsTreeProvider_ThrowsArgumentNull()
        {
            var modifier = CreateInstance();
            var tree = ProjectTreeParser.Parse("Root");

            Assert.Throws<ArgumentNullException>("projectTreeProvider", () => {

                modifier.ApplyModifications(tree, (IProjectTree)null, (IProjectTreeProvider)null);
            });
        }

        [Fact]
        public void ApplyModifications_TreeWithMyProjectCandidateButSupportsProjectDesignerFalse_ReturnsUnmodifiedTree()
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => false);   // Don't support AppDesigner
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features);

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder})
");

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder})
")]
        public void ApplyModifications_TreeWithPropertiesFolder_ReturnsUnmodifiedTree(string input)
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features);

            var tree = ProjectTreeParser.Parse(input);

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Folder (capabilities: {Folder})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Folder (capabilities: {Folder})
        AssemblyInfo.cs (capabilities: {})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Folder (capabilities: {Folder})
        AssemblyInfo.cs (capabilities: {})
    NotMy Project (capabilities: {Folder})
")]
        public void ApplyModifications_TreeWithoutMyProjectCandidate_ReturnsUnmodifiedTree(string input)
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => false);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features);

            var tree = ProjectTreeParser.Parse(input);

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {NotFolder})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Unrecognized NotAFolder})
")]
        public void ApplyModifications_TreeWithFileCalledMyProject_ReturnsUnmodifiedTree(string input)
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features);

            var tree = ProjectTreeParser.Parse(input);

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder IncludeInProjectCandidate})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {IncludeInProjectCandidate Folder})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {IncludeInProjectCandidate})
")]        
        public void ApplyModifications_TreeWithExcludedMyProjectFolder_ReturnsUnmodifiedTree(string input)
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features);

            var tree = ProjectTreeParser.Parse(input);

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Folder (capabilities: {Folder})
        My Project (capabilities: {Folder})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Folder (capabilities: {Folder})
        Folder (capabilities: {Folder})
            My Project (capabilities: {Folder})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Folder1 (capabilities: {Folder})
    Folder2 (capabilities: {Folder})
        My Project (capabilities: {Folder})
")]        
        public void ApplyModifications_TreeWithNestedMyProjectFolder_ReturnsUnmodifiedTree(string input)
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features);

            var tree = ProjectTreeParser.Parse(input);

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }
        
        [Theory]
        [InlineData(@"
Root(capabilities: {ProjectRoot})
    My Project (capabilities: {Folder AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root(capabilities: {ProjectRoot})
    My Project (capabilities: {Folder AppDesignerFolder})
")]
        [InlineData(@"
Root(capabilities: {ProjectRoot})
    My Project (capabilities: {Folder Unrecognized AppDesignerFolder})
")]
        public void ApplyModifications_TreeWithMyProjectCandidateAlreadyMarkedAsAppDesigner_ReturnsUnmodifiedTree(string input)
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features);

            var tree = ProjectTreeParser.Parse(input);
            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder})
", @"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder BubbleUp})
", @"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    my project (capabilities: {Folder})
", @"
Root (capabilities: {ProjectRoot})
    my project (capabilities: {Folder AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    MY PROJECT (capabilities: {Folder})
", @"
Root (capabilities: {ProjectRoot})
    MY PROJECT (capabilities: {Folder AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder UnrecognizedCapability})
", @"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder UnrecognizedCapability AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder})
        AssemblyInfo.cs (capabilities: {IncludeInProjectCandidate})
", @"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder AppDesignerFolder BubbleUp})
        AssemblyInfo.cs (capabilities: {IncludeInProjectCandidate VisibleOnlyInShowAllFiles})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder})
        AssemblyInfo.cs (capabilities: {IncludeInProjectCandidate VisibleOnlyInShowAllFiles})
", @"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder AppDesignerFolder BubbleUp})
        AssemblyInfo.cs (capabilities: {IncludeInProjectCandidate VisibleOnlyInShowAllFiles})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder})
        AssemblyInfo.cs (capabilities: {})
", @"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder AppDesignerFolder BubbleUp})
        AssemblyInfo.cs (capabilities: {VisibleOnlyInShowAllFiles})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder})
        Folder (capabilities: {Folder})
", @"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder AppDesignerFolder BubbleUp})
        Folder (capabilities: {Folder VisibleOnlyInShowAllFiles})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder})
        Folder (capabilities: {Folder})
            Folder (capabilities: {Folder})
                File (capabilities: {})
", @"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder AppDesignerFolder BubbleUp})
        Folder (capabilities: {Folder VisibleOnlyInShowAllFiles})
            Folder (capabilities: {Folder VisibleOnlyInShowAllFiles})
                File (capabilities: {VisibleOnlyInShowAllFiles})
")]
        public void ApplyModifications_TreeWithMyProjectCandidate_ReturnsCandidateMarkedWithAppDesignerFolderAndBubbleUp(string input, string expected)
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features);

            var inputTree = ProjectTreeParser.Parse(input);
            var expectedTree = ProjectTreeParser.Parse(expected);

            var result = modifier.ApplyModifications(inputTree, projectTreeProvider);

            AssertAreEquivalent(expectedTree, result);
        }

        [Fact]
        public void ApplyModifications_ProjectWithNullMyProjectFolder_DefaultsToMyProject()
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features, appDesignerFolder: null);

            var inputTree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder})
");
            var expectedTree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder AppDesignerFolder BubbleUp})
");

            var result = modifier.ApplyModifications(inputTree, projectTreeProvider);

            AssertAreEquivalent(expectedTree, result);
        }

        [Fact]
        public void ApplyModifications_ProjectWithEmptyMyProjectFolder_DefaultsToMyProject()
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features, appDesignerFolder: "");

            var inputTree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder})
");
            var expectedTree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder AppDesignerFolder BubbleUp})
");

            var result = modifier.ApplyModifications(inputTree, projectTreeProvider);

            AssertAreEquivalent(expectedTree, result);
        }

        [Fact]
        public void ApplyModifications_ProjectWithNonDefaultMyProjectFolder_ReturnsCandidateMarkedWithAppDesignerFolderAndBubbleUp()
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features, appDesignerFolder: "FooBar");

            var inputTree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    FooBar (capabilities: {Folder})
");
            var expectedTree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    FooBar (capabilities: {Folder AppDesignerFolder BubbleUp})
");

            var result = modifier.ApplyModifications(inputTree, projectTreeProvider);

            AssertAreEquivalent(expectedTree, result);
        }

        private void AssertAreEquivalent(IProjectTree expected, IProjectTree actual)
        {
            string expectedAsString = ProjectTreeWriter.WriteToString(expected);
            string actualAsString = ProjectTreeWriter.WriteToString(actual);

            Assert.Equal(expectedAsString, actualAsString);
        }

        private MyProjectFolderProjectTreeModifier CreateInstance()
        {
            return CreateInstance((IProjectImageProvider)null, (IProjectFeatures)null);
        }

        private MyProjectFolderProjectTreeModifier CreateInstance(IProjectFeatures features, string appDesignerFolder = "My Project")
        {
            return CreateInstance((IProjectImageProvider)null, features, appDesignerFolder);
        }

        private MyProjectFolderProjectTreeModifier CreateInstance(IProjectImageProvider imageProvider, IProjectFeatures features, string appDesignerFolder = "My Project")
        {
            var threadingPolicy = IThreadHandlingFactory.Create();
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(unconfiguredProject, 
                new PropertyData() {
                    Category = nameof(ConfigurationGeneral),
                    PropertyName = nameof(ConfigurationGeneral.AppDesignerFolder),
                    Value = appDesignerFolder
                });

            var services = IUnconfiguredProjectCommonServicesFactory.Create(features, threadingPolicy, projectProperties.ConfiguredProject, projectProperties);

            
            return new MyProjectFolderProjectTreeModifier(imageProvider ?? IProjectImageProviderFactory.Create(), services);
        }
    }
}
