// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal record RazorHtmlDependentCompletionContext : RazorCompletionContext
{
    public HashSet<string> HtmlLabels { get; }

    public RazorHtmlDependentCompletionContext(RazorCompletionContext baseContext, HashSet<string> htmlLabels)
        : base(baseContext.CodeDocument, baseContext.AbsoluteIndex, baseContext.Owner, baseContext.SyntaxTree,
               baseContext.TagHelperDocumentContext, baseContext.Reason, baseContext.Options)
    {
        HtmlLabels = htmlLabels;
    }
}
