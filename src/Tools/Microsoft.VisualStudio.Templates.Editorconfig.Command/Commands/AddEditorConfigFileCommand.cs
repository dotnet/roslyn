// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Templates.EditorConfig.FileGenerator;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Command.Commands;

internal class AddEditorConfigFileCommand : CommandBase
{
    protected override CommandID Id { get; } = new CommandID(PackageGuids.AddEditorConfigCmdSet, PackageIds.AddEditorConfigFileCommand);

    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        await Package.JoinableTaskFactory.SwitchToMainThreadAsync(Package.DisposalToken);
        var (success, fileName) = EditorConfigFileGenerator.TryAddFileToSolution();
        if (success)
        {
            VSHelpers.OpenFile(fileName);
        }
    }
}
