// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.CSharp
{
    /// <summary>
    /// Adds the "CSharp" and other capabilities to projects that import *.CSharp.targets.
    /// </summary>
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectCapabilitiesProvider))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal class CSProjXmlProjectCapabilityProvider : ProjectCapabilitiesFromImportXmlProvider
    {
        [ImportingConstructor]
        public CSProjXmlProjectCapabilityProvider()
            : base("CSharp.targets", ProjectCapabilities.CSharp)
        {
        }
    }
}
