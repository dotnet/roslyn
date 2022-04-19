// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Workspace.VSIntegration.UI;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Templates.EditorConfig.FileGenerator;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Command.Commands;

internal class AddEditorConfigFileCommandHandler : CommandHandlerBase
{
    protected override CommandID Id { get; } = new CommandID(PackageGuids.AddEditorConfigCmdSet, PackageIds.AddEditorConfigFileAnyCodeCommand);

    protected override bool QueryStatus(List<WorkspaceVisualNodeBase> selection)
        => selection.Count == 1 && selection[0] is IFolderNode;

    protected override bool Execute(List<WorkspaceVisualNodeBase> selection)
    {
        if (selection.Count == 1 && selection[0] is IFolderNode folder)
        {
            _ = EditorConfigFileGenerator.TryAddFileToFolder(folder.FullPath);
            return true;
        }

        return false;
    }
}

