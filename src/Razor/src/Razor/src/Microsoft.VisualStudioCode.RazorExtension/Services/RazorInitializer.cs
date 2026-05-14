// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudioCode.RazorExtension.Services;

[Shared]
[ExportRazorStatelessLspService(typeof(AbstractRazorInitializer))]
[method: ImportingConstructor]
internal sealed class RazorInitializer(VSCodeWorkspaceProvider workspaceProvider) : AbstractRazorInitializer
{
    private readonly VSCodeWorkspaceProvider _workspaceProvider = workspaceProvider;

    internal override void Initialize(Workspace workspace)
    {
        _workspaceProvider.SetWorkspace(workspace);
    }
}
