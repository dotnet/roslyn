// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.CodeActions
{
    internal class RoslynRemoteCodeActionOperation : CodeActionOperation
    {
        private readonly Command _command;
        private readonly ILanguageServerClient _lspClient;

        public RoslynRemoteCodeActionOperation(Command command, ILanguageServerClient lspClient)
        {
            _command = command ?? throw new ArgumentNullException(nameof(command));
            _lspClient = lspClient ?? throw new ArgumentNullException(nameof(lspClient));
        }

        public override void Apply(CodeAnalysis.Workspace workspace, CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                var executeCommandParams = new ExecuteCommandParams
                {
                    Command = _command.CommandIdentifier,
                    Arguments = _command.Arguments
                };

                await _lspClient.RequestAsync(Methods.WorkspaceExecuteCommand.ToLSRequest(), executeCommandParams, cancellationToken).ConfigureAwait(false);
            });
        }
    }
}
