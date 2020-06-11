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
    internal readonly struct UnitTestingSessionWithSolutionWrapper : IDisposable
    {
        internal SessionWithSolution? UnderlyingObject { get; }

        public bool IsDefault => UnderlyingObject == null;

        public UnitTestingSessionWithSolutionWrapper(SessionWithSolution underlyingObject)
            => UnderlyingObject = underlyingObject;

        public Task InvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            => UnderlyingObject!.KeepAliveSession.RunRemoteAsync(targetName, solution: null, arguments, cancellationToken);

        public Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            => UnderlyingObject!.KeepAliveSession.RunRemoteAsync<T>(targetName, solution: null, arguments, cancellationToken);

        public void Dispose()
            => UnderlyingObject?.Dispose();
    }
}
