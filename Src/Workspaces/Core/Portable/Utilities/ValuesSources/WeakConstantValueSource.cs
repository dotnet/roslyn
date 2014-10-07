// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A <see cref="T:ValueSource"/> that keeps a weak reference to a value.
    /// </summary>
    internal sealed class WeakConstantValueSource<T> : ValueSource<T> where T : class
    {
        private readonly WeakReference<T> weakValue;

        public WeakConstantValueSource(T value)
        {
            this.weakValue = new WeakReference<T>(value);
        }

        public override T GetValue(CancellationToken cancellationToken)
        {
            if (this.weakValue != null)
            {
                T value;
                if (this.weakValue.TryGetTarget(out value))
                {
                    return value;
                }
            }

            return default(T);
        }

        public override bool TryGetValue(out T value)
        {
            if (this.weakValue != null)
            {
                if (this.weakValue.TryGetTarget(out value))
                {
                    return true;
                }
            }

            value = default(T);
            return false;
        }

        public override Task<T> GetValueAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(this.GetValue(cancellationToken));
        }
    }
}