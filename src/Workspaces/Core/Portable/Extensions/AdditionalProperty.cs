using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal class AdditionalProperty
    {
        public static readonly AdditionalProperty None = new AdditionalProperty();

        public string Label { get; set; }
        public string Value { get; set; }

        public AdditionalProperty()
        {
        }

        public AdditionalProperty(string label, string value)
        {
            Label = label;
            Value = value;
        }
    }
}
