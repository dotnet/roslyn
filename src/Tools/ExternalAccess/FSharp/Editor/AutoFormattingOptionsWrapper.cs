﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor
{
    internal readonly struct AutoFormattingOptionsWrapper
    {
        internal readonly AutoFormattingOptions UnderlyingObject;
        private readonly FormattingOptions2.IndentStyle _indentStyle;

        public AutoFormattingOptionsWrapper(AutoFormattingOptions underlyingObject, FormattingOptions2.IndentStyle indentStyle)
        {
            UnderlyingObject = underlyingObject;
            _indentStyle = indentStyle;
        }

        public FormattingOptions.IndentStyle IndentStyle
            => (FormattingOptions.IndentStyle)_indentStyle;
    }
}
