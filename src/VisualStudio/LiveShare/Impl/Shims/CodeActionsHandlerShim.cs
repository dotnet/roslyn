// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using LiveShareCodeAction = Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol.CodeAction;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentCodeActionName)]
    internal class CodeActionsHandlerShim : AbstractLiveShareHandlerShim<CodeActionParams, object[]>
    {
        [ImportingConstructor]
        public CodeActionsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers, Methods.TextDocumentCodeActionName)
        {
        }

        public async override Task<object[]> HandleAsync(CodeActionParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var result = await base.HandleAsync(param, requestContext, cancellationToken).ConfigureAwait(false);
            // Liveshare has a custom type for code actions (predates LSP code actions) with the same shape.
            // We must convert the LSP code action to the liveshare code action because they have a hard dependecy on their type.
            // TODO - Get liveshare to remove hard dependency on custom liveshare code action type from host side.
            // See Liveshare's LanguageServiceProviderHandler.
            if (result is Command[] commands)
            {
                foreach (var command in commands)
                {
                    var liveshareCodeActions = command.Arguments.Where(a => a is CodeAction).Select(codeAction => GetLiveShareCodeAction(codeAction as CodeAction)).ToArray();
                    // Only modify the result if any code actions were able to be converted.
                    if (liveshareCodeActions.Length > 0)
                    {
                        command.Arguments = liveshareCodeActions;
                    }
                }

                return commands;
            }

            return result;

            // local functions
            static LiveShareCodeAction GetLiveShareCodeAction(CodeAction codeAction)
            {
                return new LiveShareCodeAction()
                {
                    Command = codeAction.Command,
                    Edit = codeAction.Edit,
                    Title = codeAction.Title
                };
            }
        }
    }
}
