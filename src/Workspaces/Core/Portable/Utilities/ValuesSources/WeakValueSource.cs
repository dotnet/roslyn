// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A <see cref="ValueSource{T}"/> that keeps a weak reference to a value.
    /// </summary>
    internal sealed class WeakValueSource<T> : OptionalValueSource<T>
        where T : class
    {
        private readonly WeakReference<T> _weakValue;

        public WeakValueSource(T value)
        {
            _weakValue = new WeakReference<T>(value);
        }

        public override bool TryGetValue(out T value)
            => _weakValue.TryGetTarget(out value);

        public override T? GetValue(CancellationToken cancellationToken)
            => _weakValue.TryGetTarget(out var value) ? value : null;

        public override Task<T?> GetValueAsync(CancellationToken cancellationToken)
            => Task.FromResult(GetValue(cancellationToken));
    }
}
