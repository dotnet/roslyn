// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal sealed partial class RazorDirectiveSyntax
{
    [MemberNotNullWhen(true, nameof(DirectiveDescriptor))]
    public bool HasDirectiveDescriptor
        => DirectiveDescriptor is not null;

    [MemberNotNullWhen(true, nameof(DirectiveDescriptor))]
    public bool IsDirective(DirectiveDescriptor directive)
        => DirectiveDescriptor == directive;

    [MemberNotNullWhen(true, nameof(DirectiveDescriptor))]
    public bool IsDirectiveKind(DirectiveKind kind)
        => DirectiveDescriptor?.Kind == kind;
}
