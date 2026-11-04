// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

internal static partial class RazorCodeDocumentExtensions
{
    public static Syntax.SyntaxNode GetRequiredSyntaxRoot(this RazorCodeDocument codeDocument)
        => codeDocument.GetRequiredTagHelperRewrittenSyntaxTree().Root;
}
