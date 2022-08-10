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
    internal interface IRoslynRequestExecutionQueue : IRequestExecutionQueue<RequestContext>, ILspService
    {
    }

    [ExportLspServiceFactory(typeof(IRoslynRequestExecutionQueue), StringConstants.XamlLspLanguagesContract), Shared]
    internal sealed class XamlRequestExecutionQueueFactory : ILspServiceFactory
    {
        private readonly XamlProjectService _projectService;
        private readonly IXamlLanguageServerFeedbackService? _feedbackService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public XamlRequestExecutionQueueFactory(
            XamlProjectService projectService,
            [Import(AllowDefault = true)] IXamlLanguageServerFeedbackService? feedbackService)
        {
            _projectService = projectService;
            _feedbackService = feedbackService;
        }

        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            return new XamlRequestExecutionQueue(_projectService, lspServices, _feedbackService);
        }

        private class XamlRequestExecutionQueue : IRoslynRequestExecutionQueue
        {
            private readonly XamlProjectService _projectService;
            private readonly IXamlLanguageServerFeedbackService? _feedbackService;
            private readonly ILspServices _lspServices;
            private readonly IRequestExecutionQueue<RequestContext> _baseQueue;

            public XamlRequestExecutionQueue(
                XamlProjectService projectService,
                ILspServices lspServices,
                IXamlLanguageServerFeedbackService? feedbackService)
            {
                _projectService = projectService;
                _feedbackService = feedbackService;
                _lspServices = lspServices;
            }

            public event EventHandler<RequestShutdownEventArgs>? RequestServerShutdown;

            public ValueTask DisposeAsync()
            {
                return _baseQueue.DisposeAsync();
            }

            public async Task<TResponseType> ExecuteAsync<TRequestType, TResponseType>(TRequestType? request, string methodName, ILspServices lspServices, CancellationToken cancellationToken)
            {
                // TODO: This is broken
                //var textDocument = handler.GetTextDocumentIdentifier(request);
                throw new NotImplementedException();

                //Uri textDocumentUri;
                //if (textDocument is Uri uri)
                //{
                //    textDocumentUri = uri;
                //}
                //else if (textDocument is TextDocumentIdentifier textDocumentIdentifier)
                //{
                //    textDocumentUri = textDocumentIdentifier.Uri;
                //}
                //else
                //{
                //    throw new NotImplementedException($"TextDocument was set to an unsupported value for method {methodName}");
                //}

                //DocumentId? documentId = null;
                //if (textDocumentUri.IsAbsoluteUri)
                //{
                //    documentId = _projectService.TrackOpenDocument(textDocumentUri.LocalPath);
                //}

                //using (var requestScope = _feedbackService?.CreateRequestScope(documentId, methodName))
                //{
                //    try
                //    {
                //        var result = await _baseQueue.ExecuteAsync<TRequestType, TResponseType>(
                //            request, methodName, lspServices, cancellationToken).ConfigureAwait(false);
                //        return result;
                //    }
                //    catch (Exception e) when (e is not OperationCanceledException)
                //    {
                //        // Inform Xaml language service that the RequestScope failed.
                //        // This doesn't send the exception to Telemetry or Watson
                //        requestScope?.RecordFailure(e);
                //        throw;
                //    }
                //}
            }

            public void Start(ILspServices lspServices)
            {
                _baseQueue.Start(lspServices);
            }
        }
    }
}
