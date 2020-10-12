﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Commands;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Runs a code action as a command on the server.
    /// This is done when a code action cannot be applied as a WorkspaceEdit on the LSP client.
    /// For example, all non-ApplyChangesOperations must be applied as a command.
    /// TO-DO: Currently, any ApplyChangesOperation that adds or removes a document must also be
    /// applied as a command due to an LSP bug (see https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1147293/).
    /// Commands must be applied from the UI thread in VS.
    /// </summary>
    [ExportExecuteWorkspaceCommand(CodeActionsHandler.RunCodeActionCommandName)]
    internal class RunCodeActionHandler : IExecuteWorkspaceCommandHandler
    {
        private readonly CodeActionsCache _codeActionsCache;
        private readonly ICodeFixService _codeFixService;
        private readonly ICodeRefactoringService _codeRefactoringService;
        private readonly ILspSolutionProvider _solutionProvider;
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RunCodeActionHandler(
            CodeActionsCache codeActionsCache,
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            ILspSolutionProvider solutionProvider,
            IThreadingContext threadingContext)
        {
            _codeActionsCache = codeActionsCache;
            _codeFixService = codeFixService;
            _codeRefactoringService = codeRefactoringService;
            _solutionProvider = solutionProvider;
            _threadingContext = threadingContext;
        }

        public async Task<object> HandleRequestAsync(LSP.ExecuteCommandParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var runRequest = ((JToken)request.Arguments.Single()).ToObject<CodeActionResolveData>();
            var document = _solutionProvider.GetDocument(runRequest.TextDocument);
            var codeActions = await CodeActionHelpers.GetCodeActionsAsync(
                _codeActionsCache, document, runRequest.Range, _codeFixService, _codeRefactoringService, cancellationToken).ConfigureAwait(false);

            var actionToRun = CodeActionHelpers.GetCodeActionToResolve(runRequest.UniqueIdentifier, codeActions);
            Contract.ThrowIfNull(actionToRun);

            var operations = await actionToRun.GetOperationsAsync(cancellationToken).ConfigureAwait(false);

            // TODO - This UI thread dependency should be removed.
            // https://github.com/dotnet/roslyn/projects/45#card-20619668
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            foreach (var operation in operations)
            {
                operation.Apply(document.Project.Solution.Workspace, cancellationToken);
            }

            return true;
        }
    }
}
