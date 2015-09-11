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
        public UnconfiguredProjectCommonServices(IProjectFeatures features, IThreadHandling threadingPolicy)
        {
            Requires.NotNull(features, nameof(features));
            Requires.NotNull(threadingPolicy, nameof(threadingPolicy));

            Features = features;
            ThreadingPolicy = threadingPolicy;
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
