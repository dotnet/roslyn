// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Utilities;

namespace Roslyn.Utilities
{
    /// <summary>
    /// This value source keeps a strong reference to a value.
    /// </summary>
    internal sealed class ConstantValueSource<T> : ValueSource<T>
    {
        private readonly T _value;

        [NoMainThreadDependency(AlwaysCompleted = true)]
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

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
            Debug.Assert(_task.IsCompleted);
            return _task;
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
        }
    }
}
