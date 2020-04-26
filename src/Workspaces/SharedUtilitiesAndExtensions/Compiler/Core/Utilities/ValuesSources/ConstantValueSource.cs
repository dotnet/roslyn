// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;
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
        private Task<T>? _task;

        public ConstantValueSource(T value)
            => _value = value;

        public override T GetValue(CancellationToken cancellationToken = default)
            => _value;

        public override bool TryGetValue([MaybeNullWhen(false)] out T value)
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
