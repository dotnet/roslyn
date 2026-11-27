// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal abstract partial class BaseRazorDirectiveSyntax
{
    public RazorDirectiveBodySyntax DirectiveBody => (RazorDirectiveBodySyntax)Body;

    [MemberNotNullWhen(true, nameof(DirectiveDescriptor))]
    public bool IsDirectiveKind(DirectiveKind kind)
        => DirectiveDescriptor?.Kind == kind;
}
