// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal abstract class AbstractProjectExtensionProvider<TProvider, TExtension, TExportAttribute>
    where TProvider : AbstractProjectExtensionProvider<TProvider, TExtension, TExportAttribute>, new()
    where TExportAttribute : Attribute
    where TExtension : class
{
    public sealed record class ExtensionInfo(ImmutableArray<TextDocumentKind> DocumentKinds, string[]? DocumentExtensions);

    // Following CWTs are used to cache completion providers from projects' references,
    // so we can avoid the slow path unless there's any change to the references.
    private static readonly ConditionalWeakTable<IReadOnlyList<AnalyzerReference>, StrongBox<ImmutableArray<TExtension>>> s_referencesToExtensionsMap = new();
    private static readonly ConditionalWeakTable<AnalyzerReference, TProvider> s_referenceToProviderMap = new();
    private static readonly ConditionalWeakTable<TExtension, ExtensionInfo?> s_extensionInfoMap = new();

    private AnalyzerReference Reference { get; init; } = null!;
    private ImmutableDictionary<string, ImmutableArray<TExtension>> _extensionsPerLanguage = ImmutableDictionary<string, ImmutableArray<TExtension>>.Empty;
    private IExtensionManager ExtensionManager { get; init; } = null!;

    protected abstract ImmutableArray<string> GetLanguages(TExportAttribute exportAttribute);
    protected abstract bool TryGetExtensionsFromReference(AnalyzerReference reference, out ImmutableArray<TExtension> extensions);

    public static bool TryGetCachedExtensions(IReadOnlyList<AnalyzerReference> analyzerReferences, out ImmutableArray<TExtension> extensions)
    {
        if (s_referencesToExtensionsMap.TryGetValue(analyzerReferences, out var providers))
        {
            extensions = providers.Value;
            return true;
        }

        extensions = [];
        return false;
    }

    public static ImmutableArray<TExtension> GetExtensions(Project? project)
    {
        if (project is null)
            return [];

        var extensionManager = project.Solution.Services.GetRequiredService<IExtensionManager>();
        return GetExtensions(project.Language, project.AnalyzerReferences, extensionManager);
    }

    public static ImmutableArray<TExtension> GetExtensions(string language, IReadOnlyList<AnalyzerReference> analyzerReferences, IExtensionManager extensionManager)
    {
        if (TryGetCachedExtensions(analyzerReferences, out var providers))
            return providers;

        return GetExtensionsSlow(language, analyzerReferences, extensionManager);

        static ImmutableArray<TExtension> GetExtensionsSlow(string language, IReadOnlyList<AnalyzerReference> analyzerReferences, IExtensionManager extensionManager)
            => s_referencesToExtensionsMap.GetValue(analyzerReferences, _ => new(ComputeExtensions(language, analyzerReferences, extensionManager))).Value;

        static ImmutableArray<TExtension> ComputeExtensions(string language, IReadOnlyList<AnalyzerReference> analyzerReferences, IExtensionManager extensionManager)
        {
            using var _ = ArrayBuilder<TExtension>.GetInstance(out var builder);
            foreach (var reference in analyzerReferences)
            {
                var provider = s_referenceToProviderMap.GetValue(
                    reference, reference => new TProvider() { Reference = reference, ExtensionManager = extensionManager });
                foreach (var extension in provider.GetExtensions(language))
                    builder.Add(extension);
            }

            return builder.ToImmutableAndClear();
        }
    }

    public static ImmutableArray<TExtension> GetExtensions(TextDocument document, Func<TExportAttribute, ExtensionInfo>? getExtensionInfoForFiltering)
    {
        var extensions = GetExtensions(document.Project);
        return getExtensionInfoForFiltering != null
            ? FilterExtensions(document, extensions, getExtensionInfoForFiltering)
            : extensions;
    }

    public static ImmutableArray<TExtension> FilterExtensions(TextDocument document, ImmutableArray<TExtension> extensions, Func<TExportAttribute, ExtensionInfo> getExtensionInfoForFiltering)
    {
        return extensions.WhereAsArray(ShouldIncludeExtension, (document, getExtensionInfoForFiltering));

        static bool ShouldIncludeExtension(TExtension extension, (TextDocument, Func<TExportAttribute, ExtensionInfo>) args)
        {
            var (document, getExtensionInfoForFiltering) = args;
            if (!s_extensionInfoMap.TryGetValue(extension, out var extensionInfo))
                extensionInfo = GetOrCreateExtensionInfo(extension, getExtensionInfoForFiltering);

            if (extensionInfo == null)
                return true;

            if (extensionInfo.DocumentKinds.IndexOf(document.Kind) < 0)
                return false;

            if (document.FilePath != null &&
                extensionInfo.DocumentExtensions != null &&
                Array.IndexOf(extensionInfo.DocumentExtensions, PathUtilities.GetExtension(document.FilePath)) < 0)
            {
                return false;
            }

            return true;
        }

        static ExtensionInfo? GetOrCreateExtensionInfo(TExtension extension, Func<TExportAttribute, ExtensionInfo> getExtensionInfoForFiltering)
        {
            return s_extensionInfoMap.GetValue(extension,
                new ConditionalWeakTable<TExtension, ExtensionInfo?>.CreateValueCallback(ComputeExtensionInfo));

            ExtensionInfo? ComputeExtensionInfo(TExtension extension)
            {
                TExportAttribute? attribute;
                try
                {
                    var typeInfo = extension.GetType().GetTypeInfo();
                    attribute = typeInfo.GetCustomAttribute<TExportAttribute>();
                }
                catch
                {
                    attribute = null;
                }

                if (attribute == null)
                    return null;
                return getExtensionInfoForFiltering(attribute);
            }
        }
    }

    private ImmutableArray<TExtension> GetExtensions(string language)
        => ImmutableInterlocked.GetOrAdd(ref _extensionsPerLanguage, language, (language, provider) => provider.CreateExtensions(language), this);

    private ImmutableArray<TExtension> CreateExtensions(string language)
    {
        // check whether the analyzer reference knows how to return extensions directly.
        if (TryGetExtensionsFromReference(this.Reference, out var extensions))
            return extensions;

        var analyzerFileReference = GetAnalyzerFileReference(this.Reference);

        // otherwise, see whether we can pick it up from reference itself
        if (analyzerFileReference is null)
            return [];

        using var _ = ArrayBuilder<TExtension>.GetInstance(out var builder);

        this.ExtensionManager.PerformAction(analyzerFileReference.FullPath, LoadExtensionAssembly);

        return builder.ToImmutableAndClear();

        void LoadExtensionAssembly()
        {
            var analyzerAssembly = analyzerFileReference.GetAssembly();
            var typeInfos = analyzerAssembly.DefinedTypes;

            foreach (var typeInfo in typeInfos)
            {
                if (typeof(CodeFixProvider).IsAssignableFrom(typeInfo))
                {
                    throw new Exception("External");
                }

                if (typeof(TExtension).IsAssignableFrom(typeInfo))
                {
                    this.ExtensionManager.PerformAction(typeInfo.Name, () => LoadExtensionType(typeInfo));
                }
            }
        }

        void LoadExtensionType(System.Reflection.TypeInfo typeInfo)
        {
            if (typeof(CodeFixProvider).IsAssignableFrom(typeInfo))
            {
                throw new Exception("Internal");
            }

            var attribute = typeInfo.GetCustomAttribute<TExportAttribute>();
            if (attribute is not null)
            {
                var languages = GetLanguages(attribute);
                if (languages.Contains(language))
                    builder.AddIfNotNull((TExtension?)Activator.CreateInstance(typeInfo.AsType()));
            }
        }

        static AnalyzerFileReference? GetAnalyzerFileReference(AnalyzerReference reference)
        {
            if (reference is AnalyzerFileReference analyzerFileReference)
                return analyzerFileReference;
#if NET
            if (reference is IsolatedAnalyzerFileReference isolatedReference)
                return isolatedReference.UnderlyingAnalyzerFileReference;
#endif

            return null;
        }
    }
}
