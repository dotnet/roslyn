// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.VisualStudioCode.RazorExtension.Services;

[Shared]
[ExportRazorStatelessLspService(typeof(RazorExtensionInitializer))]
[method: ImportingConstructor]
internal sealed class RazorExtensionInitializer(VSCodeWorkspaceProvider workspaceProvider) : ILspService
{
    private readonly VSCodeWorkspaceProvider _workspaceProvider = workspaceProvider;

    public void Initialize(Workspace workspace)
    {
        _workspaceProvider.SetWorkspace(workspace);
    }
}
