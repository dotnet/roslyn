// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.Designers.Imaging;
using Microsoft.VisualStudio.Testing;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Designers
{
    [ProjectSystemTrait]
    public class ProjectRootImageProjectTreeModifierTests
    {
        [Fact]
        public void Constructor_NullAsImageProvider_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("imageProvider", () => {
                new ProjectRootImageProjectTreeModifier((IProjectImageProvider)null);
            });
        }

        [Theory]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot Unrecognized})
")]
        [InlineData(@"
Root (capabilities: {Unrecognized ProjectRoot})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Folder (capabilities: {Folder})
")]
        public void ApplyModifications_ProjectRootAsTree_SetsIconToProjectRoot(string input)
        {
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var imageProvider = IProjectImageProviderFactory.ImplementGetProjectImage(ProjectImageKey.ProjectRoot, new ProjectImageMoniker(new Guid("{A140CD9F-FF94-483C-87B1-9EF5BE9F469A}"), 1));

            var modifier = CreateInstance(imageProvider);

            var tree = ProjectTreeParser.Parse(input);
            var result = modifier.ApplyModifications(tree, (IProjectTree)null, projectTreeProvider);

            Assert.Equal(new ProjectImageMoniker(new Guid("{A140CD9F-FF94-483C-87B1-9EF5BE9F469A}"), 1), tree.Icon);
        }

        [Theory]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    File (capabilities: {})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    File (capabilities: {IncludeInProjectCandidate})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Folder (capabilities: {Folder})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
    Folder (capabilities: {Folder IncludeInProjectCandidate})
")]
        public void ApplyModifications_NonProjectRootAsTree_DoesNotSetIcon(string input)
        {
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var imageProvider = IProjectImageProviderFactory.ImplementGetProjectImage(ProjectImageKey.ProjectRoot, new ProjectImageMoniker(new Guid("{A140CD9F-FF94-483C-87B1-9EF5BE9F469A}"), 1));

            var modifier = CreateInstance(imageProvider);

            var tree = ProjectTreeParser.Parse(input);
            var result = modifier.ApplyModifications(tree.Children[0], (IProjectTree)null, projectTreeProvider);

            Assert.Null(tree.Icon);
        }

        [Theory]
        [InlineData(@"
Root (capabilities: {ProjectRoot})
")]
        [InlineData(@"
Root (capabilities: {ProjectRoot Unrecognized})
")]
        [InlineData(@"
Root (capabilities: {Unrecognized ProjectRoot})
")]
        public void ApplyModifications_ProjectRootAsTreeWhenPreviousTreeSpecified_DoesNotSetIcon(string input)
        {
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var imageProvider = IProjectImageProviderFactory.ImplementGetProjectImage(ProjectImageKey.ProjectRoot, new ProjectImageMoniker(new Guid("{A140CD9F-FF94-483C-87B1-9EF5BE9F469A}"), 1));

            var modifier = CreateInstance(imageProvider);

            var tree = ProjectTreeParser.Parse(input);
            var result = modifier.ApplyModifications(tree, tree, projectTreeProvider);

            Assert.Null(tree.Icon);
        }

        [Fact]
        public void ApplyModifications_ProjectRootAsTreeWhenImageProviderReturnsNull_DoesNotSetIcon()
        {
            var projectTreeProvider = IProjectTreeProviderFactory.Create();
            var imageProvider = IProjectImageProviderFactory.ImplementGetProjectImage((string key) => null);

            var modifier = CreateInstance(imageProvider);

            var icon = new ProjectImageMoniker(new Guid("{A140CD9F-FF94-483C-87B1-9EF5BE9F469A}"), 1);
            var tree = ProjectTreeParser.Parse("Root (capabilities: {ProjectRoot})");

            tree = tree.SetIcon(icon);

            var result = modifier.ApplyModifications(tree, (IProjectTree)null, projectTreeProvider);

            Assert.Same(icon, tree.Icon);
        }

        private ProjectRootImageProjectTreeModifier CreateInstance(IProjectImageProvider imageProvider)
        {
            return new ProjectRootImageProjectTreeModifier(imageProvider);
        }
    }
}
