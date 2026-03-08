// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

#if Unified_ExternalAccess
namespace Microsoft.CodeAnalysis.ExternalAccess.Unified.Xaml;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;
#endif

/// <summary>
/// Manages sending requests to the client
/// </summary>
internal interface IClientRequestManager
{
    Task<TResponse> SendRequestAsync<TParams, TResponse>(string methodName, TParams @params, CancellationToken cancellationToken);
    ValueTask SendRequestAsync(string methodName, CancellationToken cancellationToken);
    ValueTask SendRequestAsync<TParams>(string methodName, TParams @params, CancellationToken cancellationToken);
}
