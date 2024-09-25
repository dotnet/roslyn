// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.Emit
{
    internal abstract class CommonPEModuleBuilder : Cci.IUnit, Cci.IModuleReference
    {
        internal readonly DebugDocumentsBuilder DebugDocumentsBuilder;
        internal readonly IEnumerable<ResourceDescription> ManifestResources;
        internal readonly Cci.ModulePropertiesForSerialization SerializationProperties;
        internal readonly OutputKind OutputKind;
        internal Stream? RawWin32Resources;
        internal IEnumerable<Cci.IWin32Resource>? Win32Resources;
        internal Cci.ResourceSection? Win32ResourceSection;
        internal Stream? SourceLinkStreamOpt;

        internal Cci.IMethodReference? PEEntryPoint;
        internal Cci.IMethodReference? DebugEntryPoint;

        private readonly ConcurrentDictionary<IMethodSymbolInternal, Cci.IMethodBody> _methodBodyMap;
        private readonly TokenMap _referencesInILMap = new();
        private readonly ItemTokenMap<string> _stringsInILMap = new();
        private readonly ItemTokenMap<Cci.DebugSourceDocument> _sourceDocumentsInILMap = new();

        private ImmutableArray<Cci.AssemblyReferenceAlias> _lazyAssemblyReferenceAliases;
        private ImmutableArray<Cci.ManagedResource> _lazyManagedResources;
        private IEnumerable<EmbeddedText> _embeddedTexts = SpecializedCollections.EmptyEnumerable<EmbeddedText>();

        // Only set when running tests to allow inspection of the emitted data.
        internal CompilationTestData? TestData { get; private set; }

        internal EmitOptions EmitOptions { get; }

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
            _methodBodyMap = new ConcurrentDictionary<IMethodSymbolInternal, Cci.IMethodBody>(ReferenceEqualityComparer.Instance);
            EmitOptions = emitOptions;
        }

        internal DebugInformationFormat DebugInformationFormat => EmitOptions.DebugInformationFormat;
        internal HashAlgorithmName PdbChecksumAlgorithm => EmitOptions.PdbChecksumAlgorithm;

        /// <summary>
        /// Symbol changes when emitting EnC delta.
        /// </summary>
        public abstract SymbolChanges? EncSymbolChanges { get; }

        /// <summary>
        /// Previous EnC generation baseline, or null if this is not EnC delta.
        /// </summary>
        public abstract EmitBaseline? PreviousGeneration { get; }

        /// <summary>
        /// True if this module is an EnC update.
        /// </summary>
        public bool IsEncDelta => PreviousGeneration != null;

        /// <summary>
        /// EnC generation. 0 if the module is not an EnC delta, 1 if it is the first EnC delta, etc.
        /// </summary>
        public int CurrentGenerationOrdinal => (PreviousGeneration?.Ordinal + 1) ?? 0;
#nullable disable

        /// <summary>
        /// If this module represents an assembly, name of the assembly used in AssemblyDef table. Otherwise name of the module same as <see cref="ModuleName"/>.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Name of the module. Used in ModuleDef table.
        /// </summary>
        internal abstract string ModuleName { get; }

        internal abstract Cci.IAssemblyReference Translate(IAssemblySymbolInternal symbol, DiagnosticBag diagnostics);
        internal abstract Cci.ITypeReference Translate(ITypeSymbolInternal symbol, SyntaxNode syntaxOpt, DiagnosticBag diagnostics);
        internal abstract Cci.IMethodReference Translate(IMethodSymbolInternal symbol, DiagnosticBag diagnostics, bool needDeclaration);
        internal abstract Compilation CommonCompilation { get; }
        internal abstract IModuleSymbolInternal CommonSourceModule { get; }
        internal abstract IAssemblySymbolInternal CommonCorLibrary { get; }
        internal abstract CommonModuleCompilationState CommonModuleCompilationState { get; }
        internal abstract void CompilationFinished();
        internal abstract ImmutableDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>> GetAllSynthesizedMembers();
        internal abstract CommonEmbeddedTypesManager CommonEmbeddedTypesManagerOpt { get; }
        internal abstract Cci.ITypeReference EncTranslateType(ITypeSymbolInternal type, DiagnosticBag diagnostics);
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

#nullable enable
        public abstract IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypeDefinitions(EmitContext context);

        public IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypeDefinitionsCore(EmitContext context)
        {
            foreach (var typeDef in GetAnonymousTypeDefinitions(context))
            {
                yield return typeDef;
            }

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

            var privateImpl = GetFrozenPrivateImplementationDetails();
            if (privateImpl != null)
            {
                yield return privateImpl;

                foreach (var typeDef in privateImpl.GetAdditionalTopLevelTypes())
                {
                    yield return typeDef;
                }
            }
        }

        public abstract PrivateImplementationDetails? GetFrozenPrivateImplementationDetails();

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
        /// Builds a list of types, and their documents, that would otherwise not be referenced by any document info
        /// of any methods in those types, or any nested types. This data is helpful for navigating to the source of
        /// types that have no methods in one or more of the source files they are contained in.
        ///
        /// For example:
        ///
        /// First.cs:
        /// <code>
        /// partial class Outer
        /// {
        ///     partial class Inner
        ///     {
        ///         public void Method()
        ///         {
        ///         }
        ///     }
        /// }
        /// </code>
        ///
        /// /// Second.cs:
        /// <code>
        /// partial class Outer
        /// {
        ///     partial class Inner
        ///     {
        ///     }
        /// }
        /// </code>
        ///
        /// When navigating to the definition of "Outer" we know about First.cs because of the MethodDebugInfo for Outer.Inner.Method()
        /// but there would be no document information for Second.cs so this method would return that information.
        ///
        /// When navigating to "Inner" we likewise know about First.cs because of the MethodDebugInfo, and we know about Second.cs because
        /// of the document info for its containing type, so this method would not return information for Inner. In fact this method
        /// will never return information for any nested type.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public abstract IEnumerable<(Cci.ITypeDefinition, ImmutableArray<Cci.DebugSourceDocument>)> GetTypeToDebugDocumentMap(EmitContext context);

#nullable disable

        bool Cci.IDefinition.IsEncDeleted => false;

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

        Symbols.ISymbolInternal Cci.IReference.GetInternalSymbol() => null;

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

#nullable enable
        internal Cci.IMethodBody? GetMethodBody(IMethodSymbolInternal methodSymbol)
        {
            Debug.Assert(methodSymbol.ContainingModule == CommonSourceModule);
            Debug.Assert(methodSymbol.IsDefinition);
            Debug.Assert(((IMethodSymbol)methodSymbol.GetISymbol()).PartialDefinitionPart == null); // Must be definition.

            Cci.IMethodBody? body;

            if (_methodBodyMap.TryGetValue(methodSymbol, out body))
            {
                return body;
            }

            return null;
        }
#nullable disable

        public void SetMethodBody(IMethodSymbolInternal methodSymbol, Cci.IMethodBody body)
        {
            Debug.Assert(methodSymbol.ContainingModule == CommonSourceModule);
            Debug.Assert(methodSymbol.IsDefinition);
            Debug.Assert(((IMethodSymbol)methodSymbol.GetISymbol()).PartialDefinitionPart == null); // Must be definition.
            Debug.Assert(body == null || (object)methodSymbol == body.MethodDefinition.GetInternalSymbol());

            _methodBodyMap.Add(methodSymbol, body);
        }

        internal void SetPEEntryPoint(IMethodSymbolInternal method, DiagnosticBag diagnostics)
        {
            Debug.Assert(method == null || IsSourceDefinition(method));
            Debug.Assert(OutputKind.IsApplication());

            PEEntryPoint = Translate(method, diagnostics, needDeclaration: true);
        }

        internal void SetDebugEntryPoint(IMethodSymbolInternal method, DiagnosticBag diagnostics)
        {
            Debug.Assert(method == null || IsSourceDefinition(method));

            DebugEntryPoint = Translate(method, diagnostics, needDeclaration: true);
        }

        private bool IsSourceDefinition(IMethodSymbolInternal method)
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
        /// Returns copy of User Strings referenced from the IL in the module.
        /// </summary>
        public string[] CopyStrings()
        {
            return _stringsInILMap.CopyItems();
        }

        public uint GetFakeSymbolTokenForIL(Cci.IReference symbol, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            uint token = _referencesInILMap.GetOrAddTokenFor(symbol, out bool added);
            if (added)
            {
                ReferenceDependencyWalker.VisitReference(symbol, new EmitContext(this, syntaxNode, diagnostics, metadataOnly: false, includePrivateMembers: true));
            }
            return token;
        }

        public uint GetFakeSymbolTokenForIL(Cci.ISignature symbol, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            uint token = _referencesInILMap.GetOrAddTokenFor(symbol, out bool added);
            if (added)
            {
                ReferenceDependencyWalker.VisitSignature(symbol, new EmitContext(this, syntaxNode, diagnostics, metadataOnly: false, includePrivateMembers: true));
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

        public object GetReferenceFromToken(uint token)
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

        public ReadOnlySpan<object> ReferencesInIL()
        {
            return _referencesInILMap.GetAllItems();
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
                    builder.Add(r.ToManagedResource());
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

        internal void SetTestData(CompilationTestData testData)
        {
            Debug.Assert(TestData == null);
            TestData = testData;
            testData.Module = this;
        }

        public int GetTypeDefinitionGeneration(Cci.INamedTypeDefinition typeDef)
        {
            if (PreviousGeneration != null)
            {
                var symbolChanges = EncSymbolChanges!;
                if (symbolChanges.IsReplacedDef(typeDef))
                {
                    // Type emitted with Replace semantics in this delta, it's name should have the current generation ordinal suffix.
                    return CurrentGenerationOrdinal;
                }

                var previousTypeDef = symbolChanges.DefinitionMap.MapDefinition(typeDef);
                if (previousTypeDef != null && PreviousGeneration.GenerationOrdinals.TryGetValue(previousTypeDef, out int lastEmittedOrdinal))
                {
                    // Type previously emitted with Replace semantics is now updated in-place. Use the ordinal used to emit the last version of the type.
                    return lastEmittedOrdinal;
                }
            }

            return 0;
        }
    }

    /// <summary>
    /// Common base class for C# and VB PE module builder.
    /// </summary>
    internal abstract class PEModuleBuilder<TCompilation, TSourceModuleSymbol, TAssemblySymbol, TTypeSymbol, TNamedTypeSymbol, TMethodSymbol, TSyntaxNode, TEmbeddedTypesManager, TModuleCompilationState> : CommonPEModuleBuilder, ITokenDeferral
        where TCompilation : Compilation
        where TSourceModuleSymbol : class, IModuleSymbolInternal
        where TAssemblySymbol : class, IAssemblySymbolInternal
        where TTypeSymbol : class, ITypeSymbolInternal
        where TNamedTypeSymbol : class, TTypeSymbol, INamedTypeSymbolInternal
        where TMethodSymbol : class, IMethodSymbolInternal
        where TSyntaxNode : SyntaxNode
        where TEmbeddedTypesManager : CommonEmbeddedTypesManager
        where TModuleCompilationState : ModuleCompilationState<TNamedTypeSymbol, TMethodSymbol>
    {
        internal readonly TSourceModuleSymbol SourceModule;
        internal readonly TCompilation Compilation;

        private PrivateImplementationDetails _lazyPrivateImplementationDetails;
        private ArrayMethods _lazyArrayMethods;
        private HashSet<string> _namesOfTopLevelTypes;

        internal readonly TModuleCompilationState CompilationState;
        private readonly Cci.RootModuleType _rootModuleType;

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
            _rootModuleType = new Cci.RootModuleType(this);
        }

        public Cci.RootModuleType RootModuleType => _rootModuleType;

        internal sealed override void CompilationFinished()
        {
            this.CompilationState.Freeze();
        }

        internal override IAssemblySymbolInternal CommonCorLibrary => CorLibrary;
        internal abstract TAssemblySymbol CorLibrary { get; }

        internal abstract Cci.INamedTypeReference GetSpecialType(SpecialType specialType, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics);

        internal sealed override Cci.ITypeReference EncTranslateType(ITypeSymbolInternal type, DiagnosticBag diagnostics)
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

            AddTopLevelType(names, RootModuleType);
            VisitTopLevelType(typeReferenceIndexer, RootModuleType);
            yield return RootModuleType;

            foreach (var typeDef in GetTopLevelTypeDefinitionsCore(context))
            {
                AddTopLevelType(names, typeDef);
                VisitTopLevelType(typeReferenceIndexer, typeDef);
                yield return typeDef;
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

            static void AddTopLevelType(HashSet<string> names, Cci.INamespaceTypeDefinition type)
                // _namesOfTopLevelTypes are only used to generated exported types, which are not emitted in EnC deltas (hence generation 0):
                => names?.Add(MetadataHelpers.BuildQualifiedName(type.NamespaceName, Cci.MetadataWriter.GetMetadataName(type, generation: 0)));
        }

        public virtual ImmutableArray<TNamedTypeSymbol> GetAdditionalTopLevelTypes()
            => ImmutableArray<TNamedTypeSymbol>.Empty;

        public virtual ImmutableArray<TNamedTypeSymbol> GetEmbeddedTypes(DiagnosticBag diagnostics)
            => ImmutableArray<TNamedTypeSymbol>.Empty;

        internal abstract Cci.IAssemblyReference Translate(TAssemblySymbol symbol, DiagnosticBag diagnostics);
        internal abstract Cci.ITypeReference Translate(TTypeSymbol symbol, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics);
        internal abstract Cci.IMethodReference Translate(TMethodSymbol symbol, DiagnosticBag diagnostics, bool needDeclaration);

        internal sealed override Cci.IAssemblyReference Translate(IAssemblySymbolInternal symbol, DiagnosticBag diagnostics)
        {
            return Translate((TAssemblySymbol)symbol, diagnostics);
        }

        internal sealed override Cci.ITypeReference Translate(ITypeSymbolInternal symbol, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            return Translate((TTypeSymbol)symbol, (TSyntaxNode)syntaxNodeOpt, diagnostics);
        }

        internal sealed override Cci.IMethodReference Translate(IMethodSymbolInternal symbol, DiagnosticBag diagnostics, bool needDeclaration)
        {
            return Translate((TMethodSymbol)symbol, diagnostics, needDeclaration);
        }

        internal sealed override IModuleSymbolInternal CommonSourceModule => SourceModule;
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

        internal Cci.IFieldReference GetModuleCancellationToken(Cci.ITypeReference cancellationTokenType, TSyntaxNode syntaxOpt, DiagnosticBag diagnostics)
            => GetPrivateImplClass(syntaxOpt, diagnostics).GetModuleCancellationToken(cancellationTokenType);

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
                Cci.IMethodDefinition cctor = CreatePrivateImplementationDetailsStaticConstructor(syntaxOpt, diagnostics);
                Debug.Assert(((ISynthesizedGlobalMethodSymbol)cctor.GetInternalSymbol()).ContainingPrivateImplementationDetailsType == (object)details);

                details.TryAddSynthesizedMethod(cctor);
            }
        }

        protected abstract Cci.IMethodDefinition CreatePrivateImplementationDetailsStaticConstructor(TSyntaxNode syntaxOpt, DiagnosticBag diagnostics);

        #region Synthesized Members

        /// <summary>
        /// Captures the set of synthesized definitions that should be added to a type
        /// during emit process.
        /// </summary>
        private sealed class SynthesizedDefinitions
        {
            private ConcurrentQueue<Cci.INestedTypeDefinition> NestedTypes;
            public ConcurrentQueue<Cci.IMethodDefinition> Methods;
            public ConcurrentQueue<Cci.IPropertyDefinition> Properties;
            public ConcurrentQueue<Cci.IFieldDefinition> Fields;

            // Nested types may be queued from concurrent threads, but we need to emit them
            // in a deterministic order.
            internal IEnumerable<Cci.INestedTypeDefinition> OrderedNestedTypes
            {
                get
                {
                    // We don't synthesize nested types with different arities for a given name
                    Debug.Assert(NestedTypes is null ||
                        NestedTypes.Select(t => t.Name).Distinct().Count() == NestedTypes.Count());

                    return NestedTypes?.OrderBy(t => t.Name, StringComparer.Ordinal);
                }
            }

            internal void AddNestedType(Cci.INestedTypeDefinition nestedType)
            {
                if (NestedTypes == null)
                {
                    Interlocked.CompareExchange(ref NestedTypes, new ConcurrentQueue<Cci.INestedTypeDefinition>(), null);
                }

                NestedTypes.Enqueue(nestedType);
            }

            public ImmutableArray<ISymbolInternal> GetAllMembers()
            {
                var builder = ArrayBuilder<ISymbolInternal>.GetInstance();

                if (Fields != null)
                {
                    foreach (var field in Fields)
                    {
                        builder.Add(field.GetInternalSymbol());
                    }
                }

                if (Methods != null)
                {
                    foreach (var method in Methods)
                    {
                        builder.Add(method.GetInternalSymbol());
                    }
                }

                if (Properties != null)
                {
                    foreach (var property in Properties)
                    {
                        builder.Add(property.GetInternalSymbol());
                    }
                }

                if (NestedTypes != null)
                {
                    foreach (var type in OrderedNestedTypes)
                    {
                        builder.Add(type.GetInternalSymbol());
                    }
                }

                return builder.ToImmutableAndFree();
            }
        }

        private readonly ConcurrentDictionary<TNamedTypeSymbol, SynthesizedDefinitions> _synthesizedTypeMembers =
            new ConcurrentDictionary<TNamedTypeSymbol, SynthesizedDefinitions>(ReferenceEqualityComparer.Instance);

        private ConcurrentDictionary<INamespaceSymbolInternal, ConcurrentQueue<INamespaceOrTypeSymbolInternal>> _lazySynthesizedNamespaceMembers;

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
                compileEmitTypes = defs.OrderedNestedTypes;
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
            defs.AddNestedType(nestedType);
        }

        public void AddSynthesizedDefinition(INamespaceSymbolInternal container, INamespaceOrTypeSymbolInternal typeOrNamespace)
        {
            Debug.Assert(typeOrNamespace != null);
            if (_lazySynthesizedNamespaceMembers == null)
            {
                Interlocked.CompareExchange(ref _lazySynthesizedNamespaceMembers, new ConcurrentDictionary<INamespaceSymbolInternal, ConcurrentQueue<INamespaceOrTypeSymbolInternal>>(), null);
            }

            _lazySynthesizedNamespaceMembers.GetOrAdd(container, _ => new ConcurrentQueue<INamespaceOrTypeSymbolInternal>()).Enqueue(typeOrNamespace);
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

        internal override ImmutableDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>> GetAllSynthesizedMembers()
        {
            var builder = ImmutableDictionary.CreateBuilder<ISymbolInternal, ImmutableArray<ISymbolInternal>>();

            foreach (var entry in _synthesizedTypeMembers)
            {
                builder.Add(entry.Key, entry.Value.GetAllMembers());
            }

            var namespaceMembers = _lazySynthesizedNamespaceMembers;
            if (namespaceMembers != null)
            {
                foreach (var entry in namespaceMembers)
                {
                    builder.Add(entry.Key, entry.Value.ToImmutableArray<ISymbolInternal>());
                }
            }

            return builder.ToImmutable();
        }

        #endregion

        #region Token Mapping

        Cci.IFieldReference ITokenDeferral.GetFieldForData(ImmutableArray<byte> data, ushort alignment, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            RoslynDebug.Assert(alignment is 1 or 2 or 4 or 8, $"Unexpected alignment: {alignment}");

            var privateImpl = GetPrivateImplClass((TSyntaxNode)syntaxNode, diagnostics);

            // map a field to the block (that makes it addressable via a token)
            return privateImpl.CreateDataField(data, alignment);
        }

        Cci.IFieldReference ITokenDeferral.GetArrayCachingFieldForData(ImmutableArray<byte> data, Cci.IArrayTypeReference arrayType, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            var privateImpl = GetPrivateImplClass((TSyntaxNode)syntaxNode, diagnostics);

            var emitContext = new EmitContext(this, syntaxNode, diagnostics, metadataOnly: false, includePrivateMembers: true);

            // map a field to the block (that makes it addressable via a token)
            return privateImpl.CreateArrayCachingField(data, arrayType, emitContext);
        }

        public Cci.IFieldReference GetArrayCachingFieldForConstants(ImmutableArray<ConstantValue> constants, Cci.IArrayTypeReference arrayType, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            var privateImpl = GetPrivateImplClass((TSyntaxNode)syntaxNode, diagnostics);
            var emitContext = new EmitContext(this, syntaxNode, diagnostics, metadataOnly: false, includePrivateMembers: true);
            return privateImpl.CreateArrayCachingField(constants, arrayType, emitContext);
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

#nullable enable

        internal PrivateImplementationDetails GetPrivateImplClass(TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            var result = _lazyPrivateImplementationDetails;

            if (result == null)
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

                if (Interlocked.CompareExchange(ref _lazyPrivateImplementationDetails, result, null) != null)
                {
                    result = _lazyPrivateImplementationDetails;
                }
            }

            return result;
        }

        public PrivateImplementationDetails? FreezePrivateImplementationDetails()
        {
            _lazyPrivateImplementationDetails?.Freeze();
            return _lazyPrivateImplementationDetails;
        }

        public override PrivateImplementationDetails? GetFrozenPrivateImplementationDetails()
        {
            Debug.Assert(_lazyPrivateImplementationDetails?.IsFrozen != false);
            return _lazyPrivateImplementationDetails;
        }

#nullable disable

        #endregion

        public sealed override Cci.ITypeReference GetPlatformType(Cci.PlatformType platformType, EmitContext context)
        {
            Debug.Assert((object)this == context.Module);

            switch (platformType)
            {
                case Cci.PlatformType.SystemType:
                    throw ExceptionUtilities.UnexpectedValue(platformType);

                default:
                    return GetSpecialType((SpecialType)platformType, (TSyntaxNode)context.SyntaxNode, context.Diagnostics);
            }
        }
    }
}
