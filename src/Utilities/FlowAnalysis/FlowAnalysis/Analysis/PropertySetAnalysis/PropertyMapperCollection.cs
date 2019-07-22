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

            if (propertyMappers.Any(p => p.PropertyIndex >= 0))
            {
                if (!propertyMappers.All(p => p.PropertyIndex >= 0))
                {
                    throw new ArgumentException(
                        "Either all PropertyMappers must specify a property index, or no PropertyMappers specify a property index",
                        nameof(propertyMappers));
                }

                int expected = 0;
                foreach (int pi in propertyMappers.Select(p => p.PropertyIndex).Distinct().OrderBy(propertyIndex => propertyIndex))
                {
                    if (pi != expected)
                    {
                        throw new ArgumentException(
                            "PropertyIndex values aren't contiguous starting from 0",
                            nameof(propertyMappers));
                    }

                    expected++;
                }

                this.PropertyValuesCount = expected;
            }
            else
            {
                this.PropertyValuesCount = propertyMappers.Count();
            }

            ImmutableDictionary<string, (int Index, PropertyMapper PropertyMapper)>.Builder builder = ImmutableDictionary.CreateBuilder<string, (int Index, PropertyMapper PropertyMapper)>(StringComparer.Ordinal);
            int index = 0;
            foreach (PropertyMapper p in propertyMappers)
            {
                int indexToAdd = p.PropertyIndex >= 0 ? p.PropertyIndex : index++;
                builder.Add(p.PropertyName, (indexToAdd, p));
            }

            this.PropertyMappersWithIndex = builder.ToImmutable();
            this.RequiresValueContentAnalysis = this.PropertyMappersWithIndex.Values.Any(t => t.PropertyMapper.RequiresValueContentAnalysis);
        }

        public PropertyMapperCollection(params PropertyMapper[] propertyMappers)
            : this((IEnumerable<PropertyMapper>)propertyMappers)
        {
        }

        private PropertyMapperCollection()
        {
        }

        private ImmutableDictionary<string, (int Index, PropertyMapper PropertyMapper)> PropertyMappersWithIndex { get; }

        internal bool RequiresValueContentAnalysis { get; }

        internal int PropertyValuesCount { get; }

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
