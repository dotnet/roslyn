// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VisualBasic
{
    /// <summary>
    /// Adds the "VB" capability to projects that import Microsoft.VisualBasic.targets.
    /// </summary>
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectCapabilitiesProvider))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal class VisualBasicProjectXmlProjectCapabilityProvider : ProjectCapabilitiesFromImportXmlProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VisualBasicProjectXmlProjectCapabilityProvider"/> class.
        /// </summary>
        [ImportingConstructor]
        public VisualBasicProjectXmlProjectCapabilityProvider()
            : base("VisualBasic.targets", ProjectCapabilities.VB)
        {
        }
    }
}
