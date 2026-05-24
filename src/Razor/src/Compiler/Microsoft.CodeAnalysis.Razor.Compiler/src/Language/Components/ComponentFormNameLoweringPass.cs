// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentFormNameLoweringPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
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

        var references = documentNode.FindDescendantReferences<TagHelperDirectiveAttributeIntermediateNode>();
        foreach (var reference in references)
        {
            var node = reference.Node;
            if (node.TagHelper.Kind == TagHelperKind.FormName)
            {
                var parent = reference.Parent;

                if (parent is not MarkupElementIntermediateNode { TagName: "form" })
                {
                    node.AddDiagnostic(ComponentDiagnosticFactory.CreateFormName_NotAForm(node.Source));
                    reference.Replace(RewriteForErrorRecovery(node, parent));
                    continue;
                }

                reference.Replace(Rewrite(node));
            }
        }

        static IntermediateNode Rewrite(TagHelperDirectiveAttributeIntermediateNode node)
        {
            return RewriteCore(node, new FormNameIntermediateNode
            {
                Source = node.Source
            });
        }

        static IntermediateNode RewriteForErrorRecovery(TagHelperDirectiveAttributeIntermediateNode node, IntermediateNode parent)
        {
            if (parent is ComponentIntermediateNode)
            {
                return RewriteCore(node, new ComponentAttributeIntermediateNode
                {
                    Source = node.Source,
                    AttributeName = node.OriginalAttributeName,
                });
            }

            var replacement = new HtmlAttributeIntermediateNode
            {
                Source = node.Source,
                AttributeName = node.OriginalAttributeName,
                Prefix = node.OriginalAttributeName + "=\"",
                Suffix = "\"",
            };
            replacement.Children.AddRange(node.Children.Select(static child =>
            {
                IntermediateNode result = child is CSharpExpressionIntermediateNode
                    ? new CSharpExpressionAttributeValueIntermediateNode()
                    : new HtmlAttributeValueIntermediateNode();
                result.Source = child.Source;
                result.Children.AddRange(child.Children);
                result.AddDiagnosticsFromNode(child);
                return result;
            }));
            replacement.AddDiagnosticsFromNode(node);
            return replacement;
        }

        static IntermediateNode RewriteCore(TagHelperDirectiveAttributeIntermediateNode node, IntermediateNode replacement)
        {
            replacement.Children.AddRange(node.Children);
            replacement.AddDiagnosticsFromNode(node);
            return replacement;
        }
    }
}
