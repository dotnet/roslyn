// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal interface IRoslynNotificationHandler : ILspService, INotificationHandler<RequestContext>
    {
    }
    internal interface IRoslynNotificationHandler<RequestType> : ILspService, INotificationHandler<RequestType, RequestContext>
    {
    }

    internal interface IRoslynRequestHandler<RequestType, ResponseType> : ILspService, IRequestHandler<RequestType, ResponseType, RequestContext>
    {
    }
}
