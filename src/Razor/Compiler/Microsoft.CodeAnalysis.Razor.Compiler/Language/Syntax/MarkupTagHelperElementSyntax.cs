// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal partial class MarkupTagHelperElementSyntax
{
    public override BaseMarkupStartTagSyntax? StartTag
        => TagHelperStartTag;

    internal override BaseMarkupElementSyntax WithStartTagCore(BaseMarkupStartTagSyntax startTag)
        => WithTagHelperStartTag((MarkupTagHelperStartTagSyntax)startTag);

    public override BaseMarkupEndTagSyntax? EndTag
        => TagHelperEndTag;

    internal override BaseMarkupElementSyntax WithEndTagCore(BaseMarkupEndTagSyntax endTag)
        => WithTagHelperEndTag((MarkupTagHelperEndTagSyntax?)endTag);
}
