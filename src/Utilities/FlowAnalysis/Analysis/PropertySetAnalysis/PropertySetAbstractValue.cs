// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    internal class PropertySetAbstractValue
    {
        public PropertySetAbstractValue(ImmutableArray<PropertySetAbstractValueKind> propertyAbstractValues)
        {
            PropertyAbstractValues = propertyAbstractValues;
        }

        /// <summary>
        /// Individual values of the set of properties being tracked.
        /// </summary>
        /// <remarks>
        /// Order of the array is the same as the provided <see cref="PropertyMapper"/>s.
        /// </remarks>
        public ImmutableArray<PropertySetAbstractValueKind> PropertyAbstractValues { get; }
    }
}
