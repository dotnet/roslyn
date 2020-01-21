// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Roslyn.Test.MetadataUtilities
{
    public sealed class AggregatedMetadataReader
    {
        private readonly MetadataAggregator _aggregator;
        public readonly MetadataReader Last;
        public ImmutableArray<MetadataReader> Readers { get; }

        public AggregatedMetadataReader(params MetadataReader[] readers)
            : this((IEnumerable<MetadataReader>)readers)
        {
        }

        public AggregatedMetadataReader(IEnumerable<MetadataReader> readers)
        {
            Readers = ImmutableArray.CreateRange(readers);
            Last = Readers.Last();
            _aggregator = new MetadataAggregator(readers.First(), readers.Skip(1).ToArray());
        }

        private TEntity GetValue<TEntity>(Handle handle, Func<MetadataReader, Handle, TEntity> getter)
        {
            var genHandle = _aggregator.GetGenerationHandle(handle, out var generation);
            return getter(Readers[generation], genHandle);
        }

        public IEnumerable<AssemblyReference> GetAssemblyReferences() =>
            Readers.SelectMany(r => r.AssemblyReferences.Select(h => r.GetAssemblyReference(h)));

        public string GetString(StringHandle handle) => GetValue(handle, (r, h) => r.GetString((StringHandle)h));
    }
}
