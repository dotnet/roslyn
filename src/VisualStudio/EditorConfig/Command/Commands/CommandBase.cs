// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Kinds;
using static Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Logger;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Command.Commands;

internal abstract class CommandBase
{
    public static async Task<T> InitializeAsync<T>(AsyncPackage package)
        where T : CommandBase, new()
    {
        T command = new();

        command.Command = new OleMenuCommand(command.Execute, command.Id);
        command.Package = package;
        await package.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
        var menuCommandService = await package.GetServiceAsync<IMenuCommandService, IMenuCommandService>(
            throwOnFailure: true, package.DisposalToken).ConfigureAwait(true);
        Assumes.Present(menuCommandService);
        menuCommandService.AddCommand(command.Command);
        LogEvent(EventId.CommandRegistered);

        return command;
    }

    public OleMenuCommand? Command { get; protected set; }

    public AsyncPackage? Package { get; protected set; }

    private void Execute(object? sender, EventArgs e)
    {
        var e2 = e;
        Package?.JoinableTaskFactory.RunAsync(async delegate
        {
            try
            {
                await ExecuteAsync((OleMenuCmdEventArgs)e2).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                LogException(ex, "Error executing command");
                throw;
            }
        }).FileAndForget("AddEditorConfigFileCommand");
    }

    protected abstract CommandID Id { get; }

    protected abstract Task ExecuteAsync(OleMenuCmdEventArgs e);
}
