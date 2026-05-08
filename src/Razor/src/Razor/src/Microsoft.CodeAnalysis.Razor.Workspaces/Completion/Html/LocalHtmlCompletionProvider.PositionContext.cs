// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace Microsoft.CodeAnalysis.Razor.Completion.Html;

internal static partial class LocalHtmlCompletionProvider
{
    internal readonly record struct PositionContext(
        PositionKind Kind,
        LspRange ReplacementRange,
        RazorSyntaxNode Owner,
        string? TagName = null,
        string? AttributeName = null,
        string? ParentTagName = null,
        string? TypedAttributePrefix = null);
}
