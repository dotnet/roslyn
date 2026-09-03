// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Generator;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Kinds;
using Microsoft.VisualStudio.Workspace.VSIntegration.UI;
using System.Collections.Generic;
using System.ComponentModel.Design;
using static Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Logger;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Command.Commands;

internal class AddEditorConfigFileCommandHandler : CommandHandlerBase
{
    protected override CommandID Id { get; } = new CommandID(PackageGuids.AddEditorConfigCmdSet, PackageIds.AddEditorConfigFileAnyCodeCommand);

    protected override bool QueryStatus(List<WorkspaceVisualNodeBase> selection)
        => selection.Count == 1 && selection[0] is IFolderNode;

    protected override bool Execute(List<WorkspaceVisualNodeBase> selection)
    {
        Assert(selection.Count == 1, "Multiple items selected");
        if (selection.Count == 1 && selection[0] is IFolderNode folder)
        {
            using var _1 = LogUserAction(UserTask.CreateFromRightClickMenuAnyCode);
            _ = EditorConfigFileGenerator.TryAddFileToFolder(folder.FullPath);
            return true;
        }

        return false;
    }
}

