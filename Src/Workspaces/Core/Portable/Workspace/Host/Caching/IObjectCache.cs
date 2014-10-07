// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// An object cache controls the lifetime of an object by holding a reference to it,
    /// keeping the garbage collector from collecting it. 
    /// 
    /// The cache uses a heuristic to determine how many objects to keep alive, which 
    /// to evict from the cache and when to evict them. 
    /// </summary>
    internal interface IObjectCache<T>
    {
        /// <summary>
        /// Add an object to the cache, or notify the cache that the object was recently used.
        /// </summary>
        /// <param name="instance">The object to add or notify.</param>
        /// <param name="evictor">An action to take when the object is evicted.</param>
        /// <returns>True if the instance was added.</returns>
        void AddOrAccess(T instance, IWeakAction<T> evictor);

        /// <summary>
        /// Clear the cache.
        /// </summary>
        void Clear();
    }
}