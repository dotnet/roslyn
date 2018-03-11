// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A <see cref="ValueSource{T}"/> that keeps a weak reference to a value.
    /// </summary>
    internal sealed class WeakConstantValueSource<T> : ValueSource<T> where T : class
    {
        private readonly WeakReference<T> _weakValue;

        public WeakConstantValueSource(T value)
        {
            _weakValue = new WeakReference<T>(value);
        }

        public override T GetValue(CancellationToken cancellationToken)
        {
            if (_weakValue != null)
            {
                if (_weakValue.TryGetTarget(out var value))
                {
                    return value;
                }
            }

            return default;
        }

        public override bool TryGetValue(out T value)
        {
            if (_weakValue != null)
            {
                if (_weakValue.TryGetTarget(out value))
                {
                    return true;
                }
            }

            value = default;
            return false;
        }

        public override Task<T> GetValueAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(this.GetValue(cancellationToken));
        }
    }
}
