// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


// WORKAROUND: Temporary until we can pick up the latest CPS SDK
using System;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    /// Provides support to supply an appropriate value for <code>__VSHPROPID.VSHPROPID_TypeGuid</code>
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Extension)]
    public interface IItemTypeGuidProvider
    {
        /// <summary>
        /// Identifier for the type of the project hierarchy.
        /// </summary>
        Guid ProjectTypeGuid { get; }
    }
}