// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies the formatting for a numeric literal.
    /// </summary>
    /// <seealso cref="SymbolDisplayConstantValueOptions"/>
    public enum NumericFormat
    {
        /// <summary>
        /// Format the number in decimal.
        /// </summary>
        Decimal,

        /// <summary>
        /// Format the number in hexadecimal.
        /// </summary>
        Hexadecimal
    }
}
