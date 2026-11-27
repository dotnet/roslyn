// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X;

public static class RazorExtensions
{
    public static void Register(RazorProjectEngineBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        InjectDirective.Register(builder, considerNullabilityEnforcement: false);
        ModelDirective.Register(builder);

        InheritsDirective.Register(builder);

        builder.Features.Add(new DefaultTagHelperProducer.Factory());

        // Register section directive with the 1.x compatible target extension.
        builder.AddDirective(SectionDirective.Directive);
        builder.Features.Add(new SectionDirectivePass());
        builder.AddTargetExtension(new LegacySectionTargetExtension());

        builder.AddTargetExtension(new TemplateTargetExtension()
        {
            TemplateTypeName = "global::Microsoft.AspNetCore.Mvc.Razor.HelperResult",
        });

        builder.Features.Add(new ModelExpressionPass());
        builder.Features.Add(new MvcViewDocumentClassifierPass());

        builder.Features.Add(new MvcImportProjectFeature());

        // The default C# language version for what this Razor configuration supports.
        builder.SetCSharpLanguageVersion(LanguageVersion.CSharp7_3);
    }

    public static void RegisterViewComponentTagHelpers(RazorProjectEngineBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.Features.Add(new ViewComponentTagHelperProducer.Factory());

        builder.Features.Add(new ViewComponentTagHelperPass());
        builder.AddTargetExtension(new ViewComponentTagHelperTargetExtension());
    }
}
