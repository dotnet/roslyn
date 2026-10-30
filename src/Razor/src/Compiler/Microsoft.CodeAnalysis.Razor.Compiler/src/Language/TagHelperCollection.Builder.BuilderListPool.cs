// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollection
{
    public sealed partial class Builder
    {
        private static readonly BuilderListPool s_listPool = new();

        /// <summary>
        ///  Keeps a bounded tier of large buffers for tag helper collection builders, sharing
        ///  small buffers with the namespace pool so small tag helper collections do not exhaust
        ///  the large-buffer tier.
        /// </summary>
        internal sealed class BuilderListPool : ObjectPool<List<TagHelperDescriptor>>
        {
            // Eight 32768-slot arrays add at most 2 MiB of element storage on 64-bit hosts.
            private readonly List<TagHelperDescriptor>?[] _largeLists = new List<TagHelperDescriptor>?[8];

            public override List<TagHelperDescriptor> Get()
            {
                for (var i = 0; i < _largeLists.Length; i++)
                {
                    if (Interlocked.Exchange(ref _largeLists[i], null) is { } list)
                    {
                        return list;
                    }
                }

                // Fall back to the default pool when there are no large lists available.
                return TagHelperDescriptorListPool.Default.Get();
            }

            public override void Return(List<TagHelperDescriptor> list)
            {
                if (list.Capacity is > 2048 and <= 32768)
                {
                    list.Clear();

                    for (var i = 0; i < _largeLists.Length; i++)
                    {
                        if (Interlocked.CompareExchange(ref _largeLists[i], list, null) is null)
                        {
                            return;
                        }
                    }
                }

                // Fall back to the default pool when there are no large lists available.
                TagHelperDescriptorListPool.Default.Return(list);
            }
        }
    }
}
