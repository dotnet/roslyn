// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer;

internal sealed class XamlRequestExecutionQueue : RequestExecutionQueue<RequestContext>, ILspService
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

    protected internal override void BeforeRequest<TRequest>(TRequest request)
    {
        if (request is ITextDocumentParams { TextDocument.DocumentUri: { ParsedUri: not null } documentUri })
        {
            _projectService.TrackOpenDocument(documentUri.ParsedUri.LocalPath);
        }
    }
}
