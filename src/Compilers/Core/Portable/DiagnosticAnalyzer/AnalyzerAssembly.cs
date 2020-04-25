// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class AnalyzerAssembly
    {
        private static readonly string s_diagnosticAnalyzerAttributeNamespace = typeof(DiagnosticAnalyzerAttribute).Namespace!;
        private static readonly string s_generatorAttributeNamespace = typeof(GeneratorAttribute).Namespace!;

        private sealed class LoadData
        {
            public static readonly LoadData Empty = new LoadData(
                assembly: null,
                exception: null,
                ImmutableDictionary<string, ImmutableHashSet<string>>.Empty,
                ImmutableDictionary<string, ImmutableHashSet<string>>.Empty,
                ImmutableArray<string?>.Empty);

            public readonly Assembly? Assembly;
            public readonly Exception? Exception;
            public readonly ImmutableDictionary<string, ImmutableHashSet<string>> AnalyzerTypeNameMap;
            public readonly ImmutableDictionary<string, ImmutableHashSet<string>> GeneratorTypeNameMap;
            public readonly ImmutableArray<string?> LanguagesWithNoExtension;

            internal LoadData(
                Assembly? assembly,
                Exception? exception,
                ImmutableDictionary<string, ImmutableHashSet<string>> analyzerTypeNameMap,
                ImmutableDictionary<string, ImmutableHashSet<string>> generatorTypeNameMap,
                ImmutableArray<string?> languagesWithNoExtension)
            {
                Debug.Assert(assembly == null || exception == null);

                Assembly = assembly;
                Exception = exception;
                AnalyzerTypeNameMap = analyzerTypeNameMap;
                GeneratorTypeNameMap = generatorTypeNameMap;
                LanguagesWithNoExtension = languagesWithNoExtension;
            }

            public ImmutableDictionary<string, ImmutableHashSet<string>> GetMap(AnalyzerExtensionKind kind)
                => kind switch
                {
                    AnalyzerExtensionKind.DiagnosticAnalyzer => AnalyzerTypeNameMap,
                    AnalyzerExtensionKind.Generator => GeneratorTypeNameMap,
                    _ => throw ExceptionUtilities.UnexpectedValue(kind)
                };
        }

        internal readonly string FullPath;
        internal readonly IAnalyzerAssemblyLoader Loader;

        // hook for legacy event-based error reporting:
        private readonly Action<Exception> _reportAssemblyLoadFailure;

        private volatile LoadData _loadData = LoadData.Empty;

        public AnalyzerAssembly(string fullPath, IAnalyzerAssemblyLoader loader, Action<Exception> reportAssemblyLoadFailure)
        {
            FullPath = fullPath;
            Loader = loader;
            _reportAssemblyLoadFailure = reportAssemblyLoadFailure;
        }

        internal Exception? GetLoadException(string? language)
        {
            Load(language, out var data);
            return data.Exception;
        }

        /// <summary>
        /// Returns true if extensions for the given <paramref name="language"/> may be available.
        /// Check the maps in resulting data to see if the extensions are actually available.
        /// </summary>
        internal bool Load(
            string? language,
            AnalyzerExtensionKind extensionKind,
            out Assembly? assembly,
            out ImmutableDictionary<string, ImmutableHashSet<string>> typeMap)
        {
            var result = Load(language, out var data);
            assembly = data.Assembly;
            typeMap = data.GetMap(extensionKind);
            return result;
        }

        private bool Load(string? language, out LoadData data)
        {
            while (true)
            {
                var existing = _loadData;
                data = existing;

                // assembly has been loaded, type maps are populated:
                if (existing.Assembly != null)
                {
                    return true;
                }

                // load failed:
                if (existing.Exception != null)
                {
                    return false;
                }

                // already checked for extensions of this language (or all languages) and found none:
                if (existing.LanguagesWithNoExtension.Contains(language) ||
                    existing.LanguagesWithNoExtension.Contains(null))
                {
                    return false;
                }

                var updated = Load(language, existing?.LanguagesWithNoExtension ?? ImmutableArray<string?>.Empty);
                if (Interlocked.CompareExchange(ref _loadData, updated, existing) == existing)
                {
                    data = updated;
                    return true;
                }
            }
        }

        private LoadData Load(string? language, ImmutableArray<string?> languagesWithNoExtension)
        {
            var emptyMap = ImmutableDictionary<string, ImmutableHashSet<string>>.Empty;

            try
            {
                // Keep the file open (preventing writes) until the assembly is loaded,
                // so that the map is consistent with the loaded assembly.
                using var assemblyMetadata = AssemblyMetadata.CreateFromFile(FullPath);

                // Read type metadata and build a map of type names of extensions defined in the assembly.
                // If we previosuly determined that a language does not have any extensions, keep the result
                // consistent and exclude such languages from the map. This may happen when first queried for
                // extensions of some language that doesn't have any in the current version of the file,
                // then the file is updated and an extension of that language added, and finally a query is made 
                // for all extensions.
                var (analyzerTypeNameMap, generatorTypeNameMap) = GetTypeNameMaps(assemblyMetadata.GetModules(), languagesWithNoExtension);

                // no matching extensions found -- don't load the assembly:
                if (language == null && analyzerTypeNameMap.Count == 0 && generatorTypeNameMap.Count == 0 ||
                    language != null && !analyzerTypeNameMap.ContainsKey(language) && !generatorTypeNameMap.ContainsKey(language))
                {
                    // Throw away the maps, only when we load the assembly we can keep the maps
                    // because only at that point we can guarantee the maps match the loaded metadata.
                    return new LoadData(assembly: null, exception: null, emptyMap, emptyMap, languagesWithNoExtension.Add(language));
                }

                var assembly = Loader.LoadFromPath(FullPath);
                return new LoadData(assembly, exception: null, analyzerTypeNameMap, generatorTypeNameMap, languagesWithNoExtension);
            }
            catch (Exception e)
            {
                _reportAssemblyLoadFailure(e);
                return new LoadData(assembly: null, e, emptyMap, emptyMap, ImmutableArray<string?>.Empty);
            }
        }

        /// <summary>
        /// Opens the analyzer dll with the metadata reader and builds a map of language -> analyzer type names.
        /// </summary>
        /// <exception cref="BadImageFormatException">The PE image format is invalid.</exception>
        /// <exception cref="IOException">IO error reading the metadata.</exception>
        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/30449")]
        private static (ImmutableDictionary<string, ImmutableHashSet<string>> analyzers, ImmutableDictionary<string, ImmutableHashSet<string>> generators)
            GetTypeNameMaps(ImmutableArray<ModuleMetadata> modules, ImmutableArray<string?> excludeLanguages)
        {
            var analyzers = PooledDictionary<string, ArrayBuilder<string>>.GetInstance();
            var generators = PooledDictionary<string, ArrayBuilder<string>>.GetInstance();

            var analyzerLanguages = ArrayBuilder<string>.GetInstance();
            var generatorLanguages = ArrayBuilder<string>.GetInstance();

            foreach (var module in modules)
            {
                var peModule = module.Module;

                foreach (var typeDefHandle in module.MetadataReader.TypeDefinitions)
                {
                    var typeDef = module.MetadataReader.GetTypeDefinition(typeDefHandle);

                    foreach (var customAttrHandle in typeDef.GetCustomAttributes())
                    {
                        if (IsDiagnosticAnalyzerAttribute(peModule, customAttrHandle))
                        {
                            ReadDiagnosticsAnalyzerSupportedLanguages(analyzerLanguages, peModule, customAttrHandle);
                        }
                        else if (IsGeneratorAttribute(peModule, customAttrHandle))
                        {
                            generatorLanguages.Add(LanguageNames.CSharp);
                        }
                    }

                    if (analyzerLanguages.Any() || generatorLanguages.Any())
                    {
                        // perf: only build the type name if the type is an analyzer or generator:
                        var typeName = GetFullyQualifiedTypeName(typeDef, module.Module);

                        addType(analyzers, analyzerLanguages, typeName);
                        addType(generators, generatorLanguages, typeName);
                    }

                    analyzerLanguages.Clear();
                    generatorLanguages.Clear();
                }
            }

            analyzerLanguages.Free();
            generatorLanguages.Free();

            return (analyzers: toImmutableAndFree(analyzers, excludeLanguages), generators: toImmutableAndFree(generators, excludeLanguages));

            static void addType(PooledDictionary<string, ArrayBuilder<string>> map, ArrayBuilder<string> languages, string typeName)
            {
                foreach (var language in languages)
                {
                    if (!map.TryGetValue(language, out var existing))
                    {
                        existing = ArrayBuilder<string>.GetInstance();
                    }

                    existing.Add(typeName);
                }
            }

            static ImmutableDictionary<string, ImmutableHashSet<string>> toImmutableAndFree(PooledDictionary<string, ArrayBuilder<string>> map, ImmutableArray<string?> excludeLanguages)
            {
                foreach (var language in excludeLanguages)
                {
                    if (language != null)
                    {
                        map.Remove(language);
                    }
                }

                var result = map.ToImmutableDictionary(entry => entry.Key, entry => entry.Value.ToImmutableHashSet());

                foreach (var (_, set) in map)
                {
                    set.Free();
                }

                map.Free();

                return result;
            }
        }

        private static bool IsDiagnosticAnalyzerAttribute(PEModule peModule, CustomAttributeHandle customAttrHandle)
            => peModule.IsTargetAttribute(customAttrHandle, s_diagnosticAnalyzerAttributeNamespace, nameof(DiagnosticAnalyzerAttribute), ctor: out _);

        private static bool IsGeneratorAttribute(PEModule peModule, CustomAttributeHandle customAttrHandle)
            => peModule.IsTargetAttribute(customAttrHandle, s_generatorAttributeNamespace, nameof(GeneratorAttribute), ctor: out _);

        private static void ReadDiagnosticsAnalyzerSupportedLanguages(ArrayBuilder<string> languages, PEModule peModule, CustomAttributeHandle customAttrHandle)
        {
            // The DiagnosticAnalyzerAttribute has one constructor, which has a string parameter for the
            // first supported language and an array parameter for addition supported languages.
            // Parse the argument blob to extract the languages.
            BlobReader argsReader = peModule.GetMemoryReaderOrThrow(peModule.GetCustomAttributeValueOrThrow(customAttrHandle));

            if (argsReader.Length > 4)
            {
                // Arguments are present--check prologue.
                if (argsReader.ReadByte() == 1 && argsReader.ReadByte() == 0)
                {
                    if (PEModule.CrackStringInAttributeValue(out var firstLanguageName, ref argsReader))
                    {
                        languages.Add(firstLanguageName);

                        if (PEModule.CrackStringArrayInAttributeValue(out var additionalLanguageNames, ref argsReader))
                        {
                            languages.AddRange(additionalLanguageNames);
                        }
                    }
                }
            }
        }

        private static string GetFullyQualifiedTypeName(TypeDefinition typeDef, PEModule peModule)
        {
            var declaringType = typeDef.GetDeclaringType();

            // Non nested type - simply get the full name
            if (declaringType.IsNil)
            {
                return peModule.GetFullNameOrThrow(typeDef.Namespace, typeDef.Name);
            }
            else
            {
                var declaringTypeDef = peModule.MetadataReader.GetTypeDefinition(declaringType);
                return GetFullyQualifiedTypeName(declaringTypeDef, peModule) + "+" + peModule.MetadataReader.GetString(typeDef.Name);
            }
        }
    }
}
