// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor
{
    internal readonly struct AutoFormattingOptionsWrapper
    {
        internal readonly AutoFormattingOptions UnderlyingObject;

        public AutoFormattingOptionsWrapper(AutoFormattingOptions underlyingObject)
            => UnderlyingObject = underlyingObject;

        public FormattingOptions.IndentStyle IndentStyle
            => UnderlyingObject.IndentStyle;
    }
}
