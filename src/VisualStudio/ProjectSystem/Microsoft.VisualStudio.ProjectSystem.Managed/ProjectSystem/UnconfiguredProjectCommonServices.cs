// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides a default implementation of <see cref="IUnconfiguredProjectCommonServices"/>.
    /// </summary>
    [Export(typeof(IUnconfiguredProjectCommonServices))]
    internal class UnconfiguredProjectCommonServices : IUnconfiguredProjectCommonServices
    {
        private readonly Lazy<IProjectFeatures> _features;
        private readonly Lazy<IThreadHandling> _threadingPolicy;

        [ImportingConstructor]
        public UnconfiguredProjectCommonServices(Lazy<IProjectFeatures> features, Lazy<IThreadHandling> threadingPolicy)
        {
            Requires.NotNull(features, nameof(features));
            Requires.NotNull(threadingPolicy, nameof(threadingPolicy));

            _features = features;
            _threadingPolicy = threadingPolicy;
        }

        public IProjectFeatures Features
        {
            get { return _features.Value; }
        }

        public IThreadHandling ThreadingPolicy
        {
            get { return _threadingPolicy.Value; }
        }
    }
}
