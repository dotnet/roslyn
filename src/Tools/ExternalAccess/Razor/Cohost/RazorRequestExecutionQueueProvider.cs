// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

[ExportRazorStatelessLspService(typeof(IRequestExecutionQueueProvider<RequestContext>)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RazorRequestExecutionQueueProvider() : IRequestExecutionQueueProvider<RequestContext>
{
    public IRequestExecutionQueue<RequestContext> CreateRequestExecutionQueue(AbstractLanguageServer<RequestContext> languageServer, ILspLogger logger, IHandlerProvider handlerProvider)
    {
        var queue = new RoslynRequestExecutionQueue(languageServer, logger, handlerProvider);
        queue.Start();
        return queue;
    }
}
