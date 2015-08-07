//-----------------------------------------------------------------------
// <copyright file="VBProjXmlProjectCapabilityProvider.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.VisualStudio.ProjectSystem.VB.Implementation
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.ProjectSystem.Utilities;

    /// <summary>
    /// Adds the "VB" capability to projects that import Microsoft.VisualBasic.targets.
    /// </summary>
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectCapabilitiesProvider))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal class VBProjXmlProjectCapabilityProvider : ProjectCapabilitiesFromImportXmlProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VBProjXmlProjectCapabilityProvider"/> class.
        /// </summary>
        public VBProjXmlProjectCapabilityProvider()
            : base("VisualBasic.targets", ProjectCapabilities.VB)
        {
        }
    }
}
