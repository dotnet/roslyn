// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal struct PythiaAsyncLazyWrapper<T>
    {
        private readonly AsyncLazy<T> _underlyingObject;

        public PythiaAsyncLazyWrapper(Func<CancellationToken, Task<T>> asynchronousComputeFunction, bool cacheResult)
        {
            _underlyingObject = new AsyncLazy<T>(asynchronousComputeFunction, cacheResult);
        }

        public Task<T> GetValueAsync(CancellationToken cancellationToken)
            => _underlyingObject.GetValueAsync(cancellationToken);
    }
}
