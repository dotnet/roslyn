// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
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
    [ExportLspServiceFactory(typeof(RequestDispatcher), StringConstants.XamlLspLanguagesContract), Shared]
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

        private class XamlRequestDispatcher : RequestDispatcher
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

            protected override async Task<TResponseType?> ExecuteRequestAsync<TRequestType, TResponseType>(
                RequestExecutionQueue queue, bool mutatesSolutionState, bool requiresLSPSolution, IRequestHandler<TRequestType, TResponseType> handler, TRequestType request, ClientCapabilities clientCapabilities, string methodName, CancellationToken cancellationToken)
                where TRequestType : class
                where TResponseType : default
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
                        return await base.ExecuteRequestAsync(queue, mutatesSolutionState, requiresLSPSolution, handler, request, clientCapabilities, methodName, cancellationToken).ConfigureAwait(false);
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
