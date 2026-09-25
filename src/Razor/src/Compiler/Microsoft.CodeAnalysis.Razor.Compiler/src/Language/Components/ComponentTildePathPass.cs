// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

/// <summary>
/// Rewrites <c>~/</c>-prefixed string literal attribute values into <c>Assets["path"]</c> C#
/// expressions, but only where the target has explicitly opted into asset-path expansion:
/// <list type="bullet">
///   <item>HTML element attributes whose (element, attribute) pair the runtime declared via
///     <c>[AcceptsAssetPath(elementName, attributeName)]</c> (surfaced as <see cref="AssetPathMetadata"/>
///     tag helpers).</item>
///   <item>Component parameters whose property is marked with <c>[AssetPath]</c>
///     (<see cref="PropertyMetadata.AcceptsAssetPath"/>).</item>
/// </list>
/// For example, <c>&lt;img src="~/images/logo.png" /&gt;</c> becomes <c>Assets["images/logo.png"]</c>
/// when (img, src) is opted in. When nothing is opted in, no expansion occurs.
/// </summary>
/// <remarks>
/// Runs after <c>ComponentLoweringPass</c> (Order=0) and the Order=50 passes, but before
/// <c>ComponentBindLoweringPass</c> (Order=100).
/// </remarks>
internal sealed class ComponentTildePathPass(RazorLanguageVersion version) : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    private const string TildePrefix = "~/";

    // The allowlist derives solely from the engine's discovered tag helpers, which are fixed for
    // the lifetime of this pass instance, so it is computed once and shared across every document.
    private Dictionary<string, HashSet<string>>? _allowedElementAttributes;

    public override int Order => 75;

    protected override void ExecuteCore(
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        CancellationToken cancellationToken)
    {
        if (!IsComponentDocument(documentNode))
        {
            return;
        }

        if (version < RazorLanguageVersion.Version_11_0)
        {
            return;
        }

        var rewriter = new Rewriter(GetAllowedElementAttributes(cancellationToken));
        rewriter.Visit(documentNode);
    }

    private Dictionary<string, HashSet<string>> GetAllowedElementAttributes(CancellationToken cancellationToken)
    {
        // The allowlist is idempotent, so a benign race just builds it twice; CompareExchange keeps
        // whichever result lands first and discards the other, and no lock is needed.
        if (_allowedElementAttributes is null)
        {
            Interlocked.CompareExchange(ref _allowedElementAttributes, BuildAllowedElementAttributes(cancellationToken), null);
        }

        return _allowedElementAttributes;
    }

    private Dictionary<string, HashSet<string>> BuildAllowedElementAttributes(CancellationToken cancellationToken)
    {
        // Maps an opted-in HTML element name to the set of its attributes that accept asset paths.
        // Both element and attribute comparisons are case-insensitive, matching HTML semantics. The
        // allowlist is global: it comes from the full set of discovered tag helpers (which includes
        // the AssetPathMetadata carriers produced from [AcceptsAssetPath]), independent of which tag
        // helpers are in scope for this document. An empty map means nothing is opted in.
        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        if (!Engine.TryGetFeature(out ITagHelperFeature? tagHelperFeature))
        {
            return result;
        }

        foreach (var tagHelper in tagHelperFeature.GetTagHelpers(cancellationToken))
        {
            if (tagHelper.Metadata is not AssetPathMetadata { Element: var element, Attribute: var attribute })
            {
                continue;
            }

            if (!result.TryGetValue(element, out var attributes))
            {
                attributes = new(StringComparer.OrdinalIgnoreCase);
                result.Add(element, attributes);
            }

            attributes.Add(attribute);
        }

        return result;
    }

    private sealed class Rewriter(Dictionary<string, HashSet<string>> allowedElementAttributes) : IntermediateNodeWalker
    {
        private string? _currentElementName;

        public override void VisitMarkupElement(MarkupElementIntermediateNode node)
        {
            var previous = _currentElementName;
            _currentElementName = node.TagName;

            base.VisitMarkupElement(node);

            _currentElementName = previous;
        }

        // HTML element attributes:
        //   HtmlAttributeIntermediateNode
        //     -> HtmlAttributeValueIntermediateNode (exactly one child)
        //          -> IntermediateToken (Content starts with "~/")
        public override void VisitHtmlAttribute(HtmlAttributeIntermediateNode node)
        {
            if (_currentElementName is null
                || !allowedElementAttributes.TryGetValue(_currentElementName, out var attributes)
                || !attributes.Contains(node.AttributeName))
            {
                return;
            }

            var valueNode = node.Children.Count == 1 ? node.Children[0] as HtmlAttributeValueIntermediateNode : null;
            if (TryExpandAssetPath(node, valueNode, node.AttributeName) is not { } expression)
            {
                return;
            }

            var replacement = new CSharpExpressionAttributeValueIntermediateNode { Prefix = valueNode!.Prefix };
            replacement.Children.Add(expression);
            node.Children[0] = replacement;
        }

        // Component parameter attributes:
        //   ComponentAttributeIntermediateNode
        //     -> HtmlContentIntermediateNode (exactly one child)
        //          -> IntermediateToken (Content starts with "~/")
        public override void VisitComponentAttribute(ComponentAttributeIntermediateNode node)
        {
            if (node.BoundAttribute?.Metadata is not PropertyMetadata { AcceptsAssetPath: true }
                || node.AttributeStructure == AttributeStructure.Minimized)
            {
                return;
            }

            var contentNode = node.Children.Count == 1 ? node.Children[0] as HtmlContentIntermediateNode : null;
            if (TryExpandAssetPath(node, contentNode, node.AttributeName) is not { } expression)
            {
                return;
            }

            var replacement = new CSharpExpressionIntermediateNode();
            replacement.Children.Add(expression);
            node.Children[0] = replacement;
        }

        // Expands an opted-in attribute whose value is a single ~/ literal into a synthetic
        // Assets[@"path"] C# token. Reports RZ10029 when the value mixes a ~/ literal with dynamic
        // content instead. Returns null (no change) when the value isn't an expandable asset path.
        private static CSharpIntermediateToken? TryExpandAssetPath(IntermediateNode ownerNode, IntermediateNode? valueContainer, string attributeName)
        {
            if (valueContainer is null || GetSingleLiteralToken(valueContainer) is not { } token)
            {
                ReportMixedContentDiagnostic(ownerNode, attributeName);
                return null;
            }

            var content = token.Content;
            if (!content.StartsWith(TildePrefix, StringComparison.Ordinal))
            {
                return null;
            }

            var path = content[TildePrefix.Length..];
            if (path.Length == 0)
            {
                return null;
            }

            // Verbatim string literal: backslashes (common in paths) stay literal and only embedded
            // quotes need doubling. Source is null -- synthetic code with no .razor mapping.
            var literal = path.Replace("\"", "\"\"");
            return new CSharpIntermediateToken($"{ComponentsApi.ComponentBase.Assets}[@\"{literal}\"]", source: null);
        }

        // Returns the single literal token that makes up the entire value, or null for mixed/dynamic
        // content (which never expands).
        private static IntermediateToken? GetSingleLiteralToken(IntermediateNode valueContainer)
            => valueContainer.Children is [IntermediateToken token]
                ? token
                : null;

        // Only the literal html value portions are inspected, so a ~/ that appears purely inside a
        // C# expression doesn't trigger the diagnostic.
        private static void ReportMixedContentDiagnostic(IntermediateNode node, string attributeName)
        {
            if (node.Children.Count <= 1)
            {
                return;
            }

            foreach (var child in node.Children)
            {
                if (child is HtmlAttributeValueIntermediateNode or HtmlContentIntermediateNode
                    && GetSingleLiteralToken(child) is { } token
                    && token.Content.StartsWith(TildePrefix, StringComparison.Ordinal))
                {
                    node.AddDiagnostic(
                        ComponentDiagnosticFactory.CreateTildePath_MixedContent(token.Source, attributeName));
                    return;
                }
            }
        }
    }
}
