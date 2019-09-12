// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    internal class CodeActionsHandlerShim : AbstractLiveShareHandlerShim<CodeActionParams, object[]>
    {
        public const string RemoteCommandNamePrefix = "_liveshare.remotecommand";
        protected const string ProviderName = "Roslyn";

        public CodeActionsHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers, Methods.TextDocumentCodeActionName)
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
        public async override Task<object[]> HandleAsync(CodeActionParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var result = await base.HandleAsync(param, requestContext, cancellationToken).ConfigureAwait(false);

            var commands = new ArrayBuilder<Command>();
            foreach (var resultObj in result)
            {
                var commandArguments = resultObj;
                var title = resultObj is CodeAction codeAction ? codeAction.Title : ((Command)resultObj).Title;
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
        public RoslynCodeActionsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentCodeActionName)]
    internal class CSharpCodeActionsHandlerShim : CodeActionsHandlerShim
    {
        [ImportingConstructor]
        public CSharpCodeActionsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentCodeActionName)]
    internal class VisualBasicCodeActionsHandlerShim : CodeActionsHandlerShim
    {
        [ImportingConstructor]
        public VisualBasicCodeActionsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentCodeActionName)]
    internal class TypeScriptCodeActionsHandlerShim : CodeActionsHandlerShim
    {
        [ImportingConstructor]
        public TypeScriptCodeActionsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }
}
