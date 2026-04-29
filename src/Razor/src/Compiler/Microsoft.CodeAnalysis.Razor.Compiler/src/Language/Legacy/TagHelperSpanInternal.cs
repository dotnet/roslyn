// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal readonly record struct TagHelperSpanInternal(SourceSpan Span, TagHelperBinding Binding)
{
    public TagHelperCollection TagHelpers => Binding.TagHelpers;
}
