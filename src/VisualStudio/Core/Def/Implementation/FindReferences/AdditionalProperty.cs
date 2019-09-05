using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Microsoft.VisualBasic.LanguageService.FindUsages
{
    internal struct AdditionalProperty
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
