// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer
{
    internal class XamlRequestExecutionQueue : RequestExecutionQueue<RequestContext>, ILspService
    {
        private readonly XamlProjectService _projectService;

        public XamlRequestExecutionQueue(
            XamlProjectService projectService,
            AbstractLanguageServer<RequestContext> languageServer,
            ILspLogger logger,
            AbstractHandlerProvider handlerProvider) : base(languageServer, logger, handlerProvider)
        {
            _projectService = projectService;
        }

        [Obsolete]
        protected internal override void BeforeRequest<TRequest>(TRequest request)
        {
            if (request is ITextDocumentParams textDocumentParams &&
                textDocumentParams.TextDocument is { Uri: { IsAbsoluteUri: true } documentUri })
            {
                _projectService.TrackOpenDocument(documentUri.LocalPath);
            }
        }
    }
}
