// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentClassNameDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
{
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

        var directives = documentNode.FindDirectiveReferences(ComponentClassNameDirective.Directive);
        if (directives.Length == 0)
        {
            return;
        }

        var token = directives[0].Node.Tokens.FirstOrDefault();
        if (token == null)
        {
            return;
        }

        @class.Name = IntermediateNodeFactory.CSharpToken(token.Content, token.Source);
    }
}
