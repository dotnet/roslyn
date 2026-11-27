// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class FirstDirectiveHtmlLanguageCharacteristics : HtmlLanguageCharacteristics
{
    private FirstDirectiveHtmlLanguageCharacteristics()
    {
    }

    public static new FirstDirectiveHtmlLanguageCharacteristics Instance { get; } = new FirstDirectiveHtmlLanguageCharacteristics();

    public override HtmlTokenizer CreateTokenizer(SeekableTextReader source) => new DirectiveHtmlTokenizer(source);
}
