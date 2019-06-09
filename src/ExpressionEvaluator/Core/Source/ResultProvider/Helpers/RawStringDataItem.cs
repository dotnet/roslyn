// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Debugger;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    /// <summary>
    /// Data item to associate a computed raw string with a DkmClrValue.
    /// </summary>
    internal sealed class RawStringDataItem : DkmDataItem
    {
        public readonly string RawString;

        public RawStringDataItem(string rawString)
        {
            RawString = rawString;
        }
    }
}
