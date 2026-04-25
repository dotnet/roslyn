// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;
using System.Threading;

#if !NET
using System.Collections.Generic;
#endif

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal partial class ComponentBindLoweringPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    private static readonly string s_cultureInvariantText = $"global::{typeof(CultureInfo).FullName}.{nameof(CultureInfo.InvariantCulture)}";

    // Run after event handler pass
    public override int Order => 100;

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

        var bindGetSetSupported = codeDocument.ParserOptions.LanguageVersion >= RazorLanguageVersion.Version_7_0;

        using var references = new PooledArrayBuilder<IntermediateNodeReference<TagHelperDirectiveAttributeIntermediateNode>>();
        using var parameterReferences = new PooledArrayBuilder<IntermediateNodeReference<TagHelperDirectiveAttributeParameterIntermediateNode>>();

        ref var referencesRef = ref references.AsRef();
        ref var parameterReferencesRef = ref parameterReferences.AsRef();

        // First, process duplicates so we don't have to worry about them later.
        documentNode.CollectDescendantReferences(ref referencesRef);
        documentNode.CollectDescendantReferences(ref parameterReferencesRef);

        ProcessDuplicates(ref referencesRef, ref parameterReferencesRef);

        // Now that we've processed duplicates, re-collect all the references as some may have been removed.
        references.Clear();
        documentNode.CollectDescendantReferences(ref referencesRef);

        parameterReferences.Clear();
        documentNode.CollectDescendantReferences(ref parameterReferencesRef);

        // For each @bind usage we need to rewrite the tag helper node to map to basic constructs.

        // Collect all the non-parameterized @bind or @bind-* attributes.
        // The dict key is essentially a tuple of (parent, attributeName) to differentiate attributes
        // with the same name in two different elements. We don't have to worry about duplicate bound
        // attributes in the same element such as, <Foo @bind="bar" @bind="bar" />, because IR lowering
        // takes care of that.
        using var _ = DictionaryPool<BindEntryKey, BindEntry>.GetPooledObject(out var bindEntries);

        foreach (var reference in references)
        {
            var parent = reference.Parent;
            var node = reference.Node;

            if (node.TagHelper.Kind == TagHelperKind.Bind)
            {
                bindEntries.Add(new(parent, node), new BindEntry(reference));
            }
        }

        // Do a pass to look for (@bind:get, @bind:set) pairs as this alternative form might have been used
        // to define the binding.
        foreach (var parameterReference in parameterReferences)
        {
            var parent = parameterReference.Parent;
            var node = parameterReference.Node;

            if (!node.BoundAttributeParameter.BindAttributeGetSet)
            {
                continue;
            }

            if (!bindGetSetSupported)
            {
                node.AddDiagnostic(ComponentDiagnosticFactory.CreateBindAttributeParameter_UnsupportedSyntaxBindGetSet(
                    node.Source,
                    node.AttributeName));
            }

            var key = new BindEntryKey(parent, node);

            if (!bindEntries.TryGetValue(key, out var existingEntry))
            {
                bindEntries.Add(key, new BindEntry(parameterReference));
            }
            else
            {
                var bindNode = existingEntry.BindNode.AssumeNotNull();

                bindNode.AddDiagnostic(ComponentDiagnosticFactory.CreateBindAttributeParameter_InvalidSyntaxBindAndBindGet(
                    node.Source,
                    bindNode.AttributeName));
            }
        }

        // Now collect all the parameterized attributes and store them along with their corresponding @bind or @bind-* attributes.
        foreach (var parameterReference in parameterReferences)
        {
            var parent = parameterReference.Parent;
            var node = parameterReference.Node;

            if (node.TagHelper.Kind != TagHelperKind.Bind)
            {
                continue;
            }

            // Check if this tag contains a corresponding non-parameterized bind node.
            var key = new BindEntryKey(parent, node);
            var name = node.BoundAttributeParameter.Name;

            if (!bindEntries.TryGetValue(key, out var entry))
            {
                if (name != "set")
                {
                    // There is no corresponding bind node. Add a diagnostic and move on.
                    parent.AddDiagnostic(ComponentDiagnosticFactory.CreateBindAttributeParameter_MissingBind(
                        node.Source,
                        node.AttributeName));
                }
                else
                {
                    // There is no corresponding bind node. Add a diagnostic and move on.
                    parent.AddDiagnostic(ComponentDiagnosticFactory.CreateBindAttributeParameter_MissingBindGet(
                        node.Source,
                        node.AttributeNameWithoutParameter));
                }
            }
            else if (name == "event")
            {
                entry.BindEventNode = node;
            }
            else if (name == "format")
            {
                entry.BindFormatNode = node;
            }
            else if (name == "culture")
            {
                entry.BindCultureNode = node;
            }
            else if (name == "after")
            {
                entry.BindAfterNode = node;
            }
            else if (name == "get")
            {
                // Avoid removing the reference since it will be processed later on.
                continue;
            }
            else if (name == "set")
            {
                if (entry.BindNode != null)
                {
                    parent.AddDiagnostic(ComponentDiagnosticFactory.CreateBindAttributeParameter_UseBindGet(
                        node.Source,
                        node.BoundAttribute.Name));
                }

                entry.BindSetNode = node;
            }
            else
            {
                // Unsupported bind attribute parameter. This can only happen if bound attribute descriptor
                // is configured to expect a parameter other than 'event' and 'format'.
            }

            // We've extracted what we need from the parameterized bind node. Remove it.
            parameterReference.Remove();
        }

        // We now have all the info we need to rewrite the tag helper.
        foreach (var (key, entry) in bindEntries)
        {
            if (entry.BindSetNode != null && entry.BindAfterNode is { } afterNode)
            {
                key.Parent.AddDiagnostic(ComponentDiagnosticFactory.CreateBindAttributeParameter_InvalidSyntaxBindSetAfter(
                    afterNode.Source,
                    afterNode.AttributeNameWithoutParameter));
            }

            var reference = entry.BindNodeReference;
            var newNodes = RewriteUsage(reference.Parent, entry);
            reference.Remove();

            foreach (var newNode in newNodes)
            {
                reference.Parent.Children.Add(newNode);
            }
        }
    }

    private static void ProcessDuplicates(
        ref PooledArrayBuilder<IntermediateNodeReference<TagHelperDirectiveAttributeIntermediateNode>> references,
        ref PooledArrayBuilder<IntermediateNodeReference<TagHelperDirectiveAttributeParameterIntermediateNode>> parameterReferences)
    {
        using var _ = SpecializedPools.GetPooledReferenceEqualityHashSet<IntermediateNode>(out var parents);

        foreach (var reference in references)
        {
            parents.Add(reference.Parent);
        }

        foreach (var parameterReference in parameterReferences)
        {
            parents.Add(parameterReference.Parent);
        }

        foreach (var parent in parents)
        {
            ProcessDuplicateAttributes(parent);
        }
    }

    private static void ProcessDuplicateAttributes(IntermediateNode node)
    {
        var children = node.Children;

        // First, collect all the bind-related attributes on this node.
        using var attributes = new PooledArrayBuilder<AttributeInfo>();

        for (var i = 0; i < children.Count; i++)
        {
            switch (children[i])
            {
                case TagHelperDirectiveAttributeIntermediateNode directiveAttribute:
                    attributes.Add(new(directiveAttribute, i));
                    break;

                case TagHelperDirectiveAttributeParameterIntermediateNode directiveAttributeParameter:
                    attributes.Add(new(directiveAttributeParameter, i));
                    break;
            }
        }

        // Next, identify attributes for "fallback" bind tag helpers that are overridden by attributes
        // with more specific bind tag helpers.
        using var toRemove = new PooledArrayBuilder<AttributeInfo>();

        foreach (var attribute in attributes)
        {
            // For each usage of the general 'fallback' bind tag helper, it could duplicate
            // the usage of a more specific one. Look for duplicates and remove the fallback.
            //
            // Also treat the general <input @bind="..." /> as a 'fallback' for that case and remove it.
            // This is a workaround for a limitation where you can't write a tag helper that binds only
            // when a specific attribute is *not* present.

            if (attribute.IsFallback)
            {
                foreach (var candidate in attributes)
                {
                    if (ReferenceEquals(attribute.Node, candidate.Node))
                    {
                        // Same node, skip.
                        continue;
                    }

                    // If the candidate isn't a more specific bind tag helper then skip it.
                    if ((attribute.IsFallbackBindTagHelper && !candidate.IsBindTagHelper) ||
                        (attribute.IsInputElementFallbackBindTagHelper && !candidate.IsInputElementBindTagHelper))
                    {
                        continue;
                    }

                    if (attribute.AttributeName == candidate.AttributeName)
                    {
                        // Found a duplicate - remove the 'fallback' in favor of the more specific tag helper.
                        toRemove.Add(attribute);
                        break;
                    }
                }
            }
        }

        // Now that we've identified attributes that should be removed, iterate them in reverse order
        // and remove them using the index we stored. Note: We know the attributes are already ordered
        // by index.
        for (var i = toRemove.Count - 1; i >= 0; i--)
        {
            children.RemoveAt(toRemove[i].Index);
        }

        // If we still have duplicates at this point then they are genuine conflicts.
        // Use a hash set to quickly determine whether there are any duplicates.
        // If so, we need to do a more expensive pass to identify and remove them.
        using var _ = SpecializedPools.GetPooledStringHashSet(out var duplicates);

        foreach (var child in children)
        {
            // UNDONE: For some reason, we do not look for duplicates among parameterized
            // attributes here or in ReportDiagnosticAndRemoveDuplicates. Prior to being
            // unrolled, duplicates were identified with a LINQ expression that called
            // OfType<TagHelperDirectiveAttributeIntermediateNode>(), meaning that
            // TagHelperDirectiveAttributeParameterIntermediateNode nodes were never considered.
            // This is possibly a bug, but without a report or a repro it's hard to say for sure.
            if (child is TagHelperDirectiveAttributeIntermediateNode { AttributeName: string attributeName } &&
                !duplicates.Add(attributeName))
            {
                ReportDiagnosticAndRemoveDuplicates(node);
                break;
            }
        }

        static void ReportDiagnosticAndRemoveDuplicates(IntermediateNode node)
        {
            var children = node.Children;

            using var _ = SpecializedPools.GetPooledStringDictionary<ImmutableArray<AttributeInfo>.Builder>(out var duplicates);

            for (var i = 0; i < children.Count; i++)
            {
                if (children[i] is TagHelperDirectiveAttributeIntermediateNode directiveAttribute)
                {
                    if (!duplicates.TryGetValue(directiveAttribute.AttributeName, out var builder))
                    {
                        builder = ImmutableArray.CreateBuilder<AttributeInfo>();
                        duplicates[directiveAttribute.AttributeName] = builder;
                    }

                    builder.Add(new(directiveAttribute, i));
                }
            }

            using var toRemove = new PooledArrayBuilder<AttributeInfo>();

            foreach (var (_, builder) in duplicates)
            {
                if (builder.Count > 1)
                {
                    node.AddDiagnostic(ComponentDiagnosticFactory.CreateBindAttribute_Duplicates(
                        node.Source,
                        builder[0].OriginalAttributeName,
                        builder.Select(static x => (TagHelperDirectiveAttributeIntermediateNode)x.Node)));

                    foreach (var attribute in builder)
                    {
                        toRemove.Add(attribute);
                    }
                }
            }

            // Do we have duplicates to remove? If so, remove them in reverse order.
            if (toRemove.Count > 0)
            {
                // Sort by index to ensure we remove in the right order.
                toRemove.Sort(static (x, y) => x.Index.CompareTo(y.Index));

                for (var i = toRemove.Count - 1; i >= 0; i--)
                {
                    children.RemoveAt(toRemove[i].Index);
                }
            }
        }
    }

    private static ImmutableArray<IntermediateNode> RewriteUsage(IntermediateNode parent, BindEntry bindEntry)
    {
        // Bind works similarly to a macro, it always expands to code that the user could have written.
        //
        // For the nodes that are related to the bind-attribute rewrite them to look like a set of
        // 'normal' HTML attributes similar to the following transformation.
        //
        // Input:   <MyComponent @bind-Value="@currentCount" />
        // Output:  <MyComponent Value ="...<get the value>..." ValueChanged ="... <set the value>..." ValueExpression ="() => ...<get the value>..." />
        //
        // This means that the expression that appears inside of '@bind' must be an LValue or else
        // there will be errors. In general the errors that come from C# in this case are good enough
        // to understand the problem.
        //
        // We also support and encourage the use of EventCallback<> with bind. So in the above example
        // the ValueChanged property could be an Action<> or an EventCallback<>.
        //
        // The BindMethods calls are required with Action<> because to give us a good experience. They
        // use overloading to ensure that can get an Action<object> that will convert and set an arbitrary
        // value. We have a similar set of APIs to use with EventCallback<>.
        //
        // We also assume that the element will be treated as a component for now because
        // multiple passes handle 'special' tag helpers. We have another pass that translates
        // a tag helper node back into 'regular' element when it doesn't have an associated component
        if (!TryComputeAttributeNames(
            parent,
            bindEntry,
            out var valueAttributeName,
            out var changeAttributeName,
            out var expressionAttributeName,
            out var changeAttributeNode,
            out var valueAttribute,
            out var changeAttribute,
            out var expressionAttribute))
        {
            // Skip anything we can't understand. It's important that we don't crash, that will bring down the build.
            if (bindEntry.BindNode is { } bindNode)
            {
                bindNode.AddDiagnostic(ComponentDiagnosticFactory.CreateBindAttribute_InvalidSyntax(
                    bindNode.Source,
                    bindNode.AttributeName));

                return [bindNode];
            }
            else
            {
                var bindGetNode = bindEntry.BindGetNode.AssumeNotNull();

                bindGetNode.AddDiagnostic(ComponentDiagnosticFactory.CreateBindAttribute_MissingBindSet(
                    bindGetNode.Source,
                    bindGetNode.AttributeName,
                    $"{bindGetNode.AttributeNameWithoutParameter}:set"));

                return [bindGetNode];
            }
        }

        var original = GetAttributeContent(bindEntry.GetEffectiveBindNode());
        if (string.IsNullOrEmpty(original.Content))
        {
            // This can happen in error cases, the parser will already have flagged this
            // as an error, so ignore it.
            return [bindEntry.GetEffectiveBindNode()];
        }

        // Look for a format. If we find one then we need to pass the format into the
        // two nodes we generate.
        IntermediateToken? format = null;
        if (bindEntry.BindFormatNode is { } formatNode)
        {
            format = GetAttributeContent(formatNode);
        }
        else if (bindEntry.GetFormat() is string formatContent)
        {
            // We may have a default format if one is associated with the field type.
            format = IntermediateNodeFactory.CSharpToken($"\"{formatContent}\"");
        }

        // Look for a culture. If we find one then we need to pass the culture into the
        // two nodes we generate.
        IntermediateToken? culture = null;
        if (bindEntry.BindCultureNode is { } cultureNode)
        {
            culture = GetAttributeContent(cultureNode);
        }
        else if (bindEntry.IsInvariantCultureBindTagHelper())
        {
            // We may have a default invariant culture if one is associated with the field type.
            culture = IntermediateNodeFactory.CSharpToken(s_cultureInvariantText);
        }

        // Look for an after event. If we find one then we need to pass the event into the
        // CreateBinder call we generate.
        var after = bindEntry.BindAfterNode is { } afterNode
            ? GetAttributeContent(afterNode)
            : null;

        var setter = bindEntry.BindSetNode is { } setterNode
            ? GetAttributeContent(setterNode)
            : null;

        using var valueExpressionTokens = new PooledArrayBuilder<IntermediateToken>();
        using var changeExpressionTokens = new PooledArrayBuilder<IntermediateToken>();

        // There are a few cases to handle for @bind:
        // 1. This is a component using a delegate (int Value & Action<int> Value)
        // 2. This is a component using EventCallback (int value & EventCallback<int>)
        // 3. This is an element
        if (parent is ComponentIntermediateNode && changeAttribute != null && changeAttribute.IsDelegateProperty())
        {
            RewriteNodesForComponentDelegateBind(
                original, setter, after, changeAttribute.IsDelegateWithAwaitableResult(),
                ref valueExpressionTokens.AsRef(),
                ref changeExpressionTokens.AsRef());
        }
        else if (parent is ComponentIntermediateNode)
        {
            RewriteNodesForComponentEventCallbackBind(
                original, setter, after,
                ref valueExpressionTokens.AsRef(),
                ref changeExpressionTokens.AsRef());
        }
        else
        {
            RewriteNodesForElementEventCallbackBind(
                original, format, culture, setter, after,
                ref valueExpressionTokens.AsRef(),
                ref changeExpressionTokens.AsRef());
        }

        var targetNode = bindEntry.GetEffectiveBindNode();

        if (parent is MarkupElementIntermediateNode)
        {
            var valueNode = new HtmlAttributeIntermediateNode()
            {
                OriginalAttributeName = bindEntry.OriginalAttributeName,
                AttributeName = valueAttributeName,
                Source = targetNode.Source,

                Prefix = valueAttributeName + "=\"",
                Suffix = "\"",
            };

            valueNode.AddDiagnosticsFromNode(targetNode);

            var valueAttributeValue = new CSharpExpressionAttributeValueIntermediateNode();

            foreach (var token in valueExpressionTokens)
            {
                valueAttributeValue.Children.Add(token);
            }

            valueNode.Children.Add(valueAttributeValue);

            var changeNode = new HtmlAttributeIntermediateNode()
            {
                OriginalAttributeName = bindEntry.OriginalAttributeName,
                AttributeName = changeAttributeName,
                AttributeNameExpression = changeAttributeNode,
                Source = targetNode.Source,

                Prefix = changeAttributeName + "=\"",
                Suffix = "\"",

                EventUpdatesAttributeName = valueAttributeName,
            };

            var changeAttributeValue = new CSharpExpressionAttributeValueIntermediateNode();

            foreach (var token in changeExpressionTokens)
            {
                changeAttributeValue.Children.Add(token);
            }

            changeNode.Children.Add(changeAttributeValue);

            return [valueNode, changeNode];
        }
        else
        {
            var node = bindEntry.BindNode;
            var getNode = bindEntry.BindGetNode;

            using var builder = new PooledArrayBuilder<IntermediateNode>();

            var valueNode = node != null
                ? ComponentAttributeIntermediateNode.From(node, addChildren: false)
                : ComponentAttributeIntermediateNode.From(getNode.AssumeNotNull(), addChildren: false);

            valueNode.OriginalAttributeName = bindEntry.OriginalAttributeName;

            valueNode.PropertySpan = GetOriginalPropertySpan(valueNode);
            valueNode.AttributeName = valueAttributeName;
            valueNode.BoundAttribute = valueAttribute; // Might be null if it doesn't match a component attribute
            valueNode.PropertyName = valueAttribute?.PropertyName;
            valueNode.TypeName = valueAttribute?.IsWeaklyTyped == false ? valueAttribute.TypeName : null;

            var valueExpressionNode = new CSharpExpressionIntermediateNode();

            foreach (var token in valueExpressionTokens)
            {
                valueExpressionNode.Children.Add(token);
            }

            valueNode.Children.Add(valueExpressionNode);

            builder.Add(valueNode);

            var changeNode = node != null
                ? ComponentAttributeIntermediateNode.From(node, addChildren: false)
                : ComponentAttributeIntermediateNode.From(getNode.AssumeNotNull(), addChildren: false);

            changeNode.OriginalAttributeName = bindEntry.OriginalAttributeName;
            changeNode.PropertySpan = GetOriginalPropertySpan(changeNode);
            changeNode.IsSynthesized = true;
            changeNode.AttributeName = changeAttributeName;
            changeNode.BoundAttribute = changeAttribute; // Might be null if it doesn't match a component attribute
            changeNode.PropertyName = changeAttribute?.PropertyName;
            changeNode.TypeName = changeAttribute?.IsWeaklyTyped == false ? changeAttribute.TypeName : null;

            var changeExpressionNode = new CSharpExpressionIntermediateNode();

            foreach (var token in changeExpressionTokens)
            {
                changeExpressionNode.Children.Add(token);
            }

            changeNode.Children.Add(changeExpressionNode);

            builder.Add(changeNode);

            // Finally, also emit a node for the "Expression" attribute, but only if the target
            // component is defined to accept one
            if (expressionAttribute != null)
            {
                var expressionNode = node != null
                    ? ComponentAttributeIntermediateNode.From(node, addChildren: false)
                    : ComponentAttributeIntermediateNode.From(getNode.AssumeNotNull(), addChildren: false);

                expressionNode.OriginalAttributeName = bindEntry.OriginalAttributeName;
                expressionNode.PropertySpan = GetOriginalPropertySpan(expressionNode);
                expressionNode.IsSynthesized = true;
                expressionNode.AttributeName = expressionAttributeName;
                expressionNode.BoundAttribute = expressionAttribute;
                expressionNode.PropertyName = expressionAttribute.PropertyName;
                expressionNode.TypeName = expressionAttribute.IsWeaklyTyped ? null : expressionAttribute.TypeName;

                expressionNode.Children.Add(new CSharpExpressionIntermediateNode()
                {
                    Children = { IntermediateNodeFactory.CSharpToken($"() => {original.Content}") }
                });

                builder.Add(expressionNode);
            }

            // We don't need to generate any runtime code for these attributes normally, as they're handled by the above nodes,
            // but in order for IDE scenarios around component attributes to work we need to generate a little bit of design
            // time code, so we create design time specific nodes with minimal information in order to do so.
            ref var builderRef = ref builder.AsRef();

            TryAddDesignTimePropertyAccessHelperNode(ref builderRef, bindEntry.BindSetNode, valueAttribute);
            TryAddDesignTimePropertyAccessHelperNode(ref builderRef, bindEntry.BindEventNode, valueAttribute);
            TryAddDesignTimePropertyAccessHelperNode(ref builderRef, bindEntry.BindAfterNode, valueAttribute);

            return builder.ToImmutableAndClear();
        }
    }

    private static void TryAddDesignTimePropertyAccessHelperNode(
        ref PooledArrayBuilder<IntermediateNode> builder,
        TagHelperDirectiveAttributeParameterIntermediateNode? node,
        BoundAttributeDescriptor? valueAttribute)
    {
        if (node is null || valueAttribute is null)
        {
            return;
        }

        var helperNode = ComponentAttributeIntermediateNode.From(node, addChildren: true);
        helperNode.OriginalAttributeName = node.OriginalAttributeName;
        helperNode.IsDesignTimePropertyAccessHelper = true;
        helperNode.PropertySpan = GetOriginalPropertySpan(node);
        helperNode.BoundAttribute = valueAttribute;
        helperNode.PropertyName = valueAttribute.PropertyName;

        builder.Add(helperNode);
    }

    private static bool TryParseBindAttribute(BindEntry bindEntry, out string? valueAttributeName)
    {
        var attributeName = bindEntry.AttributeName;
        valueAttributeName = null;

        if (attributeName == "bind")
        {
            return true;
        }

        if (!attributeName.StartsWith("bind-", StringComparison.Ordinal))
        {
            return false;
        }

        valueAttributeName = attributeName["bind-".Length..];
        return true;
    }

    // Attempts to compute the attribute names that should be used for an instance of 'bind'.
    private static bool TryComputeAttributeNames(
        IntermediateNode parent,
        BindEntry bindEntry,
        out string? valueAttributeName,
        [NotNullWhen(true)] out string? changeAttributeName,
        [NotNullWhen(true)] out string? expressionAttributeName,
        out CSharpExpressionIntermediateNode? changeAttributeNode,
        out BoundAttributeDescriptor? valueAttribute,
        out BoundAttributeDescriptor? changeAttribute,
        out BoundAttributeDescriptor? expressionAttribute)
    {
        changeAttributeName = null;
        expressionAttributeName = null;
        changeAttributeNode = null;
        valueAttribute = null;
        changeAttribute = null;
        expressionAttribute = null;

        // The tag helper specifies attribute names, they should win.
        //
        // This handles cases like <input type="text" bind="@Foo" /> where the tag helper is
        // generated to match a specific tag and has metadata that identify the attributes.
        //
        // We expect 1 bind tag helper per-node.

        // Even though some of our 'bind' tag helpers specify the attribute names, they
        // should still satisfy one of the valid syntaxes.
        if (!TryParseBindAttribute(bindEntry, out valueAttributeName))
        {
            return false;
        }

        valueAttributeName = bindEntry.GetValueAttributeName() ?? valueAttributeName;

        // If there an attribute that specifies the event like @bind:event="oninput",
        // that should be preferred. Otherwise, use the one from the tag helper.
        if (bindEntry.BindEventNode == null)
        {
            // @bind:event not specified
            changeAttributeName = bindEntry.GetChangeAttributeName();
        }
        else if (TryExtractEventNodeStaticText(bindEntry.BindEventNode, out var text))
        {
            // @bind:event="oninput" - change attribute is static
            changeAttributeName = text;
        }
        else
        {
            // @bind:event="@someExpr" - we can't know the name of the change attribute, it's dynamic
            changeAttributeNode = ExtractEventNodeExpression(bindEntry.BindEventNode);
        }

        expressionAttributeName = bindEntry.GetExpressionAttributeName();

        // We expect 0-1 components per-node.
        if ((parent as ComponentIntermediateNode)?.Component is not { } componentTagHelper)
        {
            // If it's not a component node then there isn't too much else to figure out.
            return changeAttributeName != null || changeAttributeNode != null;
        }

        // If this is a component, then we can infer '<PropertyName>Changed' as the name
        // of the change event.
        changeAttributeName ??= valueAttributeName + "Changed";

        // Likewise for the expression attribute
        expressionAttributeName ??= valueAttributeName + "Expression";

        var boundAttributes = componentTagHelper.BoundAttributes;
        for (int i = boundAttributes.Length - 1, set = 0; i >= 0 && set != 3; i--)
        {
            var attribute = boundAttributes[i];
            var comparison = attribute.GetComparison();

            if (valueAttribute is null && string.Equals(valueAttributeName, attribute.Name, comparison))
            {
                valueAttribute = attribute;
                set++;
            }

            if (changeAttribute is null && string.Equals(changeAttributeName, attribute.Name, comparison))
            {
                changeAttribute = attribute;
                set++;
            }

            if (expressionAttribute is null && string.Equals(expressionAttributeName, attribute.Name, comparison))
            {
                expressionAttribute = attribute;
                set++;
            }
        }

        return true;

        static bool TryExtractEventNodeStaticText(TagHelperDirectiveAttributeParameterIntermediateNode node, [NotNullWhen(true)] out string? text)
        {
            if (node.Children is [HtmlContentIntermediateNode html, ..])
            {
                text = GetAttributeContent(html).Content;
                return true;
            }

            text = null;
            return false;
        }

        static CSharpExpressionIntermediateNode? ExtractEventNodeExpression(TagHelperDirectiveAttributeParameterIntermediateNode node)
        {
            return node.Children is [CSharpExpressionIntermediateNode expr, ..] ? expr : null;
        }
    }

    private static void RewriteNodesForComponentDelegateBind(
        IntermediateToken original,
        IntermediateToken? setter,
        IntermediateToken? after,
        bool awaitable,
        ref PooledArrayBuilder<IntermediateToken> valueExpressionTokens,
        ref PooledArrayBuilder<IntermediateToken> changeExpressionTokens)
    {
        // For a component using @bind we want to:
        //  - use the value as-is
        //  - create a delegate to handle changes
        valueExpressionTokens.Add(original);

        // Since we have to support setters and after, there are a few things to consider:
        // If we are provided with a setter, we can cast it to the change attribute type, like
        // (Action<int>)(value => { }) or (Func<int,Task>)(value => Task.CompletedTask) and use that.
        // If we are provided with an 'after' we'll need to generate a callback where we invoke the 'after' expression
        // after the regular setter. In this case, unfortunately we can't rely on EventCallbackFactory to normalize things
        // since the target attribute type is a delegate and not an EventCallback.
        // For that reason, we at least captured whether the attribute has an awaitable result, and we'll use that information
        // during code generation.
        // For example, with a synchronous 'after' method we will generate code as follows:
        // (TargetAttributeType)(__value => <code> = __value; RuntimeHelpers.InferSynchronousDelegate(after)(); }
        // With an asynchronous 'after' method we will generate code as follows:
        // (TargetAttributeType)(__value => <code> = __value; return RuntimeHelpers.InferAsynchronousDelegate(after)(); }

        // Now rewrite the content of the change-handler node. Since it's a component attribute,
        // we don't use the 'BindMethods' wrapper. We expect component attributes to always 'match' on type.
        //
        // __value => <code> = __value

        switch ((setter, after))
        {
            case (null, null):
                changeExpressionTokens.Add(IntermediateNodeFactory.CSharpToken($"__value => {original.Content} = __value"));
                break;

            case (not null, null):
                changeExpressionTokens.Add(setter);
                break;

            case (null, not null):
                var startToken = awaitable
                    ? IntermediateNodeFactory.CSharpToken($"async  __value => {{ {original.Content} = __value; await {ComponentsApi.RuntimeHelpers.InvokeAsynchronousDelegate}(")
                    : IntermediateNodeFactory.CSharpToken($" __value => {{ {original.Content} = __value; {ComponentsApi.RuntimeHelpers.InvokeSynchronousDelegate}(");

                changeExpressionTokens.Add(startToken);
                changeExpressionTokens.Add(after);
                changeExpressionTokens.Add(IntermediateNodeFactory.CSharpToken("); }"));
                break;

            default:
                // Treat this as the original case, since we don't support bind:set and bind:after simultaneously, we will produce an error.
                changeExpressionTokens.Add(IntermediateNodeFactory.CSharpToken($"__value => {original.Content} = __value"));
                break;
        }
    }

    private static void RewriteNodesForComponentEventCallbackBind(
        IntermediateToken original,
        IntermediateToken? setter,
        IntermediateToken? after,
        ref PooledArrayBuilder<IntermediateToken> valueExpressionTokens,
        ref PooledArrayBuilder<IntermediateToken> changeExpressionTokens)
    {
        // For a component using @bind we want to:
        //  - use the value as-is
        //  - create a delegate to handle changes
        valueExpressionTokens.Add(original);

        // This is largely the same as the one for elements as we can invoke CreateInferredCallback all the way to victory
        changeExpressionTokens.Add(IntermediateNodeFactory.CSharpToken($"{ComponentsApi.RuntimeHelpers.CreateInferredEventCallback}(this, "));

        switch ((setter, after))
        {
            case (null, null):
                // no bind:set nor bind:after, assign to the bound expression
                changeExpressionTokens.Add(IntermediateNodeFactory.CSharpToken($"__value => {original.Content} = __value"));
                break;

            case (not null, null):
                // bind:set only
                changeExpressionTokens.Add(setter);
                break;

            case (null, not null):
                // bind:after only
                changeExpressionTokens.Add(
                    IntermediateNodeFactory.CSharpToken($"{ComponentsApi.RuntimeHelpers.CreateInferredBindSetter}(callback: __value => {{ {original.Content} = __value; return {ComponentsApi.RuntimeHelpers.InvokeAsynchronousDelegate}(callback: "));
                changeExpressionTokens.Add(after);
                changeExpressionTokens.Add(IntermediateNodeFactory.CSharpToken($"); }}, value: {original.Content})"));
                break;

            default:
                // bind:set and bind:after create the code even though we disallow this combination through a diagnostic
                changeExpressionTokens.Add(
                    IntermediateNodeFactory.CSharpToken($"{ComponentsApi.RuntimeHelpers.CreateInferredEventCallback}(this, callback: async __value => {{ await {ComponentsApi.RuntimeHelpers.CreateInferredBindSetter}(callback: "));
                changeExpressionTokens.Add(setter);
                changeExpressionTokens.Add(
                    IntermediateNodeFactory.CSharpToken($", value: {original.Content}); await {ComponentsApi.RuntimeHelpers.InvokeAsynchronousDelegate}(callback: "));
                changeExpressionTokens.Add(after);
                changeExpressionTokens.Add(IntermediateNodeFactory.CSharpToken($"); }}, value: {original.Content})"));
                break;
        }

        changeExpressionTokens.Add(IntermediateNodeFactory.CSharpToken($", {original.Content})"));
    }

    private static void RewriteNodesForElementEventCallbackBind(
        IntermediateToken original,
        IntermediateToken? format,
        IntermediateToken? culture,
        IntermediateToken? setter,
        IntermediateToken? after,
        ref PooledArrayBuilder<IntermediateToken> valueExpressionTokens,
        ref PooledArrayBuilder<IntermediateToken> changeExpressionTokens)
    {
        // This is bind on a markup element. We use FormatValue to transform the value in the correct way
        // according to format and culture.
        //
        // Now rewrite the content of the value node to look like:
        //
        // BindConverter.FormatValue(<code>, format: <format>, culture: <culture>)
        valueExpressionTokens.Add(IntermediateNodeFactory.CSharpToken($"global::{ComponentsApi.BindConverter.FormatValue}("));
        valueExpressionTokens.Add(original);

        if (format is { Content.Length: > 0 })
        {
            valueExpressionTokens.Add(IntermediateNodeFactory.CSharpToken(", format: "));
            valueExpressionTokens.Add(format);
        }

        if (culture is { Content.Length: > 0 })
        {
            valueExpressionTokens.Add(IntermediateNodeFactory.CSharpToken(", culture: "));
            valueExpressionTokens.Add(culture);
        }

        valueExpressionTokens.Add(IntermediateNodeFactory.CSharpToken(")"));

        // Now rewrite the content of the change-handler node. There are two cases we care about
        // here. If it's a component attribute, then don't use the 'CreateBinder' wrapper. We expect
        // component attributes to always 'match' on type.
        //
        // The really tricky part of this is that we CANNOT write the type name of of the EventCallback we
        // intend to create. Doing so would really complicate the story for how we deal with generic types,
        // since the generic type lowering pass runs after this. To keep this simple we're relying on
        // the compiler to resolve overloads for us.
        //
        // RuntimeHelpers.CreateInferredEventCallback(this, __value => <code> = __value, <code>)
        //
        // For general DOM attributes, we need to be able to create a delegate that accepts UIEventArgs
        // so we use 'CreateBinder'
        //
        // EventCallbackFactory.CreateBinder(this, __value => <code> = __value, <code>, format: <format>, culture: <culture>)
        //
        // Note that the line-mappings here are applied to the value attribute, not the change attribute.
        changeExpressionTokens.Add(
            IntermediateNodeFactory.CSharpToken($"global::{ComponentsApi.EventCallback.FactoryAccessor}.{ComponentsApi.EventCallbackFactory.CreateBinderMethod}(this, "));

        switch ((setter, after))
        {
            case (null, null):
                // no bind:set nor bind:after, , assign to the bound expression
                changeExpressionTokens.Add(IntermediateNodeFactory.CSharpToken($"__value => {original.Content} = __value"));
                break;

            case (not null, null):
                // bind:set only
                changeExpressionTokens.Add(IntermediateNodeFactory.CSharpToken($"{ComponentsApi.RuntimeHelpers.CreateInferredBindSetter}(callback: "));
                changeExpressionTokens.Add(setter);
                changeExpressionTokens.Add(IntermediateNodeFactory.CSharpToken($", value: {original.Content})"));
                break;

            case (null, not null):
                // bind:after only
                changeExpressionTokens.Add(
                    IntermediateNodeFactory.CSharpToken($"{ComponentsApi.RuntimeHelpers.CreateInferredBindSetter}(callback: __value => {{ {original.Content} = __value; return {ComponentsApi.RuntimeHelpers.InvokeAsynchronousDelegate}(callback: "));
                changeExpressionTokens.Add(after);
                changeExpressionTokens.Add(IntermediateNodeFactory.CSharpToken($"); }}, value: {original.Content})"));
                break;

            default:
                // bind:set and bind:after create the code even though we disallow this combination through a diagnostic
                changeExpressionTokens.Add(
                    IntermediateNodeFactory.CSharpToken($"{ComponentsApi.RuntimeHelpers.CreateInferredEventCallback}(this, callback: async __value => {{ await {ComponentsApi.RuntimeHelpers.CreateInferredBindSetter}(callback: "));
                changeExpressionTokens.Add(setter);
                changeExpressionTokens.Add(
                    IntermediateNodeFactory.CSharpToken($", value: {original.Content})(); await {ComponentsApi.RuntimeHelpers.InvokeAsynchronousDelegate}(callback: "));
                changeExpressionTokens.Add(after);
                changeExpressionTokens.Add(IntermediateNodeFactory.CSharpToken($"); }}, value: {original.Content})"));
                break;
        }

        changeExpressionTokens.Add(IntermediateNodeFactory.CSharpToken(", "));

        changeExpressionTokens.Add(IntermediateNodeFactory.CSharpToken(original.Content));

        if (format is { Content.Length: > 0 })
        {
            changeExpressionTokens.Add(IntermediateNodeFactory.CSharpToken($", format: {format.Content}"));
        }

        if (culture is { Content.Length: > 0 })
        {
            changeExpressionTokens.Add(IntermediateNodeFactory.CSharpToken($", culture: {culture.Content}"));
        }

        changeExpressionTokens.Add(IntermediateNodeFactory.CSharpToken(")"));
    }

    private static IntermediateToken GetAttributeContent(IntermediateNode node)
    {
        using var nodes = new PooledArrayBuilder<TemplateIntermediateNode>();
        node.CollectDescendantNodes(ref nodes.AsRef());

        if (nodes.FirstOrDefault() is { } template)
        {
            // See comments in TemplateDiagnosticPass
            node.AddDiagnostic(ComponentDiagnosticFactory.Create_TemplateInvalidLocation(template.Source));
            return IntermediateNodeFactory.CSharpToken(string.Empty);
        }

        return node.Children[0] switch
        {
            // This case can be hit for a 'string' attribute. We want to turn it into an expression.
            HtmlContentIntermediateNode htmlContentNode
                => IntermediateNodeFactory.CSharpToken(GetTokenContent(htmlContentNode.Children, addQuotes: true)),

            // This case can be hit when the attribute has an explicit @ inside, which
            // 'escapes' any special sugar we provide for codegen.
            CSharpExpressionIntermediateNode csharpNode
                => GetToken(csharpNode),

            // This is the common case for 'mixed' content.
            _ => GetToken(node)
        };

        static IntermediateToken GetToken(IntermediateNode node)
        {
            if (node.Children is [IntermediateToken token])
            {
                return token;
            }

            // In error cases we won't have a single token, but we still want to generate the code.
            return IntermediateNodeFactory.CSharpToken(GetTokenContent(node.Children));
        }

        static string GetTokenContent(IntermediateNodeCollection children, bool addQuotes = false)
        {
            using var _ = StringBuilderPool.GetPooledObject(out var builder);

            if (addQuotes)
            {
                builder.Append('"');
            }

            foreach (var child in children)
            {
                if (child is IntermediateToken token)
                {
                    builder.Append(token.Content);
                }
            }

            if (addQuotes)
            {
                builder.Append('"');
            }

            return builder.ToString();
        }
    }

    private static SourceSpan? GetOriginalPropertySpan(IntermediateNode node)
    {
        var originalSpan = node switch
        {
            ComponentAttributeIntermediateNode n => n.OriginalAttributeSpan,
            TagHelperDirectiveAttributeIntermediateNode n => n.OriginalAttributeSpan,
            TagHelperDirectiveAttributeParameterIntermediateNode n => n.OriginalAttributeSpan,
            TagHelperPropertyIntermediateNode n => n.OriginalAttributeSpan,
            _ => null
        };

        if (originalSpan is SourceSpan span)
        {
            var offset = "bind-".Length;

            return new SourceSpan(span.FilePath,
                                  span.AbsoluteIndex + offset,
                                  span.LineIndex,
                                  span.CharacterIndex + offset,
                                  span.Length - offset,
                                  span.LineCount,
                                  span.EndCharacterIndex);
        }

        return null;
    }

    private sealed class BindEntry
    {
        public IntermediateNodeReference BindNodeReference { get; }

        // Note: Either BindNode or BindGetNode is non-null. They can't both be null or non-null.
        public TagHelperDirectiveAttributeIntermediateNode? BindNode { get; }
        public TagHelperDirectiveAttributeParameterIntermediateNode? BindGetNode { get; }

        public TagHelperDirectiveAttributeParameterIntermediateNode? BindEventNode { get; set; }
        public TagHelperDirectiveAttributeParameterIntermediateNode? BindFormatNode { get; set; }
        public TagHelperDirectiveAttributeParameterIntermediateNode? BindCultureNode { get; set; }
        public TagHelperDirectiveAttributeParameterIntermediateNode? BindSetNode { get; set; }
        public TagHelperDirectiveAttributeParameterIntermediateNode? BindAfterNode { get; set; }

        public TagHelperDescriptor TagHelper => field ??= GetTagHelper();
        public string AttributeName => field ??= GetAttributeName();
        public string OriginalAttributeName => field ??= GetOriginalAttributeName();

        public BindEntry(IntermediateNodeReference<TagHelperDirectiveAttributeIntermediateNode> bindNodeReference)
        {
            BindNodeReference = bindNodeReference;
            BindNode = bindNodeReference.Node;
        }

        public BindEntry(IntermediateNodeReference<TagHelperDirectiveAttributeParameterIntermediateNode> bindNodeReference)
        {
            BindNodeReference = bindNodeReference;
            BindGetNode = bindNodeReference.Node;
        }

        [DoesNotReturn]
        private static T CantBothBeNullOrNonNull<T>()
            => Assumed.Unreachable<T>($"{nameof(BindNode)} and {nameof(BindGetNode)} can't both be null or non-null.");

        public IntermediateNode GetEffectiveBindNode()
            => (BindNode, BindGetNode) switch
            {
                ({ } result, null) => result,
                (null, { } result) => result,
                _ => CantBothBeNullOrNonNull<IntermediateNode>()
            };

        private TagHelperDescriptor GetTagHelper()
            => (BindNode, BindGetNode) switch
            {
                ({ TagHelper: var result }, null) => result,
                (null, { TagHelper: var result }) => result,
                _ => CantBothBeNullOrNonNull<TagHelperDescriptor>()
            };

        private string GetOriginalAttributeName()
            => (BindNode, BindGetNode) switch
            {
                ({ OriginalAttributeName: var result }, null) => result,
                (null, { OriginalAttributeName: var result }) => result,
                _ => CantBothBeNullOrNonNull<string>()
            };

        // Return the attribute name, for @bind it's the attribute, for @bind:get is the attribute without the parameter part.
        private string GetAttributeName()
            => (BindNode, BindGetNode) switch
            {
                ({ AttributeName: var result }, null) => result,
                (null, { AttributeNameWithoutParameter: var result }) => result,
                _ => CantBothBeNullOrNonNull<string>()
            };

        public string? GetChangeAttributeName()
            => TagHelper.GetChangeAttributeName();

        public string? GetExpressionAttributeName()
            => TagHelper.GetExpressionAttributeName();

        public string? GetValueAttributeName()
            => TagHelper.GetValueAttributeName();

        public string? GetFormat()
            => TagHelper.GetFormat();

        public bool IsInvariantCultureBindTagHelper()
            => TagHelper.IsInvariantCultureBindTagHelper();
    }
}
