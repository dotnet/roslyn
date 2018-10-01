// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
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

            public override PropertySetAbstractValue Bottom => PropertySetAbstractValue.NotApplicable;

            public override PropertySetAbstractValue UnknownOrMayBeValue => PropertySetAbstractValue.NotApplicable;

            public override int Compare(PropertySetAbstractValue oldValue, PropertySetAbstractValue newValue)
            {
                return Comparer<PropertySetAbstractValue>.Default.Compare(oldValue, newValue);
            }

            public override PropertySetAbstractValue Merge(PropertySetAbstractValue value1, PropertySetAbstractValue value2)
            {
                if (value1 == value2)
                {
                    return value1;
                }
                else
                {
                    return PropertySetAbstractValue.MaybeFlagged;
                }
            }
        }
    }
}
