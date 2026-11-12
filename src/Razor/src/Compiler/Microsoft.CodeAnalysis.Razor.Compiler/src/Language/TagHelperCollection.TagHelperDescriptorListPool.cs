// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollection
{
    /// <summary>
    ///  A pool of <see cref="List{T}"/> instances for <see cref="TagHelperDescriptor"/>.
    /// </summary>
    /// <remarks>
    ///  Large projects can have thousands of tag helpers distributed across many namespaces and
    ///  assemblies. The directive visitors allocate lists per-namespace (or per-assembly) during
    ///  document compilation, then return them on reset. With a small retention limit, these lists
    ///  are trimmed on return and must re-grow on the next document — traces have shown GBs of
    ///  allocation pressure from repeated <c>List&lt;T&gt;</c> resizing in this path. A higher
    ///  retention limit keeps the grown lists in the pool, trading modest stable memory for
    ///  eliminating repeated resize allocations.
    /// </remarks>
    internal sealed class TagHelperDescriptorListPool : CustomObjectPool<List<TagHelperDescriptor>>
    {
        public static readonly TagHelperDescriptorListPool Default = new(
            initialCapacity: 4, maximumCapacity: 2048, poolSize: 128);

        internal TagHelperDescriptorListPool(int initialCapacity, int maximumCapacity, int poolSize)
            : base(new Policy(initialCapacity, maximumCapacity), poolSize)
        {
        }

        private sealed class Policy(int initialCapacity, int maximumCapacity) : PooledObjectPolicy
        {
            public override List<TagHelperDescriptor> Create() => new(capacity: initialCapacity);

            public override bool Return(List<TagHelperDescriptor> list)
            {
                list.Clear();

                if (list.Capacity > maximumCapacity)
                {
                    list.Capacity = maximumCapacity;
                }

                return true;
            }
        }
    }
}
