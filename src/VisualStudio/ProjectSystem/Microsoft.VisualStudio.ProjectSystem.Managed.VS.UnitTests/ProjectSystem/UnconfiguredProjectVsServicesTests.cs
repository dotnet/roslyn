// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [ProjectSystemTrait]
    public class UnconfiguredProjectVsServicesTests
    {
        [Fact]
        public void Constructor_NullAsUnconfiguedProject_ThrowsArgumentNull()
        {
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create();

            Assert.Throws<ArgumentNullException>("unconfiguredProject", () => {
                new UnconfiguredProjectVsServices((UnconfiguredProject)null, commonServices);
            });
        }

        [Fact]
        public void Constructor_NullAsCommonSevices_ThrowsArgumentNull()
        {
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();

            Assert.Throws<ArgumentNullException>("commonServices", () => {
                new UnconfiguredProjectVsServices(unconfiguredProject, (IUnconfiguredProjectCommonServices)null);
            });
        }

        [Fact]
        public void Constructor_ValueAsUnconfiguedProject_SetsHierarchyToHostObject()
        {
            var hierarchy = IVsHierarchyFactory.Create();
            var unconfiguredProject = IUnconfiguredProjectFactory.Create(hostObject:hierarchy);
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create();

            var vsServices = CreateInstance(unconfiguredProject, commonServices);

            Assert.Same(hierarchy, vsServices.Hierarchy);
        }

        [Fact]
        public void Constructor_ValueAsUnconfiguedProject_SetsProjectToHostObject()
        {
            var hierarchy = IVsHierarchyFactory.Create();
            var unconfiguredProject = IUnconfiguredProjectFactory.Create(hostObject: hierarchy);
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create();

            var vsServices = CreateInstance(unconfiguredProject, commonServices);

            Assert.Same(hierarchy, vsServices.Project);
        }

        [Fact]
        public void Constructor_ValueAsCommonServices_SetsThreadingPolicyToCommonServicesThreadingPolicy()
        {
            var threadingPolicy = IThreadHandlingFactory.Create();
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(threadingPolicy: threadingPolicy);

            var vsServices = CreateInstance(unconfiguredProject, commonServices);

            Assert.Same(threadingPolicy, vsServices.ThreadingPolicy);
        }

        [Fact]
        public void Constructor_ValueAsCommonServices_SetsActiveConfiguredProjectProjectToCommonServicesActiveConfiguredProject()
        {
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(unconfiguredProject);
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(configuredProject: projectProperties.ConfiguredProject);

            var vsServices = CreateInstance(unconfiguredProject, commonServices);

            Assert.Same(projectProperties.ConfiguredProject, vsServices.ActiveConfiguredProject);
        }

        [Fact]
        public void Constructor_ValueAsCommonServices_SetsActiveConfiguredProjectPropertiesToCommonServicesActiveConfiguredProjectProperties()
        {
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(unconfiguredProject);
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(projectProperties: projectProperties);

            var vsServices = CreateInstance(unconfiguredProject, commonServices);

            Assert.Same(projectProperties, vsServices.ActiveConfiguredProjectProperties);
        }

        private static UnconfiguredProjectVsServices CreateInstance(UnconfiguredProject unconfiguredProject, IUnconfiguredProjectCommonServices commonServices)
        {
            return new UnconfiguredProjectVsServices(unconfiguredProject, commonServices);
        }
    }
}
