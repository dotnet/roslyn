// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Designers.Imaging
{
    [ProjectSystemTrait]
    public class ProjectImageProviderAggregatorTests
    {
        [Fact]
        public void Constructor_NullAsUnconfiguredProject_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("unconfiguredProject", () => {

                new ProjectImageProviderAggregator((UnconfiguredProject)null);
            });
        }

        [Fact]
        public void GetImageKey_NullAsKey_ThrowsArgumentNull()
        {
            var aggregator = CreateInstance();

            Assert.Throws<ArgumentNullException>("key", () => {

                aggregator.GetProjectImage((string)null);
            });
        }

        [Fact]
        public void GetImageKey_EmptyAsKey_ThrowsArgument()
        {
            var aggregator = CreateInstance();

            Assert.Throws<ArgumentException>("key", () => {

                aggregator.GetProjectImage(string.Empty);
            });
        }

        [Fact]
        public void GetImageKey_WhenNoImageProviders_ReturnsNull()
        {
            var aggregator = CreateInstance();

            var result = aggregator.GetProjectImage("key");

            Assert.Null(result);
        }

        [Fact]
        public void GetImageKey_SingleImageProviderReturningNull_ReturnsNull()
        {
            var unconfiguredProject = IUnconfiguredProjectFactory.Create(capabilities: new[] { "CSharp" });
            var provider = IProjectImageProviderFactory.ImplementGetProjectImage((key) => null);
            var aggregator = CreateInstance(unconfiguredProject);

            aggregator.ImageProviders.Add(provider, "CSharp");
            
            var result = aggregator.GetProjectImage("key");

            Assert.Null(result);
        }

        [Fact]
        public void GetImageKey_SingleImageProviderReturningKey_ReturnsKey()
        {
            ProjectImageMoniker moniker = new ProjectImageMoniker(Guid.NewGuid(), 0);

            var unconfiguredProject = IUnconfiguredProjectFactory.Create(capabilities: new[] { "CSharp" });
            var provider = IProjectImageProviderFactory.ImplementGetProjectImage((key) => moniker);
            var aggregator = CreateInstance(unconfiguredProject);

            aggregator.ImageProviders.Add(provider, "CSharp");

            var result = aggregator.GetProjectImage("key");

            Assert.Same(moniker, result);
        }

        [Fact]
        public void GetImageKey_ManyImageProviderReturningKey_ReturnsFirstByOrderPrecedence()
        {
            ProjectImageMoniker moniker1 = new ProjectImageMoniker(Guid.NewGuid(), 0);
            ProjectImageMoniker moniker2 = new ProjectImageMoniker(Guid.NewGuid(), 0);

            var unconfiguredProject = IUnconfiguredProjectFactory.Create(capabilities: new[] { "CSharp" });
            var provider1 = IProjectImageProviderFactory.ImplementGetProjectImage((key) => moniker1);
            var provider2 = IProjectImageProviderFactory.ImplementGetProjectImage((key) => moniker2);
            var aggregator = CreateInstance(unconfiguredProject);

            aggregator.ImageProviders.Add(provider2, "CSharp", 0);  // Lowest
            aggregator.ImageProviders.Add(provider1, "CSharp", 10); // Highest

            var result = aggregator.GetProjectImage("key");

            Assert.Same(moniker1, result);
        }

        [Fact]
        public void GetImageKey_ManyImageProviders_ReturnsFirstThatReturnsKey()
        {
            ProjectImageMoniker moniker = new ProjectImageMoniker(Guid.NewGuid(), 0);

            var unconfiguredProject = IUnconfiguredProjectFactory.Create(capabilities: new[] { "CSharp" });
            var provider1 = IProjectImageProviderFactory.ImplementGetProjectImage((key) => null);
            var provider2 = IProjectImageProviderFactory.ImplementGetProjectImage((key) => moniker);
            var aggregator = CreateInstance(unconfiguredProject);

            aggregator.ImageProviders.Add(provider1, "CSharp", 0);
            aggregator.ImageProviders.Add(provider2, "CSharp", 10);

            var result = aggregator.GetProjectImage("key");

            Assert.Same(moniker, result);
        }

        private ProjectImageProviderAggregator CreateInstance(UnconfiguredProject unconfiguredProject = null)
        {
            unconfiguredProject = unconfiguredProject ?? IUnconfiguredProjectFactory.Create();

            return new ProjectImageProviderAggregator(unconfiguredProject);
        }
    }
}
