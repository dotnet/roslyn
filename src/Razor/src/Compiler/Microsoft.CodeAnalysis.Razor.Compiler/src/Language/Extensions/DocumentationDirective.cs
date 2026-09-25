// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal static class DocumentationDirective
{
    public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
        "documentation",
        DirectiveKind.CodeBlock,
        builder =>
        {
            builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
            builder.Description = Resources.DocumentationDirective_Description;
        });

    public static void Register(RazorProjectEngineBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.AddDirective(Directive, RazorFileKind.Legacy, RazorFileKind.Component, RazorFileKind.ComponentImport);
        builder.Features.Add(new DocumentationDirectivePass());
    }
}
