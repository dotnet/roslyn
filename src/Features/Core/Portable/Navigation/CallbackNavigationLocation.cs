// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal class CallbackNavigationLocation : INavigationLocation
    {
        private readonly Func<CancellationToken, Task<bool>> _tryNavigateToAsync;

        public CallbackNavigationLocation(Func<CancellationToken, Task<bool>> tryNavigateToAsync)
        {
            _tryNavigateToAsync = tryNavigateToAsync;
        }

        public Task<bool> TryNavigateToAsync(CancellationToken cancellationToken)
            => _tryNavigateToAsync(cancellationToken);
    }
}
