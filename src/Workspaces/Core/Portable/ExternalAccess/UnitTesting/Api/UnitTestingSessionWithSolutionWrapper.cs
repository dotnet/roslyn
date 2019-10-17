// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingSessionWithSolutionWrapper
    {
        internal SessionWithSolution UnderlyingObject { get; }

        public UnitTestingSessionWithSolutionWrapper(SessionWithSolution underlyingObject)
            => UnderlyingObject = underlyingObject;

        public Task InvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            => UnderlyingObject?.InvokeAsync(targetName, arguments, cancellationToken) ?? Task.CompletedTask;

        public Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            => UnderlyingObject?.InvokeAsync<T>(targetName, arguments, cancellationToken);

        public void Dispose()
            => UnderlyingObject?.Dispose();
    }
}
