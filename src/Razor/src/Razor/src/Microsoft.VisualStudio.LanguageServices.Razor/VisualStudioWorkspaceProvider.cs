// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.LanguageServices;

namespace Microsoft.VisualStudio.Razor;

[Export(typeof(IWorkspaceProvider))]
[method: ImportingConstructor]
internal sealed class VisualStudioWorkspaceProvider(VisualStudioWorkspace workspace) : IWorkspaceProvider
{
    public CodeAnalysis.Workspace GetWorkspace() => workspace;
}
