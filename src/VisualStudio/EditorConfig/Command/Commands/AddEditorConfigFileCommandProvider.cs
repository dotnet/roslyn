// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Workspace.VSIntegration.UI;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Command.Commands;

[Export(typeof(INodeExtender))]
internal class AddEditorConfigFileCommandProvider : INodeExtender
{
    private readonly IWorkspaceCommandHandler _handler = new AddEditorConfigFileCommandHandler();

    [ImportingConstructor]
    [Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    public AddEditorConfigFileCommandProvider()
    {
    }

    public IChildrenSource? ProvideChildren(WorkspaceVisualNodeBase parentNode) => null;

    public IWorkspaceCommandHandler? ProvideCommandHandler(WorkspaceVisualNodeBase parentNode)
        => parentNode is IFolderNode ? _handler : null;
}
