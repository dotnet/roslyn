// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
#pragma warning disable CA1812 // Is too instantiated.
    internal sealed class PropertyMapperCollection
#pragma warning restore CA1812
    {
        public PropertyMapperCollection(IEnumerable<PropertyMapper> propertyMappers)
        {
            if (propertyMappers == null)
            {
                throw new ArgumentNullException(nameof(propertyMappers));
            }

            if (!propertyMappers.Any())
            {
                throw new ArgumentException("No PropertyMappers specified", nameof(propertyMappers));
            }

            ImmutableDictionary<string, (int Index, PropertyMapper PropertyMapper)>.Builder builder = ImmutableDictionary.CreateBuilder<string, (int Index, PropertyMapper PropertyMapper)>(StringComparer.Ordinal);
            int index = 0;
            foreach (PropertyMapper p in propertyMappers)
            {
                builder.Add(p.PropertyName, (index++, p));
            }

            this.PropertyMappersWithIndex = builder.ToImmutable();
        }

        public PropertyMapperCollection(params PropertyMapper[] propertyMappers)
            : this((IEnumerable<PropertyMapper>)propertyMappers)
        {
        }

        private PropertyMapperCollection()
        {
        }

        private ImmutableDictionary<string, (int Index, PropertyMapper PropertyMapper)> PropertyMappersWithIndex { get; }

        internal bool RequiresValueContentAnalysis
        {
            get
            {
                return this.PropertyMappersWithIndex.Values.Any(t => t.PropertyMapper.RequiresValueContentAnalysis);
            }
        }

        internal int Count => this.PropertyMappersWithIndex.Count;

        internal bool TryGetPropertyMapper(string propertyName, out PropertyMapper propertyMapper, out int index)
        {
            if (this.PropertyMappersWithIndex.TryGetValue(propertyName, out (int Index, PropertyMapper PropertyMapper) tuple))
            {
                propertyMapper = tuple.PropertyMapper;
                index = tuple.Index;
                return true;
            }
            else
            {
                propertyMapper = null;
                index = -1;
                return false;
            }
        }
    }
}
