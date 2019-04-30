// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    /// <summary>
    /// Information for mapping an object's property's assigned value to a <see cref="PropertySetAbstractValueKind"/>.
    /// </summary>
#pragma warning disable CA1812 // Is too instantiated.
    internal sealed class PropertyMapper
#pragma warning restore CA1812
    {
        /// <summary>
        /// Mapping from <see cref="ValueContentAbstractValue"/> to a <see cref="PropertySetAbstractValueKind"/>
        /// </summary>
        /// <param name="valueContentAbstractValue">Property's assigned value's <see cref="ValueContentAbstractValue"/>.</param>
        /// <returns>What the property's assigned value should map to.</returns>
        public delegate PropertySetAbstractValueKind ValueContentAbstractValueCallback(ValueContentAbstractValue valueContentAbstractValue);

        /// <summary>
        /// Mapping from <see cref="PointsToAbstractValue"/> to a <see cref="PropertySetAbstractValueKind"/>
        /// </summary>
        /// <param name="pointsToAbstractValue">Property's assigned value's <see cref="PointsToAbstractValue"/>.</param>
        /// <returns>What the property's assigned value should map to.</returns>
        public delegate PropertySetAbstractValueKind PointsToAbstractValueCallback(PointsToAbstractValue pointsToAbstractValue);

        /// <summary>
        /// Initializes a <see cref="PropertyMapper"/> that maps a property's assigned value's <see cref="ValueContentAbstractValue"/> to a <see cref="PropertySetAbstractValueKind"/>.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="mapFromValueContentAbstractValueCallback">Callback that implements the mapping.</param>
        public PropertyMapper(string propertyName, ValueContentAbstractValueCallback mapFromValueContentAbstractValueCallback)
        {
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            MapFromValueContentAbstractValue = mapFromValueContentAbstractValueCallback ?? throw new ArgumentNullException(nameof(mapFromValueContentAbstractValueCallback));
        }

        /// <summary>
        /// Initializes a <see cref="PropertyMapper"/> that maps a property's assigned value's <see cref="NullAbstractValue"/> to a <see cref="PropertySetAbstractValueKind"/>.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="mapFromNullAbstractValueCallback">Callback that implements the mapping.</param>
        public PropertyMapper(string propertyName, PointsToAbstractValueCallback mapFromNullAbstractValueCallback)
        {
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            MapFromPointsToAbstractValue = mapFromNullAbstractValueCallback ?? throw new ArgumentNullException(nameof(mapFromNullAbstractValueCallback));
        }

        /// <summary>
        /// Doesn't construct.
        /// </summary>
        private PropertyMapper()
        {
        }

        /// <summary>
        /// Name of the property.
        /// </summary>
        internal string PropertyName { get; }

        /// <summary>
        /// Callback for mapping from <see cref="ValueContentAbstractValue"/> to a <see cref="PropertySetAbstractValueKind"/>, or null.
        /// </summary>
        internal ValueContentAbstractValueCallback MapFromValueContentAbstractValue { get; }

        /// <summary>
        /// Callback for mapping from <see cref="PointsToAbstractValue"/> to a <see cref="PropertySetAbstractValueKind"/>, or null.
        /// </summary>
        internal PointsToAbstractValueCallback MapFromPointsToAbstractValue { get; }

        /// <summary>
        /// Indicates that this <see cref="PropertyMapper"/> uses <see cref="ValueContentAbstractValue"/>s.
        /// </summary>
        internal bool RequiresValueContentAnalysis => this.MapFromValueContentAbstractValue != null;

        public override int GetHashCode()
        {
            return HashUtilities.Combine(
                this.PropertyName.GetHashCodeOrDefault(),
                HashUtilities.Combine(this.MapFromValueContentAbstractValue.GetHashCodeOrDefault(),
                this.MapFromPointsToAbstractValue.GetHashCodeOrDefault()));
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as PropertyMapper);
        }

        public bool Equals(PropertyMapper other)
        {
            return other != null
                && this.PropertyName == other.PropertyName
                && this.MapFromValueContentAbstractValue == other.MapFromValueContentAbstractValue
                && this.MapFromPointsToAbstractValue == other.MapFromPointsToAbstractValue;
        }
    }
}
