//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.Cascade.Telemetry;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    internal class RoslynRemoteCodeActionOperation : CodeActionOperation
    {
        private readonly Command command;
        private readonly ILanguageServerClient lspClient;

        public RoslynRemoteCodeActionOperation(Command command, ILanguageServerClient lspClient)
        {
            this.command = command ?? throw new ArgumentNullException(nameof(command));
            this.lspClient = lspClient ?? throw new ArgumentNullException(nameof(lspClient));
        }

        public override void Apply(Workspace workspace, CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                var executeCommandParams = new ExecuteCommandParams
                {
                    Command = this.command.CommandIdentifier,
                    Arguments = this.command.Arguments
                };

                await this.lspClient.RequestAsync(Methods.WorkspaceExecuteCommand, executeCommandParams, cancellationToken).ConfigureAwait(false);
            }).FileAndForget(CascadeTelemetry.FeaturePrefix + nameof(RoslynRemoteCodeActionOperation) + "/" + nameof(Apply));
        }
    }
}
