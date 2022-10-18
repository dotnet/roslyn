// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer;
using Microsoft.VisualStudio.LanguageServices.Xaml.Telemetry;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer;
internal class XamlLanguageServer : RoslynLanguageServer
{
    private readonly XamlProjectService _projectService;
    private readonly IXamlLanguageServerFeedbackService? _feedbackService;

    public XamlLanguageServer(
        AbstractLspServiceProvider lspServiceProvider,
        JsonRpc jsonRpc,
        ICapabilitiesProvider capabilitiesProvider,
        ILspServiceLogger logger,
        ImmutableArray<string> supportedLanguages,
        WellKnownLspServerKinds serverKind,
        XamlProjectService projectService,
        IXamlLanguageServerFeedbackService? feedbackService) : base(lspServiceProvider, jsonRpc, capabilitiesProvider, logger, supportedLanguages, serverKind)
    {
        _projectService = projectService;
        _feedbackService = feedbackService;
    }

    protected override IRequestExecutionQueue<RequestContext> ConstructRequestExecutionQueue()
    {
        var queue = new XamlRequestExecutionQueue(_projectService, _feedbackService, this, _logger, GetHandlerProvider());
        queue.Start();
        return queue;
    }
}
