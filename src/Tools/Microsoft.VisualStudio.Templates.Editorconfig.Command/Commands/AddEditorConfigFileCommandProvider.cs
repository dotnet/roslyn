// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Workspace.VSIntegration.UI;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Command.Commands;

[Export(typeof(INodeExtender))]
internal class AddEditorConfigFileCommandProvider : INodeExtender
{
    private IWorkspaceCommandHandler _handler = new AddEditorConfigFileCommandHandler();

    public IChildrenSource ProvideChildren(WorkspaceVisualNodeBase parentNode) => null;

    public IWorkspaceCommandHandler ProvideCommandHandler(WorkspaceVisualNodeBase parentNode)
        => parentNode is IFolderNode ? _handler : null;
}

