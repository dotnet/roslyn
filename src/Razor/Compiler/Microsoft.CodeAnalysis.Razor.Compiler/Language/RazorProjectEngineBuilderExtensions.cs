// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;
using Microsoft.CodeAnalysis.CSharp;
using RazorExtensionsV1_X = Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X.RazorExtensions;
using RazorExtensionsV2_X = Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X.RazorExtensions;
using RazorExtensionsV3 = Microsoft.AspNetCore.Mvc.Razor.Extensions.RazorExtensions;

namespace Microsoft.AspNetCore.Razor.Language;

public static class RazorProjectEngineBuilderExtensions
{
    private static readonly ReadOnlyMemory<char> s_prefix = "MVC-".ToCharArray();

    public static void RegisterExtensions(this RazorProjectEngineBuilder builder)
    {
        var configurationName = builder.Configuration.ConfigurationName.AsSpanOrDefault();

        if (!configurationName.StartsWith(s_prefix.Span))
        {
            return;
        }

        configurationName = configurationName[s_prefix.Length..];

        switch (configurationName)
        {
            case ['1', '.', '0' or '1']: // 1.0 or 1.1
                RazorExtensionsV1_X.Register(builder);

                if (configurationName[^1] == '1') // 1.1.
                {
                    RazorExtensionsV1_X.RegisterViewComponentTagHelpers(builder);
                }

                break;

            case ['2', '.', '0' or '1']: // 2.0 or 2.1
                RazorExtensionsV2_X.Register(builder);
                break;

            case ['3', '.', '0']: // 3.0
                RazorExtensionsV3.Register(builder);
                break;
        }
    }

    public static RazorProjectEngineBuilder RegisterDefaultTagHelperProducer(this RazorProjectEngineBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        if (!builder.Features.OfType<DefaultTagHelperProducer.Factory>().Any())
        {
            builder.Features.Add(new DefaultTagHelperProducer.Factory());
        }

        return builder;
    }

    public static RazorProjectEngineBuilder ConfigureParserOptions(this RazorProjectEngineBuilder builder, Action<RazorParserOptions.Builder> configure)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(configure);

        builder.Features.Add(new ConfigureParserOptionsFeature(configure));

        return builder;
    }

    public static RazorProjectEngineBuilder ConfigureCodeGenerationOptions(this RazorProjectEngineBuilder builder, Action<RazorCodeGenerationOptions.Builder> configure)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(configure);

        builder.Features.Add(new ConfigureCodeGenerationOptionsFeature(configure));

        return builder;
    }

    /// <summary>
    /// Sets the root namespace for the generated code.
    /// </summary>
    /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
    /// <param name="rootNamespace">The root namespace.</param>
    /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
    public static RazorProjectEngineBuilder SetRootNamespace(this RazorProjectEngineBuilder builder, string? rootNamespace)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.ConfigureCodeGenerationOptions(builder =>
        {
            builder.RootNamespace = rootNamespace;
        });

        return builder;
    }

    /// <summary>
    /// Sets the SupportLocalizedComponentNames property to make localized component name diagnostics available.
    /// </summary>
    /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
    /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
    public static RazorProjectEngineBuilder SetSupportLocalizedComponentNames(this RazorProjectEngineBuilder builder)
    {
        ArgHelper.ThrowIfNull(builder);

        builder.ConfigureCodeGenerationOptions(builder =>
        {
            builder.SupportLocalizedComponentNames = true;
        });

        return builder;
    }

    /// <summary>
    /// Adds the specified <see cref="ICodeTargetExtension"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
    /// <param name="extension">The <see cref="ICodeTargetExtension"/> to add.</param>
    /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
    internal static RazorProjectEngineBuilder AddTargetExtension(this RazorProjectEngineBuilder builder, ICodeTargetExtension extension)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(extension);

        var targetExtensionFeature = builder.GetOrCreateFeature<IRazorTargetExtensionFeature, DefaultRazorTargetExtensionFeature>();
        targetExtensionFeature.TargetExtensions.Add(extension);

        return builder;
    }

    /// <summary>
    /// Adds the specified <see cref="DirectiveDescriptor"/> for the provided file kind.
    /// </summary>
    /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
    /// <param name="directive">The <see cref="DirectiveDescriptor"/> to add.</param>
    /// <param name="fileKinds">The file kinds, for which to register the directive. See <see cref="FileKinds"/>.</param>
    /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
    internal static RazorProjectEngineBuilder AddDirective(this RazorProjectEngineBuilder builder, DirectiveDescriptor directive, params ReadOnlySpan<RazorFileKind> fileKinds)
    {
        ArgHelper.ThrowIfNull(builder);
        ArgHelper.ThrowIfNull(directive);

        var directiveFeature = builder.GetOrCreateFeature<ConfigureDirectivesFeature>();
        directiveFeature.AddDirective(directive, fileKinds);

        return builder;
    }

    /// <summary>
    /// Sets the C# language version to target when generating code.
    /// </summary>
    /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
    /// <param name="csharpLanguageVersion">The C# <see cref="LanguageVersion"/>.</param>
    /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
    public static RazorProjectEngineBuilder SetCSharpLanguageVersion(this RazorProjectEngineBuilder builder, LanguageVersion csharpLanguageVersion)
    {
        ArgHelper.ThrowIfNull(builder);

        var existingFeature = builder.Features.OfType<ConfigureParserForCSharpVersionFeature>().FirstOrDefault();
        if (existingFeature != null)
        {
            builder.Features.Remove(existingFeature);
        }

        // This will convert any "latest", "default" or "LatestMajor" LanguageVersions into their numerical equivalent.
        var effectiveCSharpLanguageVersion = LanguageVersionFacts.MapSpecifiedToEffectiveVersion(csharpLanguageVersion);
        builder.Features.Add(new ConfigureParserForCSharpVersionFeature(builder.Configuration.LanguageVersion, effectiveCSharpLanguageVersion));

        return builder;
    }

    private static T GetOrCreateFeature<T>(this RazorProjectEngineBuilder builder)
        where T : class, IRazorEngineFeature, new()
        => builder.GetOrCreateFeature<T, T>();

    private static TInterface GetOrCreateFeature<TInterface, TFeature>(this RazorProjectEngineBuilder builder)
        where TInterface : IRazorEngineFeature
        where TFeature : class, TInterface, new()
    {
        if (builder.Features.OfType<TInterface>().FirstOrDefault() is not { } feature)
        {
            feature = new TFeature();
            builder.Features.Add(feature);
        }

        return feature;
    }

    private sealed class ConfigureParserOptionsFeature(Action<RazorParserOptions.Builder> configure) : RazorEngineFeatureBase, IConfigureRazorParserOptionsFeature
    {
        public int Order => 0;

        public void Configure(RazorParserOptions.Builder builder)
        {
            configure(builder);
        }
    }

    private sealed class ConfigureCodeGenerationOptionsFeature(Action<RazorCodeGenerationOptions.Builder> configure) : RazorEngineFeatureBase, IConfigureRazorCodeGenerationOptionsFeature
    {
        public int Order => 0;

        public void Configure(RazorCodeGenerationOptions.Builder builder)
        {
            configure(builder);
        }
    }

    private sealed class ConfigureParserForCSharpVersionFeature(
        RazorLanguageVersion razorLanguageVersion,
        LanguageVersion csharpLanguageVersion)
        : RazorEngineFeatureBase, IConfigureRazorCodeGenerationOptionsFeature
    {
        public int Order { get; set; }

        public void Configure(RazorCodeGenerationOptions.Builder builder)
        {
            if (razorLanguageVersion.Major is < 3)
            {
                // Prior to 3.0 there were no C# version specific controlled features. Suppress nullability enforcement.
                builder.SuppressNullabilityEnforcement = true;
            }
            else if (csharpLanguageVersion < LanguageVersion.CSharp8)
            {
                // Having nullable flags < C# 8.0 would cause compile errors.
                builder.SuppressNullabilityEnforcement = true;
            }
            else
            {
                // Given that nullability enforcement can be a compile error we only turn it on for C# >= 8.0. There are
                // cases in tooling when the project isn't fully configured yet at which point the CSharpLanguageVersion
                // may be Default (value 0). In those cases that C# version is equivalently "unspecified" and is up to the consumer
                // to act in a safe manner to not cause unneeded errors for older compilers. Therefore if the version isn't
                // >= 8.0 (Latest has a higher value) then nullability enforcement is suppressed.
                //
                // Once the project finishes configuration the C# language version will be updated to reflect the effective
                // language version for the project by our workspace change detectors. That mechanism extracts the correlated
                // Roslyn project and acquires the effective C# version at that point.
                builder.SuppressNullabilityEnforcement = false;
            }

            if (razorLanguageVersion.Major is >= 5)
            {
                // This is a useful optimization but isn't supported by older framework versions
                builder.OmitMinimizedComponentAttributeValues = true;
            }

            if (csharpLanguageVersion >= LanguageVersion.CSharp10)
            {
                builder.UseEnhancedLinePragma = true;
            }
        }
    }
}
