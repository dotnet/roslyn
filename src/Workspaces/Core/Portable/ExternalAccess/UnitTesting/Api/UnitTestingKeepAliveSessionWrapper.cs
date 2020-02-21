// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingKeepAliveSessionWrapper
    {
        internal UnitTestingKeepAliveSessionWrapper(KeepAliveSession underlyingObject)
            => UnderlyingObject = underlyingObject ?? throw new ArgumentNullException(nameof(underlyingObject));

        internal KeepAliveSession UnderlyingObject { get; }

        public async Task<T> TryInvokeAsync<T>(string targetName, Solution solution, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            var result = await UnderlyingObject.TryInvokeAsync<T>(targetName, solution, arguments, cancellationToken).ConfigureAwait(false);
            return result.HasValue ? result.Value : default;
        }

        public Task<bool> TryInvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            => UnderlyingObject.TryInvokeAsync(targetName, solution: null, arguments, cancellationToken);

        public Task<bool> TryInvokeAsync(string targetName, Solution solution, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            => UnderlyingObject.TryInvokeAsync(targetName, solution, arguments, cancellationToken);

        public async Task<T> TryInvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            var result = await UnderlyingObject.TryInvokeAsync<T>(targetName, solution: null, arguments, cancellationToken).ConfigureAwait(false);
            return result.HasValue ? result.Value : default;
        }
    }
}
