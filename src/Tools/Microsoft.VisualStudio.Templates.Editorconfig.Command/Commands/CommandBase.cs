// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Command.Commands;

internal abstract class CommandBase
{
    public static async Task<T> InitializeAsync<T>(AsyncPackage package)
        where T : CommandBase, new()
    {
        var command = new T();

        command.Command = new OleMenuCommand(command.Execute, command.Id);
        command.Package = package;
        await package.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
        IMenuCommandService obj = (IMenuCommandService)await package.GetServiceAsync(typeof(IMenuCommandService));
        Assumes.Present(obj);
        obj.AddCommand(command.Command);
        return command;
    }

    public OleMenuCommand Command { get; protected set; }

    public AsyncPackage Package { get; protected set; }

    private void Execute(object sender, EventArgs e)
    {
        EventArgs e2 = e;
        Package.JoinableTaskFactory.RunAsync(async delegate
        {
            try
            {
                await ExecuteAsync((OleMenuCmdEventArgs)e2);
            }
            catch (Exception)
            {
                // TODO: log exception
            }
        }).FileAndForget("AddEditorConfigFileCommand");
    }

    protected abstract CommandID Id { get; }

    protected abstract Task ExecuteAsync(OleMenuCmdEventArgs e);
}

