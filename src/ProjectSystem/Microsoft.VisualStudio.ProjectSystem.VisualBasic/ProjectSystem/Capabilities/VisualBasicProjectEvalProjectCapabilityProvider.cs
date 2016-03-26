// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Capabilities
{
    /// <summary>
    /// Adds the "VB" capability to projects that import Microsoft.VisualBasic.targets.
    /// </summary>
    [ExcludeFromCodeCoverage] // Temporary class until we start reading capabilities from targets
    [Export(ExportContractNames.Scopes.ConfiguredProject, typeof(IProjectCapabilitiesProvider))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal class VisualBasicProjectEvalProjectCapabilityProvider : ProjectCapabilitiesFromImportEvaluationProvider
    {
        [ImportingConstructor]
        public VisualBasicProjectEvalProjectCapabilityProvider()
            : base(@"VisualBasic.targets", ProjectCapabilities.VB)
        {
        }
    }
}
