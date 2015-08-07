//-----------------------------------------------------------------------
// <copyright file="CSProjXmlProjectCapabilityProvider.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.VisualStudio.ProjectSystem.CSharp.Implementation
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.ProjectSystem.Utilities;

    /// <summary>
    /// Adds the "CSharp" and other capabilities to projects that import *.CSharp.targets.
    /// </summary>
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectCapabilitiesProvider))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal class CSProjXmlProjectCapabilityProvider : ProjectCapabilitiesFromImportXmlProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CSProjXmlProjectCapabilityProvider"/> class.
        /// </summary>
        public CSProjXmlProjectCapabilityProvider()
            : base("CSharp.targets", ProjectCapabilities.CSharp)
        {
        }
    }
}
