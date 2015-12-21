// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides access to common Visual Studio project services provided by the <see cref="UnconfiguredProject"/>.
    /// </summary>
    internal interface IUnconfiguredProjectVsServices : IUnconfiguredProjectCommonServices
    {
        /// <summary>
        ///     Gets <see cref="IVsHierarchy"/> provided by the <see cref="UnconfiguredProject"/>.
        /// </summary>
        IVsHierarchy Hierarchy
        {
            get;
        }

        /// <summary>
        ///     Gets <see cref="IVsProject4"/> provided by the <see cref="UnconfiguredProject"/>.
        /// </summary>
        IVsProject4 Project
        {
            get;
        }
    }
}
