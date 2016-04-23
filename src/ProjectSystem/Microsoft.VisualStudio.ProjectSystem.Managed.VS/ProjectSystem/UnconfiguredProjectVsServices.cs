// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides an implementation of <see cref="IUnconfiguredProjectVsServices"/> that delegates onto 
    ///     it's <see cref="IUnconfiguredProjectServices.HostObject"/> and underlying <see cref="IUnconfiguredProjectCommonServices"/>.
    /// </summary>
    [Export(typeof(IUnconfiguredProjectVsServices))]
    internal class UnconfiguredProjectVsServices : IUnconfiguredProjectVsServices
    {
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IUnconfiguredProjectCommonServices _commonServices;

        [ImportingConstructor]
        public UnconfiguredProjectVsServices(UnconfiguredProject unconfiguredProject, IUnconfiguredProjectCommonServices commonServices)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));
            Requires.NotNull(commonServices, nameof(commonServices));

            _unconfiguredProject = unconfiguredProject;
            _commonServices = commonServices;
        }

        public IVsHierarchy Hierarchy
        {
            get { return (IVsHierarchy)_unconfiguredProject.Services.HostObject; }
        }

        public IVsProject4 Project
        {
            get { return (IVsProject4)_unconfiguredProject.Services.HostObject; }
        }

        public IThreadHandling ThreadingPolicy
        {
            get { return _commonServices.ThreadingPolicy; }
        }

        public ConfiguredProject ActiveConfiguredProject
        {
            get { return _commonServices.ActiveConfiguredProject; }
        }

        public ProjectProperties ActiveConfiguredProjectProperties
        {
            get { return _commonServices.ActiveConfiguredProjectProperties; }
        }
    }
}
