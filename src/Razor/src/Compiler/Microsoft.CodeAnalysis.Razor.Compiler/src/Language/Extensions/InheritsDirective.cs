// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public static class InheritsDirective
{
    public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
        SyntaxConstants.CSharp.InheritsKeyword,
        DirectiveKind.SingleLine,
        builder =>
        {
            builder.AddTypeToken(Resources.InheritsDirective_TypeToken_Name, Resources.InheritsDirective_TypeToken_Description);
            builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
            builder.Description = Resources.InheritsDirective_Description;
        });

    public static void Register(RazorProjectEngineBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddDirective(Directive, RazorFileKind.Legacy, RazorFileKind.Component, RazorFileKind.ComponentImport);
        builder.Features.Add(new InheritsDirectivePass());
    }
}
