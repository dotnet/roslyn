// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    /// <summary>
    /// Maps a constructor invocation to <see cref="PropertySetAbstractValueKind"/>s for the properties being tracked by PropertySetAnalysis.
    /// </summary>
#pragma warning disable CA1812 // Is too instantiated.
    internal sealed class ConstructorMapper
#pragma warning restore CA1812
    {
        /// <summary>
        /// Mapping a constructor's arguments to a <see cref="PropertySetAbstractValue"/>.
        /// </summary>
        /// <param name="constructorMethodSymbol">Invoked constructor.</param>
        /// <param name="argumentValueContentAbstractValues"><see cref="ValueContentAbstractValue"/>s for the constructor's arguments.</param>
        /// <param name="argumentNullAbstractValues"><see cref="NullAbstractValue"/>s for the constructor's arguments.</param>
        /// <returns>Abstract value for PropertySetAnalysis, with <see cref="PropertySetAbstractValueKind"/>s in the same order as the <see cref="PropertyMapper"/>s in the <see cref="PropertyMapperCollection"/>.</returns>
        public delegate PropertySetAbstractValue ValueContentAbstractValueCallback(
            IMethodSymbol constructorMethodSymbol,
            IReadOnlyList<ValueContentAbstractValue> argumentValueContentAbstractValues,
            IReadOnlyList<NullAbstractValue> argumentNullAbstractValues);

        /// <summary>
        /// Mapping a constructor's arguments to a <see cref="PropertySetAbstractValue"/>.
        /// </summary>
        /// <param name="constructorMethodSymbol">Invoked constructor.</param>
        /// <param name="argumentNullAbstractValues"><see cref="NullAbstractValue"/>s for the constructor's arguments.</param>
        /// <returns>Abstract value for PropertySetAnalysis, with <see cref="PropertySetAbstractValueKind"/>s in the same order as the <see cref="PropertyMapper"/>s in the <see cref="PropertyMapperCollection"/>.</returns>
        public delegate PropertySetAbstractValue NullAbstractValueCallback(
            IMethodSymbol constructorMethodSymbol,
            IReadOnlyList<NullAbstractValue> argumentNullAbstractValues);

        /// <summary>
        /// Initializes a <see cref="ConstructorMapper"/> using constant <see cref="PropertySetAbstractValueKind"/>s whenever the type being tracked by PropertySetAnalysis is instantiated.
        /// </summary>
        /// <param name="propertyAbstractValues">Constant <see cref="PropertySetAbstractValueKind"/>s, in the same order that the corresponding <see cref="PropertyMapperCollection"/> was initialized with.</param>
        public ConstructorMapper(ImmutableArray<PropertySetAbstractValueKind> propertyAbstractValues)
        {
            this.PropertyAbstractValues = propertyAbstractValues;
        }

        /// <summary>
        /// Initializes a <see cref="ConstructorMapper"/> that maps a constructor invocation's arguments' <see cref="ValueContentAbstractValue"/>s to <see cref="PropertySetAbstractValueKind"/>s for the properties being tracked by PropertySetAnalysis.
        /// </summary>
        /// <param name="mapFromValueContentAbstractValue">Callback that implements the mapping.</param>
        public ConstructorMapper(ValueContentAbstractValueCallback mapFromValueContentAbstractValue)
        {
            this.MapFromValueContentAbstractValue = mapFromValueContentAbstractValue ?? throw new ArgumentNullException(nameof(mapFromValueContentAbstractValue));
            this.PropertyAbstractValues = ImmutableArray<PropertySetAbstractValueKind>.Empty;
        }

        /// <summary>
        /// Initializes a <see cref="ConstructorMapper"/> that maps a constructor invocation's arguments' <see cref="NullAbstractValue"/>s to <see cref="PropertySetAbstractValueKind"/>s for the properties being tracked by PropertySetAnalysis.
        /// </summary>
        /// <param name="mapFromNullAbstractValue">Callback that implements the mapping.</param>
        public ConstructorMapper(NullAbstractValueCallback mapFromNullAbstractValue)
        {
            this.MapFromNullAbstractValue = mapFromNullAbstractValue ?? throw new ArgumentNullException(nameof(mapFromNullAbstractValue));
            this.PropertyAbstractValues = ImmutableArray<PropertySetAbstractValueKind>.Empty;
        }

        /// <summary>
        /// Doesn't construct.
        /// </summary>
        private ConstructorMapper()
        {
        }

        internal ValueContentAbstractValueCallback MapFromValueContentAbstractValue { get; }

        internal NullAbstractValueCallback MapFromNullAbstractValue { get; }

        internal ImmutableArray<PropertySetAbstractValueKind> PropertyAbstractValues { get; }

        /// <summary>
        /// Indicates that this <see cref="ConstructorMapper"/> uses <see cref="ValueContentAbstractValue"/>s.
        /// </summary>
        internal bool RequiresValueContentAnalysis => this.MapFromValueContentAbstractValue != null;

        internal void Validate(int propertyCount)
        {
            if (!this.PropertyAbstractValues.IsEmpty)
            {
                if (this.PropertyAbstractValues.Length != propertyCount)
                {
                    throw new ArgumentException($"ConstructorMapper PropertyAbstractValues has invalid length (expected {propertyCount}, actual length {this.PropertyAbstractValues.Length})");
                }
            }
        }

        public override int GetHashCode()
        {
            return HashUtilities.Combine(this.PropertyAbstractValues,
                HashUtilities.Combine(this.MapFromValueContentAbstractValue.GetHashCodeOrDefault(),
                this.MapFromNullAbstractValue.GetHashCodeOrDefault()));
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ConstructorMapper);
        }

        public bool Equals(ConstructorMapper other)
        {
            return other != null
                && this.MapFromValueContentAbstractValue == other.MapFromValueContentAbstractValue
                && this.MapFromNullAbstractValue == other.MapFromNullAbstractValue
                && this.PropertyAbstractValues == other.PropertyAbstractValues;
        }
    }
}
