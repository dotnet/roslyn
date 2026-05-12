// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal partial class MarkupElementSyntax
{
    public override BaseMarkupStartTagSyntax? StartTag
        => MarkupStartTag;

    internal override BaseMarkupElementSyntax WithStartTagCore(BaseMarkupStartTagSyntax startTag)
        => WithMarkupStartTag((MarkupStartTagSyntax?)startTag);

    public override BaseMarkupEndTagSyntax? EndTag
        => MarkupEndTag;

    internal override BaseMarkupElementSyntax WithEndTagCore(BaseMarkupEndTagSyntax endTag)
        => WithMarkupEndTag((MarkupEndTagSyntax?)endTag);
}
