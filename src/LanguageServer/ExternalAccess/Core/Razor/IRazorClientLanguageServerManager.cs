// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

internal interface IRazorClientLanguageServerManager : ILspService
{
    ValueTask SendNotificationAsync(string methodName, CancellationToken cancellationToken);
    ValueTask SendNotificationAsync<TParams>(string methodName, TParams @params, CancellationToken cancellationToken);
    ValueTask SendRequestAsync(string methodName, CancellationToken cancellationToken);
    Task<TResponse> SendRequestAsync<TParams, TResponse>(string methodName, TParams @params, CancellationToken cancellationToken);
    ValueTask SendRequestAsync<TParams>(string methodName, TParams @params, CancellationToken cancellationToken);
}
