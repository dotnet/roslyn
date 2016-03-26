// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.Designers
{
    /// <summary>
    ///     Provides an implementation of <see cref="IProjectDesignerService"/> based on Visual Studio services.
    /// </summary>
    [Export(typeof(IProjectDesignerService))]
    internal class ProjectDesignerService : IProjectDesignerService
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;

        [ImportingConstructor]
        public ProjectDesignerService(IUnconfiguredProjectVsServices projectVsServices)
        {
            Requires.NotNull(projectVsServices, nameof(projectVsServices));

            _projectVsServices = projectVsServices;
        }

        public bool SupportsProjectDesigner
        {
            get { return _projectVsServices.Hierarchy.GetProperty(VsHierarchyPropID.SupportsProjectDesigner, defaultValue: false); }
        }

        public Task ShowProjectDesignerAsync()
        {
            if (SupportsProjectDesigner)
            {
                return OpenProjectDesignerAsyncCore();
            }

            throw new InvalidOperationException("This project does not support the Project Designer (SupportsProjectDesigner is false).");
        }

        private async Task OpenProjectDesignerAsyncCore()
        {
            Guid projectDesignerGuid = _projectVsServices.Hierarchy.GetGuidProperty(VsHierarchyPropID.ProjectDesignerEditor);

            IVsWindowFrame frame = _projectVsServices.Project.OpenItemWithSpecific(HierarchyId.Root, projectDesignerGuid);
            if (frame != null)
            {   // Opened within Visual Studio

                // Can only use Shell APIs on the UI thread
                await _projectVsServices.ThreadingPolicy.SwitchToUIThread();

                HResult hr = frame.Show();
                if (hr.Failed)
                    throw hr.Exception;
            }
        }
    }
}
