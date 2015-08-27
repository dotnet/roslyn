// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Export(typeof(IVsUnconfiguredProjectServices))]
    internal class VsUnconfiguredProjectServices : IVsUnconfiguredProjectServices
    {
        private readonly UnconfiguredProject _unconfiguredProject;

        [ImportingConstructor]
        public VsUnconfiguredProjectServices(UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));

            _unconfiguredProject = unconfiguredProject;
        }

        public IVsHierarchy Hierarchy
        {
            get { return (IVsHierarchy)_unconfiguredProject.Services.HostObject; }
        }

        public IVsProject3 Project
        {
            get { return (IVsProject3)_unconfiguredProject.Services.HostObject; }
        }
    }
}
