// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentCssScopePass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    // Runs after components/bind, since it's preferable for the auto-generated attribute to appear later
    // in the DOM than developer-written ones
    public override int Order => 110;

    protected override void ExecuteCore(
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        CancellationToken cancellationToken)
    {
        if (!IsComponentDocument(documentNode))
        {
            return;
        }

        var cssScope = codeDocument.CodeGenerationOptions.CssScope;
        if (string.IsNullOrEmpty(cssScope))
        {
            return;
        }

        foreach (var node in documentNode.FindDescendantNodes<MarkupElementIntermediateNode>())
        {
            // Add a minimized attribute whose name is simply the CSS scope
            node.Children.Add(new HtmlAttributeIntermediateNode
            {
                AttributeName = cssScope,
                Prefix = cssScope,
                Suffix = string.Empty,
            });
        }
    }
}
