// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentRenderModeLoweringPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
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
            var (node, parent) = reference;

            if (node.TagHelper.Kind == TagHelperKind.RenderMode)
            {
                if (parent is not ComponentIntermediateNode componentNode)
                {
                    node.AddDiagnostic(ComponentDiagnosticFactory.CreateAttribute_ValidOnlyOnComponent(node.Source, node.OriginalAttributeName));
                    continue;
                }

                var expression = node.Children[0] switch
                {
                    CSharpExpressionIntermediateNode csharpNode => csharpNode.Children[0],
                    IntermediateNode token => token
                };

                var renderModeNode = new RenderModeIntermediateNode() { Source = node.Source, Children = { expression } };
                renderModeNode.AddDiagnosticsFromNode(node);

                if (componentNode.Component.Metadata is ComponentMetadata { HasRenderModeDirective: true })
                {
                    renderModeNode.AddDiagnostic(ComponentDiagnosticFactory.CreateRenderModeAttribute_ComponentDeclaredRenderMode(
                       node.Source,
                       componentNode.Component.Name));
                }

                reference.Replace(renderModeNode);
            }
        }
    }
}
