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
            var tree = ProjectTreeProvider.CreateRoot();

            Assert.Throws<ArgumentNullException>("projectTreeProvider", () => {

                modifier.ApplyModifications(tree, (IProjectTreeProvider)null);
            });
        }

        [Fact]
        public void ApplyModifications2_NullAsTreeProvider_ThrowsArgumentNull()
        {
            var modifier = CreateInstance();
            var tree = ProjectTreeProvider.CreateRoot();

            Assert.Throws<ArgumentNullException>("projectTreeProvider", () => {

                modifier.ApplyModifications(tree, (IProjectTree)null, (IProjectTreeProvider)null);
            });
        }

        [Fact]
        public void ApplyModifications1_RootAsTree_ReturnsUnmodifiedRoot()
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features);

            var tree = ProjectTreeProvider.CreateRoot();

            var result = modifier.ApplyModifications(tree, projectTreeProvider);

            Assert.Same(tree, result);
        }

        [Fact]
        public void ApplyModifications2_RootAsTree_ReturnsUnmodifiedRoot()
        {
            var features = IProjectFeaturesFactory.ImplementSupportsProjectDesigner(() => true);
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var modifier = CreateInstance(features);

            var tree = ProjectTreeProvider.CreateRoot();

            var result = modifier.ApplyModifications(tree, (IProjectTree)null, projectTreeProvider);

            Assert.Same(tree, result);
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
