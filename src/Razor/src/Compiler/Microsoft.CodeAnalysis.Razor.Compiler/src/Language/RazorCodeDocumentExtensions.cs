// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language;

public static class RazorCodeDocumentExtensions
{
    /// <summary>
    /// Returns the implementation C# document for <paramref name="codeDocument"/> -- the generated code
    /// containing the render method and, for a non-component document, the complete class. A component
    /// that split has its declaration surface in a separate declaration document; the implementation
    /// document is the code the source generator emits as the document's primary output.
    /// </summary>
    public static RazorCSharpDocument GetCSharpDocument(this RazorCodeDocument codeDocument)
        => codeDocument.GetRequiredCSharpDocument(declarationDocument: false);

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
    /// Returns the content of the <c>@inherits</c> directive if present in a legacy <c>.cshtml</c>
    /// document's syntax tree, or <see langword="null"/> for non-legacy files or when absent.
    /// </summary>
    internal static string? GetInheritsDirectiveValue(this RazorCodeDocument codeDocument)
    {
        if (!codeDocument.FileKind.IsLegacy())
        {
            return null;
        }

        var syntaxTree = codeDocument.GetSyntaxTree();
        if (syntaxTree is null)
        {
            return null;
        }

        // Check the main document first -- its @inherits overrides any from imports.
        var inheritsValue = FindInheritsDirective(syntaxTree);
        if (inheritsValue is not null)
        {
            return inheritsValue;
        }

        // Check import syntax trees. The last import's @inherits wins (most specific scope).
        if (codeDocument.TryGetImportSyntaxTrees(out var importSyntaxTrees))
        {
            for (var i = importSyntaxTrees.Length - 1; i >= 0; i--)
            {
                inheritsValue = FindInheritsDirective(importSyntaxTrees[i]);
                if (inheritsValue is not null)
                {
                    return inheritsValue;
                }
            }
        }

        return null;

        static string? FindInheritsDirective(RazorSyntaxTree tree)
        {
            foreach (var node in tree.Root.DescendantNodes())
            {
                if (node is RazorDirectiveSyntax
                    {
                        DirectiveDescriptor: var descriptor,
                        Body: RazorDirectiveBodySyntax { CSharpCode: { } csharpCode }
                    } &&
                    descriptor == InheritsDirective.Directive)
                {
                    return csharpCode.GetContent()?.Trim();
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Returns all <c>@using</c> directives from the document and its import files, in the
    /// same order as <see cref="DefaultRazorIntermediateNodeLoweringPhase"/> emits them
    /// during code generation: imports first (outer to inner), then the page's own usings.
    /// </summary>
    internal static ImmutableArray<string> GetUsingDirectives(this RazorCodeDocument codeDocument)
    {
        var syntaxTree = codeDocument.GetSyntaxTree();
        if (syntaxTree is null)
        {
            return [];
        }

        var usings = new List<string>();

        if (codeDocument.TryGetImportSyntaxTrees(out var importSyntaxTrees))
        {
            foreach (var importTree in importSyntaxTrees)
            {
                CollectUsings(importTree, usings);
            }
        }

        CollectUsings(syntaxTree, usings);

        return [.. usings];

        static void CollectUsings(RazorSyntaxTree tree, List<string> usings)
        {
            foreach (var node in tree.Root.DescendantNodes())
            {
                if (node is RazorUsingDirectiveSyntax usingDirective)
                {
                    var content = usingDirective.Body?.GetContent()?.Trim();
                    if (content is not null && content.StartsWith("using ", StringComparison.Ordinal))
                    {
                        var ns = content.Substring("using ".Length).TrimEnd(';').Trim();
                        if (ns.Length > 0)
                        {
                            usings.Add(ns);
                        }
                    }
                }
            }
        }
    }

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

        // Contributions are the descriptors each directive made available, not necessarily ones
        // used in the document. The same descriptor can be contributed by an alias and an ordinary using.
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
                // The context contains the ordinary and alias candidates available for binding,
                // including those from imports. Without alias candidates, descriptor overlap is enough.
                if (!codeDocument.TryGetTagHelperContext(out var context) || !context.HasAliases)
                {
                    return AnyContributedTagHelperIsReferenced(contribution.ContributedTagHelpers, referencedTagHelpers);
                }

                // If none of the descriptors made available by this directive are referenced,
                // the directive is unused regardless of tag spelling. Avoid walking the tree in that case.
                if (!AnyContributedTagHelperIsReferenced(contribution.ContributedTagHelpers, referencedTagHelpers))
                {
                    return false;
                }

                // Only check the requested directive. Alias and ordinary directives take separate
                // paths below, so each call walks the rewritten tree at most once.
                if (TryGetUsingAlias(directive, out var alias))
                {
                    // For an alias directive, a contributed descriptor must appear on a tag using
                    // this alias's spelling. Referencing that descriptor as <Button> doesn't prove
                    // that UI = Library was used; referencing it as <UI.Button> does.
                    foreach (var node in codeDocument.GetRequiredTagHelperRewrittenSyntaxTree().Root.DescendantNodes())
                    {
                        if (node is not MarkupTagHelperElementSyntax { TagHelperInfo.BindingResult: var binding })
                        {
                            continue;
                        }

                        var tagName = binding.TagName.AsSpan();
                        foreach (var descriptor in binding.TagHelpers)
                        {
                            // Child-content elements take precedence over component matches.
                            // Their name can coincide with an alias, but only the parent uses that alias.
                            if (descriptor.Kind == TagHelperKind.ChildContent)
                            {
                                tagName = binding.ParentTagName.AsSpan();
                                break;
                            }
                        }

                        if (!tagName.Equals(alias, StringComparison.Ordinal) &&
                            !(tagName.Length > alias.Length &&
                              tagName.StartsWith(alias, StringComparison.Ordinal) &&
                              tagName[alias.Length] == '.'))
                        {
                            continue;
                        }

                        if (AnyContributedTagHelperIsReferenced(contribution.ContributedTagHelpers, binding.TagHelpers))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                // Prefer aliases for component names, but not for unrelated helpers on the same tag.
                // For example, <UI.Button @oncustom="..."> can still need an event-handler namespace.
                foreach (var node in codeDocument.GetRequiredTagHelperRewrittenSyntaxTree().Root.DescendantNodes())
                {
                    if (node is not MarkupTagHelperElementSyntax { TagHelperInfo.BindingResult: var binding })
                    {
                        continue;
                    }

                    foreach (var descriptor in binding.TagHelpers)
                    {
                        if (!contribution.ContributedTagHelpers.Contains(descriptor))
                        {
                            continue;
                        }

                        // Child-content names belong to the parent component. <ChildContent> under
                        // <UI.Button> doesn't make an ordinary import of Button's namespace necessary.
                        var componentTagName = descriptor.Kind == TagHelperKind.ChildContent
                            ? binding.ParentTagName
                            : binding.TagName;
                        var usesComponentName = descriptor.Kind == TagHelperKind.Component ||
                            descriptor.Kind == TagHelperKind.ChildContent ||
                            descriptor is { Kind: TagHelperKind.Bind, ClassifyAttributesOnly: false };
                        if (!usesComponentName || componentTagName is null || !context.IsAliasTagName(componentTagName))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        return false;

        static bool TryGetUsingAlias(BaseRazorDirectiveSyntax directive, out ReadOnlySpan<char> alias)
        {
            alias = default;
            foreach (var child in directive.DescendantNodes())
            {
                if (child is CSharpStatementLiteralSyntax { ChunkGenerator: AddImportChunkGenerator { IsStatic: false } addImport })
                {
                    var equalsIndex = addImport.ParsedNamespace.IndexOf('=');
                    if (equalsIndex <= 0)
                    {
                        continue;
                    }

                    alias = addImport.ParsedNamespace.AsSpan(0, equalsIndex).Trim();
                    if (alias.StartsWith('@'))
                    {
                        alias = alias[1..];
                    }

                    return !alias.IsEmpty;
                }
            }

            return false;
        }

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
