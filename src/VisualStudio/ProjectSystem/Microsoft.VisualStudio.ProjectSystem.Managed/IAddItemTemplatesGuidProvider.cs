// WORKAROUND: Temporary until we can pick up the latest CPS SDK

//-----------------------------------------------------------------------
// <copyright file="IAddItemTemplatesGuidProvider.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.VisualStudio.ProjectSystem
{
    using System;

    /// <summary>
    /// Provides support to supply an appropriate value for __VSHPROPID2.VSHPROPID_AddItemTemplatesGuid
    /// </summary>
    /// <see cref="https://msdn.microsoft.com/en-us/library/vstudio/microsoft.visualstudio.shell.interop.__vshpropid2.aspx"/>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Extension)]
    public interface IAddItemTemplatesGuidProvider
    {
        /// <summary>
        /// GUID to use to get add item templates.
        /// </summary>
        Guid AddItemTemplatesGuid { get; }
    }
}
