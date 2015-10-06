// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Designers
{
    [UnitTestTrait]
    public class ProjectDesignerServiceTests
    {
        [Fact]
        public void Constructor_NullAsProjectVsServices_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("projectVsServices", () => {

                new ProjectDesignerService((IUnconfiguredProjectVsServices)null);
            });
        }

        [Fact]
        public void SupportsProjectDesigner_WhenHierarchyGetPropertyReturnsHResult_ThrowsCOMException()
        {
            var hierarchy = IVsHierarchyFactory.ImplementGetProperty(hr: VSConstants.E_FAIL);
            var projectVsServices = IUnconfiguredProjectVsServicesFactory.ImplementHierarchy(() => hierarchy);

            var designerService = CreateInstance(projectVsServices);

            Assert.Throws<COMException>(() => {

                var result = designerService.SupportsProjectDesigner;
            });
        }

        [Fact]
        public void SupportsProjectDesigner_WhenHierarchyGetPropertyReturnsMemberNotFound_IsFalse()
        {
            var hierarchy = IVsHierarchyFactory.ImplementGetProperty(hr: VSConstants.DISP_E_MEMBERNOTFOUND);
            var projectVsServices = IUnconfiguredProjectVsServicesFactory.ImplementHierarchy(() => hierarchy);

            var designerService = CreateInstance(projectVsServices);

            var result = designerService.SupportsProjectDesigner;

            Assert.False(result);
        }

        [Fact]
        public void SupportsProjectDesigner_ReturnsResultOfHierarchyGetProperty()
        {
            foreach (var value in new[] { true, false })
            {
                var hierarchy = IVsHierarchyFactory.ImplementGetProperty(result: value);
                var projectVsServices = IUnconfiguredProjectVsServicesFactory.ImplementHierarchy(() => hierarchy);

                var designerService = CreateInstance(projectVsServices);

                var result = designerService.SupportsProjectDesigner;

                Assert.Equal(value, result);
            }
        }

        private static ProjectDesignerService CreateInstance(IUnconfiguredProjectVsServices projectVsServices)
        {
            return new ProjectDesignerService(projectVsServices);
        }
    }
}
