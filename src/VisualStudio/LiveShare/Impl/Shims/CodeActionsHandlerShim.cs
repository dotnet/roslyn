// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    internal abstract class CodeActionsHandlerShim : CodeActionsHandler, ILspRequestHandler<CodeActionParams, SumType<Command, CodeAction>[], Solution>
    {
        public const string RemoteCommandNamePrefix = "_liveshare.remotecommand";
        protected const string ProviderName = "Roslyn";

        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public CodeActionsHandlerShim(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService) : base(codeFixService, codeRefactoringService)
        {
        }

        /// <summary>
        /// Handle a <see cref="Methods.TextDocumentCodeActionName"/> by delegating to the base LSP implementation
        /// from <see cref="CodeActionsHandler"/>.
        /// 
        /// We need to return a command that is a generic wrapper that VS Code can execute.
        /// The argument to this wrapper will either be a RunCodeAction command which will carry
        /// enough information to run the command or a CodeAction with the edits.
        /// There are server and client side dependencies on this shape in liveshare.
        /// </summary>
        /// <param name="param"></param>
        /// <param name="requestContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<SumType<Command, CodeAction>[]> HandleAsync(CodeActionParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var result = await base.HandleRequestAsync(requestContext.Context, param, requestContext.GetClientCapabilities(), cancellationToken).ConfigureAwait(false);

            var commands = new ArrayBuilder<SumType<Command, CodeAction>>();
            foreach (var resultObj in result)
            {
                var commandArguments = resultObj;
                var title = resultObj.Value is CodeAction codeAction ? codeAction.Title : ((Command)resultObj).Title;
                commands.Add(new Command
                {
                    Title = title,
                    // Overwrite the command identifier to match the expected liveshare remote command identifier.
                    CommandIdentifier = $"{RemoteCommandNamePrefix}.{ProviderName}",
                    Arguments = new object[] { commandArguments }
                });
            }

            return commands.ToArrayAndFree();
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentCodeActionName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynCodeActionsHandlerShim : CodeActionsHandlerShim
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RoslynCodeActionsHandlerShim(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService)
            : base(codeFixService, codeRefactoringService)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentCodeActionName)]
    internal class CSharpCodeActionsHandlerShim : CodeActionsHandlerShim
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpCodeActionsHandlerShim(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService)
            : base(codeFixService, codeRefactoringService)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentCodeActionName)]
    internal class VisualBasicCodeActionsHandlerShim : CodeActionsHandlerShim
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualBasicCodeActionsHandlerShim(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService)
            : base(codeFixService, codeRefactoringService)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentCodeActionName)]
    internal class TypeScriptCodeActionsHandlerShim : CodeActionsHandlerShim
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptCodeActionsHandlerShim(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService)
            : base(codeFixService, codeRefactoringService)
        {
        }
    }
}
