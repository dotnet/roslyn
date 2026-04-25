// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public static class FunctionsDirective
{
    public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
        SyntaxConstants.CSharp.FunctionsKeyword,
        DirectiveKind.CodeBlock,
        builder =>
        {
            builder.Description = Resources.FunctionsDirective_Description;
        });

    public static void Register(RazorProjectEngineBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddDirective(Directive, RazorFileKind.Legacy, RazorFileKind.Component);
        builder.Features.Add(new FunctionsDirectivePass());
    }
}
