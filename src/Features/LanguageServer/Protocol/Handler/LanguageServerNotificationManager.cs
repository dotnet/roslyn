// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using StreamJsonRpc;

    internal class LanguageServerNotificationManager : ILanguageServerNotificationManager
    {
        private readonly JsonRpc _jsonRpc;

        public LanguageServerNotificationManager(JsonRpc jsonRpc)
        {
            if (jsonRpc is null)
            {
                throw new ArgumentNullException(nameof(jsonRpc));
            }

            _jsonRpc = jsonRpc;
        }

        public async ValueTask SendNotificationAsync(string methodName, CancellationToken cancellationToken)
            => await _jsonRpc.NotifyAsync(methodName).ConfigureAwait(false);
    }
}
