// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer;

[ExportStatelessXamlLspService(typeof(IRequestExecutionQueueProvider<RequestContext>)), Shared]
internal sealed class XamlRequestExecutionQueueProvider : IRequestExecutionQueueProvider<RequestContext>
{
    private readonly XamlProjectService _projectService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
    public XamlRequestExecutionQueueProvider(XamlProjectService projectService)
    {
        _projectService = projectService;
    }

    public IRequestExecutionQueue<RequestContext> CreateRequestExecutionQueue(AbstractLanguageServer<RequestContext> languageServer, ILspLogger logger, AbstractHandlerProvider handlerProvider)
    {
        var queue = new XamlRequestExecutionQueue(_projectService, languageServer, logger, handlerProvider);
        queue.Start();
        return queue;
    }
}
