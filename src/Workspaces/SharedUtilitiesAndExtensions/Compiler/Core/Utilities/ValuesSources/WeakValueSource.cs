// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A <see cref="ValueSource{T}"/> that keeps a weak reference to a value.
    /// </summary>
    internal sealed class WeakValueSource<T> : ValueSource<Optional<T>>
        where T : class
    {
        private readonly WeakReference<T> _weakValue;

        public WeakValueSource(T value)
            => _weakValue = new WeakReference<T>(value);

        public override bool TryGetValue(out Optional<T> value)
        {
            if (_weakValue.TryGetTarget(out var target))
            {
                value = target;
                return true;
            }

            value = default;
            return false;
        }

        public override Optional<T> GetValue(CancellationToken cancellationToken)
        {
            if (_weakValue.TryGetTarget(out var target))
            {
                return target;
            }

            return default;
        }

        public override Task<Optional<T>> GetValueAsync(CancellationToken cancellationToken)
            => Task.FromResult(GetValue(cancellationToken));
    }
}
