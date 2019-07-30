// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Newtonsoft.Json.Linq;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    /// <summary>
    /// Run code actions handler.  Called when lightbulb invoked.
    /// </summary>
    internal class RunCodeActionsHandlerShim : AbstractLiveShareHandlerShim<LSP.ExecuteCommandParams, object>
    {
        private readonly IThreadingContext _threadingContext;

        public RunCodeActionsHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext)
            : base(requestHandlers, LSP.Methods.WorkspaceExecuteCommandName)
        {
            _threadingContext = threadingContext;
        }

        /// <summary>
        /// Unwraps the run code action request from the liveshare shape into the expected shape
        /// handled by <see cref="RunCodeActionsHandler"/>.  This shape is an <see cref="LSP.Command"/>
        /// containing <see cref="RunCodeActionParams"/> as the argument.
        /// </summary>
        public async override Task<object> HandleAsync(LSP.ExecuteCommandParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            // Unwrap the command to get the RunCodeActions command.
            if (request.Command.StartsWith(CodeActionsHandlerShim.RemoteCommandNamePrefix))
            {
                var command = ((JObject)request.Arguments[0]).ToObject<LSP.Command>();
                // The CommandIdentifier should match the exported sub-handler for workspace execute command request.
                request.Command = command.CommandIdentifier;
                request.Arguments = command.Arguments;
            }

            try
            {
                // Code actions must be applied from the UI thread in the VS context.
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                return await base.HandleAsyncPreserveThreadContext(request, requestContext, cancellationToken).ConfigureAwait(false);
            }
            catch (ArgumentException ex)
            {
                // We may not have a handler for the command identifier passed in so the base throws.
                // This is possible, especially in VSCode scenarios, so rather than blow up, log telemetry.
                Logger.Log(FunctionId.Liveshare_UnknownCodeAction, KeyValueLogMessage.Create(m => m["callstack"] = ex.ToString()));

                // Return true even in the case that we didn't run the command. Returning false would prompt the guest to tell the host to
                // enable command executuion, which wouldn't solve their problem.
                return true;
            }
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, LSP.Methods.WorkspaceExecuteCommandName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynRunCodeActionsHandlerShim : RunCodeActionsHandlerShim
    {
        [ImportingConstructor]
        public RoslynRunCodeActionsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, LSP.Methods.WorkspaceExecuteCommandName)]
    internal class CSharpRunCodeActionsHandlerShim : RunCodeActionsHandlerShim
    {
        [ImportingConstructor]
        public CSharpRunCodeActionsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, LSP.Methods.WorkspaceExecuteCommandName)]
    internal class VisualBasicRunCodeActionsHandlerShim : RunCodeActionsHandlerShim
    {
        [ImportingConstructor]
        public VisualBasicRunCodeActionsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, LSP.Methods.WorkspaceExecuteCommandName)]
    internal class TypeScriptRunCodeActionsHandlerShim : RunCodeActionsHandlerShim
    {
        [ImportingConstructor]
        public TypeScriptRunCodeActionsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }
}
