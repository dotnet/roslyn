// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    internal partial class PropertySetAbstractValue
    {
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional - the arrays will be completely filled.
        private class ValuePool
        {
            /// <summary>
            /// Index is the <see cref="PropertySetAbstractValueKind" />.
            /// </summary>
            private readonly PropertySetAbstractValue[] OneDimensionalPool;

            /// <summary>
            /// Indices are the two <see cref="PropertySetAbstractValueKind"/>s.
            /// </summary>
            private readonly PropertySetAbstractValue[,] TwoDimensionalPool;

            public ValuePool()
            {
                int[] values = Enum.GetValues<PropertySetAbstractValueKind>().Cast<int>().ToArray();
                Array.Sort(values);
                for (int i = 0; i < values.Length; i++)
                {
                    Debug.Assert(values[i] == i, $"{nameof(PropertySetAbstractValueKind)} isn't a contiguous enum starting at 0");
                }

                this.OneDimensionalPool = new PropertySetAbstractValue[values.Length];
                foreach (int i in values)
                {
                    if (i == (int)PropertySetAbstractValueKind.Unknown)
                    {
                        this.OneDimensionalPool[i] = PropertySetAbstractValue.Unknown;
                    }
                    else
                    {
                        this.OneDimensionalPool[i] = new PropertySetAbstractValue(
                            ImmutableArray.Create<PropertySetAbstractValueKind>((PropertySetAbstractValueKind)i));
                    }
                }

                this.TwoDimensionalPool = new PropertySetAbstractValue[values.Length, values.Length];
                foreach (int i in values)
                {
                    foreach (int j in values)
                    {
                        if (j == (int)PropertySetAbstractValueKind.Unknown)
                        {
                            // Because PropertySetAbstractValueKinds beyond the KnownPropertyAbstractValues array are
                            // implicitly Unknown, and the second kind (j) is Unknown, we can just reuse the one-dimensional
                            // pool's instances.
                            this.TwoDimensionalPool[i, j] = OneDimensionalPool[i];
                        }
                        else
                        {
                            this.TwoDimensionalPool[i, j] = new PropertySetAbstractValue(
                                ImmutableArray.Create<PropertySetAbstractValueKind>(
                                    (PropertySetAbstractValueKind)i,
                                    (PropertySetAbstractValueKind)j));
                        }
                    }
                }
            }

            public PropertySetAbstractValue GetInstance(PropertySetAbstractValueKind v1)
            {
                return this.OneDimensionalPool[(int)v1];
            }

            public PropertySetAbstractValue GetInstance(PropertySetAbstractValueKind v1, PropertySetAbstractValueKind v2)
            {
                return this.TwoDimensionalPool[(int)v1, (int)v2];
            }
        }
#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional
    }
}
