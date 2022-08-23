// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal interface IClientLanguageServerManager : ILspService
{
    Task<TResponse> SendRequestAsync<TParams, TResponse>(string methodName, TParams @params, CancellationToken cancellationToken);
    ValueTask SendNotificationAsync(string methodName, CancellationToken cancellationToken);
    ValueTask SendNotificationAsync<TParams>(string methodName, TParams @params, CancellationToken cancellationToken);
}
