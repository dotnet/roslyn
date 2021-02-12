// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Xaml.Telemetry;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer
{
    /// <summary>
    /// Implements the Language Server Protocol for XAML
    /// </summary>
    [Export(typeof(XamlRequestDispatcherFactory)), Shared]
    internal sealed class XamlRequestDispatcherFactory : AbstractRequestDispatcherFactory
    {
        private readonly XamlProjectService _projectService;
        private readonly IXamlLanguageServerFeedbackService? _feedbackService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public XamlRequestDispatcherFactory(
            [ImportMany] IEnumerable<Lazy<AbstractRequestHandlerProvider, RequestHandlerProviderMetadataView>> requestHandlerProviders,
            XamlProjectService projectService,
            [Import(AllowDefault = true)] IXamlLanguageServerFeedbackService? feedbackService)
            : base(requestHandlerProviders, languageName: StringConstants.XamlLanguageName)
        {
            _projectService = projectService;
            _feedbackService = feedbackService;
        }

        public override RequestDispatcher CreateRequestDispatcher()
        {
            return new XamlRequestDispatcher(_projectService, _requestHandlerProviders, _feedbackService, _languageName);
        }

        private class XamlRequestDispatcher : RequestDispatcher
        {
            private readonly XamlProjectService _projectService;
            private readonly IXamlLanguageServerFeedbackService? _feedbackService;

            public XamlRequestDispatcher(
                XamlProjectService projectService,
                ImmutableArray<Lazy<AbstractRequestHandlerProvider, RequestHandlerProviderMetadataView>> requestHandlerProviders,
                IXamlLanguageServerFeedbackService? feedbackService,
                string? languageName = null) : base(requestHandlerProviders, languageName)
            {
                _projectService = projectService;
                _feedbackService = feedbackService;
            }

            protected override async Task<ResponseType> ExecuteRequestAsync<RequestType, ResponseType>(RequestExecutionQueue queue, RequestType request, ClientCapabilities clientCapabilities, string? clientName, string methodName, bool mutatesSolutionState, bool requiresLSPSolution, IRequestHandler<RequestType, ResponseType> handler, CancellationToken cancellationToken)
            {
                var textDocument = handler.GetTextDocumentIdentifier(request);

                DocumentId? documentId = null;
                if (textDocument is { Uri: { IsAbsoluteUri: true } documentUri })
                {
                    documentId = _projectService.TrackOpenDocument(documentUri.LocalPath);
                }

                using (var requestScope = _feedbackService?.CreateRequestScope(documentId, methodName))
                {
                    try
                    {
                        return await base.ExecuteRequestAsync(queue, request, clientCapabilities, clientName, methodName, mutatesSolutionState, requiresLSPSolution, handler, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        // Inform Xaml language service that the RequestScope failed.
                        // This doesn't send the exception to Telemetry or Watson
                        requestScope?.RecordFailure(e);
                        throw;
                    }
                }
            }
        }
    }
}
