// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal static class ComponentClassNameDirective
{
    public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
        "classname",
        DirectiveKind.SingleLine,
        builder =>
        {
            builder.AddMemberToken(ComponentResources.ClassNameDirective_Token_Name, ComponentResources.ClassNameDirective_Token_Description);
            builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
            builder.Description = ComponentResources.ClassNameDirective_Description;
        });

    public static void Register(RazorProjectEngineBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddDirective(Directive, RazorFileKind.Component);
        builder.Features.Add(new ComponentClassNameDirectivePass());
    }
}
