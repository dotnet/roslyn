// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;

internal interface IRazorRequestWrapper
{
    Task<TResponse> SendRequestAsync<TParams, TResponse>(string methodName, TParams @params, CancellationToken cancellationToken);
    ValueTask SendRequestAsync(string methodName, CancellationToken cancellationToken);
    ValueTask SendRequestAsync<TParams>(string methodName, TParams @params, CancellationToken cancellationToken);
}
