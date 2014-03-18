// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    internal static partial class AsyncEnumerable
    {
        private class EmptyAsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            public static readonly IAsyncEnumerable<T> Instance = new EmptyAsyncEnumerable<T>();

            private EmptyAsyncEnumerable()
            {
            }

            public IAsyncEnumerator<T> GetEnumerator()
            {
                return EmptyAsyncEnumerator<T>.Instance;
            }
        }

        private class EmptyAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            public static readonly IAsyncEnumerator<T> Instance = new EmptyAsyncEnumerator<T>();

            private EmptyAsyncEnumerator()
            {
            }

            public T Current
            {
                get
                {
                    throw new InvalidOperationException();
                }
            }

            public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
            {
                return SpecializedTasks.False;
            }

            public void Dispose()
            {
            }
        }
    }
}
