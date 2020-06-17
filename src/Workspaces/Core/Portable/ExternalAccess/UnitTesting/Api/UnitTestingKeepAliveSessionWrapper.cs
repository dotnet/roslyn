// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    [Obsolete]
    internal readonly struct UnitTestingKeepAliveSessionWrapper
    {
        internal RemoteServiceConnection UnderlyingObject { get; }

        internal UnitTestingKeepAliveSessionWrapper(RemoteServiceConnection underlyingObject)
            => UnderlyingObject = underlyingObject;

        public bool IsDefault => UnderlyingObject == null;

        public Task<T> TryInvokeAsync<T>(string targetName, Solution solution, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            => UnderlyingObject.RunRemoteAsync<T>(targetName, solution, arguments, cancellationToken);

        public async Task<bool> TryInvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            await UnderlyingObject.RunRemoteAsync(targetName, solution: null, arguments, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public async Task<bool> TryInvokeAsync(string targetName, Solution solution, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            await UnderlyingObject.RunRemoteAsync(targetName, solution, arguments, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public Task<T> TryInvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            => UnderlyingObject.RunRemoteAsync<T>(targetName, solution: null, arguments, cancellationToken);
    }
}
