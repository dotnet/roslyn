// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal interface ILspServiceRequestHandler<RequestType, ResponseType> : ILspService, IRequestHandler<RequestType, ResponseType, RequestContext>
    {
    }

    internal interface ILspServiceDocumentRequestHandler<RequestType, ResponseType> : ILspService, IRequestHandler<RequestType, ResponseType, RequestContext>, ITextDocumentIdentifierHandler<RequestType, TextDocumentIdentifier>
    {
    }
}
