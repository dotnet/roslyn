// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Represents analyzers stored in an analyzer assembly file.
    /// </summary>
    /// <remarks>
    /// Analyzer are read from the file, owned by the reference, and doesn't change 
    /// since the reference is accessed until the reference object is garbage collected.
    /// 
    /// If you need to manage the lifetime of the analyzer reference (and the file stream) explicitly use <see cref="AnalyzerImageReference"/>.
    /// </remarks>
    public sealed class AnalyzerFileReference : AnalyzerReference, IEquatable<AnalyzerReference>
    {
        private static readonly string s_diagnosticAnalyzerAttributeNamespace = typeof(DiagnosticAnalyzerAttribute).Namespace;

        private delegate bool AttributePredicate(PEModule module, CustomAttributeHandle attribute);

        private readonly string _fullPath;
        private readonly IAnalyzerAssemblyLoader _assemblyLoader;
        private readonly Extensions<DiagnosticAnalyzer> _diagnosticAnalyzers;

        private string _lazyDisplay;
        private object _lazyIdentity;
        private Assembly _lazyAssembly;

        public event EventHandler<AnalyzerLoadFailureEventArgs> AnalyzerLoadFailed;

        /// <summary>
        /// Creates an AnalyzerFileReference with the given <paramref name="fullPath"/> and <paramref name="assemblyLoader"/>.
        /// </summary>
        /// <param name="fullPath">Full path of the analyzer assembly.</param>
        /// <param name="assemblyLoader">Loader for obtaining the <see cref="Assembly"/> from the <paramref name="fullPath"/></param>
        public AnalyzerFileReference(string fullPath, IAnalyzerAssemblyLoader assemblyLoader)
        {
            _fullPath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
            _diagnosticAnalyzers = new Extensions<DiagnosticAnalyzer>(this, IsDiagnosticAnalyzerAttribute);
            _assemblyLoader = assemblyLoader ?? throw new ArgumentNullException(nameof(assemblyLoader));

            // Note this analyzer full path as a dependency location, so that the analyzer loader
            // can correctly load analyzer dependencies.
            if (PathUtilities.IsAbsolute(fullPath))
            {
                assemblyLoader.AddDependencyLocation(fullPath);
            }
        }

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
        {
            return _diagnosticAnalyzers.GetExtensionsForAllLanguages();
        }

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
        {
            return _diagnosticAnalyzers.GetExtensions(language);
        }

        public override string FullPath
        {
            get
            {
                return _fullPath;
            }
        }

        public override string Display
        {
            get
            {
                if (_lazyDisplay == null)
                {
                    InitializeDisplayAndId();
                }

                return _lazyDisplay;
            }
        }

        public override object Id
        {
            get
            {
                if (_lazyIdentity == null)
                {
                    InitializeDisplayAndId();
                }

                return _lazyIdentity;
            }
        }

        private void InitializeDisplayAndId()
        {
            try
            {
                // AssemblyName.GetAssemblyName(path) is not available on CoreCLR.
                // Use our metadata reader to do the equivalent thing.
                using (var reader = new PEReader(FileUtilities.OpenRead(_fullPath)))
                {
                    var metadataReader = reader.GetMetadataReader();
                    var assemblyIdentity = metadataReader.ReadAssemblyIdentityOrThrow();
                    _lazyDisplay = assemblyIdentity.Name;
                    _lazyIdentity = assemblyIdentity;
                }
            }
            catch
            {
                _lazyDisplay = Path.GetFileNameWithoutExtension(_fullPath);
                _lazyIdentity = _lazyDisplay;
            }
        }

        /// <summary>
        /// Adds the <see cref="ImmutableArray{T}"/> of <see cref="DiagnosticAnalyzer"/> defined in this assembly reference of given <paramref name="language"/>.
        /// </summary>
        internal void AddAnalyzers(ImmutableArray<DiagnosticAnalyzer>.Builder builder, string language)
        {
            _diagnosticAnalyzers.AddExtensions(builder, language);
        }

        private static AnalyzerLoadFailureEventArgs CreateAnalyzerFailedArgs(Exception e, string typeNameOpt = null)
        {
            // unwrap:
            e = (e as TargetInvocationException) ?? e;

            // remove all line breaks from the exception message
            string message = e.Message.Replace("\r", "").Replace("\n", "");

            var errorCode = (typeNameOpt != null) ?
                AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToCreateAnalyzer :
                AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToLoadAnalyzer;

            return new AnalyzerLoadFailureEventArgs(errorCode, message, e, typeNameOpt);
        }

        internal ImmutableDictionary<string, ImmutableHashSet<string>> GetAnalyzerTypeNameMap()
        {
            return _diagnosticAnalyzers.GetExtensionTypeNameMap();
        }

        /// <summary>
        /// Opens the analyzer dll with the metadata reader and builds a map of language -> analyzer type names.
        /// </summary>
        /// <exception cref="BadImageFormatException">The PE image format is invalid.</exception>
        /// <exception cref="IOException">IO error reading the metadata.</exception>
        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/30449")]
        private static ImmutableDictionary<string, ImmutableHashSet<string>> GetAnalyzerTypeNameMap(string fullPath, AttributePredicate attributePredicate)
        {
            using (var assembly = AssemblyMetadata.CreateFromFile(fullPath))
            {
                // This is longer than strictly necessary to avoid thrashing the GC with string allocations
                // in the call to GetFullyQualifiedTypeNames. Specifically, this checks for the presence of
                // supported languages prior to creating the type names.
                var typeNameMap = from module in assembly.GetModules()
                                  from typeDefHandle in module.MetadataReader.TypeDefinitions
                                  let typeDef = module.MetadataReader.GetTypeDefinition(typeDefHandle)
                                  let supportedLanguages = GetSupportedLanguages(typeDef, module.Module, attributePredicate)
                                  where supportedLanguages.Any()
                                  let typeName = GetFullyQualifiedTypeName(typeDef, module.Module)
                                  from supportedLanguage in supportedLanguages
                                  group typeName by supportedLanguage;

                return typeNameMap.ToImmutableDictionary(g => g.Key, g => g.ToImmutableHashSet());
            }
        }

        private static IEnumerable<string> GetSupportedLanguages(TypeDefinition typeDef, PEModule peModule, AttributePredicate attributePredicate)
        {
            var attributeLanguagesList = from customAttrHandle in typeDef.GetCustomAttributes()
                                         where attributePredicate(peModule, customAttrHandle)
                                         let attributeSupportedLanguages = GetSupportedLanguages(peModule, customAttrHandle)
                                         where attributeSupportedLanguages != null
                                         select attributeSupportedLanguages;

            return attributeLanguagesList.SelectMany(x => x);
        }

        private static IEnumerable<string> GetSupportedLanguages(PEModule peModule, CustomAttributeHandle customAttrHandle)
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
                    string firstLanguageName;
                    if (!PEModule.CrackStringInAttributeValue(out firstLanguageName, ref argsReader))
                    {
                        return SpecializedCollections.EmptyEnumerable<string>();
                    }

                    ImmutableArray<string> additionalLanguageNames;
                    if (PEModule.CrackStringArrayInAttributeValue(out additionalLanguageNames, ref argsReader))
                    {
                        if (additionalLanguageNames.Length == 0)
                        {
                            return SpecializedCollections.SingletonEnumerable(firstLanguageName);
                        }

                        return additionalLanguageNames.Insert(0, firstLanguageName);
                    }
                }
            }

            return SpecializedCollections.EmptyEnumerable<string>();
        }

        private static bool IsDiagnosticAnalyzerAttribute(PEModule peModule, CustomAttributeHandle customAttrHandle)
        {
            EntityHandle ctor;
            return peModule.IsTargetAttribute(customAttrHandle, s_diagnosticAnalyzerAttributeNamespace, nameof(DiagnosticAnalyzerAttribute), out ctor);
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

        private sealed class Extensions<TExtension> where TExtension : class
        {
            private readonly AnalyzerFileReference _reference;
            private readonly AttributePredicate _attributePredicate;
            private ImmutableArray<TExtension> _lazyAllExtensions;
            private ImmutableDictionary<string, ImmutableArray<TExtension>> _lazyExtensionsPerLanguage;
            private ImmutableDictionary<string, ImmutableHashSet<string>> _lazyExtensionTypeNameMap;

            internal Extensions(AnalyzerFileReference reference, AttributePredicate attributePredicate)
            {
                _reference = reference;
                _attributePredicate = attributePredicate;
                _lazyAllExtensions = default(ImmutableArray<TExtension>);
                _lazyExtensionsPerLanguage = ImmutableDictionary<string, ImmutableArray<TExtension>>.Empty;
            }

            internal ImmutableArray<TExtension> GetExtensionsForAllLanguages()
            {
                if (_lazyAllExtensions.IsDefault)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyAllExtensions, CreateExtensionsForAllLanguages(this));
                }

                return _lazyAllExtensions;
            }

            private static ImmutableArray<TExtension> CreateExtensionsForAllLanguages(Extensions<TExtension> extensions)
            {
                // Get all analyzers in the assembly.
                var map = ImmutableDictionary.CreateBuilder<string, ImmutableArray<TExtension>>();
                extensions.AddExtensions(map);

                var builder = ImmutableArray.CreateBuilder<TExtension>();
                foreach (var analyzers in map.Values)
                {
                    builder.AddRange(analyzers);
                }

                return builder.ToImmutable();
            }

            internal ImmutableArray<TExtension> GetExtensions(string language)
            {
                if (string.IsNullOrEmpty(language))
                {
                    throw new ArgumentException("language");
                }

                return ImmutableInterlocked.GetOrAdd(ref _lazyExtensionsPerLanguage, language, CreateLanguageSpecificExtensions, this);
            }

            private static ImmutableArray<TExtension> CreateLanguageSpecificExtensions(string language, Extensions<TExtension> extensions)
            {
                // Get all analyzers in the assembly for the given language.
                var builder = ImmutableArray.CreateBuilder<TExtension>();
                extensions.AddExtensions(builder, language);
                return builder.ToImmutable();
            }

            internal ImmutableDictionary<string, ImmutableHashSet<string>> GetExtensionTypeNameMap()
            {
                if (_lazyExtensionTypeNameMap == null)
                {
                    var analyzerTypeNameMap = GetAnalyzerTypeNameMap(_reference._fullPath, _attributePredicate);
                    Interlocked.CompareExchange(ref _lazyExtensionTypeNameMap, analyzerTypeNameMap, null);
                }

                return _lazyExtensionTypeNameMap;
            }

            internal void AddExtensions(ImmutableDictionary<string, ImmutableArray<TExtension>>.Builder builder)
            {
                ImmutableDictionary<string, ImmutableHashSet<string>> analyzerTypeNameMap;
                Assembly analyzerAssembly;

                try
                {
                    analyzerTypeNameMap = GetExtensionTypeNameMap();
                    if (analyzerTypeNameMap.Count == 0)
                    {
                        return;
                    }

                    analyzerAssembly = _reference.GetAssembly();
                }
                catch (Exception e)
                {
                    _reference.AnalyzerLoadFailed?.Invoke(_reference, CreateAnalyzerFailedArgs(e));
                    return;
                }

                var initialCount = builder.Count;
                var reportedError = false;

                // Add language specific analyzers.
                foreach (var (language, _) in analyzerTypeNameMap)
                {
                    if (language == null)
                    {
                        continue;
                    }

                    var analyzers = GetLanguageSpecificAnalyzers(analyzerAssembly, analyzerTypeNameMap, language, ref reportedError);
                    builder.Add(language, analyzers);
                }

                // If there were types with the attribute but weren't an analyzer, generate a diagnostic.
                // If we've reported errors already while trying to instantiate types, don't complain that there are no analyzers.
                if (builder.Count == initialCount && !reportedError)
                {
                    _reference.AnalyzerLoadFailed?.Invoke(_reference, new AnalyzerLoadFailureEventArgs(AnalyzerLoadFailureEventArgs.FailureErrorCode.NoAnalyzers, CodeAnalysisResources.NoAnalyzersFound));
                }
            }

            internal void AddExtensions(ImmutableArray<TExtension>.Builder builder, string language)
            {
                ImmutableDictionary<string, ImmutableHashSet<string>> analyzerTypeNameMap;
                Assembly analyzerAssembly;

                try
                {
                    analyzerTypeNameMap = GetExtensionTypeNameMap();

                    // If there are no analyzers, don't load the assembly at all.
                    if (!analyzerTypeNameMap.ContainsKey(language))
                    {
                        return;
                    }

                    analyzerAssembly = _reference.GetAssembly();
                    if (analyzerAssembly == null)
                    {
                        // This can be null if NoOpAnalyzerAssemblyLoader is used.
                        return;
                    }
                }
                catch (Exception e)
                {
                    _reference.AnalyzerLoadFailed?.Invoke(_reference, CreateAnalyzerFailedArgs(e));
                    return;
                }

                var initialCount = builder.Count;
                var reportedError = false;

                // Add language specific analyzers.
                var analyzers = GetLanguageSpecificAnalyzers(analyzerAssembly, analyzerTypeNameMap, language, ref reportedError);
                builder.AddRange(analyzers);

                // If there were types with the attribute but weren't an analyzer, generate a diagnostic.
                // If we've reported errors already while trying to instantiate types, don't complain that there are no analyzers.
                if (builder.Count == initialCount && !reportedError)
                {
                    _reference.AnalyzerLoadFailed?.Invoke(_reference, new AnalyzerLoadFailureEventArgs(AnalyzerLoadFailureEventArgs.FailureErrorCode.NoAnalyzers, CodeAnalysisResources.NoAnalyzersFound));
                }
            }

            private ImmutableArray<TExtension> GetLanguageSpecificAnalyzers(Assembly analyzerAssembly, ImmutableDictionary<string, ImmutableHashSet<string>> analyzerTypeNameMap, string language, ref bool reportedError)
            {
                ImmutableHashSet<string> languageSpecificAnalyzerTypeNames;
                if (!analyzerTypeNameMap.TryGetValue(language, out languageSpecificAnalyzerTypeNames))
                {
                    return ImmutableArray<TExtension>.Empty;
                }
                return this.GetAnalyzersForTypeNames(analyzerAssembly, languageSpecificAnalyzerTypeNames, ref reportedError);
            }

            private ImmutableArray<TExtension> GetAnalyzersForTypeNames(Assembly analyzerAssembly, IEnumerable<string> analyzerTypeNames, ref bool reportedError)
            {
                var analyzers = ImmutableArray.CreateBuilder<TExtension>();

                // Given the type names, get the actual System.Type and try to create an instance of the type through reflection.
                foreach (var typeName in analyzerTypeNames)
                {
                    Type type;
                    try
                    {
                        // TODO: Once we move to CoreCLR we should just call GetType(typeName, throwOnError: true, ignoreCase: false) directly.
                        // For now we fall back to reflection shim in order to report good error message (type load exception).
                        type = analyzerAssembly.GetType(typeName, throwOnError: true, ignoreCase: false);
                    }
                    catch (Exception e)
                    {
                        _reference.AnalyzerLoadFailed?.Invoke(_reference, CreateAnalyzerFailedArgs(e, typeName));
                        reportedError = true;
                        continue;
                    }

                    Debug.Assert(type != null);

                    TExtension analyzer;
                    try
                    {
                        analyzer = Activator.CreateInstance(type) as TExtension;
                    }
                    catch (Exception e)
                    {
                        _reference.AnalyzerLoadFailed?.Invoke(_reference, CreateAnalyzerFailedArgs(e, typeName));
                        reportedError = true;
                        continue;
                    }

                    if (analyzer != null)
                    {
                        analyzers.Add(analyzer);
                    }
                }

                return analyzers.ToImmutable();
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AnalyzerFileReference);
        }

        public bool Equals(AnalyzerReference other)
        {
            if (other != null)
            {
                return other is { Display: this.Display, FullPath: this.FullPath };
            }

            return base.Equals(other);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Display, this.FullPath.GetHashCode());
        }

        public Assembly GetAssembly()
        {
            if (_lazyAssembly == null)
            {
                _lazyAssembly = _assemblyLoader.LoadFromPath(_fullPath);
            }

            return _lazyAssembly;
        }
    }
}
