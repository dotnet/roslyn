// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// Initializes a <see cref="PropertyMapper"/> that maps a property's assigned value's <see cref="ValueContentAbstractValue"/> to a <see cref="PropertySetAbstractValueKind"/>.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="mapFromValueContentAbstractValueCallback">Callback that implements the mapping.</param>
        /// <param name="propertyIndex">Internal index into the <see cref="PropertySetAbstractValueKind"/> array.</param>
        /// <remarks>This overload is useful if there are properties that effectively aliases of the same underlying value.</remarks>
        public PropertyMapper(string propertyName, ValueContentAbstractValueCallback mapFromValueContentAbstractValueCallback, int propertyIndex)
        {
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            MapFromValueContentAbstractValue = mapFromValueContentAbstractValueCallback ?? throw new ArgumentNullException(nameof(mapFromValueContentAbstractValueCallback));
            PropertyIndex = propertyIndex;
            if (propertyIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(propertyIndex), "propertyIndex must be non-negative");
            }
        }

        /// <summary>
        /// Initializes a <see cref="PropertyMapper"/> that maps a property's assigned value's <see cref="NullAbstractValue"/> to a <see cref="PropertySetAbstractValueKind"/>.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="mapFromPointsToAbstractValueCallback">Callback that implements the mapping.</param>
        public PropertyMapper(string propertyName, PointsToAbstractValueCallback mapFromPointsToAbstractValueCallback)
        {
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            MapFromPointsToAbstractValue = mapFromPointsToAbstractValueCallback ?? throw new ArgumentNullException(nameof(mapFromPointsToAbstractValueCallback));
        }

        /// <summary>
        /// Initializes a <see cref="PropertyMapper"/> that maps a property's assigned value's <see cref="NullAbstractValue"/> to a <see cref="PropertySetAbstractValueKind"/>.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="mapFromPointsToAbstractValueCallback">Callback that implements the mapping.</param>
        /// <param name="propertyIndex">Internal index into the <see cref="PropertySetAbstractValueKind"/> array.</param>
        /// <remarks>This overload is useful if there are properties that effectively aliases of the same underlying value.</remarks>
        public PropertyMapper(string propertyName, PointsToAbstractValueCallback mapFromPointsToAbstractValueCallback, int propertyIndex)
        {
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            MapFromPointsToAbstractValue = mapFromPointsToAbstractValueCallback ?? throw new ArgumentNullException(nameof(mapFromPointsToAbstractValueCallback));
            PropertyIndex = propertyIndex;
            if (propertyIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(propertyIndex), "propertyIndex must be non-negative");
            }
        }

        /// <summary>
        /// Doesn't construct.
        /// </summary>
        private PropertyMapper()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Name of the property.
        /// </summary>
        internal string PropertyName { get; }

        internal int PropertyIndex { get; } = -1;

        /// <summary>
        /// Callback for mapping from <see cref="ValueContentAbstractValue"/> to a <see cref="PropertySetAbstractValueKind"/>, or null.
        /// </summary>
        internal ValueContentAbstractValueCallback? MapFromValueContentAbstractValue { get; }

        /// <summary>
        /// Callback for mapping from <see cref="PointsToAbstractValue"/> to a <see cref="PropertySetAbstractValueKind"/>, or null.
        /// </summary>
        internal PointsToAbstractValueCallback? MapFromPointsToAbstractValue { get; }

        /// <summary>
        /// Indicates that this <see cref="PropertyMapper"/> uses <see cref="ValueContentAbstractValue"/>s.
        /// </summary>
        internal bool RequiresValueContentAnalysis => this.MapFromValueContentAbstractValue != null;

        public override int GetHashCode()
        {
            return RoslynHashCode.Combine(
                this.PropertyName.GetHashCodeOrDefault(),
                this.MapFromValueContentAbstractValue.GetHashCodeOrDefault(),
                this.MapFromPointsToAbstractValue.GetHashCodeOrDefault());
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as PropertyMapper);
        }

        public bool Equals(PropertyMapper? other)
        {
            return other != null
                && this.PropertyName == other.PropertyName
                && this.MapFromValueContentAbstractValue == other.MapFromValueContentAbstractValue
                && this.MapFromPointsToAbstractValue == other.MapFromPointsToAbstractValue;
        }
    }
}
