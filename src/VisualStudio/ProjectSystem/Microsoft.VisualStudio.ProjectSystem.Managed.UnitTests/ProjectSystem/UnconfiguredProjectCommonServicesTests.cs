// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [UnitTestTrait]
    public class UnconfiguredProjectCommonServicesTests
    {
        [Fact]
        public void Constructor_NullAsFeatures_ThrowsArgumentNull()
        {
            var threadingPolicy = new Lazy<IThreadHandling>(() => IThreadHandlingFactory.Create());

            Assert.Throws<ArgumentNullException>("features", () => {
                new UnconfiguredProjectCommonServices((Lazy<IProjectFeatures>)null, threadingPolicy);
            });
        }

        [Fact]
        public void Constructor_NullAsThreadingPolicy_ThrowsArgumentNull()
        {
            var features = new Lazy<IProjectFeatures>(() => IProjectFeaturesFactory.Create());

            Assert.Throws<ArgumentNullException>("threadingPolicy", () => {
                new UnconfiguredProjectCommonServices(features, (Lazy<IThreadHandling>)null);
            });
        }

        [Fact]
        public void Constructor_ValueAsFeatures_SetsFeaturesProperty()
        {
            var features = new Lazy<IProjectFeatures>(() => IProjectFeaturesFactory.Create());
            var threadingPolicy = new Lazy<IThreadHandling>(() => IThreadHandlingFactory.Create());

            var services = new UnconfiguredProjectCommonServices(features, threadingPolicy);

            Assert.Same(features.Value, services.Features);
        }

        [Fact]
        public void Constructor_ValueAsThreadingPolicy_SetsThreadingPolicyProperty()
        {
            var features = new Lazy<IProjectFeatures>(() => IProjectFeaturesFactory.Create());
            var threadingPolicy = new Lazy<IThreadHandling>(() => IThreadHandlingFactory.Create());

            var services = new UnconfiguredProjectCommonServices(features, threadingPolicy);

            Assert.Same(threadingPolicy.Value, services.ThreadingPolicy);
        }
    }
}
