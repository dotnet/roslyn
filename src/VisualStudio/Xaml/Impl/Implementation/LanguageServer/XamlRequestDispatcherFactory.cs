// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;
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
    [ExportLspServiceFactory(typeof(RoslynRequestDispatcher), StringConstants.XamlLspLanguagesContract), Shared]
    internal sealed class XamlRequestDispatcherFactory : RequestDispatcherFactory
    {
        private readonly XamlProjectService _projectService;
        private readonly IXamlLanguageServerFeedbackService? _feedbackService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public XamlRequestDispatcherFactory(
            XamlProjectService projectService,
            [Import(AllowDefault = true)] IXamlLanguageServerFeedbackService? feedbackService)
        {
            _projectService = projectService;
            _feedbackService = feedbackService;
        }

        public override ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            return new XamlRequestDispatcher(_projectService, lspServices, _feedbackService);
        }

        private class XamlRequestDispatcher : RoslynRequestDispatcher, ILspService
        {
            private readonly XamlProjectService _projectService;
            private readonly IXamlLanguageServerFeedbackService? _feedbackService;

            public XamlRequestDispatcher(
                XamlProjectService projectService,
                LspServices services,
                IXamlLanguageServerFeedbackService? feedbackService) : base(services)
            {
                _projectService = projectService;
                _feedbackService = feedbackService;
            }

            protected override ImmutableDictionary<RequestHandlerMetadata, Lazy<IMethodHandler>> GetRequestHandlers()
            {
                throw new NotImplementedException();
            }

            protected override async Task<TResponseType> ExecuteRequestAsync<TRequestType, TResponseType>(
                IRequestExecutionQueue<RequestContext> queue, bool mutatesSolutionState,
                IRequestHandler<TRequestType, TResponseType, RequestContext> handler, TRequestType request, string methodName, CancellationToken cancellationToken)
            {
                var textDocument = handler.GetTextDocumentIdentifier(request);

                Uri textDocumentUri;
                if (textDocument is Uri uri)
                {
                    textDocumentUri = uri;
                }
                else if (textDocument is TextDocumentIdentifier textDocumentIdentifier)
                {
                    textDocumentUri = textDocumentIdentifier.Uri;
                }
                else
                {
                    throw new NotImplementedException($"TextDocument was set to an unsupported value for method {methodName}");
                }

                DocumentId? documentId = null;
                if (textDocumentUri.IsAbsoluteUri)
                {
                    documentId = _projectService.TrackOpenDocument(textDocumentUri.LocalPath);
                }

                using (var requestScope = _feedbackService?.CreateRequestScope(documentId, methodName))
                {
                    try
                    {
                        return await base.ExecuteRequestAsync(queue, mutatesSolutionState, handler, request, methodName, cancellationToken).ConfigureAwait(false);
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
