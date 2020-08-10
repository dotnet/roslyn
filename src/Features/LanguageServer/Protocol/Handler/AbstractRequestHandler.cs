// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal abstract class AbstractRequestHandler<RequestType, ResponseType> : IRequestHandler<RequestType, ResponseType>
    {
        public virtual TextDocumentIdentifier? GetTextDocumentIdentifier(RequestType request) => null;
        public abstract Task<ResponseType> HandleRequestAsync(RequestType request, RequestContext context, CancellationToken cancellationToken);
    }
}
