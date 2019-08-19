using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal class AdditionalProperty
    {
        public (string label, string value) propertyInfo;

        public static readonly AdditionalProperty None = new AdditionalProperty();

        public AdditionalProperty()
        {
        }

        public AdditionalProperty(string label, string value)
        {
            propertyInfo.label = label;
            propertyInfo.value = value;
        }
    }
}
