// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Utilities
{
    internal sealed class EmptyAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        public static readonly IAsyncEnumerable<T> Instance = new EmptyAsyncEnumerable<T>();

        private EmptyAsyncEnumerable()
        {
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
            => EmptyAsyncEnumerator<T>.Instance;
    }

    internal sealed class EmptyAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        public static readonly IAsyncEnumerator<T> Instance = new EmptyAsyncEnumerator<T>();

        private EmptyAsyncEnumerator()
        {
        }

        public T Current => default;

        public ValueTask DisposeAsync()
            => new ValueTask();

        public ValueTask<bool> MoveNextAsync()
            => new ValueTask<bool>(false);
    }
}
