// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

internal class TagHelperHtmlAttributeRuntimeNodeWriter : RuntimeNodeWriter
{
    public static new readonly TagHelperHtmlAttributeRuntimeNodeWriter Instance = new TagHelperHtmlAttributeRuntimeNodeWriter();

    public override string WriteAttributeValueMethod => "AddHtmlAttributeValue";

    private TagHelperHtmlAttributeRuntimeNodeWriter()
    {
    }
}
