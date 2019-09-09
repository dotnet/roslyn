// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FindSymbols.FindReferences
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
