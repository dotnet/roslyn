// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security;
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
    public sealed partial class AnalyzerFileReference : AnalyzerReference
    {
        private readonly string fullPath;
        private readonly Func<string, Assembly> getAssembly;
        private string displayName;
        private ImmutableArray<IDiagnosticAnalyzer>? lazyAnalyzers;
        private Assembly assembly;

        private Dictionary<string, HashSet<string>> analyzerTypeNameMap;

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

            lazyAnalyzers = null;
            this.getAssembly = getAssembly;
        }

        public override ImmutableArray<IDiagnosticAnalyzer> GetAnalyzers()
        {
            if (!lazyAnalyzers.HasValue)
            {
                lazyAnalyzers = MetadataCache.GetOrCreateAnalyzersFromFile(this);
            }

            return lazyAnalyzers.Value;
        }

        public override ImmutableArray<IDiagnosticAnalyzer> GetAnalyzersForLanguage(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                throw new ArgumentException("language");
            }

            if (!lazyAnalyzers.HasValue)
            {
                lazyAnalyzers = MetadataCache.GetOrCreateAnalyzersFromFile(this, language);
            }

            return lazyAnalyzers.Value;
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
                if (displayName == null)
                {
                    try
                    {
                        var assemblyName = AssemblyName.GetAssemblyName(this.FullPath);
                        displayName = assemblyName.Name;
                        return displayName;
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

                    displayName = base.Display;
                }

                return displayName;
            }
        }

        /// <summary>
        /// Adds the <see cref="ImmutableArray{T}"/> of <see cref="IDiagnosticAnalyzer"/> defined in this assembly reference.
        /// </summary>
        internal void AddAnalyzers(ImmutableArray<IDiagnosticAnalyzer>.Builder builder, string languageOpt = null)
        {
            // We handle loading of analyzer assemblies ourselves. This allows us to avoid locking the assembly
            // file on disk.
            List<Type> types = new List<Type>();
            Assembly analyzerAssembly = null;
            HashSet<string> analyzerTypeNames = new HashSet<string>();

            try
            {
                if (analyzerTypeNameMap == null)
                {
                    analyzerTypeNameMap = GetAnalyzerTypeNameMap();

                    // If there are language agnostic analyzers, then instantiate them but only do it once.
                    if (analyzerTypeNameMap.ContainsKey(string.Empty))
                    {
                        analyzerTypeNames.AddAll(analyzerTypeNameMap[string.Empty]);
                    }
                }

                // If the user didn't ask for a specific language then return all analyzers.
                if (languageOpt == null)
                {
                    // The type names of language agnostic analyzer have already been added. Add all other language analyzers.
                    analyzerTypeNames.AddAll(analyzerTypeNameMap.SelectMany(kvp => kvp.Key != string.Empty ? kvp.Value : SpecializedCollections.EmptyEnumerable<string>()));
                }
                else
                {
                    // Add the analyzers for the specific language.
                    if (analyzerTypeNameMap.ContainsKey(languageOpt))
                    {
                        analyzerTypeNames.AddAll(analyzerTypeNameMap[languageOpt]);
                    }
                }

                // If there are no analyzers, don't load the assembly at all.
                if (analyzerTypeNames.IsEmpty())
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

            // Now given the type names, get the actual System.Type and try to create an instance of the type through reflection.
            bool hasAnalyzers = false;
            foreach (var typeName in analyzerTypeNames)
            {
                try
                {
                    var type = analyzerAssembly.GetType(typeName, throwOnError: true);
                    if (type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IDiagnosticAnalyzer)))
                    {
                        hasAnalyzers = true;
                        builder.Add((IDiagnosticAnalyzer)Activator.CreateInstance(type));
                    }
                }
                catch (Exception e) if (e is TypeLoadException || e is BadImageFormatException || e is FileNotFoundException || e is FileLoadException ||
                                        e is ArgumentException || e is NotSupportedException || e is TargetInvocationException || e is MemberAccessException)
                {
                    this.AnalyzerLoadFailed?.Invoke(this, new AnalyzerLoadFailureEventArgs(AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToCreateAnalyzer, e, typeName));
                }
            }

            // If there were types with the attribute but weren't an analyzer.
            if (!hasAnalyzers)
            {
                this.AnalyzerLoadFailed?.Invoke(this, new AnalyzerLoadFailureEventArgs(AnalyzerLoadFailureEventArgs.FailureErrorCode.NoAnalyzers, null, null));
            }
        }

        /// <summary>
        /// Opens the analyzer dll with the metadata reader and builds a map of language -> analyzer type names.
        /// </summary>
        internal Dictionary<string, HashSet<string>> GetAnalyzerTypeNameMap()
        {
            var typeNameMap = new Dictionary<string, HashSet<string>>();
            var diagnosticNamespaceName = string.Format("{0}.{1}.{2}", nameof(Microsoft), nameof(CodeAnalysis), nameof(Diagnostics));

            using (var assembly = MetadataFileFactory.CreateAssembly(this.fullPath))
            {
                foreach (var module in assembly.Modules)
                {
                    foreach (var typeDefHandle in module.MetadataReader.TypeDefinitions)
                    {
                        var typeDef = module.MetadataReader.GetTypeDefinition(typeDefHandle);
                        var customAttrs = typeDef.GetCustomAttributes();

                        foreach (var customAttrHandle in customAttrs)
                        {
                            var peModule = module.Module;
                            Handle ctor;
                            if (peModule.IsTargetAttribute(customAttrHandle, diagnosticNamespaceName, nameof(DiagnosticAnalyzerAttribute), out ctor))
                            {
                                string typeName = GetFullyQualifiedTypeName(typeDef, peModule);

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
                                            if (!typeNameMap.ContainsKey(languageName))
                                            {
                                                typeNameMap.Add(languageName, new HashSet<string>());
                                            }
                                            typeNameMap[languageName].Add(typeName);
                                        }
                                    }
                                }
                                // otherwise the attribute is applicable to all languages.
                                else
                                {
                                    if (!typeNameMap.ContainsKey(string.Empty))
                                    {
                                        typeNameMap.Add(string.Empty, new HashSet<string>());
                                    }
                                    typeNameMap[string.Empty].Add(typeName);
                                }
                            }
                        }
                    }
                }
            }

            return typeNameMap;
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
                return string.Concat(GetFullyQualifiedTypeName(declaringTypeDef, peModule), "+", peModule.MetadataReader.GetString(typeDef.Name));
            }
        }

        public override bool Equals(object obj)
        {
            AnalyzerFileReference other = obj as AnalyzerFileReference;

            if (other != null)
            {
                return other.Display == this.Display &&
                       other.FullPath == this.FullPath &&
                       other.IsUnresolved == this.IsUnresolved;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Display,
                        Hash.Combine(this.FullPath, this.IsUnresolved.GetHashCode()));
        }

        public Assembly GetAssembly()
        {
            if (assembly == null)
            {
                assembly = getAssembly != null ?
                    getAssembly(fullPath) :
                    InMemoryAssemblyLoader.Load(fullPath);
            }

            return assembly;
        }
    }
}
