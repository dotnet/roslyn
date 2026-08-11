// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal enum FormattingBlockKind
{
    // Code
    Statement,
    Directive,
    Expression,

    // Markup
    Markup,
    Template,

    // Special
    Comment,
    Tag,
    HtmlComment
}
