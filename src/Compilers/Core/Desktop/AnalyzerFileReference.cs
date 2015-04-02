// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable CS0618
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security;
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
    /// If you need to manage the lifetime of the anayzer reference (and the file stream) explicitly use <see cref="AnalyzerImageReference"/>.
    /// </remarks>
    public sealed partial class AnalyzerFileReference : AnalyzerReference, IEquatable<AnalyzerReference>
    {
        private readonly string _fullPath;
        private readonly Func<string, Assembly> _getAssembly;

        private string _lazyDisplayName;
        private ImmutableArray<DiagnosticAnalyzer> _lazyAllAnalyzers;
        private ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> _lazyAnalyzersPerLanguage;
        private Assembly _lazyAssembly;
        private static string s_diagnosticNamespaceName = string.Format("{0}.{1}.{2}", nameof(Microsoft), nameof(CodeAnalysis), nameof(Diagnostics));
        private ImmutableDictionary<string, ImmutableHashSet<string>> _lazyAnalyzerTypeNameMap;

        public event EventHandler<AnalyzerLoadFailureEventArgs> AnalyzerLoadFailed;

        /// <summary>
        /// Creates an AnalyzerFileReference with the given <paramref name="fullPath"/>.
        /// </summary>
        /// <param name="fullPath">Full path of the analyzer assembly.</param>
        /// <param name="getAssembly">Function for loading the analyzer assembly</param>
        public AnalyzerFileReference(string fullPath, Func<string, Assembly> getAssembly)
        {
            if (fullPath == null)
            {
                throw new ArgumentNullException(nameof(fullPath));
            }

            // TODO: remove full path normalization
            CompilerPathUtilities.RequireAbsolutePath(fullPath, nameof(fullPath));

            try
            {
                _fullPath = Path.GetFullPath(fullPath);
            }
            catch (Exception e)
            {
                throw new ArgumentException(e.Message, "fullPath");
            }

            _lazyAllAnalyzers = default(ImmutableArray<DiagnosticAnalyzer>);
            _lazyAnalyzersPerLanguage = ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>>.Empty;
            _getAssembly = getAssembly;
        }

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
        {
            if (_lazyAllAnalyzers.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(ref _lazyAllAnalyzers, CreateAnalyzersForAllLanguages(this));
            }

            return _lazyAllAnalyzers;
        }

        private static ImmutableArray<DiagnosticAnalyzer> CreateAnalyzersForAllLanguages(AnalyzerFileReference reference)
        {
            // Get all analyzers in the assembly.
            var map = ImmutableDictionary.CreateBuilder<string, ImmutableArray<DiagnosticAnalyzer>>();
            reference.AddAnalyzers(map);

            var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            foreach (var analyzers in map.Values)
            {
                builder.AddRange(analyzers);
            }

            return builder.ToImmutable();
        }

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                throw new ArgumentException("language");
            }

            return ImmutableInterlocked.GetOrAdd(ref _lazyAnalyzersPerLanguage, language, CreateLanguageSpecificAnalyzers, this);
        }

        private static ImmutableArray<DiagnosticAnalyzer> CreateLanguageSpecificAnalyzers(string langauge, AnalyzerFileReference reference)
        {
            // Get all analyzers in the assembly for the given language.
            var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            reference.AddAnalyzers(builder, langauge);
            return builder.ToImmutable();
        }

        public override string FullPath
        {
            get
            {
                return _fullPath;
            }
        }

        private static string GetAssemblyNameFromPath(string path)
        {
            // AssemblyName.GetAssemblyName(path) is not available on CoreCLR.
            // Use our metadata reader to do the equivalent thing.
            using (var reader = new PEReader(FileUtilities.OpenRead(path)))
            {
                var metadataReader = reader.GetMetadataReader();
                var assemblyDefinition = metadataReader.GetAssemblyDefinition();
                return metadataReader.GetString(assemblyDefinition.Name);
            }
        }

        public override string Display
        {
            get
            {
                if (_lazyDisplayName == null)
                {
                    try
                    {
                        _lazyDisplayName = GetAssemblyNameFromPath(_fullPath);
                    }
                    catch (Exception)
                    { }

                    _lazyDisplayName = _lazyDisplayName ?? Path.GetFileName(this.FullPath);
                }

                return _lazyDisplayName;
            }
        }

        /// <summary>
        /// Adds the <see cref="ImmutableDictionary{TKey, TValue}"/> of <see cref="ImmutableArray{T}"/> of <see cref="DiagnosticAnalyzer"/> 
        /// for all languages defined in this assembly reference.
        /// </summary>
        internal void AddAnalyzers(ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>>.Builder builder)
        {
            ImmutableDictionary<string, ImmutableHashSet<string>> analyzerTypeNameMap;
            Assembly analyzerAssembly = null;

            try
            {
                analyzerTypeNameMap = GetAnalyzerTypeNameMap();
                if (analyzerTypeNameMap.Count == 0)
                {
                    return;
                }

                analyzerAssembly = GetAssembly();
            }
            catch (Exception e)
            {
                this.AnalyzerLoadFailed?.Invoke(this, new AnalyzerLoadFailureEventArgs(AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToLoadAnalyzer, e, null));
                return;
            }

            var initialCount = builder.Count;
            var reportedError = false;

            // Add language specific analyzers.
            foreach (var language in analyzerTypeNameMap.Keys)
            {
                if (language == null)
                {
                    continue;
                }

                var analyzers = GetLanguageSpecificAnalyzers(analyzerAssembly, analyzerTypeNameMap, language, ref reportedError);
                builder.Add(language, analyzers.ToImmutableArray());
            }

            // If there were types with the attribute but weren't an analyzer, generate a diagnostic.
            // If we've reported errors already while trying to instantiate types, don't complain that there are no analyzers.
            if (builder.Count == initialCount && !reportedError)
            {
                this.AnalyzerLoadFailed?.Invoke(this, new AnalyzerLoadFailureEventArgs(AnalyzerLoadFailureEventArgs.FailureErrorCode.NoAnalyzers, null, null));
            }
        }

        /// <summary>
        /// Adds the <see cref="ImmutableArray{T}"/> of <see cref="DiagnosticAnalyzer"/> defined in this assembly reference of given <paramref name="language"/>.
        /// </summary>
        internal void AddAnalyzers(ImmutableArray<DiagnosticAnalyzer>.Builder builder, string language)
        {
            ImmutableDictionary<string, ImmutableHashSet<string>> analyzerTypeNameMap;
            Assembly analyzerAssembly = null;

            try
            {
                analyzerTypeNameMap = GetAnalyzerTypeNameMap();

                // If there are no analyzers, don't load the assembly at all.
                if (!analyzerTypeNameMap.ContainsKey(language))
                {
                    return;
                }

                analyzerAssembly = GetAssembly();
            }
            catch (Exception e)
            {
                this.AnalyzerLoadFailed?.Invoke(this, new AnalyzerLoadFailureEventArgs(AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToLoadAnalyzer, e, null));
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
                this.AnalyzerLoadFailed?.Invoke(this, new AnalyzerLoadFailureEventArgs(AnalyzerLoadFailureEventArgs.FailureErrorCode.NoAnalyzers, null, null));
            }
        }

        private IEnumerable<DiagnosticAnalyzer> GetLanguageSpecificAnalyzers(Assembly analyzerAssembly, ImmutableDictionary<string, ImmutableHashSet<string>> analyzerTypeNameMap, string language, ref bool reportedError)
        {
            var languageSpecificAnalyzerTypeNames = GetLanguageSpecificAnalyzerTypeNames(analyzerTypeNameMap, language);
            return this.GetAnalyzersForTypeNames(analyzerAssembly, languageSpecificAnalyzerTypeNames, ref reportedError);
        }

        private static IEnumerable<string> GetLanguageSpecificAnalyzerTypeNames(ImmutableDictionary<string, ImmutableHashSet<string>> analyzerTypeNameMap, string language)
        {
            ImmutableHashSet<string> analyzerTypeNames;
            if (analyzerTypeNameMap.TryGetValue(language, out analyzerTypeNames))
            {
                return analyzerTypeNames;
            }

            return SpecializedCollections.EmptyEnumerable<string>();
        }

        private IEnumerable<DiagnosticAnalyzer> GetAnalyzersForTypeNames(Assembly analyzerAssembly, IEnumerable<string> analyzerTypeNames, ref bool reportedError)
        {
            var analyzers = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();

            // Given the type names, get the actual System.Type and try to create an instance of the type through reflection.
            foreach (var typeName in analyzerTypeNames)
            {
                DiagnosticAnalyzer analyzer = null;
                try
                {
                    var type = analyzerAssembly.GetType(typeName, throwOnError: true, ignoreCase: false);
                    if (DerivesFromDiagnosticAnalyzer(type))
                    {
                        analyzer = (DiagnosticAnalyzer)Activator.CreateInstance(type);
                    }
                }
                catch (Exception e) when (e is TypeLoadException || e is BadImageFormatException || e is FileNotFoundException || e is FileLoadException ||
                                          e is ArgumentException || e is NotSupportedException || e is TargetInvocationException || e is MemberAccessException)
                {
                    this.AnalyzerLoadFailed?.Invoke(this, new AnalyzerLoadFailureEventArgs(AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToCreateAnalyzer, e, typeName));
                    analyzer = null;
                    reportedError = true;
                }

                if (analyzer != null)
                {
                    analyzers.Add(analyzer);
                }
            }

            return analyzers.ToImmutable();
        }

        internal ImmutableDictionary<string, ImmutableHashSet<string>> GetAnalyzerTypeNameMap()
        {
            if (_lazyAnalyzerTypeNameMap == null)
            {
                var analyzerTypeNameMap = GetAnalyzerTypeNameMap(_fullPath);
                Interlocked.CompareExchange(ref _lazyAnalyzerTypeNameMap, analyzerTypeNameMap, null);
            }

            return _lazyAnalyzerTypeNameMap;
        }

        /// <summary>
        /// Opens the analyzer dll with the metadata reader and builds a map of language -> analyzer type names.
        /// </summary>
        /// <exception cref="BadImageFormatException">The PE image format is invalid.</exception>
        /// <exception cref="IOException">IO error reading the metadata.</exception>
        private static ImmutableDictionary<string, ImmutableHashSet<string>> GetAnalyzerTypeNameMap(string fullPath)
        {
            using (var assembly = AssemblyMetadata.CreateFromFile(fullPath))
            {
                var typeNameMap = from module in assembly.GetModules()
                                  from typeDefHandle in module.MetadataReader.TypeDefinitions
                                  let typeDef = module.MetadataReader.GetTypeDefinition(typeDefHandle)
                                  let typeName = GetFullyQualifiedTypeName(typeDef, module.Module)
                                  from supportedLanguage in GetSupportedLanguages(typeDef, module.Module)
                                  group typeName by supportedLanguage;

                return typeNameMap.ToImmutableDictionary(g => g.Key, g => g.ToImmutableHashSet());
            }
        }

        private static IEnumerable<string> GetSupportedLanguages(TypeDefinition typeDef, PEModule peModule)
        {
            var attributeLanguagesList = from customAttrHandle in typeDef.GetCustomAttributes()
                                         where IsDiagnosticAnalyzerAttribute(peModule, customAttrHandle)
                                         let attributeSupportedLanguages = GetSupportedLanguages(peModule, customAttrHandle)
                                         where attributeSupportedLanguages != null
                                         select attributeSupportedLanguages;

            IEnumerable<string> supportedLanguages = SpecializedCollections.EmptyEnumerable<string>();
            foreach (IEnumerable<string> languages in attributeLanguagesList)
            {
                supportedLanguages = supportedLanguages.Concat(languages);
            }

            return supportedLanguages;
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
            Handle ctor;
            return peModule.IsTargetAttribute(customAttrHandle, s_diagnosticNamespaceName, nameof(DiagnosticAnalyzerAttribute), out ctor);
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

        private static bool DerivesFromDiagnosticAnalyzer(Type type)
        {
            return type.GetTypeInfo().IsSubclassOf(typeof(DiagnosticAnalyzer));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AnalyzerFileReference);
        }

        public bool Equals(AnalyzerReference other)
        {
            if (other != null)
            {
                return other.Display == this.Display &&
                       other.FullPath == this.FullPath &&
                       other.IsUnresolved == this.IsUnresolved;
            }

            return base.Equals(other);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Display,
                        Hash.Combine(this.FullPath, this.IsUnresolved.GetHashCode()));
        }

        public Assembly GetAssembly()
        {
            if (_lazyAssembly == null)
            {
                var assembly = _getAssembly(_fullPath);
                Interlocked.CompareExchange(ref _lazyAssembly, assembly, null);
            }

            return _lazyAssembly;
        }
    }
}
