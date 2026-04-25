// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// A phase that runs after lowering and tag helper discovery to resolve
/// <see cref="UnresolvedElementIntermediateNode"/> nodes into either
/// <see cref="TagHelperIntermediateNode"/> (if the element matches a tag helper)
/// or the appropriate plain element nodes (if it does not).
/// Works with IR nodes only -- no syntax tree access.
/// </summary>
internal partial class DefaultTagHelperResolutionPhase : RazorEnginePhaseBase
{
    private TagHelperResolver _resolver;

    /// <summary>
    /// Entry point: resolves all unresolved <see cref="UnresolvedElementIntermediateNode"/> nodes
    /// in the IR tree. For each, matches against tag helper bindings and either converts to a
    /// <see cref="TagHelperIntermediateNode"/> or unwraps to plain markup. A final
    /// <see cref="UnwrapAllElements"/> pass handles any remaining unresolved nodes.
    /// </summary>
    protected override RazorCodeDocument ExecuteCore(RazorCodeDocument codeDocument, CancellationToken cancellationToken)
    {
        var documentNode = codeDocument.GetDocumentNode();
        if (documentNode == null)
        {
            return codeDocument;
        }

        var tagHelperContext = codeDocument.GetTagHelperContext();

        // This phase works with IR nodes only -- no syntax tree access needed.
        // RazorCodeDocument.ParserOptions is a non-nullable property initialized by Create(),
        // so it is always available here.
        var parserOptions = codeDocument.ParserOptions;

        // Choose resolver based on file kind and language version. Component features
        // (MarkupElementIntermediateNode, RZ10012 diagnostics) require Version_3_0+ because
        // the ComponentDocumentClassifierPass is only registered at that version.
        _resolver = (codeDocument.FileKind.IsComponent() || codeDocument.FileKind.IsComponentImport())
            && parserOptions.LanguageVersion >= RazorLanguageVersion.Version_3_0
            ? new ComponentTagHelperResolver()
            : new LegacyTagHelperResolver();

        if (tagHelperContext == null || tagHelperContext.TagHelpers is [])
        {
            // No tag helpers discovered - unwrap all UnresolvedElement nodes to their fallback.
            UnwrapAllElements(documentNode, documentNode);

            // Still need to set referenced tag helpers for downstream phases.
            return codeDocument.WithReferencedTagHelpers([]);
        }

        var binder = tagHelperContext.GetBinder();
        var prefix = tagHelperContext.Prefix;

        using var usedHelpers = new TagHelperCollection.Builder();
        var sourceDocument = codeDocument.Source;
        var context = new ResolutionContext(sourceDocument, documentNode);
        ResolveElements(documentNode, binder, prefix, usedHelpers, in context);

        // Add tag helper descriptor validation diagnostics (e.g. RZ3003).
        using var descriptorDiagnostics = new PooledArrayBuilder<RazorDiagnostic>();
        foreach (var descriptor in tagHelperContext.TagHelpers)
        {
            descriptor.AppendAllDiagnostics(ref descriptorDiagnostics.AsRef());
        }

        foreach (var diagnostic in descriptorDiagnostics)
        {
            documentNode.AddDiagnostic(diagnostic);
        }

        return codeDocument.WithReferencedTagHelpers(usedHelpers.ToCollection());
    }

    /// <summary>
    /// Holds ambient state needed during element resolution. Passed by ref to avoid
    /// threading many parameters through the call chain.
    /// </summary>
    private readonly struct ResolutionContext
    {
        public readonly RazorSourceDocument SourceDocument;
        public readonly DocumentIntermediateNode DocumentNode;

        public ResolutionContext(RazorSourceDocument sourceDocument, DocumentIntermediateNode documentNode)
        {
            SourceDocument = sourceDocument;
            DocumentNode = documentNode;
        }
    }

    private void ResolveElements(IntermediateNode node, TagHelperBinder binder, string prefix, TagHelperCollection.Builder usedHelpers, in ResolutionContext context)
    {
        // Process children in reverse order since we may be replacing nodes.
        for (var i = node.Children.Count - 1; i >= 0; i--)
        {
            var child = node.Children[i];

            if (child is UnresolvedElementIntermediateNode elementNode)
            {
                // Resolve THIS element first. If it becomes a component tag helper,
                // BuildComponentTagHelper moves body children into the body node,
                // and the post-build re-resolution pass handles them with parent context.
                // This is important for child content elements like <ChildContent> that
                // need to know their parent tag helper to bind correctly.
                ResolveElement(node, i, elementNode, binder, prefix, usedHelpers, in context);
            }
            else
            {
                // For non-element nodes, recurse into children normally.
                ResolveElements(child, binder, prefix, usedHelpers, in context);
            }
        }
    }

    /// <summary>
    /// Resolves a single <see cref="UnresolvedElementIntermediateNode"/> by checking its tag
    /// name and attributes against the <paramref name="binder"/>. If it matches a tag helper,
    /// replaces it with a <see cref="TagHelperIntermediateNode"/>. Otherwise, delegates to
    /// the resolver to convert the element back to plain HTML markup.
    /// </summary>
    private void ResolveElement(
        IntermediateNode parent,
        int index,
        UnresolvedElementIntermediateNode elementNode,
        TagHelperBinder binder,
        string prefix,
        TagHelperCollection.Builder usedHelpers,
        in ResolutionContext context,
        TagHelperIntermediateNode tagHelperParent = null)
    {
        // Check for escaped tag helpers (<!tagname>) - these should NOT be matched.
        if (elementNode.IsEscaped)
        {
            ConvertToPlainElementAndResolve(parent, index, elementNode, binder, prefix, usedHelpers, in context, emitDiagnostics: false);
            return;
        }

        // Use pre-extracted attribute data for binding.
        var tagName = elementNode.TagName;
        var attributes = elementNode.AttributeData;

        // End-tag-only elements (e.g. </body> without matching <body>) should not match tag helpers.
        if (elementNode.StartTagNameSpan == null && !elementNode.IsSelfClosing && attributes.IsEmpty)
        {
            TryAddMalformedEndTagDiagnostic(elementNode, tagName, binder, attributes, parent, tagHelperParent);

            _resolver.ConvertToPlainElement(parent, index, elementNode);

            // Transfer element diagnostics to the converted/unwrapped node.
            // ConvertToMarkupElement already copies diagnostics, but UnwrapElement does not,
            // so this is needed for the legacy path.
            parent.Children[index].AddDiagnosticsFromNode(elementNode);
            return;
        }

        var (parentTagName, parentIsTagHelper) = GetParentTagInfo(parent, tagHelperParent);
        var binding = binder.GetBinding(tagName, attributes, parentTagName, parentIsTagHelper);
        if (binding == null)
        {
            ConvertToPlainElementAndResolve(parent, index, elementNode, binder, prefix, usedHelpers, in context);
            return;
        }

        // Build the tag helper node (binding validation + node creation + diagnostics + body).
        var (tagHelperNode, bodyNode) = BuildTagHelperNode(elementNode, binding, tagName, prefix, usedHelpers, in context);

        // Resolve any body children that are still UnresolvedElementIntermediateNode.
        ResolveBodyChildren(bodyNode, binder, prefix, usedHelpers, in context, tagHelperNode);

        // Check AllowedChildren constraints (RZ2009, RZ2010).
        ValidateAllowedChildren(tagHelperNode, bodyNode, binding, prefix);

        // Replace the UnresolvedElement with the TagHelperIntermediateNode.
        parent.Children[index] = tagHelperNode;

        // For StartTagOnly elements, body content from the original element
        // belongs to the parent, not the tag helper. Promote it.
        if (tagHelperNode.TagMode == TagMode.StartTagOnly)
        {
            var startTagEndIdx = elementNode.StartTagEndIndex;
            var bodyEndIdx = elementNode.BodyEndIndex;

            if (startTagEndIdx >= 0 && bodyEndIdx >= 0)
            {
                var insertIdx = index + 1;
                for (var i = startTagEndIdx; i < bodyEndIdx; i++)
                {
                    parent.Children.Insert(insertIdx++, elementNode.Children[i]);
                }
            }
        }
    }

    /// <summary>
    /// Creates a <see cref="TagHelperIntermediateNode"/> from a confirmed tag helper binding,
    /// adds all binding-level diagnostics, and builds the body node by delegating to the resolver.
    /// Covers the "tag helper binding and validation" and "element construction" split points.
    /// </summary>
    private (TagHelperIntermediateNode TagHelperNode, TagHelperBodyIntermediateNode BodyNode) BuildTagHelperNode(
        UnresolvedElementIntermediateNode elementNode,
        TagHelperBinding binding,
        string tagName,
        string prefix,
        TagHelperCollection.Builder usedHelpers,
        in ResolutionContext context)
    {
        // It IS a tag helper. Track the used helpers.
        usedHelpers.AddRange(binding.TagHelpers);

        // Determine tag name (strip prefix if present).
        var resolvedTagName = tagName;
        if (prefix != null && resolvedTagName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            resolvedTagName = resolvedTagName.Substring(prefix.Length);
        }

        // Determine TagMode from IR properties.
        var tagMode = GetTagMode(elementNode, binding);

        // Create the TagHelperIntermediateNode.
        var tagHelperNode = new TagHelperIntermediateNode()
        {
            TagName = resolvedTagName,
            TagMode = tagMode,
            Source = elementNode.Source,
            TagHelpers = binding.TagHelpers,
            StartTagSpan = elementNode.StartTagNameSpan,
        };

        // Add resolver-specific diagnostics (e.g. RZ10012 for component-like elements,
        // case mismatch between start/end tags).
        _resolver.AddMatchedElementDiagnostics(tagHelperNode, elementNode, binding, in context);

        // Check if resolved tag name is a void element (handles prefixed elements like th:input).
        var isResolvedVoidElement = elementNode.IsVoidElement || Legacy.ParserHelpers.VoidElements.Contains(resolvedTagName);

        // Directive-attribute-only matches (e.g. only @bind, @onclick) don't produce
        // structural diagnostics -- the rewriter doesn't emit them for such matches.
        if (!IsDirectiveAttributeOnly(binding))
        {
            AddStructuralDiagnostics(tagHelperNode, elementNode, tagName, tagMode, isResolvedVoidElement);
        }

        // Check for inconsistent TagStructure across bound rules (RZ2011).
        ValidateConsistentTagStructure(tagHelperNode, binding, elementNode, tagName);

        // Build body and attributes.
        var bodyNode = new TagHelperBodyIntermediateNode();
        _resolver.BuildTagHelper(tagHelperNode, bodyNode, elementNode, binding, context.SourceDocument, in context);

        return (tagHelperNode, bodyNode);
    }

    /// <summary>
    /// Resolves body children of a newly built tag helper node.
    /// Iterates over <paramref name="bodyNode"/> children in reverse order, recursively
    /// resolving any <see cref="UnresolvedElementIntermediateNode"/> entries with the
    /// tag helper as the parent context. Covers the "child attribute processing" split point.
    /// </summary>
    /// <remarks>
    /// Passing <paramref name="tagHelperParent"/> is critical so the binder can see the parent tag
    /// name. This is needed for:
    /// <list type="bullet">
    ///   <item><description>Components: child content matching (e.g., Found/NotFound inside Router)</description></item>
    ///   <item><description>Legacy tag helpers: RequireParentTag matching (e.g., &lt;td&gt; inside &lt;tr&gt;)</description></item>
    /// </list>
    /// </remarks>
    private void ResolveBodyChildren(
        TagHelperBodyIntermediateNode bodyNode,
        TagHelperBinder binder,
        string prefix,
        TagHelperCollection.Builder usedHelpers,
        in ResolutionContext context,
        TagHelperIntermediateNode tagHelperParent)
    {
        for (var i = bodyNode.Children.Count - 1; i >= 0; i--)
        {
            var bodyChild = bodyNode.Children[i];

            if (bodyChild is UnresolvedElementIntermediateNode bodyElementNode)
            {
                // Resolve the element first with parent context. This is critical because
                // ResolveElement will call BuildComponentTagHelper which moves the element's
                // own children into a body node and then recursively resolves them with the
                // correct parent tag helper context. If we called ResolveElements first, it
                // would descend into the element's children and prematurely resolve them
                // without knowing the parent tag helper (e.g., Found/NotFound inside Router
                // need to know Router is their parent to be matched as child content).
                ResolveElement(bodyNode, i, bodyElementNode, binder, prefix, usedHelpers, in context, tagHelperParent);
            }
            else
            {
                ResolveElements(bodyChild, binder, prefix, usedHelpers, in context);
            }
        }

        // Note: RZ1033 (tag helper must not have an end tag when TagStructure is WithoutEndTag)
        // is NOT emitted here. The UnresolvedElementIntermediateNode represents a matched
        // start/end tag pair. RZ1033 is only for orphan end tags (end tags without a matching
        // start tag on the tracker stack). For matched pairs like <component ...></component>,
        // the rewriter handles them normally. The rewriter (which still runs after this phase)
        // will emit RZ1033 for orphan end tags.
    }

    /// <summary>
    /// Adds structural diagnostics (RZ1035 missing close angle, RZ1034 malformed tag helper,
    /// RZ1042 void element) to a matched tag helper node.
    /// </summary>
    private static void AddStructuralDiagnostics(
        TagHelperIntermediateNode tagHelperNode,
        UnresolvedElementIntermediateNode elementNode,
        string tagName,
        TagMode tagMode,
        bool isResolvedVoidElement)
    {
        // Missing close angle diagnostics (RZ1035) -- emitted before RZ1034 to match rewriter ordering.
        if (elementNode.HasMissingCloseAngle)
        {
            var diagSource = elementNode.StartTagNameSpan ?? elementNode.Source;
            if (diagSource is SourceSpan ds)
            {
                tagHelperNode.AddDiagnostic(
                    RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(ds, tagName));
            }
        }

        if (elementNode is { HasMissingEndCloseAngle: true, EndTagSpan: SourceSpan endDs })
        {
            tagHelperNode.AddDiagnostic(
                RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(endDs, elementNode.EndTagName ?? tagName));
        }

        // Structural diagnostics: RZ1034 (malformed tag helper without end tag).
        // Only for non-void, non-self-closing elements without end tags,
        // and only when TagMode expects an end tag (StartTagAndEndTag).
        if (!elementNode.HasEndTag && !elementNode.IsSelfClosing && !isResolvedVoidElement
            && tagMode == TagMode.StartTagAndEndTag)
        {
            var diagSource = elementNode.StartTagNameSpan ?? elementNode.Source;
            if (diagSource is SourceSpan ds)
            {
                tagHelperNode.AddDiagnostic(
                    RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(ds, tagName));
            }
        }

        // RZ1042: void element without end tag (parser treated it as void).
        // Fires when a void-element-named tag helper has no end tag and
        // TagMode is StartTagAndEndTag (meaning the binding expected an end tag
        // but the parser treated the element as void). Elements with TagMode
        // StartTagOnly (e.g., TagStructure.WithoutEndTag) are handled without
        // diagnostics since the tag structure explicitly permits no end tag.
        if (isResolvedVoidElement && !elementNode.HasEndTag
            && tagMode == TagMode.StartTagAndEndTag)
        {
            var diagSource = elementNode.StartTagNameSpan ?? elementNode.Source;
            if (diagSource is SourceSpan ds)
            {
                tagHelperNode.AddDiagnostic(
                    RazorDiagnosticFactory.CreateParsing_VoidElement(ds, tagName));
            }
        }
    }

    /// <summary>
    /// Converts an unmatched element to plain markup, emits resolver-specific diagnostics,
    /// and recursively resolves any resulting children that may be tag helpers.
    /// </summary>
    private void ConvertToPlainElementAndResolve(
        IntermediateNode parent, int index,
        UnresolvedElementIntermediateNode elementNode,
        TagHelperBinder binder, string prefix,
        TagHelperCollection.Builder usedHelpers,
        in ResolutionContext context,
        bool emitDiagnostics = true)
    {
        var childCountBefore = parent.Children.Count;
        _resolver.ConvertToPlainElement(parent, index, elementNode);
        var resultCount = parent.Children.Count - childCountBefore + 1; // +1 because the original was removed

        if (emitDiagnostics && resultCount > 0)
        {
            _resolver.AddUnmatchedElementDiagnostic(parent.Children[index], elementNode, context.DocumentNode);
        }

        for (var j = index + resultCount - 1; j >= index; j--)
        {
            // Guard: ResolveElement can modify parent.Children (e.g., StartTagOnly promotion
            // inserts siblings), so j may exceed the updated count.
            if (j < parent.Children.Count)
            {
                if (parent.Children[j] is UnresolvedElementIntermediateNode promotedElement)
                {
                    ResolveElement(parent, j, promotedElement, binder, prefix, usedHelpers, in context);
                }
                else
                {
                    ResolveElements(parent.Children[j], binder, prefix, usedHelpers, in context);
                }
            }
        }
    }

    /// <summary>
    /// Post-pass that consolidates adjacent <see cref="HtmlContentIntermediateNode"/> children.
    /// Merges tokens and extends source spans. Called after element unwrapping to clean up
    /// fragmented content.
    /// </summary>
    private static void MergeAdjacentHtmlContent(IntermediateNode parent)
    {
        for (var i = 0; i < parent.Children.Count - 1; i++)
        {
            if (parent.Children[i] is HtmlContentIntermediateNode current &&
                parent.Children[i + 1] is HtmlContentIntermediateNode next)
            {
                current.Children.AddRange(next.Children);
                if (current.Source is SourceSpan cs && next.Source is SourceSpan ns)
                {
                    // Adjacent nodes are sequential, so next always ends after current.
                    current.Source = MergeSourceSpans(cs, ns);
                }
                else if (current.Source == null)
                {
                    current.Source = next.Source;
                }
                parent.Children.RemoveAt(i + 1);
                i--;
            }
        }
    }

    private static TagMode GetTagMode(UnresolvedElementIntermediateNode elementNode, TagHelperBinding binding)
    {
        if (elementNode.IsSelfClosing)
        {
            return TagMode.SelfClosing;
        }

        var hasDirectiveAttribute = false;
        foreach (var boundRulesInfo in binding.AllBoundRules)
        {
            if (boundRulesInfo.Rules.Any(static rule => rule.TagStructure == TagStructure.WithoutEndTag))
            {
                return TagMode.StartTagOnly;
            }

            var descriptor = boundRulesInfo.Descriptor;
            hasDirectiveAttribute |= descriptor.IsAnyComponentDocumentTagHelper() && !descriptor.IsComponentOrChildContentTagHelper();
        }

        if (hasDirectiveAttribute && elementNode.IsVoidElement && !elementNode.HasEndTag)
        {
            return TagMode.StartTagOnly;
        }

        return TagMode.StartTagAndEndTag;
    }

    private static void ValidateAllowedChildren(
        TagHelperIntermediateNode tagHelperNode,
        TagHelperBodyIntermediateNode bodyNode,
        TagHelperBinding binding,
        string prefix)
    {
        // Collect allowed child tag names from all descriptors.
        using var allowedNames = new PooledArrayBuilder<string>();
        foreach (var th in binding.TagHelpers)
        {
            foreach (var childTag in th.AllowedChildTags)
            {
                allowedNames.Add(childTag.Name);
            }
        }

        if (allowedNames.Count == 0)
        {
            return; // No AllowedChildTags constraints
        }

        var allowedChildrenString = string.Join(", ", allowedNames.ToArray());
        var parentTagName = tagHelperNode.TagName;

        foreach (var child in bodyNode.Children)
        {
            if (child is TagHelperIntermediateNode childTagHelper)
            {
                var childTagName = childTagHelper.TagName;
                if (!IsAllowedChild(childTagName, in allowedNames))
                {
                    childTagHelper.AddDiagnostic(
                        RazorDiagnosticFactory.CreateTagHelper_InvalidNestedTag(
                            child.Source ?? SourceSpan.Undefined, childTagName, parentTagName, allowedChildrenString));
                }
            }
            else if (child is MarkupElementIntermediateNode markupElement)
            {
                var childTagName = markupElement.TagName;
                // Strip prefix if present
                if (prefix != null && childTagName != null && childTagName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    childTagName = childTagName.Substring(prefix.Length);
                }
                if (childTagName != null && !IsAllowedChild(childTagName, in allowedNames))
                {
                    markupElement.AddDiagnostic(
                        RazorDiagnosticFactory.CreateTagHelper_InvalidNestedTag(
                            child.Source ?? SourceSpan.Undefined, childTagName, parentTagName, allowedChildrenString));
                }
            }
            else if (child is HtmlContentIntermediateNode htmlContent)
            {
                // Check if content is non-whitespace
                foreach (var token in htmlContent.Children)
                {
                    if (token is IntermediateToken t && !string.IsNullOrWhiteSpace(t.Content))
                    {
                        htmlContent.AddDiagnostic(
                            RazorDiagnosticFactory.CreateTagHelper_CannotHaveNonTagContent(
                                child.Source ?? SourceSpan.Undefined, parentTagName, allowedChildrenString));
                        break;
                    }
                }
            }
            else if (child is CSharpExpressionIntermediateNode or CSharpCodeIntermediateNode)
            {
                child.AddDiagnostic(
                    RazorDiagnosticFactory.CreateTagHelper_CannotHaveNonTagContent(
                        child.Source ?? SourceSpan.Undefined, parentTagName, allowedChildrenString));
            }
        }
    }

    private static bool IsAllowedChild(string tagName, in PooledArrayBuilder<string> allowedNames)
    {
        foreach (var name in allowedNames)
        {
            if (string.Equals(tagName, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static void ConvertUnresolvedValuesToBasicForm(HtmlAttributeIntermediateNode source, IntermediateNode target)
    {
        // Check if all children are unresolved literal values -- merge into single HtmlContent
        // to enable preallocated attribute optimization.
        if (AreAllChildrenOfType<UnresolvedAttributeValueIntermediateNode>(source.Children) && source.Children.Count > 0)
        {
            var htmlContent = new HtmlContentIntermediateNode();
            foreach (var child in source.Children)
            {
                var unresolvedLiteral = (UnresolvedAttributeValueIntermediateNode)child;
                foreach (var valueChild in unresolvedLiteral.Children)
                {
                    htmlContent.Children.Add(valueChild);
                    htmlContent.Source ??= valueChild.Source;
                }
            }

            // Compute merged source span when there are multiple children.
            // For a single child, the source was already set above.
            if (htmlContent.Children.Count > 1)
            {
                var firstSrc = htmlContent.Children[0].Source;
                var lastSrc = htmlContent.Children[^1].Source;
                if (firstSrc is { } fs && lastSrc is { } ls)
                {
                    htmlContent.Source = MergeSourceSpans(fs, ls);
                }
            }

            target.Children.Add(htmlContent);
            return;
        }

        foreach (var child in source.Children)
        {
            if (child is UnresolvedAttributeValueIntermediateNode unresolvedLiteral)
            {
                var htmlAttrValue = new HtmlAttributeValueIntermediateNode()
                {
                    Prefix = unresolvedLiteral.Prefix,
                    Source = unresolvedLiteral.Source,
                };

                htmlAttrValue.Children.AddRange(unresolvedLiteral.Children);

                target.Children.Add(htmlAttrValue);
            }
            else if (child is UnresolvedExpressionAttributeValueIntermediateNode unresolvedExpr)
            {
                IntermediateNode exprNode = unresolvedExpr.ContainsExpression
                    ? new CSharpExpressionAttributeValueIntermediateNode()
                    {
                        Prefix = unresolvedExpr.Prefix,
                        Source = unresolvedExpr.Source,
                    }
                    : new CSharpCodeAttributeValueIntermediateNode()
                    {
                        Prefix = unresolvedExpr.Prefix,
                        Source = unresolvedExpr.Source,
                    };

                // For expression attribute values: unwrap both CSharpExpression and CSharpCode.
                // For code attribute values: only unwrap CSharpCode, keep CSharpExpression.
                var unwrapExpressions = unresolvedExpr.ContainsExpression;
                foreach (var valueChild in unresolvedExpr.Children)
                {
                    if (unwrapExpressions && valueChild is CSharpExpressionIntermediateNode csharpExpr)
                    {
                        exprNode.Children.AddRange(csharpExpr.Children);
                    }
                    else if (valueChild is CSharpCodeIntermediateNode csharpCode)
                    {
                        exprNode.Children.AddRange(csharpCode.Children);
                    }
                    else
                    {
                        exprNode.Children.Add(valueChild);
                    }
                }

                target.Children.Add(exprNode);
            }
            else
            {
                target.Children.Add(child);
            }
        }
    }

    private static void ConvertHtmlTokensToCSharp(
        IntermediateNodeCollection tokens,
        ref PooledArrayBuilder<IntermediateNode> output,
        SourceSpan? source,
        bool wrapInCSharpExpression)
    {
        if (wrapInCSharpExpression)
        {
            var expr = new CSharpExpressionIntermediateNode() { Source = source };
            foreach (var token in tokens)
            {
                if (token is HtmlIntermediateToken htmlToken)
                {
                    expr.Children.Add(ToCSharpToken(htmlToken));
                }
                else
                {
                    expr.Children.Add(token);
                }
            }

            output.Add(expr);
        }
        else
        {
            foreach (var token in tokens)
            {
                if (token is HtmlIntermediateToken htmlToken)
                {
                    output.Add(ToCSharpToken(htmlToken));
                }
                else
                {
                    output.Add(token);
                }
            }
        }
    }

    /// <summary>
    /// Recursively flattens a node to direct CSharpIntermediateToken children on the target,
    /// converting HtmlIntermediateToken to CSharpIntermediateToken and unwrapping wrapper nodes.
    /// </summary>
    private static void FlattenToDirectCSharpTokens(IntermediateNode source, IntermediateNode target)
    {
        if (source is CSharpIntermediateToken csharpToken)
        {
            target.Children.Add(csharpToken);
        }
        else if (source is HtmlIntermediateToken htmlToken)
        {
            target.Children.Add(ToCSharpToken(htmlToken));
        }
        else if (source is IntermediateToken token)
        {
            target.Children.Add(new CSharpIntermediateToken(token.Content, token.Source));
        }
        else
        {
            // Unwrap container nodes (HtmlContentIntermediateNode, CSharpExpressionIntermediateNode, etc.)
            foreach (var child in source.Children)
            {
                FlattenToDirectCSharpTokens(child, target);
            }
        }
    }

    /// <summary>
    /// Final pass that finds remaining <see cref="UnresolvedElementIntermediateNode"/> nodes not
    /// resolved by tag helper matching. Converts each to a plain element using the resolver.
    /// Recursively processes the tree to handle nested elements.
    /// </summary>
    private void UnwrapAllElements(IntermediateNode node, DocumentIntermediateNode documentNode = null)
    {
        if (node is DocumentIntermediateNode doc)
        {
            documentNode = doc;
        }

        for (var i = node.Children.Count - 1; i >= 0; i--)
        {
            var child = node.Children[i];
            UnwrapAllElements(child, documentNode);

            if (child is UnresolvedElementIntermediateNode elementNode)
            {
                var countBefore = node.Children.Count;
                _resolver.ConvertToPlainElement(node, i, elementNode);
                var resultCount = node.Children.Count - countBefore + 1;

                if (resultCount > 0)
                {
                    _resolver.AddUnmatchedElementDiagnostic(node.Children[i], elementNode, documentNode);
                }
            }
        }
    }

    private static AttributeStructure InferAttributeStructure(HtmlAttributeIntermediateNode htmlAttr)
    {
        if (htmlAttr.Prefix != null)
        {
            if (htmlAttr.Prefix.Contains('\''))
            {
                return AttributeStructure.SingleQuotes;
            }
            if (htmlAttr.Prefix.Contains('"'))
            {
                return AttributeStructure.DoubleQuotes;
            }
            // Has '=' but no quotes (e.g., type= or checked=) - not minimized, treat as DoubleQuotes.
            if (htmlAttr.Prefix.Contains('='))
            {
                return AttributeStructure.DoubleQuotes;
            }
        }

        if (htmlAttr.Children.Count == 0)
        {
            return AttributeStructure.Minimized;
        }

        return AttributeStructure.DoubleQuotes;
    }

    /// <summary>
    /// Computes the source span of the attribute value by merging the source spans of all value children.
    /// </summary>
    private static SourceSpan? ComputeValueSource(HtmlAttributeIntermediateNode htmlAttr)
    {
        SourceSpan? result = null;
        foreach (var child in htmlAttr.Children)
        {
            // For HtmlAttributeValueIntermediateNode and CSharpExpressionAttributeValueIntermediateNode,
            // use the inner token sources (not the wrapper).
            if (child is HtmlAttributeValueIntermediateNode or CSharpExpressionAttributeValueIntermediateNode)
            {
                foreach (var token in child.Children)
                {
                    if (token.Source is SourceSpan tokenSource)
                    {
                        result = result == null ? tokenSource : MergeSpans(result.Value, tokenSource);
                    }
                }
            }
            else if (child.Source is SourceSpan childSource)
            {
                result = result == null ? childSource : MergeSpans(result.Value, childSource);
            }
        }
        return result;
    }

    private static SourceSpan MergeSpans(SourceSpan a, SourceSpan b)
    {
        var start = Math.Min(a.AbsoluteIndex, b.AbsoluteIndex);
        var end = Math.Max(a.AbsoluteIndex + a.Length, b.AbsoluteIndex + b.Length);
        var first = a.AbsoluteIndex <= b.AbsoluteIndex ? a : b;
        var last = a.AbsoluteIndex + a.Length >= b.AbsoluteIndex + b.Length ? a : b;
        var lineCount = (last.LineIndex + last.LineCount) - first.LineIndex;
        return first.WithAbsoluteIndex(start)
            .WithLength(end - start)
            .WithLineCount(lineCount)
            .WithEndCharacterIndex(last.EndCharacterIndex);
    }

    /// <summary>
    /// Merges two already-ordered source spans into a single span covering both.
    /// <paramref name="first"/> must start at or before <paramref name="last"/>.
    /// </summary>
    internal static SourceSpan MergeSourceSpans(SourceSpan first, SourceSpan last)
    {
        Debug.Assert(first.AbsoluteIndex <= last.AbsoluteIndex,
            "first span must start at or before the last span");
        return new SourceSpan(
            first.FilePath,
            first.AbsoluteIndex,
            first.LineIndex,
            first.CharacterIndex,
            last.AbsoluteIndex + last.Length - first.AbsoluteIndex,
            last.LineIndex + last.LineCount - first.LineIndex,
            last.EndCharacterIndex);
    }

    /// <summary>
    /// Splits an explicit expression <c>@(expr)</c> at the given source position into
    /// structured <see cref="CSharpIntermediateToken"/> children: <c>@</c>, <c>(</c>,
    /// inner content, <c>)</c>. Falls back to a single token for non-<c>@()</c> patterns.
    /// </summary>
    private static void EmitExplicitExpressionTokens(
        IntermediateNode target,
        int exprStart,
        int exprLength,
        RazorSourceDocument sourceDocument)
    {
        var exprText = sourceDocument.Text.ToString(
            new Microsoft.CodeAnalysis.Text.TextSpan(exprStart, exprLength));
        var filePath = sourceDocument.FilePath;

        if (exprText.Length >= 3 && exprText[0] == '@' && exprText[1] == '(')
        {
            // @
            var atLoc = sourceDocument.Text.Lines.GetLinePosition(exprStart);
            target.Children.Add(IntermediateNodeFactory.CSharpToken(
                "@",
                new SourceSpan(filePath, exprStart, atLoc.Line, atLoc.Character, 1, 0, atLoc.Character + 1)));

            // (, inner content, )
            EmitParenthesizedExpressionTokens(target, exprStart + 1, exprLength - 1, sourceDocument);
        }
        else
        {
            // Not @() -- emit as single token.
            var loc = sourceDocument.Text.Lines.GetLinePosition(exprStart);
            target.Children.Add(IntermediateNodeFactory.CSharpToken(
                exprText,
                new SourceSpan(filePath, exprStart, loc.Line, loc.Character, exprLength, 0, loc.Character + exprLength)));
        }
    }

    /// <summary>
    /// Emits a parenthesized expression <c>(expr)</c> as three tokens: <c>(</c>, inner content, <c>)</c>.
    /// The <paramref name="parenStart"/> should point at the <c>(</c> character and
    /// <paramref name="parenLength"/> should include both parens.
    /// </summary>
    private static void EmitParenthesizedExpressionTokens(
        IntermediateNode target,
        int parenStart,
        int parenLength,
        RazorSourceDocument sourceDocument)
    {
        var filePath = sourceDocument.FilePath;

        // (
        var openLoc = sourceDocument.Text.Lines.GetLinePosition(parenStart);
        target.Children.Add(IntermediateNodeFactory.CSharpToken(
            "(",
            new SourceSpan(filePath, parenStart, openLoc.Line, openLoc.Character, 1, 0, openLoc.Character + 1)));

        // inner content
        var innerStart = parenStart + 1;
        var innerLen = parenLength - 2; // skip ( and )
        if (innerLen > 0)
        {
            var innerText = sourceDocument.Text.ToString(
                new Microsoft.CodeAnalysis.Text.TextSpan(innerStart, innerLen));
            var innerLoc = sourceDocument.Text.Lines.GetLinePosition(innerStart);
            target.Children.Add(IntermediateNodeFactory.CSharpToken(
                innerText,
                new SourceSpan(filePath, innerStart, innerLoc.Line, innerLoc.Character, innerLen, 0, innerLoc.Character + innerLen)));
        }

        // )
        var closePos = parenStart + parenLength - 1;
        var closeLoc = sourceDocument.Text.Lines.GetLinePosition(closePos);
        target.Children.Add(IntermediateNodeFactory.CSharpToken(
            ")",
            new SourceSpan(filePath, closePos, closeLoc.Line, closeLoc.Character, 1, 0, closeLoc.Character + 1)));
    }

    /// <summary>
    /// Emits the full <c>@@(expr)</c> escape sequence as a <see cref="CSharpExpressionIntermediateNode"/>
    /// with structured tokens. The expression source should point at the <c>@(expr)</c> portion
    /// (after the literal <c>@</c> from <c>@@</c>).
    /// </summary>
    private static void EmitEscapedAtCSharpExpression(
        IntermediateNode target,
        SourceSpan expressionSource,
        RazorSourceDocument sourceDocument)
    {
        var expr = new CSharpExpressionIntermediateNode();
        EmitExplicitExpressionTokens(expr, expressionSource.AbsoluteIndex, expressionSource.Length, sourceDocument);

        var exprLoc = sourceDocument.Text.Lines.GetLinePosition(expressionSource.AbsoluteIndex);
        expr.Source = expressionSource.WithLineIndex(exprLoc.Line)
            .WithCharacterIndex(exprLoc.Character)
            .WithLineCount(0)
            .WithEndCharacterIndex(exprLoc.Character + expressionSource.Length);
        target.Children.Add(expr);
    }

    /// <summary>
    /// Collects the text content and first child source from an <see cref="HtmlAttributeValueIntermediateNode"/>,
    /// concatenating the prefix with all token content.
    /// </summary>
    private static (string Content, SourceSpan? Source) CollectAttributeValueContent(HtmlAttributeValueIntermediateNode attrValue)
    {
        var content = attrValue.Prefix ?? string.Empty;
        foreach (var token in attrValue.Children)
        {
            if (token is IntermediateToken intermediateToken)
            {
                content += intermediateToken.Content;
            }
        }

        var source = attrValue.Children.Count > 0 ? attrValue.Children[0].Source : attrValue.Source;
        return (content, source);
    }

    /// <summary>
    /// Converts an <see cref="HtmlIntermediateToken"/> to a <see cref="CSharpIntermediateToken"/>,
    /// preserving lazy content when present.
    /// </summary>
    private static CSharpIntermediateToken ToCSharpToken(HtmlIntermediateToken htmlToken)
    {
        return htmlToken.IsLazy
            ? IntermediateNodeFactory.CSharpToken(htmlToken, static t => t.Content, htmlToken.Source)
            : new CSharpIntermediateToken(htmlToken.Content, htmlToken.Source);
    }

    /// <summary>
    /// Creates an empty <see cref="HtmlContentIntermediateNode"/> with a single empty token.
    /// </summary>
    private static HtmlContentIntermediateNode CreateEmptyHtmlContent(SourceSpan? source)
    {
        return new HtmlContentIntermediateNode()
        {
            Source = source,
            Children = { IntermediateNodeFactory.HtmlToken("", source) }
        };
    }

    /// <summary>
    /// Creates an empty <see cref="CSharpIntermediateToken"/> with an empty string.
    /// </summary>
    private static CSharpIntermediateToken CreateEmptyCSharpToken(SourceSpan? source)
    {
        return IntermediateNodeFactory.CSharpToken("", source);
    }

    /// <summary>
    /// Checks whether all children of a node collection are of the specified type.
    /// </summary>
    private static bool AreAllChildrenOfType<T>(IntermediateNodeCollection children) where T : IntermediateNode
    {
        foreach (var child in children)
        {
            if (child is not T)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Extends a source span backward by <paramref name="prefixLength"/> characters.
    /// Used when merging an attribute prefix (e.g., whitespace before the value) into
    /// the first token's content and source span.
    /// </summary>
    private static SourceSpan? ExtendSpanBackward(SourceSpan? source, int prefixLength)
    {
        if (source is not { } s)
        {
            return null;
        }

        return s.WithAbsoluteIndex(s.AbsoluteIndex - prefixLength)
            .WithCharacterIndex(s.CharacterIndex - prefixLength)
            .WithLength(s.Length + prefixLength);
    }

    /// <summary>
    /// Collects literal content and source spans from unresolved attribute value children.
    /// Used by both the string and non-string unresolved lowering paths.
    /// </summary>
    private static (string Content, SourceSpan? MergedSpan) CollectUnresolvedLiteralContent(
        HtmlAttributeIntermediateNode htmlAttr,
        SourceSpan? valueSourceSpan)
    {
        using var _sb = StringBuilderPool.GetPooledObject(out var sb);
        SourceSpan? firstSpan = null;
        SourceSpan? lastSpan = null;

        foreach (var child in htmlAttr.Children)
        {
            var unresolvedValue = (UnresolvedAttributeValueIntermediateNode)child;

            if (!string.IsNullOrEmpty(unresolvedValue.Prefix))
            {
                sb.Append(unresolvedValue.Prefix);
            }

            foreach (var valueChild in unresolvedValue.Children)
            {
                if (valueChild is HtmlIntermediateToken htmlToken)
                {
                    sb.Append(htmlToken.Content);
                    if (htmlToken.Source is { } s)
                    {
                        firstSpan ??= s;
                        lastSpan = s;
                    }
                }
            }
        }

        var mergedSpan = valueSourceSpan;
        if (mergedSpan is null && firstSpan is { } first && lastSpan is { } last)
        {
            mergedSpan = MergeSpans(first, last);
        }

        return (sb.ToString(), mergedSpan);
    }

    /// <summary>
    /// Returns the parent tag name and whether it's a tag helper, for use with the binder.
    /// Checks the explicit <paramref name="tagHelperParent"/> first (passed during body
    /// resolution), then falls back to checking if <paramref name="parent"/> is a tag helper node.
    /// </summary>
    private static (string TagName, bool IsTagHelper) GetParentTagInfo(
        IntermediateNode parent,
        TagHelperIntermediateNode tagHelperParent)
    {
        if (tagHelperParent != null)
        {
            return (tagHelperParent.TagName, true);
        }

        if (parent is TagHelperIntermediateNode parentTh)
        {
            return (parentTh.TagName, true);
        }

        return (null, false);
    }

    /// <summary>
    /// Checks if an end-tag-only element matches a tag helper and adds RZ1034 (malformed tag
    /// helper) to the element node if so. The diagnostic is added to <paramref name="elementNode"/>
    /// so that the subsequent convert/unwrap operation can transfer it to the replacement node.
    /// </summary>
    private static void TryAddMalformedEndTagDiagnostic(
        UnresolvedElementIntermediateNode elementNode,
        string tagName,
        TagHelperBinder binder,
        ImmutableArray<KeyValuePair<string, string>> attributes,
        IntermediateNode parent,
        TagHelperIntermediateNode tagHelperParent)
    {
        var (endParentTagName, endParentIsTagHelper) = GetParentTagInfo(parent, tagHelperParent);
        var endBinding = binder.GetBinding(tagName, attributes, endParentTagName, endParentIsTagHelper);
        if (endBinding == null)
        {
            return;
        }

        // Compute tag name position within end tag (after "</").
        var diagSource = elementNode.Source;
        if (elementNode.EndTagSpan is SourceSpan ets)
        {
            diagSource = ets.WithAbsoluteIndex(ets.AbsoluteIndex + 2)
                .WithCharacterIndex(ets.CharacterIndex + 2)
                .WithLength(tagName.Length)
                .WithLineCount(0)
                .WithEndCharacterIndex(ets.CharacterIndex + 2 + tagName.Length);
        }

        if (diagSource is SourceSpan ds)
        {
            elementNode.AddDiagnostic(
                RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(ds, tagName));
        }
    }

    /// <summary>
    /// Returns <c>true</c> if every tag helper in the binding is a directive attribute helper
    /// (e.g., <c>@bind</c>, <c>@onclick</c>) and not a component or child content tag helper.
    /// The rewriter does not produce structural diagnostics for directive-attribute-only matches.
    /// </summary>
    private static bool IsDirectiveAttributeOnly(TagHelperBinding binding)
    {
        foreach (var th in binding.TagHelpers)
        {
            if (!th.IsAnyComponentDocumentTagHelper() || th.IsComponentOrChildContentTagHelper())
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks for inconsistent <see cref="TagStructure"/> values across all bound rules
    /// in a tag helper binding, emitting RZ2011 if a conflict is found.
    /// </summary>
    private static void ValidateConsistentTagStructure(
        TagHelperIntermediateNode tagHelperNode,
        TagHelperBinding binding,
        UnresolvedElementIntermediateNode elementNode,
        string tagName)
    {
        TagStructure? baseStructure = null;
        string baseDisplayName = null;
        foreach (var boundRulesInfo in binding.AllBoundRules)
        {
            foreach (var rule in boundRulesInfo.Rules)
            {
                if (rule.TagStructure != TagStructure.Unspecified)
                {
                    if (baseStructure.HasValue && baseStructure != rule.TagStructure)
                    {
                        tagHelperNode.AddDiagnostic(
                            RazorDiagnosticFactory.CreateTagHelper_InconsistentTagStructure(
                                elementNode.Source ?? SourceSpan.Undefined,
                                baseDisplayName,
                                boundRulesInfo.Descriptor.DisplayName,
                                tagName));
                    }
                    baseStructure ??= rule.TagStructure;
                    baseDisplayName ??= boundRulesInfo.Descriptor.DisplayName;
                }
            }
        }
    }

    /// <summary>
    /// Returns <c>true</c> if all children of a node are empty or whitespace-only tokens.
    /// Used to detect empty bound attribute values for RZ2008 diagnostics.
    /// </summary>
    private static bool HasOnlyWhitespaceContent(IntermediateNode node)
    {
        foreach (var child in node.Children)
        {
            if (child is IntermediateToken token)
            {
                if (!string.IsNullOrWhiteSpace(token.Content))
                {
                    return false;
                }
            }
            else if (child is HtmlContentIntermediateNode htmlContent)
            {
                foreach (var hc in htmlContent.Children)
                {
                    if (hc is IntermediateToken ht && !string.IsNullOrWhiteSpace(ht.Content))
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Abstract base class for tag helper resolution strategy. Subclasses handle
    /// either legacy (.cshtml) or component (.razor) element processing.
    /// </summary>
    private abstract class TagHelperResolver
    {
        /// <summary>
        /// Called when an element matches tag helpers and needs to be built into a
        /// <see cref="TagHelperIntermediateNode"/>.
        /// </summary>
        public abstract void BuildTagHelper(
            TagHelperIntermediateNode tagHelperNode,
            TagHelperBodyIntermediateNode bodyNode,
            UnresolvedElementIntermediateNode elementNode,
            TagHelperBinding binding,
            RazorSourceDocument sourceDocument,
            in ResolutionContext context);

        /// <summary>
        /// Called when an element does NOT match any tag helper and needs to be converted
        /// back to plain HTML markup nodes. Only handles conversion — does not recurse
        /// into children or emit diagnostics.
        /// </summary>
        public abstract void ConvertToPlainElement(
            IntermediateNode parent, int index,
            UnresolvedElementIntermediateNode elementNode);

        /// <summary>
        /// Called after an element is matched to a tag helper. Adds resolver-specific
        /// diagnostics such as RZ10012 (unrecognized component-like element) and case
        /// mismatch warnings. Default implementation does nothing.
        /// </summary>
        public virtual void AddMatchedElementDiagnostics(
            TagHelperIntermediateNode tagHelperNode,
            UnresolvedElementIntermediateNode elementNode,
            TagHelperBinding binding,
            in ResolutionContext context)
        {
        }

        /// <summary>
        /// Called after <see cref="ConvertToPlainElement"/> during the final unwrap pass
        /// to add resolver-specific diagnostics. Default implementation does nothing.
        /// </summary>
        public virtual void AddUnmatchedElementDiagnostic(
            IntermediateNode convertedNode,
            UnresolvedElementIntermediateNode originalNode,
            DocumentIntermediateNode documentNode)
        {
        }

        /// <summary>
        /// Lowers unresolved attribute value nodes into the correct IR shape for a
        /// bound tag helper property. Handles the shared "all-literal" fast paths,
        /// then delegates to <see cref="LowerComplexNonStringValues"/> or
        /// <see cref="LowerComplexStringValues"/> for mixed content.
        /// </summary>
        protected void LowerUnresolvedAttributeValues(
            HtmlAttributeIntermediateNode htmlAttr,
            IntermediateNode target,
            bool expectsStringValue,
            SourceSpan? valueSourceSpan,
            RazorSourceDocument sourceDocument)
        {
            if (htmlAttr.Children.Count == 0)
            {
                return;
            }

            if (AreAllChildrenOfType<UnresolvedAttributeValueIntermediateNode>(htmlAttr.Children) && htmlAttr.Children.Count > 0)
            {
                var (mergedContent, mergedSpan) = CollectUnresolvedLiteralContent(htmlAttr, valueSourceSpan);

                if (expectsStringValue)
                {
                    var htmlContent = new HtmlContentIntermediateNode() { Source = mergedSpan };
                    htmlContent.Children.Add(new HtmlIntermediateToken(mergedContent, mergedSpan));
                    target.Children.Add(htmlContent);
                }
                else
                {
                    target.Children.Add(new CSharpIntermediateToken(mergedContent, mergedSpan));
                }

                return;
            }

            if (expectsStringValue)
            {
                LowerComplexStringValues(htmlAttr, target, sourceDocument);
            }
            else
            {
                LowerComplexNonStringValues(htmlAttr, target, valueSourceSpan, sourceDocument);
            }
        }

        /// <summary>
        /// Handles complex (non-all-literal) non-string attribute value lowering.
        /// </summary>
        protected abstract void LowerComplexNonStringValues(
            HtmlAttributeIntermediateNode htmlAttr,
            IntermediateNode target,
            SourceSpan? valueSourceSpan,
            RazorSourceDocument sourceDocument);

        /// <summary>
        /// Handles complex (non-all-literal) string attribute value lowering.
        /// </summary>
        protected abstract void LowerComplexStringValues(
            HtmlAttributeIntermediateNode htmlAttr,
            IntermediateNode target,
            RazorSourceDocument sourceDocument);
    }

}
