// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal static class ImplementsDirective
{
    public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
        "implements",
        DirectiveKind.SingleLine,
        builder =>
        {
            builder.AddTypeToken(ComponentResources.ImplementsDirective_TypeToken_Name, ComponentResources.ImplementsDirective_TypeToken_Description);
            builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
            builder.Description = ComponentResources.ImplementsDirective_Description;
        });

    public static void Register(RazorProjectEngineBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddDirective(Directive, RazorFileKind.Legacy, RazorFileKind.Component, RazorFileKind.ComponentImport);
        builder.Features.Add(new ImplementsDirectivePass());
    }
}
