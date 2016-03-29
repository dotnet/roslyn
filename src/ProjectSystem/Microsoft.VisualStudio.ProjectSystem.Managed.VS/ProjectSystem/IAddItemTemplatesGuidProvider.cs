// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    /// Provides support to supply an appropriate value for __VSHPROPID2.VSHPROPID_AddItemTemplatesGuid
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Extension)]
    public interface IAddItemTemplatesGuidProvider
    {
        /// <summary>
        /// GUID to use to get add item templates.
        /// </summary>
        Guid AddItemTemplatesGuid { get; }
    }
}
