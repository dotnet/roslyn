// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

internal partial class DefaultTagHelperResolutionPhase
{
    private sealed class ComponentTagHelperResolver : TagHelperResolver
    {
        public override void AddMatchedElementDiagnostics(
            TagHelperIntermediateNode tagHelperNode,
            UnresolvedElementIntermediateNode elementNode,
            TagHelperBinding binding,
            in ResolutionContext context)
        {
            var tagName = elementNode.TagName;

            // Add RZ10012 for elements that look like components but didn't match a Component
            // or ChildContent tag helper. Catch-all directive attribute helpers (@key, @ref,
            // @rendermode) match any element, not just components, so they don't count.
            if (LooksLikeUnexpectedComponent(context.DocumentNode, tagName) &&
                !binding.TagHelpers.Any(static th => th.Kind.IsComponentOrChildContentKind))
            {
                tagHelperNode.AddDiagnostic(
                    ComponentDiagnosticFactory.Create_UnexpectedMarkupElement(tagName, elementNode.StartTagSpan ?? elementNode.Source));
            }

            // Check for case mismatch between start and end tag names.
            if (elementNode.EndTagName != null)
            {
                var startTagName = elementNode.TagName;
                var endTagName = elementNode.EndTagName;
                if (!string.Equals(startTagName, endTagName, StringComparison.Ordinal))
                {
                    tagHelperNode.AddDiagnostic(
                        ComponentDiagnosticFactory.Create_InconsistentStartAndEndTagName(startTagName, endTagName, elementNode.EndTagSpan));
                }
            }
        }

        public override void AddUnmatchedElementDiagnostic(
            IntermediateNode convertedNode,
            UnresolvedElementIntermediateNode originalNode,
            DocumentIntermediateNode documentNode)
        {
            if (LooksLikeUnexpectedComponent(documentNode, originalNode.TagName))
            {
                convertedNode.AddDiagnostic(
                    ComponentDiagnosticFactory.Create_UnexpectedMarkupElement(originalNode.TagName, originalNode.StartTagSpan ?? originalNode.Source));
            }
        }

        protected override void LowerComplexNonStringValues(
            HtmlAttributeIntermediateNode htmlAttr,
            IntermediateNode target,
            SourceSpan? valueSourceSpan,
            RazorSourceDocument sourceDocument)
        {
            LowerUnresolvedNonStringAttributeValues_Component(htmlAttr, target);
        }

        protected override void LowerComplexStringValues(
            HtmlAttributeIntermediateNode htmlAttr,
            IntermediateNode target,
            RazorSourceDocument sourceDocument)
        {
            LowerUnresolvedStringAttributeValues_Component(htmlAttr, target);
        }

        /// <summary>
        /// Builds a <see cref="TagHelperIntermediateNode"/> from a component element. Iterates
        /// through the element's children, converting unresolved and HTML attributes to tag helper
        /// attribute nodes, and adding remaining children (body content) to the body node.
        /// </summary>
        public override void BuildTagHelper(
            TagHelperIntermediateNode tagHelperNode,
            TagHelperBodyIntermediateNode bodyNode,
            UnresolvedElementIntermediateNode elementNode,
            TagHelperBinding binding,
            RazorSourceDocument sourceDocument,
            in ResolutionContext context)
        {
            var renderedBoundAttributeNames = new PooledHashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                tagHelperNode.Children.Add(bodyNode);

                foreach (var child in elementNode.Children)
                {
                    if (child is UnresolvedAttributeIntermediateNode unresolvedAttr)
                    {
                        ConvertUnresolvedAttributeToTagHelper(tagHelperNode, bodyNode, unresolvedAttr, binding, ref renderedBoundAttributeNames, sourceDocument, in context);
                    }
                    else if (child is HtmlAttributeIntermediateNode htmlAttr)
                    {
                        ConvertComponentAttributeToTagHelper(tagHelperNode, htmlAttr, binding);
                    }
                    else if (child is ComponentAttributeIntermediateNode or
                        SplatIntermediateNode or
                        SetKeyIntermediateNode or
                        ReferenceCaptureIntermediateNode)
                    {
                        // Already-resolved attribute types don't need conversion.
                        tagHelperNode.Children.Add(child);
                    }
                    else if (tagHelperNode.TagMode != TagMode.StartTagOnly)
                    {
                        bodyNode.Children.Add(child);
                    }
                }
            }
            finally
            {
                renderedBoundAttributeNames.Dispose();
            }
        }

        /// <summary>
        /// Resolves an unresolved attribute against the tag helper binding for a component element.
        /// Handles directive attributes (e.g., <c>@bind-Value</c>), regular bound properties,
        /// and unbound HTML attributes. Directive attributes require additional processing for
        /// parameter matches, mixed content detection, and bind:get/set wrapping.
        /// </summary>
        private void ConvertUnresolvedAttributeToTagHelper(
            TagHelperIntermediateNode tagHelperNode,
            TagHelperBodyIntermediateNode bodyNode,
            UnresolvedAttributeIntermediateNode unresolvedAttr,
            TagHelperBinding binding,
            ref PooledHashSet<string> renderedBoundAttributeNames,
            RazorSourceDocument sourceDocument,
            in ResolutionContext context)
        {
            var attributeName = unresolvedAttr.AttributeName;
            using var matches = new PooledArrayBuilder<TagHelperAttributeMatch>();
            TagHelperMatchingConventions.GetAttributeMatches(binding.TagHelpers, attributeName, ref matches.AsRef());

            var hasMatches = matches.Any();
            var isDuplicateBound = hasMatches && !renderedBoundAttributeNames.Add(attributeName);

            if (hasMatches && !isDuplicateBound)
            {
                var hasDirectiveMatch = matches.Any(static m => m.Attribute.IsDirectiveAttribute);

                foreach (var match in matches)
                {
                    if (hasDirectiveMatch && !match.Attribute.IsDirectiveAttribute)
                    {
                        continue;
                    }

                    if (match.Attribute.IsDirectiveAttribute)
                    {
                        ConvertToUnresolvedDirectiveAttribute(tagHelperNode, unresolvedAttr, match, attributeName, sourceDocument);
                    }
                    else
                    {
                        ConvertToUnresolvedBoundProperty(tagHelperNode, unresolvedAttr, match, attributeName, sourceDocument);
                    }
                }
            }
            else
            {
                ConvertToUnresolvedUnboundAttribute(tagHelperNode, unresolvedAttr, attributeName, isDuplicateBound);
            }
        }

        /// <summary>
        /// Handles directive attributes (<c>@bind-Value</c>, <c>@onclick</c>, <c>@ref</c>).
        /// Creates a <see cref="TagHelperDirectiveAttributeIntermediateNode"/> (or its Parameter variant
        /// for <c>@bind-Value:get</c>). For mixed literal+expression content, routes through
        /// <see cref="LowerUnresolvedAttributeValues"/>. Normalizes bind:get/set parameters by wrapping
        /// in <see cref="CSharpExpressionIntermediateNode"/>.
        /// </summary>
        private void ConvertToUnresolvedDirectiveAttribute(
            TagHelperIntermediateNode tagHelperNode,
            UnresolvedAttributeIntermediateNode unresolvedAttr,
            TagHelperAttributeMatch match,
            string attributeName,
            RazorSourceDocument sourceDocument)
        {
            var attrStructure = unresolvedAttr.AttributeStructure;
            var directiveAttributeName = new DirectiveAttributeName(attributeName);
            var nameSpan = unresolvedAttr.AttributeNameSpan;
            var directiveNameSpan = nameSpan;
            if (directiveNameSpan is SourceSpan ns && attributeName.StartsWith('@'))
            {
                directiveNameSpan = ns.WithAbsoluteIndex(ns.AbsoluteIndex + 1)
                    .WithCharacterIndex(ns.CharacterIndex + 1)
                    .WithLength(ns.Length - 1);
            }

            // Strip parameter suffix from OriginalAttributeSpan for parameter matches
            var parameterOriginalSpan = directiveNameSpan;
            if (match.IsParameterMatch && directiveAttributeName.HasParameter && parameterOriginalSpan is SourceSpan ps)
            {
                var nameWithoutParamLen = directiveAttributeName.TextWithoutParameter.Length;
                parameterOriginalSpan = ps.WithLength(nameWithoutParamLen)
                    .WithEndCharacterIndex(ps.CharacterIndex + nameWithoutParamLen);
            }

            IntermediateNode directiveNode = match.IsParameterMatch && directiveAttributeName.HasParameter
                ? new TagHelperDirectiveAttributeParameterIntermediateNode(match)
                {
                    AttributeName = directiveAttributeName.Text,
                    AttributeNameWithoutParameter = directiveAttributeName.TextWithoutParameter,
                    OriginalAttributeName = attributeName,
                    AttributeStructure = attrStructure,
                    OriginalAttributeSpan = parameterOriginalSpan,
                }
                : new TagHelperDirectiveAttributeIntermediateNode(match)
                {
                    AttributeName = directiveAttributeName.Text,
                    OriginalAttributeName = attributeName,
                    AttributeStructure = attrStructure,
                    OriginalAttributeSpan = directiveNameSpan,
                };

            if (!unresolvedAttr.IsMinimized)
            {
                // Try unresolved children for MIXED string content (literal + expressions).
                // This avoids MarkupBlock issues from LowerAttributeValue.
                var htmlAttrChild = unresolvedAttr.HtmlAttributeNode;
                if (htmlAttrChild != null)
                {
                    // Check if it has both literal and expression children.
                    var hasLiteral = false;
                    var hasExpression = false;
                    foreach (var vc in htmlAttrChild.Children)
                    {
                        if (vc is UnresolvedAttributeValueIntermediateNode)
                        {
                            hasLiteral = true;
                        }

                        if (vc is UnresolvedExpressionAttributeValueIntermediateNode)
                        {
                            hasExpression = true;
                        }
                    }

                    var hasMixedStringContent = hasLiteral && hasExpression;

                    if (hasMixedStringContent && match.ExpectsStringValue)
                    {
                        // Use unresolved string path for mixed content -- produces correct HtmlContent + CSharpExpression.
                        LowerUnresolvedAttributeValues(htmlAttrChild, directiveNode, true, unresolvedAttr.ValueSourceSpan, sourceDocument);
                    }
                    else if (!match.ExpectsStringValue && !hasMixedStringContent)
                    {
                        // Non-string directive attribute with single expression (no mixed content):
                        // use the unresolved non-string path which produces flat CSharp tokens
                        // producing flat CSharp tokens.
                        LowerUnresolvedAttributeValues(htmlAttrChild, directiveNode, false, unresolvedAttr.ValueSourceSpan, sourceDocument);
                    }
                    else
                    {
                        // Remaining combinations: string with single expression, or non-string with mixed content.
                        // Use ConvertUnresolvedValuesToBasicForm to produce standard node types, then post-process.
                        ConvertUnresolvedValuesToBasicForm(htmlAttrChild, directiveNode);

                        if (match.ExpectsStringValue)
                        {
                            ConvertExpressionAttributeValuesToCSharpExpression(directiveNode);
                        }
                    }
                }

                directiveNode.Source = unresolvedAttr.ValueSourceSpan
                    ?? (directiveNode.Children.Count > 0 ? directiveNode.Children[0].Source : null);

                if (!match.ExpectsStringValue)
                {
                    if (match.IsParameterMatch)
                    {
                        FlattenDirectiveChildrenToCSharpTokens(directiveNode);
                    }
                    else
                    {
                        NormalizeBoundPropertyChildren(directiveNode, wrapLiteralsInCSharpExpression: true);
                    }
                }

                // bind:get/bind:set parameter matches need CSharpExpression wrapping.
                if (match.IsParameterMatch &&
                    match.Parameter is { Name: "get" or "set" } &&
                    directiveNode.Children.Count > 0 &&
                    directiveNode.Children[0] is not CSharpExpressionIntermediateNode)
                {
                    var expr = new CSharpExpressionIntermediateNode();
                    expr.Children.AddRange(directiveNode.Children);
                    directiveNode.Children.Clear();
                    expr.Source = expr.Children.Count > 0 ? expr.Children[0].Source : directiveNode.Source;
                    directiveNode.Children.Add(expr);
                }
            }

            tagHelperNode.Children.Add(directiveNode);
        }

        /// <summary>
        /// Handles non-directive bound properties (e.g., Value, Class on a tag helper).
        /// Creates a <see cref="TagHelperPropertyIntermediateNode"/> and routes the value through
        /// <see cref="LowerUnresolvedAttributeValues"/> with the <see cref="TagHelperAttributeMatch.ExpectsStringValue"/>
        /// flag. Empty values receive a synthetic child so downstream consumers can
        /// distinguish "empty value" from "no value" (minimized attribute).
        /// </summary>
        private void ConvertToUnresolvedBoundProperty(
            TagHelperIntermediateNode tagHelperNode,
            UnresolvedAttributeIntermediateNode unresolvedAttr,
            TagHelperAttributeMatch match,
            string attributeName,
            RazorSourceDocument sourceDocument)
        {
            var attrStructure = unresolvedAttr.AttributeStructure;
            var nameSpan = unresolvedAttr.AttributeNameSpan;
            var prop = new TagHelperPropertyIntermediateNode(match)
            {
                AttributeName = attributeName,
                AttributeStructure = attrStructure,
                OriginalAttributeSpan = nameSpan,
            };

            if (!unresolvedAttr.IsMinimized)
            {
                var htmlAttrChild = unresolvedAttr.HtmlAttributeNode;

                if (htmlAttrChild != null)
                {
                    LowerUnresolvedAttributeValues(htmlAttrChild, prop, match.ExpectsStringValue, unresolvedAttr.ValueSourceSpan, sourceDocument);
                }

                // If the property still has no children (empty value like Href=""),
                // add a synthetic empty child so downstream consumers can distinguish
                // "empty value" from "no value". This mirrors the same handling in the legacy tag helper path above.
                if (prop.Children.Count == 0)
                {
                    var emptySpan = unresolvedAttr.ValueSourceSpan;
                    if (match.ExpectsStringValue)
                    {
                        prop.Children.Add(CreateEmptyHtmlContent(emptySpan));
                    }
                    else
                    {
                        prop.Children.Add(CreateEmptyCSharpToken(emptySpan));
                    }
                }
            }

            prop.Source = unresolvedAttr.ValueSourceSpan ?? (prop.Children.Count > 0 ? prop.Children[0].Source : null);

            tagHelperNode.Children.Add(prop);
        }

        /// <summary>
        /// Handles attributes with no tag helper binding matches. Creates a
        /// <see cref="TagHelperHtmlAttributeIntermediateNode"/> using the pre-lowered
        /// <see cref="UnresolvedAttributeIntermediateNode.AsTagHelperAttribute"/>. For duplicate
        /// bound directive attributes, wraps expression values in <see cref="CSharpExpressionIntermediateNode"/>.
        /// </summary>
        private static void ConvertToUnresolvedUnboundAttribute(
            TagHelperIntermediateNode tagHelperNode,
            UnresolvedAttributeIntermediateNode unresolvedAttr,
            string attributeName,
            bool isDuplicateBound)
        {
            // Not bound -- re-lower as regular HTML attribute.
            var htmlAttrNode = new TagHelperHtmlAttributeIntermediateNode()
            {
                AttributeName = attributeName,
                AttributeStructure = unresolvedAttr.AttributeStructure,
            };

            if (!unresolvedAttr.IsMinimized && unresolvedAttr.AsTagHelperAttribute is HtmlAttributeIntermediateNode fallbackAttr)
            {
                // Use the pre-lowered fallback form's value children.
                htmlAttrNode.Children.AddRange(fallbackAttr.Children);

                // For duplicate bound directive attributes (e.g. second @formname="@y"),
                // convert CSharpExpressionAttributeValue to CSharpExpression.
                if (isDuplicateBound)
                {
                    ConvertExpressionAttributeValuesToCSharpExpression(htmlAttrNode);
                }

                if (htmlAttrNode.Children.Count == 0)
                {
                    htmlAttrNode.Children.Add(CreateEmptyHtmlContent(null));
                }
            }
            else if (!unresolvedAttr.IsMinimized && unresolvedAttr.AttributeStructure != AttributeStructure.Minimized)
            {
                // Empty-valued attribute with quotes (e.g. onsubmit="") -- add empty content.
                htmlAttrNode.Children.Add(CreateEmptyHtmlContent(null));
            }

            tagHelperNode.Children.Add(htmlAttrNode);
        }

        /// <summary>
        /// Post-processes a bound tag helper property's children to convert markup-shaped IR
        /// to the C# shapes expected by downstream code generation. Since resolution operates on
        /// unrewritten syntax, we get markup shapes (HtmlContent, CSharpExpressionAttributeValue, etc.)
        /// that need conversion to the CSharp token structure that tag helper properties expect.
        ///
        /// When <paramref name="wrapLiteralsInCSharpExpression"/> is true (directive attributes),
        /// literal content is wrapped in <see cref="CSharpExpressionIntermediateNode"/>.
        /// When false (regular bound properties), literals become direct <see cref="CSharpIntermediateToken"/>s.
        /// </summary>
        private static void NormalizeBoundPropertyChildren(IntermediateNode prop, bool wrapLiteralsInCSharpExpression)
        {
            using var newChildren = new PooledArrayBuilder<IntermediateNode>();

            foreach (var child in prop.Children)
            {
                if (child is CSharpExpressionAttributeValueIntermediateNode csharpExprAttrValue)
                {
                    // When an expression appears after literal content (i.e., not at the start
                    // of the attribute value), the @ transition and prefix whitespace must be
                    // merged into the first CSharp token. When Prefix is non-empty, include it
                    // and @ before the expression content. When Prefix is empty, the @ was
                    // consumed as a normal transition and should not appear in the output.
                    if (wrapLiteralsInCSharpExpression && !string.IsNullOrEmpty(csharpExprAttrValue.Prefix))
                    {
                        // If the only child is a CSharpExpression, prepend the prefix+@ to its
                        // first token and reuse it (avoiding double-wrapping).
                        if (csharpExprAttrValue.Children is [CSharpExpressionIntermediateNode innerExpr])
                        {
                            if (innerExpr.Children is [CSharpIntermediateToken firstToken, ..])
                            {
                                innerExpr.Children[0] = new CSharpIntermediateToken(
                                    csharpExprAttrValue.Prefix + "@" + firstToken.Content, firstToken.Source);
                            }

                            newChildren.Add(innerExpr);
                        }
                        else
                        {
                            var expr = new CSharpExpressionIntermediateNode() { Source = csharpExprAttrValue.Source };
                            var prefixedFirst = false;
                            foreach (var token in csharpExprAttrValue.Children)
                            {
                                if (!prefixedFirst && token is CSharpIntermediateToken csharpToken)
                                {
                                    expr.Children.Add(new CSharpIntermediateToken(
                                        csharpExprAttrValue.Prefix + "@" + csharpToken.Content, csharpToken.Source));
                                    prefixedFirst = true;
                                }
                                else
                                {
                                    expr.Children.Add(token);
                                }
                            }

                            newChildren.Add(expr);
                        }
                    }
                    else
                    {
                        // Avoid double-wrapping: if the only child is already a CSharpExpression
                        // (from VisitCSharpImplicitExpression), reuse it directly. Single expression
                        // values should produce direct tokens without an extra wrapper node.
                        if (csharpExprAttrValue.Children is [CSharpExpressionIntermediateNode existingExpr])
                        {
                            newChildren.Add(existingExpr);
                        }
                        else
                        {
                            // CSharpExpressionAttributeValue -> CSharpExpression (always wrapped)
                            var expr = new CSharpExpressionIntermediateNode() { Source = csharpExprAttrValue.Source };
                            expr.Children.AddRange(csharpExprAttrValue.Children);

                            newChildren.Add(expr);
                        }
                    }
                }
                else if (child is CSharpCodeAttributeValueIntermediateNode csharpCodeAttrValue)
                {
                    // CSharpCodeAttributeValue -> CSharpExpression (always wrapped)
                    var expr = new CSharpExpressionIntermediateNode() { Source = csharpCodeAttrValue.Source };
                    expr.Children.AddRange(csharpCodeAttrValue.Children);

                    newChildren.Add(expr);
                }
                else if (child is HtmlContentIntermediateNode or HtmlAttributeValueIntermediateNode)
                {
                    ConvertHtmlTokensToCSharp(child.Children, ref newChildren.AsRef(), child.Source, wrapLiteralsInCSharpExpression);
                }
                else
                {
                    newChildren.Add(child);
                }
            }

            prop.Children.Clear();
            prop.Children.AddRange(in newChildren);

            // After normalization, merge CSharp tokens into a single token for non-directive
            // bound properties. For directives (wrapLiteralsInCSharpExpression=true), CSharpExpression
            // wrappers must be preserved for downstream passes like ComponentBindLoweringPass.
            if (!wrapLiteralsInCSharpExpression)
            {
                MergeAdjacentCSharpTokens(prop);
            }
        }

        /// <summary>
        /// Merges <see cref="CSharpIntermediateToken"/> children (and content from
        /// <see cref="CSharpExpressionIntermediateNode"/> wrappers) within a node into a single
        /// token. Tag helper properties expect a single CSharp token containing the full expression.
        /// Only merges when all children are CSharpIntermediateToken or CSharpExpression containing
        /// only CSharpIntermediateToken.
        /// </summary>
        private static void MergeAdjacentCSharpTokens(IntermediateNode node)
        {
            // Check that all children are flattenable to CSharp tokens.
            var canMerge = node.Children.Count > 1;
            foreach (var child in node.Children)
            {
                if (child is CSharpIntermediateToken)
                {
                    continue;
                }

                if (child is CSharpExpressionIntermediateNode expr
                    && expr.Children.All(static inner => inner is CSharpIntermediateToken))
                {
                    continue;
                }

                // Non-flattenable child -- can't merge
                canMerge = false;
                break;
            }

            if (!canMerge)
            {
                return;
            }

            using var _sb = StringBuilderPool.GetPooledObject(out var sb);
            SourceSpan? firstSpan = null;
            SourceSpan? lastSpan = null;

            foreach (var child in node.Children)
            {
                if (child is CSharpIntermediateToken csharpToken)
                {
                    sb.Append(csharpToken.Content);
                    if (csharpToken.Source is { } s)
                    {
                        firstSpan ??= s;
                        lastSpan = s;
                    }
                }
                else if (child is CSharpExpressionIntermediateNode expr)
                {
                    foreach (var inner in expr.Children)
                    {
                        if (inner is CSharpIntermediateToken innerToken)
                        {
                            sb.Append(innerToken.Content);
                            if (innerToken.Source is { } s)
                            {
                                firstSpan ??= s;
                                lastSpan = s;
                            }
                        }
                    }
                }
            }

            SourceSpan? mergedSpan = null;
            if (firstSpan is { } first && lastSpan is { } last)
            {
                mergedSpan = MergeSourceSpans(first, last);
            }

            var content = sb.ToString();
            node.Children.Clear();
            node.Children.Add(new CSharpIntermediateToken(content, mergedSpan));
        }

        /// <summary>Component path for non-string unresolved attribute values.</summary>
        private static void LowerUnresolvedNonStringAttributeValues_Component(
            HtmlAttributeIntermediateNode htmlAttr,
            IntermediateNode target)
        {
            // Component path: flatten each child individually (no merging).
            foreach (var child in htmlAttr.Children)
            {
                if (child is UnresolvedAttributeValueIntermediateNode unresolvedLiteral)
                {
                    foreach (var valueChild in unresolvedLiteral.Children)
                    {
                        if (valueChild is HtmlIntermediateToken htmlToken)
                        {
                            target.Children.Add(ToCSharpToken(htmlToken));
                        }
                    }
                }
                else if (child is UnresolvedExpressionAttributeValueIntermediateNode unresolvedExpr)
                {
                    FlattenToDirectCSharpTokens(unresolvedExpr, target);
                }
                else
                {
                    FlattenToDirectCSharpTokens(child, target);
                }
            }
        }

        /// <summary>Component path for string unresolved attribute values.</summary>
        private static void LowerUnresolvedStringAttributeValues_Component(
            HtmlAttributeIntermediateNode htmlAttr,
            IntermediateNode target)
        {
            // Component path: process each child individually (no merging).
            foreach (var child in htmlAttr.Children)
            {
                if (child is UnresolvedAttributeValueIntermediateNode unresolvedLiteral)
                {
                    var htmlContent = new HtmlContentIntermediateNode();
                    var prefix = unresolvedLiteral.Prefix;
                    var mergedFirst = false;

                    foreach (var valueChild in unresolvedLiteral.Children)
                    {
                        if (!mergedFirst && !string.IsNullOrEmpty(prefix) && valueChild is HtmlIntermediateToken htmlToken)
                        {
                            // Merge prefix into first token.
                            var mergedContent = prefix + htmlToken.Content;
                            var mergedSource = ExtendSpanBackward(htmlToken.Source, prefix.Length);

                            htmlContent.Children.Add(new HtmlIntermediateToken(mergedContent, mergedSource));
                            htmlContent.Source ??= mergedSource;
                            mergedFirst = true;
                        }
                        else
                        {
                            htmlContent.Children.Add(valueChild);
                            htmlContent.Source ??= valueChild.Source;
                        }
                    }

                    if (!mergedFirst && !string.IsNullOrEmpty(prefix))
                    {
                        htmlContent.Children.Insert(0, new HtmlIntermediateToken(prefix, source: null));
                    }

                    target.Children.Add(htmlContent);
                }
                else if (child is UnresolvedExpressionAttributeValueIntermediateNode unresolvedExpr)
                {
                    // Add prefix (space before @expr) as HtmlContent.
                    if (!string.IsNullOrEmpty(unresolvedExpr.Prefix))
                    {
                        var prefixContent = new HtmlContentIntermediateNode();
                        prefixContent.Children.Add(new HtmlIntermediateToken(unresolvedExpr.Prefix, source: null));
                        target.Children.Add(prefixContent);
                    }

                    // Wrap in CSharpExpression.
                    var expr = new CSharpExpressionIntermediateNode();
                    FlattenToDirectCSharpTokens(unresolvedExpr, expr);
                    expr.Source = expr.Children.Count > 0 ? expr.Children[0].Source : unresolvedExpr.Source;
                    target.Children.Add(expr);
                }
                else if (child is IntermediateToken token)
                {
                    // Wrap bare tokens (e.g. from @@ escape) in HtmlContent.
                    var htmlContent = new HtmlContentIntermediateNode() { Source = token.Source };
                    htmlContent.Children.Add(token);
                    target.Children.Add(htmlContent);
                }
                else
                {
                    target.Children.Add(child);
                }
            }

            // Merge adjacent HtmlContent nodes (e.g. @@ escape "@" + "currentCount" -> "@currentCount").
            MergeAdjacentHtmlContent(target);
        }

        /// <summary>
        /// Post-processes directive attribute children by removing wrapper nodes and inserting
        /// direct CSharp tokens. Used for parameter matches where the value is a simple identifier.
        /// </summary>
        private static void FlattenDirectiveChildrenToCSharpTokens(IntermediateNode directiveNode)
        {
            using var newChildren = new PooledArrayBuilder<IntermediateNode>();
            foreach (var child in directiveNode.Children)
            {
                if (child is HtmlContentIntermediateNode or UnresolvedAttributeValueIntermediateNode)
                {
                    foreach (var token in child.Children)
                    {
                        if (token is HtmlIntermediateToken htmlToken)
                        {
                            newChildren.Add(ToCSharpToken(htmlToken));
                        }
                        else
                        {
                            newChildren.Add(token);
                        }
                    }
                }
                else if (child is CSharpExpressionIntermediateNode or
                         CSharpExpressionAttributeValueIntermediateNode or
                         UnresolvedExpressionAttributeValueIntermediateNode)
                {
                    // Flatten expression children to direct tokens.
                    foreach (var token in child.Children)
                    {
                        newChildren.Add(token);
                    }
                }
                else
                {
                    newChildren.Add(child);
                }
            }
            directiveNode.Children.Clear();
            directiveNode.Children.AddRange(in newChildren);
        }

        /// <summary>
        /// Converts <see cref="CSharpExpressionAttributeValueIntermediateNode"/> children to
        /// <see cref="CSharpExpressionIntermediateNode"/> without converting HTML content or
        /// flattening structure. Used for string-valued directive attributes where the old
        /// pipeline produced <c>CSharpExpression</c> from rewritten syntax.
        /// </summary>
        private static void ConvertExpressionAttributeValuesToCSharpExpression(IntermediateNode node)
        {
            for (var i = node.Children.Count - 1; i >= 0; i--)
            {
                var child = node.Children[i];
                if (child is CSharpExpressionAttributeValueIntermediateNode csharpExprAttrValue)
                {
                    ConvertExpressionChildToCSharpExpression(node, i, csharpExprAttrValue.Prefix, csharpExprAttrValue.Children, csharpExprAttrValue.Source);
                    // ConvertExpressionChildToCSharpExpression may insert a prefix node, adjusting i
                    if (node.Children[i] is HtmlContentIntermediateNode)
                    {
                        i++; // skip past inserted prefix to the expression we just placed
                    }
                }
                else if (child is UnresolvedExpressionAttributeValueIntermediateNode unresolvedExprAttrValue)
                {
                    ConvertExpressionChildToCSharpExpression(node, i, unresolvedExprAttrValue.Prefix, unresolvedExprAttrValue.Children, unresolvedExprAttrValue.Source);
                    if (node.Children[i] is HtmlContentIntermediateNode)
                    {
                        i++;
                    }
                }
                else if (child is HtmlAttributeValueIntermediateNode htmlAttrValue)
                {
                    // Convert HtmlAttributeValue to HtmlContent, merging prefix into first token.
                    var htmlContent = new HtmlContentIntermediateNode();
                    var prefix = htmlAttrValue.Prefix;

                    if (!string.IsNullOrEmpty(prefix) && htmlAttrValue.Children is [IntermediateToken firstToken, ..])
                    {
                        var mergedContent = prefix + firstToken.Content;
                        var mergedSource = ExtendSpanBackward(firstToken.Source, prefix.Length);

                        htmlContent.Children.Add(new HtmlIntermediateToken(mergedContent, mergedSource));
                        htmlContent.Source = mergedSource ?? htmlAttrValue.Source;

                        for (var j = 1; j < htmlAttrValue.Children.Count; j++)
                        {
                            htmlContent.Children.Add(htmlAttrValue.Children[j]);
                        }
                    }
                    else
                    {
                        htmlContent.Source = htmlAttrValue.Source;
                        htmlContent.Children.AddRange(htmlAttrValue.Children);
                    }

                    node.Children[i] = htmlContent;
                }
                else if (node.Children[i] is CSharpCodeAttributeValueIntermediateNode csharpCodeAttrValue)
                {
                    // Convert CSharpCodeAttributeValue to CSharpCode.
                    var csharpCode = new CSharpCodeIntermediateNode() { Source = csharpCodeAttrValue.Source };
                    csharpCode.Children.AddRange(csharpCodeAttrValue.Children);
                    csharpCode.Source = csharpCode.Children.Count > 0 ? csharpCode.Children[0].Source : csharpCodeAttrValue.Source;

                    node.Children[i] = csharpCode;
                }
                else if (node.Children[i] is MarkupBlockIntermediateNode markupBlock)
                {
                    // Convert MarkupBlock to HtmlContent.
                    var htmlContent = new HtmlContentIntermediateNode() { Source = markupBlock.Source };
                    htmlContent.Children.AddRange(markupBlock.Children);

                    node.Children[i] = htmlContent;
                }
            }
        }

        /// <summary>
        /// Converts an expression attribute value child (either <see cref="CSharpExpressionAttributeValueIntermediateNode"/>
        /// or <see cref="UnresolvedExpressionAttributeValueIntermediateNode"/>) to a
        /// <see cref="CSharpExpressionIntermediateNode"/>, optionally inserting a prefix HtmlContent node.
        /// </summary>
        private static void ConvertExpressionChildToCSharpExpression(
            IntermediateNode parent,
            int index,
            string prefix,
            IntermediateNodeCollection children,
            SourceSpan? fallbackSource)
        {
            if (!string.IsNullOrEmpty(prefix))
            {
                var prefixContent = new HtmlContentIntermediateNode();
                prefixContent.Children.Add(new HtmlIntermediateToken(prefix, source: null));
                parent.Children.Insert(index, prefixContent);
                index++;
            }

            var expr = new CSharpExpressionIntermediateNode();
            foreach (var token in children)
            {
                expr.Children.Add(token);
            }
            expr.Source = expr.Children.Count > 0 ? expr.Children[0].Source : fallbackSource;

            parent.Children[index] = expr;
        }

        private static void ConvertComponentAttributeToTagHelper(
            TagHelperIntermediateNode tagHelperNode,
            HtmlAttributeIntermediateNode htmlAttr,
            TagHelperBinding binding)
        {
            var attributeName = htmlAttr.AttributeName;
            using var matches = new PooledArrayBuilder<TagHelperAttributeMatch>();
            TagHelperMatchingConventions.GetAttributeMatches(binding.TagHelpers, attributeName, ref matches.AsRef());

            // Compute the attribute name source span from the HtmlAttributeIntermediateNode.
            // The Source covers the whole attribute; the name span is at the start of Prefix.
            var attributeNameSpan = ComputeAttributeNameSpan(htmlAttr);
            var attributeValueSpan = ComputeAttributeValueSpan(htmlAttr);

            if (matches.Any())
            {
                foreach (var match in matches)
                {
                    if (match.Attribute.IsDirectiveAttribute)
                    {
                        var directiveAttributeName = new DirectiveAttributeName(attributeName);

                        // For directive attributes, the OriginalAttributeSpan should cover
                        // the attribute name WITHOUT the leading '@' (e.g., "bind-Value" not "@bind-Value").
                        // Downstream passes like ComponentBindLoweringPass offset from this span.
                        var directiveNameSpan = attributeNameSpan;
                        if (directiveNameSpan is SourceSpan nameSpan && attributeName.StartsWith('@'))
                        {
                            directiveNameSpan = nameSpan.WithAbsoluteIndex(nameSpan.AbsoluteIndex + 1)
                                .WithCharacterIndex(nameSpan.CharacterIndex + 1)
                                .WithLength(nameSpan.Length - 1);
                        }

                        IntermediateNode directiveNode = match.IsParameterMatch && directiveAttributeName.HasParameter
                            ? new TagHelperDirectiveAttributeParameterIntermediateNode(match)
                            {
                                AttributeName = directiveAttributeName.Text,
                                AttributeNameWithoutParameter = directiveAttributeName.TextWithoutParameter,
                                OriginalAttributeName = attributeName,
                                AttributeStructure = InferAttributeStructure(htmlAttr),
                                Source = attributeValueSpan,
                                OriginalAttributeSpan = directiveNameSpan,
                            }
                            : new TagHelperDirectiveAttributeIntermediateNode(match)
                            {
                                AttributeName = directiveAttributeName.Text,
                                OriginalAttributeName = attributeName,
                                AttributeStructure = InferAttributeStructure(htmlAttr),
                                Source = attributeValueSpan,
                                OriginalAttributeSpan = directiveNameSpan,
                            };

                        CopyAsTagHelperAttributeValues(htmlAttr, directiveNode);

                        if (!match.ExpectsStringValue)
                        {
                            NormalizeBoundPropertyChildren(directiveNode, wrapLiteralsInCSharpExpression: true);
                        }

                        tagHelperNode.Children.Add(directiveNode);
                    }
                    else
                    {
                        var prop = new TagHelperPropertyIntermediateNode(match)
                        {
                            AttributeName = attributeName,
                            AttributeStructure = InferAttributeStructure(htmlAttr),
                            Source = attributeValueSpan,
                            OriginalAttributeSpan = attributeNameSpan,
                        };

                        CopyAsTagHelperAttributeValues(htmlAttr, prop);

                        if (!match.ExpectsStringValue)
                        {
                            NormalizeBoundPropertyChildren(prop, wrapLiteralsInCSharpExpression: false);
                        }

                        tagHelperNode.Children.Add(prop);
                    }
                }
            }
            else
            {
                var thHtml = new TagHelperHtmlAttributeIntermediateNode()
                {
                    AttributeName = attributeName,
                    AttributeStructure = InferAttributeStructure(htmlAttr),
                };

                thHtml.Children.AddRange(htmlAttr.Children);

                // Convert CSharpExpressionAttributeValue to CSharpExpression for unbound
                // attributes that have expression values (e.g. duplicate @formname="@y").
                ConvertExpressionAttributeValuesToCSharpExpression(thHtml);

                tagHelperNode.Children.Add(thHtml);
            }
        }

        /// <summary>
        /// Copies attribute value children into the expected tag helper property IR structure.
        /// Literal values (HtmlAttributeValueIntermediateNode) -> HtmlContentIntermediateNode -> HtmlIntermediateToken.
        /// Expression values (CSharpExpressionAttributeValueIntermediateNode) -> direct CSharpIntermediateToken (no wrapper).
        /// Adjacent literal values are merged into a single HtmlContent node.
        /// </summary>
        private static void CopyAsTagHelperAttributeValues(HtmlAttributeIntermediateNode source, IntermediateNode target)
        {
            // Check if all children are literal attribute values. If so, merge them into a single
            // HtmlContent node since adjacent literal tokens should be combined.
            if (AreAllChildrenOfType<HtmlAttributeValueIntermediateNode>(source.Children) && source.Children.Count > 1)
            {
                // Merge all literal pieces (including their prefixes) into a single HtmlContent.
                var mergedContent = new HtmlContentIntermediateNode()
                {
                    Source = source.Source,
                };

                using var _sb = StringBuilderPool.GetPooledObject(out var sb);
                foreach (var child in source.Children)
                {
                    var htmlValue = (HtmlAttributeValueIntermediateNode)child;
                    sb.Append(CollectAttributeValueContent(htmlValue).Content);
                }

                var mergedText = sb.ToString();

                // Use the source span from the parent HtmlAttribute value portion if available,
                // otherwise compute from the first child.
                var firstValue = (HtmlAttributeValueIntermediateNode)source.Children[0];
                var spanSource = firstValue.Source;
                if (spanSource is { } fs)
                {
                    // Span from first value start to end of the full attribute value
                    var totalLength = source.Children[^1] is HtmlAttributeValueIntermediateNode lastValue && lastValue.Source is { } ls
                        ? (ls.AbsoluteIndex + ls.Length) - fs.AbsoluteIndex
                        : mergedText.Length;
                    spanSource = fs.WithLength(totalLength);
                }

                mergedContent.Source = spanSource;
                mergedContent.Children.Add(new HtmlIntermediateToken(mergedText, spanSource));

                target.Children.Add(mergedContent);
                return;
            }

            foreach (var child in source.Children)
            {
                if (child is HtmlAttributeValueIntermediateNode htmlValue)
                {
                    // Literal value path: VisitAttributeValue -> MarkupTextLiteral -> VisitMarkupTextLiteral
                    // produces HtmlContentIntermediateNode -> HtmlIntermediateToken
                    var htmlContent = new HtmlContentIntermediateNode()
                    {
                        Source = htmlValue.Source,
                    };

                    htmlContent.Children.AddRange(htmlValue.Children);

                    target.Children.Add(htmlContent);
                }
                else if (child is CSharpExpressionAttributeValueIntermediateNode or CSharpCodeAttributeValueIntermediateNode)
                {
                    // Expression/code value: flatten to direct CSharp tokens.
                    FlattenToDirectCSharpTokens(child, target);
                }
                else
                {
                    target.Children.Add(child);
                }
            }
        }

        private static SourceSpan? ComputeAttributeNameSpan(HtmlAttributeIntermediateNode htmlAttr)
        {
            if (htmlAttr.Source is not SourceSpan attrSource)
            {
                return null;
            }

            var nameLength = htmlAttr.AttributeName?.Length ?? 0;
            if (nameLength == 0)
            {
                return attrSource;
            }

            // The Source on HtmlAttributeIntermediateNode includes leading whitespace.
            // The Prefix is " name=\"" -- find where the actual attribute name starts.
            var prefix = htmlAttr.Prefix ?? string.Empty;
            var nameIndex = prefix.IndexOf(htmlAttr.AttributeName!, StringComparison.Ordinal);
            if (nameIndex < 0)
            {
                nameIndex = 0;
            }

            var nameCharIndex = attrSource.CharacterIndex + nameIndex;

            return attrSource.WithAbsoluteIndex(attrSource.AbsoluteIndex + nameIndex)
                .WithCharacterIndex(nameCharIndex)
                .WithLength(nameLength)
                .WithLineCount(0)
                .WithEndCharacterIndex(nameCharIndex + nameLength);
        }

        private static SourceSpan? ComputeAttributeValueSpan(HtmlAttributeIntermediateNode htmlAttr)
        {
            // Try to get the value span directly from the children, which is more accurate.
            if (htmlAttr.Children is [{ Source: SourceSpan childSource }, ..])
            {
                // If there's a single child, use its source. For multiple children, merge spans.
                if (htmlAttr.Children.Count == 1)
                {
                    return childSource;
                }

                // Merge spans of all children.
                var lastChild = htmlAttr.Children[^1];
                if (lastChild.Source is SourceSpan lastSource)
                {
                    var endIndex = lastSource.AbsoluteIndex + lastSource.Length;
                    var length = endIndex - childSource.AbsoluteIndex;
                    var endCharIndex = lastSource.CharacterIndex + lastSource.Length;
                    return childSource.WithLength(length)
                        // Note: does not incorporate lastSource.LineCount; attribute values
                        // spanning multiple lines are uncommon and the old pipeline had the same limitation.
                        .WithLineCount(lastSource.LineIndex - childSource.LineIndex)
                        .WithEndCharacterIndex(endCharIndex);
                }

                return childSource;
            }

            if (htmlAttr.Source is not SourceSpan attrSource)
            {
                return null;
            }

            // Fallback: compute from prefix/suffix.
            var prefix = htmlAttr.Prefix ?? string.Empty;
            var suffix = htmlAttr.Suffix ?? string.Empty;

            var valueStart = prefix.Length;
            var valueLength = attrSource.Length - prefix.Length - suffix.Length;
            if (valueLength <= 0)
            {
                return null;
            }

            var valueCharIndex = attrSource.CharacterIndex + valueStart;

            return attrSource.WithAbsoluteIndex(attrSource.AbsoluteIndex + valueStart)
                .WithCharacterIndex(valueCharIndex)
                .WithLength(valueLength)
                .WithLineCount(0)
                .WithEndCharacterIndex(valueCharIndex + valueLength);
        }

        /// <summary>
        /// Parses a directive attribute name like "@bind-Value:event" into its component parts.
        /// </summary>
        private readonly struct DirectiveAttributeName
        {
            public string Text { get; }
            public string TextWithoutParameter { get; }
            public bool HasParameter { get; }

            public DirectiveAttributeName(string fullAttributeName)
            {
                // Directive attribute names look like:
                //   @bind-Value          (no parameter)
                //   @bind-Value:event    (with parameter)
                //   @onclick             (no parameter)
                //   @onclick:preventDefault (with parameter)

                // Strip leading @
                var span = fullAttributeName.StartsWith('@')
                    ? fullAttributeName.Substring(1)
                    : fullAttributeName;

                var colonIndex = span.IndexOf(':');
                HasParameter = colonIndex >= 0;
                TextWithoutParameter = HasParameter ? span[..colonIndex] : span;
                Text = span;
            }
        }

        /// <summary>
        /// Converts a non-tag-helper element to <see cref="MarkupElementIntermediateNode"/> (component files).
        /// Preserves element structure (tag name, source span). Unresolved attributes are replaced with their
        /// <see cref="UnresolvedAttributeIntermediateNode.AsMarkupAttribute"/> (full attribute form).
        /// </summary>
        public override void ConvertToPlainElement(IntermediateNode parent, int index, UnresolvedElementIntermediateNode elementNode)
        {
            var markupElement = new MarkupElementIntermediateNode()
            {
                Source = elementNode.Source,
                TagName = elementNode.TagName,
            };

            // Move diagnostics.
            markupElement.AddDiagnosticsFromNode(elementNode);

            // Transfer all children, lowering unresolved attributes to their fallback form.
            foreach (var child in elementNode.Children)
            {
                if (child is UnresolvedAttributeIntermediateNode unresolvedAttr)
                {
                    // Use the pre-lowered AsMarkupAttribute fallback form.
                    if (unresolvedAttr.AsMarkupAttribute != null)
                    {
                        markupElement.Children.Add(unresolvedAttr.AsMarkupAttribute);
                    }
                }
                else
                {
                    markupElement.Children.Add(child);
                }
            }

            parent.Children[index] = markupElement;
        }
        private static bool LooksLikeUnexpectedComponent(DocumentIntermediateNode? documentNode, string? tagName)
        {
            return documentNode != null &&
                !documentNode.Options.SuppressPrimaryMethodBody &&
                !string.IsNullOrEmpty(tagName) &&
                DefaultRazorIntermediateNodeLoweringPhase.LooksLikeAComponentName(documentNode, tagName);
        }
    }
}
