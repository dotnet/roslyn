// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using RazorSyntaxKind = Microsoft.AspNetCore.Razor.Language.SyntaxKind;
using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;
using RazorSyntaxToken = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxToken;

namespace Microsoft.CodeAnalysis.Razor.GoToDefinition;

internal sealed record BoundTagHelperResult(TagHelperDescriptor ElementDescriptor, BoundAttributeDescriptor? AttributeDescriptor);

internal static class RazorComponentDefinitionHelpers
{
    /// <summary>
    /// Gets bound tag helpers that might apply to the specified index
    /// </summary>
    /// <remarks>
    /// This method will not match component attribute tag helpers
    /// </remarks>
    public static bool TryGetBoundTagHelpers(
        RazorCodeDocument codeDocument, int absoluteIndex, ILogger logger,
        out ImmutableArray<BoundTagHelperResult> descriptors)
    {
        descriptors = default;

        var root = codeDocument.GetRequiredSyntaxRoot();

        var innermostNode = root.FindInnermostNode(absoluteIndex);
        if (innermostNode is null)
        {
            logger.LogInformation($"Could not locate innermost node at index, {absoluteIndex}.");
            return false;
        }

        var tagHelperNode = innermostNode.FirstAncestorOrSelf<RazorSyntaxNode>(IsTagHelperNode);
        if (tagHelperNode is null)
        {
            logger.LogInformation($"Could not locate ancestor of type MarkupTagHelperStartTag or MarkupTagHelperEndTag.");
            return false;
        }

        if (!TryGetTagName(tagHelperNode, out var tagName))
        {
            logger.LogInformation($"Could not retrieve name of start or end tag.");
            return false;
        }

        var nameSpan = tagName.Span;
        string? propertyName = null;

        if (tagHelperNode.Parent is not MarkupTagHelperElementSyntax tagHelperElement)
        {
            logger.LogInformation($"Parent of start or end tag is not a MarkupTagHelperElement.");
            return false;
        }

        if (tagHelperElement.TagHelperInfo?.BindingResult is not TagHelperBinding binding)
        {
            logger.LogInformation($"MarkupTagHelperElement does not contain TagHelperInfo.");
            return false;
        }

        using var descriptorsBuilder = new PooledArrayBuilder<BoundTagHelperResult>();

        foreach (var boundTagHelper in binding.TagHelpers.Where(d => !d.IsAttributeDescriptor()))
        {
            var requireAttributeMatch = false;
            if (boundTagHelper.Kind != TagHelperKind.Component &&
                tagHelperNode is MarkupTagHelperStartTagSyntax startTag)
            {
                // Include attributes where the end index also matches, since GetSyntaxNodeAsync will consider that the start tag but we behave
                // as if the user wants to go to the attribute definition.
                // ie: <Component attribute$$></Component>
                var selectedAttribute = startTag.Attributes.FirstOrDefault(absoluteIndex, static (a, absoluteIndex) => a.Span.Contains(absoluteIndex) || a.Span.End == absoluteIndex);

                requireAttributeMatch = selectedAttribute is not null;

                // If we're on an attribute then just validate against the attribute name
                switch (selectedAttribute)
                {
                    case MarkupTagHelperAttributeSyntax attribute:
                        // Normal attribute, ie <Component attribute=value />
                        nameSpan = attribute.Name.Span;
                        propertyName = attribute.TagHelperAttributeInfo.Name;
                        break;

                    case MarkupMinimizedTagHelperAttributeSyntax minimizedAttribute:
                        // Minimized attribute, ie <Component attribute />
                        nameSpan = minimizedAttribute.Name.Span;
                        propertyName = minimizedAttribute.TagHelperAttributeInfo.Name;
                        break;
                }
            }

            if (!nameSpan.IntersectsWith(absoluteIndex))
            {
                logger.LogInformation($"Tag name or attributes' span does not intersect with index, {absoluteIndex}.");
                continue;
            }

            var boundAttribute = propertyName is not null
                ? boundTagHelper.BoundAttributes.FirstOrDefault(propertyName, static (a, propertyName) => a.Name == propertyName)
                : null;

            if (requireAttributeMatch && boundAttribute is null)
            {
                // The user is on an attribute, but we couldn't find a matching BoundAttributeDescriptor.
                continue;
            }

            descriptorsBuilder.Add(new BoundTagHelperResult(boundTagHelper, boundAttribute));
        }

        if (descriptorsBuilder.Count == 0)
        {
            return false;
        }

        descriptors = descriptorsBuilder.ToImmutableAndClear();

        return true;

        static bool IsTagHelperNode(RazorSyntaxNode node)
        {
            return node.Kind is RazorSyntaxKind.MarkupTagHelperStartTag or RazorSyntaxKind.MarkupTagHelperEndTag;
        }

        static bool TryGetTagName(RazorSyntaxNode node, out RazorSyntaxToken tagName)
        {
            tagName = node switch
            {
                MarkupTagHelperStartTagSyntax tagHelperStartTag => tagHelperStartTag.Name,
                MarkupTagHelperEndTagSyntax tagHelperEndTag => tagHelperEndTag.Name,
                _ => default
            };

            return tagName != default;
        }
    }

    public static async Task<LspRange?> TryGetPropertyRangeAsync(
        IDocumentSnapshot documentSnapshot,
        string propertyName,
        IDocumentMappingService documentMappingService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // Process the C# tree and find the property that matches the name.
        // We don't worry about parameter attributes here for two main reasons:
        //   1. We don't have symbolic information, so the best we could do would be checking for any
        //      attribute named Parameter, regardless of which namespace. It also means we would have
        //      to do more checks for all of the various ways that the attribute could be specified
        //      (eg fully qualified, aliased, etc.)
        //   2. Since C# doesn't allow multiple properties with the same name, and we're doing a case
        //      sensitive search, we know the property we find is the one the user is trying to encode in a
        //      tag helper attribute. If they don't have the [Parameter] attribute then the Razor compiler
        //      will error, but allowing them to Go To Def on that property regardless, actually helps
        //      them fix the error.

        var csharpSyntaxTree = await documentSnapshot.GetCSharpSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var root = await csharpSyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        if (root.TryGetClassDeclaration(out var classDeclaration))
        {
            var property = classDeclaration
                .Members
                .OfType<PropertyDeclarationSyntax>()
                .Where(p => p.Identifier.ValueText.Equals(propertyName, StringComparison.Ordinal))
                .FirstOrDefault();

            if (property is null)
            {
                // The property probably exists in a partial class
                logger.LogInformation($"Could not find property in the generated source. Comes from partial?");
                return null;
            }

            var csharpDocument = codeDocument.GetRequiredCSharpDocument();
            var range = csharpDocument.Text.GetRange(property.Identifier.Span);
            if (documentMappingService.TryMapToRazorDocumentRange(csharpDocument, range, out var originalRange))
            {
                return originalRange;
            }

            logger.LogInformation($"Property found but couldn't map its location.");
        }

        logger.LogInformation($"Generated C# was not in expected shape (CompilationUnit [-> Namespace] -> Class)");

        return null;
    }
}
