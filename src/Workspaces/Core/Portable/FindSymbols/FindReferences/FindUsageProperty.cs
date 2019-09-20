// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FindSymbols.FindReferences
{
    internal readonly struct FindUsageProperty
    {
        public readonly string Label { get; }
        public readonly string Value { get; }

        public FindUsageProperty(string label, string value)
        {
            Label = label;
            Value = value;
        }
    }
}
