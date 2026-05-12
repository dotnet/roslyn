// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentSplatLoweringPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
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

        foreach (var reference in documentNode.FindDescendantReferences<TagHelperDirectiveAttributeIntermediateNode>())
        {
            if (reference.Node.TagHelper.Kind == TagHelperKind.Splat)
            {
                RewriteUsage(reference);
            }
        }
    }

    private static void RewriteUsage(IntermediateNodeReference<TagHelperDirectiveAttributeIntermediateNode> reference)
    {
        var (node, _) = reference;

        var result = new SplatIntermediateNode()
        {
            Source = node.Source,
        };

        result.Children.AddRange(node.FindDescendantNodes<CSharpIntermediateToken>());
        result.AddDiagnosticsFromNode(node);

        reference.Replace(result);
    }
}
