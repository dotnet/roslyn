// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal abstract class AbstractProjectExtensionProvider<TExtension, TExportAttribute>
        where TExportAttribute : Attribute
    {
        private readonly AnalyzerReference _reference;
        private ImmutableDictionary<string, ImmutableArray<TExtension>> _extensionsPerLanguage;

        public AbstractProjectExtensionProvider(AnalyzerReference reference)
        {
            _reference = reference;
            _extensionsPerLanguage = ImmutableDictionary<string, ImmutableArray<TExtension>>.Empty;
        }

        protected abstract bool SupportsLanguage(TExportAttribute exportAttribute, string language);

        protected abstract bool TryGetExtensionsFromReference(AnalyzerReference reference, out ImmutableArray<TExtension> extensions);

        public ImmutableArray<TExtension> GetExtensions(string language)
        {
            return ImmutableInterlocked.GetOrAdd(ref _extensionsPerLanguage, language, (language, provider) => provider.CreateExtensions(language), this);
        }

        private ImmutableArray<TExtension> CreateExtensions(string language)
        {
            // check whether the analyzer reference knows how to return extensions directly.
            if (TryGetExtensionsFromReference(_reference, out var extensions))
            {
                return extensions;
            }

            // otherwise, see whether we can pick it up from reference itself
            if (!(_reference is AnalyzerFileReference analyzerFileReference))
            {
                return ImmutableArray<TExtension>.Empty;
            }

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
                            if (attribute is object && SupportsLanguage(attribute, language))
                            {
                                builder.Add((TExtension)Activator.CreateInstance(typeInfo.AsType()));
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
