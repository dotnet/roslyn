// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    internal partial class PropertySetAnalysis
    {
        /// <summary>
        /// Abstract value domain for <see cref="PropertySetAnalysis"/> to merge and compare <see cref="PropertySetAbstractValue"/> values.
        /// </summary>
        private class PropertySetAbstractValueDomain : AbstractValueDomain<PropertySetAbstractValue>
        {
            public static PropertySetAbstractValueDomain Default = new();

            private PropertySetAbstractValueDomain() { }

            public override PropertySetAbstractValue Bottom => PropertySetAbstractValue.Unknown;

            public override PropertySetAbstractValue UnknownOrMayBeValue => PropertySetAbstractValue.Unknown;

            public override int Compare(PropertySetAbstractValue oldValue, PropertySetAbstractValue newValue, bool assertMonotonicity)
            {
                if (Object.ReferenceEquals(oldValue, newValue))
                {
                    return 0;
                }

                // The PropertySetAbstractValue indexer allows accessing beyond KnownValuesCount (returns Unknown),
                // so looping through the max of the two KnownValuesCount.
                int maxKnownCount = Math.Max(oldValue.KnownValuesCount, newValue.KnownValuesCount);
                int result = 0;
                for (int i = 0; i < maxKnownCount; i++)
                {
                    if (oldValue[i] == newValue[i])
                    {
                        continue;
                    }
                    else if (oldValue[i] < newValue[i])
                    {
                        result = -1;
                    }
                    else
                    {
                        FireNonMonotonicAssertIfNeeded(assertMonotonicity);
                        return 1;
                    }
                }

                return result;
            }

            public override PropertySetAbstractValue Merge(PropertySetAbstractValue value1, PropertySetAbstractValue value2)
            {
                // The PropertySetAbstractValue indexer allows accessing beyond KnownValuesCount (returns Unknown),
                // so looping through the max of the two KnownValuesCount.
                int maxKnownCount = Math.Max(value1.KnownValuesCount, value2.KnownValuesCount);
                using var _ = ArrayBuilder<PropertySetAbstractValueKind>.GetInstance(maxKnownCount, out var builder);

                for (int i = 0; i < maxKnownCount; i++)
                {
                    builder.Add(MergeKind(value1[i], value2[i]));
                }

                return PropertySetAbstractValue.GetInstance(builder);
            }

            private static PropertySetAbstractValueKind MergeKind(PropertySetAbstractValueKind kind1, PropertySetAbstractValueKind kind2)
            {
                if (kind1 == kind2)
                {
                    return kind1;
                }
                else
                {
                    return PropertySetAbstractValueKind.MaybeFlagged;
                }
            }
        }
    }
}
