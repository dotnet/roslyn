using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    /// <summary>
    /// Information for mapping an object's property's assigned value to a <see cref="PropertySetAbstractValueKind"/>.
    /// </summary>
    internal sealed class PropertyMapper
    {
        public delegate PropertySetAbstractValueKind ValueContentCallback(ValueContentAbstractValue valueContentAbstractValue);
        public delegate PropertySetAbstractValueKind IsNullCallback(bool isNull);

        /// <summary>
        /// Initializes a PropertyMapper that examines a 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="mapFromValueContentAbstractValue"></param>
        public PropertyMapper(string propertyName, ValueContentCallback mapFromValueContentAbstractValue)
        {
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            MapFromValueContentAbstractValue = mapFromValueContentAbstractValue ?? throw new ArgumentNullException(nameof(mapFromValueContentAbstractValue));
        }

        public PropertyMapper(string propertyName, IsNullCallback mapFromNullOrNonNull)
        {
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            MapFromNullOrNonNull = mapFromNullOrNonNull ?? throw new ArgumentNullException(nameof(mapFromNullOrNonNull));
        }

        /// <summary>
        /// Doesn't construct.
        /// </summary>
        private PropertyMapper()
        {
        }

        public string PropertyName { get; }

        public ValueContentCallback MapFromValueContentAbstractValue { get; }

        public IsNullCallback MapFromNullOrNonNull { get; }
    }
}
