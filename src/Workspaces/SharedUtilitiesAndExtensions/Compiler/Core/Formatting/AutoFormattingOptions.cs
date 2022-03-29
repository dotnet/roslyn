// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// Solution-wide format-on-type options.
    /// </summary>
    internal readonly partial record struct AutoFormattingOptions(
        bool FormatOnReturn,
        bool FormatOnTyping,
        bool FormatOnSemicolon,
        bool FormatOnCloseBrace);
}
