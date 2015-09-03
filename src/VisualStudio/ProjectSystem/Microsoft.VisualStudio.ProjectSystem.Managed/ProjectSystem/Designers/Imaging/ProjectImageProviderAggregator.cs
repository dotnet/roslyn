// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Designers.Imaging
{
    /// <summary>
    ///     Aggregates <see cref="IProjectImageProvider"/> instances into a single importable
    ///     <see cref="IProjectImageProvider"/>.
    /// </summary>
    [Export]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class ProjectImageProviderAggregator : IProjectImageProvider
    {
        [ImportingConstructor]
        public ProjectImageProviderAggregator(UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));

            ImageProviders = new OrderPrecedenceImportCollection<IProjectImageProvider>(projectCapabilityCheckProvider: unconfiguredProject);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<IProjectImageProvider> ImageProviders
        {
            get;
        }

        public bool TryGetProjectImage(string key, out ProjectImageMoniker result)
        {
            foreach (Lazy<IProjectImageProvider> provider in ImageProviders)
            {
                if (provider.Value.TryGetProjectImage(key, out result))
                {
                    return true;
                }
            }

            result = default(ProjectImageMoniker);
            return false;
        }
    }
}
