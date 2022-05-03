// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal abstract class AbstractProjectExtensionProvider<TProvider, TExtension, TExportAttribute>
        where TProvider : AbstractProjectExtensionProvider<TProvider, TExtension, TExportAttribute>, new()
        where TExportAttribute : Attribute
        where TExtension : class
    {
        // Following CWTs are used to cache completion providers from projects' references,
        // so we can avoid the slow path unless there's any change to the references.
        private static readonly ConditionalWeakTable<IReadOnlyList<AnalyzerReference>, StrongBox<ImmutableArray<TExtension>>> s_referencesToExtensionsMap = new();
        private static readonly ConditionalWeakTable<AnalyzerReference, TProvider> s_referenceToProviderMap = new();

        private AnalyzerReference Reference { get; init; } = null!;
        private ImmutableDictionary<string, ImmutableArray<TExtension>> _extensionsPerLanguage = ImmutableDictionary<string, ImmutableArray<TExtension>>.Empty;

        protected abstract bool SupportsLanguage(TExportAttribute exportAttribute, string language);
        protected abstract bool TryGetExtensionsFromReference(AnalyzerReference reference, out ImmutableArray<TExtension> extensions);

        public static ImmutableArray<TExtension> GetExtensions(Project? project)
        {
            if (project is null)
                return ImmutableArray<TExtension>.Empty;

            if (s_referencesToExtensionsMap.TryGetValue(project.AnalyzerReferences, out var providers))
                return providers.Value;

            return GetExtensionsSlow(project);

            ImmutableArray<TExtension> GetExtensionsSlow(Project project)
                => s_referencesToExtensionsMap.GetValue(project.AnalyzerReferences, _ => new(ComputeExtensions(project))).Value;

            ImmutableArray<TExtension> ComputeExtensions(Project project)
            {
                using var _ = ArrayBuilder<TExtension>.GetInstance(out var builder);
                foreach (var reference in project.AnalyzerReferences)
                {
                    var provider = s_referenceToProviderMap.GetValue(
                        reference, static reference => new TProvider() { Reference = reference });
                    foreach (var extension in provider.GetExtensions(project.Language))
                        builder.Add(extension);
                }

                return builder.ToImmutable();
            }
        }

        private ImmutableArray<TExtension> GetExtensions(string language)
            => ImmutableInterlocked.GetOrAdd(ref _extensionsPerLanguage, language, (language, provider) => provider.CreateExtensions(language), this);

        private ImmutableArray<TExtension> CreateExtensions(string language)
        {
            // check whether the analyzer reference knows how to return extensions directly.
            if (TryGetExtensionsFromReference(this.Reference, out var extensions))
                return extensions;

            // otherwise, see whether we can pick it up from reference itself
            if (this.Reference is not AnalyzerFileReference analyzerFileReference)
                return ImmutableArray<TExtension>.Empty;

            using var _ = ArrayBuilder<TExtension>.GetInstance(out var builder);

            try
            {
                var analyzerAssembly = analyzerFileReference.GetAssembly();
                var typeInfos = analyzerAssembly.DefinedTypes;

                foreach (var typeInfo in typeInfos)
                {
                    if (typeInfo.IsSubclassOf(typeof(TExtension)))
                    {
                        try
                        {
                            var attribute = typeInfo.GetCustomAttribute<TExportAttribute>();
                            if (attribute is not null && SupportsLanguage(attribute, language))
                            {
                                builder.AddIfNotNull((TExtension?)Activator.CreateInstance(typeInfo.AsType()));
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
                // REVIEW: is the below message right?
                // NOTE: We could report "unable to load analyzer" exception here but it should have been already reported by DiagnosticService.
            }

            return builder.ToImmutable();
        }
    }
}
