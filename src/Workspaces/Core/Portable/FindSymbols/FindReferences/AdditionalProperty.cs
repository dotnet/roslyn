using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal class AdditionalProperty
    {
        public string Label { get; }
        public string Value { get; }

        public AdditionalProperty(string label, string value)
        {
            Label = label;
            Value = value;
        }
    }
}
