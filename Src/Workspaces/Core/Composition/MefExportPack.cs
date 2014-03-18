// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.WorkspaceServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Composition
{
    /// <summary>
    /// A feature pack for all exports in a MEF composition.
    /// </summary>
    public class MefExportPack : FeaturePack
    {
        private readonly ImmutableList<ComposablePartCatalog> catalogs;
        private ExportProvider provider;

        public MefExportPack(ComposablePartCatalog catalog)
        {
            this.catalogs = ImmutableList<ComposablePartCatalog>.Empty.Add(catalog);
        }

        public MefExportPack(IEnumerable<ComposablePartCatalog> catalogs)
        {
            this.catalogs = catalogs.ToImmutableList();
        }

        public MefExportPack(ExportProvider provider)
        {
            var container = provider as CompositionContainer;
            if (container != null)
            {
                this.catalogs = ImmutableList<ComposablePartCatalog>.Empty.Add(container.Catalog);
            }
            else
            {
                this.catalogs = ImmutableList<ComposablePartCatalog>.Empty;
            }

            this.provider = provider;
        }

        internal override IEnumerable<FeaturePack> Aggregate(IEnumerable<FeaturePack> packs)
        {
            // attempt to combine all 
            var list = new List<FeaturePack>();

            var combinedCatalogs = ImmutableList<ComposablePartCatalog>.Empty;

            foreach (var pack in packs)
            {
                var exportPack = pack as MefExportPack;

                if (exportPack != null)
                {
                    if (exportPack.catalogs.Count == 0)
                    {
                        // cannot combine catalogs from export provider that doesn't have any
                        list.Add(exportPack);
                    }
                    else
                    {
                        combinedCatalogs = combinedCatalogs.AddRange(exportPack.catalogs);
                    }
                }
                else
                {
                    // wasn't even our type... don't forget it.
                    list.Add(pack);
                }
            }

            if (combinedCatalogs.Count > 0)
            {
                list.Add(new MefExportPack(combinedCatalogs));
            }

            return list;
        }

        internal override ExportSource ComposeExports()
        {
            if (this.provider == null)
            {
                Interlocked.CompareExchange(
                    ref this.provider,
                    new CompositionContainer(
                        new AggregateCatalog(this.catalogs),
                        compositionOptions: CompositionOptions.DisableSilentRejection | CompositionOptions.IsThreadSafe),
                    null);
            }

            return new MefExportSource(this.provider);
        }

        internal override ExportSource ComposeExports(ExportSource root)
        {
            // MEF exports don't get to see the ExportSource
            return this.ComposeExports();
        }

        private class MefExportSource : ExportSource
        {
            private readonly ExportProvider provider;

            public MefExportSource(ExportProvider provider)
            {
                this.provider = provider;
            }

            public override IEnumerable<Lazy<T>> GetExports<T>()
            {
                return this.provider.GetExports<T>();
            }

            public override IEnumerable<Lazy<T, M>> GetExports<T, M>()
            {
                return this.provider.GetExports<T, M>();
            }
        }
    }
}
