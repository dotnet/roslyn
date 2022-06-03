// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
