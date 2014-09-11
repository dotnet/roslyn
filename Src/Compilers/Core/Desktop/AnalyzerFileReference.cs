// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
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
    /// During this time the file is open and its content is read-only.
    /// 
    /// If you need to manage the lifetime of the anayzer reference (and the file stream) explicitly use <see cref="AnalyzerImageReference"/>.
    /// </remarks>
    public sealed partial class AnalyzerFileReference : AnalyzerReference, IEquatable<AnalyzerReference>
    {
        private readonly string fullPath;
        private readonly Func<string, Assembly> getAssembly;

        private string lazyDisplayName;
        private ImmutableArray<IDiagnosticAnalyzer> lazyAllAnalyzers;
        private ImmutableArray<IDiagnosticAnalyzer> lazyLanguageAgnosticAnalyzers;
        private ImmutableDictionary<string, ImmutableArray<IDiagnosticAnalyzer>> lazyAnalyzersPerLanguage;
        private Assembly lazyAssembly;
        private static string diagnosticNamespaceName = string.Format("{0}.{1}.{2}", nameof(Microsoft), nameof(CodeAnalysis), nameof(Diagnostics));
        private ImmutableDictionary<string, ImmutableHashSet<string>> lazyAnalyzerTypeNameMap;

        public event EventHandler<AnalyzerLoadFailureEventArgs> AnalyzerLoadFailed;

        /// <summary>
        /// Fired when an <see cref="Assembly"/> referred to by an <see cref="AnalyzerFileReference"/>
        /// (or a dependent <see cref="Assembly"/>) is loaded.
        /// </summary>
        public static event EventHandler<AnalyzerAssemblyLoadEventArgs> AssemblyLoad;
        
        /// <summary>
        /// Maps from one assembly back to the assembly that requested it, if known.
        /// </summary>
        public static string TryGetRequestingAssemblyPath(string assemblyPath)
        {
            return InMemoryAssemblyLoader.TryGetRequestingAssembly(assemblyPath);
        }

        /// <summary>
        /// Creates an AnalyzerFileReference with the given <paramref name="fullPath"/>.
        /// </summary>
        /// <param name="fullPath">Full path of the analyzer assembly.</param>
        /// <param name="getAssembly">An optional assembly loader to override the default assembly load mechanism.</param>
        public AnalyzerFileReference(string fullPath, Func<string, Assembly> getAssembly = null)
        {
            if (fullPath == null)
            {
                throw new ArgumentNullException("fullPath");
            }

            // TODO: remove full path normalization
            CompilerPathUtilities.RequireAbsolutePath(fullPath, "fullPath");

            try
            {
                this.fullPath = Path.GetFullPath(fullPath);
            }
            catch (Exception e)
            {
                throw new ArgumentException(e.Message, "fullPath");
            }

            this.lazyAllAnalyzers = default(ImmutableArray<IDiagnosticAnalyzer>);
            this.lazyLanguageAgnosticAnalyzers = default(ImmutableArray<IDiagnosticAnalyzer>);
            this.lazyAnalyzersPerLanguage = ImmutableDictionary<string, ImmutableArray<IDiagnosticAnalyzer>>.Empty;
            this.getAssembly = getAssembly;
        }

        public override ImmutableArray<IDiagnosticAnalyzer> GetAnalyzersForAllLanguages()
        {
            if (lazyAllAnalyzers.IsDefault)
            {
                var allAnalyzers = MetadataCache.GetOrCreateAnalyzersFromFile(this);
                ImmutableInterlocked.InterlockedInitialize(ref this.lazyAllAnalyzers, allAnalyzers);
            }

            return lazyAllAnalyzers;
        }

        public override ImmutableArray<IDiagnosticAnalyzer> GetAnalyzers(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                throw new ArgumentException("language");
            }

            return ImmutableInterlocked.GetOrAdd(ref this.lazyAnalyzersPerLanguage, language, CreateLanguageSpecificAnalyzers, this);
            }

        private static ImmutableArray<IDiagnosticAnalyzer> CreateLanguageSpecificAnalyzers(string language, AnalyzerFileReference @this)
            {
            return MetadataCache.GetOrCreateAnalyzersFromFile(@this, language);
        }

        public override string FullPath
        {
            get
            {
                return this.fullPath;
            }
        }

        public override string Display
        {
            get
            {
                if (lazyDisplayName == null)
                {
                    try
                    {
                        var assemblyName = AssemblyName.GetAssemblyName(this.FullPath);
                        lazyDisplayName = assemblyName.Name;
                        return lazyDisplayName;
                    }
                    catch (ArgumentException)
                    { }
                    catch (BadImageFormatException)
                    { }
                    catch (SecurityException)
                    { }
                    catch (FileLoadException)
                    { }
                    catch (FileNotFoundException)
                    { }

                    lazyDisplayName = base.Display;
                }

                return lazyDisplayName;
            }
        }

        private ImmutableArray<IDiagnosticAnalyzer> GetLanguageAgnosticAnalyzers(ImmutableDictionary<string, ImmutableHashSet<string>> analyzerTypeNameMap, Assembly analyzerAssembly)
        {
            if (this.lazyLanguageAgnosticAnalyzers.IsDefault)
            {
                ImmutableHashSet<string> analyzerTypeNames;
                ImmutableArray<IDiagnosticAnalyzer> analyzers;
                if (!analyzerTypeNameMap.TryGetValue(string.Empty, out analyzerTypeNames))
                {
                    analyzers = ImmutableArray<IDiagnosticAnalyzer>.Empty;
                }
                else
                {
                    analyzers = GetAnalyzersForTypeNames(analyzerAssembly, analyzerTypeNames).ToImmutableArrayOrEmpty();
                }

                ImmutableInterlocked.InterlockedInitialize(ref this.lazyLanguageAgnosticAnalyzers, analyzers);
            }

            return this.lazyLanguageAgnosticAnalyzers;
        }

        /// <summary>
        /// Adds the <see cref="ImmutableArray{T}"/> of <see cref="IDiagnosticAnalyzer"/> defined in this assembly reference.
        /// </summary>
        internal void AddAnalyzers(ImmutableArray<IDiagnosticAnalyzer>.Builder builder, string languageOpt = null)
        {
            ImmutableDictionary<string, ImmutableHashSet<string>> analyzerTypeNameMap;
            Assembly analyzerAssembly = null;
            
            try
            {
                analyzerTypeNameMap = GetAnalyzerTypeNameMap();

                bool hasAnyAnalyzerTypes;
                if (languageOpt == null)
                {
                    hasAnyAnalyzerTypes = analyzerTypeNameMap.Any();
                }
                else
                {
                    hasAnyAnalyzerTypes = analyzerTypeNameMap.ContainsKey(string.Empty) || analyzerTypeNameMap.ContainsKey(languageOpt);
                }

                // If there are no analyzers, don't load the assembly at all.
                if (!hasAnyAnalyzerTypes)
                {
                    this.AnalyzerLoadFailed?.Invoke(this, new AnalyzerLoadFailureEventArgs(AnalyzerLoadFailureEventArgs.FailureErrorCode.NoAnalyzers, null, null));
                    return;
                }

                analyzerAssembly = GetAssembly();
            }
            catch (Exception e) if (e is FileLoadException || e is FileNotFoundException || e is BadImageFormatException ||
                                    e is SecurityException || e is ArgumentException || e is PathTooLongException)
            {
                this.AnalyzerLoadFailed?.Invoke(this, new AnalyzerLoadFailureEventArgs(AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToLoadAnalyzer, e, null));
                return;
            }

            var initialCount = builder.Count;
            
            // Add language agnostic analyzers.
            var languageAgnosticAnalyzers = this.GetLanguageAgnosticAnalyzers(analyzerTypeNameMap, analyzerAssembly);
            builder.AddRange(languageAgnosticAnalyzers);

            // Add language specific analyzers.
            var languageSpecificAnalyzerTypeNames = GetLanguageSpecificAnalyzerTypeNames(analyzerTypeNameMap, languageOpt);
            var languageSpecificAnalyzers = this.GetAnalyzersForTypeNames(analyzerAssembly, languageSpecificAnalyzerTypeNames);
            builder.AddRange(languageSpecificAnalyzers);

            // If there were types with the attribute but weren't an analyzer, generate a diagnostic.
            if (builder.Count == initialCount)
            {
                this.AnalyzerLoadFailed?.Invoke(this, new AnalyzerLoadFailureEventArgs(AnalyzerLoadFailureEventArgs.FailureErrorCode.NoAnalyzers, null, null));
            }
        }

        private static IEnumerable<string> GetLanguageSpecificAnalyzerTypeNames(ImmutableDictionary<string, ImmutableHashSet<string>> analyzerTypeNameMap, string languageOpt)
        {
            HashSet<string> languageSpecificAnalyzerTypeNames = new HashSet<string>();
            if (languageOpt == null)
            {
                // If the user didn't ask for a specific language then return all language specific analyzers.
                languageSpecificAnalyzerTypeNames.AddAll(analyzerTypeNameMap.SelectMany(kvp => kvp.Key != string.Empty ? kvp.Value : SpecializedCollections.EmptyEnumerable<string>()));
            }
            else
            {
                // Add the analyzers for the specific language.
                if (analyzerTypeNameMap.ContainsKey(languageOpt))
                {
                    languageSpecificAnalyzerTypeNames.AddAll(analyzerTypeNameMap[languageOpt]);
                }
            }

            return languageSpecificAnalyzerTypeNames;
        }

        private IEnumerable<IDiagnosticAnalyzer> GetAnalyzersForTypeNames(Assembly analyzerAssembly, IEnumerable<string> analyzerTypeNames)
        {
            // Given the type names, get the actual System.Type and try to create an instance of the type through reflection.
            foreach (var typeName in analyzerTypeNames)
            {
                IDiagnosticAnalyzer analyzer = null;
                try
                {
                    var type = analyzerAssembly.GetType(typeName, throwOnError: true);
                    if (type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IDiagnosticAnalyzer)))
                    {
                        analyzer = (IDiagnosticAnalyzer)Activator.CreateInstance(type);
                    }
                }
                catch (Exception e) if (e is TypeLoadException || e is BadImageFormatException || e is FileNotFoundException || e is FileLoadException ||
                                        e is ArgumentException || e is NotSupportedException || e is TargetInvocationException || e is MemberAccessException)
                {
                    this.AnalyzerLoadFailed?.Invoke(this, new AnalyzerLoadFailureEventArgs(AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToCreateAnalyzer, e, typeName));
                    analyzer = null;
                }

                if (analyzer != null)
                {
                    yield return analyzer;
                }
            }
        }

        internal ImmutableDictionary<string, ImmutableHashSet<string>> GetAnalyzerTypeNameMap()
        {
            if (this.lazyAnalyzerTypeNameMap == null)
            {
                var analyzerTypeNameMap = GetAnalyzerTypeNameMap(this.fullPath);
                Interlocked.CompareExchange(ref this.lazyAnalyzerTypeNameMap, analyzerTypeNameMap, null);
            }

            return this.lazyAnalyzerTypeNameMap;
        }

        /// <summary>
        /// Opens the analyzer dll with the metadata reader and builds a map of language -> analyzer type names.
        /// </summary>
        private static ImmutableDictionary<string, ImmutableHashSet<string>> GetAnalyzerTypeNameMap(string fullPath)
        {
            using (var assembly = MetadataFileFactory.CreateAssembly(fullPath))
            {
                var typeNameMap = from module in assembly.Modules
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
            var supportedLanguages =  from customAttrHandle in typeDef.GetCustomAttributes()
                                      where IsDiagnosticAnalyzerAttribute(peModule, customAttrHandle)
                                      let supportedLangauge = GetSupportedLanguage(peModule, customAttrHandle)
                                      where supportedLangauge != null
                                      select supportedLangauge;

            // If the analyzer claims it is language agnostic but also claims it supports some specific language, ignore the specific languages.
            if (supportedLanguages.Contains(string.Empty))
                    {
                return SpecializedCollections.SingletonEnumerable(string.Empty);
            }
            else
            {
                return supportedLanguages;
            }
        }
                        
        private static string GetSupportedLanguage(PEModule peModule, CustomAttributeHandle customAttrHandle)
                            {
                                // The DiagnosticAnalyzerAttribute has two constructors:
                                // 1. Paramterless - means that the analyzer is applicable to any language. 
                                // 2. Single string parameter specifying the language.
                                // Parse the argument blob to extract these two cases.
                                BlobReader argsReader = peModule.GetMemoryReaderOrThrow(peModule.GetCustomAttributeValueOrThrow(customAttrHandle));

                                // Single string parameter
                                if (argsReader.Length > 4)
                                {
                                    // check prolog
                                    if (argsReader.ReadByte() == 1 && argsReader.ReadByte() == 0)
                                    {
                                        string languageName;
                                        if (PEModule.CrackStringInAttributeValue(out languageName, ref argsReader))
                                        {
                        return languageName;
                                        }
                                    }
                                }
                                // otherwise the attribute is applicable to all languages.
                                else
                                {
                return string.Empty;
                            }

            return null;
            }

        private static bool IsDiagnosticAnalyzerAttribute(PEModule peModule, CustomAttributeHandle customAttrHandle)
        {
            Handle ctor;
            return peModule.IsTargetAttribute(customAttrHandle, diagnosticNamespaceName, nameof(DiagnosticAnalyzerAttribute), out ctor);
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
                return GetFullyQualifiedTypeName(declaringTypeDef, peModule) +  "+" + peModule.MetadataReader.GetString(typeDef.Name);
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
            if (lazyAssembly == null)
            {
                var assembly = getAssembly != null ?
                    getAssembly(fullPath) :
                    InMemoryAssemblyLoader.Load(fullPath);
                Interlocked.CompareExchange(ref this.lazyAssembly, assembly, null);
            }

            return lazyAssembly;
        }
    }
}
