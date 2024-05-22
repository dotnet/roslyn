// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[ExportStatelessLspService(typeof(IRequestExecutionQueueProvider<RequestContext>), ProtocolConstants.TypeScriptLanguageContract), Shared]
internal sealed class VSTypeScriptRequestExecutionQueueProvider : IRequestExecutionQueueProvider<RequestContext>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
    public VSTypeScriptRequestExecutionQueueProvider()
    {
    }

    public IRequestExecutionQueue<RequestContext> CreateRequestExecutionQueue(AbstractLanguageServer<RequestContext> languageServer, ILspLogger logger, AbstractHandlerProvider handlerProvider)
    {
        var queue = new RoslynRequestExecutionQueue(languageServer, logger, handlerProvider);
        queue.Start();
        return queue;
    }
}
