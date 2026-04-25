// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class RazorHtmlDocument
{
    public SourceText Text { get; }

    public RazorHtmlDocument(SourceText text)
    {
        ArgHelper.ThrowIfNull(text);

        Text = text;
    }
}
