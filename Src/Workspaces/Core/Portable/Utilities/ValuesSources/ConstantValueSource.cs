// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    /// <summary>
    /// This value source keeps a strong reference to a value.
    /// </summary>
    internal sealed class ConstantValueSource<T> : ValueSource<T>
    {
        private readonly T value;
        private Task<T> task;

        public ConstantValueSource(T value)
        {
            this.value = value;
        }

        public override T GetValue(CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.value;
        }

        public override bool TryGetValue(out T value)
        {
            value = this.value;
            return true;
        }

        public override Task<T> GetValueAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (this.task == null)
            {
                Interlocked.CompareExchange(ref this.task, Task.FromResult(this.value), null);
            }

            return this.task;
        }
    }
}