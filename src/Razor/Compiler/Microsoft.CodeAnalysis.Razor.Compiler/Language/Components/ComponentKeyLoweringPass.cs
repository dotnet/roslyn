// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentKeyLoweringPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
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

        var @namespace = documentNode.FindPrimaryNamespace();
        var @class = documentNode.FindPrimaryClass();
        if (@namespace == null || @class == null)
        {
            // Nothing to do, bail. We can't function without the standard structure.
            return;
        }

        var references = documentNode.FindDescendantReferences<TagHelperDirectiveAttributeIntermediateNode>();

        foreach (var reference in references)
        {
            if (reference.Node.TagHelper.Kind == TagHelperKind.Key)
            {
                RewriteUsage(reference);
            }
        }
    }

    private static void RewriteUsage(IntermediateNodeReference<TagHelperDirectiveAttributeIntermediateNode> reference)
    {
        var (node, _) = reference;

        // If we can't get a nonempty attribute value, do nothing because there will
        // already be a diagnostic for empty values
        var keyValueToken = DetermineKeyValueToken(node);
        if (keyValueToken is null)
        {
            return;
        }

        var newNode = new SetKeyIntermediateNode(keyValueToken);
        reference.Replace(newNode);
    }

    private static IntermediateToken? DetermineKeyValueToken(TagHelperDirectiveAttributeIntermediateNode attributeNode)
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
