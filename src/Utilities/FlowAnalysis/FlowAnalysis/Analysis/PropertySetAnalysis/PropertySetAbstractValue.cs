// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    /// <summary>
    /// Abstract value for a set properties on an object instance, with each
    /// individual property represented by a <see 
    /// cref="PropertySetAbstractValueKind"/>.
    /// </summary>
    /// <remarks>
    /// Note that <see cref="KnownPropertyAbstractValues"/> may be
    /// "incomplete", i.e. it doesn't cover all properties.  In such cases,
    /// missing elements are implicitly <see 
    /// cref="PropertySetAbstractValueKind.Unknown"/>.
    /// 
    /// The reason for the "sparse" array is so that the Unknown value doesn't
    /// have to be aware of how many properties are being tracked.
    /// </remarks>
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

        public static PropertySetAbstractValue GetInstance(PropertySetAbstractValueKind v1)
        {
            // TODO: Pool instances.
            return GetInstance(ImmutableArray.Create<PropertySetAbstractValueKind>(v1));
        }

        public static PropertySetAbstractValue GetInstance(PropertySetAbstractValueKind v1, PropertySetAbstractValueKind v2)
        {
            // TODO: Pool instances.
            return GetInstance(ImmutableArray.Create<PropertySetAbstractValueKind>(v1, v2));
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

        /// <summary>
        /// Count of how many properties' abstract values are tracked by this instance.
        /// </summary>
        public int KnownValuesCount => this.KnownPropertyAbstractValues.Length;

        /// <summary>
        /// Gets an individual property's <see cref="PropertySetAbstractValueKind"/>.
        /// </summary>
        /// <param name="index">Index of the property, from the corresponding
        /// <see cref="PropertyMapperCollection"/>'s initialization.</param>
        /// <returns>The property's <see cref="PropertySetAbstractValueKind"/>.</returns>
        /// <remarks>If accessing an index greater than or equal to KnownValuesCount, the property's
        /// abstract value is implicitly <see cref="PropertySetAbstractValueKind.Unknown"/>.</remarks>
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

        internal PropertySetAbstractValue ReplaceAt(int index, PropertySetAbstractValueKind kind)
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

                while (kinds.Count < newLength)
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
