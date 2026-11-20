// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal sealed class DocumentationDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
{
    // Documentation must precede attributes added by the other directive passes.
    public override int Order => -1;

    protected override void ExecuteCore(
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        CancellationToken cancellationToken)
    {
        var @namespace = documentNode.FindPrimaryNamespace();
        var @class = documentNode.FindPrimaryClass();
        if (@namespace is null || @class is null)
        {
            return;
        }

        var references = documentNode.FindDescendantReferences<DocumentationIntermediateNode>();
        var hasLocalDocumentation = false;
        for (var i = 0; i < references.Length; i++)
        {
            var reference = references[i];
            reference.Remove();

            if (!hasLocalDocumentation && !reference.Node.IsImported)
            {
                hasLocalDocumentation = true;
                @namespace.Children.Insert(@namespace.Children.IndexOf(@class), reference.Node);
            }
        }
    }
}
