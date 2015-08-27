// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides access to common Visual Studio project services.
    /// </summary>
    internal interface IVsUnconfiguredProjectServices
    {
        IVsHierarchy Hierarchy
        {
            get;
        }

        IVsProject3 Project
        {
            get;
        }
    }
}
