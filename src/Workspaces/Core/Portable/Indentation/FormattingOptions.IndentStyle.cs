// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Formatting
{
    public static partial class FormattingOptions
    {
        // Publicly exposed.  Keep in sync with <see cref="FormattingOptions2.IndentStyle"/> in the CodeStyle layer.
        public enum IndentStyle
        {
            None = 0,
            Block = 1,
            Smart = 2
        }
    }
}
