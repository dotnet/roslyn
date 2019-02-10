// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Note that <see cref="KnownPropertyAbstractValues"/> may be "incomplete", e.g. it doesn't cover all properties.  In such cases, missing elements are <see cref="PropertySetAbstractValueKind.Unknown"/>.</remarks>
    internal class PropertySetAbstractValue
    {
        public static readonly PropertySetAbstractValue Unknown = new PropertySetAbstractValue();

        public static PropertySetAbstractValue GetInstance(ImmutableArray<PropertySetAbstractValueKind> propertyAbstractValues)
        {
            // TODO: Pool instances.
            return new PropertySetAbstractValue(propertyAbstractValues);
        }

        public static PropertySetAbstractValue GetInstance(ArrayBuilder<PropertySetAbstractValueKind> arrayBuilder)
        {
            // TODO: Pool instances.
            return new PropertySetAbstractValue(arrayBuilder.ToImmutable());
        }

        private PropertySetAbstractValue(ImmutableArray<PropertySetAbstractValueKind> propertyAbstractValues)
        {
            this.KnownPropertyAbstractValues = propertyAbstractValues;
        }

        private PropertySetAbstractValue()
        {
            this.KnownPropertyAbstractValues = ImmutableArray<PropertySetAbstractValueKind>.Empty;
        }

        /// <summary>
        /// Individual values of the set of properties being tracked.
        /// </summary>
        /// <remarks>
        /// Order of the array is the same as the provided <see cref="PropertyMapper"/>s.
        /// </remarks>
        private ImmutableArray<PropertySetAbstractValueKind> KnownPropertyAbstractValues { get; }

        public int KnownValuesCount => this.KnownPropertyAbstractValues.Length;

        public PropertySetAbstractValueKind this[int index]
        {
            get
            {
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (index >= this.KnownValuesCount)
                {
                    return PropertySetAbstractValueKind.Unknown;
                }
                else
                {
                    return this.KnownPropertyAbstractValues[index];
                }
            }
        }

        public PropertySetAbstractValue ReplaceAt(int index, PropertySetAbstractValueKind kind)
        {
            Debug.Assert(index >= 0);

            int newLength;
            if (index >= this.KnownPropertyAbstractValues.Length)
            {
                newLength = index + 1;
            }
            else
            {
                newLength = this.KnownPropertyAbstractValues.Length;
            }

            ArrayBuilder<PropertySetAbstractValueKind> kinds = ArrayBuilder<PropertySetAbstractValueKind>.GetInstance(newLength);
            try
            {
                kinds.AddRange(this.KnownPropertyAbstractValues);

                for (int i = kinds.Count; i < newLength; i++)
                {
                    kinds.Add(PropertySetAbstractValueKind.Unknown);
                }

                kinds[index] = kind;
                return GetInstance(kinds);
            }
            finally
            {
                kinds.Free();
            }
        }
    }
}
