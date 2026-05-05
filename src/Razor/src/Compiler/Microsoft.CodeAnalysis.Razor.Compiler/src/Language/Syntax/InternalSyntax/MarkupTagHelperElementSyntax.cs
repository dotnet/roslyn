// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

internal sealed partial class MarkupTagHelperElementSyntax
{
    public override BaseMarkupStartTagSyntax StartTag
        => TagHelperStartTag;

    public override BaseMarkupEndTagSyntax EndTag
        => TagHelperEndTag;
}
