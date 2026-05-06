// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

internal abstract partial class RazorSyntaxNode : GreenNode
{
    protected RazorSyntaxNode(SyntaxKind kind) : base(kind)
    {
    }

    protected RazorSyntaxNode(SyntaxKind kind, int width)
        : base(kind, width)
    {
    }

    protected RazorSyntaxNode(SyntaxKind kind, RazorDiagnostic[] diagnostics)
        : base(kind, diagnostics)
    {
    }

    protected RazorSyntaxNode(SyntaxKind kind, int width, RazorDiagnostic[] diagnostics)
        : base(kind, width, diagnostics)
    {
    }
}
