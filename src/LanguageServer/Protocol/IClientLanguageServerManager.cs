// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Manages sending requests or notifications to the client or server.
/// Note - be extremely intentional about using a request or notification.  Use exactly what the LSP spec defines the method as.
/// For example methods defined as requests even with no parameters or return value must be sent as requests regardless.
/// </summary>
internal interface IClientLanguageServerManager : ILspService
{
    Task<TResponse> SendRequestAsync<TParams, TResponse>(string methodName, TParams @params, CancellationToken cancellationToken);
    ValueTask SendRequestAsync(string methodName, CancellationToken cancellationToken);
    ValueTask SendRequestAsync<TParams>(string methodName, TParams @params, CancellationToken cancellationToken);
    ValueTask SendNotificationAsync(string methodName, CancellationToken cancellationToken);
    ValueTask SendNotificationAsync<TParams>(string methodName, TParams @params, CancellationToken cancellationToken);
}
