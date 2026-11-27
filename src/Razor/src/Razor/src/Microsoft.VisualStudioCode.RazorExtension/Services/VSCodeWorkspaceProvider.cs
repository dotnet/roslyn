// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.VisualStudioCode.RazorExtension.Services;

[Shared]
[Export(typeof(IWorkspaceProvider))]
[Export(typeof(VSCodeWorkspaceProvider))]
internal sealed class VSCodeWorkspaceProvider : IWorkspaceProvider
{
    private Workspace? _workspace;

    public void SetWorkspace(Workspace workspace)
    {
        _workspace = workspace;
    }

    public Workspace GetWorkspace()
    {
        return _workspace ?? Assumed.Unreachable<Workspace>("Accessing the workspace before it has been provided");
    }
}
