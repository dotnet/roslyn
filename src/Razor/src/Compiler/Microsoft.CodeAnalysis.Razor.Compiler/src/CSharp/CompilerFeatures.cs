// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;

namespace Microsoft.CodeAnalysis.Razor;

/// <summary>
/// Provides access to built-in Razor features that require a reference to <c>Microsoft.CodeAnalysis.CSharp</c>.
/// </summary>
public static class CompilerFeatures
{
    /// <summary>
    /// Registers built-in Razor features that require a reference to <c>Microsoft.CodeAnalysis.CSharp</c>.
    /// </summary>
    /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
    public static void Register(RazorProjectEngineBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        if (builder.Configuration.LanguageVersion >= RazorLanguageVersion.Version_3_0)
        {
            builder.Features.Add(new BindTagHelperProducer.Factory());
            builder.Features.Add(new ComponentTagHelperProducer.Factory());
            builder.Features.Add(new EventHandlerTagHelperProducer.Factory());
            builder.Features.Add(new RefTagHelperProducer.Factory());
            builder.Features.Add(new KeyTagHelperProducer.Factory());
            builder.Features.Add(new SplatTagHelperProducer.Factory());
        }

        if (builder.Configuration.LanguageVersion >= RazorLanguageVersion.Version_8_0)
        {
            builder.Features.Add(new RenderModeTagHelperProducer.Factory());
            builder.Features.Add(new FormNameTagHelperProducer.Factory());
        }
    }
}
