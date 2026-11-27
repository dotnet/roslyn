// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentEventHandlerLoweringPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    public override int Order => 50;

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

        // For each event handler *usage* we need to rewrite the tag helper node to map to basic constructs.
        // Each usage will be represented by a tag helper property that is a descendant of either
        // a component or element.
        using var _ = SpecializedPools.GetPooledReferenceEqualityHashSet<IntermediateNode>(out var parents);
        var references = documentNode.FindDescendantReferences<TagHelperDirectiveAttributeIntermediateNode>();

        foreach (var reference in references)
        {
            parents.Add(reference.Parent);
        }

        // We need to do something similar for directive attribute parameters like @onclick:preventDefault.
        var parameterReferences = documentNode.FindDescendantReferences<TagHelperDirectiveAttributeParameterIntermediateNode>();

        foreach (var parameterReference in parameterReferences)
        {
            parents.Add(parameterReference.Parent);
        }

        foreach (var parent in parents)
        {
            ProcessDuplicates(parent);
        }

        foreach (var reference in references)
        {
            var node = reference.Node;

            if (!reference.Parent.Children.Contains(node))
            {
                // This node was removed as a duplicate, skip it.
                continue;
            }

            if (node.TagHelper.Kind == TagHelperKind.EventHandler)
            {
                reference.Replace(RewriteUsage(reference.Parent, node));
            }
        }

        foreach (var parameterReference in parameterReferences)
        {
            var node = parameterReference.Node;

            if (!parameterReference.Parent.Children.Contains(node))
            {
                // This node was removed as a duplicate, skip it.
                continue;
            }

            if (node.TagHelper.Kind == TagHelperKind.EventHandler)
            {
                parameterReference.Replace(RewriteParameterUsage(node));
            }
        }
    }

    private static void ProcessDuplicates(IntermediateNode parent)
    {
        // Reverse order because we will remove nodes.
        //
        // Each 'property' node could be duplicated if there are multiple tag helpers that match that
        // particular attribute. This is likely to happen when a component also defines something like
        // OnClick. We want to remove the 'onclick' and let it fall back to be handled by the component.
        for (var i = parent.Children.Count - 1; i >= 0; i--)
        {
            if (parent.Children[i] is TagHelperPropertyIntermediateNode eventHandler &&
                eventHandler.TagHelper != null &&
                eventHandler.TagHelper.Kind == TagHelperKind.EventHandler)
            {
                for (var j = 0; j < parent.Children.Count; j++)
                {
                    if (parent.Children[j] is ComponentAttributeIntermediateNode componentAttribute &&
                        componentAttribute.TagHelper != null &&
                        componentAttribute.TagHelper.Kind == TagHelperKind.Component &&
                        componentAttribute.AttributeName == eventHandler.AttributeName)
                    {
                        // Found a duplicate - remove the 'fallback' in favor of the component's own handling.
                        parent.Children.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        // If we still have duplicates at this point then they are genuine conflicts.
        var duplicates = parent.Children
            .OfType<TagHelperDirectiveAttributeIntermediateNode>()
            .Where(p => p.TagHelper?.Kind == TagHelperKind.EventHandler)
            .GroupBy(p => p.AttributeName)
            .Where(g => g.Count() > 1);

        foreach (var duplicate in duplicates)
        {
            parent.AddDiagnostic(ComponentDiagnosticFactory.CreateEventHandler_Duplicates(
                parent.Source,
                duplicate.Key,
                duplicate.ToArray()));

            foreach (var property in duplicate)
            {
                parent.Children.Remove(property);
            }
        }

        var parameterDuplicates = parent.Children
            .OfType<TagHelperDirectiveAttributeParameterIntermediateNode>()
            .Where(p => p.TagHelper.Kind == TagHelperKind.EventHandler)
            .GroupBy(p => p.AttributeName)
            .Where(g => g.Count() > 1);

        foreach (var duplicate in parameterDuplicates)
        {
            parent.AddDiagnostic(ComponentDiagnosticFactory.CreateEventHandlerParameter_Duplicates(
                parent.Source,
                duplicate.Key,
                duplicate.ToArray()));

            foreach (var property in duplicate)
            {
                parent.Children.Remove(property);
            }
        }
    }

    private static IntermediateNode RewriteUsage(IntermediateNode parent, TagHelperDirectiveAttributeIntermediateNode node)
    {
        var original = GetAttributeContent(node);
        if (original.Length == 0)
        {
            // This can happen in error cases, the parser will already have flagged this
            // as an error, so ignore it.
            return node;
        }

        // Now rewrite the content of the value node to look like:
        //
        // EventCallback.Factory.Create<T>(this, <code>)
        //
        // This method is overloaded on string and T, which means that it will put the code in the
        // correct context for intellisense when typing in the attribute.
        var eventArgsType = node.TagHelper.GetEventArgsType().AssumeNotNull();

        using var tokens = new PooledArrayBuilder<IntermediateToken>(capacity: original.Length + 2);

        tokens.Add(
            IntermediateNodeFactory.CSharpToken($"global::{ComponentsApi.EventCallback.FactoryAccessor}.{ComponentsApi.EventCallbackFactory.CreateMethod}<{TypeNameHelper.GetGloballyQualifiedNameIfNeeded(eventArgsType)}>(this, "));

        tokens.AddRange(original);

        tokens.Add(IntermediateNodeFactory.CSharpToken(")"));

        var attributeName = node.AttributeName;

        if (parent is MarkupElementIntermediateNode)
        {
            var result = new HtmlAttributeIntermediateNode()
            {
                OriginalAttributeName = node.OriginalAttributeName,
                AttributeName = attributeName,
                Source = node.Source,

                Prefix = attributeName + "=\"",
                Suffix = "\"",
            };

            result.AddDiagnosticsFromNode(node);

            var attributeValueNode = new CSharpExpressionAttributeValueIntermediateNode();
            result.Children.Add(attributeValueNode);

            foreach (var token in tokens)
            {
                attributeValueNode.Children.Add(token);
            }

            return result;
        }
        else
        {
            var result = ComponentAttributeIntermediateNode.From(node, addChildren: false);
            result.OriginalAttributeName = node.OriginalAttributeName;

            var expressionNode = new CSharpExpressionIntermediateNode();

            foreach (var token in tokens)
            {
                expressionNode.Children.Add(token);
            }

            result.Children.Add(expressionNode);

            return result;
        }
    }

    private static ImmutableArray<IntermediateToken> GetAttributeContent(IntermediateNode node)
    {
        var nodes = node.FindDescendantNodes<TemplateIntermediateNode>();
        var template = nodes.Length > 0 ? nodes[0] : null;
        if (template != null)
        {
            // See comments in TemplateDiagnosticPass
            node.AddDiagnostic(ComponentDiagnosticFactory.Create_TemplateInvalidLocation(template.Source));
            return [IntermediateNodeFactory.CSharpToken(string.Empty)];
        }

        if (node.Children.Count == 1 && node.Children[0] is HtmlContentIntermediateNode htmlContentNode)
        {
            // This case can be hit for a 'string' attribute. We want to turn it into
            // an expression.
            var tokens = htmlContentNode.FindDescendantNodes<IntermediateToken>();

            var content = "\"" + string.Join(string.Empty, tokens.Select(t => t.Content.Replace("\"", "\\\""))) + "\"";
            return [IntermediateNodeFactory.CSharpToken(content)];
        }

        return node.FindDescendantNodes<IntermediateToken>();
    }

    private static IntermediateNode RewriteParameterUsage(TagHelperDirectiveAttributeParameterIntermediateNode node)
    {
        // Now rewrite the node to look like:
        //
        // builder.AddEventPreventDefaultAttribute(2, "onclick", true); // If minimized.
        // or
        // builder.AddEventPreventDefaultAttribute(2, "onclick", someBoolExpression); // If a bool expression is provided in the value.

        string eventHandlerMethod;
        if (node.BoundAttributeParameter.Name == "preventDefault")
        {
            eventHandlerMethod = ComponentsApi.RenderTreeBuilder.AddEventPreventDefaultAttribute;
        }
        else if (node.BoundAttributeParameter.Name == "stopPropagation")
        {
            eventHandlerMethod = ComponentsApi.RenderTreeBuilder.AddEventStopPropagationAttribute;
        }
        else
        {
            // Unsupported event handler attribute parameter. This can only happen if bound attribute descriptor
            // is configured to expect a parameter other than 'preventDefault' and 'stopPropagation'.
            return node;
        }

        var result = ComponentAttributeIntermediateNode.From(node, addChildren: false);
        result.OriginalAttributeName = node.OriginalAttributeName;
        result.AddAttributeMethodName = eventHandlerMethod;

        if (node.AttributeStructure != AttributeStructure.Minimized)
        {
            var tokens = GetAttributeContent(node);
            var expressionNode = new CSharpExpressionIntermediateNode();
            result.Children.Add(expressionNode);
            expressionNode.Children.AddRange(tokens);
        }

        return result;
    }
}
