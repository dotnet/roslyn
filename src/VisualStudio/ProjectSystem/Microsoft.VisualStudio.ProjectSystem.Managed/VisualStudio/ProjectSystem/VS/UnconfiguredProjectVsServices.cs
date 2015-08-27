// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides an implementation of <see cref="IUnconfiguredProjectVsServices"/> that delegates onto 
    ///     it's <see cref="IUnconfiguredProjectServices.HostObject"/>.
    /// </summary>
    [Export(typeof(IUnconfiguredProjectVsServices))]
    internal class UnconfiguredProjectVsServices : IUnconfiguredProjectVsServices
    {
        private readonly UnconfiguredProject _unconfiguredProject;

        [ImportingConstructor]
        public UnconfiguredProjectVsServices(UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));

            _unconfiguredProject = unconfiguredProject;
        }

        public IVsHierarchy Hierarchy
        {
            get { return (IVsHierarchy)_unconfiguredProject.Services.HostObject; }
        }

        public IVsProject4 Project
        {
            get { return (IVsProject4)_unconfiguredProject.Services.HostObject; }
        }
    }
}
