// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Designers.Imaging
{
    /// <summary>
    ///     Aggregates <see cref="IProjectImageMonikerProvider"/> instances into a single importable
    ///     <see cref="IProjectImageMonikerProvider"/>.
    /// </summary>
    [Export]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class ProjectImageMonikerProviderAggregator : IProjectImageMonikerProvider
    {
        [ImportingConstructor]
        public ProjectImageMonikerProviderAggregator(UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));

            ImageProviders = new OrderPrecedenceImportCollection<IProjectImageMonikerProvider>(projectCapabilityCheckProvider: unconfiguredProject);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<IProjectImageMonikerProvider> ImageProviders
        {
            get;
        }

        public bool TryGetProjectImageMoniker(string key, out ProjectImageMoniker result)
        {
            foreach (Lazy<IProjectImageMonikerProvider> provider in ImageProviders)
            {
                if (provider.Value.TryGetProjectImageMoniker(key, out result))
                {
                    return true;
                }
            }

            result = default(ProjectImageMoniker);
            return false;
        }
    }
}
