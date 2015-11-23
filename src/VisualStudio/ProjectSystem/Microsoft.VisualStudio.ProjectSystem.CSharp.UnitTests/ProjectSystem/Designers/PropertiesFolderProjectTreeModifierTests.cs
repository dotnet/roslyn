// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.Designers.Imaging;
using Microsoft.VisualStudio.Testing;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Designers
{
    [UnitTestTrait]
    public class PropertiesFolderProjectTreeModifierTests
    {
        [Fact]
        public void Constructor_NullAsImageProvider_ThrowsArgumentNull()
        {
            var projectServices = IUnconfiguredProjectCommonServicesFactory.Create();
            var designerService = IProjectDesignerServiceFactory.Create();

            Assert.Throws<ArgumentNullException>("imageProvider", () => {

                new PropertiesFolderProjectTreeModifier((IProjectImageProvider)null, projectServices, designerService);
            });
        }

        [Fact]
        public void Constructor_NullAsProjectServices_ThrowsArgumentNull()
        {
            var imageProvider = IProjectImageProviderFactory.Create();
            var designerService = IProjectDesignerServiceFactory.Create();

            Assert.Throws<ArgumentNullException>("projectServices", () => {

                new PropertiesFolderProjectTreeModifier(imageProvider, (IUnconfiguredProjectCommonServices)null, designerService);
            });
        }


        [Fact]
        public void Constructor_NullAsDesignerService_ThrowsArgumentNull()
        {
            var projectServices = IUnconfiguredProjectCommonServicesFactory.Create();
            var imageProvider = IProjectImageProviderFactory.Create();

            Assert.Throws<ArgumentNullException>("designerService", () => {

                new PropertiesFolderProjectTreeModifier(imageProvider, projectServices, (IProjectDesignerService)null);
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
        public void ApplyModifications_TreeWithPropertiesCandidateButSupportsProjectDesignerFalse_ReturnsUnmodifiedTree()
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => false);   // Don't support AppDesigner
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder})
");

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder})
")]
        public void ApplyModifications_TreeWithMyProjectFolder_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService);

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
    NotProperties (capabilities: {Folder})
")]
        public void ApplyModifications_TreeWithoutPropertiesCandidate_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => false);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {NotFolder})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Unrecognized NotAFolder})
")]
        public void ApplyModifications_TreeWithFileCalledProperties_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder IncludeInProjectCandidate})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {IncludeInProjectCandidate Folder})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {IncludeInProjectCandidate})
")]        
        public void ApplyModifications_TreeWithExcludedPropertiesFolder_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Folder (capabilities: {Folder})
        Properties (capabilities: {Folder})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Folder (capabilities: {Folder})
        Folder (capabilities: {Folder})
            Properties (capabilities: {Folder})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Folder1 (capabilities: {Folder})
    Folder2 (capabilities: {Folder})
        Properties (capabilities: {Folder})
")]        
        public void ApplyModifications_TreeWithNestedPropertiesFolder_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }
        
        [Theory]
        [InlineData(@"
Root(capabilities: {ProjectRoot})
    Properties (capabilities: {Folder AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root(capabilities: {ProjectRoot})
    Properties (capabilities: {Folder AppDesignerFolder})
")]
        [InlineData(@"
Root(capabilities: {ProjectRoot})
    Properties (capabilities: {Folder Unrecognized AppDesignerFolder})
")]
        public void ApplyModifications_TreeWithPropertiesCandidateAlreadyMarkedAsAppDesigner_ReturnsUnmodifiedTree(string input)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(input);
            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Theory]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder})
", @"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder BubbleUp})
", @"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    properties (capabilities: {Folder})
", @"
Root (capabilities: {ProjectRoot})
    properties (capabilities: {Folder AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    PROPERTIES (capabilities: {Folder})
", @"
Root (capabilities: {ProjectRoot})
    PROPERTIES (capabilities: {Folder AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder UnrecognizedCapability})
", @"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder UnrecognizedCapability AppDesignerFolder BubbleUp})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder})
        AssemblyInfo.cs (capabilities: {IncludeInProjectCandidate})
", @"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder AppDesignerFolder BubbleUp})
        AssemblyInfo.cs (capabilities: {IncludeInProjectCandidate})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder})
        AssemblyInfo.cs (capabilities: {})
", @"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder AppDesignerFolder BubbleUp})
        AssemblyInfo.cs (capabilities: {})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder})
        Folder (capabilities: {Folder})
", @"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder AppDesignerFolder BubbleUp})
        Folder (capabilities: {Folder})
")]
        public void ApplyModifications_TreeWithPropertiesCandidate_ReturnsCandidateMarkedWithAppDesignerFolderAndBubbleUp(string input, string expected)
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService);

            var inputTree = ProjectTreeParser.Parse(input);
            var expectedTree = ProjectTreeParser.Parse(expected);

            var result = modifier.ApplyModifications(inputTree, projectTreeProvider);

            AssertAreEquivalent(expectedTree, result);
        }

        [Fact]
        public void ApplyModifications_ProjectWithNullPropertiesFolder_DefaultsToProperties()
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService, appDesignerFolder: null);

            var inputTree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder})
");
            var expectedTree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder AppDesignerFolder BubbleUp})
");

            var result = modifier.ApplyModifications(inputTree, projectTreeProvider);

            AssertAreEquivalent(expectedTree, result);
        }

        [Fact]
        public void ApplyModifications_ProjectWithEmptyPropertiesFolder_DefaultsToProperties()
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService, appDesignerFolder: "");

            var inputTree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder})
");
            var expectedTree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder AppDesignerFolder BubbleUp})
");

            var result = modifier.ApplyModifications(inputTree, projectTreeProvider);

            AssertAreEquivalent(expectedTree, result);
        }

        [Fact]
        public void ApplyModifications_ProjectWithNonDefaultPropertiesFolder_ReturnsCandidateMarkedWithAppDesignerFolderAndBubbleUp()
        {
            var designerService = IProjectDesignerServiceFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(designerService, appDesignerFolder: "FooBar");

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

        private PropertiesFolderProjectTreeModifier CreateInstance()
        {
            return CreateInstance((IProjectImageProvider)null, (IProjectDesignerService)null);
        }

        private PropertiesFolderProjectTreeModifier CreateInstance(IProjectDesignerService designerService, string appDesignerFolder = "Properties")
        {
            return CreateInstance((IProjectImageProvider)null, designerService, appDesignerFolder);
        }

        private PropertiesFolderProjectTreeModifier CreateInstance(IProjectImageProvider imageProvider, IProjectDesignerService designerService, string appDesignerFolder = "Properties")
        {
            designerService = designerService ?? IProjectDesignerServiceFactory.Create();
            var threadingPolicy = IThreadHandlingFactory.Create();
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(unconfiguredProject,
                new PropertyPageData() {
                    Category = nameof(ConfigurationGeneral),
                    PropertyName = nameof(ConfigurationGeneral.AppDesignerFolder),
                    Value = appDesignerFolder,
                });

            var services = IUnconfiguredProjectCommonServicesFactory.Create(threadingPolicy, projectProperties.ConfiguredProject, projectProperties);

            return new PropertiesFolderProjectTreeModifier(imageProvider ?? IProjectImageProviderFactory.Create(), services, designerService);
        }
    }
}
