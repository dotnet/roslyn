// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit.NoPia;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    internal abstract class CommonPEModuleBuilder : Cci.IUnit, Cci.IModuleReference
    {
        internal readonly DebugDocumentsBuilder DebugDocumentsBuilder;
        internal readonly IEnumerable<ResourceDescription> ManifestResources;
        internal readonly Cci.ModulePropertiesForSerialization SerializationProperties;
        internal readonly OutputKind OutputKind;
        internal IEnumerable<Cci.IWin32Resource> Win32Resources;
        internal Cci.ResourceSection Win32ResourceSection;
        internal Stream SourceLinkStreamOpt;

        internal Cci.IMethodReference PEEntryPoint;
        internal Cci.IMethodReference DebugEntryPoint;

        private readonly ConcurrentDictionary<IMethodSymbol, Cci.IMethodBody> _methodBodyMap;
        private readonly TokenMap<Cci.IReference> _referencesInILMap = new TokenMap<Cci.IReference>();
        private readonly ItemTokenMap<string> _stringsInILMap = new ItemTokenMap<string>();
        private readonly ItemTokenMap<Cci.DebugSourceDocument> _sourceDocumentsInILMap = new ItemTokenMap<Cci.DebugSourceDocument>();

        private ImmutableArray<Cci.AssemblyReferenceAlias> _lazyAssemblyReferenceAliases;
        private ImmutableArray<Cci.ManagedResource> _lazyManagedResources;
        private IEnumerable<EmbeddedText> _embeddedTexts = SpecializedCollections.EmptyEnumerable<EmbeddedText>();

        // Only set when running tests to allow realized IL for a given method to be looked up by method.
        internal ConcurrentDictionary<IMethodSymbol, CompilationTestData.MethodData> TestData { get; private set; }

        internal readonly DebugInformationFormat DebugInformationFormat;
        internal readonly HashAlgorithmName PdbChecksumAlgorithm;

        public CommonPEModuleBuilder(
            IEnumerable<ResourceDescription> manifestResources,
            EmitOptions emitOptions,
            OutputKind outputKind,
            Cci.ModulePropertiesForSerialization serializationProperties,
            Compilation compilation)
        {
            Debug.Assert(manifestResources != null);
            Debug.Assert(serializationProperties != null);
            Debug.Assert(compilation != null);

            ManifestResources = manifestResources;
            DebugDocumentsBuilder = new DebugDocumentsBuilder(compilation.Options.SourceReferenceResolver, compilation.IsCaseSensitive);
            OutputKind = outputKind;
            SerializationProperties = serializationProperties;
            _methodBodyMap = new ConcurrentDictionary<IMethodSymbol, Cci.IMethodBody>(ReferenceEqualityComparer.Instance);
            DebugInformationFormat = emitOptions.DebugInformationFormat;
            PdbChecksumAlgorithm = emitOptions.PdbChecksumAlgorithm;
        }

        /// <summary>
        /// EnC generation.
        /// </summary>
        public abstract int CurrentGenerationOrdinal { get; }

        /// <summary>
        /// If this module represents an assembly, name of the assembly used in AssemblyDef table. Otherwise name of the module same as <see cref="ModuleName"/>.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Name of the module. Used in ModuleDef table.
        /// </summary>
        internal abstract string ModuleName { get; }

        internal abstract Cci.IAssemblyReference Translate(IAssemblySymbol symbol, DiagnosticBag diagnostics);
        internal abstract Cci.ITypeReference Translate(ITypeSymbol symbol, SyntaxNode syntaxOpt, DiagnosticBag diagnostics);
        internal abstract Cci.IMethodReference Translate(IMethodSymbol symbol, DiagnosticBag diagnostics, bool needDeclaration);
        internal abstract bool SupportsPrivateImplClass { get; }
        internal abstract Compilation CommonCompilation { get; }
        internal abstract IModuleSymbol CommonSourceModule { get; }
        internal abstract IAssemblySymbol CommonCorLibrary { get; }
        internal abstract CommonModuleCompilationState CommonModuleCompilationState { get; }
        internal abstract void CompilationFinished();
        internal abstract ImmutableDictionary<ISymbol, ImmutableArray<ISymbol>> GetAllSynthesizedMembers();
        internal abstract CommonEmbeddedTypesManager CommonEmbeddedTypesManagerOpt { get; }
        internal abstract Cci.ITypeReference EncTranslateType(ITypeSymbol type, DiagnosticBag diagnostics);
        public abstract IEnumerable<Cci.ICustomAttribute> GetSourceAssemblyAttributes(bool isRefAssembly);
        public abstract IEnumerable<Cci.SecurityAttribute> GetSourceAssemblySecurityAttributes();
        public abstract IEnumerable<Cci.ICustomAttribute> GetSourceModuleAttributes();
        internal abstract Cci.ICustomAttribute SynthesizeAttribute(WellKnownMember attributeConstructor);

        /// <summary>
        /// Public types defined in other modules making up this assembly and to which other assemblies may refer to via this assembly
        /// followed by types forwarded to another assembly.
        /// </summary>
        public abstract ImmutableArray<Cci.ExportedType> GetExportedTypes(DiagnosticBag diagnostics);

        /// <summary>
        /// Used to distinguish which style to pick while writing native PDB information.
        /// </summary>
        /// <remarks>
        /// The PDB content for custom debug information is different between Visual Basic and CSharp.
        /// E.g. C# always includes a CustomMetadata Header (MD2) that contains the namespace scope counts, where 
        /// as VB only outputs namespace imports into the namespace scopes. 
        /// C# defines forwards in that header, VB includes them into the scopes list.
        /// 
        /// Currently the compiler doesn't allow mixing C# and VB method bodies. Thus this flag can be per module.
        /// It is possible to move this flag to per-method basis but native PDB CDI forwarding would need to be adjusted accordingly.
        /// </remarks>
        public abstract bool GenerateVisualBasicStylePdb { get; }

        /// <summary>
        /// Linked assembly names to be stored to native PDB (VB only).
        /// </summary>
        public abstract IEnumerable<string> LinkedAssembliesDebugInfo { get; }

        /// <summary>
        /// Project level imports (VB only, TODO: C# scripts).
        /// </summary>
        public abstract ImmutableArray<Cci.UsedNamespaceOrType> GetImports();

        /// <summary>
        /// Default namespace (VB only).
        /// </summary>
        public abstract string DefaultNamespace { get; }

        protected abstract Cci.IAssemblyReference GetCorLibraryReferenceToEmit(EmitContext context);
        protected abstract IEnumerable<Cci.IAssemblyReference> GetAssemblyReferencesFromAddedModules(DiagnosticBag diagnostics);
        protected abstract void AddEmbeddedResourcesFromAddedModules(ArrayBuilder<Cci.ManagedResource> builder, DiagnosticBag diagnostics);
        public abstract Cci.ITypeReference GetPlatformType(Cci.PlatformType platformType, EmitContext context);
        public abstract bool IsPlatformType(Cci.ITypeReference typeRef, Cci.PlatformType platformType);

        public abstract IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypeDefinitions(EmitContext context);

        public IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypeDefinitionsCore(EmitContext context)
        {
            foreach (var typeDef in GetAdditionalTopLevelTypeDefinitions(context))
            {
                yield return typeDef;
            }

            foreach (var typeDef in GetEmbeddedTypeDefinitions(context))
            {
                yield return typeDef;
            }

            foreach (var typeDef in GetTopLevelSourceTypeDefinitions(context))
            {
                yield return typeDef;
            }
        }

        /// <summary>
        /// Additional top-level types injected by the Expression Evaluators.
        /// </summary>
        public abstract IEnumerable<Cci.INamespaceTypeDefinition> GetAdditionalTopLevelTypeDefinitions(EmitContext context);

        /// <summary>
        /// Anonymous types defined in the compilation.
        /// </summary>
        public abstract IEnumerable<Cci.INamespaceTypeDefinition> GetAnonymousTypeDefinitions(EmitContext context);

        /// <summary>
        /// Top-level embedded types (e.g. attribute types that are not present in referenced assemblies).
        /// </summary>
        public abstract IEnumerable<Cci.INamespaceTypeDefinition> GetEmbeddedTypeDefinitions(EmitContext context);

        /// <summary>
        /// Top-level named types defined in source.
        /// </summary>
        public abstract IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelSourceTypeDefinitions(EmitContext context);

        /// <summary>
        /// A list of the files that constitute the assembly. Empty for netmodule. These are not the source language files that may have been
        /// used to compile the assembly, but the files that contain constituent modules of a multi-module assembly as well
        /// as any external resources. It corresponds to the File table of the .NET assembly file format.
        /// </summary>
        public abstract IEnumerable<Cci.IFileReference> GetFiles(EmitContext context);

        /// <summary>
        /// Builds symbol definition to location map used for emitting token -> location info
        /// into PDB to be consumed by WinMdExp.exe tool (only applicable for /t:winmdobj)
        /// </summary>
        public abstract MultiDictionary<Cci.DebugSourceDocument, Cci.DefinitionWithLocation> GetSymbolToLocationMap();

        /// <summary>
        /// Number of debug documents in the module. 
        /// Used to determine capacities of lists and indices when emitting debug info.
        /// </summary>
        public int DebugDocumentCount => DebugDocumentsBuilder.DebugDocumentCount;

        public void Dispatch(Cci.MetadataVisitor visitor) => visitor.Visit(this);

        IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context) => SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            Debug.Assert(ReferenceEquals(context.Module, this));
            return this;
        }

        public abstract ISourceAssemblySymbolInternal SourceAssemblyOpt { get; }

        /// <summary>
        /// An approximate number of method definitions that can
        /// provide a basis for approximating the capacities of
        /// various databases used during Emit.
        /// </summary>
        public int HintNumberOfMethodDefinitions
            // Try to guess at the size of tables to prevent re-allocation. The method body
            // map is pretty close, but unfortunately it tends to undercount. x1.5 seems like
            // a healthy amount of room based on compiling Roslyn.
            => (int)(_methodBodyMap.Count * 1.5);

        internal Cci.IMethodBody GetMethodBody(IMethodSymbol methodSymbol)
        {
            Debug.Assert(methodSymbol.ContainingModule == CommonSourceModule);
            Debug.Assert(methodSymbol.IsDefinition);
            Debug.Assert(methodSymbol.PartialDefinitionPart == null); // Must be definition.

            Cci.IMethodBody body;

            if (_methodBodyMap.TryGetValue(methodSymbol, out body))
            {
                return body;
            }

            return null;
        }

        public void SetMethodBody(IMethodSymbol methodSymbol, Cci.IMethodBody body)
        {
            Debug.Assert(methodSymbol.ContainingModule == CommonSourceModule);
            Debug.Assert(methodSymbol.IsDefinition);
            Debug.Assert(methodSymbol.PartialDefinitionPart == null); // Must be definition.
            Debug.Assert(body == null || (object)methodSymbol == body.MethodDefinition);

            _methodBodyMap.Add(methodSymbol, body);
        }

        internal void SetPEEntryPoint(IMethodSymbol method, DiagnosticBag diagnostics)
        {
            Debug.Assert(method == null || IsSourceDefinition(method));
            Debug.Assert(OutputKind.IsApplication());

            PEEntryPoint = Translate(method, diagnostics, needDeclaration: true);
        }

        internal void SetDebugEntryPoint(IMethodSymbol method, DiagnosticBag diagnostics)
        {
            Debug.Assert(method == null || IsSourceDefinition(method));

            DebugEntryPoint = Translate(method, diagnostics, needDeclaration: true);
        }

        private bool IsSourceDefinition(IMethodSymbol method)
        {
            return method.ContainingModule == CommonSourceModule && method.IsDefinition;
        }

        /// <summary>
        /// CorLibrary assembly referenced by this module.
        /// </summary>
        public Cci.IAssemblyReference GetCorLibrary(EmitContext context)
        {
            return Translate(CommonCorLibrary, context.Diagnostics);
        }

        public Cci.IAssemblyReference GetContainingAssembly(EmitContext context)
        {
            return OutputKind == OutputKind.NetModule ? null : (Cci.IAssemblyReference)this;
        }

        /// <summary>
        /// Returns User Strings referenced from the IL in the module. 
        /// </summary>
        public IEnumerable<string> GetStrings()
        {
            return _stringsInILMap.GetAllItems();
        }

        public uint GetFakeSymbolTokenForIL(Cci.IReference symbol, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            bool added;
            uint token = _referencesInILMap.GetOrAddTokenFor(symbol, out added);
            if (added)
            {
                ReferenceDependencyWalker.VisitReference(symbol, new EmitContext(this, syntaxNode, diagnostics, metadataOnly: false, includePrivateMembers: true));
            }
            return token;
        }

        public uint GetSourceDocumentIndexForIL(Cci.DebugSourceDocument document)
        {
            return _sourceDocumentsInILMap.GetOrAddTokenFor(document);
        }

        internal Cci.DebugSourceDocument GetSourceDocumentFromIndex(uint token)
        {
            return _sourceDocumentsInILMap.GetItem(token);
        }

        public Cci.IReference GetReferenceFromToken(uint token)
        {
            return _referencesInILMap.GetItem(token);
        }

        public uint GetFakeStringTokenForIL(string str)
        {
            return _stringsInILMap.GetOrAddTokenFor(str);
        }

        public string GetStringFromToken(uint token)
        {
            return _stringsInILMap.GetItem(token);
        }

        public IEnumerable<Cci.IReference> ReferencesInIL(out int count)
        {
            return _referencesInILMap.GetAllItemsAndCount(out count);
        }

        /// <summary>
        /// Assembly reference aliases (C# only).
        /// </summary>
        public ImmutableArray<Cci.AssemblyReferenceAlias> GetAssemblyReferenceAliases(EmitContext context)
        {
            if (_lazyAssemblyReferenceAliases.IsDefault)
            {
                ImmutableInterlocked.InterlockedCompareExchange(ref _lazyAssemblyReferenceAliases, CalculateAssemblyReferenceAliases(context), default(ImmutableArray<Cci.AssemblyReferenceAlias>));
            }

            return _lazyAssemblyReferenceAliases;
        }

        private ImmutableArray<Cci.AssemblyReferenceAlias> CalculateAssemblyReferenceAliases(EmitContext context)
        {
            var result = ArrayBuilder<Cci.AssemblyReferenceAlias>.GetInstance();

            foreach (var assemblyAndAliases in CommonCompilation.GetBoundReferenceManager().GetReferencedAssemblyAliases())
            {
                var assembly = assemblyAndAliases.Item1;
                var aliases = assemblyAndAliases.Item2;

                for (int i = 0; i < aliases.Length; i++)
                {
                    string alias = aliases[i];

                    // filter out duplicates and global aliases:
                    if (alias != MetadataReferenceProperties.GlobalAlias && aliases.IndexOf(alias, 0, i) < 0)
                    {
                        result.Add(new Cci.AssemblyReferenceAlias(alias, Translate(assembly, context.Diagnostics)));
                    }
                }
            }

            return result.ToImmutableAndFree();
        }

        public IEnumerable<Cci.IAssemblyReference> GetAssemblyReferences(EmitContext context)
        {
            Cci.IAssemblyReference corLibrary = GetCorLibraryReferenceToEmit(context);

            // Only add Cor Library reference explicitly, PeWriter will add
            // other references implicitly on as needed basis.
            if (corLibrary != null)
            {
                yield return corLibrary;
            }

            if (OutputKind != OutputKind.NetModule)
            {
                // Explicitly add references from added modules
                foreach (var aRef in GetAssemblyReferencesFromAddedModules(context.Diagnostics))
                {
                    yield return aRef;
                }
            }
        }

        public ImmutableArray<Cci.ManagedResource> GetResources(EmitContext context)
        {
            if (context.IsRefAssembly)
            {
                // Manifest resources are not included in ref assemblies
                // Ref assemblies don't support added modules
                return ImmutableArray<Cci.ManagedResource>.Empty;
            }

            if (_lazyManagedResources.IsDefault)
            {
                var builder = ArrayBuilder<Cci.ManagedResource>.GetInstance();

                foreach (ResourceDescription r in ManifestResources)
                {
                    builder.Add(r.ToManagedResource(this));
                }

                if (OutputKind != OutputKind.NetModule)
                {
                    // Explicitly add resources from added modules
                    AddEmbeddedResourcesFromAddedModules(builder, context.Diagnostics);
                }

                _lazyManagedResources = builder.ToImmutableAndFree();
            }

            return _lazyManagedResources;
        }

        public IEnumerable<EmbeddedText> EmbeddedTexts
        {
            get
            {
                return _embeddedTexts;
            }
            set
            {
                Debug.Assert(value != null);
                _embeddedTexts = value;
            }
        }

        internal bool SaveTestData => TestData != null;

        internal void SetMethodTestData(IMethodSymbol method, ILBuilder builder)
        {
            TestData.Add(method, new CompilationTestData.MethodData(builder, method));
        }

        internal void SetMethodTestData(ConcurrentDictionary<IMethodSymbol, CompilationTestData.MethodData> methods)
        {
            Debug.Assert(TestData == null);
            TestData = methods;
        }
    }

    /// <summary>
    /// Common base class for C# and VB PE module builder.
    /// </summary>
    internal abstract class PEModuleBuilder<TCompilation, TSourceModuleSymbol, TAssemblySymbol, TTypeSymbol, TNamedTypeSymbol, TMethodSymbol, TSyntaxNode, TEmbeddedTypesManager, TModuleCompilationState> : CommonPEModuleBuilder, ITokenDeferral
        where TCompilation : Compilation
        where TSourceModuleSymbol : class, IModuleSymbol
        where TAssemblySymbol : class, IAssemblySymbol
        where TTypeSymbol : class
        where TNamedTypeSymbol : class, TTypeSymbol, INamedTypeSymbol, Cci.INamespaceTypeDefinition
        where TMethodSymbol : class, Cci.IMethodDefinition
        where TSyntaxNode : SyntaxNode
        where TEmbeddedTypesManager : CommonEmbeddedTypesManager
        where TModuleCompilationState : ModuleCompilationState<TNamedTypeSymbol, TMethodSymbol>
    {
        private readonly Cci.RootModuleType _rootModuleType = new Cci.RootModuleType();

        internal readonly TSourceModuleSymbol SourceModule;
        internal readonly TCompilation Compilation;

        private PrivateImplementationDetails _privateImplementationDetails;
        private ArrayMethods _lazyArrayMethods;
        private HashSet<string> _namesOfTopLevelTypes;

        internal readonly TModuleCompilationState CompilationState;

        public abstract TEmbeddedTypesManager EmbeddedTypesManagerOpt { get; }

        protected PEModuleBuilder(
            TCompilation compilation,
            TSourceModuleSymbol sourceModule,
            Cci.ModulePropertiesForSerialization serializationProperties,
            IEnumerable<ResourceDescription> manifestResources,
            OutputKind outputKind,
            EmitOptions emitOptions,
            TModuleCompilationState compilationState)
            : base(manifestResources, emitOptions, outputKind, serializationProperties, compilation)
        {
            Debug.Assert(sourceModule != null);
            Debug.Assert(serializationProperties != null);

            Compilation = compilation;
            SourceModule = sourceModule;
            this.CompilationState = compilationState;
        }

        internal sealed override void CompilationFinished()
        {
            this.CompilationState.Freeze();
        }

        internal override IAssemblySymbol CommonCorLibrary => CorLibrary;
        internal abstract TAssemblySymbol CorLibrary { get; }

        internal abstract Cci.INamedTypeReference GetSystemType(TSyntaxNode syntaxOpt, DiagnosticBag diagnostics);
        internal abstract Cci.INamedTypeReference GetSpecialType(SpecialType specialType, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics);

        internal sealed override Cci.ITypeReference EncTranslateType(ITypeSymbol type, DiagnosticBag diagnostics)
        {
            return EncTranslateLocalVariableType((TTypeSymbol)type, diagnostics);
        }

        internal virtual Cci.ITypeReference EncTranslateLocalVariableType(TTypeSymbol type, DiagnosticBag diagnostics)
        {
            return Translate(type, null, diagnostics);
        }

        protected bool HaveDeterminedTopLevelTypes
        {
            get { return _namesOfTopLevelTypes != null; }
        }

        protected bool ContainsTopLevelType(string fullEmittedName)
        {
            return _namesOfTopLevelTypes.Contains(fullEmittedName);
        }

        /// <summary>
        /// Returns all top-level (not nested) types defined in the module. 
        /// </summary>
        public override IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypeDefinitions(EmitContext context)
        {
            Cci.TypeReferenceIndexer typeReferenceIndexer = null;
            HashSet<string> names;

            // First time through, we need to collect emitted names of all top level types.
            if (_namesOfTopLevelTypes == null)
            {
                names = new HashSet<string>();
            }
            else
            {
                names = null;
            }

            // First time through, we need to push things through TypeReferenceIndexer
            // to make sure we collect all to be embedded NoPia types and members.
            if (EmbeddedTypesManagerOpt != null && !EmbeddedTypesManagerOpt.IsFrozen)
            {
                typeReferenceIndexer = new Cci.TypeReferenceIndexer(context);
                Debug.Assert(names != null);

                // Run this reference indexer on the assembly- and module-level attributes first.
                // We'll run it on all other types below.
                // The purpose is to trigger Translate on all types.
                Dispatch(typeReferenceIndexer);
            }

            AddTopLevelType(names, _rootModuleType);
            VisitTopLevelType(typeReferenceIndexer, _rootModuleType);
            yield return _rootModuleType;

            foreach (var typeDef in GetAnonymousTypeDefinitions(context))
            {
                AddTopLevelType(names, typeDef);
                VisitTopLevelType(typeReferenceIndexer, typeDef);
                yield return typeDef;
            }

            foreach (var typeDef in GetTopLevelTypeDefinitionsCore(context))
            {
                AddTopLevelType(names, typeDef);
                VisitTopLevelType(typeReferenceIndexer, typeDef);
                yield return typeDef;
            }

            var privateImpl = PrivateImplClass;
            if (privateImpl != null)
            {
                AddTopLevelType(names, privateImpl);
                VisitTopLevelType(typeReferenceIndexer, privateImpl);
                yield return privateImpl;
            }

            if (EmbeddedTypesManagerOpt != null)
            {
                foreach (var embedded in EmbeddedTypesManagerOpt.GetTypes(context.Diagnostics, names))
                {
                    AddTopLevelType(names, embedded);
                    yield return embedded;
                }
            }

            if (names != null)
            {
                Debug.Assert(_namesOfTopLevelTypes == null);
                _namesOfTopLevelTypes = names;
            }
        }

        public override IEnumerable<Cci.INamespaceTypeDefinition> GetAdditionalTopLevelTypeDefinitions(EmitContext context)
            => GetAdditionalTopLevelTypes(context.Diagnostics);

        public virtual ImmutableArray<TNamedTypeSymbol> GetAdditionalTopLevelTypes(DiagnosticBag diagnostics)
            => ImmutableArray<TNamedTypeSymbol>.Empty;

        public override IEnumerable<Cci.INamespaceTypeDefinition> GetEmbeddedTypeDefinitions(EmitContext context)
            => GetEmbeddedTypes(context.Diagnostics);

        public virtual ImmutableArray<TNamedTypeSymbol> GetEmbeddedTypes(DiagnosticBag diagnostics)
            => ImmutableArray<TNamedTypeSymbol>.Empty;

        internal abstract Cci.IAssemblyReference Translate(TAssemblySymbol symbol, DiagnosticBag diagnostics);
        internal abstract Cci.ITypeReference Translate(TTypeSymbol symbol, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics);
        internal abstract Cci.IMethodReference Translate(TMethodSymbol symbol, DiagnosticBag diagnostics, bool needDeclaration);

        internal sealed override Cci.IAssemblyReference Translate(IAssemblySymbol symbol, DiagnosticBag diagnostics)
        {
            return Translate((TAssemblySymbol)symbol, diagnostics);
        }

        internal sealed override Cci.ITypeReference Translate(ITypeSymbol symbol, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            return Translate((TTypeSymbol)symbol, (TSyntaxNode)syntaxNodeOpt, diagnostics);
        }

        internal sealed override Cci.IMethodReference Translate(IMethodSymbol symbol, DiagnosticBag diagnostics, bool needDeclaration)
        {
            return Translate((TMethodSymbol)symbol, diagnostics, needDeclaration);
        }

        internal sealed override IModuleSymbol CommonSourceModule => SourceModule;
        internal sealed override Compilation CommonCompilation => Compilation;
        internal sealed override CommonModuleCompilationState CommonModuleCompilationState => CompilationState;
        internal sealed override CommonEmbeddedTypesManager CommonEmbeddedTypesManagerOpt => EmbeddedTypesManagerOpt;

        internal MetadataConstant CreateConstant(
            TTypeSymbol type,
            object value,
            TSyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics)
        {
            return new MetadataConstant(Translate(type, syntaxNodeOpt, diagnostics), value);
        }

        private static void AddTopLevelType(HashSet<string> names, Cci.INamespaceTypeDefinition type)
        {
            names?.Add(MetadataHelpers.BuildQualifiedName(type.NamespaceName, Cci.MetadataWriter.GetMangledName(type)));
        }

        private static void VisitTopLevelType(Cci.TypeReferenceIndexer noPiaIndexer, Cci.INamespaceTypeDefinition type)
        {
            noPiaIndexer?.Visit((Cci.ITypeDefinition)type);
        }

        internal Cci.IFieldReference GetModuleVersionId(Cci.ITypeReference mvidType, TSyntaxNode syntaxOpt, DiagnosticBag diagnostics)
        {
            PrivateImplementationDetails details = GetPrivateImplClass(syntaxOpt, diagnostics);
            EnsurePrivateImplementationDetailsStaticConstructor(details, syntaxOpt, diagnostics);

            return details.GetModuleVersionId(mvidType);
        }

        internal Cci.IFieldReference GetInstrumentationPayloadRoot(int analysisKind, Cci.ITypeReference payloadType, TSyntaxNode syntaxOpt, DiagnosticBag diagnostics)
        {
            PrivateImplementationDetails details = GetPrivateImplClass(syntaxOpt, diagnostics);
            EnsurePrivateImplementationDetailsStaticConstructor(details, syntaxOpt, diagnostics);

            return details.GetOrAddInstrumentationPayloadRoot(analysisKind, payloadType);
        }

        private void EnsurePrivateImplementationDetailsStaticConstructor(PrivateImplementationDetails details, TSyntaxNode syntaxOpt, DiagnosticBag diagnostics)
        {
            if (details.GetMethod(WellKnownMemberNames.StaticConstructorName) == null)
            {
                details.TryAddSynthesizedMethod(CreatePrivateImplementationDetailsStaticConstructor(details, syntaxOpt, diagnostics));
            }
        }

        protected abstract Cci.IMethodDefinition CreatePrivateImplementationDetailsStaticConstructor(PrivateImplementationDetails details, TSyntaxNode syntaxOpt, DiagnosticBag diagnostics);

        #region Synthesized Members

        /// <summary>
        /// Captures the set of synthesized definitions that should be added to a type
        /// during emit process.
        /// </summary>
        private sealed class SynthesizedDefinitions
        {
            public ConcurrentQueue<Cci.INestedTypeDefinition> NestedTypes;
            public ConcurrentQueue<Cci.IMethodDefinition> Methods;
            public ConcurrentQueue<Cci.IPropertyDefinition> Properties;
            public ConcurrentQueue<Cci.IFieldDefinition> Fields;

            public ImmutableArray<ISymbol> GetAllMembers()
            {
                var builder = ArrayBuilder<ISymbol>.GetInstance();

                if (Fields != null)
                {
                    foreach (var field in Fields)
                    {
                        builder.Add((ISymbol)field);
                    }
                }

                if (Methods != null)
                {
                    foreach (var method in Methods)
                    {
                        builder.Add((ISymbol)method);
                    }
                }

                if (Properties != null)
                {
                    foreach (var property in Properties)
                    {
                        builder.Add((ISymbol)property);
                    }
                }

                if (NestedTypes != null)
                {
                    foreach (var type in NestedTypes)
                    {
                        builder.Add((ISymbol)type);
                    }
                }

                return builder.ToImmutableAndFree();
            }
        }

        private readonly ConcurrentDictionary<TNamedTypeSymbol, SynthesizedDefinitions> _synthesizedTypeMembers =
            new ConcurrentDictionary<TNamedTypeSymbol, SynthesizedDefinitions>(ReferenceEqualityComparer.Instance);

        private ConcurrentDictionary<INamespaceSymbol, ConcurrentQueue<INamespaceOrTypeSymbol>> _lazySynthesizedNamespaceMembers;

        internal abstract IEnumerable<Cci.INestedTypeDefinition> GetSynthesizedNestedTypes(TNamedTypeSymbol container);

        /// <summary>
        /// Returns null if there are no compiler generated types.
        /// </summary>
        public IEnumerable<Cci.INestedTypeDefinition> GetSynthesizedTypes(TNamedTypeSymbol container)
        {
            IEnumerable<Cci.INestedTypeDefinition> declareTypes = GetSynthesizedNestedTypes(container);
            IEnumerable<Cci.INestedTypeDefinition> compileEmitTypes = null;

            if (_synthesizedTypeMembers.TryGetValue(container, out var defs))
            {
                compileEmitTypes = defs.NestedTypes;
            }

            if (declareTypes == null)
            {
                return compileEmitTypes;
            }

            if (compileEmitTypes == null)
            {
                return declareTypes;
            }

            return declareTypes.Concat(compileEmitTypes);
        }

        private SynthesizedDefinitions GetOrAddSynthesizedDefinitions(TNamedTypeSymbol container)
        {
            Debug.Assert(container.IsDefinition);
            return _synthesizedTypeMembers.GetOrAdd(container, _ => new SynthesizedDefinitions());
        }

        public void AddSynthesizedDefinition(TNamedTypeSymbol container, Cci.IMethodDefinition method)
        {
            Debug.Assert(method != null);

            SynthesizedDefinitions defs = GetOrAddSynthesizedDefinitions(container);
            if (defs.Methods == null)
            {
                Interlocked.CompareExchange(ref defs.Methods, new ConcurrentQueue<Cci.IMethodDefinition>(), null);
            }

            defs.Methods.Enqueue(method);
        }

        public void AddSynthesizedDefinition(TNamedTypeSymbol container, Cci.IPropertyDefinition property)
        {
            Debug.Assert(property != null);

            SynthesizedDefinitions defs = GetOrAddSynthesizedDefinitions(container);
            if (defs.Properties == null)
            {
                Interlocked.CompareExchange(ref defs.Properties, new ConcurrentQueue<Cci.IPropertyDefinition>(), null);
            }

            defs.Properties.Enqueue(property);
        }

        public void AddSynthesizedDefinition(TNamedTypeSymbol container, Cci.IFieldDefinition field)
        {
            Debug.Assert(field != null);

            SynthesizedDefinitions defs = GetOrAddSynthesizedDefinitions(container);
            if (defs.Fields == null)
            {
                Interlocked.CompareExchange(ref defs.Fields, new ConcurrentQueue<Cci.IFieldDefinition>(), null);
            }

            defs.Fields.Enqueue(field);
        }

        public void AddSynthesizedDefinition(TNamedTypeSymbol container, Cci.INestedTypeDefinition nestedType)
        {
            Debug.Assert(nestedType != null);

            SynthesizedDefinitions defs = GetOrAddSynthesizedDefinitions(container);
            if (defs.NestedTypes == null)
            {
                Interlocked.CompareExchange(ref defs.NestedTypes, new ConcurrentQueue<Cci.INestedTypeDefinition>(), null);
            }

            defs.NestedTypes.Enqueue(nestedType);
        }

        public void AddSynthesizedDefinition(INamespaceSymbol container, INamespaceOrTypeSymbol typeOrNamespace)
        {
            Debug.Assert(typeOrNamespace != null);
            if (_lazySynthesizedNamespaceMembers == null)
            {
                Interlocked.CompareExchange(ref _lazySynthesizedNamespaceMembers, new ConcurrentDictionary<INamespaceSymbol, ConcurrentQueue<INamespaceOrTypeSymbol>>(), null);
            }

            _lazySynthesizedNamespaceMembers.GetOrAdd(container, _ => new ConcurrentQueue<INamespaceOrTypeSymbol>()).Enqueue(typeOrNamespace);
        }

        /// <summary>
        /// Returns null if there are no synthesized fields.
        /// </summary>
        public IEnumerable<Cci.IFieldDefinition> GetSynthesizedFields(TNamedTypeSymbol container)
            => _synthesizedTypeMembers.TryGetValue(container, out var defs) ? defs.Fields : null;

        /// <summary>
        /// Returns null if there are no synthesized properties.
        /// </summary>
        public IEnumerable<Cci.IPropertyDefinition> GetSynthesizedProperties(TNamedTypeSymbol container)
            => _synthesizedTypeMembers.TryGetValue(container, out var defs) ? defs.Properties : null;

        /// <summary>
        /// Returns null if there are no synthesized methods.
        /// </summary>
        public IEnumerable<Cci.IMethodDefinition> GetSynthesizedMethods(TNamedTypeSymbol container)
            => _synthesizedTypeMembers.TryGetValue(container, out var defs) ? defs.Methods : null;

        internal override ImmutableDictionary<ISymbol, ImmutableArray<ISymbol>> GetAllSynthesizedMembers()
        {
            var builder = ImmutableDictionary.CreateBuilder<ISymbol, ImmutableArray<ISymbol>>();

            foreach (var entry in _synthesizedTypeMembers)
            {
                builder.Add(entry.Key, entry.Value.GetAllMembers());
            }

            var namespaceMembers = _lazySynthesizedNamespaceMembers;
            if (namespaceMembers != null)
            {
                foreach (var entry in namespaceMembers)
                {
                    builder.Add(entry.Key, entry.Value.ToImmutableArray<ISymbol>());
                }
            }

            return builder.ToImmutable();
        }

        #endregion

        #region Token Mapping

        Cci.IFieldReference ITokenDeferral.GetFieldForData(ImmutableArray<byte> data, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            Debug.Assert(this.SupportsPrivateImplClass);

            var privateImpl = this.GetPrivateImplClass((TSyntaxNode)syntaxNode, diagnostics);

            // map a field to the block (that makes it addressable via a token)
            return privateImpl.CreateDataField(data);
        }

        public abstract Cci.IMethodReference GetInitArrayHelper();

        public ArrayMethods ArrayMethods
        {
            get
            {
                ArrayMethods result = _lazyArrayMethods;

                if (result == null)
                {
                    result = new ArrayMethods();

                    if (Interlocked.CompareExchange(ref _lazyArrayMethods, result, null) != null)
                    {
                        result = _lazyArrayMethods;
                    }
                }

                return result;
            }
        }

        #endregion

        #region Private Implementation Details Type

        internal PrivateImplementationDetails GetPrivateImplClass(TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            var result = _privateImplementationDetails;

            if ((result == null) && this.SupportsPrivateImplClass)
            {
                result = new PrivateImplementationDetails(
                        this,
                        this.SourceModule.Name,
                        Compilation.GetSubmissionSlotIndex(),
                        this.GetSpecialType(SpecialType.System_Object, syntaxNodeOpt, diagnostics),
                        this.GetSpecialType(SpecialType.System_ValueType, syntaxNodeOpt, diagnostics),
                        this.GetSpecialType(SpecialType.System_Byte, syntaxNodeOpt, diagnostics),
                        this.GetSpecialType(SpecialType.System_Int16, syntaxNodeOpt, diagnostics),
                        this.GetSpecialType(SpecialType.System_Int32, syntaxNodeOpt, diagnostics),
                        this.GetSpecialType(SpecialType.System_Int64, syntaxNodeOpt, diagnostics),
                        SynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));

                if (Interlocked.CompareExchange(ref _privateImplementationDetails, result, null) != null)
                {
                    result = _privateImplementationDetails;
                }
            }

            return result;
        }

        internal PrivateImplementationDetails PrivateImplClass
        {
            get { return _privateImplementationDetails; }
        }

        internal override bool SupportsPrivateImplClass
        {
            get { return true; }
        }

        #endregion

        public sealed override Cci.ITypeReference GetPlatformType(Cci.PlatformType platformType, EmitContext context)
        {
            Debug.Assert((object)this == context.Module);

            switch (platformType)
            {
                case Cci.PlatformType.SystemType:
                    return GetSystemType((TSyntaxNode)context.SyntaxNodeOpt, context.Diagnostics);

                default:
                    return GetSpecialType((SpecialType)platformType, (TSyntaxNode)context.SyntaxNodeOpt, context.Diagnostics);
            }
        }
    }
}
