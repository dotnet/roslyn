// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    internal partial class PropertySetAnalysis
    {
        /// <summary>
        /// Abstract value domain for <see cref="PropertySetAnalysis"/> to merge and compare <see cref="PropertySetAbstractValue"/> values.
        /// </summary>
        private class PropertySetAbstractValueDomain : AbstractValueDomain<PropertySetAbstractValue>
        {
            public static PropertySetAbstractValueDomain Default = new PropertySetAbstractValueDomain();

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
                ArrayBuilder<PropertySetAbstractValueKind> builder = ArrayBuilder<PropertySetAbstractValueKind>.GetInstance(maxKnownCount);
                try
                {
                    for (int i = 0; i < maxKnownCount; i++)
                    {
                        builder.Add(this.MergeKind(value1[i], value2[i]));
                    }

                    return PropertySetAbstractValue.GetInstance(builder);
                }
                finally
                {
                    builder.Free();
                }
            }

            private PropertySetAbstractValueKind MergeKind(PropertySetAbstractValueKind kind1, PropertySetAbstractValueKind kind2)
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
