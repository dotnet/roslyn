// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Newtonsoft.Json.Linq;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    /// <summary>
    /// Run code actions handler.  Called when lightbulb invoked.
    /// TODO - Move to CodeAnalysis.LanguageServer once the UI thread dependency is removed.
    /// </summary>
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, LSP.Methods.WorkspaceExecuteCommandName)]
    internal class RunCodeActionsHandler : CodeActionsHandlerBase, ILspRequestHandler<LSP.ExecuteCommandParams, object, Solution>
    {
        private IThreadingContext _threadingContext;

        [ImportingConstructor]
        public RunCodeActionsHandler(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService, IThreadingContext threadingContext)
            : base(codeFixService, codeRefactoringService)
        {
        }

        public async Task<object> HandleAsync(LSP.ExecuteCommandParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var solution = requestContext.Context;
            if (request.Command.StartsWith(RemoteCommandNamePrefix))
            {
                var command = ((JObject)request.Arguments[0]).ToObject<LSP.Command>();
                request.Command = command.CommandIdentifier;
                request.Arguments = command.Arguments;
            }
            if (request.Command == RunCodeActionCommandName)
            {
                var runRequest = ((JToken)request.Arguments.Single()).ToObject<RunCodeActionParams>();

                var codeActions = await GetCodeActionsAsync(solution,
                                                            runRequest.TextDocument.Uri,
                                                            runRequest.Range,
                                                            cancellationToken).ConfigureAwait(false);

                var actionToRun = codeActions?.FirstOrDefault(a => a.Title == runRequest.Title);

                if (actionToRun != null)
                {
                    // CodeAction Operation's Apply methods needs to be called on the UI thread.
                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                    foreach (var operation in await actionToRun.GetOperationsAsync(cancellationToken).ConfigureAwait(true))
                    {
                        operation.Apply(solution.Workspace, cancellationToken);
                    }
                }
            }
            else
            {
                // TODO - Modify to our telemetry.
            }

            // Return true even in the case that we didn't run the command. Returning false would prompt the guest to tell the host to
            // enable command executuion, which wouldn't solve their problem.
            return true;
        }
    }
}
