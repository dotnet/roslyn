// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingRemoteServiceConnectionWrapper
    {
        internal KeepAliveSession? UnderlyingObject { get; }

        internal UnitTestingRemoteServiceConnectionWrapper(KeepAliveSession? underlyingObject)
            => UnderlyingObject = underlyingObject;

        public bool IsDefault => UnderlyingObject == null;

        public async Task<bool> TryRunRemoteAsync(string targetName, Solution? solution, IReadOnlyList<object?> arguments, CancellationToken cancellationToken)
        {
            if (UnderlyingObject == null)
            {
                return false;
            }

            await UnderlyingObject.RunRemoteAsync(targetName, solution, arguments, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public async Task<Optional<T>> TryRunRemoteAsync<T>(string targetName, Solution? solution, IReadOnlyList<object?> arguments, CancellationToken cancellationToken)
        {
            if (UnderlyingObject == null)
            {
                return default;
            }

            return await UnderlyingObject.RunRemoteAsync<T>(targetName, solution, arguments, cancellationToken).ConfigureAwait(false);
        }
    }
}
