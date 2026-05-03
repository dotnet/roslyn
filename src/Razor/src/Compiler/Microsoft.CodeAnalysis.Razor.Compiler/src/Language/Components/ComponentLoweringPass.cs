// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentLoweringPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    // This pass runs earlier than our other passes that 'lower' specific kinds of attributes.
    public override int Order => 0;

    protected override void ExecuteCore(
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        CancellationToken cancellationToken)
    {
        if (!IsComponentDocument(documentNode))
        {
            return;
        }

        var @namespace = documentNode.FindPrimaryNamespace();
        var @class = documentNode.FindPrimaryClass();
        if (@namespace == null || @class == null)
        {
            // Nothing to do, bail. We can't function without the standard structure.
            return;
        }

        // For each component *usage* we need to rewrite the tag helper node to map to the relevant component
        // APIs.
        var usings = documentNode.FindDescendantNodes<UsingDirectiveIntermediateNode>();
        var references = documentNode.FindDescendantReferences<TagHelperIntermediateNode>();

        foreach (var reference in references)
        {
            var node = reference.Node;
            if (node.TagHelpers.Any(t => t.Kind == TagHelperKind.ChildContent))
            {
                // This is a child content tag helper. This will be rewritten when we visit its parent.
                continue;
            }

            // The element didn't match any child content descriptors. Look for any matching component descriptors.
            var count = 0;
            foreach (var tagHelper in node.TagHelpers)
            {
                if (tagHelper.Kind == TagHelperKind.Component)
                {
                    // Count component tag helpers
                    count++;
                }
            }

            if (count == 1)
            {
                reference.Replace(RewriteAsComponent(node, node.TagHelpers.Single(n => n.Kind == TagHelperKind.Component)));
            }
            else if (count > 1)
            {
                var tagHelper = GetTagHelperOrAddDiagnostic(node, usings);
                if (tagHelper == null)
                {
                    break;
                }

                reference.Replace(RewriteAsComponent(node, tagHelper));
            }
            else
            {
                reference.Replace(RewriteAsElement(node));
            }
        }

        // We have several tag helpers that are candidates for the same tag node. We need to disambiguate which tag helper applies to this tag
        // since we now fully qualify the tag helper when we emit the code, and we can't rely on the compiler to error out when there are ambiguous
        // tag helpers.
        // We look based on the set of usings if there is only a single component for that can be applied to that tag or if we detect more than one
        // we add a diagnostic and return null
        static TagHelperDescriptor GetTagHelperOrAddDiagnostic(TagHelperIntermediateNode node, IReadOnlyList<UsingDirectiveIntermediateNode> usings)
        {
            TagHelperDescriptor candidate = null;
            List<TagHelperDescriptor> matched = null;
            foreach (var tagHelper in node.TagHelpers)
            {
                if (tagHelper.Kind != TagHelperKind.Component)
                {
                    continue;
                }

                for (var j = 0; j < usings.Count; j++)
                {
                    var usingNamespace = usings[j].Content;
                    if (string.Equals(tagHelper.TypeNamespace, usingNamespace, StringComparison.Ordinal))
                    {
                        if (candidate == null)
                        {
                            candidate = tagHelper;
                        }
                        else
                        {
                            matched ??= new();
                            matched.Add(tagHelper);
                        }
                    }
                }
            }

            if (matched != null)
            {
                // Insert candidate at the beginning to maintain the original order
                matched.Insert(0, candidate);

                // Before reporting an ambiguity error, try to disambiguate based on whether
                // type parameters are provided. This handles the case where both a generic
                // and non-generic version of a component exist with the same name.
                var resolvedCandidate = TryDisambiguateByTypeParameters(node, matched);
                if (resolvedCandidate != null)
                {
                    return resolvedCandidate;
                }

                // Iterate over existing diagnostics to avoid adding multiple diagnostics when we find an ambiguous tag.
                foreach (var diagnostic in node.Diagnostics)
                {
                    if (diagnostic.Id == ComponentDiagnosticFactory.MultipleComponents.Id ||
                        diagnostic.Id == ComponentDiagnosticFactory.AmbiguousComponentSelection.Id)
                    {
                        return null;
                    }
                }

                node.AddDiagnostic(ComponentDiagnosticFactory.Create_MultipleComponents(node.Source, node.TagName, matched));

                return null;
            }

            return candidate;
        }

        // Try to disambiguate between multiple components with the same name by checking if
        // type parameters are provided. The logic handles:
        // 1. One generic and one non-generic component
        // 2. Multiple generic components with different type parameter counts
        //
        // Disambiguation is based on matching the provided type parameters in the markup
        // with the type parameters defined by each candidate component.
        static TagHelperDescriptor TryDisambiguateByTypeParameters(TagHelperIntermediateNode node, List<TagHelperDescriptor> candidates)
        {
            // Separate candidates into generic and non-generic
            var genericCandidates = candidates.Where(c => c.IsGenericTypedComponent()).ToList();
            var nonGenericCandidates = candidates.Where(c => !c.IsGenericTypedComponent()).ToList();

            // Get all type parameter attributes provided in the markup
            using var providedTypeParameters = GetProvidedTypeParameters(node);

            // If no type parameters are provided
            if (providedTypeParameters.Count == 0)
            {
                // If there's exactly one non-generic component, prefer it
                if (nonGenericCandidates.Count == 1 && genericCandidates.Count >= 1)
                {
                    var nonGenericComponent = nonGenericCandidates[0];
                    
                    // Check for ambiguity with any generic component
                    foreach (var genericComponent in genericCandidates)
                    {
                        if (HasAmbiguousParameters(node, genericComponent, nonGenericComponent))
                        {
                            // Report an ambiguity error and return null
                            node.AddDiagnostic(ComponentDiagnosticFactory.Create_AmbiguousComponentSelection(
                                node.Source, 
                                node.TagName, 
                                genericComponent, 
                                nonGenericComponent));
                            return null;
                        }
                    }
                    
                    // No ambiguity with any generic variant, use the non-generic component
                    return nonGenericComponent;
                }
                
                // Can't disambiguate - either no non-generic or multiple non-generic components
                return null;
            }

            // Type parameters are provided - find the generic component that matches
            TagHelperDescriptor bestMatch = null;
            var providedTypeParametersArray = providedTypeParameters.ToArray();
            
            foreach (var candidate in genericCandidates)
            {
                using var candidateTypeParams = GetTypeParameterNames(candidate);
                
                // Check if all provided type parameters exist in this candidate's type parameters
                var allProvidedMatch = true;
                foreach (var provided in providedTypeParametersArray)
                {
                    if (!candidateTypeParams.Contains(provided))
                    {
                        allProvidedMatch = false;
                        break;
                    }
                }
                
                if (!allProvidedMatch)
                {
                    continue;
                }
                
                // All provided type parameters match this candidate
                // Check if this is a complete match (all type parameters provided)
                if (providedTypeParameters.Count == candidateTypeParams.Count)
                {
                    // Exact match - this is the component to use
                    return candidate;
                }
                
                // Partial match - could be this component (type inference will handle the rest)
                // Keep track of it as a potential match
                if (bestMatch == null)
                {
                    bestMatch = candidate;
                }
                else
                {
                    // Multiple components match the provided type parameters - ambiguous
                    return null;
                }
            }

            // Return the best match if we found one, otherwise null (ambiguous or no match)
            return bestMatch;
        }

        // Get all type parameter names provided in the markup
        static PooledHashSet<string> GetProvidedTypeParameters(TagHelperIntermediateNode node)
        {
            var result = new PooledHashSet<string>(StringComparer.Ordinal);
            
            foreach (var child in node.Children)
            {
                if (child is TagHelperPropertyIntermediateNode property)
                {
                    // Check if this property matches any type parameter from any candidate
                    // We'll check this by seeing if it's a type parameter for any of the tag helpers
                    foreach (var tagHelper in node.TagHelpers)
                    {
                        if (tagHelper.IsGenericTypedComponent())
                        {
                            foreach (var typeParam in tagHelper.GetTypeParameters())
                            {
                                if (string.Equals(property.AttributeName, typeParam.Name, StringComparison.Ordinal))
                                {
                                    result.Add(property.AttributeName);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            
            return result;
        }

        // Get the set of type parameter names for a component
        static PooledHashSet<string> GetTypeParameterNames(TagHelperDescriptor component)
        {
            var result = new PooledHashSet<string>(StringComparer.Ordinal);
            foreach (var typeParam in component.GetTypeParameters())
            {
                result.Add(typeParam.Name);
            }
            return result;
        }

        // Check if both components have parameters with the same names as those used in the markup,
        // which would make selection ambiguous when no type parameters are provided
        static bool HasAmbiguousParameters(TagHelperIntermediateNode node, TagHelperDescriptor genericComponent, TagHelperDescriptor nonGenericComponent)
        {
            // Get all the attribute names used in the markup (excluding type parameters)
            using var typeParameterNames = GetTypeParameterNames(genericComponent);

            using var markupAttributeNames = new PooledHashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var child in node.Children)
            {
                if (child is TagHelperPropertyIntermediateNode property)
                {
                    // Skip type parameters
                    if (!typeParameterNames.Contains(property.AttributeName))
                    {
                        markupAttributeNames.Add(property.AttributeName);
                    }
                }
            }

            // If no non-type-parameter attributes are used, there's no ambiguity
            if (markupAttributeNames.Count == 0)
            {
                return false;
            }

            // Check if both components have bound attributes with the same names
            using var genericParamNames = new PooledHashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var attr in genericComponent.BoundAttributes)
            {
                if (!attr.IsTypeParameterProperty())
                {
                    genericParamNames.Add(attr.Name);
                }
            }

            using var nonGenericParamNames = new PooledHashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var attr in nonGenericComponent.BoundAttributes)
            {
                nonGenericParamNames.Add(attr.Name);
            }

            // Check if any of the used attributes exist in both components
            var markupAttributesArray = markupAttributeNames.ToArray();
            foreach (var attrName in markupAttributesArray)
            {
                if (genericParamNames.Contains(attrName) && nonGenericParamNames.Contains(attrName))
                {
                    // Found a parameter that exists in both components - this is ambiguous
                    return true;
                }
            }

            return false;
        }
    }

    private static ComponentIntermediateNode RewriteAsComponent(TagHelperIntermediateNode node, TagHelperDescriptor tagHelper)
    {
        Debug.Assert(node.StartTagSpan.HasValue, "Component tags should always have a start tag span.");
        var component = new ComponentIntermediateNode()
        {
            Component = tagHelper,
            Source = node.Source,
            TagName = node.TagName,
            TypeName = tagHelper.TypeName,
            StartTagSpan = node.StartTagSpan.AssumeNotNull(),
        };

        component.AddDiagnosticsFromNode(node);

        var visitor = new ComponentRewriteVisitor(component);
        visitor.Visit(node);

        // Fixup the parameter names of child content elements. We can't do this during the rewrite
        // because we see the nodes in the wrong order.
        foreach (var childContent in component.ChildContents)
        {
            childContent.ParameterName ??= component.ChildContentParameterName ?? ComponentHelpers.ChildContent.DefaultParameterName;
        }

        ValidateRequiredAttributes(node, tagHelper, component);

        return component;
    }

    private static void ValidateRequiredAttributes(TagHelperIntermediateNode node, TagHelperDescriptor tagHelper, ComponentIntermediateNode intermediateNode)
    {
        if (intermediateNode.Children.Any(static c => c is TagHelperDirectiveAttributeIntermediateNode node && (node.TagHelper?.Kind == TagHelperKind.Splat)))
        {
            // If there are any splat attributes, assume the user may have provided all values.
            // This pass runs earlier than ComponentSplatLoweringPass, so we cannot rely on the presence of SplatIntermediateNode to make this check.
            return;
        }

        foreach (var requiredAttribute in tagHelper.EditorRequiredAttributes)
        {
            if (!IsPresentAsAttribute(requiredAttribute.Name, intermediateNode))
            {
                intermediateNode.AddDiagnostic(
                  RazorDiagnosticFactory.CreateComponent_EditorRequiredParameterNotSpecified(
                      node.Source,
                      intermediateNode.TagName,
                      requiredAttribute.Name));
            }
        }

        static bool IsPresentAsAttribute(string attributeName, ComponentIntermediateNode intermediateNode)
        {
            foreach (var child in intermediateNode.Children)
            {
                if (child is ComponentAttributeIntermediateNode attributeNode && attributeName == attributeNode.AttributeName)
                {
                    return true;
                }
                if (child is ComponentChildContentIntermediateNode childContent && attributeName == childContent.AttributeName)
                {
                    return true;
                }
                const string bindPrefix = "@bind-";
                if (child is TagHelperDirectiveAttributeIntermediateNode { OriginalAttributeName: { } originalAttributeName } &&
                    originalAttributeName.StartsWith(bindPrefix, StringComparison.Ordinal) &&
                    EqualsWithOptionalChangedOrExpressionSuffix(originalAttributeName.AsSpan(start: bindPrefix.Length), attributeName))
                {
                    return true;
                }
                if (child is TagHelperDirectiveAttributeParameterIntermediateNode { OriginalAttributeName: { } originalName, AttributeNameWithoutParameter: { } nameWithoutParameter } &&
                    originalName.StartsWith(bindPrefix, StringComparison.Ordinal) &&
                    EqualsWithOptionalChangedOrExpressionSuffix(nameWithoutParameter.AsSpan(start: bindPrefix.Length - 1), attributeName))
                {
                    // `@bind-Value:get` or `@bind-Value:set` is specified.
                    return true;
                }
            }

            return false;
        }

        // True if `requiredName` is equal to `specifiedName` or to `specifiedName + "Changed"` or to `specifiedName + "Expression"`.
        static bool EqualsWithOptionalChangedOrExpressionSuffix(ReadOnlySpan<char> specifiedName, string requiredName)
        {
            var requiredNameSpan = requiredName.AsSpan();
            return EqualsWithSuffix(specifiedName, requiredNameSpan, "Changed") ||
                EqualsWithSuffix(specifiedName, requiredNameSpan, "Expression") ||
                specifiedName.Equals(requiredNameSpan, StringComparison.Ordinal);
        }

        // True if `requiredName` is equal to `specifiedName + suffix`.
        static bool EqualsWithSuffix(ReadOnlySpan<char> specifiedName, ReadOnlySpan<char> requiredName, string suffix)
        {
            return requiredName.EndsWith(suffix.AsSpan(), StringComparison.Ordinal) &&
                specifiedName.Equals(requiredName[..^suffix.Length], StringComparison.Ordinal);
        }
    }

    private static MarkupElementIntermediateNode RewriteAsElement(TagHelperIntermediateNode node)
    {
        var result = new MarkupElementIntermediateNode()
        {
            Source = node.Source,
            TagName = node.TagName,
        };

        result.AddDiagnosticsFromNode(node);

        var visitor = new ElementRewriteVisitor(result.Children);
        visitor.Visit(node);

        return result;
    }

    private class ComponentRewriteVisitor : IntermediateNodeWalker
    {
        private readonly ComponentIntermediateNode _component;
        private readonly IntermediateNodeCollection _children;

        public ComponentRewriteVisitor(ComponentIntermediateNode component)
        {
            _component = component;
            _children = component.Children;
        }

        public override void VisitTagHelper(TagHelperIntermediateNode node)
        {
            // Visit children, we're replacing this node.
            base.VisitDefault(node);
        }

        public override void VisitTagHelperBody(TagHelperBodyIntermediateNode node)
        {
            // Wrap the component's children in a ChildContent node if we have some significant
            // content.
            if (node.Children.Count == 0)
            {
                return;
            }

            // If we get a single HTML content node containing only whitespace,
            // then this is probably a tag that looks like '<MyComponent>  </MyComponent>
            //
            // We don't want to create a child content for this case, because it can conflict
            // with a child content that's set via an attribute. We don't want the formatting
            // of insignificant whitespace to be annoying when setting attributes directly.
            if (node.Children.Count == 1 && IsIgnorableWhitespace(node.Children[0]))
            {
                return;
            }

            // From here we fork and behave differently based on whether the component's child content is
            // implicit or explicit.
            //
            // Explicit child content will look like: <MyComponent><ChildContent><div>...</div></ChildContent></MyComponent>
            // compared with implicit: <MyComponent><div></div></MyComponent>
            //
            // Using implicit child content:
            // 1. All content is grouped into a single child content lambda, and assigned to the property 'ChildContent'
            //
            // Using explicit child content:
            // 1. All content must be contained within 'child content' elements that are direct children
            // 2. Whitespace outside of 'child content' elements will be ignored (not an error)
            // 3. Non-whitespace outside of 'child content' elements will cause an error
            // 4. All 'child content' elements must match parameters on the component (exception for ChildContent,
            //    which is always allowed.
            // 5. Each 'child content' element will generate its own lambda, and be assigned to the property
            //    that matches the element name.
            if (!node.Children.OfType<TagHelperIntermediateNode>().Any(t => t.TagHelpers.Any(th => th.Kind == TagHelperKind.ChildContent)))
            {
                // This node has implicit child content. It may or may not have an attribute that matches.
                var attribute = _component.Component.BoundAttributes
                    .Where(a => string.Equals(a.Name, ComponentsApi.RenderTreeBuilder.ChildContent, StringComparison.Ordinal))
                    .FirstOrDefault();
                _children.Add(RewriteChildContent(attribute, node.Source, node.Children));
                return;
            }

            // OK this node has explicit child content, we can rewrite it by visiting each node
            // in sequence, since we:
            // a) need to rewrite each child content element
            // b) any significant content outside of a child content is an error
            for (var i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i];
                if (IsIgnorableWhitespace(child))
                {
                    continue;
                }

                if (child is TagHelperIntermediateNode tagHelperNode &&
                    tagHelperNode.TagHelpers.Any(th => th.Kind == TagHelperKind.ChildContent))
                {
                    // This is a child content element
                    var attribute = _component.Component.BoundAttributes
                        .Where(a => string.Equals(a.Name, tagHelperNode.TagName, StringComparison.Ordinal))
                        .FirstOrDefault();
                    var rewrittenChildContent = RewriteChildContent(attribute, child.Source, child.Children);
                    // Transfer diagnostics from the TagHelperIntermediateNode to the rewritten child content.
                    // The resolution phase may have placed diagnostics on the tag helper node that need
                    // to survive rewriting into a ComponentChildContentIntermediateNode.
                    rewrittenChildContent.AddDiagnosticsFromNode(tagHelperNode);
                    _children.Add(rewrittenChildContent);
                    continue;
                }

                // If we get here then this is significant content inside a component with explicit child content.
                child.AddDiagnostic(ComponentDiagnosticFactory.Create_ChildContentMixedWithExplicitChildContent(child.Source, _component));
                _children.Add(child);
            }

            bool IsIgnorableWhitespace(IntermediateNode n)
            {
                if (n is HtmlContentIntermediateNode html &&
                    html.Children.Count == 1 &&
                    html.Children[0] is IntermediateToken token &&
                    string.IsNullOrWhiteSpace(token.Content))
                {
                    return true;
                }

                return false;
            }
        }

        private ComponentChildContentIntermediateNode RewriteChildContent(BoundAttributeDescriptor attribute, SourceSpan? source, IntermediateNodeCollection children)
        {
            var childContent = new ComponentChildContentIntermediateNode()
            {
                BoundAttribute = attribute,
                Source = source,
                TypeName = attribute?.TypeName ?? ComponentsApi.RenderFragment.FullTypeName,
            };

            // There are two cases here:
            // 1. Implicit child content - the children will be non-taghelper nodes, just accept them
            // 2. Explicit child content - the children will be various tag helper nodes, that need special processing.
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child is TagHelperBodyIntermediateNode body)
                {
                    // The body is all of the content we want to render, the rest of the children will
                    // be the attributes.
                    for (var j = 0; j < body.Children.Count; j++)
                    {
                        childContent.Children.Add(body.Children[j]);
                    }
                }
                else if (child is TagHelperPropertyIntermediateNode property)
                {
                    if (property.BoundAttribute.IsChildContentParameterNameProperty())
                    {
                        // Check for each child content with a parameter name, that the parameter name is specified
                        // with literal text. For instance, the following is not allowed and should generate a diagnostic.
                        //
                        // <MyComponent><ChildContent Context="@Foo()">...</ChildContent></MyComponent>
                        if (TryGetAttributeStringContent(property, out var parameterName))
                        {
                            childContent.ParameterName = parameterName;
                            continue;
                        }

                        // The parameter name is invalid.
                        childContent.AddDiagnostic(ComponentDiagnosticFactory.Create_ChildContentHasInvalidParameter(property.Source, property.AttributeName, attribute.Name));
                        continue;
                    }

                    // This is an unrecognized tag helper bound attribute. This will practically never happen unless the child content descriptor was misconfigured.
                    childContent.AddDiagnostic(ComponentDiagnosticFactory.Create_ChildContentHasInvalidAttribute(property.Source, property.AttributeName, attribute.Name));
                }
                else if (child is TagHelperHtmlAttributeIntermediateNode a)
                {
                    // This is an HTML attribute on a child content.
                    childContent.AddDiagnostic(ComponentDiagnosticFactory.Create_ChildContentHasInvalidAttribute(a.Source, a.AttributeName, attribute.Name));
                }
                else if (child is TagHelperDirectiveAttributeIntermediateNode directiveAttribute)
                {
                    // We don't support directive attributes inside child content, this is possible if you try to do something like put '@ref' on a child content.
                    childContent.AddDiagnostic(ComponentDiagnosticFactory.Create_ChildContentHasInvalidAttribute(directiveAttribute.Source, directiveAttribute.OriginalAttributeName, attribute.Name));
                }
                else
                {
                    // This is some other kind of node (likely an implicit child content)
                    childContent.Children.Add(child);
                }
            }

            return childContent;
        }

        private bool TryGetAttributeStringContent(TagHelperPropertyIntermediateNode property, out string content)
        {
            // The success path looks like - a single HTML Attribute Value node with tokens
            if (property.Children.Count == 1 &&
                property.Children[0] is HtmlContentIntermediateNode html)
            {
                content = string.Join(string.Empty, html.Children.OfType<IntermediateToken>().Select(n => n.Content));
                return true;
            }

            content = null;
            return false;
        }

        public override void VisitTagHelperHtmlAttribute(TagHelperHtmlAttributeIntermediateNode node)
        {
            var attribute = new ComponentAttributeIntermediateNode(node);
            _children.Add(attribute);

            // Since we don't support complex content, we can rewrite the inside of this
            // node to the rather simpler form that property nodes usually have.
            for (var i = 0; i < attribute.Children.Count; i++)
            {
                if (attribute.Children[i] is HtmlAttributeValueIntermediateNode htmlValue)
                {
                    var newNode = new HtmlContentIntermediateNode()
                    {
                        Source = htmlValue.Source,
                    };
                    for (var j = 0; j < htmlValue.Children.Count; j++)
                    {
                        newNode.Children.Add(htmlValue.Children[j]);
                    }

                    attribute.Children[i] = newNode;
                }
                else if (attribute.Children[i] is CSharpExpressionAttributeValueIntermediateNode expressionValue)
                {
                    var newNode = new CSharpExpressionIntermediateNode()
                    {
                        Source = expressionValue.Source,
                    };
                    for (var j = 0; j < expressionValue.Children.Count; j++)
                    {
                        newNode.Children.Add(expressionValue.Children[j]);
                    }

                    attribute.Children[i] = newNode;
                }
                else if (attribute.Children[i] is CSharpCodeAttributeValueIntermediateNode codeValue)
                {
                    var newNode = new CSharpExpressionIntermediateNode()
                    {
                        Source = codeValue.Source,
                    };
                    for (var j = 0; j < codeValue.Children.Count; j++)
                    {
                        newNode.Children.Add(codeValue.Children[j]);
                    }

                    attribute.Children[i] = newNode;
                }
            }
        }

        public override void VisitTagHelperProperty(TagHelperPropertyIntermediateNode node)
        {
            // Each 'tag helper property' belongs to a specific tag helper. We want to handle
            // the cases for components, but leave others alone. This allows our other passes
            // to handle those cases.
            if (node.TagHelper.Kind != TagHelperKind.Component)
            {
                _children.Add(node);
                return;
            }

            // Another special case here - this might be a type argument. These don't represent 'real' parameters
            // that get passed to the component, it needs special code generation support.
            if (node.TagHelper.IsGenericTypedComponent() && node.BoundAttribute.IsTypeParameterProperty())
            {
                _children.Add(new ComponentTypeArgumentIntermediateNode(node));
                return;
            }

            // Another special case here -- this might be a 'Context' parameter, which specifies the name
            // for lambda parameter for parameterized child content
            if (node.BoundAttribute.IsChildContentParameterNameProperty())
            {
                // Check for each child content with a parameter name, that the parameter name is specified
                // with literal text. For instance, the following is not allowed and should generate a diagnostic.
                //
                // <MyComponent Context="@Foo()">...</MyComponent>
                if (TryGetAttributeStringContent(node, out var parameterName))
                {
                    _component.ChildContentParameterName = parameterName;
                    return;
                }

                // The parameter name is invalid.
                _component.AddDiagnostic(ComponentDiagnosticFactory.Create_ChildContentHasInvalidParameterOnComponent(node.Source, node.AttributeName, _component.TagName));
                return;
            }

            _children.Add(new ComponentAttributeIntermediateNode(node));
        }

        public override void VisitTagHelperDirectiveAttribute(TagHelperDirectiveAttributeIntermediateNode node)
        {
            // We don't want to do anything special with directive attributes here.
            // Let their corresponding lowering pass take care of processing them.
            _children.Add(node);
        }

        public override void VisitDefault(IntermediateNode node)
        {
            _children.Add(node);
        }
    }

    private class ElementRewriteVisitor : IntermediateNodeWalker
    {
        private readonly IntermediateNodeCollection _children;

        public ElementRewriteVisitor(IntermediateNodeCollection children)
        {
            _children = children;
        }

        public override void VisitTagHelper(TagHelperIntermediateNode node)
        {
            // Visit children, we're replacing this node.
            for (var i = 0; i < node.Children.Count; i++)
            {
                Visit(node.Children[i]);
            }
        }

        public override void VisitTagHelperBody(TagHelperBodyIntermediateNode node)
        {
            for (var i = 0; i < node.Children.Count; i++)
            {
                _children.Add(node.Children[i]);
            }
        }

        public override void VisitTagHelperHtmlAttribute(TagHelperHtmlAttributeIntermediateNode node)
        {
            var attribute = new HtmlAttributeIntermediateNode()
            {
                AttributeName = node.AttributeName,
                Source = node.Source,
            };

            _children.Add(attribute);

            attribute.AddDiagnosticsFromNode(node);

            switch (node.AttributeStructure)
            {
                case AttributeStructure.Minimized:

                    attribute.Prefix = node.AttributeName;
                    attribute.Suffix = string.Empty;
                    break;

                case AttributeStructure.NoQuotes:
                case AttributeStructure.SingleQuotes:
                case AttributeStructure.DoubleQuotes:

                    // We're ignoring attribute structure here for simplicity, it doesn't effect us.
                    attribute.Prefix = node.AttributeName + "=\"";
                    attribute.Suffix = "\"";

                    for (var i = 0; i < node.Children.Count; i++)
                    {
                        attribute.Children.Add(RewriteAttributeContent(node.Children[i]));
                    }

                    break;
            }

            IntermediateNode RewriteAttributeContent(IntermediateNode content)
            {
                if (content is HtmlContentIntermediateNode html)
                {
                    var value = new HtmlAttributeValueIntermediateNode()
                    {
                        Source = content.Source,
                    };

                    for (var i = 0; i < html.Children.Count; i++)
                    {
                        value.Children.Add(html.Children[i]);
                    }


                    value.AddDiagnosticsFromNode(html);

                    return value;
                }


                return content;
            }
        }

        public override void VisitTagHelperProperty(TagHelperPropertyIntermediateNode node)
        {
            // Each 'tag helper property' belongs to a specific tag helper. We want to handle
            // the cases for components, but leave others alone. This allows our other passes
            // to handle those cases.
            _children.Add(node.TagHelper.Kind == TagHelperKind.Component ? new ComponentAttributeIntermediateNode(node) : node);
        }

        public override void VisitTagHelperDirectiveAttribute(TagHelperDirectiveAttributeIntermediateNode node)
        {
            // We don't want to do anything special with directive attributes here.
            // Let their corresponding lowering pass take care of processing them.
            _children.Add(node);
        }

        public override void VisitDefault(IntermediateNode node)
        {
            _children.Add(node);
        }
    }
}
