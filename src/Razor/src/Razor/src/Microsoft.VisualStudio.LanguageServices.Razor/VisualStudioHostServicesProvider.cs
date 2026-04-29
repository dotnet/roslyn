// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices;

namespace Microsoft.VisualStudio.Razor;

[Export(typeof(VisualStudioHostServicesProvider))]
[method: ImportingConstructor]
internal sealed class VisualStudioHostServicesProvider([Import(typeof(VisualStudioWorkspace))] CodeAnalysis.Workspace workspace)
{
    private readonly CodeAnalysis.Workspace _workspace = workspace;

    public HostServices GetServices() => _workspace.Services.HostServices;
}
