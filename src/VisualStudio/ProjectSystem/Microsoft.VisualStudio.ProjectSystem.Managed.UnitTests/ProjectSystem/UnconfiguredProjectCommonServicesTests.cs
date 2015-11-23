// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [UnitTestTrait]
    public class UnconfiguredProjectCommonServicesTests
    {
        [Fact]
        public void Constructor_NullAsThreadingPolicy_ThrowsArgumentNull()
        {
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(unconfiguredProject);
            var activeConfiguredProject = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            Assert.Throws<ArgumentNullException>("threadingPolicy", () => {
                new UnconfiguredProjectCommonServices((Lazy<IThreadHandling>)null, activeConfiguredProject, activeConfiguredProjectProperties);
            });
        }

        [Fact]
        public void Constructor_NullAsActiveConfiguredProject_ThrowsArgumentNull()
        {
            var threadingPolicy = new Lazy<IThreadHandling>(() => IThreadHandlingFactory.Create());
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(unconfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            Assert.Throws<ArgumentNullException>("activeConfiguredProject", () => {
                new UnconfiguredProjectCommonServices(threadingPolicy, (ActiveConfiguredProject<ConfiguredProject>)null, activeConfiguredProjectProperties);
            });
        }

        [Fact]
        public void Constructor_NullAsActiveConfguredProjectProperties_ThrowsArgumentNull()
        {
            var threadingPolicy = new Lazy<IThreadHandling>(() => IThreadHandlingFactory.Create());
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(unconfiguredProject);
            var activeConfiguredProject = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);

            Assert.Throws<ArgumentNullException>("activeConfiguredProjectProperties", () => {
                new UnconfiguredProjectCommonServices(threadingPolicy, activeConfiguredProject, (ActiveConfiguredProject<ProjectProperties>)null);
            });
        }

        [Fact]
        public void Constructor_ValueAsThreadingPolicy_SetsThreadingPolicyProperty()
        {
            var threadingPolicy = new Lazy<IThreadHandling>(() => IThreadHandlingFactory.Create());
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(unconfiguredProject);
            var activeConfiguredProject = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            var services = new UnconfiguredProjectCommonServices(threadingPolicy, activeConfiguredProject, activeConfiguredProjectProperties);

            Assert.Same(threadingPolicy.Value, services.ThreadingPolicy);
        }

        [Fact]
        public void Constructor_ValueAsActiveConfiguredProject_SetsActiveConfiguredProjectProperty()
        {
            var threadingPolicy = new Lazy<IThreadHandling>(() => IThreadHandlingFactory.Create());
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(unconfiguredProject);
            var activeConfiguredProject = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            var services = new UnconfiguredProjectCommonServices(threadingPolicy, activeConfiguredProject, activeConfiguredProjectProperties);

            Assert.Same(projectProperties.ConfiguredProject, services.ActiveConfiguredProject);
        }

        [Fact]
        public void Constructor_ValueAsActiveConfiguredProjectProperties_SetsActiveConfiguredProjectPropertiesProperty()
        {
            var threadingPolicy = new Lazy<IThreadHandling>(() => IThreadHandlingFactory.Create());
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(unconfiguredProject);
            var activeConfiguredProject = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            var services = new UnconfiguredProjectCommonServices(threadingPolicy, activeConfiguredProject, activeConfiguredProjectProperties);

            Assert.Same(projectProperties, services.ActiveConfiguredProjectProperties);
        }
    }
}
