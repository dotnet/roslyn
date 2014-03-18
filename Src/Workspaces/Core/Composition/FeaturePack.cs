// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Composition
{
    public abstract class FeaturePack
    {
        public FeaturePack Combine(params FeaturePack[] packs)
        {
            return Combine((IEnumerable<FeaturePack>)packs);
        }

        public FeaturePack Combine(IEnumerable<FeaturePack> packs)
        {
            var groups = Flatten(new[] { this }.Concat(packs)).Distinct().GroupBy(p => p.GetType());
            var newPacks = groups.SelectMany(g => g.First().Aggregate(g)).ToImmutableList();

            if (newPacks.Count == 1)
            {
                return newPacks[0];
            }
            else
            {
                return new AggregatePack(newPacks);
            }
        }

        private IEnumerable<FeaturePack> Flatten(IEnumerable<FeaturePack> packs)
        {
            foreach (var pack in packs)
            {
                var aggregate = pack as AggregatePack;
                if (aggregate != null)
                {
                    foreach (var subpack in aggregate.Packs)
                    {
                        yield return subpack;
                    }
                }
                else
                {
                    yield return pack;
                }
            }
        }

        internal virtual IEnumerable<FeaturePack> Aggregate(IEnumerable<FeaturePack> packs)
        {
            return packs;
        }

        internal abstract ExportSource ComposeExports();
        internal abstract ExportSource ComposeExports(ExportSource root);
    }
}