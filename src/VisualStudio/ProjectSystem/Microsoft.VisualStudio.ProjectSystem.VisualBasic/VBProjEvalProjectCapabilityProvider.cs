//-----------------------------------------------------------------------
// <copyright file="VBProjEvalProjectCapabilityProvider.cs" company="Microsoft">
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
    [Export(ExportContractNames.Scopes.ConfiguredProject, typeof(IProjectCapabilitiesProvider))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal class VBProjEvalProjectCapabilityProvider : ProjectCapabilitiesFromImportEvaluationProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VBProjEvalProjectCapabilityProvider"/> class.
        /// </summary>
        public VBProjEvalProjectCapabilityProvider()
            : base(@"VisualBasic.targets", ProjectCapabilities.VB)
        {
        }
    }
}
