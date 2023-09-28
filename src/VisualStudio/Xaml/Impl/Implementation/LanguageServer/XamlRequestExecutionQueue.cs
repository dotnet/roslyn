// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Xaml.Telemetry;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer
{
    internal class XamlRequestExecutionQueue : RoslynRequestExecutionQueue, ILspService
    {
        private readonly XamlProjectService _projectService;
        private readonly IXamlLanguageServerFeedbackService? _feedbackService;

        public XamlRequestExecutionQueue(
            XamlProjectService projectService,
            IXamlLanguageServerFeedbackService? feedbackService,
            AbstractLanguageServer<RequestContext> languageServer,
            ILspLogger logger,
            IHandlerProvider handlerProvider) : base(languageServer, logger, handlerProvider)
        {
            _projectService = projectService;
            _feedbackService = feedbackService;
        }

        protected override IMethodHandler GetHandlerForRequest(IQueueItem<RequestContext> work)
        {
            var methodHandler = base.GetHandlerForRequest(work);
            var textDocument = GetTextDocumentIdentifier(work, methodHandler);

            if (textDocument is not null)
            {
                var filePath = ProtocolConversions.GetDocumentFilePathFromUri(textDocument.Uri);

                _projectService.TrackOpenDocument(filePath);
            }

            return methodHandler;
        }
    }
}
