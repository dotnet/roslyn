// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language;

public static class RazorCodeDocumentExtensions
{
    public static bool TryComputeClassName(this RazorCodeDocument codeDocument, [NotNullWhen(true)] out string? className)
    {
        var filePath = codeDocument.Source.RelativePath ?? codeDocument.Source.FilePath;
        if (filePath.IsNullOrEmpty())
        {
            className = null;
            return false;
        }

        className = CSharpIdentifier.GetClassNameFromPath(filePath);
        return className is not null;
    }

    public static bool TryGetNamespace(
        this RazorCodeDocument codeDocument,
        bool fallbackToRootNamespace,
        [NotNullWhen(true)] out string? @namespace)
        => codeDocument.TryGetNamespace(fallbackToRootNamespace, out @namespace, out _);

    public static bool TryGetNamespace(
        this RazorCodeDocument codeDocument,
        bool fallbackToRootNamespace,
        [NotNullWhen(true)] out string? @namespace,
        out SourceSpan? namespaceSpan)
        => codeDocument.TryGetNamespace(fallbackToRootNamespace, considerImports: true, out @namespace, out namespaceSpan);

    internal static bool IsImportsFile(this RazorCodeDocument codeDocument)
        => codeDocument.FileKind.IsComponentImport() ||
           (codeDocument.FileKind.IsLegacy() && string.Equals(Path.GetFileName(codeDocument.Source.FilePath), MvcImportProjectFeature.ImportsFileName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Returns whether the directive specified was involved in tag helper binding
    /// </summary>
    /// <remarks>
    /// If passed a directive that has no effect on tag helper binding at all, like `@if` or `@code`,
    /// this method will return false, correctly identifying that the tag helper didn't contribute.
    /// </remarks>
    internal static bool IsDirectiveUsed(this RazorCodeDocument codeDocument, BaseRazorDirectiveSyntax directive)
    {
        Debug.Assert(directive is RazorUsingDirectiveSyntax || directive.DirectiveBody.Keyword.GetContent() == SyntaxConstants.CSharp.AddTagHelperKeyword);

        // In imports files, all directives are considered used as usage tracking is only for source documents.
        if (codeDocument.IsImportsFile())
        {
            return true;
        }

        var contributions = codeDocument.GetDirectiveTagHelperContributions();
        // No contributions, means no directives contributed, so no directives are used
        if (contributions.IsDefaultOrEmpty)
        {
            return false;
        }

        // No tag helpers referenced, so no directives are used
        if (!codeDocument.TryGetReferencedTagHelpers(out var referencedTagHelpers))
        {
            return false;
        }

        foreach (var contribution in contributions)
        {
            if (contribution.DirectiveSpanStart == directive.SpanStart)
            {
                return AnyContributedTagHelperIsReferenced(contribution.ContributedTagHelpers, referencedTagHelpers);
            }
        }

        return false;

        static bool AnyContributedTagHelperIsReferenced(
            TagHelperCollection contributedTagHelpers,
            TagHelperCollection referencedTagHelpers)
        {
            foreach (var contributed in contributedTagHelpers)
            {
                if (referencedTagHelpers.Contains(contributed))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
