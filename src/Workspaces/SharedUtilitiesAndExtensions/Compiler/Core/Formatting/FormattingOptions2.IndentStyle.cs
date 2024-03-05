// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Formatting;

internal partial class FormattingOptions2
{
    /// <summary>
    /// For use in the shared CodeStyle layer.  Keep in syntax with FormattingOptions.IndentStyle.
    /// </summary>
    internal enum IndentStyle
    {
        None = 0,
        Block = 1,
        Smart = 2
    }
}
