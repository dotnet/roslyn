// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
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
                Debug.Assert(oldValue != null);
                Debug.Assert(newValue != null);

                if (Object.ReferenceEquals(oldValue, newValue))
                {
                    return 0;
                }

                int result = 0;
                int i;
                for (i = 0; i < oldValue.KnownValuesCount && i < newValue.KnownValuesCount; i++)
                {
                    if (oldValue[i] == newValue[i])
                    {
                        continue;
                    }
                    else if (oldValue[i] < newValue[i])
                    {
                        if (result > 0)
                        {
                            // Previously encountered a non-monotonic merge.  Don't overwrite result.
                        }
                        else
                        {
                            result = -1;
                        }
                    }
                    else
                    {
                        Debug.Assert(oldValue[i] > newValue[i]);
                        FireNonMonotonicAssertIfNeeded(assertMonotonicity);
                        result = 1;
                    }
                }

                for (; i < oldValue.KnownValuesCount; i++)
                {
                    // The missing elements in newValue.PropertyAbstractValues are implicitly Unknown.
                    if (oldValue[i] > PropertySetAbstractValueKind.Unknown)
                    {
                        FireNonMonotonicAssertIfNeeded(assertMonotonicity);
                        result = 1;
                    }
                }

                for (; i < newValue.KnownValuesCount; i++)
                {
                    // The missing elements in oldValue.PropertyAbstractValues are implicitly Unknown.
                    if (newValue[i] > PropertySetAbstractValueKind.Unknown)
                    {
                        if (result > 0)
                        {
                            // Previously encountered a non-monotonic merge.  Don't overwrite result.
                        }
                        else
                        {
                            result = -1;
                        }
                    }
                }

                return result;
            }

            public override PropertySetAbstractValue Merge(PropertySetAbstractValue value1, PropertySetAbstractValue value2)
            {
                Debug.Assert(value1 != null);
                Debug.Assert(value2 != null);

                ArrayBuilder<PropertySetAbstractValueKind> builder = ArrayBuilder<PropertySetAbstractValueKind>.GetInstance(
                    Math.Max(value1.KnownValuesCount, value2.KnownValuesCount));
                try
                {
                    int i;
                    for (i = 0; i < value1.KnownValuesCount && i < value2.KnownValuesCount; i++)
                    {
                        builder.Add(this.MergeKind(value1[i], value2[i]));
                    }

                    for (; i < value1.KnownValuesCount; i++)
                    {
                        // The missing elements in value2.PropertyAbstractValues are implicitly Unknown.
                        builder.Add(this.MergeKind(value1[i], PropertySetAbstractValueKind.Unknown));
                    }

                    for (; i < value2.KnownValuesCount; i++)
                    {
                        // The missing elements in value1.PropertyAbstractValues are implicitly Unknown.
                        builder.Add(this.MergeKind(PropertySetAbstractValueKind.Unknown, value2[i]));
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
