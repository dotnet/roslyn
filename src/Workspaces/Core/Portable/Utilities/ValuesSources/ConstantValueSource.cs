// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    /// <summary>
    /// This value source keeps a strong reference to a value.
    /// </summary>
    internal sealed class ConstantValueSource<T> : ValueSource<T>
    {
        private readonly T _value;
        private Task<T> _task;

        public ConstantValueSource(T value)
        {
            _value = value;
        }

        public override T GetValue(CancellationToken cancellationToken = default)
        {
            return _value;
        }

        public override bool TryGetValue(out T value)
        {
            value = _value;
            return true;
        }

        public override Task<T> GetValueAsync(CancellationToken cancellationToken = default)
        {
            if (_task == null)
            {
                Interlocked.CompareExchange(ref _task, Task.FromResult(_value), null);
            }

            return _task;
        }
    }
}
