// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal sealed class SymbolSearchCallbackDispatcher : RemoteServiceCallbackDispatcher, IRemoteSymbolSearchUpdateService.ICallback
    {
        private ISymbolSearchLogService GetLogService(RemoteServiceCallbackId callbackId)
            => (ISymbolSearchLogService)GetCallback(callbackId);

        public ValueTask LogExceptionAsync(RemoteServiceCallbackId callbackId, string exception, string text, CancellationToken cancellationToken)
            => GetLogService(callbackId).LogExceptionAsync(exception, text, cancellationToken);

        public ValueTask LogInfoAsync(RemoteServiceCallbackId callbackId, string text, CancellationToken cancellationToken)
            => GetLogService(callbackId).LogInfoAsync(text, cancellationToken);
    }
}
