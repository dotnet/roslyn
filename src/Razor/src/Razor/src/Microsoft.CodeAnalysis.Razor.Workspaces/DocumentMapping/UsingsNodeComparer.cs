// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.CodeAnalysis.Razor.DocumentMapping;

internal sealed class UsingsNodeComparer : IComparer<RazorUsingDirectiveSyntax>
{
    public static readonly UsingsNodeComparer Instance = new();

    public int Compare(RazorUsingDirectiveSyntax? x, RazorUsingDirectiveSyntax? y)
    {
        if (x is null)
        {
            return y is null ? 0 : -1;
        }

        if (y is null)
        {
            return 1;
        }

        RazorSyntaxFacts.TryGetNamespaceFromDirective(x, out var xNamespace);
        RazorSyntaxFacts.TryGetNamespaceFromDirective(y, out var yNamespace);

        return UsingsStringComparer.Instance.Compare(xNamespace, yNamespace);
    }
}
