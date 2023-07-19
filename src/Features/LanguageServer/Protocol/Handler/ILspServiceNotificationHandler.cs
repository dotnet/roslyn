// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal interface ILspServiceNotificationHandler :
    ILspService,
    INotificationHandler<RequestContext>,
    ISolutionRequiredHandler
{
}

internal interface ILspServiceNotificationHandler<TRequest> :
    ILspService,
    INotificationHandler<TRequest, RequestContext>,
    ISolutionRequiredHandler
{
}
