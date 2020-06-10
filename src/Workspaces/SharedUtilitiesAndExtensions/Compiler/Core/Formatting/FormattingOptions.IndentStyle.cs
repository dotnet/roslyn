// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Formatting
{
#if CODE_STYLE
    internal static class FormattingOptions
#else
    public static partial class FormattingOptions
#endif
    {
        public enum IndentStyle
        {
            None = 0,
            Block = 1,
            Smart = 2
        }
    }
}
