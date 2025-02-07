// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

internal interface ILspClient
{
    Task<TResponseType?> ExecuteRequestAsync<TRequestType, TResponseType>(string methodName, TRequestType request, CancellationToken cancellationToken) where TRequestType : class;

    Task ExecuteNotificationAsync<RequestType>(string methodName, RequestType request) where RequestType : class;
    Task ExecuteNotification0Async(string methodName);

    void AddClientLocalRpcTarget(object target);
    void AddClientLocalRpcTarget(string methodName, Delegate handler);
}
