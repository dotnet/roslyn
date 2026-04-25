// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem.Rules;

[Export]
internal partial class RazorProjectProperties : StronglyTypedPropertyAccess
{
    [ImportingConstructor]
    public RazorProjectProperties(ConfiguredProject configuredProject)
        : base(configuredProject)
    {
    }

    public RazorProjectProperties(ConfiguredProject configuredProject, UnconfiguredProject unconfiguredProject)
        : base(configuredProject, unconfiguredProject)
    {
    }

    public RazorProjectProperties(ConfiguredProject configuredProject, IProjectPropertiesContext projectPropertiesContext)
        : base(configuredProject, projectPropertiesContext)
    {
    }

    public RazorProjectProperties(ConfiguredProject configuredProject, string file, string itemType, string itemName)
        : base(configuredProject, file, itemType, itemName)
    {
    }
}
