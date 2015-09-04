// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides a default implementation of <see cref="IUnconfiguredProjectCommonServices"/>.
    /// </summary>
    [Export(typeof(IUnconfiguredProjectCommonServices))]
    internal class UnconfiguredProjectCommonServices : IUnconfiguredProjectCommonServices
    {
        [ImportingConstructor]
        public UnconfiguredProjectCommonServices(IProjectFeatures features, IThreadHandling threadHandling)
        {
            Requires.NotNull(features, nameof(features));
            Requires.NotNull(threadHandling, nameof(threadHandling));

            Features = features;
            ThreadingPolicy = threadHandling;
        }

        public IProjectFeatures Features
        {
            get;
        }

        public IThreadHandling ThreadingPolicy
        {
            get;
        }
    }
}
