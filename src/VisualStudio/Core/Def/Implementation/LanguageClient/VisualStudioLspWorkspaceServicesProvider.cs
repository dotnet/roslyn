// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.LanguageServer;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient;

[Export(typeof(ILspWorkspaceServicesProvider))]
internal class VisualStudioLspWorkspaceServicesProvider : ILspWorkspaceServicesProvider
{
    private readonly VisualStudioWorkspace _workspace;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioLspWorkspaceServicesProvider(VisualStudioWorkspace workspace)
    {
        _workspace = workspace;
    }

    public HostWorkspaceServices GetHostWorkspaceServices()
    {
        return _workspace.Services;
    }
}
