// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Commands;
using Newtonsoft.Json.Linq;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportExecuteWorkspaceCommand(RunCodeActionCommandName)]
    internal class RunCodeActionsHandler : CodeActionsHandlerBase, IExecuteWorkspaceCommandHandler
    {
        [ImportingConstructor]
        public RunCodeActionsHandler(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService)
            : base(codeFixService, codeRefactoringService)
        {
        }

        public async Task<object> HandleRequestAsync(Solution solution, LSP.ExecuteCommandParams request, LSP.ClientCapabilities clientCapabilities,
            CancellationToken cancellationToken, bool keepThreadContext = false)
        {
            var runRequest = ((JToken)request.Arguments.Single()).ToObject<RunCodeActionParams>();
            var codeActions = await GetCodeActionsAsync(solution,
                                                        runRequest.CodeActionParams.TextDocument.Uri,
                                                        runRequest.CodeActionParams.Range,
                                                        keepThreadContext,
                                                        cancellationToken).ConfigureAwait(keepThreadContext);

            var actionToRun = codeActions?.FirstOrDefault(a => a.Title == runRequest.Title);

            if (actionToRun != null)
            {
                foreach (var operation in await actionToRun.GetOperationsAsync(cancellationToken).ConfigureAwait(keepThreadContext))
                {
                    operation.Apply(solution.Workspace, cancellationToken);
                }
            }

            return true;
        }
    }
}
