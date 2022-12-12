// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer;
using Microsoft.VisualStudio.LanguageServices.Xaml.Telemetry;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer;

[ExportStatelessXamlLspService(typeof(IRequestExecutionQueueProvider<RequestContext>)), Shared]
internal sealed class XamlRequestExecutionQueueProvider : IRequestExecutionQueueProvider<RequestContext>
{
    private readonly XamlProjectService _projectService;
    private readonly IXamlLanguageServerFeedbackService? _feedbackService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
    public XamlRequestExecutionQueueProvider(XamlProjectService projectService, IXamlLanguageServerFeedbackService? feedbackService)
    {
        _projectService = projectService;
        _feedbackService = feedbackService;
    }

    public IRequestExecutionQueue<RequestContext> CreateRequestExecutionQueue(AbstractLanguageServer<RequestContext> languageServer, ILspLogger logger, IHandlerProvider handlerProvider)
    {
        var queue = new XamlRequestExecutionQueue(_projectService, _feedbackService, languageServer, logger, handlerProvider);
        queue.Start();
        return queue;
    }
}
