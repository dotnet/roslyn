// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal record RazorCompletionContext(
    RazorCodeDocument CodeDocument,
    int AbsoluteIndex,
    RazorSyntaxNode? Owner,
    RazorSyntaxTree SyntaxTree,
    TagHelperDocumentContext TagHelperDocumentContext,
    CompletionReason Reason = CompletionReason.Invoked,
    RazorCompletionOptions Options = default,
    HashSet<string>? ExistingCompletions = null)
{
}
