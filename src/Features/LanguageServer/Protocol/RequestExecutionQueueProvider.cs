﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer;

[ExportCSharpVisualBasicStatelessLspService(typeof(IRequestExecutionQueueProvider<RequestContext>)), Shared]
internal sealed class RequestExecutionQueueProvider : IRequestExecutionQueueProvider<RequestContext>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
    public RequestExecutionQueueProvider()
    {
    }

    public IRequestExecutionQueue<RequestContext> CreateRequestExecutionQueue(AbstractLanguageServer<RequestContext> languageServer, ILspLogger logger, IHandlerProvider handlerProvider)
    {
        var queue = new RoslynRequestExecutionQueue(languageServer, logger, handlerProvider);
        queue.Start();
        return queue;
    }
}
