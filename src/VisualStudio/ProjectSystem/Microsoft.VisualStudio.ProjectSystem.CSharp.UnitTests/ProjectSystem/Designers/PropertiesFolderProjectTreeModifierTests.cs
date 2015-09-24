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
            var features = IProjectFeaturesFactory.Create();

            Assert.Throws<ArgumentNullException>("imageProvider", () => {

                new PropertiesFolderProjectTreeModifier((IProjectImageProvider)null, features);
            });
        }

        [Fact]
        public void Constructor_NullAsFeatures_ThrowsArgumentNull()
        {
            var imageProvider = IProjectImageProviderFactory.Create();

            Assert.Throws<ArgumentNullException>("features", () => {

                new PropertiesFolderProjectTreeModifier(imageProvider, (IProjectFeatures)null);
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
        public void ApplyModifications_RootWithZeroChildren_ReturnsUnmodifiedTree()
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features);

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})"
);

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Fact]
        public void ApplyModifications_TreeWithMyProjectFolder_ReturnsUnmodifiedTree()
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features);

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    My Project (capabilities: {Folder})
");

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Fact]
        public void ApplyModifications_TreeWithNormalFolder_ReturnsUnmodifiedTree()
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => false);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features);

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Folder (capabilities: {Folder})
");

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Fact]
        public void ApplyModifications_TreeWithFileCalledProperties_ReturnsUnmodifiedTree()
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features);

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {})
");

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Fact]
        public void ApplyModifications_TreeWithExcludedPropertiesFolder_ReturnsUnmodifiedTree()
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features);

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder IncludeInProjectCandidate})
");

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Fact]
        public void ApplyModifications_TreeWithNestedPropertiesFolder_ReturnsUnmodifiedTree()
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features);

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Parent
        Properties (capabilities: {Folder})
");

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Fact]
        public void ApplyModifications_TreeWithPropertiesCandidateButSupportsProjectDesignerFalse_ReturnsUnmodifiedTree()
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => false);   // Don't support AppDesigner
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features);

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder})
");

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Fact]
        public void ApplyModifications_TreeWithPropertiesCandidateAlreadyMarkedAsAppDesigner_ReturnsUnmodifiedTree()
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features);

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder AppDesignerFolder BubbleUp})
");
            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(tree, result);
        }

        [Fact]
        public void ApplyModifications_TreeWithPropertiesCandidate_ReturnsCandidateMarkedWithAppDesignerFolderAndBubbleUp()
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features);

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder})
");
            var expected = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder AppDesignerFolder BubbleUp})
");
            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(expected, result);
        }

        [Fact]
        public void ApplyModifications_TreeWithPropertiesCandidate_ReturnsCandidateCapabilitiesUnionedWithMarkedWithAppDesignerFolderAndBubbleUp()
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features);

            var tree = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder NonExistentCapability})
");
            var expected = ProjectTreeParser.Parse(@"
Root (capabilities: {ProjectRoot})
    Properties (capabilities: {Folder NonExistentCapability AppDesignerFolder BubbleUp})
");
            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            AssertAreEquivalent(expected, result);
        }

        private void AssertAreEquivalent(IProjectTree expected, IProjectTree actual)
        {
            string expectedAsString = ProjectTreeWriter.WriteToString(expected);
            string actualAsString = ProjectTreeWriter.WriteToString(actual);

            Assert.Equal(expectedAsString, actualAsString);
        }

        private PropertiesFolderProjectTreeModifier CreateInstance()
        {
            return CreateInstance((IProjectImageProvider)null, (IProjectFeatures)null);
        }

        private PropertiesFolderProjectTreeModifier CreateInstance(IProjectFeatures features)
        {
            return CreateInstance((IProjectImageProvider)null, features);
        }

        private PropertiesFolderProjectTreeModifier CreateInstance(IProjectImageProvider imageProvider, IProjectFeatures features)
        {
            return new PropertiesFolderProjectTreeModifier(imageProvider ?? IProjectImageProviderFactory.Create(), features ?? IProjectFeaturesFactory.Create());
        }
    }
}
