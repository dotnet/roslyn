// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal static class AttributeDirective
{
    public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
        "attribute",
        DirectiveKind.SingleLine,
        builder =>
        {
            builder.AddAttributeToken(ComponentResources.AttributeDirective_AttributeToken_Name, ComponentResources.AttributeDirective_AttributeToken_Description);
            builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
            builder.Description = ComponentResources.AttributeDirective_Description;
        });

    public static void Register(RazorProjectEngineBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddDirective(Directive, RazorFileKind.Legacy, RazorFileKind.Component, RazorFileKind.ComponentImport);
        builder.Features.Add(new AttributeDirectivePass());
    }
}
