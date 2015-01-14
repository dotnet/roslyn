// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#pragma warning disable CS0618

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
        private ImmutableArray<DiagnosticAnalyzer> lazyAllAnalyzers;
        private ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> lazyAnalyzersPerLanguage;
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

            this.lazyAllAnalyzers = default(ImmutableArray<DiagnosticAnalyzer>);
            this.lazyAnalyzersPerLanguage = ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>>.Empty;
            this.getAssembly = getAssembly;
        }

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
        {
            if (lazyAllAnalyzers.IsDefault)
            {
                var allAnalyzers = MetadataCache.GetOrCreateAnalyzersFromFile(this);
                ImmutableInterlocked.InterlockedInitialize(ref this.lazyAllAnalyzers, allAnalyzers);
            }

            return lazyAllAnalyzers;
        }

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                throw new ArgumentException("language");
            }

            return ImmutableInterlocked.GetOrAdd(ref this.lazyAnalyzersPerLanguage, language, CreateLanguageSpecificAnalyzers, this);
        }

        private static ImmutableArray<DiagnosticAnalyzer> CreateLanguageSpecificAnalyzers(string language, AnalyzerFileReference @this)
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

                    lazyDisplayName = Path.GetFileName(this.FullPath);
                }

                return lazyDisplayName;
            }
        }

        /// <summary>
        /// Adds the <see cref="ImmutableArray{T}"/> of <see cref="DiagnosticAnalyzer"/> defined in this assembly reference.
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
                    this.AnalyzerLoadFailed?.Invoke(this, new AnalyzerLoadFailureEventArgs(AnalyzerLoadFailureEventArgs.FailureErrorCode.NoAnalyzers, null, null));
                    return;
                }

                analyzerAssembly = GetAssembly();
            }
            catch (Exception e) when (e is IOException || e is BadImageFormatException || e is SecurityException || e is ArgumentException)
            {
                this.AnalyzerLoadFailed?.Invoke(this, new AnalyzerLoadFailureEventArgs(AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToLoadAnalyzer, e, null));
                return;
            }

            var initialCount = builder.Count;
            var reportedError = false;

            // Add language specific analyzers.
            var languageSpecificAnalyzerTypeNames = GetLanguageSpecificAnalyzerTypeNames(analyzerTypeNameMap, language);
            var languageSpecificAnalyzers = this.GetAnalyzersForTypeNames(analyzerAssembly, languageSpecificAnalyzerTypeNames, ref reportedError);
            builder.AddRange(languageSpecificAnalyzers);

            // If there were types with the attribute but weren't an analyzer, generate a diagnostic.
            // If we've reported errors already while trying to instantiate types, don't complain that there are no analyzers.
            if (builder.Count == initialCount && !reportedError)
            {
                this.AnalyzerLoadFailed?.Invoke(this, new AnalyzerLoadFailureEventArgs(AnalyzerLoadFailureEventArgs.FailureErrorCode.NoAnalyzers, null, null));
            }
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
                    var type = analyzerAssembly.GetType(typeName, throwOnError: true);
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
                return GetFullyQualifiedTypeName(declaringTypeDef, peModule) + "+" + peModule.MetadataReader.GetString(typeDef.Name);
            }
        }

        private static bool DerivesFromDiagnosticAnalyzer(Type type)
        {
            return type.IsSubclassOf(typeof(DiagnosticAnalyzer));
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
