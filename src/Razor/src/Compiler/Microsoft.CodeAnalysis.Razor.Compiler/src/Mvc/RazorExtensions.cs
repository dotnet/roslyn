// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public static class RazorExtensions
{
    public static void Register(RazorProjectEngineBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        InjectDirective.Register(builder, considerNullabilityEnforcement: true);
        ModelDirective.Register(builder);
        PageDirective.Register(builder);

        SectionDirective.Register(builder);

        builder.Features.Add(new DefaultTagHelperProducer.Factory());
        builder.Features.Add(new ViewComponentTagHelperProducer.Factory());

        builder.AddTargetExtension(new ViewComponentTagHelperTargetExtension());
        builder.AddTargetExtension(new TemplateTargetExtension()
        {
            TemplateTypeName = "global::Microsoft.AspNetCore.Mvc.Razor.HelperResult",
        });

        builder.Features.Add(new ModelExpressionPass());
        builder.Features.Add(new PagesPropertyInjectionPass());
        builder.Features.Add(new ViewComponentTagHelperPass());

        builder.Features.Add(new RazorPageDocumentClassifierPass(builder.Configuration.UseConsolidatedMvcViews));
        builder.Features.Add(new MvcViewDocumentClassifierPass(builder.Configuration.UseConsolidatedMvcViews));

        builder.Features.Add(new MvcImportProjectFeature());

        // The default C# language version for what this Razor configuration supports.
        builder.SetCSharpLanguageVersion(LanguageVersion.CSharp8);

        if (builder.Configuration.LanguageVersion >= RazorLanguageVersion.Version_6_0)
        {
            builder.Features.Add(new CreateNewOnMetadataUpdateAttributePass());
        }
    }
}
