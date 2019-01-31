using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    /// <summary>
    /// Information for mapping an object's property assigned value to a <see cref="PropertySetAbstractValue"/>.
    /// </summary>
    internal sealed class PropertyMapper
    {
        public PropertyMapper(string propertyName, Func<ValueContentAbstractValue, PropertySetAbstractValue> mapFromValueContentAbstractValue)
        {
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            MapFromValueContentAbstractValue = mapFromValueContentAbstractValue ?? throw new ArgumentNullException(nameof(mapFromValueContentAbstractValue));
        }

        public PropertyMapper(string propertyName, Func<bool, PropertySetAbstractValue> mapFromNullOrNonNull)
        {
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            MapFromNullOrNonNull = mapFromNullOrNonNull ?? throw new ArgumentNullException(nameof(mapFromNullOrNonNull));
        }

        public string PropertyName { get; }

        public Func<ValueContentAbstractValue, PropertySetAbstractValue> MapFromValueContentAbstractValue { get; }

        public Func<bool, PropertySetAbstractValue> MapFromNullOrNonNull { get; }
    }
}
