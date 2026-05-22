// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentReferenceCaptureLoweringPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    // Run after component lowering pass
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

        var namespaceNode = documentNode.FindPrimaryNamespace();
        var classNode = documentNode.FindPrimaryClass();
        if (namespaceNode == null || classNode == null)
        {
            // Nothing to do, bail. We can't function without the standard structure.
            return;
        }

        var references = documentNode.FindDescendantReferences<TagHelperDirectiveAttributeIntermediateNode>();

        foreach (var reference in references)
        {
            if (reference.Node.TagHelper.Kind == TagHelperKind.Ref)
            {
                RewriteUsage(reference);
            }
        }
    }

    private static void RewriteUsage(IntermediateNodeReference<TagHelperDirectiveAttributeIntermediateNode> reference)
    {
        var (node, parent) = reference;

        // If we can't get a non-empty attribute name, do nothing because there will
        // already be a diagnostic for empty values
        var identifierToken = DetermineIdentifierToken(node);
        if (identifierToken is null)
        {
            return;
        }

        // Determine whether this is an element capture or a component capture, and
        // if applicable the type name that will appear in the resulting capture code
        var referenceCapture = parent as ComponentIntermediateNode is { Component: { } componentTagHelper }
            ? new ReferenceCaptureIntermediateNode(identifierToken, componentTagHelper.TypeName)
            : new ReferenceCaptureIntermediateNode(identifierToken);

        reference.Replace(referenceCapture);
    }

    private static IntermediateToken? DetermineIdentifierToken(TagHelperDirectiveAttributeIntermediateNode attributeNode)
    {
        var foundToken = attributeNode.Children switch
        {
            [IntermediateToken token] => token,
            [CSharpExpressionIntermediateNode { Children: [IntermediateToken token] }] => token,
            _ => null,
        };

        if (foundToken is null)
        {
            return null;
        }

        return !foundToken.Content.IsNullOrWhiteSpace()
            ? foundToken
            : null;
    }
}
