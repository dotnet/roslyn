// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Capabilities
{
    /// <summary>
    /// Adds the "CSharp" and other capabilities to projects that import *.CSharp.targets.
    /// </summary>
    [ExcludeFromCodeCoverage] // Temporary class until we start reading capabilities from targets
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectCapabilitiesProvider))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal class CSharpProjectXmlProjectCapabilityProvider : ProjectCapabilitiesFromImportXmlProvider
    {
        [ImportingConstructor]
        public CSharpProjectXmlProjectCapabilityProvider()
            : base("CSharp.targets", ProjectCapabilities.CSharp)
        {
        }
    }
}
