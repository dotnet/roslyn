// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Shims
{
    internal class LiveShareInitializeHandler : ILspRequestHandler<InitializeParams, InitializeResult, Solution>
    {
        private static readonly InitializeResult s_initializeResult = new InitializeResult
        {
            Capabilities = new ServerCapabilities
            {
                CodeActionProvider = true,
                ExecuteCommandProvider = new ExecuteCommandOptions(),
                ReferencesProvider = true,
                RenameProvider = true,
                Experimental = new RoslynExperimentalCapabilities { SyntacticLspProvider = true },
            }
        };

        public Task<InitializeResult> HandleAsync(InitializeParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
            => Task.FromResult(s_initializeResult);
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.InitializeName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynInitializeHandlerShim : LiveShareInitializeHandler
    {
        [ImportingConstructor]
        public RoslynInitializeHandlerShim()
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.InitializeName)]
    internal class CSharpInitializeHandlerShim : LiveShareInitializeHandler
    {
        [ImportingConstructor]
        public CSharpInitializeHandlerShim()
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.InitializeName)]
    internal class VisualBasicInitializeHandlerShim : LiveShareInitializeHandler
    {
        [ImportingConstructor]
        public VisualBasicInitializeHandlerShim()
        {
        }
    }
}
