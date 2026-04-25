// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal class ImplementsDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
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

        using var interfaces = new PooledArrayBuilder<IntermediateToken>();
        interfaces.AddRange(@class.Interfaces);

        foreach (var implements in documentNode.FindDirectiveReferences(ImplementsDirective.Directive))
        {
            var token = implements.Node.Tokens.FirstOrDefault();
            if (token != null)
            {
                var source = codeDocument.ParserOptions.DesignTime ? null : token.Source;
                interfaces.Add(IntermediateNodeFactory.CSharpToken(token.Content, source));
            }
        }

        if (interfaces.Count > @class.Interfaces.Length)
        {
            @class.Interfaces = interfaces.ToImmutableAndClear();
        }
    }
}
