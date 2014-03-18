// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.WorkspaceServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Composition
{
    internal class AggregatePack : FeaturePack
    {
        private readonly ImmutableList<FeaturePack> packs;

        public AggregatePack(IEnumerable<FeaturePack> packs)
        {
            this.packs = packs.ToImmutableList();
        }

        public AggregatePack(params FeaturePack[] packs)
            : this((IEnumerable<FeaturePack>)packs)
        {
        }

        public ImmutableList<FeaturePack> Packs
        {
            get { return this.packs; }
        }

        internal override ExportSource ComposeExports()
        {
            return new AggregateExportSource(this.packs.Select(p => (Func<ExportSource, ExportSource>)(root => p.ComposeExports(root))));
        }

        internal override ExportSource ComposeExports(ExportSource root)
        {
            return new AggregateExportSource(this.packs.Select(p => p.ComposeExports(root)));
        }

        internal class AggregateExportSource : ExportSource
        {
            private readonly ExportSource[] sources;

            public AggregateExportSource(IEnumerable<ExportSource> sources)
            {
                this.sources = sources.ToArray();
            }

            public AggregateExportSource(IEnumerable<Func<ExportSource, ExportSource>> sources)
            {
                this.sources = sources.Select(fn => fn(this)).ToArray();
            }

            public override IEnumerable<Lazy<T, M>> GetExports<T, M>()
            {
                return this.sources.SelectMany(src => src.GetExports<T, M>());
            }

            public override IEnumerable<Lazy<T>> GetExports<T>()
            {
                return this.sources.SelectMany(src => src.GetExports<T>());
            }
        }
    }
}