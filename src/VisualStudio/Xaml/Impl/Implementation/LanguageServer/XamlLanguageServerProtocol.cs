// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer
{
    /// <summary>
    /// Implements the Language Server Protocol for XAML
    /// </summary>
    [Shared]
    [Export(typeof(XamlLanguageServerProtocol))]
    internal sealed class XamlLanguageServerProtocol : AbstractRequestHandlerProvider
    {
        private readonly XamlProjectService _projectService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public XamlLanguageServerProtocol([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, XamlProjectService projectService)
            : base(requestHandlers, languageName: StringConstants.XamlLanguageName)
        {
            _projectService = projectService;
        }

        protected override Task<ResponseType> ExecuteRequestAsync<RequestType, ResponseType>(RequestExecutionQueue queue, RequestType request, ClientCapabilities clientCapabilities, string? clientName, string methodName, bool mutatesSolutionState, IRequestHandler<RequestType, ResponseType> handler, CancellationToken cancellationToken)
        {
            var textDocument = handler.GetTextDocumentIdentifier(request);

            if (textDocument is { Uri: { IsAbsoluteUri: true } documentUri })
            {
                _projectService.TrackOpenDocument(documentUri.LocalPath);
            }

            return base.ExecuteRequestAsync(queue, request, clientCapabilities, clientName, methodName, mutatesSolutionState, handler, cancellationToken);
        }
    }
}
