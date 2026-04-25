// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public sealed class FunctionsDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
{
    private static readonly Comparer<int?> s_nullableIntComparer = Comparer<int?>.Default;

    protected override void ExecuteCore(
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        CancellationToken cancellationToken)
    {
        var @class = documentNode.FindPrimaryClass();
        if (@class == null)
        {
            return;
        }

        using var directiveNodes = new PooledArrayBuilder<IntermediateNodeReference<DirectiveIntermediateNode>>();

        documentNode.CollectDirectiveReferences(FunctionsDirective.Directive, ref directiveNodes.AsRef());

        if (codeDocument.FileKind.IsComponent())
        {
            documentNode.CollectDirectiveReferences(ComponentCodeDirective.Directive, ref directiveNodes.AsRef());
        }

        // Now we have all the directive nodes, we want to add them to the end of the class node in document order.
        // So, we sort them by their absolute index.
        directiveNodes.Sort(CompareAbsoluteIndices);

        foreach (var directiveReference in directiveNodes)
        {
            var node = directiveReference.Node;
            @class.Children.AddRange(node.Children);

            // We don't want to keep the original directive node around anymore.
            // Otherwise this can cause unintended side effects in the subsequent passes.
            directiveReference.Remove();
        }

        static int CompareAbsoluteIndices(
            IntermediateNodeReference<DirectiveIntermediateNode> n1,
            IntermediateNodeReference<DirectiveIntermediateNode> n2)
        {
            return s_nullableIntComparer.Compare(n1.Node.Source?.AbsoluteIndex, n2.Node.Source?.AbsoluteIndex);
        }
    }
}
