// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal readonly struct RazorAutoFormattingOptions
    {
        internal readonly AutoFormattingOptions UnderlyingObject;

        public RazorAutoFormattingOptions(AutoFormattingOptions underlyingObject)
            => UnderlyingObject = underlyingObject;

        public RazorAutoFormattingOptions(
            bool formatOnReturn,
            bool formatOnTyping,
            bool formatOnSemicolon,
            bool formatOnCloseBrace)
            : this(new AutoFormattingOptions()
            {
                FormatOnReturn = formatOnReturn,
                FormatOnTyping = formatOnTyping,
                FormatOnSemicolon = formatOnSemicolon,
                FormatOnCloseBrace = formatOnCloseBrace,
            })
        {
        }
    }
}
