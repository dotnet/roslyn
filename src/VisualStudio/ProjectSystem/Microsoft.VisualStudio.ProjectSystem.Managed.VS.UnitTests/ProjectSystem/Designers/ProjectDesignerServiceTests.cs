// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Xunit;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.Designers
{
    [ProjectSystemTrait]
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
            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy);

            var designerService = CreateInstance(projectVsServices);

            Assert.Throws<COMException>(() => {

                var result = designerService.SupportsProjectDesigner;
            });
        }

        [Fact]
        public void SupportsProjectDesigner_WhenHierarchyGetPropertyReturnsMemberNotFound_IsFalse()
        {
            var hierarchy = IVsHierarchyFactory.ImplementGetProperty(hr: VSConstants.DISP_E_MEMBERNOTFOUND);
            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy);

            var designerService = CreateInstance(projectVsServices);

            var result = designerService.SupportsProjectDesigner;

            Assert.False(result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SupportsProjectDesigner_ReturnsResultOfHierarchyGetProperty(bool supportsProjectDesigner)
        {
            var hierarchy = IVsHierarchyFactory.ImplementGetProperty(result: supportsProjectDesigner);
            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy);

            var designerService = CreateInstance(projectVsServices);

            var result = designerService.SupportsProjectDesigner;

            Assert.Equal(supportsProjectDesigner, result);
        }

        [Fact]
        public void ShowProjectDesignerAsync_WhenSupportsProjectDesignerFalse_ThrowsInvalidOperation()
        {
            var hierarchy = IVsHierarchyFactory.ImplementGetProperty(result: false);
            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy);

            var designerService = CreateInstance(projectVsServices);

            Assert.Throws<InvalidOperationException>(() => {

                designerService.ShowProjectDesignerAsync();
            });
        }

        [Fact]
        public async Task ShowProjectDesignerAsync_WhenGetGuidPropertyForProjectDesignerEditorReturnsHResult_Throws()
        {
            var hierarchy = IVsHierarchyFactory.Create();
            hierarchy.ImplementGetProperty(VsHierarchyPropID.SupportsProjectDesigner, result: true);
            hierarchy.ImplementGetGuid(VsHierarchyPropID.ProjectDesignerEditor, VSConstants.E_FAIL);

            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy);

            var designerService = CreateInstance(projectVsServices);

#pragma warning disable RS0003 // Do not directly await a Task (see https://github.com/dotnet/roslyn/issues/6770)
            await Assert.ThrowsAsync<COMException>(() => {

                return designerService.ShowProjectDesignerAsync();
            });
#pragma warning restore RS0003 // Do not directly await a Task
        }

        [Fact]
        public async Task ShowProjectDesignerAsync_WhenProjectDesignerEditorReturnsHResult_Throws()
        {
            var hierarchy = IVsHierarchyFactory.Create();
            hierarchy.ImplementGetProperty(VsHierarchyPropID.SupportsProjectDesigner, result: true);
            hierarchy.ImplementGetGuid(VsHierarchyPropID.ProjectDesignerEditor, VSConstants.E_FAIL);

            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy);

            var designerService = CreateInstance(projectVsServices);

#pragma warning disable RS0003 // Do not directly await a Task (see https://github.com/dotnet/roslyn/issues/6770)
            await Assert.ThrowsAsync<COMException>(() => {

                return designerService.ShowProjectDesignerAsync();
            });
#pragma warning restore RS0003 // Do not directly await a Task
        }

        [Fact]
        public async Task ShowProjectDesignerAsync_WhenOpenItemWithSpecificEditorReturnsHResult_Throws()
        {
            Guid editorGuid = Guid.NewGuid();

            var hierarchy = IVsHierarchyFactory.Create();
            hierarchy.ImplementGetProperty(VsHierarchyPropID.SupportsProjectDesigner, result: true);
            hierarchy.ImplementGetGuid(VsHierarchyPropID.ProjectDesignerEditor, result: editorGuid);

            var project = (IVsProject4)hierarchy;
            project.ImplementOpenItemWithSpecific(editorGuid, VSConstants.LOGVIEWID_Primary, VSConstants.E_FAIL);

            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy, ()=> project);

            var designerService = CreateInstance(projectVsServices);

#pragma warning disable RS0003 // Do not directly await a Task (see https://github.com/dotnet/roslyn/issues/6770)
            await Assert.ThrowsAsync<COMException>(() => {

                return designerService.ShowProjectDesignerAsync();
            });
#pragma warning restore RS0003 // Do not directly await a Task
        }

        [Fact]
        public Task ShowProjectDesignerAsync_WhenOpenedInExternalEditor_DoesNotAttemptToShowWindow()
        {   // OpenItemWithSpecific returns null frame when opened in external editor

            Guid editorGuid = Guid.NewGuid();

            var hierarchy = IVsHierarchyFactory.Create();
            hierarchy.ImplementGetProperty(VsHierarchyPropID.SupportsProjectDesigner, result: true);
            hierarchy.ImplementGetGuid(VsHierarchyPropID.ProjectDesignerEditor, result: editorGuid);

            var project = (IVsProject4)hierarchy;
            project.ImplementOpenItemWithSpecific(editorGuid, VSConstants.LOGVIEWID_Primary, (IVsWindowFrame)null);
               
            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy, () => project);

            var designerService = CreateInstance(projectVsServices);

            return designerService.ShowProjectDesignerAsync();
        }

        [Fact]
        public async Task ShowProjectDesignerAsync_WhenWindowShowReturnsHResult_Throws()
        {
            Guid editorGuid = Guid.NewGuid();

            var hierarchy = IVsHierarchyFactory.Create();
            hierarchy.ImplementGetProperty(VsHierarchyPropID.SupportsProjectDesigner, result: true);
            hierarchy.ImplementGetGuid(VsHierarchyPropID.ProjectDesignerEditor, result: editorGuid);
            var project = (IVsProject4)hierarchy;

            var frame = IVsWindowFrameFactory.ImplementShow(() => VSConstants.E_FAIL);
            project.ImplementOpenItemWithSpecific(editorGuid, VSConstants.LOGVIEWID_Primary, frame);

            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy, () => project);

            var designerService = CreateInstance(projectVsServices);

#pragma warning disable RS0003 // Do not directly await a Task (see https://github.com/dotnet/roslyn/issues/6770)
            await Assert.ThrowsAsync<COMException>(() => {

                return designerService.ShowProjectDesignerAsync();
            });
#pragma warning restore RS0003 // Do not directly await a Task
        }

        [Fact]
        public async Task ShowProjectDesignerAsync_WhenOpenedInInternalEditor_ShowsWindow()
        {   
            Guid editorGuid = Guid.NewGuid();
            
            var hierarchy = IVsHierarchyFactory.Create();
            hierarchy.ImplementGetProperty(VsHierarchyPropID.SupportsProjectDesigner, result: true);
            hierarchy.ImplementGetGuid(VsHierarchyPropID.ProjectDesignerEditor, result: editorGuid);
            var project = (IVsProject4)hierarchy;

            int callCount = 0;
            var frame = IVsWindowFrameFactory.ImplementShow(() => { callCount++; return 0; });
            project.ImplementOpenItemWithSpecific(editorGuid, VSConstants.LOGVIEWID_Primary, frame);

            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy, () => project);

            var designerService = CreateInstance(projectVsServices);

#pragma warning disable RS0003 // Do not directly await a Task (see https://github.com/dotnet/roslyn/issues/6770)
            await designerService.ShowProjectDesignerAsync();
#pragma warning restore RS0003 // Do not directly await a Task

            Assert.Equal(1, callCount);
        }

        private static ProjectDesignerService CreateInstance(IUnconfiguredProjectVsServices projectVsServices)
        {
            return new ProjectDesignerService(projectVsServices);
        }
    }
}
