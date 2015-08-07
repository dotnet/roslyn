// WORKAROUND: Temporary until we can pick up the latest CPS SDK
//
//-----------------------------------------------------------------------
// <copyright file="IItemTypeGuidProvider.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.VisualStudio.ProjectSystem
{
    using System;

    /// <summary>
    /// Provides support to supply an appropriate value for <code>__VSHPROPID.VSHPROPID_TypeGuid</code>
    /// </summary>
    /// <see cref="https://msdn.microsoft.com/en-us/library/microsoft.visualstudio.shell.interop.__vshpropid.aspx"/>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Extension)]
    public interface IItemTypeGuidProvider
    {
        /// <summary>
        /// Identifier for the type of the project hierarchy.
        /// </summary>
        Guid ProjectTypeGuid { get; }
    }
}