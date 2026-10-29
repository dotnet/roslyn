// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Generator;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Kinds;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Utilities;
using static Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Logger;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Command.Commands;

internal class AddEditorConfigFileCommand : CommandBase
{
    protected override CommandID Id { get; } = new CommandID(PackageGuids.AddEditorConfigCmdSet, PackageIds.AddEditorConfigFileCommand);

    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        using var _ = LogUserAction(UserTask.CreateFromRightClickMenu);
        if (Package is not null)
        {
            await Package.JoinableTaskFactory.SwitchToMainThreadAsync(Package.DisposalToken);
            var (success, fileName) = EditorConfigFileGenerator.TryAddFileToSolution();
            Assert(success, "Unable to add the editorconfig file to the solution");

            if (success && fileName is not null)
            {
                VSHelpers.OpenFile(fileName);
            }
        }
    }
}
