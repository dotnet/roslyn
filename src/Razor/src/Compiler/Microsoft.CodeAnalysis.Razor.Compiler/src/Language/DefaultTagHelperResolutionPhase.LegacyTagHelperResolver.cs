// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

internal partial class DefaultTagHelperResolutionPhase
{
    private sealed class LegacyTagHelperResolver : TagHelperResolver
    {
        protected override void LowerComplexNonStringValues(
            HtmlAttributeIntermediateNode htmlAttr,
            IntermediateNode target,
            SourceSpan? valueSourceSpan,
            RazorSourceDocument sourceDocument)
        {
            LowerUnresolvedNonStringAttributeValues_Legacy(htmlAttr, target, valueSourceSpan, sourceDocument);
        }

        protected override void LowerComplexStringValues(
            HtmlAttributeIntermediateNode htmlAttr,
            IntermediateNode target,
            RazorSourceDocument sourceDocument)
        {
            LowerUnresolvedStringAttributeValues_Legacy(htmlAttr, target, sourceDocument);
        }

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
                // Add body node first (like the original lowering).
                tagHelperNode.Children.Add(bodyNode);

                // Use pre-computed boundary indices to identify body content and attribute region.
                var startTagEndIdx = elementNode.StartTagEndIndex;
                var bodyEndIdx = elementNode.BodyEndIndex;

                // Body content is between the boundary indices.
                if (startTagEndIdx >= 0 && bodyEndIdx >= 0 && tagHelperNode.TagMode != TagMode.StartTagOnly)
                {
                    for (var i = startTagEndIdx; i < bodyEndIdx; i++)
                    {
                        bodyNode.Children.Add(elementNode.Children[i]);
                    }
                }

                // Check if the element has dynamic C# expression children (e.g. @s).
                // When present, unbound html attributes are excluded from the tag helper.
                var attrEnd = startTagEndIdx >= 0 ? startTagEndIdx : elementNode.Children.Count;
                var hasDynamicExpressionChild = elementNode.HasDynamicExpressionChild;

                // RZ1031: Tag helpers must not have C# in the element's attribute declaration area.
                if (hasDynamicExpressionChild)
                {
                    TryAddCSharpInDeclarationDiagnostic(tagHelperNode, elementNode, attrEnd);
                }

                // Process attributes before StartTagEnd.
                for (var i = 0; i < attrEnd; i++)
                {
                    var child = elementNode.Children[i];
                    if (child is UnresolvedAttributeIntermediateNode unresolvedAttr)
                    {
                        if (hasDynamicExpressionChild)
                        {
                            if (!TagHelperMatchingConventions.HasAttributeMatches(binding.TagHelpers, unresolvedAttr.AttributeName))
                            {
                                continue;
                            }
                        }

                        ConvertUnresolvedLegacyAttribute(tagHelperNode, unresolvedAttr, binding, ref renderedBoundAttributeNames, sourceDocument, in context);
                    }
                    else if (child is HtmlAttributeIntermediateNode htmlAttr)
                    {
                        if (hasDynamicExpressionChild)
                        {
                            continue;
                        }

                        ConvertAttributeToTagHelper(tagHelperNode, htmlAttr, binding, ref renderedBoundAttributeNames, sourceDocument);
                    }
                }
            }
            finally
            {
                renderedBoundAttributeNames.Dispose();
            }
        }

        /// <summary>
        /// Resolves an unresolved attribute against the tag helper binding for a legacy (non-component)
        /// tag helper. If the attribute matches a bound property, creates a <see cref="TagHelperPropertyIntermediateNode"/>
        /// with appropriately lowered value children. Otherwise, creates a <see cref="TagHelperHtmlAttributeIntermediateNode"/>.
        /// </summary>
        private void ConvertUnresolvedLegacyAttribute(
            TagHelperIntermediateNode tagHelperNode,
            UnresolvedAttributeIntermediateNode unresolvedAttr,
            TagHelperBinding binding,
            ref PooledHashSet<string> renderedBoundAttributeNames,
            RazorSourceDocument sourceDocument,
            in ResolutionContext context)
        {
            var attributeName = unresolvedAttr.AttributeName;
            using var matches = new PooledArrayBuilder<TagHelperAttributeMatch>();
            TagHelperMatchingConventions.GetAttributeMatches(binding.TagHelpers, attributeName, ref matches.AsRef());

            // Track this attribute name to detect duplicates. The Add call intentionally
            // uses short-circuit evaluation: if there are no matches, we skip adding so
            // a later minimized attribute with the same name can still match.
            if (!matches.Any() || !renderedBoundAttributeNames.Add(attributeName))
            {
                // Not bound -- convert unresolved children to TagHelperHtmlAttribute.
                ConvertUnresolvedToUnboundLegacyAttribute(tagHelperNode, unresolvedAttr);
                return;
            }

            // Bound attribute -- create a TagHelperPropertyIntermediateNode for each match.
            foreach (var match in matches)
            {
                var attrStructure = unresolvedAttr.AttributeStructure;

                if (attrStructure == AttributeStructure.Minimized)
                {
                    ConvertMinimizedBoundAttribute(tagHelperNode, unresolvedAttr, attributeName, match);
                    continue;
                }

                // Non-minimized bound attribute: lower the value children.
                var prop = new TagHelperPropertyIntermediateNode(match)
                {
                    AttributeName = attributeName,
                    AttributeStructure = attrStructure,
                };

                LowerBoundLegacyAttributeValue(prop, unresolvedAttr, match, sourceDocument);

                // RZ2008: Non-string bound attribute with empty or whitespace value.
                if (!match.ExpectsStringValue && HasOnlyWhitespaceContent(prop))
                {
                    var propertyType = match.IsIndexerMatch ? match.Attribute.IndexerTypeName : match.Attribute.TypeName;
                    tagHelperNode.AddDiagnostic(
                        RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                            unresolvedAttr.AttributeNameSpan ?? SourceSpan.Undefined, attributeName, tagHelperNode.TagName, propertyType!));
                }

                prop.Source= unresolvedAttr.ValueSourceSpan ?? (prop.Children.Count > 0 ? prop.Children[0].Source : null);

                tagHelperNode.Children.Add(prop);
            }
        }

        /// <summary>
        /// Handles a minimized bound attribute (e.g., <c>&lt;input disabled&gt;</c>). If the bound
        /// property expects a boolean value, creates a minimized property node. Otherwise, emits RZ2008.
        /// </summary>
        private static void ConvertMinimizedBoundAttribute(
            TagHelperIntermediateNode tagHelperNode,
            UnresolvedAttributeIntermediateNode unresolvedAttr,
            string attributeName,
            TagHelperAttributeMatch match)
        {
            if (!match.ExpectsBooleanValue)
            {
                // RZ2008: Minimized non-boolean bound attribute requires a value.
                var propertyType = match.IsIndexerMatch ? match.Attribute.IndexerTypeName : match.Attribute.TypeName;
                tagHelperNode.AddDiagnostic(
                    RazorDiagnosticFactory.CreateTagHelper_EmptyBoundAttribute(
                        unresolvedAttr.AttributeNameSpan ?? SourceSpan.Undefined, attributeName, tagHelperNode.TagName, propertyType!));
                return;
            }

            var prop = new TagHelperPropertyIntermediateNode(match)
            {
                AttributeName = attributeName,
                AttributeStructure = AttributeStructure.Minimized,
                Source = null,
            };
            tagHelperNode.Children.Add(prop);
        }

        /// <summary>
        /// Lowers the value of a non-minimized bound legacy attribute, populating the property node's
        /// children with the appropriate IR (HtmlContent for string properties, CSharp tokens for non-string).
        /// Adds an empty placeholder child if the value is empty (e.g., <c>type=""</c>).
        /// </summary>
        private void LowerBoundLegacyAttributeValue(
            TagHelperPropertyIntermediateNode prop,
            UnresolvedAttributeIntermediateNode unresolvedAttr,
            TagHelperAttributeMatch match,
            RazorSourceDocument sourceDocument)
        {
            var htmlAttrChild = unresolvedAttr.HtmlAttributeNode;

            if (htmlAttrChild != null)
            {
                LowerUnresolvedAttributeValues(htmlAttrChild, prop, match.ExpectsStringValue, unresolvedAttr.ValueSourceSpan, sourceDocument);
            }

            // If the property still has no children (empty value like type="" or checked=),
            // add a synthetic empty child so downstream consumers can distinguish
            // "empty value" from "no value" (minimized attribute).
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

        private static void ConvertUnresolvedToUnboundLegacyAttribute(
            TagHelperIntermediateNode tagHelperNode,
            UnresolvedAttributeIntermediateNode unresolvedAttr)
        {
            var htmlAttrNode = new TagHelperHtmlAttributeIntermediateNode()
            {
                AttributeName = unresolvedAttr.AttributeName,
                AttributeStructure = unresolvedAttr.AttributeStructure,
            };

            if (!unresolvedAttr.IsMinimized)
            {
                var htmlAttrChild = unresolvedAttr.HtmlAttributeNode;

                if (htmlAttrChild != null)
                {
                    ConvertUnresolvedValuesToBasicForm(htmlAttrChild, htmlAttrNode);
                }

                if (htmlAttrNode.Children.Count == 0)
                {
                    htmlAttrNode.Children.Add(CreateEmptyHtmlContent(unresolvedAttr.ValueSourceSpan));
                }
            }

            tagHelperNode.Children.Add(htmlAttrNode);
        }

        private static void ConvertAttributeToTagHelper(
            TagHelperIntermediateNode tagHelperNode,
            HtmlAttributeIntermediateNode htmlAttr,
            TagHelperBinding binding,
            ref PooledHashSet<string> renderedBoundAttributeNames,
            RazorSourceDocument sourceDocument)
        {
            var attributeName = htmlAttr.AttributeName;
            var attributeStructure = InferAttributeStructure(htmlAttr);

            using var matches = new PooledArrayBuilder<TagHelperAttributeMatch>();
            TagHelperMatchingConventions.GetAttributeMatches(binding.TagHelpers, attributeName, ref matches.AsRef());

            // Add returns false for duplicates — only the first match for each name gets converted.
            if (matches.Any() && renderedBoundAttributeNames.Add(attributeName))
            {
                foreach (var match in matches)
                {
                    if (attributeStructure == AttributeStructure.Minimized)
                    {
                        // Minimized bound attribute (e.g., <input disabled>).
                        if (!match.ExpectsBooleanValue)
                        {
                            return;
                        }

                        var setTagHelperProperty = new TagHelperPropertyIntermediateNode(match)
                        {
                            AttributeName = attributeName,
                            AttributeStructure = AttributeStructure.Minimized,
                            Source = null,
                        };
                        tagHelperNode.Children.Add(setTagHelperProperty);
                    }
                    else
                    {
                        var isBoundStringProperty = match.ExpectsStringValue;

                        var setTagHelperProperty = new TagHelperPropertyIntermediateNode(match)
                        {
                            AttributeName = attributeName,
                            AttributeStructure = attributeStructure,
                            Source = ComputeValueSource(htmlAttr),
                        };

                        ConvertValueChildren(setTagHelperProperty, htmlAttr, isBoundStringProperty, sourceDocument);
                        tagHelperNode.Children.Add(setTagHelperProperty);
                    }
                }
            }
            else
            {
                var htmlAttribute = new TagHelperHtmlAttributeIntermediateNode()
                {
                    AttributeName = attributeName,
                    AttributeStructure = attributeStructure,
                };

                if (attributeStructure != AttributeStructure.Minimized)
                {
                    PopulateUnboundAttributeValue(htmlAttribute, htmlAttr, sourceDocument);
                }

                tagHelperNode.Children.Add(htmlAttribute);
            }
        }

        /// <summary>
        /// Populates an unbound <see cref="TagHelperHtmlAttributeIntermediateNode"/> with the correct
        /// value children based on the attribute's content type: dynamic expressions are kept as-is,
        /// all-literal values are flattened for preallocation, mixed values are unwrapped individually,
        /// and empty values get a placeholder HtmlContent.
        /// </summary>
        private static void PopulateUnboundAttributeValue(
            TagHelperHtmlAttributeIntermediateNode htmlAttribute,
            HtmlAttributeIntermediateNode htmlAttr,
            RazorSourceDocument sourceDocument)
        {
            // Classify the attribute value children.
            var hasTrueDynamicSegments = false;
            var hasHtmlAttributeValues = false;
            foreach (var child in htmlAttr.Children)
            {
                if (child is CSharpExpressionAttributeValueIntermediateNode csharpSeg)
                {
                    hasTrueDynamicSegments |= !IsLiteralEscapeSegment(csharpSeg);
                }
                else if (child is CSharpCodeAttributeValueIntermediateNode)
                {
                    hasTrueDynamicSegments = true;
                }
                else if (child is HtmlAttributeValueIntermediateNode)
                {
                    hasHtmlAttributeValues = true;
                }
            }

            if (hasTrueDynamicSegments)
            {
                // True dynamic segments: keep children as-is for AddHtmlAttributeValue pattern.
                htmlAttribute.Children.AddRange(htmlAttr.Children);
            }
            else if (hasHtmlAttributeValues)
            {
                PopulateUnboundLiteralOrMixedValue(htmlAttribute, htmlAttr);
            }
            else if (htmlAttr.Children.Count == 0)
            {
                // Empty value (like class=""): create empty HtmlContent for preallocation.
                var emptyHtml = new HtmlContentIntermediateNode() { Source = htmlAttr.Source };
                emptyHtml.Children.Add(new HtmlIntermediateToken(string.Empty, htmlAttr.Source));
                htmlAttribute.Children.Add(emptyHtml);
            }
            else
            {
                // No HtmlAttributeValues, no dynamic segments, but has children (e.g. @@-escaped content):
                // collect into a single HtmlContentIntermediateNode for preallocation.
                ConvertValueChildren(htmlAttribute, htmlAttr, isBoundStringProperty: true, sourceDocument);
            }

            // Merge adjacent HtmlContent in the unbound html attribute,
            // but only if children were processed (not transferred as-is).
            if (hasTrueDynamicSegments || hasHtmlAttributeValues)
            {
                DefaultTagHelperResolutionPhase.MergeAdjacentHtmlContent(htmlAttribute);
            }
        }

        /// <summary>
        /// Handles unbound attribute values that contain <see cref="HtmlAttributeValueIntermediateNode"/>
        /// children (and possibly literal @@ escape segments). Flattens all-literal values into a single
        /// HtmlContent, or unwraps mixed values individually.
        /// </summary>
        private static void PopulateUnboundLiteralOrMixedValue(
            TagHelperHtmlAttributeIntermediateNode htmlAttribute,
            HtmlAttributeIntermediateNode htmlAttr)
        {
            // Check if all content is literal.
            var allLiteral = true;
            foreach (var child in htmlAttr.Children)
            {
                if (child is HtmlAttributeValueIntermediateNode)
                {
                    continue; // literal
                }
                else if (child is CSharpExpressionAttributeValueIntermediateNode csharpSeg && IsLiteralEscapeSegment(csharpSeg))
                {
                    continue; // @@ escape, treated as literal
                }
                else
                {
                    allLiteral = false;
                    break;
                }
            }

            if (allLiteral)
            {
                // All literal segments: flatten to single HtmlContentIntermediateNode for preallocation.
                FlattenLiteralAttributeValue(htmlAttribute, htmlAttr);
            }
            else
            {
                // Mixed HtmlAttributeValue + CSharpExpression (data-dash style):
                // Unwrap HtmlAttributeValue to HtmlContent, keep CSharpExpression as-is.
                foreach (var child in htmlAttr.Children)
                {
                    if (child is HtmlAttributeValueIntermediateNode attrValue)
                    {
                        var (content, tokenSource) = CollectAttributeValueContent(attrValue);
                        if (content.Length > 0)
                        {
                            var htmlContent = new HtmlContentIntermediateNode() { Source = tokenSource };
                            htmlContent.Children.Add(new HtmlIntermediateToken(content, tokenSource));
                            htmlAttribute.Children.Add(htmlContent);
                        }
                    }
                    else
                    {
                        htmlAttribute.Children.Add(child);
                    }
                }
            }
        }

        /// <summary>
        /// Converts value children from an HtmlAttributeIntermediateNode to the target node.
        /// For bound string properties, flattens HtmlAttributeValueIntermediateNode to HtmlContentIntermediateNode.
        /// For bound non-string properties, converts to CSharpIntermediateToken(s).
        /// </summary>
        private static void ConvertValueChildren(IntermediateNode targetNode, HtmlAttributeIntermediateNode htmlAttr, bool isBoundStringProperty, RazorSourceDocument sourceDocument)
        {
            if (htmlAttr.Children.Count == 0)
            {
                // Empty value - compute source span from attribute prefix position.
                if (isBoundStringProperty)
                {
                    var htmlContent = new HtmlContentIntermediateNode();
                    htmlContent.Children.Add(new HtmlIntermediateToken(string.Empty, source: null));
                    targetNode.Children.Add(htmlContent);
                }
                else
                {
                    var emptySource = ComputeEmptyValueSource(htmlAttr);
                    targetNode.Children.Add(new CSharpIntermediateToken(string.Empty, emptySource));
                }
                return;
            }

            // Check if all children are simple HtmlAttributeValueIntermediateNode (literal segments).
            var allLiteral = true;
            var hasDynamicContent = false;
            foreach (var child in htmlAttr.Children)
            {
                if (child is CSharpExpressionAttributeValueIntermediateNode or
                    CSharpCodeAttributeValueIntermediateNode or
                    CSharpExpressionIntermediateNode or
                    CSharpCodeIntermediateNode)
                {
                    allLiteral = false;
                    hasDynamicContent = true;
                    break;
                }
                else if (child is not HtmlAttributeValueIntermediateNode &&
                         child is not HtmlContentIntermediateNode)
                {
                    // Non-literal, non-dynamic (e.g. unresolved wrapper nodes).
                    allLiteral = false;
                    break;
                }
            }

            if (allLiteral)
            {
                // All literal content: collect the text content.
                var content = CollectLiteralContent(htmlAttr);
                var source = ComputeValueSource(htmlAttr);

                if (isBoundStringProperty)
                {
                    var htmlContent = new HtmlContentIntermediateNode() { Source = source };
                    htmlContent.Children.Add(new HtmlIntermediateToken(content, source));
                    targetNode.Children.Add(htmlContent);
                }
                else
                {
                    // For non-string bound properties, use CSharpIntermediateToken directly
                    // (not wrapped in CSharpExpressionIntermediateNode) so the optimization pass
                    // can detect enum types and add the type prefix.
                    targetNode.Children.Add(new CSharpIntermediateToken(content, source));
                }
            }
            else if (!isBoundStringProperty && hasDynamicContent)
            {
                ConvertDynamicNonStringValueChildren(targetNode, htmlAttr, sourceDocument);
            }
            else
            {
                // Bound string property with dynamic content, or complex/non-dynamic fallback:
                // unwrap attribute value nodes to content nodes for BeginWriteTagHelperAttribute pattern.
                UnwrapValueChildrenToTokens(targetNode, htmlAttr);
            }

            // Merge adjacent HtmlContentIntermediateNode children.
            DefaultTagHelperResolutionPhase.MergeAdjacentHtmlContent(targetNode);
        }

        /// <summary>
        /// Handles non-string bound properties with dynamic content (expressions/code blocks).
        /// Three sub-cases: (1) mixed literal+expression with @@ escape pattern,
        /// (2) mixed literal+expression without escape, (3) pure C# expressions.
        /// </summary>
        private static void ConvertDynamicNonStringValueChildren(IntermediateNode targetNode, HtmlAttributeIntermediateNode htmlAttr, RazorSourceDocument sourceDocument)
        {
            // Check if there are literal HTML segments mixed with C# expressions.
            var hasLiteralSegments = false;
            foreach (var child in htmlAttr.Children)
            {
                if (child is HtmlAttributeValueIntermediateNode or HtmlContentIntermediateNode)
                {
                    hasLiteralSegments = true;
                    break;
                }
            }

            if (hasLiteralSegments)
            {
                ConvertMixedLiteralAndExpressionValue(targetNode, htmlAttr, sourceDocument);
            }
            else
            {
                ConvertPureCSharpExpressionValue(targetNode, htmlAttr, sourceDocument);
            }
        }

        /// <summary>
        /// Handles non-string bound properties with mixed literal + C# expression content.
        /// Detects @@ escape patterns and source-text-extracts the value when possible.
        /// </summary>
        private static void ConvertMixedLiteralAndExpressionValue(IntermediateNode targetNode, HtmlAttributeIntermediateNode htmlAttr, RazorSourceDocument sourceDocument)
        {
            // Check if this is a @@expr pattern (escape + explicit expression)
            // which needs special token handling. Detected by an @@ literal escape
            // segment among the children.
            foreach (var child in htmlAttr.Children)
            {
                if (child is CSharpExpressionAttributeValueIntermediateNode csharpSeg && IsLiteralEscapeSegment(csharpSeg))
                {
                    ConvertEscapedAtExpressionValue(targetNode, htmlAttr);
                    return;
                }
            }

            if (htmlAttr.Source is SourceSpan attrSource && sourceDocument != null)
            {
                // Non-escape mixed content: extract value from source text.
                var prefix = htmlAttr.Prefix ?? string.Empty;
                var suffix = htmlAttr.Suffix ?? string.Empty;
                var valueStart = attrSource.AbsoluteIndex + prefix.Length;
                var valueLength = attrSource.Length - prefix.Length - suffix.Length;
                if (valueLength > 0)
                {
                    // If the value starts with @ (Razor transition for the first expression),
                    // strip it to get the C# code (e.g., @DateTimeOffset.Now.Year -> DateTimeOffset.Now.Year).
                    if (sourceDocument.Text[valueStart] == '@' &&
                        htmlAttr.Children.Count > 0 &&
                        htmlAttr.Children[0] is CSharpExpressionAttributeValueIntermediateNode)
                    {
                        valueStart++;
                        valueLength--;
                    }

                    var sourceText = sourceDocument.Text.ToString(
                        new Microsoft.CodeAnalysis.Text.TextSpan(valueStart, valueLength));

                    // Compute value source span with correct line/char positions.
                    var valueCharIndex = attrSource.CharacterIndex + prefix.Length + (attrSource.AbsoluteIndex + prefix.Length < valueStart ? 1 : 0);
                    var valueEndCharIndex = attrSource.EndCharacterIndex - suffix.Length;
                    var valueSource = new SourceSpan(
                        attrSource.FilePath, valueStart, attrSource.LineIndex, valueCharIndex,
                        valueLength, attrSource.LineCount, valueEndCharIndex);
                    targetNode.Children.Add(new CSharpIntermediateToken(sourceText, valueSource));
                }
            }
            else
            {
                UnwrapValueChildrenToTokens(targetNode, htmlAttr);
            }
        }

        /// <summary>
        /// Handles the <c>@@expr</c> escape pattern for non-string bound properties.
        /// Deduplicates literal <c>@</c> tokens, inserts synthetic empty + transition tokens,
        /// and splits explicit <c>@(expr)</c> into structured token output.
        /// </summary>
        private static void ConvertEscapedAtExpressionValue(IntermediateNode targetNode, HtmlAttributeIntermediateNode htmlAttr)
        {
            SourceSpan? lastAtTokenSource = null;
            foreach (var child in htmlAttr.Children)
            {
                if (child is HtmlContentIntermediateNode hc2)
                {
                    foreach (var token in hc2.Children)
                    {
                        if (token is IntermediateToken intermediateToken)
                        {
                            // Skip duplicate @ tokens at the same position
                            if (intermediateToken.Content == "@" && intermediateToken.Source is SourceSpan src)
                            {
                                if (lastAtTokenSource is SourceSpan last && last.AbsoluteIndex == src.AbsoluteIndex)
                                {
                                    continue; // duplicate
                                }
                                targetNode.Children.Add(new CSharpIntermediateToken(intermediateToken.Content, intermediateToken.Source));
                                lastAtTokenSource = src;
                            }
                            else
                            {
                                targetNode.Children.Add(new CSharpIntermediateToken(intermediateToken.Content, intermediateToken.Source));
                            }
                        }
                    }
                }
                else if (child is HtmlAttributeValueIntermediateNode htmlAttrValue)
                {
                    var attrContent = (htmlAttrValue.Prefix ?? string.Empty);
                    foreach (var token in htmlAttrValue.Children)
                    {
                        if (token is IntermediateToken intermediateToken)
                        {
                            attrContent += intermediateToken.Content;
                        }
                    }
                    if (attrContent.Length > 0)
                    {
                        var tokenSource = htmlAttrValue.Children.Count > 0 ? htmlAttrValue.Children[0].Source : htmlAttrValue.Source;
                        if (attrContent == "@" && tokenSource is SourceSpan ats)
                        {
                            if (lastAtTokenSource is SourceSpan last && last.AbsoluteIndex == ats.AbsoluteIndex)
                            {
                                continue;
                            }
                            lastAtTokenSource = ats;
                        }
                        targetNode.Children.Add(new CSharpIntermediateToken(attrContent, tokenSource));
                    }
                }
                else if (child is CSharpExpressionAttributeValueIntermediateNode csharpExprValue)
                {
                    // Before the explicit expression, add empty + @ transition tokens
                    // to match the baseline's VisitAttributeValue @@-pattern handling.
                    if (lastAtTokenSource is SourceSpan atSrc)
                    {
                        var transAbsIdx = atSrc.AbsoluteIndex + 2;
                        var transCharIdx = atSrc.CharacterIndex + 2;
                        // Empty token (MarkupEphemeralTextLiteral equivalent)
                        var emptySource = new SourceSpan(atSrc.FilePath, transAbsIdx, atSrc.LineIndex, transCharIdx, 0, 0, transCharIdx);
                        targetNode.Children.Add(new CSharpIntermediateToken(string.Empty, emptySource));
                        // @ transition token
                        var transSource = new SourceSpan(atSrc.FilePath, transAbsIdx, atSrc.LineIndex, transCharIdx, 1, 0, transCharIdx + 1);
                        targetNode.Children.Add(new CSharpIntermediateToken("@", transSource));
                    }

                    // For explicit expression @(expr): produce (, expr, ) tokens
                    if (csharpExprValue.Children.Count > 0)
                    {
                        var firstInnerChild = csharpExprValue.Children[0];
                        if (firstInnerChild.Source is SourceSpan innerSource)
                        {
                            var openAbsIndex = innerSource.AbsoluteIndex - 1;
                            var openCharIndex = innerSource.CharacterIndex - 1;
                            var openSource = new SourceSpan(innerSource.FilePath, openAbsIndex, innerSource.LineIndex, openCharIndex, 1, 0, openCharIndex + 1);
                            targetNode.Children.Add(new CSharpIntermediateToken("(", openSource));

                            foreach (var innerChild in csharpExprValue.Children)
                            {
                                targetNode.Children.Add(innerChild);
                            }

                            var lastInnerChild = csharpExprValue.Children[^1];
                            if (lastInnerChild.Source is SourceSpan lastSource)
                            {
                                var closeAbsIndex = lastSource.AbsoluteIndex + lastSource.Length;
                                var closeCharIndex = lastSource.EndCharacterIndex;
                                var closeSource = new SourceSpan(lastSource.FilePath, closeAbsIndex, lastSource.LineIndex, closeCharIndex, 1, 0, closeCharIndex + 1);
                                targetNode.Children.Add(new CSharpIntermediateToken(")", closeSource));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles non-string bound properties with pure C# expression content (no literal segments).
        /// For explicit expressions <c>@(expr)</c>, splits into structured tokens.
        /// For implicit expressions <c>@expr</c>, uses <see cref="UnwrapValueChildrenToTokens"/>.
        /// </summary>
        private static void ConvertPureCSharpExpressionValue(IntermediateNode targetNode, HtmlAttributeIntermediateNode htmlAttr, RazorSourceDocument sourceDocument)
        {
            if (htmlAttr.Source is SourceSpan attrSource && sourceDocument != null)
            {
                var prefix = htmlAttr.Prefix ?? string.Empty;
                var suffix = htmlAttr.Suffix ?? string.Empty;
                var valueStart = attrSource.AbsoluteIndex + prefix.Length;
                var valueLength = attrSource.Length - prefix.Length - suffix.Length;
                if (valueLength > 0)
                {
                    var rawText = sourceDocument.Text.ToString(
                        new Microsoft.CodeAnalysis.Text.TextSpan(valueStart, valueLength));
                    // Only use source text extraction for explicit expressions @(...)
                    // which have delimiters that are lost in the IR.
                    // For implicit expressions @expr, use UnwrapValueChildrenToTokens
                    // which produces CSharpExpressionIntermediateNode (no enum prefix).
                    if (rawText.StartsWith("@(", StringComparison.Ordinal))
                    {
                        // Explicit expression @(expr): split into (, expr, ) tokens
                        // to match baseline's separate token structure.
                        var openParenAbsIndex = valueStart + 1; // after @
                        var openParenCharIndex = attrSource.CharacterIndex + prefix.Length + 1;
                        var openParenSource = new SourceSpan(
                            attrSource.FilePath, openParenAbsIndex, attrSource.LineIndex, openParenCharIndex,
                            1, 0, openParenCharIndex + 1);
                        targetNode.Children.Add(new CSharpIntermediateToken("(", openParenSource));

                        // Inner expression content from the CSharpExpressionAttributeValueIntermediateNode
                        foreach (var child in htmlAttr.Children)
                        {
                            if (child is CSharpExpressionAttributeValueIntermediateNode csharpAttrVal)
                            {
                                foreach (var innerChild in csharpAttrVal.Children)
                                {
                                    if (innerChild is CSharpIntermediateToken innerToken)
                                    {
                                        targetNode.Children.Add(new CSharpIntermediateToken(innerToken.Content, innerToken.Source));
                                    }
                                    else
                                    {
                                        targetNode.Children.Add(innerChild);
                                    }
                                }
                            }
                        }

                        var closeParenAbsIndex = valueStart + valueLength - 1; // last char
                        var closeParenCharIndex = attrSource.CharacterIndex + prefix.Length + valueLength - 1;
                        var closeParenSource = new SourceSpan(
                            attrSource.FilePath, closeParenAbsIndex, attrSource.LineIndex, closeParenCharIndex,
                            1, 0, closeParenCharIndex + 1);
                        targetNode.Children.Add(new CSharpIntermediateToken(")", closeParenSource));
                        return;
                    }
                }
            }

            UnwrapValueChildrenToTokens(targetNode, htmlAttr);
        }

        private static void UnwrapValueChildrenToTokens(IntermediateNode targetNode, HtmlAttributeIntermediateNode htmlAttr)
        {
            foreach (var child in htmlAttr.Children)
            {
                if (child is HtmlAttributeValueIntermediateNode attrValue)
                {
                    var (content, tokenSource) = CollectAttributeValueContent(attrValue);
                    if (content.Length > 0)
                    {
                        var htmlContent = new HtmlContentIntermediateNode() { Source = tokenSource };
                        htmlContent.Children.Add(new HtmlIntermediateToken(content, tokenSource));
                        targetNode.Children.Add(htmlContent);
                    }
                }
                else if (child is CSharpExpressionAttributeValueIntermediateNode csharpAttrValue)
                {
                    UnwrapCSharpAttributeValue(targetNode, csharpAttrValue.Prefix, csharpAttrValue.Children,
                        static (token) => new CSharpExpressionIntermediateNode() { Source = token.Source });
                }
                else if (child is CSharpCodeAttributeValueIntermediateNode csharpCodeAttrValue)
                {
                    UnwrapCSharpAttributeValue(targetNode, csharpCodeAttrValue.Prefix, csharpCodeAttrValue.Children,
                        static (token) => new CSharpCodeIntermediateNode() { Source = token.Source });
                }
                else
                {
                    targetNode.Children.Add(child);
                }
            }
        }

        /// <summary>
        /// Unwraps a C# attribute value node (expression or code) into the target, emitting a prefix
        /// HtmlContent if present, then wrapping each CSharp token in a node created by
        /// <paramref name="createCSharpWrapper"/> and each HTML token in an HtmlContent.
        /// </summary>
        private static void UnwrapCSharpAttributeValue(
            IntermediateNode targetNode,
            string prefix,
            IntermediateNodeCollection children,
            Func<CSharpIntermediateToken, IntermediateNode> createCSharpWrapper)
        {
            if (!string.IsNullOrEmpty(prefix))
            {
                var prefixContent = new HtmlContentIntermediateNode();
                prefixContent.Children.Add(new HtmlIntermediateToken(prefix, source: null));
                targetNode.Children.Add(prefixContent);
            }

            foreach (var innerChild in children)
            {
                if (innerChild is CSharpIntermediateToken csharpToken)
                {
                    var wrapper = createCSharpWrapper(csharpToken);
                    wrapper.Children.Add(csharpToken);
                    targetNode.Children.Add(wrapper);
                }
                else if (innerChild is HtmlIntermediateToken htmlToken)
                {
                    var htmlContent = new HtmlContentIntermediateNode() { Source = htmlToken.Source };
                    htmlContent.Children.Add(htmlToken);
                    targetNode.Children.Add(htmlContent);
                }
                else
                {
                    targetNode.Children.Add(innerChild);
                }
            }
        }

        /// <summary>
        /// Computes a source span for an empty attribute value (the position between the quotes or after =).
        /// </summary>
        private static SourceSpan? ComputeEmptyValueSource(HtmlAttributeIntermediateNode htmlAttr)
        {
            if (htmlAttr.Source is not SourceSpan attrSource)
            {
                return null;
            }

            var prefix = htmlAttr.Prefix ?? string.Empty;

            // Value position is after the prefix, before the suffix.
            var valueAbsIndex = attrSource.AbsoluteIndex + prefix.Length;
            var valueCharIndex = attrSource.CharacterIndex + prefix.Length;

            return new SourceSpan(
                attrSource.FilePath,
                valueAbsIndex,
                attrSource.LineIndex,
                valueCharIndex,
                length: 0,
                lineCount: 0,
                endCharacterIndex: valueCharIndex);
        }

        /// <summary>
        /// Collects all literal text content from an HtmlAttributeIntermediateNode's children.
        /// </summary>
        private static string CollectLiteralContent(HtmlAttributeIntermediateNode htmlAttr)
        {
            using var _ = StringBuilderPool.GetPooledObject(out var sb);

            foreach (var child in htmlAttr.Children)
            {
                if (child is HtmlAttributeValueIntermediateNode attrValue)
                {
                    sb.Append(CollectAttributeValueContent(attrValue).Content);
                }
                else if (child is CSharpExpressionAttributeValueIntermediateNode csharpSeg && IsLiteralEscapeSegment(csharpSeg))
                {
                    // @@ escape: collect the literal content (e.g., "@" from @@).
                    sb.Append(csharpSeg.Prefix ?? string.Empty);
                    AppendTokenContent(sb, csharpSeg.Children);
                }
                else if (child is HtmlContentIntermediateNode htmlContent)
                {
                    AppendTokenContent(sb, htmlContent.Children);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Checks if a CSharpExpressionAttributeValueIntermediateNode is a literal escape (like @@ -> "@").
        /// This is the case when the segment contains a single token with literal content.
        /// </summary>
        private static bool IsLiteralEscapeSegment(CSharpExpressionAttributeValueIntermediateNode segment)
        {
            // @@ escape produces a CSharpExpressionAttributeValueIntermediateNode with:
            // - Empty or whitespace prefix
            // - A single CSharpIntermediateToken or HtmlIntermediateToken with "@" content
            return segment.Children is [IntermediateToken { Content: "@" }];
        }

        private static void AppendTokenContent(StringBuilder sb, IntermediateNodeCollection children)
        {
            foreach (var token in children)
            {
                if (token is IntermediateToken intermediateToken)
                {
                    sb.Append(intermediateToken.Content);
                }
            }
        }

        /// <summary>
        /// Flattens all literal HtmlAttributeValueIntermediateNode children into a single HtmlContentIntermediateNode.
        /// </summary>
        private static void FlattenLiteralAttributeValue(IntermediateNode targetNode, HtmlAttributeIntermediateNode htmlAttr)
        {
            var content = CollectLiteralContent(htmlAttr);
            var source = htmlAttr.Source;

            var htmlContent = new HtmlContentIntermediateNode() { Source = source };
            htmlContent.Children.Add(new HtmlIntermediateToken(content, source));
            targetNode.Children.Add(htmlContent);
        }

        public override void ConvertToPlainElement(IntermediateNode parent, int index, UnresolvedElementIntermediateNode elementNode)
        {
            // Remove the wrapper and promote its children to the parent,
            // handling unresolved attributes and HtmlAttributeIntermediateNode appropriately.
            parent.Children.RemoveAt(index);

            var insertIndex = index;
            foreach (var child in elementNode.Children)
            {
                if (child is UnresolvedAttributeIntermediateNode unresolvedAttr)
                {
                    // Use the pre-lowered AsMarkupAttribute fallback.
                    if (unresolvedAttr.AsMarkupAttribute is MarkupElementIntermediateNode container)
                    {
                        foreach (var lowered in container.Children)
                        {
                            parent.Children.Insert(insertIndex++, lowered);
                        }
                    }
                    else if (unresolvedAttr.AsMarkupAttribute != null)
                    {
                        parent.Children.Insert(insertIndex++, unresolvedAttr.AsMarkupAttribute);
                    }
                    continue;
                }

                if (child is HtmlAttributeIntermediateNode htmlAttr)
                {
                    insertIndex = UnwrapHtmlAttribute(parent, insertIndex, htmlAttr);
                    continue;
                }

                parent.Children.Insert(insertIndex++, child);
            }

            MergeAdjacentHtmlContent(parent, index, insertIndex);
        }

        private static int UnwrapHtmlAttribute(IntermediateNode parent, int insertIndex, HtmlAttributeIntermediateNode htmlAttr)
        {
            var attrName = htmlAttr.AttributeName ?? string.Empty;
            var isDataDash = attrName.StartsWith("data-", StringComparison.OrdinalIgnoreCase);
            var hasDynamicChildren = false;
            foreach (var attrChild in htmlAttr.Children)
            {
                if (attrChild is CSharpExpressionAttributeValueIntermediateNode or
                    CSharpCodeAttributeValueIntermediateNode or
                    CSharpExpressionIntermediateNode)
                {
                    hasDynamicChildren = true;
                    break;
                }
            }

            if (isDataDash)
            {
                return UnwrapDataDashAttribute(parent, insertIndex, htmlAttr, hasDynamicChildren);
            }

            // Non-data-dash attributes: check if they should be flattened to text.
            // - Literal value (has HtmlAttributeValueIntermediateNode children) -> flatten
            // - Minimized (no children, no = in prefix) -> flatten
            // - Empty value with = (no children, = in prefix) -> keep as HtmlAttributeIntermediateNode
            // - Dynamic content -> keep as HtmlAttributeIntermediateNode
            var shouldFlatten = false;
            if (htmlAttr.Children.Count > 0)
            {
                // Check if all children are literal.
                shouldFlatten = true;
                foreach (var attrChild in htmlAttr.Children)
                {
                    if (attrChild is not HtmlAttributeValueIntermediateNode and
                        not HtmlContentIntermediateNode)
                    {
                        shouldFlatten = false;
                        break;
                    }
                }
            }
            else if (!(htmlAttr.Prefix ?? string.Empty).Contains('='))
            {
                // 0 children: minimized (no =) -> flatten; empty value (has =) -> keep.
                shouldFlatten = true;
            }

            if (shouldFlatten)
            {
                // Flatten to HtmlContent (merges with surrounding text).
                var attrContent = FlattenAttributeToHtml(htmlAttr);
                if (!string.IsNullOrEmpty(attrContent))
                {
                    var htmlContent = new HtmlContentIntermediateNode() { Source = htmlAttr.Source };
                    htmlContent.Children.Add(new HtmlIntermediateToken(attrContent, htmlAttr.Source));
                    parent.Children.Insert(insertIndex++, htmlContent);
                }
            }
            else
            {
                // Empty or dynamic: keep as HtmlAttributeIntermediateNode (BeginWriteAttribute pattern).
                parent.Children.Insert(insertIndex++, htmlAttr);
            }

            return insertIndex;
        }

        private static int UnwrapDataDashAttribute(IntermediateNode parent, int insertIndex, HtmlAttributeIntermediateNode htmlAttr, bool hasDynamicChildren)
        {
            if (!hasDynamicChildren)
            {
                // Data-dash with only literal content: flatten to HtmlContent.
                var attrContent = FlattenAttributeToHtml(htmlAttr);
                if (!string.IsNullOrEmpty(attrContent))
                {
                    var htmlContent = new HtmlContentIntermediateNode() { Source = htmlAttr.Source };
                    htmlContent.Children.Add(new HtmlIntermediateToken(attrContent, htmlAttr.Source));
                    parent.Children.Insert(insertIndex++, htmlContent);
                }

                return insertIndex;
            }

            // Data-dash with dynamic content: flatten prefix to HtmlContent,
            // then promote each child (unwrapping HtmlAttributeValue to HtmlContent).
            var prefix = htmlAttr.Prefix ?? string.Empty;
            if (prefix.Length > 0)
            {
                var prefixHtml = new HtmlContentIntermediateNode() { Source = htmlAttr.Source };
                prefixHtml.Children.Add(new HtmlIntermediateToken(prefix, htmlAttr.Source));
                parent.Children.Insert(insertIndex++, prefixHtml);
            }
            foreach (var attrChild in htmlAttr.Children)
            {
                if (attrChild is HtmlAttributeValueIntermediateNode attrValue)
                {
                    var (content, tokenSource) = CollectAttributeValueContent(attrValue);
                    if (content.Length > 0)
                    {
                        var hc = new HtmlContentIntermediateNode() { Source = tokenSource };
                        hc.Children.Add(new HtmlIntermediateToken(content, tokenSource));
                        parent.Children.Insert(insertIndex++, hc);
                    }
                }
                else if (attrChild is CSharpExpressionAttributeValueIntermediateNode exprAttrValue)
                {
                    // Unwrap: prefix as text, inner expression
                    if (!string.IsNullOrEmpty(exprAttrValue.Prefix))
                    {
                        var pHtml = new HtmlContentIntermediateNode();
                        pHtml.Children.Add(new HtmlIntermediateToken(exprAttrValue.Prefix, source: null));
                        parent.Children.Insert(insertIndex++, pHtml);
                    }
                    foreach (var innerChild in exprAttrValue.Children)
                    {
                        if (innerChild is CSharpIntermediateToken csharpToken)
                        {
                            var expr = new CSharpExpressionIntermediateNode() { Source = csharpToken.Source };
                            expr.Children.Add(csharpToken);
                            parent.Children.Insert(insertIndex++, expr);
                        }
                        else
                        {
                            parent.Children.Insert(insertIndex++, innerChild);
                        }
                    }
                }
                else
                {
                    parent.Children.Insert(insertIndex++, attrChild);
                }
            }
            var suffix = htmlAttr.Suffix ?? string.Empty;
            if (suffix.Length > 0)
            {
                var suffixHtml = new HtmlContentIntermediateNode();
                suffixHtml.Children.Add(new HtmlIntermediateToken(suffix, source: null));
                parent.Children.Insert(insertIndex++, suffixHtml);
            }

            return insertIndex;
        }

        private static void MergeAdjacentHtmlContent(IntermediateNode parent, int index, int insertIndex)
        {
            // After unwrapping, aggressively merge all adjacent HtmlContent nodes in the
            // affected range (and at boundaries with surrounding content).
            // Use unconditional merging since flattened attributes may have non-adjacent source spans.
            var mergeStart = Math.Max(0, index - 1);
            var mergeEnd = Math.Min(parent.Children.Count - 1, insertIndex);

            for (var i = mergeStart; i < mergeEnd; )
            {
                if (parent.Children[i] is HtmlContentIntermediateNode current &&
                    parent.Children[i + 1] is HtmlContentIntermediateNode next &&
                    CanMerge(current, next))
                {
                    // Merge next into current.
                    current.Children.AddRange(next.Children);
                    if (current.Source is SourceSpan currentSource && next.Source is SourceSpan nextSource)
                    {
                        current.Source = MergeSourceSpans(currentSource, nextSource);
                    }
                    else if (current.Source == null)
                    {
                        current.Source = next.Source;
                    }
                    parent.Children.RemoveAt(i + 1);
                    mergeEnd--;
                }
                else
                {
                    i++;
                }
            }
        }

        /// <summary>
        /// Flattens an HtmlAttributeIntermediateNode back to its original HTML text representation:
        /// prefix + child content + suffix.
        /// </summary>
        private static string FlattenAttributeToHtml(HtmlAttributeIntermediateNode htmlAttr)
        {
            using var _ = StringBuilderPool.GetPooledObject(out var sb);

            sb.Append(htmlAttr.Prefix ?? string.Empty);

            foreach (var child in htmlAttr.Children)
            {
                if (child is HtmlAttributeValueIntermediateNode attrValue)
                {
                    sb.Append(CollectAttributeValueContent(attrValue).Content);
                }
                else if (child is IntermediateToken directToken)
                {
                    sb.Append(directToken.Content);
                }
                else if (child is HtmlContentIntermediateNode htmlContent)
                {
                    foreach (var token in htmlContent.Children)
                    {
                        if (token is IntermediateToken intermediateToken)
                        {
                            sb.Append(intermediateToken.Content);
                        }
                    }
                }
                else if (child is CSharpExpressionAttributeValueIntermediateNode csharpAttrValue)
                {
                    sb.Append(csharpAttrValue.Prefix ?? string.Empty);
                    foreach (var exprChild in csharpAttrValue.Children)
                    {
                        if (exprChild is IntermediateToken exprToken)
                        {
                            sb.Append(exprToken.Content);
                        }
                        else if (exprChild is CSharpExpressionIntermediateNode innerExpr)
                        {
                            foreach (var innerToken in innerExpr.Children)
                            {
                                if (innerToken is IntermediateToken t)
                                {
                                    sb.Append(t.Content);
                                }
                            }
                        }
                    }
                }
            }

            sb.Append(htmlAttr.Suffix ?? string.Empty);

            return sb.ToString();
        }

        private static bool CanMerge(HtmlContentIntermediateNode a, HtmlContentIntermediateNode b)
        {
            if (a.Source == null || b.Source == null)
            {
                return true;
            }

            if (a.Source is SourceSpan aSource && b.Source is SourceSpan bSource)
            {
                return aSource.FilePath == bSource.FilePath &&
                       aSource.AbsoluteIndex + aSource.Length == bSource.AbsoluteIndex;
            }

            return false;
        }

        private static void LowerUnresolvedNonStringAttributeValues_Legacy(
            HtmlAttributeIntermediateNode htmlAttr,
            IntermediateNode target,
            SourceSpan? valueSourceSpan,
            RazorSourceDocument sourceDocument)
        {
            // Legacy non-string path: for non-string properties, the entire attribute
            // value is treated as C# code.

            // Case 1: Implicit/explicit expression (e.g. @int, @new string(...), @(@object)).
            if (htmlAttr.Children.Count >= 1 &&
                htmlAttr.Children[0] is UnresolvedExpressionAttributeValueIntermediateNode firstExpr &&
                string.IsNullOrEmpty(firstExpr.Prefix) &&
                firstExpr.ContainsExpression)
            {
                LowerImplicitExpressionAttribute_Legacy(htmlAttr, target, firstExpr, sourceDocument);
                return;
            }

            // Case 2: Code block as sole content (e.g. @{1 + 2}).
            if (htmlAttr.Children.Count == 1 &&
                htmlAttr.Children[0] is UnresolvedExpressionAttributeValueIntermediateNode soleCodeBlock &&
                !soleCodeBlock.ContainsExpression &&
                string.IsNullOrEmpty(soleCodeBlock.Prefix))
            {
                LowerCodeBlockAttribute_Legacy(soleCodeBlock, target);
                return;
            }

            // Case 3: Mixed content (literals + expressions).
            // For @@ escape cases, use children-based collection to avoid double-counting
            // the escaped @ that was already added as an unresolved literal.
            var hasEscapedAt = false;
            foreach (var child in htmlAttr.Children)
            {
                if (child is UnresolvedAttributeValueIntermediateNode { Children: [HtmlIntermediateToken { Content: "@" }] })
                {
                    hasEscapedAt = true;
                    break;
                }
            }

            if (!hasEscapedAt && valueSourceSpan is { Length: > 0 } vss)
            {
                LowerMixedContentFromSource_Legacy(target, vss, sourceDocument);
            }
            else
            {
                LowerMixedContentFromChildren_Legacy(htmlAttr, target, sourceDocument);
            }
        }

        /// <summary>
        /// Handles implicit (<c>@expr</c>) and explicit (<c>@(expr)</c>) C# expressions in legacy attributes.
        /// For explicit expressions, splits into three tokens: <c>(</c>, content, <c>)</c>.
        /// For implicit expressions, produces a single <see cref="CSharpIntermediateToken"/>.
        /// Extracts content from the source document, skipping the <c>@</c> transition character.
        /// </summary>
        private static void LowerImplicitExpressionAttribute_Legacy(
            HtmlAttributeIntermediateNode htmlAttr,
            IntermediateNode target,
            UnresolvedExpressionAttributeValueIntermediateNode firstExpr,
            RazorSourceDocument sourceDocument)
        {
            var expr = new CSharpExpressionIntermediateNode();

            // Extract expression content from source document, skipping the @ transition.
            // This preserves explicit expression parens: @(@object) -> (@object), @(1+2) -> (1+2).
            // For implicit expressions: @int -> int.
            if (firstExpr.Source is { } exprSource && exprSource.Length > 1)
            {
                // Skip the @ transition character.
                var contentStart = exprSource.AbsoluteIndex + 1;
                var contentLength = exprSource.Length - 1;

                // Also include any following literal children's content.
                for (var i = 1; i < htmlAttr.Children.Count; i++)
                {
                    if (htmlAttr.Children[i] is UnresolvedAttributeValueIntermediateNode lit && lit.Source is { } litSrc)
                    {
                        var litEnd = litSrc.AbsoluteIndex + litSrc.Length;
                        contentLength = litEnd - contentStart;
                    }
                }

                var text = sourceDocument.Text.ToString(
                    new Microsoft.CodeAnalysis.Text.TextSpan(contentStart, contentLength));

                // For explicit expressions @(...), split into 3 tokens: (, content, )
                if (text.Length >= 2 && text[0] == '(' && text[text.Length - 1] == ')')
                {
                    EmitParenthesizedExpressionTokens(expr, contentStart, contentLength, sourceDocument);

                    var openLoc = sourceDocument.Text.Lines.GetLinePosition(contentStart);
                    var closeLoc = sourceDocument.Text.Lines.GetLinePosition(contentStart + contentLength - 1);
                    expr.Source = new SourceSpan(exprSource.FilePath, contentStart, openLoc.Line, openLoc.Character, contentLength, 0, closeLoc.Character + 1);
                }
                else
                {
                    // Implicit expression: single token.
                    var contentLocation = sourceDocument.Text.Lines.GetLinePosition(contentStart);
                    var contentSpan = new SourceSpan(
                        exprSource.FilePath,
                        contentStart,
                        contentLocation.Line,
                        contentLocation.Character,
                        contentLength,
                        0,
                        contentLocation.Character + contentLength);
                    expr.Children.Add(new CSharpIntermediateToken(text, contentSpan));
                    expr.Source = contentSpan;
                }
            }
            else
            {
                // Fallback: collect from children.
                using var _sb = StringBuilderPool.GetPooledObject(out var sb);
                SourceSpan? firstSpan = null;
                SourceSpan? lastSpan = null;
                foreach (var child in htmlAttr.Children)
                {
                    if (child is UnresolvedExpressionAttributeValueIntermediateNode unresolvedExpr
                        && !string.IsNullOrEmpty(unresolvedExpr.Prefix))
                    {
                        sb.Append(unresolvedExpr.Prefix);
                    }
                    else if (child is UnresolvedAttributeValueIntermediateNode unresolvedLiteral
                        && !string.IsNullOrEmpty(unresolvedLiteral.Prefix))
                    {
                        sb.Append(unresolvedLiteral.Prefix);
                    }

                    CollectAllTokenContent(child, sb, ref firstSpan, ref lastSpan);
                }

                var mergedContent = sb.ToString();
                var tokenSpan = firstSpan is { } f && lastSpan is { } l
                    ? MergeSourceSpans(f, l)
                    : firstSpan;
                expr.Children.Add(new CSharpIntermediateToken(mergedContent, tokenSpan));
                expr.Source = tokenSpan;
            }

            target.Children.Add(expr);
        }

        /// <summary>
        /// Handles code block attribute values (<c>@{ ... }</c>). Preserves internal CSharpCode
        /// structure without wrapping in <see cref="CSharpExpressionIntermediateNode"/>, matching
        /// legacy pipeline behavior where code blocks are distinct from expressions.
        /// </summary>
        private static void LowerCodeBlockAttribute_Legacy(
            UnresolvedExpressionAttributeValueIntermediateNode soleCodeBlock,
            IntermediateNode target)
        {
            target.Children.AddRange(soleCodeBlock.Children);
        }

        /// <summary>
        /// Handles mixed literal+expression content by extracting the full text from the source document.
        /// Produces a single <see cref="CSharpIntermediateToken"/> with the raw source text.
        /// Only used when a source span is available with non-zero length.
        /// </summary>
        private static void LowerMixedContentFromSource_Legacy(
            IntermediateNode target,
            SourceSpan vss,
            RazorSourceDocument sourceDocument)
        {
            var text = sourceDocument.Text.ToString(new Microsoft.CodeAnalysis.Text.TextSpan(vss.AbsoluteIndex, vss.Length));
            target.Children.Add(new CSharpIntermediateToken(text, vss));
        }

        /// <summary>
        /// Fallback for mixed content when source span is unavailable. Walks children producing:
        /// flat <see cref="CSharpIntermediateToken"/>s for literals, <see cref="CSharpExpressionIntermediateNode"/>
        /// wrappers for expressions. Handles <c>@@</c> escape by detecting literal <c>@</c> tokens
        /// after ephemeral markers.
        /// </summary>
        private static void LowerMixedContentFromChildren_Legacy(
            HtmlAttributeIntermediateNode htmlAttr,
            IntermediateNode target,
            RazorSourceDocument sourceDocument)
        {
            // @@ escape case: the @@ literal becomes a flat @ token, and expression children
            // become CSharpExpression with source-extracted content.
            for (var i = 0; i < htmlAttr.Children.Count; i++)
            {
                var child = htmlAttr.Children[i];
                if (child is UnresolvedAttributeValueIntermediateNode unresolvedLiteral)
                {
                    // Literal children (including the @ from @@): produce flat CSharp token.
                    foreach (var valueChild in unresolvedLiteral.Children)
                    {
                        if (valueChild is HtmlIntermediateToken htmlToken)
                        {
                            target.Children.Add(ToCSharpToken(htmlToken));
                        }
                    }

                    // Add empty token after @@ literal. The @@ escape produces a single
                    // @ content token, and requires a trailing empty token to represent
                    // the ephemeral second @ that was consumed by the escape.
                    if (unresolvedLiteral.Children is [HtmlIntermediateToken { Content: "@" }])
                    {
                        // Find the next child's position for the empty token span.
                        SourceSpan? emptySpan = null;
                        if (i + 1 < htmlAttr.Children.Count && htmlAttr.Children[i + 1].Source is { } nextSrc)
                        {
                            var loc = sourceDocument.Text.Lines.GetLinePosition(nextSrc.AbsoluteIndex);
                            emptySpan = new SourceSpan(nextSrc.FilePath, nextSrc.AbsoluteIndex, loc.Line, loc.Character, 0, 0, loc.Character);
                        }

                        target.Children.Add(CreateEmptyCSharpToken(emptySpan));
                    }
                }
                else if (child is UnresolvedExpressionAttributeValueIntermediateNode unresolvedExpr)
                {
                    if (unresolvedExpr.ContainsExpression && unresolvedExpr.Source is { Length: > 1 } exprSrc)
                    {
                        // Expression after @@ escape: emit CSharpExpression(@, (, content, ))
                        EmitEscapedAtCSharpExpression(target, exprSrc, sourceDocument);
                    }
                    else
                    {
                        // Code block or no source: flatten to tokens.
                        target.Children.AddRange(unresolvedExpr.Children);
                    }
                }
            }
        }

        /// <summary>
        /// Converts unresolved values to <see cref="HtmlContentIntermediateNode"/> tokens matching the
        /// legacy string attribute pipeline. Groups adjacent literals into pending parts, flushing as
        /// <see cref="HtmlContentIntermediateNode"/>. Merges prefix into first token content and extends
        /// source span backward via <see cref="ExtendSpanBackward"/>. Expressions become
        /// <see cref="CSharpExpressionIntermediateNode"/> nodes between literal groups.
        /// </summary>
        private static void LowerUnresolvedStringAttributeValues_Legacy(
            HtmlAttributeIntermediateNode htmlAttr,
            IntermediateNode target,
            RazorSourceDocument sourceDocument)
        {
            // Legacy path: preserve individual literal tokens (including prefixes/spaces) and wrap expressions
            // in CSharpExpression. Adjacent literals are batched into single HtmlContent nodes.
            using var pendingLiteralParts = new PooledArrayBuilder<(string text, SourceSpan? source)>();
            SourceSpan? pendingFirstSpan = null;
            SourceSpan? pendingLastSpan = null;

            foreach (var child in htmlAttr.Children)
            {
                if (child is UnresolvedAttributeValueIntermediateNode unresolvedLiteral)
                {
                    var prefix = unresolvedLiteral.Prefix;
                    var mergedPrefixWithFirst = false;

                    foreach (var valueChild in unresolvedLiteral.Children)
                    {
                        if (valueChild is HtmlIntermediateToken htmlToken)
                        {
                            if (!mergedPrefixWithFirst && !string.IsNullOrEmpty(prefix))
                            {
                                // Merge prefix into the first token's content and extend span backward.
                                var mergedContent = prefix + htmlToken.Content;
                                var mergedSource = ExtendSpanBackward(htmlToken.Source, prefix.Length);

                                pendingLiteralParts.Add((mergedContent, mergedSource));
                                if (mergedSource is { } ms)
                                {
                                    pendingFirstSpan ??= ms;
                                    pendingLastSpan = ms;
                                }

                                mergedPrefixWithFirst = true;
                            }
                            else
                            {
                                pendingLiteralParts.Add((htmlToken.Content, htmlToken.Source));
                                if (htmlToken.Source is { } s)
                                {
                                    pendingFirstSpan ??= s;
                                    pendingLastSpan = s;
                                }
                            }
                        }
                    }

                    // If prefix wasn't merged (no children), add it standalone.
                    if (!mergedPrefixWithFirst && !string.IsNullOrEmpty(prefix))
                    {
                        pendingLiteralParts.Add((prefix, null));
                    }
                }
                else
                {
                    // Include the expression's prefix (e.g. space before @expr) in pending literals.
                    if (child is UnresolvedExpressionAttributeValueIntermediateNode unresolvedExpr2
                        && !string.IsNullOrEmpty(unresolvedExpr2.Prefix))
                    {
                        pendingLiteralParts.Add((unresolvedExpr2.Prefix, null));
                    }

                    // Flush pending literals as HtmlContent with individual tokens.
                    FlushPendingLiterals(target, ref pendingLiteralParts.AsRef(), ref pendingFirstSpan, ref pendingLastSpan);

                    if (child is UnresolvedExpressionAttributeValueIntermediateNode unresolvedExpr)
                    {
                        if (unresolvedExpr.ContainsExpression)
                        {
                            // Implicit/explicit expression: wrap in CSharpExpression.
                            var expr = new CSharpExpressionIntermediateNode();
                            FlattenToDirectCSharpTokens(unresolvedExpr, expr);
                            expr.Source = expr.Children.Count > 0 ? expr.Children[0].Source : unresolvedExpr.Source;
                            target.Children.Add(expr);
                        }
                        else
                        {
                            // Code block (@if{}, @{...}): preserve internal structure
                            // (CSharpCode + HtmlContent interleaving).
                            target.Children.AddRange(unresolvedExpr.Children);
                        }
                    }
                    else
                    {
                        target.Children.Add(child);
                    }
                }
            }

            FlushPendingLiterals(target, ref pendingLiteralParts.AsRef(), ref pendingFirstSpan, ref pendingLastSpan);
        }

        /// <summary>
        /// Flushes accumulated pending literal parts as a single <see cref="HtmlContentIntermediateNode"/>
        /// with individual tokens, then clears the pending state.
        /// </summary>
        private static void FlushPendingLiterals(
            IntermediateNode target,
            ref PooledArrayBuilder<(string text, SourceSpan? source)> pendingParts,
            ref SourceSpan? pendingFirstSpan,
            ref SourceSpan? pendingLastSpan)
        {
            if (pendingParts.Count == 0)
            {
                return;
            }

            var htmlContent = new HtmlContentIntermediateNode() { Source = pendingFirstSpan };
            foreach (var (text, tokenSource) in pendingParts)
            {
                htmlContent.Children.Add(new HtmlIntermediateToken(text, tokenSource));
            }

            if (pendingFirstSpan is { } f && pendingLastSpan is { } l)
            {
                htmlContent.Source = MergeSourceSpans(f, l);
            }

            target.Children.Add(htmlContent);
            pendingParts.Clear();
            pendingFirstSpan = null;
            pendingLastSpan = null;
        }

        private static void TryAddCSharpInDeclarationDiagnostic(
            TagHelperIntermediateNode tagHelperNode,
            UnresolvedElementIntermediateNode elementNode,
            int attrEnd)
        {
            for (var i = 0; i < attrEnd; i++)
            {
                if (elementNode.Children[i] is CSharpCodeIntermediateNode codeChild)
                {
                    tagHelperNode.AddDiagnostic(
                        RazorDiagnosticFactory.CreateParsing_TagHelpersCannotHaveCSharpInTagDeclaration(
                            codeChild.Source ?? elementNode.Source ?? SourceSpan.Undefined, elementNode.TagName));
                    return;
                }

                if (elementNode.Children[i] is CSharpExpressionIntermediateNode exprChild)
                {
                    // The CSharpExpressionIntermediateNode source doesn't include the @ transition.
                    // Adjust the span back by 1 to include it so the diagnostic points at the full @expression.
                    var diagSource = exprChild.Source ?? elementNode.Source ?? SourceSpan.Undefined;
                    if (diagSource.AbsoluteIndex > 0)
                    {
                        diagSource = new SourceSpan(
                            diagSource.FilePath,
                            diagSource.AbsoluteIndex - 1,
                            diagSource.LineIndex,
                            diagSource.CharacterIndex - 1,
                            diagSource.Length + 1,
                            diagSource.LineCount,
                            diagSource.EndCharacterIndex);
                    }

                    tagHelperNode.AddDiagnostic(
                        RazorDiagnosticFactory.CreateParsing_TagHelpersCannotHaveCSharpInTagDeclaration(
                            diagSource, elementNode.TagName));
                    return;
                }
            }
        }

        /// <summary>
        /// Recursively collects all token content (string text) from a node and its descendants,
        /// tracking the first and last source spans encountered.
        /// </summary>
        private static void CollectAllTokenContent(
            IntermediateNode node,
            StringBuilder sb,
            ref SourceSpan? firstSpan,
            ref SourceSpan? lastSpan)
        {
            if (node is IntermediateToken token)
            {
                sb.Append(token.Content);
                if (token.Source is { } s)
                {
                    firstSpan ??= s;
                    lastSpan = s;
                }
            }
            else
            {
                foreach (var child in node.Children)
                {
                    CollectAllTokenContent(child, sb, ref firstSpan, ref lastSpan);
                }
            }
        }
    }
}

