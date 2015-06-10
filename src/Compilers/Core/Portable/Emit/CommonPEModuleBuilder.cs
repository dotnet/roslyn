// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit.NoPia;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    internal abstract class CommonPEModuleBuilder
    {
        internal abstract EmitOptions EmitOptions { get; }
        internal abstract Cci.IAssemblyReference Translate(IAssemblySymbol symbol, DiagnosticBag diagnostics);
        internal abstract Cci.ITypeReference Translate(ITypeSymbol symbol, SyntaxNode syntaxOpt, DiagnosticBag diagnostics);
        internal abstract Cci.IMethodReference EntryPoint { get; }
        internal abstract bool SupportsPrivateImplClass { get; }
        internal abstract ImmutableArray<Cci.INamespaceTypeDefinition> GetAnonymousTypes();
        internal abstract Compilation CommonCompilation { get; }
        internal abstract CommonModuleCompilationState CommonModuleCompilationState { get; }
        internal abstract void CompilationFinished();
        internal abstract ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> GetSynthesizedMembers();
        internal abstract CommonEmbeddedTypesManager CommonEmbeddedTypesManagerOpt { get; }
        internal abstract Cci.ITypeReference EncTranslateType(ITypeSymbol type, DiagnosticBag diagnostics);
    }

    /// <summary>
    /// Common base class for C# and VB PE module builder.
    /// </summary>
    internal abstract class PEModuleBuilder<TCompilation, TSourceModuleSymbol, TAssemblySymbol, TTypeSymbol, TNamedTypeSymbol, TMethodSymbol, TSyntaxNode, TEmbeddedTypesManager, TModuleCompilationState> : CommonPEModuleBuilder, Cci.IModule, ITokenDeferral
        where TCompilation : Compilation
        where TSourceModuleSymbol : class, IModuleSymbol
        where TAssemblySymbol : class
        where TTypeSymbol : class
        where TNamedTypeSymbol : class, TTypeSymbol, Cci.INamespaceTypeDefinition
        where TMethodSymbol : class, Cci.IMethodDefinition
        where TSyntaxNode : SyntaxNode
        where TEmbeddedTypesManager : NoPia.CommonEmbeddedTypesManager
        where TModuleCompilationState : ModuleCompilationState<TNamedTypeSymbol, TMethodSymbol>
    {
        private readonly Cci.RootModuleType _rootModuleType = new Cci.RootModuleType();

        private readonly TSourceModuleSymbol _sourceModule;
        private readonly TCompilation _compilation;
        private readonly OutputKind _outputKind;
        private readonly EmitOptions _emitOptions;
        private readonly ModulePropertiesForSerialization _serializationProperties;
        private readonly ConcurrentCache<ValueTuple<string, string>, string> _normalizedPathsCache = new ConcurrentCache<ValueTuple<string, string>, string>(16);

        /// <summary>
        /// Used to translate assembly symbols to assembly references in scenarios when the physical assemblies 
        /// being emitted don't correspond to the assembly symbols 1:1. This happens, for example, in interactive sessions where
        /// multiple code submissions might be compiled into a single dynamic assembly or into multiple assemblies 
        /// depending on properties of the code being emitted. If null we map assembly symbol exactly to its assembly name.
        /// </summary>
        protected readonly Func<TAssemblySymbol, AssemblyIdentity> assemblySymbolMapper;

        private readonly TokenMap<Cci.IReference> _referencesInILMap = new TokenMap<Cci.IReference>();
        private readonly StringTokenMap _stringsInILMap = new StringTokenMap();
        private readonly ConcurrentDictionary<TMethodSymbol, Cci.IMethodBody> _methodBodyMap =
            new ConcurrentDictionary<TMethodSymbol, Cci.IMethodBody>(ReferenceEqualityComparer.Instance);

        private TMethodSymbol _entryPoint;
        private PrivateImplementationDetails _privateImplementationDetails;
        private ArrayMethods _lazyArrayMethods;
        private HashSet<string> _namesOfTopLevelTypes;
        internal IEnumerable<Cci.IWin32Resource> Win32Resources { set; private get; }
        internal Cci.ResourceSection Win32ResourceSection { set; private get; }

        internal readonly IEnumerable<ResourceDescription> ManifestResources;
        internal readonly TModuleCompilationState CompilationState;

        // This is a map from the document "name" to the document.
        // Document "name" is typically a file path like "C:\Abc\Def.cs". However, that is not guaranteed.
        // For compatibility reasons the names are treated as case-sensitive in C# and case-insensitive in VB.
        // Neither language trims the names, so they are both sensitive to the leading and trailing whitespaces.
        // NOTE: We are not considering how filesystem or debuggers do the comparisons, but how native implementations did.
        // Deviating from that may result in unexpected warnings or different behavior (possibly without warnings).
        private readonly ConcurrentDictionary<string, Cci.DebugSourceDocument> _debugDocuments;

        public abstract TEmbeddedTypesManager EmbeddedTypesManagerOpt { get; }

        /// <summary>
        /// EnC generation.
        /// </summary>
        public abstract int CurrentGenerationOrdinal { get; }

        private ImmutableArray<Cci.AssemblyReferenceAlias> _lazyAssemblyReferenceAliases;

        protected PEModuleBuilder(
            TCompilation compilation,
            TSourceModuleSymbol sourceModule,
            ModulePropertiesForSerialization serializationProperties,
            IEnumerable<ResourceDescription> manifestResources,
            OutputKind outputKind,
            Func<TAssemblySymbol, AssemblyIdentity> assemblySymbolMapper,
            EmitOptions emitOptions,
            TModuleCompilationState compilationState)
        {
            Debug.Assert(sourceModule != null);
            Debug.Assert(serializationProperties != null);

            _compilation = compilation;
            _sourceModule = sourceModule;
            _serializationProperties = serializationProperties;
            this.ManifestResources = manifestResources;
            _outputKind = outputKind;
            this.assemblySymbolMapper = assemblySymbolMapper;
            _emitOptions = emitOptions;
            this.CompilationState = compilationState;

            if (compilation.IsCaseSensitive)
            {
                _debugDocuments = new ConcurrentDictionary<string, Cci.DebugSourceDocument>(StringComparer.Ordinal);
            }
            else
            {
                _debugDocuments = new ConcurrentDictionary<string, Cci.DebugSourceDocument>(StringComparer.OrdinalIgnoreCase);
            }
        }

        internal sealed override void CompilationFinished()
        {
            this.CompilationState.Freeze();
        }

        internal override EmitOptions EmitOptions
        {
            get { return _emitOptions; }
        }

        internal abstract string ModuleName { get; }
        internal abstract string Name { get; }
        internal abstract TAssemblySymbol CorLibrary { get; }

        internal abstract byte LinkerMajorVersion { get; }
        internal abstract byte LinkerMinorVersion { get; }

        internal abstract IEnumerable<Cci.ICustomAttribute> GetSourceAssemblyAttributes();
        internal abstract IEnumerable<Cci.SecurityAttribute> GetSourceAssemblySecurityAttributes();
        internal abstract IEnumerable<Cci.ICustomAttribute> GetSourceModuleAttributes();
        internal abstract Cci.ICustomAttribute SynthesizeAttribute(WellKnownMember attributeConstructor);

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

        internal abstract IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypesCore(EmitContext context);

        private IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypes(EmitContext context)
        {
            Cci.NoPiaReferenceIndexer noPiaIndexer = null;
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

            // First time through, we need to push things through NoPiaReferenceIndexer
            // to make sure we collect all to be embedded NoPia types and members.
            if (EmbeddedTypesManagerOpt != null && !EmbeddedTypesManagerOpt.IsFrozen)
            {
                noPiaIndexer = new Cci.NoPiaReferenceIndexer(context);
                Debug.Assert(names != null);
                this.Dispatch(noPiaIndexer);
            }

            AddTopLevelType(names, _rootModuleType);
            VisitTopLevelType(noPiaIndexer, _rootModuleType);
            yield return _rootModuleType;

            foreach (var type in this.GetAnonymousTypes())
            {
                AddTopLevelType(names, type);
                VisitTopLevelType(noPiaIndexer, type);
                yield return type;
            }

            foreach (var type in this.GetTopLevelTypesCore(context))
            {
                AddTopLevelType(names, type);
                VisitTopLevelType(noPiaIndexer, type);
                yield return type;
            }

            var privateImpl = this.PrivateImplClass;
            if (privateImpl != null)
            {
                AddTopLevelType(names, privateImpl);
                VisitTopLevelType(noPiaIndexer, privateImpl);
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

        internal abstract Cci.IAssemblyReference Translate(TAssemblySymbol symbol, DiagnosticBag diagnostics);
        internal abstract Cci.ITypeReference Translate(TTypeSymbol symbol, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics);

        internal sealed override Cci.IAssemblyReference Translate(IAssemblySymbol symbol, DiagnosticBag diagnostics)
        {
            return Translate((TAssemblySymbol)symbol, diagnostics);
        }

        internal sealed override Cci.ITypeReference Translate(ITypeSymbol symbol, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            return Translate((TTypeSymbol)symbol, (TSyntaxNode)syntaxNodeOpt, diagnostics);
        }

        internal OutputKind OutputKind => _outputKind;
        internal TSourceModuleSymbol SourceModule => _sourceModule;
        internal TCompilation Compilation => _compilation;

        internal sealed override Compilation CommonCompilation => _compilation;
        internal sealed override CommonModuleCompilationState CommonModuleCompilationState => CompilationState;
        internal sealed override CommonEmbeddedTypesManager CommonEmbeddedTypesManagerOpt => EmbeddedTypesManagerOpt;

        // General entry point method. May be a PE entry point or a submission entry point.
        internal sealed override Cci.IMethodReference EntryPoint => _entryPoint;

        internal void SetEntryPoint(TMethodSymbol value)
        {
            Debug.Assert(value == null ||
                ((object)((IMethodSymbol)value).ContainingModule == (object)_sourceModule && ReferenceEquals(value, ((IMethodSymbol)value).OriginalDefinition)));
            _entryPoint = value;
        }

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

        private static void VisitTopLevelType(Cci.NoPiaReferenceIndexer noPiaIndexer, Cci.INamespaceTypeDefinition type)
        {
            noPiaIndexer?.Visit((Cci.ITypeDefinition)type);
        }

        private ImmutableArray<Cci.AssemblyReferenceAlias> CalculateAssemblyReferenceAliases(EmitContext context)
        {
            var result = ArrayBuilder<Cci.AssemblyReferenceAlias>.GetInstance(_compilation.ExternalReferences.Length);

            var referenceManager = _compilation.GetBoundReferenceManager();

            // Enumerate external references (#r's don't define aliases) to preserve the order.
            foreach (MetadataReference reference in _compilation.ExternalReferences)
            {
                // duplicate references might have been skipped by the assembly binder:

                IAssemblySymbol symbol;
                ImmutableArray<string> aliases;
                if (referenceManager.TryGetReferencedAssemblySymbol(reference, out symbol, out aliases))
                {
                    for (int i = 0; i < aliases.Length; i++)
                    {
                        string alias = aliases[i];

                        // filter out duplicates and global aliases:
                        if (alias != MetadataReferenceProperties.GlobalAlias && aliases.IndexOf(alias, 0, i) < 0)
                        {
                            result.Add(new Cci.AssemblyReferenceAlias(alias, Translate(symbol, context.Diagnostics)));
                        }
                    }
                }
            }

            return result.ToImmutableAndFree();
        }

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

            public ImmutableArray<Cci.ITypeDefinitionMember> GetAllMembers()
            {
                var builder = ArrayBuilder<Cci.ITypeDefinitionMember>.GetInstance();

                if (Fields != null)
                {
                    foreach (var field in Fields)
                    {
                        builder.Add(field);
                    }
                }

                if (Methods != null)
                {
                    foreach (var method in Methods)
                    {
                        builder.Add(method);
                    }
                }

                if (Properties != null)
                {
                    foreach (var property in Properties)
                    {
                        builder.Add(property);
                    }
                }

                if (NestedTypes != null)
                {
                    foreach (var type in NestedTypes)
                    {
                        builder.Add(type);
                    }
                }

                return builder.ToImmutableAndFree();
            }
        }

        private readonly ConcurrentDictionary<TNamedTypeSymbol, SynthesizedDefinitions> _synthesizedDefs =
            new ConcurrentDictionary<TNamedTypeSymbol, SynthesizedDefinitions>();

        public void AddSynthesizedDefinition(TNamedTypeSymbol container, Cci.INestedTypeDefinition nestedType)
        {
            Debug.Assert(nestedType != null);

            SynthesizedDefinitions defs = GetCacheOfSynthesizedDefinitions(container);
            if (defs.NestedTypes == null)
            {
                Interlocked.CompareExchange(ref defs.NestedTypes, new ConcurrentQueue<Cci.INestedTypeDefinition>(), null);
            }

            defs.NestedTypes.Enqueue(nestedType);
        }

        internal abstract IEnumerable<Cci.INestedTypeDefinition> GetSynthesizedNestedTypes(TNamedTypeSymbol container);

        /// <summary>
        /// Returns null if there are no compiler generated types.
        /// </summary>
        public IEnumerable<Cci.INestedTypeDefinition> GetSynthesizedTypes(TNamedTypeSymbol container)
        {
            IEnumerable<Cci.INestedTypeDefinition> declareTypes = GetSynthesizedNestedTypes(container);
            IEnumerable<Cci.INestedTypeDefinition> compileEmitTypes = null;

            SynthesizedDefinitions defs = GetCacheOfSynthesizedDefinitions(container, addIfNotFound: false);
            if (defs != null)
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

        private SynthesizedDefinitions GetCacheOfSynthesizedDefinitions(TNamedTypeSymbol container, bool addIfNotFound = true)
        {
            Debug.Assert(((INamedTypeSymbol)container).IsDefinition);
            if (addIfNotFound)
            {
                return _synthesizedDefs.GetOrAdd(container, _ => new SynthesizedDefinitions());
            }

            SynthesizedDefinitions defs;
            _synthesizedDefs.TryGetValue(container, out defs);
            return defs;
        }

        public void AddSynthesizedDefinition(TNamedTypeSymbol container, Cci.IMethodDefinition method)
        {
            Debug.Assert(method != null);

            SynthesizedDefinitions defs = GetCacheOfSynthesizedDefinitions(container);
            if (defs.Methods == null)
            {
                Interlocked.CompareExchange(ref defs.Methods, new ConcurrentQueue<Cci.IMethodDefinition>(), null);
            }

            defs.Methods.Enqueue(method);
        }

        /// <summary>
        /// Returns null if there are no synthesized methods.
        /// </summary>
        public IEnumerable<Cci.IMethodDefinition> GetSynthesizedMethods(TNamedTypeSymbol container)
        {
            return GetCacheOfSynthesizedDefinitions(container, addIfNotFound: false)?.Methods;
        }

        public void AddSynthesizedDefinition(TNamedTypeSymbol container, Cci.IPropertyDefinition property)
        {
            Debug.Assert(property != null);

            SynthesizedDefinitions defs = GetCacheOfSynthesizedDefinitions(container);
            if (defs.Properties == null)
            {
                Interlocked.CompareExchange(ref defs.Properties, new ConcurrentQueue<Cci.IPropertyDefinition>(), null);
            }

            defs.Properties.Enqueue(property);
        }

        /// <summary>
        /// Returns null if there are no synthesized properties.
        /// </summary>
        public IEnumerable<Cci.IPropertyDefinition> GetSynthesizedProperties(TNamedTypeSymbol container)
        {
            return GetCacheOfSynthesizedDefinitions(container, addIfNotFound: false)?.Properties;
        }

        public void AddSynthesizedDefinition(TNamedTypeSymbol container, Cci.IFieldDefinition field)
        {
            Debug.Assert(field != null);

            SynthesizedDefinitions defs = GetCacheOfSynthesizedDefinitions(container);
            if (defs.Fields == null)
            {
                Interlocked.CompareExchange(ref defs.Fields, new ConcurrentQueue<Cci.IFieldDefinition>(), null);
            }

            defs.Fields.Enqueue(field);
        }

        /// <summary>
        /// Returns null if there are no synthesized fields.
        /// </summary>
        public IEnumerable<Cci.IFieldDefinition> GetSynthesizedFields(TNamedTypeSymbol container)
        {
            return GetCacheOfSynthesizedDefinitions(container, addIfNotFound: false)?.Fields;
        }

        internal override ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> GetSynthesizedMembers()
        {
            var builder = ImmutableDictionary.CreateBuilder<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>>();

            foreach (var entry in _synthesizedDefs)
            {
                builder.Add(entry.Key, entry.Value.GetAllMembers());
            }

            return builder.ToImmutable();
        }

        public ImmutableArray<Cci.ITypeDefinitionMember> GetSynthesizedMembers(Cci.ITypeDefinition container)
        {
            SynthesizedDefinitions defs = GetCacheOfSynthesizedDefinitions((TNamedTypeSymbol)container, addIfNotFound: false);
            if (defs == null)
            {
                return ImmutableArray<Cci.ITypeDefinitionMember>.Empty;
            }

            return defs.GetAllMembers();
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

        public uint GetFakeSymbolTokenForIL(Cci.IReference symbol, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            bool added;
            uint token = _referencesInILMap.GetOrAddTokenFor(symbol, out added);
            if (added)
            {
                ReferenceDependencyWalker.VisitReference(symbol, new EmitContext(this, syntaxNode, diagnostics));
            }
            return token;
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

        IEnumerable<Cci.IReference> Cci.IModule.ReferencesInIL(out int count)
        {
            return _referencesInILMap.GetAllItemsAndCount(out count);
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
                        _compilation.GetSubmissionSlotIndex(),
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

        #region Method Body Map

        internal Cci.IMethodBody GetMethodBody(TMethodSymbol methodSymbol)
        {
            Debug.Assert(((IMethodSymbol)methodSymbol).ContainingModule == this.SourceModule);
            Debug.Assert(((IMethodSymbol)methodSymbol).IsDefinition);
            Debug.Assert(((IMethodSymbol)methodSymbol).PartialDefinitionPart == null); // Must be definition.

            Cci.IMethodBody body;

            if (_methodBodyMap.TryGetValue(methodSymbol, out body))
            {
                return body;
            }

            return null;
        }

        public void SetMethodBody(TMethodSymbol methodSymbol, Cci.IMethodBody body)
        {
            Debug.Assert(((IMethodSymbol)methodSymbol).ContainingModule == this.SourceModule);
            Debug.Assert(((IMethodSymbol)methodSymbol).IsDefinition);
            Debug.Assert(((IMethodSymbol)methodSymbol).PartialDefinitionPart == null); // Must be definition.
            Debug.Assert(body == null || (object)methodSymbol == body.MethodDefinition);

            _methodBodyMap.Add(methodSymbol, body);
        }

        #endregion

        #region IModule

        public virtual void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.IModule)this);
        }

        ushort Cci.IModule.MajorSubsystemVersion
        {
            get { return (ushort)_serializationProperties.SubsystemVersion.Major; }
        }

        ushort Cci.IModule.MinorSubsystemVersion
        {
            get { return (ushort)_serializationProperties.SubsystemVersion.Minor; }
        }

        byte Cci.IModule.LinkerMajorVersion
        {
            get
            {
                return LinkerMajorVersion;
            }
        }

        byte Cci.IModule.LinkerMinorVersion
        {
            get
            {
                return LinkerMinorVersion;
            }
        }

        IEnumerable<Cci.INamespaceTypeDefinition> Cci.IModule.GetTopLevelTypes(EmitContext context)
        {
            return GetTopLevelTypes(context);
        }

        public abstract IEnumerable<Cci.ITypeExport> GetExportedTypes(EmitContext context);

        Cci.ITypeReference Cci.IModule.GetPlatformType(Cci.PlatformType platformType, EmitContext context)
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

        protected abstract bool IsPlatformType(Cci.ITypeReference typeRef, Cci.PlatformType platformType);

        bool Cci.IModule.IsPlatformType(Cci.ITypeReference typeRef, Cci.PlatformType platformType)
        {
            return IsPlatformType(typeRef, platformType);
        }

        IEnumerable<Cci.ICustomAttribute> Cci.IModule.AssemblyAttributes
        {
            get
            {
                return GetSourceAssemblyAttributes();
            }
        }

        IEnumerable<Cci.SecurityAttribute> Cci.IModule.AssemblySecurityAttributes
        {
            get
            {
                return GetSourceAssemblySecurityAttributes();
            }
        }

        IEnumerable<Cci.ICustomAttribute> Cci.IModule.ModuleAttributes
        {
            get { return GetSourceModuleAttributes(); }
        }

        ImmutableArray<Cci.AssemblyReferenceAlias> Cci.IModule.GetAssemblyReferenceAliases(EmitContext context)
        {
            if (_lazyAssemblyReferenceAliases.IsDefault)
            {
                ImmutableInterlocked.InterlockedCompareExchange(ref _lazyAssemblyReferenceAliases, CalculateAssemblyReferenceAliases(context), default(ImmutableArray<Cci.AssemblyReferenceAlias>));
            }

            return _lazyAssemblyReferenceAliases;
        }

        bool Cci.IModule.GenerateVisualBasicStylePdb => GenerateVisualBasicStylePdb;
        protected abstract bool GenerateVisualBasicStylePdb { get; }

        IEnumerable<string> Cci.IModule.LinkedAssembliesDebugInfo => LinkedAssembliesDebugInfo;
        protected abstract IEnumerable<string> LinkedAssembliesDebugInfo { get; }

        ImmutableArray<Cci.UsedNamespaceOrType> Cci.IModule.GetImports() => GetImports();
        protected abstract ImmutableArray<Cci.UsedNamespaceOrType> GetImports();

        string Cci.IModule.DefaultNamespace => DefaultNamespace;
        protected abstract string DefaultNamespace { get; }

        // PE entry point, only available for console and windows apps:
        Cci.IMethodReference Cci.IModule.EntryPoint
        {
            get
            {
                return _outputKind.IsApplication() ? _entryPoint : null;
            }
        }

        protected abstract Cci.IAssemblyReference GetCorLibraryReferenceToEmit(EmitContext context);

        /// <summary>
        /// Builds symbol definition to location map used for emitting token -> location info
        /// into PDB to be consumed by WinMdExp.exe tool (only applicable for /t:winmdobj)
        /// </summary>
        protected abstract MultiDictionary<Cci.DebugSourceDocument, Cci.DefinitionWithLocation> GetSymbolToLocationMap();

        MultiDictionary<Cci.DebugSourceDocument, Cci.DefinitionWithLocation> Cci.IModule.GetSymbolToLocationMap()
        {
            return GetSymbolToLocationMap();
        }

        IEnumerable<Cci.IAssemblyReference> Cci.IModule.GetAssemblyReferences(EmitContext context)
        {
            Cci.IAssemblyReference corLibrary = GetCorLibraryReferenceToEmit(context);

            // Only add Cor Library reference explicitly, PeWriter will add
            // other references implicitly on as needed basis.
            if (corLibrary != null)
            {
                yield return corLibrary;
            }

            if (OutputKind != CodeAnalysis.OutputKind.NetModule)
            {
                // Explicitly add references from added modules
                foreach (var aRef in GetAssemblyReferencesFromAddedModules(context.Diagnostics))
                {
                    yield return aRef;
                }
            }
        }

        protected abstract IEnumerable<Cci.IAssemblyReference> GetAssemblyReferencesFromAddedModules(DiagnosticBag diagnostics);

        private IEnumerable<Cci.ManagedResource> _lazyManagedResources;

        IEnumerable<Cci.ManagedResource> Cci.IModule.GetResources(EmitContext context)
        {
            if (_lazyManagedResources == null)
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

        protected abstract void AddEmbeddedResourcesFromAddedModules(ArrayBuilder<Cci.ManagedResource> builder, DiagnosticBag diagnostics);

        Cci.IAssembly Cci.IModule.AsAssembly
        {
            get { return this as Cci.IAssembly; }
        }

        Cci.IAssemblyReference Cci.IModule.GetCorLibrary(EmitContext context)
        {
            return Translate(CorLibrary, context.Diagnostics);
        }

        ulong Cci.IModule.BaseAddress
        {
            get { return _serializationProperties.BaseAddress; }
        }

        Cci.IAssembly Cci.IModule.GetContainingAssembly(EmitContext context)
        {
            return this.OutputKind.IsNetModule() ? null : (Cci.IAssembly)this;
        }

        Cci.IAssemblyReference Cci.IModuleReference.GetContainingAssembly(EmitContext context)
        {
            return this.OutputKind.IsNetModule() ? null : (Cci.IAssemblyReference)this;
        }

        ushort Cci.IModule.DllCharacteristics
        {
            get { return _serializationProperties.DllCharacteristics; }
        }

        uint Cci.IModule.FileAlignment
        {
            get { return _serializationProperties.FileAlignment; }
        }

        IEnumerable<string> Cci.IModule.GetStrings()
        {
            return _stringsInILMap.GetAllItems();
        }

        bool Cci.IModule.ILOnly
        {
            get { return _serializationProperties.ILOnly; }
        }

        Cci.ModuleKind Cci.IModule.Kind
        {
            get
            {
                switch (_outputKind)
                {
                    case OutputKind.ConsoleApplication:
                        return Cci.ModuleKind.ConsoleApplication;

                    case OutputKind.WindowsRuntimeApplication: // TODO: separate ModuleKind?
                    case OutputKind.WindowsApplication:
                        return Cci.ModuleKind.WindowsApplication;

                    case OutputKind.WindowsRuntimeMetadata:
                        return Cci.ModuleKind.WindowsRuntimeMetadata;

                    case OutputKind.DynamicallyLinkedLibrary:
                    case OutputKind.NetModule:
                        return Cci.ModuleKind.DynamicallyLinkedLibrary;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(_outputKind);
                }
            }
        }

        byte Cci.IModule.MetadataFormatMajorVersion
        {
            get { return _serializationProperties.MetadataFormatMajorVersion; }
        }

        byte Cci.IModule.MetadataFormatMinorVersion
        {
            get { return _serializationProperties.MetadataFormatMinorVersion; }
        }

        string Cci.IModule.ModuleName
        {
            get { return ModuleName; }
        }

        IEnumerable<Cci.IModuleReference> Cci.IModule.ModuleReferences
        {
            get
            {
                // Let's not add any module references explicitly,
                // PeWriter will implicitly add those needed.
                return SpecializedCollections.EmptyEnumerable<Cci.IModuleReference>();
            }
        }

        Guid Cci.IModule.PersistentIdentifier
        {
            get { return _serializationProperties.PersistentIdentifier; }
        }

        bool Cci.IModule.StrongNameSigned
        {
            get { return _serializationProperties.StrongNameSigned; }
        }

        Cci.Machine Cci.IModule.Machine
        {
            get { return _serializationProperties.Machine; }
        }

        bool Cci.IModule.RequiresStartupStub
        {
            get { return _serializationProperties.RequiresStartupStub; }
        }

        bool Cci.IModule.Prefers32bits
        {
            get { return _serializationProperties.Platform == Platform.AnyCpu32BitPreferred; }
        }

        bool Cci.IModule.RequiresAmdInstructionSet
        {
            get { return _serializationProperties.Platform.RequiresAmdInstructionSet(); }
        }

        bool Cci.IModule.Requires32bits
        {
            get { return _serializationProperties.Platform.Requires32Bit(); }
        }

        bool Cci.IModule.Requires64bits
        {
            get { return _serializationProperties.Platform.Requires64Bit(); }
        }

        ulong Cci.IModule.SizeOfHeapCommit
        {
            get { return _serializationProperties.SizeOfHeapCommit; }
        }

        ulong Cci.IModule.SizeOfHeapReserve
        {
            get { return _serializationProperties.SizeOfHeapReserve; }
        }

        ulong Cci.IModule.SizeOfStackCommit
        {
            get { return _serializationProperties.SizeOfStackCommit; }
        }

        ulong Cci.IModule.SizeOfStackReserve
        {
            get { return _serializationProperties.SizeOfStackReserve; }
        }

        string Cci.IModule.TargetRuntimeVersion
        {
            get { return _serializationProperties.TargetRuntimeVersion; }
        }

        bool Cci.IModule.TrackDebugData
        {
            get { return _serializationProperties.TrackDebugData; }
        }

        Cci.ResourceSection Cci.IModule.Win32ResourceSection
        {
            get
            {
                return this.Win32ResourceSection;
            }
        }

        IEnumerable<Cci.IWin32Resource> Cci.IModule.Win32Resources
        {
            get
            {
                return this.Win32Resources;
            }
        }

        int Cci.IModule.HintNumberOfMethodDefinitions
        {
            get
            {
                return _methodBodyMap.Count;
            }
        }

        #endregion

        #region INamedEntity

        string Cci.INamedEntity.Name
        {
            get
            {
                return Name;
            }
        }

        #endregion

        #region IReference

        IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
        }

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            Debug.Assert(ReferenceEquals(context.Module, this));
            return this;
        }

        #endregion

        #region Debug Documents

        internal void AddDebugDocument(Cci.DebugSourceDocument document)
        {
            _debugDocuments.Add(document.Location, document);
        }

        internal Cci.DebugSourceDocument TryGetDebugDocument(string path, string basePath)
        {
            return TryGetDebugDocumentForNormalizedPath(NormalizeDebugDocumentPath(path, basePath));
        }

        internal Cci.DebugSourceDocument TryGetDebugDocumentForNormalizedPath(string normalizedPath)
        {
            Cci.DebugSourceDocument document;
            _debugDocuments.TryGetValue(normalizedPath, out document);
            return document;
        }

        internal Cci.DebugSourceDocument GetOrAddDebugDocument(string path, string basePath, Func<string, Cci.DebugSourceDocument> factory)
        {
            return _debugDocuments.GetOrAdd(NormalizeDebugDocumentPath(path, basePath), factory);
        }

        internal string NormalizeDebugDocumentPath(string path, string basePath)
        {
            var resolver = _compilation.Options.SourceReferenceResolver;
            if (resolver == null)
            {
                return path;
            }

            var key = ValueTuple.Create(path, basePath);
            string normalizedPath;
            if (!_normalizedPathsCache.TryGetValue(key, out normalizedPath))
            {
                normalizedPath = resolver.NormalizePath(path, basePath) ?? path;
                _normalizedPathsCache.TryAdd(key, normalizedPath);
            }

            return normalizedPath;
        }
        #endregion
    }
}
