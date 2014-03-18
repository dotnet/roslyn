// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal class CachedObjectSource<T> : ValueSource<T> where T : class
    {
        private readonly IObjectCache<T> cache;
        private readonly WeakReference<T> weakInstance;
        private readonly IWeakAction<T> evictAction;

        public CachedObjectSource(T instance, IObjectCache<T> cache)
        {
            this.weakInstance = new WeakReference<T>(instance);
            this.cache = cache;

            this.evictAction = new WeakAction<CachedObjectSource<T>, T>(this, (o, d) => o.OnEvicted(d));
            cache.AddOrAccess(instance, this.evictAction);
        }

        protected virtual void OnEvicted(T instance)
        {
            // nothing to do.. maybe some derived class would want to do something...
        }

        public override bool TryGetValue(out T value)
        {
            if (this.weakInstance.TryGetTarget(out value))
            {
                // let the cache know the instance was accessed
                this.cache.AddOrAccess(value, this.evictAction);

                return true;
            }

            return false;
        }

        public override T GetValue(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            T instance;
            if (this.weakInstance.TryGetTarget(out instance))
            {
                // let the cache know the instance was accessed
                this.cache.AddOrAccess(instance, this.evictAction);

                return instance;
            }
            else
            {
                return null;
            }
        }

        public override Task<T> GetValueAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(this.GetValue(cancellationToken));
        }
    }
}