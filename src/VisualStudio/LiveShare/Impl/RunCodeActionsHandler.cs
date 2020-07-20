// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Newtonsoft.Json.Linq;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    /// <summary>
    /// Run code actions handler.  Called when lightbulb invoked.
    /// Code actions must be applied from the UI thread in VS.
    /// </summary>
    internal abstract class RunCodeActionsHandler : ILspRequestHandler<LSP.ExecuteCommandParams, object, Solution>
    {
        private readonly ICodeFixService _codeFixService;
        private readonly ICodeRefactoringService _codeRefactoringService;
        private readonly ILspSolutionProvider _solutionProvider;
        private readonly IThreadingContext _threadingContext;

        protected RunCodeActionsHandler(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService, ILspSolutionProvider solutionProvider, IThreadingContext threadingContext)
        {
            _codeFixService = codeFixService;
            _codeRefactoringService = codeRefactoringService;
            _solutionProvider = solutionProvider;
            _threadingContext = threadingContext;
        }

        public async Task<object> HandleAsync(LSP.ExecuteCommandParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            // Unwrap the command to get the RunCodeActions command.
            if (request.Command.StartsWith(CodeActionsHandlerShim.RemoteCommandNamePrefix))
            {
                var command = ((JObject)request.Arguments[0]).ToObject<LSP.Command>();
                request.Command = command.CommandIdentifier;
                request.Arguments = command.Arguments;
            }

            if (request.Command == CodeActionsHandler.RunCodeActionCommandName)
            {
                var runRequest = ((JToken)request.Arguments.Single()).ToObject<RunCodeActionParams>();
                var document = _solutionProvider.GetDocument(runRequest.CodeActionParams.TextDocument);
                var codeActions = await CodeActionsHandler.GetCodeActionsAsync(document, _codeFixService, _codeRefactoringService,
                    runRequest.CodeActionParams.Range, cancellationToken).ConfigureAwait(false);

                var actionToRun = codeActions?.FirstOrDefault(a => a.Title == runRequest.Title);

                if (actionToRun != null)
                {
                    foreach (var operation in await actionToRun.GetOperationsAsync(cancellationToken).ConfigureAwait(false))
                    {
                        // TODO - This UI thread dependency should be removed.
                        // https://github.com/dotnet/roslyn/projects/45#card-20619668
                        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                        operation.Apply(document.Project.Solution.Workspace, cancellationToken);
                    }
                }

                return true;
            }
            else
            {
                // We may not have a handler for the command identifier passed in so the base throws.
                // This is possible, especially in VSCode scenarios, so rather than blow up, log telemetry.
                Logger.Log(FunctionId.Liveshare_UnknownCodeAction, KeyValueLogMessage.Create(m => m["command"] = request.Command));

                // Return true even in the case that we didn't run the command. Returning false would prompt the guest to tell the host to
                // enable command execution, which wouldn't solve their problem.
                return true;
            }
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, LSP.Methods.WorkspaceExecuteCommandName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynRunCodeActionsHandler : RunCodeActionsHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RoslynRunCodeActionsHandler(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService,
            ILspSolutionProvider solutionProvider, IThreadingContext threadingContext) : base(codeFixService, codeRefactoringService, solutionProvider, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, LSP.Methods.WorkspaceExecuteCommandName)]
    internal class CSharpRunCodeActionsHandler : RunCodeActionsHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpRunCodeActionsHandler(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService,
            ILspSolutionProvider solutionProvider, IThreadingContext threadingContext) : base(codeFixService, codeRefactoringService, solutionProvider, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, LSP.Methods.WorkspaceExecuteCommandName)]
    internal class VisualBasicRunCodeActionsHandler : RunCodeActionsHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualBasicRunCodeActionsHandler(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService,
            ILspSolutionProvider solutionProvider, IThreadingContext threadingContext) : base(codeFixService, codeRefactoringService, solutionProvider, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, LSP.Methods.WorkspaceExecuteCommandName)]
    internal class TypeScriptRunCodeActionsHandler : RunCodeActionsHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptRunCodeActionsHandler(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService,
            ILspSolutionProvider solutionProvider, IThreadingContext threadingContext) : base(codeFixService, codeRefactoringService, solutionProvider, threadingContext)
        {
        }
    }
}
