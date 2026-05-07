// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class DirectiveRemovalOptimizationPass : IntermediateNodePassBase, IRazorOptimizationPass
{
    public override int Order => DefaultFeatureOrder + 50;

    protected override void ExecuteCore(
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        CancellationToken cancellationToken)
    {
        foreach (var reference in documentNode.FindDescendantReferences<DirectiveIntermediateNode>())
        {
            // Lift the diagnostics in the directive node up to the document node.
            documentNode.AddDiagnosticsFromNode(reference.Node);

            reference.Remove();
        }
    }
}
