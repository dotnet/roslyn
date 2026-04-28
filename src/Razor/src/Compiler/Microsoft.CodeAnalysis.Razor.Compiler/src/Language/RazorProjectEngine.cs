// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorProjectEngine
{
    public RazorConfiguration Configuration { get; }
    public RazorProjectFileSystem FileSystem { get; }
    public RazorEngine Engine { get; }
    public ImmutableArray<IRazorEnginePhase> Phases => Engine.Phases;
    public ImmutableArray<IRazorProjectEngineFeature> Features { get; }

    private readonly FeatureCache<IRazorProjectEngineFeature> _featureCache;

    private readonly ImmutableArray<IConfigureRazorParserOptionsFeature> _configureParserOptionsFeatures;
    private readonly ImmutableArray<IConfigureRazorCodeGenerationOptionsFeature> _configureCodeGenerationOptionsFeatures;

    internal RazorProjectEngine(
        RazorConfiguration configuration,
        RazorEngine engine,
        RazorProjectFileSystem fileSystem,
        ImmutableArray<IRazorProjectEngineFeature> features)
    {
        Configuration = configuration;
        Engine = engine;
        FileSystem = fileSystem;
        Features = features;

        _featureCache = new(features);

        foreach (var projectFeature in features)
        {
            projectFeature.Initialize(this);
        }

        _configureParserOptionsFeatures = Engine
            .GetFeatures<IConfigureRazorParserOptionsFeature>()
            .OrderByAsArray(static x => x.Order);

        _configureCodeGenerationOptionsFeatures = Engine
            .GetFeatures<IConfigureRazorCodeGenerationOptionsFeature>()
            .OrderByAsArray(static x => x.Order);
    }

    public ImmutableArray<TFeature> GetFeatures<TFeature>()
        where TFeature : class, IRazorProjectEngineFeature
        => _featureCache.GetFeatures<TFeature>();

    public RazorCodeDocument Process(RazorProjectItem projectItem, CancellationToken cancellationToken = default)
    {
        ArgHelper.ThrowIfNull(projectItem);

        var codeDocument = CreateCodeDocumentCore(projectItem);
        return ProcessCore(codeDocument, cancellationToken);
    }

    public RazorCodeDocument Process(
        RazorSourceDocument source,
        RazorFileKind fileKind,
        ImmutableArray<RazorSourceDocument> importSources,
        TagHelperCollection? tagHelpers,
        CancellationToken cancellationToken = default)
    {
        ArgHelper.ThrowIfNull(source);
        ArgHelper.ThrowIfNull(fileKind);

        var codeDocument = CreateCodeDocumentCore(source, fileKind, importSources, tagHelpers, cssScope: null, configureParser: null, configureCodeGeneration: null);
        return ProcessCore(codeDocument, cancellationToken);
    }

    public RazorCodeDocument ProcessDeclarationOnly(RazorProjectItem projectItem, CancellationToken cancellationToken = default)
    {
        ArgHelper.ThrowIfNull(projectItem);

        var codeDocument = CreateCodeDocumentCore(projectItem, configureParser: null, configureCodeGeneration: (builder) =>
        {
            builder.SuppressPrimaryMethodBody = true;
        });

        return ProcessCore(codeDocument, cancellationToken);
    }

    public RazorCodeDocument ProcessDeclarationOnly(
        RazorSourceDocument source,
        RazorFileKind fileKind,
        ImmutableArray<RazorSourceDocument> importSources,
        TagHelperCollection? tagHelpers,
        CancellationToken cancellationToken = default)
    {
        ArgHelper.ThrowIfNull(source);
        ArgHelper.ThrowIfNull(fileKind);

        var codeDocument = CreateCodeDocumentCore(source, fileKind, importSources, tagHelpers, cssScope: null, configureParser: null, configureCodeGeneration: (builder) =>
        {
            builder.SuppressPrimaryMethodBody = true;
        });

        return ProcessCore(codeDocument, cancellationToken);
    }

    public RazorCodeDocument ProcessDesignTime(RazorProjectItem projectItem, CancellationToken cancellationToken = default)
    {
        ArgHelper.ThrowIfNull(projectItem);

        var codeDocument = CreateCodeDocumentDesignTimeCore(projectItem);
        return ProcessCore(codeDocument, cancellationToken);
    }

    public RazorCodeDocument ProcessDesignTime(
        RazorSourceDocument source,
        RazorFileKind fileKind,
        ImmutableArray<RazorSourceDocument> importSources,
        TagHelperCollection? tagHelpers,
        CancellationToken cancellationToken = default)
    {
        ArgHelper.ThrowIfNull(source);
        ArgHelper.ThrowIfNull(fileKind);

        var codeDocument = CreateCodeDocumentDesignTimeCore(source, fileKind, importSources, tagHelpers, configureParser: null, configureCodeGeneration: null);
        return ProcessCore(codeDocument, cancellationToken);
    }

    internal RazorCodeDocument CreateCodeDocument(RazorProjectItem projectItem)
    {
        ArgHelper.ThrowIfNull(projectItem);

        return CreateCodeDocumentCore(projectItem);
    }

    internal RazorCodeDocument CreateCodeDocument(
        RazorSourceDocument source,
        RazorFileKind fileKind,
        ImmutableArray<RazorSourceDocument> importSources,
        TagHelperCollection? tagHelpers,
        string? cssScope)
    {
        ArgHelper.ThrowIfNull(source);

        return CreateCodeDocumentCore(source, fileKind, importSources, tagHelpers, cssScope, configureParser: null, configureCodeGeneration: null);
    }

    internal RazorCodeDocument CreateDesignTimeCodeDocument(
        RazorSourceDocument source,
        RazorFileKind fileKind,
        ImmutableArray<RazorSourceDocument> importSources,
        TagHelperCollection? tagHelpers)
    {
        ArgHelper.ThrowIfNull(source);

        return CreateCodeDocumentDesignTimeCore(source, fileKind, importSources, tagHelpers, configureParser: null, configureCodeGeneration: null);
    }

    private RazorCodeDocument CreateCodeDocumentCore(
        RazorProjectItem projectItem,
        Action<RazorParserOptions.Builder>? configureParser = null,
        Action<RazorCodeGenerationOptions.Builder>? configureCodeGeneration = null)
    {
        var source = projectItem.GetSource();
        var importSources = GetImportSources(projectItem, designTime: false);

        return CreateCodeDocumentCore(
            source, projectItem.FileKind, importSources, tagHelpers: null, cssScope: projectItem.CssScope, configureParser, configureCodeGeneration);
    }

    private RazorCodeDocument CreateCodeDocumentCore(
        RazorSourceDocument source,
        RazorFileKind fileKind,
        ImmutableArray<RazorSourceDocument> importSources,
        TagHelperCollection? tagHelpers,
        string? cssScope,
        Action<RazorParserOptions.Builder>? configureParser,
        Action<RazorCodeGenerationOptions.Builder>? configureCodeGeneration)
    {
        var parserOptions = ComputeParserOptions(fileKind, configureParser);
        var codeGenerationOptions = ComputeCodeGenerationOptions(cssScope, configureCodeGeneration);

        var codeDocument = RazorCodeDocument.Create(source, importSources, parserOptions, codeGenerationOptions);

        return tagHelpers != null ? codeDocument.WithTagHelpers(tagHelpers) : codeDocument;
    }

    private RazorCodeDocument CreateCodeDocumentDesignTimeCore(
        RazorProjectItem projectItem,
        Action<RazorParserOptions.Builder>? configureParser = null,
        Action<RazorCodeGenerationOptions.Builder>? configureCodeGeneration = null)
    {
        var source = projectItem.GetSource();
        var importSources = GetImportSources(projectItem, designTime: true);

        return CreateCodeDocumentDesignTimeCore(source, projectItem.FileKind, importSources, tagHelpers: null, configureParser, configureCodeGeneration);
    }

    private RazorCodeDocument CreateCodeDocumentDesignTimeCore(
        RazorSourceDocument sourceDocument,
        RazorFileKind fileKind,
        ImmutableArray<RazorSourceDocument> importSources,
        TagHelperCollection? tagHelpers,
        Action<RazorParserOptions.Builder>? configureParser,
        Action<RazorCodeGenerationOptions.Builder>? configureCodeGeneration)
    {
        ArgHelper.ThrowIfNull(sourceDocument);

        var parserOptions = ComputeParserOptions(fileKind, builder =>
        {
            builder.DesignTime = true;

            configureParser?.Invoke(builder);
        });

        var codeGenerationOptions = ComputeCodeGenerationOptions(cssScope: null, builder =>
        {
            builder.DesignTime = true;
            builder.SuppressChecksum = true;
            builder.SuppressMetadataAttributes = true;

            configureCodeGeneration?.Invoke(builder);
        });

        var codeDocument = RazorCodeDocument.Create(sourceDocument, importSources, parserOptions, codeGenerationOptions);

        return tagHelpers != null ? codeDocument.WithTagHelpers(tagHelpers) : codeDocument;
    }

    private RazorParserOptions ComputeParserOptions(RazorFileKind fileKind, Action<RazorParserOptions.Builder>? configure)
    {
        var builder = new RazorParserOptions.Builder(Configuration.LanguageVersion, fileKind);

        configure?.Invoke(builder);

        foreach (var feature in _configureParserOptionsFeatures)
        {
            feature.Configure(builder);
        }

        return builder.ToOptions();
    }

    private RazorCodeGenerationOptions ComputeCodeGenerationOptions(string? cssScope, Action<RazorCodeGenerationOptions.Builder>? configure)
    {
        var configuration = Configuration;
        var builder = new RazorCodeGenerationOptions.Builder()
        {
            CssScope = cssScope,
            SuppressAddComponentParameter = configuration.SuppressAddComponentParameter,
            RazorWarningLevel = configuration.RazorWarningLevel
        };

        configure?.Invoke(builder);

        foreach (var feature in _configureCodeGenerationOptionsFeatures)
        {
            feature.Configure(builder);
        }

        return builder.ToOptions();
    }

    private RazorCodeDocument ProcessCore(RazorCodeDocument codeDocument, CancellationToken cancellationToken)
    {
        ArgHelper.ThrowIfNull(codeDocument);

        return Engine.Process(codeDocument, cancellationToken);
    }

    internal static RazorProjectEngine CreateEmpty(Action<RazorProjectEngineBuilder>? configure = null)
    {
        var builder = new RazorProjectEngineBuilder(RazorConfiguration.Default, RazorProjectFileSystem.Empty);

        configure?.Invoke(builder);

        return builder.Build();
    }

    internal static RazorProjectEngine Create(Action<RazorProjectEngineBuilder> configure)
        => Create(RazorConfiguration.Default, RazorProjectFileSystem.Empty, configure);

    public static RazorProjectEngine Create(RazorConfiguration configuration, RazorProjectFileSystem fileSystem)
        => Create(configuration, fileSystem, configure: null);

    public static RazorProjectEngine Create(
        RazorConfiguration configuration,
        RazorProjectFileSystem fileSystem,
        Action<RazorProjectEngineBuilder>? configure)
    {
        ArgHelper.ThrowIfNull(configuration);
        ArgHelper.ThrowIfNull(fileSystem);

        var builder = new RazorProjectEngineBuilder(configuration, fileSystem);

        // The initialization order is somewhat important.
        //
        // Defaults -> Extensions -> Additional customization
        //
        // This allows extensions to rely on default features, and customizations to override choices made by
        // extensions.
        AddDefaultPhases(builder.Phases);
        AddDefaultFeatures(builder.Features);

        if (configuration.LanguageVersion >= RazorLanguageVersion.Version_5_0)
        {
            builder.Features.Add(new ViewCssScopePass());
        }

        if (configuration.LanguageVersion >= RazorLanguageVersion.Version_3_0)
        {
            FunctionsDirective.Register(builder);
            ImplementsDirective.Register(builder);
            InheritsDirective.Register(builder);
            NamespaceDirective.Register(builder);
            AttributeDirective.Register(builder);

            AddComponentFeatures(builder, configuration.LanguageVersion);
        }

        configure?.Invoke(builder);

        return builder.Build();
    }

    private static void AddDefaultPhases(ImmutableArray<IRazorEnginePhase>.Builder phases)
    {
        phases.Add(new DefaultRazorParsingPhase());
        phases.Add(new DefaultRazorSyntaxTreePhase());
        phases.Add(new DefaultRazorTagHelperContextDiscoveryPhase());
        phases.Add(new DefaultRazorIntermediateNodeLoweringPhase());
        phases.Add(new DefaultTagHelperResolutionPhase());
        phases.Add(new DefaultRazorTagHelperRewritePhase());
        phases.Add(new DefaultRazorDocumentClassifierPhase());
        phases.Add(new DefaultRazorDirectiveClassifierPhase());
        phases.Add(new DefaultRazorOptimizationPhase());
        phases.Add(new DefaultRazorCSharpLoweringPhase());
    }

    private static void AddDefaultFeatures(ImmutableArray<IRazorFeature>.Builder features)
    {
        features.Add(new DefaultImportProjectFeature());
        features.Add(new TagHelperDiscoveryService());

        // General extensibility
        features.Add(new ConfigureDirectivesFeature());
        features.Add(new DefaultMetadataIdentifierFeature());

        // Syntax Tree passes
        features.Add(new DefaultDirectiveSyntaxTreePass());
        features.Add(new HtmlNodeOptimizationPass());

        // Intermediate Node Passes
        features.Add(new DefaultDocumentClassifierPass());
        features.Add(new MetadataAttributePass());
        features.Add(new DesignTimeDirectivePass());
        features.Add(new DirectiveRemovalOptimizationPass());
        features.Add(new DefaultTagHelperOptimizationPass());
        features.Add(new PreallocatedTagHelperAttributeOptimizationPass());
        features.Add(new EliminateMethodBodyPass());

        // Default Code Target Extensions
        var targetExtensionFeature = new DefaultRazorTargetExtensionFeature();
        features.Add(targetExtensionFeature);
        targetExtensionFeature.TargetExtensions.Add(new MetadataAttributeTargetExtension());
        targetExtensionFeature.TargetExtensions.Add(new DefaultTagHelperTargetExtension());
        targetExtensionFeature.TargetExtensions.Add(new PreallocatedAttributeTargetExtension());
        targetExtensionFeature.TargetExtensions.Add(new DesignTimeDirectiveTargetExtension());

        // Default configuration
        var configurationFeature = new DefaultDocumentClassifierPassFeature();
        features.Add(configurationFeature);
        configurationFeature.ConfigureClass.Add((document, @class) =>
        {
            @class.Name = "Template";
            @class.Modifiers = CommonModifiers.Public;
        });

        configurationFeature.ConfigureNamespace.Add((document, @namespace) =>
        {
            @namespace.Name = "Razor";
        });

        configurationFeature.ConfigureMethod.Add((document, method) =>
        {
            method.Name = "ExecuteAsync";
            method.ReturnType = $"global::{typeof(Task).FullName}";
            method.Modifiers = CommonModifiers.PublicAsyncOverride;
        });
    }

    private static void AddComponentFeatures(RazorProjectEngineBuilder builder, RazorLanguageVersion razorLanguageVersion)
    {
        // Project Engine Features
        builder.Features.Add(new ComponentImportProjectFeature());

        // Directives (conditional on file kind)
        ComponentCodeDirective.Register(builder);
        ComponentInjectDirective.Register(builder);
        ComponentLayoutDirective.Register(builder);
        ComponentPageDirective.Register(builder);

        if (razorLanguageVersion >= RazorLanguageVersion.Version_6_0)
        {
            ComponentConstrainedTypeParamDirective.Register(builder);
        }
        else
        {
            ComponentTypeParamDirective.Register(builder);
        }

        if (razorLanguageVersion >= RazorLanguageVersion.Version_5_0)
        {
            ComponentPreserveWhitespaceDirective.Register(builder);
        }

        if (razorLanguageVersion >= RazorLanguageVersion.Version_8_0)
        {
            ComponentRenderModeDirective.Register(builder);
        }

        // Document Classifier
        builder.Features.Add(new ComponentDocumentClassifierPass());

        // Directive Classifier
        builder.Features.Add(new ComponentWhitespacePass());

        // Optimization
        builder.Features.Add(new ComponentComplexAttributeContentPass());
        builder.Features.Add(new ComponentLoweringPass());
        builder.Features.Add(new ComponentEventHandlerLoweringPass());
        builder.Features.Add(new ComponentKeyLoweringPass());
        builder.Features.Add(new ComponentReferenceCaptureLoweringPass());
        builder.Features.Add(new ComponentSplatLoweringPass());
        builder.Features.Add(new ComponentFormNameLoweringPass());
        builder.Features.Add(new ComponentBindLoweringPass());
        builder.Features.Add(new ComponentRenderModeLoweringPass());
        builder.Features.Add(new ComponentCssScopePass());
        builder.Features.Add(new ComponentTemplateDiagnosticPass());
        builder.Features.Add(new ComponentGenericTypePass());
        builder.Features.Add(new ComponentChildContentDiagnosticPass());
        builder.Features.Add(new ComponentMarkupDiagnosticPass());
        builder.Features.Add(new ComponentMarkupBlockPass(razorLanguageVersion));
        builder.Features.Add(new ComponentMarkupEncodingPass(razorLanguageVersion));
    }

    internal void CollectImports(RazorProjectItem projectItem, ref PooledArrayBuilder<RazorProjectItem> importItems)
    {
        foreach (var importProjectFeature in GetFeatures<IImportProjectFeature>())
        {
            importProjectFeature.CollectImports(projectItem, ref importItems);
        }
    }

    internal ImmutableArray<RazorProjectItem> GetImports(RazorProjectItem projectItem)
    {
        using var imports = new PooledArrayBuilder<RazorProjectItem>();
        CollectImports(projectItem, ref imports.AsRef());

        return imports.ToImmutable();
    }

    internal ImmutableArray<RazorProjectItem> GetImports(RazorProjectItem projectItem, Func<RazorProjectItem, bool> predicate)
    {
        using var imports = new PooledArrayBuilder<RazorProjectItem>();
        CollectImports(projectItem, ref imports.AsRef());

        if (imports.Count == 0)
        {
            return [];
        }

        using var result = new PooledArrayBuilder<RazorProjectItem>(capacity: imports.Count);

        foreach (var import in imports)
        {
            if (predicate(import))
            {
                result.Add(import);
            }
        }

        return result.ToImmutableAndClear();
    }

    private ImmutableArray<RazorSourceDocument> GetImportSources(RazorProjectItem projectItem, bool designTime)
    {
        using var importItems = new PooledArrayBuilder<RazorProjectItem>();
        CollectImports(projectItem, ref importItems.AsRef());

        if (importItems.Count == 0)
        {
            return [];
        }

        // Suppress exceptions for design-time requests.
        return GetImportSourceDocuments(in importItems, suppressExceptions: designTime);
    }

    // Internal for testing
    internal static ImmutableArray<RazorSourceDocument> GetImportSourceDocuments(
        ref readonly PooledArrayBuilder<RazorProjectItem> importItems,
        bool suppressExceptions = false)
    {
        using var imports = new PooledArrayBuilder<RazorSourceDocument>(importItems.Count);

        foreach (var importItem in importItems)
        {
            if (importItem.Exists)
            {
                try
                {
                    // Normal import, has file paths, content etc.
                    var sourceDocument = importItem.GetSource();
                    imports.Add(sourceDocument);
                }
                catch (IOException) when (suppressExceptions)
                {
                    // Something happened when trying to read the item from disk.
                    // Catch the exception so we don't crash the editor.
                }
            }
        }

        return imports.ToImmutableAndClear();
    }
}
