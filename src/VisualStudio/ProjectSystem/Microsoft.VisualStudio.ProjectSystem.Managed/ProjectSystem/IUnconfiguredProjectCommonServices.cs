// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides access to common project services provided by the <see cref="UnconfiguredProject"/>.
    /// </summary>
    internal interface IUnconfiguredProjectCommonServices
    {
        /// <summary>
        ///     Gets the <see cref="IProjectFeatures"/> for the current <see cref="UnconfiguredProject"/>.
        /// </summary>
        IProjectFeatures Features
        {
            get;
        }

        /// <summary>
        ///     Gets the <see cref="IThreadHandling"/> for the current <see cref="UnconfiguredProject"/>.
        /// </summary>
        IThreadHandling ThreadingPolicy
        {
            get;
        }
    }
}
