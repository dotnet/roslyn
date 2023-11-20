// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal abstract class PEModuleBuilder : PEModuleBuilder<CSharpCompilation, SourceModuleSymbol, AssemblySymbol, TypeSymbol, NamedTypeSymbol, MethodSymbol, SyntaxNode, NoPia.EmbeddedTypesManager, ModuleCompilationState>
    {
        // TODO: Need to estimate amount of elements for this map and pass that value to the constructor.
        protected readonly ConcurrentDictionary<Symbol, Cci.IModuleReference> AssemblyOrModuleSymbolToModuleRefMap = new ConcurrentDictionary<Symbol, Cci.IModuleReference>();
        private readonly ConcurrentDictionary<Symbol, object> _genericInstanceMap = new ConcurrentDictionary<Symbol, object>(Symbols.SymbolEqualityComparer.ConsiderEverything);
        private readonly ConcurrentDictionary<ImportChain, ImmutableArray<Cci.UsedNamespaceOrType>> _translatedImportsMap =
                                                            new ConcurrentDictionary<ImportChain, ImmutableArray<Cci.UsedNamespaceOrType>>(ReferenceEqualityComparer.Instance);
        private readonly ConcurrentSet<TypeSymbol> _reportedErrorTypesMap = new ConcurrentSet<TypeSymbol>();

        private readonly NoPia.EmbeddedTypesManager _embeddedTypesManagerOpt;
        public override NoPia.EmbeddedTypesManager EmbeddedTypesManagerOpt
            => _embeddedTypesManagerOpt;

        // Gives the name of this module (may not reflect the name of the underlying symbol).
        // See Assembly.MetadataName.
        private readonly string _metadataName;

        private ImmutableArray<Cci.ExportedType> _lazyExportedTypes;

        /// <summary>
        /// The compiler-generated implementation type for each fixed-size buffer.
        /// </summary>
        private Dictionary<FieldSymbol, NamedTypeSymbol> _fixedImplementationTypes;

        private SynthesizedPrivateImplementationDetailsType _lazyPrivateImplementationDetailsClass;

        private int _needsGeneratedAttributes;
        private bool _needsGeneratedAttributes_IsFrozen;

        /// <summary>
        /// Returns a value indicating which embedded attributes should be generated during emit phase.
        /// The value is set during binding the symbols that need those attributes, and is frozen on first trial to get it.
        /// Freezing is needed to make sure that nothing tries to modify the value after the value is read.
        /// </summary>
        internal EmbeddableAttributes GetNeedsGeneratedAttributes()
        {
            _needsGeneratedAttributes_IsFrozen = true;
            return GetNeedsGeneratedAttributesInternal();
        }

        private EmbeddableAttributes GetNeedsGeneratedAttributesInternal()
        {
            return (EmbeddableAttributes)_needsGeneratedAttributes | Compilation.GetNeedsGeneratedAttributes();
        }

        private void SetNeedsGeneratedAttributes(EmbeddableAttributes attributes)
        {
            Debug.Assert(!_needsGeneratedAttributes_IsFrozen);
            ThreadSafeFlagOperations.Set(ref _needsGeneratedAttributes, (int)attributes);
        }

        internal PEModuleBuilder(
            SourceModuleSymbol sourceModule,
            EmitOptions emitOptions,
            OutputKind outputKind,
            Cci.ModulePropertiesForSerialization serializationProperties,
            IEnumerable<ResourceDescription> manifestResources)
            : base(sourceModule.ContainingSourceAssembly.DeclaringCompilation,
                   sourceModule,
                   serializationProperties,
                   manifestResources,
                   outputKind,
                   emitOptions,
                   new ModuleCompilationState())
        {
            var specifiedName = sourceModule.MetadataName;

            _metadataName = specifiedName != Microsoft.CodeAnalysis.Compilation.UnspecifiedModuleAssemblyName ?
                            specifiedName :
                            emitOptions.OutputNameOverride ?? specifiedName;

            AssemblyOrModuleSymbolToModuleRefMap.Add(sourceModule, this);

            if (sourceModule.AnyReferencedAssembliesAreLinked)
            {
                _embeddedTypesManagerOpt = new NoPia.EmbeddedTypesManager(this);
            }
        }

        public override string Name
        {
            get { return _metadataName; }
        }

        internal sealed override string ModuleName
        {
            get { return _metadataName; }
        }

        internal sealed override Cci.ICustomAttribute SynthesizeAttribute(WellKnownMember attributeConstructor)
        {
            return Compilation.TrySynthesizeAttribute(attributeConstructor);
        }

        public sealed override IEnumerable<Cci.ICustomAttribute> GetSourceAssemblyAttributes(bool isRefAssembly)
        {
            return SourceModule.ContainingSourceAssembly
                .GetCustomAttributesToEmit(this, isRefAssembly, emittingAssemblyAttributesInNetModule: OutputKind.IsNetModule());
        }

        public sealed override IEnumerable<Cci.SecurityAttribute> GetSourceAssemblySecurityAttributes()
        {
            return SourceModule.ContainingSourceAssembly.GetSecurityAttributes();
        }

        public sealed override IEnumerable<Cci.ICustomAttribute> GetSourceModuleAttributes()
        {
            return SourceModule.GetCustomAttributesToEmit(this);
        }

        internal sealed override AssemblySymbol CorLibrary
        {
            get { return SourceModule.ContainingSourceAssembly.CorLibrary; }
        }

        public sealed override bool GenerateVisualBasicStylePdb => false;

        // C# doesn't emit linked assembly names into PDBs.
        public sealed override IEnumerable<string> LinkedAssembliesDebugInfo => SpecializedCollections.EmptyEnumerable<string>();

        // C# currently doesn't emit compilation level imports (TODO: scripting).
        public sealed override ImmutableArray<Cci.UsedNamespaceOrType> GetImports() => ImmutableArray<Cci.UsedNamespaceOrType>.Empty;

        // C# doesn't allow to define default namespace for compilation.
        public sealed override string DefaultNamespace => null;

        protected sealed override IEnumerable<Cci.IAssemblyReference> GetAssemblyReferencesFromAddedModules(DiagnosticBag diagnostics)
        {
            ImmutableArray<ModuleSymbol> modules = SourceModule.ContainingAssembly.Modules;

            for (int i = 1; i < modules.Length; i++)
            {
                foreach (AssemblySymbol aRef in modules[i].GetReferencedAssemblySymbols())
                {
                    yield return Translate(aRef, diagnostics);
                }
            }
        }

        private void ValidateReferencedAssembly(AssemblySymbol assembly, AssemblyReference asmRef, DiagnosticBag diagnostics)
        {
            AssemblyIdentity asmIdentity = SourceModule.ContainingAssembly.Identity;
            AssemblyIdentity refIdentity = asmRef.Identity;

            if (asmIdentity.IsStrongName && !refIdentity.IsStrongName &&
                asmRef.Identity.ContentType != AssemblyContentType.WindowsRuntime)
            {
                // Dev12 reported error, we have changed it to a warning to allow referencing libraries
                // built for platforms that don't support strong names.
                diagnostics.Add(new CSDiagnosticInfo(ErrorCode.WRN_ReferencedAssemblyDoesNotHaveStrongName, assembly), NoLocation.Singleton);
            }

            if (OutputKind != OutputKind.NetModule &&
               !string.IsNullOrEmpty(refIdentity.CultureName) &&
               !string.Equals(refIdentity.CultureName, asmIdentity.CultureName, StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.Add(new CSDiagnosticInfo(ErrorCode.WRN_RefCultureMismatch, assembly, refIdentity.CultureName), NoLocation.Singleton);
            }

            var refMachine = assembly.Machine;
            // If other assembly is agnostic this is always safe
            // Also, if no mscorlib was specified for back compat we add a reference to mscorlib
            // that resolves to the current framework directory. If the compiler is 64-bit
            // this is a 64-bit mscorlib, which will produce a warning if /platform:x86 is
            // specified. A reference to the default mscorlib should always succeed without
            // warning so we ignore it here.
            if ((object)assembly != (object)assembly.CorLibrary &&
                !(refMachine == Machine.I386 && !assembly.Bit32Required))
            {
                var machine = SourceModule.Machine;

                if (!(machine == Machine.I386 && !SourceModule.Bit32Required) &&
                    machine != refMachine)
                {
                    // Different machine types, and neither is agnostic
                    diagnostics.Add(new CSDiagnosticInfo(ErrorCode.WRN_ConflictingMachineAssembly, assembly), NoLocation.Singleton);
                }
            }

            if (_embeddedTypesManagerOpt != null && _embeddedTypesManagerOpt.IsFrozen)
            {
                _embeddedTypesManagerOpt.ReportIndirectReferencesToLinkedAssemblies(assembly, diagnostics);
            }
        }

        internal sealed override IEnumerable<Cci.INestedTypeDefinition> GetSynthesizedNestedTypes(NamedTypeSymbol container)
        {
            return null;
        }

        public sealed override IEnumerable<(Cci.ITypeDefinition, ImmutableArray<Cci.DebugSourceDocument>)> GetTypeToDebugDocumentMap(EmitContext context)
        {
            var typesToProcess = ArrayBuilder<Cci.ITypeDefinition>.GetInstance();
            var debugDocuments = ArrayBuilder<Cci.DebugSourceDocument>.GetInstance();
            var methodDocumentList = PooledHashSet<Cci.DebugSourceDocument>.GetInstance();

            var namespacesAndTopLevelTypesToProcess = ArrayBuilder<NamespaceOrTypeSymbol>.GetInstance();
            namespacesAndTopLevelTypesToProcess.Push(SourceModule.GlobalNamespace);
            while (namespacesAndTopLevelTypesToProcess.Count > 0)
            {
                var symbol = namespacesAndTopLevelTypesToProcess.Pop();

                switch (symbol.Kind)
                {
                    case SymbolKind.Namespace:
                        var location = GetSmallestSourceLocationOrNull(symbol);

                        // filtering out synthesized symbols not having real source
                        // locations such as anonymous types, etc...
                        if (location != null)
                        {
                            foreach (var member in symbol.GetMembers())
                            {
                                switch (member.Kind)
                                {
                                    case SymbolKind.Namespace:
                                    case SymbolKind.NamedType:
                                        namespacesAndTopLevelTypesToProcess.Push((NamespaceOrTypeSymbol)member);
                                        break;
                                    default:
                                        throw ExceptionUtilities.UnexpectedValue(member.Kind);
                                }
                            }
                        }
                        break;
                    case SymbolKind.NamedType:
                        Debug.Assert(debugDocuments.Count == 0);
                        Debug.Assert(methodDocumentList.Count == 0);
                        Debug.Assert(typesToProcess.Count == 0);

                        var typeDefinition = (Cci.ITypeDefinition)symbol.GetCciAdapter();
                        typesToProcess.Push(typeDefinition);
                        GetDocumentsForMethodsAndNestedTypes(methodDocumentList, typesToProcess, context);

                        foreach (var loc in symbol.Locations)
                        {
                            if (!loc.IsInSource)
                            {
                                continue;
                            }

                            var span = loc.GetLineSpan();
                            var debugDocument = DebugDocumentsBuilder.TryGetDebugDocument(span.Path, basePath: null);

                            // If we have a debug document that is already referenced by method debug info in this type, or a nested type,
                            // then we don't need to include it. Since its impossible to declare a nested type without also including
                            // a declaration for its containing type, we don't need to consider nested types in this method itself.
                            if (debugDocument is not null && !methodDocumentList.Contains(debugDocument))
                            {
                                debugDocuments.Add(debugDocument);
                            }
                        }

                        if (debugDocuments.Count > 0)
                        {
                            yield return (typeDefinition, debugDocuments.ToImmutable());
                        }

                        debugDocuments.Clear();
                        methodDocumentList.Clear();
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
                }
            }

            namespacesAndTopLevelTypesToProcess.Free();
            debugDocuments.Free();
            methodDocumentList.Free();
            typesToProcess.Free();
        }

        /// <summary>
        /// Gets a list of documents from the method definitions in the types in <paramref name="typesToProcess"/> or any
        /// nested types of those types.
        /// </summary>
        private static void GetDocumentsForMethodsAndNestedTypes(PooledHashSet<Cci.DebugSourceDocument> documentList, ArrayBuilder<Cci.ITypeDefinition> typesToProcess, EmitContext context)
        {
            Debug.Assert(!context.MetadataOnly);

            while (typesToProcess.Count > 0)
            {
                var definition = typesToProcess.Pop();

                var typeMethods = definition.GetMethods(context);
                foreach (var method in typeMethods)
                {
                    var body = method.GetBody(context);
                    if (body is null)
                    {
                        continue;
                    }

                    foreach (var point in body.SequencePoints)
                    {
                        documentList.Add(point.Document);
                    }
                }

                var nestedTypes = definition.GetNestedTypes(context);
                foreach (var nestedTypeDefinition in nestedTypes)
                {
                    typesToProcess.Push(nestedTypeDefinition);
                }
            }
        }

        public sealed override MultiDictionary<Cci.DebugSourceDocument, Cci.DefinitionWithLocation> GetSymbolToLocationMap()
        {
            var result = new MultiDictionary<Cci.DebugSourceDocument, Cci.DefinitionWithLocation>();

            var namespacesAndTypesToProcess = new Stack<NamespaceOrTypeSymbol>();
            namespacesAndTypesToProcess.Push(SourceModule.GlobalNamespace);

            Location location = null;

            while (namespacesAndTypesToProcess.Count > 0)
            {
                NamespaceOrTypeSymbol symbol = namespacesAndTypesToProcess.Pop();
                switch (symbol.Kind)
                {
                    case SymbolKind.Namespace:
                        location = GetSmallestSourceLocationOrNull(symbol);

                        // filtering out synthesized symbols not having real source
                        // locations such as anonymous types, etc...
                        if (location != null)
                        {
                            foreach (var member in symbol.GetMembers())
                            {
                                switch (member.Kind)
                                {
                                    case SymbolKind.Namespace:
                                    case SymbolKind.NamedType:
                                        namespacesAndTypesToProcess.Push((NamespaceOrTypeSymbol)member);
                                        break;

                                    default:
                                        throw ExceptionUtilities.UnexpectedValue(member.Kind);
                                }
                            }
                        }
                        break;

                    case SymbolKind.NamedType:
                        location = GetSmallestSourceLocationOrNull(symbol);
                        if (location != null)
                        {
                            //  add this named type location
                            AddSymbolLocation(result, location, (Cci.IDefinition)symbol.GetCciAdapter());

                            foreach (var member in symbol.GetMembers())
                            {
                                switch (member.Kind)
                                {
                                    case SymbolKind.NamedType:
                                        namespacesAndTypesToProcess.Push((NamespaceOrTypeSymbol)member);
                                        break;

                                    case SymbolKind.Method:
                                        // NOTE: Dev11 does not add synthesized static constructors to this map,
                                        //       but adds synthesized instance constructors, Roslyn adds both
                                        var method = (MethodSymbol)member;
                                        if (!method.ShouldEmit())
                                        {
                                            break;
                                        }

                                        AddSymbolLocation(result, member);
                                        break;

                                    case SymbolKind.Property:
                                        AddSymbolLocation(result, member);
                                        break;
                                    case SymbolKind.Field:
                                        if (member is TupleErrorFieldSymbol)
                                        {
                                            break;
                                        }

                                        // NOTE: Dev11 does not add synthesized backing fields for properties,
                                        //       but adds backing fields for events, Roslyn adds both
                                        AddSymbolLocation(result, member);
                                        break;

                                    case SymbolKind.Event:
                                        AddSymbolLocation(result, member);
                                        //  event backing fields do not show up in GetMembers
                                        {
                                            FieldSymbol field = ((EventSymbol)member).AssociatedField;
                                            if ((object)field != null)
                                            {
                                                AddSymbolLocation(result, field);
                                            }
                                        }
                                        break;

                                    default:
                                        throw ExceptionUtilities.UnexpectedValue(member.Kind);
                                }
                            }
                        }
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
                }
            }

            return result;
        }

        private void AddSymbolLocation(MultiDictionary<Cci.DebugSourceDocument, Cci.DefinitionWithLocation> result, Symbol symbol)
        {
            var location = GetSmallestSourceLocationOrNull(symbol);
            if (location != null)
            {
                AddSymbolLocation(result, location, (Cci.IDefinition)symbol.GetCciAdapter());
            }
        }

        private void AddSymbolLocation(MultiDictionary<Cci.DebugSourceDocument, Cci.DefinitionWithLocation> result, Location location, Cci.IDefinition definition)
        {
            FileLinePositionSpan span = location.GetLineSpan();

            Cci.DebugSourceDocument doc = DebugDocumentsBuilder.TryGetDebugDocument(span.Path, basePath: location.SourceTree.FilePath);

            if (doc != null)
            {
                result.Add(doc,
                           new Cci.DefinitionWithLocation(
                               definition,
                               span.StartLinePosition.Line,
                               span.StartLinePosition.Character,
                               span.EndLinePosition.Line,
                               span.EndLinePosition.Character));
            }
        }

        private Location GetSmallestSourceLocationOrNull(Symbol symbol)
        {
            CSharpCompilation compilation = symbol.DeclaringCompilation;
            Debug.Assert(Compilation == compilation, "How did we get symbol from different compilation?");

            Location result = null;
            foreach (var loc in symbol.Locations)
            {
                if (loc.IsInSource && (result == null || compilation.CompareSourceLocations(result, loc) > 0))
                {
                    result = loc;
                }
            }

            return result;
        }

        /// <summary>
        /// Ignore accessibility when resolving well-known type
        /// members, in particular for generic type arguments
        /// (e.g.: binding to internal types in the EE).
        /// </summary>
        internal virtual bool IgnoreAccessibility => false;

        /// <summary>
        /// Override the dynamic operation context type for all dynamic calls in the module.
        /// </summary>
        internal virtual NamedTypeSymbol GetDynamicOperationContextType(NamedTypeSymbol contextType)
        {
            return contextType;
        }

        internal virtual VariableSlotAllocator TryCreateVariableSlotAllocator(MethodSymbol method, MethodSymbol topLevelMethod, DiagnosticBag diagnostics)
        {
            return null;
        }

        internal virtual MethodInstrumentation GetMethodBodyInstrumentations(MethodSymbol method)
            => new MethodInstrumentation { Kinds = EmitOptions.InstrumentationKinds };

        internal virtual ImmutableArray<AnonymousTypeKey> GetPreviousAnonymousTypes()
        {
            return ImmutableArray<AnonymousTypeKey>.Empty;
        }

        internal virtual ImmutableArray<SynthesizedDelegateKey> GetPreviousAnonymousDelegates()
        {
            return ImmutableArray<SynthesizedDelegateKey>.Empty;
        }

        internal virtual int GetNextAnonymousTypeIndex()
        {
            return 0;
        }

        internal virtual int GetNextAnonymousDelegateIndex()
        {
            return 0;
        }

        internal virtual bool TryGetPreviousAnonymousTypeValue(AnonymousTypeManager.AnonymousTypeOrDelegateTemplateSymbol template, out AnonymousTypeValue typeValue)
        {
            Debug.Assert(Compilation == template.DeclaringCompilation);

            typeValue = default;
            return false;
        }

        internal virtual bool TryGetAnonymousDelegateValue(AnonymousTypeManager.AnonymousDelegateTemplateSymbol template, out SynthesizedDelegateValue delegateValue)
        {
            Debug.Assert(Compilation == template.DeclaringCompilation);

            delegateValue = default;
            return false;
        }

        public sealed override IEnumerable<Cci.INamespaceTypeDefinition> GetAnonymousTypeDefinitions(EmitContext context)
        {
            if (context.MetadataOnly)
            {
                return SpecializedCollections.EmptyEnumerable<Cci.INamespaceTypeDefinition>();
            }

            return Compilation.AnonymousTypeManager.GetAllCreatedTemplates()
#if DEBUG
                   .Select(type => type.GetCciAdapter())

#endif
                   ;
        }

        public override IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelSourceTypeDefinitions(EmitContext context)
        {
            var namespacesToProcess = new Stack<NamespaceSymbol>();
            namespacesToProcess.Push(SourceModule.GlobalNamespace);

            while (namespacesToProcess.Count > 0)
            {
                var ns = namespacesToProcess.Pop();
                foreach (var member in ns.GetMembers())
                {
                    if (member.Kind == SymbolKind.Namespace)
                    {
                        namespacesToProcess.Push((NamespaceSymbol)member);
                    }
                    else
                    {
                        yield return ((NamedTypeSymbol)member).GetCciAdapter();
                    }
                }
            }
        }

        private static void GetExportedTypes(NamespaceOrTypeSymbol symbol, int parentIndex, ArrayBuilder<Cci.ExportedType> builder)
        {
            int index;
            if (symbol.Kind == SymbolKind.NamedType)
            {
                if (symbol.DeclaredAccessibility != Accessibility.Public)
                {
                    return;
                }

                Debug.Assert(symbol.IsDefinition);
                index = builder.Count;
                builder.Add(new Cci.ExportedType((Cci.ITypeReference)symbol.GetCciAdapter(), parentIndex, isForwarder: false));
            }
            else
            {
                index = -1;
            }

            foreach (var member in symbol.GetMembers())
            {
                var namespaceOrType = member as NamespaceOrTypeSymbol;
                if ((object)namespaceOrType != null)
                {
                    GetExportedTypes(namespaceOrType, index, builder);
                }
            }
        }

        public sealed override ImmutableArray<Cci.ExportedType> GetExportedTypes(DiagnosticBag diagnostics)
        {
            Debug.Assert(HaveDeterminedTopLevelTypes);

            if (_lazyExportedTypes.IsDefault)
            {
                _lazyExportedTypes = CalculateExportedTypes();

                if (_lazyExportedTypes.Length > 0)
                {
                    ReportExportedTypeNameCollisions(_lazyExportedTypes, diagnostics);
                }
            }

            return _lazyExportedTypes;
        }

        /// <summary>
        /// Builds an array of public type symbols defined in netmodules included in the compilation
        /// and type forwarders defined in this compilation or any included netmodule (in this order).
        /// </summary>
        private ImmutableArray<Cci.ExportedType> CalculateExportedTypes()
        {
            SourceAssemblySymbol sourceAssembly = SourceModule.ContainingSourceAssembly;
            var builder = ArrayBuilder<Cci.ExportedType>.GetInstance();

            if (!OutputKind.IsNetModule())
            {
                var modules = sourceAssembly.Modules;
                for (int i = 1; i < modules.Length; i++) //NOTE: skipping modules[0]
                {
                    GetExportedTypes(modules[i].GlobalNamespace, -1, builder);
                }
            }

            Debug.Assert(OutputKind.IsNetModule() == sourceAssembly.DeclaringCompilation.Options.OutputKind.IsNetModule());
            GetForwardedTypes(sourceAssembly, builder);

            return builder.ToImmutableAndFree();
        }

#nullable enable
        /// <summary>
        /// Returns a set of top-level forwarded types
        /// </summary>
        internal static HashSet<NamedTypeSymbol> GetForwardedTypes(SourceAssemblySymbol sourceAssembly, ArrayBuilder<Cci.ExportedType>? builder)
        {
            var seenTopLevelForwardedTypes = new HashSet<NamedTypeSymbol>();
            GetForwardedTypes(seenTopLevelForwardedTypes, sourceAssembly.GetSourceDecodedWellKnownAttributeData(), builder);

            if (!sourceAssembly.DeclaringCompilation.Options.OutputKind.IsNetModule())
            {
                GetForwardedTypes(seenTopLevelForwardedTypes, sourceAssembly.GetNetModuleDecodedWellKnownAttributeData(), builder);
            }

            return seenTopLevelForwardedTypes;
        }
#nullable disable

        private void ReportExportedTypeNameCollisions(ImmutableArray<Cci.ExportedType> exportedTypes, DiagnosticBag diagnostics)
        {
            var sourceAssembly = SourceModule.ContainingSourceAssembly;
            var exportedNamesMap = new Dictionary<string, NamedTypeSymbol>(StringOrdinalComparer.Instance);

            foreach (var exportedType in exportedTypes)
            {
                var type = (NamedTypeSymbol)exportedType.Type.GetInternalSymbol();

                Debug.Assert(type.IsDefinition);

                if (!type.IsTopLevelType())
                {
                    continue;
                }

                // exported types are not emitted in EnC deltas (hence generation 0):
                string fullEmittedName = MetadataHelpers.BuildQualifiedName(
                    ((Cci.INamespaceTypeReference)type.GetCciAdapter()).NamespaceName,
                    Cci.MetadataWriter.GetMetadataName(type.GetCciAdapter(), generation: 0));

                // First check against types declared in the primary module
                if (ContainsTopLevelType(fullEmittedName))
                {
                    if ((object)type.ContainingAssembly == sourceAssembly)
                    {
                        diagnostics.Add(ErrorCode.ERR_ExportedTypeConflictsWithDeclaration, NoLocation.Singleton, type, type.ContainingModule);
                    }
                    else
                    {
                        diagnostics.Add(ErrorCode.ERR_ForwardedTypeConflictsWithDeclaration, NoLocation.Singleton, type);
                    }

                    continue;
                }

                NamedTypeSymbol contender;

                // Now check against other exported types
                if (exportedNamesMap.TryGetValue(fullEmittedName, out contender))
                {
                    if ((object)type.ContainingAssembly == sourceAssembly)
                    {
                        // all exported types precede forwarded types, therefore contender cannot be a forwarded type.
                        Debug.Assert(contender.ContainingAssembly == sourceAssembly);

                        diagnostics.Add(ErrorCode.ERR_ExportedTypesConflict, NoLocation.Singleton, type, type.ContainingModule, contender, contender.ContainingModule);
                    }
                    else if ((object)contender.ContainingAssembly == sourceAssembly)
                    {
                        // Forwarded type conflicts with exported type
                        diagnostics.Add(ErrorCode.ERR_ForwardedTypeConflictsWithExportedType, NoLocation.Singleton, type, type.ContainingAssembly, contender, contender.ContainingModule);
                    }
                    else
                    {
                        // Forwarded type conflicts with another forwarded type
                        diagnostics.Add(ErrorCode.ERR_ForwardedTypesConflict, NoLocation.Singleton, type, type.ContainingAssembly, contender, contender.ContainingAssembly);
                    }

                    continue;
                }

                exportedNamesMap.Add(fullEmittedName, type);
            }
        }

#nullable enable
        private static void GetForwardedTypes(
            HashSet<NamedTypeSymbol> seenTopLevelTypes,
            CommonAssemblyWellKnownAttributeData<NamedTypeSymbol> wellKnownAttributeData,
            ArrayBuilder<Cci.ExportedType>? builder)
        {
            if (wellKnownAttributeData?.ForwardedTypes?.Count > 0)
            {
                // (type, index of the parent exported type in builder, or -1 if the type is a top-level type)
                var stack = ArrayBuilder<(NamedTypeSymbol type, int parentIndex)>.GetInstance();

                // Hashset enumeration is not guaranteed to be deterministic. Emitting in the order of fully qualified names.
                IEnumerable<NamedTypeSymbol> orderedForwardedTypes = wellKnownAttributeData.ForwardedTypes;

                if (builder is object)
                {
                    orderedForwardedTypes = orderedForwardedTypes.OrderBy(t => t.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat));
                }

                foreach (NamedTypeSymbol forwardedType in orderedForwardedTypes)
                {
                    NamedTypeSymbol originalDefinition = forwardedType.OriginalDefinition;
                    Debug.Assert((object)originalDefinition.ContainingType == null, "How did a nested type get forwarded?");

                    // Since we need to allow multiple constructions of the same generic type at the source
                    // level, we need to de-dup the original definitions before emitting.
                    if (!seenTopLevelTypes.Add(originalDefinition)) continue;

                    if (builder is object)
                    {
                        // Return all nested types.
                        // Note the order: depth first, children in reverse order (to match dev10, not a requirement).
                        Debug.Assert(stack.Count == 0);
                        stack.Push((originalDefinition, -1));

                        while (stack.Count > 0)
                        {
                            var (type, parentIndex) = stack.Pop();

                            // In general, we don't want private types to appear in the ExportedTypes table.
                            // BREAK: dev11 emits these types.  The problem was discovered in dev10, but failed
                            // to meet the bar Bug: Dev10/258038 and was left as-is.
                            if (type.DeclaredAccessibility == Accessibility.Private)
                            {
                                // NOTE: this will also exclude nested types of type
                                continue;
                            }

                            // NOTE: not bothering to put nested types in seenTypes - the top-level type is adequate protection.

                            int index = builder.Count;
                            builder.Add(new Cci.ExportedType(type.GetCciAdapter(), parentIndex, isForwarder: true));

                            // Iterate backwards so they get popped in forward order.
                            ImmutableArray<NamedTypeSymbol> nested = type.GetTypeMembers(); // Ordered.
                            for (int i = nested.Length - 1; i >= 0; i--)
                            {
                                stack.Push((nested[i], index));
                            }
                        }
                    }
                }

                stack.Free();
            }
        }
#nullable disable

        internal IEnumerable<AssemblySymbol> GetReferencedAssembliesUsedSoFar()
        {
            foreach (AssemblySymbol a in SourceModule.GetReferencedAssemblySymbols())
            {
                if (!a.IsLinked && !a.IsMissing && AssemblyOrModuleSymbolToModuleRefMap.ContainsKey(a))
                {
                    yield return a;
                }
            }
        }

        private NamedTypeSymbol GetUntranslatedSpecialType(SpecialType specialType, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            Debug.Assert(diagnostics != null);

            var typeSymbol = SourceModule.ContainingAssembly.GetSpecialType(specialType);

            DiagnosticInfo info = typeSymbol.GetUseSiteInfo().DiagnosticInfo;
            if (info != null)
            {
                Symbol.ReportUseSiteDiagnostic(info,
                                               diagnostics,
                                               syntaxNodeOpt != null ? syntaxNodeOpt.Location : NoLocation.Singleton);
            }

            return typeSymbol;
        }

        internal sealed override Cci.INamedTypeReference GetSpecialType(SpecialType specialType, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            return Translate(GetUntranslatedSpecialType(specialType, syntaxNodeOpt, diagnostics),
                             diagnostics: diagnostics,
                             syntaxNodeOpt: syntaxNodeOpt,
                             needDeclaration: true);
        }

        public sealed override Cci.IMethodReference GetInitArrayHelper()
        {
            return ((MethodSymbol)Compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__InitializeArrayArrayRuntimeFieldHandle))?.GetCciAdapter();
        }

        public sealed override bool IsPlatformType(Cci.ITypeReference typeRef, Cci.PlatformType platformType)
        {
            var namedType = typeRef.GetInternalSymbol() as NamedTypeSymbol;
            if ((object)namedType != null)
            {
                if (platformType == Cci.PlatformType.SystemType)
                {
                    return (object)namedType == (object)Compilation.GetWellKnownType(WellKnownType.System_Type);
                }

                return namedType.SpecialType == (SpecialType)platformType;
            }

            return false;
        }

        protected sealed override Cci.IAssemblyReference GetCorLibraryReferenceToEmit(CodeAnalysis.Emit.EmitContext context)
        {
            AssemblySymbol corLibrary = CorLibrary;

            if (!corLibrary.IsMissing &&
                !corLibrary.IsLinked &&
                !ReferenceEquals(corLibrary, SourceModule.ContainingAssembly))
            {
                return Translate(corLibrary, context.Diagnostics);
            }

            return null;
        }

        internal sealed override Cci.IAssemblyReference Translate(AssemblySymbol assembly, DiagnosticBag diagnostics)
        {
            if (ReferenceEquals(SourceModule.ContainingAssembly, assembly))
            {
                return (Cci.IAssemblyReference)this;
            }

            Cci.IModuleReference reference;

            if (AssemblyOrModuleSymbolToModuleRefMap.TryGetValue(assembly, out reference))
            {
                return (Cci.IAssemblyReference)reference;
            }

            AssemblyReference asmRef = new AssemblyReference(assembly);

            AssemblyReference cachedAsmRef = (AssemblyReference)AssemblyOrModuleSymbolToModuleRefMap.GetOrAdd(assembly, asmRef);

            if (cachedAsmRef == asmRef)
            {
                ValidateReferencedAssembly(assembly, cachedAsmRef, diagnostics);
            }

            // TryAdd because whatever is associated with assembly should be associated with Modules[0]
            AssemblyOrModuleSymbolToModuleRefMap.TryAdd(assembly.Modules[0], cachedAsmRef);

            return cachedAsmRef;
        }

        internal Cci.IModuleReference Translate(ModuleSymbol module, DiagnosticBag diagnostics)
        {
            if (ReferenceEquals(SourceModule, module))
            {
                return this;
            }

            if ((object)module == null)
            {
                return null;
            }

            Cci.IModuleReference moduleRef;

            if (AssemblyOrModuleSymbolToModuleRefMap.TryGetValue(module, out moduleRef))
            {
                return moduleRef;
            }

            moduleRef = TranslateModule(module, diagnostics);
            moduleRef = AssemblyOrModuleSymbolToModuleRefMap.GetOrAdd(module, moduleRef);

            return moduleRef;
        }

        protected virtual Cci.IModuleReference TranslateModule(ModuleSymbol module, DiagnosticBag diagnostics)
        {
            AssemblySymbol container = module.ContainingAssembly;

            if ((object)container != null && ReferenceEquals(container.Modules[0], module))
            {
                Cci.IModuleReference moduleRef = new AssemblyReference(container);
                Cci.IModuleReference cachedModuleRef = AssemblyOrModuleSymbolToModuleRefMap.GetOrAdd(container, moduleRef);

                if (cachedModuleRef == moduleRef)
                {
                    ValidateReferencedAssembly(container, (AssemblyReference)moduleRef, diagnostics);
                }
                else
                {
                    moduleRef = cachedModuleRef;
                }

                return moduleRef;
            }
            else
            {
                return new ModuleReference(this, module);
            }
        }

        internal Cci.INamedTypeReference Translate(
            NamedTypeSymbol namedTypeSymbol,
            SyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics,
            bool fromImplements = false,
            bool needDeclaration = false)
        {
            Debug.Assert(namedTypeSymbol.IsDefinitionOrDistinct());
            Debug.Assert(diagnostics != null);

            // Anonymous type being translated
            if (namedTypeSymbol.IsAnonymousType)
            {
                Debug.Assert(!needDeclaration);
                namedTypeSymbol = AnonymousTypeManager.TranslateAnonymousTypeSymbol(namedTypeSymbol);
            }
            else if (namedTypeSymbol.IsTupleType)
            {
                CheckTupleUnderlyingType(namedTypeSymbol, syntaxNodeOpt, diagnostics);
            }

            // Substitute error types with a special singleton object.
            // Unreported bad types can come through NoPia embedding, for example.
            if (namedTypeSymbol.OriginalDefinition.Kind == SymbolKind.ErrorType)
            {
                ErrorTypeSymbol errorType = (ErrorTypeSymbol)namedTypeSymbol.OriginalDefinition;
                DiagnosticInfo diagInfo = errorType.GetUseSiteInfo().DiagnosticInfo ?? errorType.ErrorInfo;

                if (diagInfo == null && namedTypeSymbol.Kind == SymbolKind.ErrorType)
                {
                    errorType = (ErrorTypeSymbol)namedTypeSymbol;
                    diagInfo = errorType.GetUseSiteInfo().DiagnosticInfo ?? errorType.ErrorInfo;
                }

                // Try to decrease noise by not complaining about the same type over and over again.
                if (_reportedErrorTypesMap.Add(errorType))
                {
                    diagnostics.Add(new CSDiagnostic(diagInfo ?? new CSDiagnosticInfo(ErrorCode.ERR_BogusType, string.Empty), syntaxNodeOpt == null ? NoLocation.Singleton : syntaxNodeOpt.Location));
                }

                return CodeAnalysis.Emit.ErrorType.Singleton;
            }

            if (!namedTypeSymbol.IsDefinition)
            {
                // generic instantiation for sure
                Debug.Assert(!needDeclaration);

                if (namedTypeSymbol.IsUnboundGenericType)
                {
                    namedTypeSymbol = namedTypeSymbol.OriginalDefinition;
                }
                else
                {
                    return (Cci.INamedTypeReference)GetCciAdapter(namedTypeSymbol);
                }
            }
            else if (!needDeclaration)
            {
                object reference;
                Cci.INamedTypeReference typeRef;

                NamedTypeSymbol container = namedTypeSymbol.ContainingType;

                if (namedTypeSymbol.Arity > 0)
                {
                    if (_genericInstanceMap.TryGetValue(namedTypeSymbol, out reference))
                    {
                        return (Cci.INamedTypeReference)reference;
                    }

                    if ((object)container != null)
                    {
                        if (IsGenericType(container))
                        {
                            // Container is a generic instance too.
                            typeRef = new SpecializedGenericNestedTypeInstanceReference(namedTypeSymbol);
                        }
                        else
                        {
                            typeRef = new GenericNestedTypeInstanceReference(namedTypeSymbol);
                        }
                    }
                    else
                    {
                        typeRef = new GenericNamespaceTypeInstanceReference(namedTypeSymbol);
                    }

                    typeRef = (Cci.INamedTypeReference)_genericInstanceMap.GetOrAdd(namedTypeSymbol, typeRef);

                    return typeRef;
                }
                else if (IsGenericType(container))
                {
                    Debug.Assert((object)container != null);

                    if (_genericInstanceMap.TryGetValue(namedTypeSymbol, out reference))
                    {
                        return (Cci.INamedTypeReference)reference;
                    }

                    typeRef = new SpecializedNestedTypeReference(namedTypeSymbol);
                    typeRef = (Cci.INamedTypeReference)_genericInstanceMap.GetOrAdd(namedTypeSymbol, typeRef);

                    return typeRef;
                }
                else if (namedTypeSymbol.NativeIntegerUnderlyingType is NamedTypeSymbol underlyingType)
                {
                    namedTypeSymbol = underlyingType;
                }
            }

            // NoPia: See if this is a type, which definition we should copy into our assembly.
            Debug.Assert(namedTypeSymbol.IsDefinition);

            return _embeddedTypesManagerOpt?.EmbedTypeIfNeedTo(namedTypeSymbol, fromImplements, syntaxNodeOpt, diagnostics) ?? namedTypeSymbol.GetCciAdapter();
        }

        private object GetCciAdapter(Symbol symbol)
        {
            return _genericInstanceMap.GetOrAdd(symbol, s => s.GetCciAdapter());
        }

        private void CheckTupleUnderlyingType(NamedTypeSymbol namedTypeSymbol, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            // check that underlying type of a ValueTuple is indeed a value type (or error)
            // this should never happen, in theory,
            // but if it does happen we should make it a failure.
            // NOTE: declaredBase could be null for interfaces
            var declaredBase = namedTypeSymbol.BaseTypeNoUseSiteDiagnostics;
            if ((object)declaredBase != null && declaredBase.SpecialType == SpecialType.System_ValueType)
            {
                return;
            }

            // Try to decrease noise by not complaining about the same type over and over again.
            if (!_reportedErrorTypesMap.Add(namedTypeSymbol))
            {
                return;
            }

            var location = syntaxNodeOpt == null ? NoLocation.Singleton : syntaxNodeOpt.Location;
            if ((object)declaredBase != null)
            {
                var diagnosticInfo = declaredBase.GetUseSiteInfo().DiagnosticInfo;
                if (diagnosticInfo != null && diagnosticInfo.Severity == DiagnosticSeverity.Error)
                {
                    diagnostics.Add(diagnosticInfo, location);
                    return;
                }
            }

            diagnostics.Add(
                new CSDiagnostic(
                    new CSDiagnosticInfo(ErrorCode.ERR_PredefinedValueTupleTypeMustBeStruct, namedTypeSymbol.MetadataName),
                    location));
        }

        public static bool IsGenericType(NamedTypeSymbol toCheck)
        {
            while ((object)toCheck != null)
            {
                if (toCheck.Arity > 0)
                {
                    return true;
                }

                toCheck = toCheck.ContainingType;
            }

            return false;
        }

        internal static Cci.IGenericParameterReference Translate(TypeParameterSymbol param)
        {
            if (!param.IsDefinition)
                throw new InvalidOperationException(string.Format(CSharpResources.GenericParameterDefinition, param.Name));

            return param.GetCciAdapter();
        }

        internal sealed override Cci.ITypeReference Translate(
            TypeSymbol typeSymbol,
            SyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(diagnostics != null);

            switch (typeSymbol.Kind)
            {
                case SymbolKind.DynamicType:
                    return Translate(syntaxNodeOpt, diagnostics);

                case SymbolKind.ArrayType:
                    return Translate((ArrayTypeSymbol)typeSymbol);

                case SymbolKind.ErrorType:
                case SymbolKind.NamedType:
                    return Translate((NamedTypeSymbol)typeSymbol, syntaxNodeOpt, diagnostics);

                case SymbolKind.PointerType:
                    return Translate((PointerTypeSymbol)typeSymbol);

                case SymbolKind.TypeParameter:
                    return Translate((TypeParameterSymbol)typeSymbol);

                case SymbolKind.FunctionPointerType:
                    return Translate((FunctionPointerTypeSymbol)typeSymbol);
            }

            throw ExceptionUtilities.UnexpectedValue(typeSymbol.Kind);
        }

        internal Cci.IFieldReference Translate(
            FieldSymbol fieldSymbol,
            SyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics,
            bool needDeclaration = false)
        {
            Debug.Assert(fieldSymbol.IsDefinitionOrDistinct());
            Debug.Assert(!fieldSymbol.IsVirtualTupleField &&
                (object)(fieldSymbol.TupleUnderlyingField ?? fieldSymbol) == fieldSymbol &&
                fieldSymbol is not TupleErrorFieldSymbol, "tuple fields should be rewritten to underlying by now");

            if (!fieldSymbol.IsDefinition)
            {
                Debug.Assert(!needDeclaration);

                return (Cci.IFieldReference)GetCciAdapter(fieldSymbol);
            }
            else if (!needDeclaration && IsGenericType(fieldSymbol.ContainingType))
            {
                object reference;
                Cci.IFieldReference fieldRef;

                if (_genericInstanceMap.TryGetValue(fieldSymbol, out reference))
                {
                    return (Cci.IFieldReference)reference;
                }

                fieldRef = new SpecializedFieldReference(fieldSymbol);
                fieldRef = (Cci.IFieldReference)_genericInstanceMap.GetOrAdd(fieldSymbol, fieldRef);

                return fieldRef;
            }

            return _embeddedTypesManagerOpt?.EmbedFieldIfNeedTo(fieldSymbol.GetCciAdapter(), syntaxNodeOpt, diagnostics) ?? fieldSymbol.GetCciAdapter();
        }

        internal sealed override Cci.IMethodReference Translate(MethodSymbol symbol, DiagnosticBag diagnostics, bool needDeclaration)
        {
            return Translate(symbol, null, diagnostics, null, needDeclaration);
        }

        internal Cci.IMethodReference Translate(
            MethodSymbol methodSymbol,
            SyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics,
            BoundArgListOperator optArgList = null,
            bool needDeclaration = false)
        {
            Debug.Assert(!methodSymbol.IsDefaultValueTypeConstructor());
            Debug.Assert(optArgList == null || (methodSymbol.IsVararg && !needDeclaration));

            Cci.IMethodReference unexpandedMethodRef = Translate(methodSymbol, syntaxNodeOpt, diagnostics, needDeclaration);

            if (optArgList != null && optArgList.Arguments.Length > 0)
            {
                Cci.IParameterTypeInformation[] @params = new Cci.IParameterTypeInformation[optArgList.Arguments.Length];
                int ordinal = methodSymbol.ParameterCount;

                for (int i = 0; i < @params.Length; i++)
                {
                    @params[i] = new ArgListParameterTypeInformation(ordinal,
                                                                    !optArgList.ArgumentRefKindsOpt.IsDefaultOrEmpty && optArgList.ArgumentRefKindsOpt[i] != RefKind.None,
                                                                    Translate(optArgList.Arguments[i].Type, syntaxNodeOpt, diagnostics));
                    ordinal++;
                }

                return new ExpandedVarargsMethodReference(unexpandedMethodRef, @params.AsImmutableOrNull());
            }
            else
            {
                return unexpandedMethodRef;
            }
        }

#nullable enable
        private Cci.IMethodReference Translate(
            MethodSymbol methodSymbol,
            SyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics,
            bool needDeclaration)
        {
            object? reference;
            Cci.IMethodReference methodRef;
            NamedTypeSymbol container = methodSymbol.ContainingType;

            // Method of anonymous type being translated
            if (container?.IsAnonymousType == true)
            {
                Debug.Assert(!needDeclaration);
                methodSymbol = AnonymousTypeManager.TranslateAnonymousTypeMethodSymbol(methodSymbol);
            }

            Debug.Assert(methodSymbol.IsDefinitionOrDistinct());

            if (!methodSymbol.IsDefinition)
            {
                Debug.Assert(!needDeclaration);
                Debug.Assert(!(methodSymbol.OriginalDefinition is NativeIntegerMethodSymbol));
                Debug.Assert(!(methodSymbol.ConstructedFrom is NativeIntegerMethodSymbol));

                return (Cci.IMethodReference)GetCciAdapter(methodSymbol);
            }
            else if (!needDeclaration)
            {
                bool methodIsGeneric = methodSymbol.IsGenericMethod;
                bool typeIsGeneric = IsGenericType(container);

                if (methodIsGeneric || typeIsGeneric)
                {
                    if (_genericInstanceMap.TryGetValue(methodSymbol, out reference))
                    {
                        return (Cci.IMethodReference)reference;
                    }

                    if (methodIsGeneric)
                    {
                        if (typeIsGeneric)
                        {
                            // Specialized and generic instance at the same time.
                            methodRef = new SpecializedGenericMethodInstanceReference(methodSymbol);
                        }
                        else
                        {
                            methodRef = new GenericMethodInstanceReference(methodSymbol);
                        }
                    }
                    else
                    {
                        Debug.Assert(typeIsGeneric);
                        methodRef = new SpecializedMethodReference(methodSymbol);
                    }

                    methodRef = (Cci.IMethodReference)_genericInstanceMap.GetOrAdd(methodSymbol, methodRef);

                    return methodRef;
                }
                else if (methodSymbol is NativeIntegerMethodSymbol { UnderlyingMethod: MethodSymbol underlyingMethod })
                {
                    methodSymbol = underlyingMethod;
                }
            }

            if (_embeddedTypesManagerOpt != null)
            {
                return _embeddedTypesManagerOpt.EmbedMethodIfNeedTo(methodSymbol.GetCciAdapter(), syntaxNodeOpt, diagnostics);
            }

            return methodSymbol.GetCciAdapter();
        }
#nullable disable

        internal Cci.IMethodReference TranslateOverriddenMethodReference(
            MethodSymbol methodSymbol,
            CSharpSyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics)
        {
            Cci.IMethodReference methodRef;
            NamedTypeSymbol container = methodSymbol.ContainingType;

            if (IsGenericType(container))
            {
                if (methodSymbol.IsDefinition)
                {
                    object reference;

                    if (_genericInstanceMap.TryGetValue(methodSymbol, out reference))
                    {
                        methodRef = (Cci.IMethodReference)reference;
                    }
                    else
                    {
                        methodRef = new SpecializedMethodReference(methodSymbol);
                        methodRef = (Cci.IMethodReference)_genericInstanceMap.GetOrAdd(methodSymbol, methodRef);
                    }
                }
                else
                {
                    methodRef = new SpecializedMethodReference(methodSymbol);
                }
            }
            else
            {
                Debug.Assert(methodSymbol.IsDefinition);

                if (_embeddedTypesManagerOpt != null)
                {
                    methodRef = _embeddedTypesManagerOpt.EmbedMethodIfNeedTo(methodSymbol.GetCciAdapter(), syntaxNodeOpt, diagnostics);
                }
                else
                {
                    methodRef = methodSymbol.GetCciAdapter();
                }
            }

            return methodRef;
        }

        internal ImmutableArray<Cci.IParameterTypeInformation> Translate(ImmutableArray<ParameterSymbol> @params)
        {
            Debug.Assert(@params.All(p => p.IsDefinitionOrDistinct()));

            bool mustBeTranslated = @params.Any() && MustBeWrapped(@params.First());
            Debug.Assert(@params.All(p => mustBeTranslated == MustBeWrapped(p)), "either all or no parameters need translating");

            if (!mustBeTranslated)
            {
#if DEBUG
                return @params.SelectAsArray<ParameterSymbol, Cci.IParameterTypeInformation>(p => p.GetCciAdapter());
#else
                return StaticCast<Cci.IParameterTypeInformation>.From(@params);
#endif
            }

            return TranslateAll(@params);
        }

        private static bool MustBeWrapped(ParameterSymbol param)
        {
            // we represent parameters of generic methods as definitions
            // CCI wants them represented as IParameterTypeInformation
            // so we need to create a wrapper of parameters iff
            // 1) parameters are definitions and
            // 2) container is generic
            // NOTE: all parameters must always agree on whether they need wrapping
            if (param.IsDefinition)
            {
                var container = param.ContainingSymbol;
                if (ContainerIsGeneric(container))
                {
                    return true;
                }
            }

            return false;
        }

        private ImmutableArray<Cci.IParameterTypeInformation> TranslateAll(ImmutableArray<ParameterSymbol> @params)
        {
            var builder = ArrayBuilder<Cci.IParameterTypeInformation>.GetInstance();
            foreach (var param in @params)
            {
                builder.Add(CreateParameterTypeInformationWrapper(param));
            }
            return builder.ToImmutableAndFree();
        }

        private Cci.IParameterTypeInformation CreateParameterTypeInformationWrapper(ParameterSymbol param)
        {
            object reference;
            Cci.IParameterTypeInformation paramRef;

            if (_genericInstanceMap.TryGetValue(param, out reference))
            {
                return (Cci.IParameterTypeInformation)reference;
            }

            paramRef = new ParameterTypeInformation(param);
            paramRef = (Cci.IParameterTypeInformation)_genericInstanceMap.GetOrAdd(param, paramRef);

            return paramRef;
        }

        private static bool ContainerIsGeneric(Symbol container)
        {
            return container.Kind == SymbolKind.Method && ((MethodSymbol)container).IsGenericMethod ||
                IsGenericType(container.ContainingType);
        }

        internal Cci.ITypeReference Translate(
            SyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics)
        {
            // Translate the dynamic type to System.Object special type to avoid duplicate entries in TypeRef table.
            // We don't need to recursively replace the dynamic type with Object since the DynamicTypeSymbol adapter
            // masquerades the TypeRef as System.Object when used to encode signatures.
            return GetSpecialType(SpecialType.System_Object, syntaxNodeOpt, diagnostics);
        }

        internal Cci.IArrayTypeReference Translate(ArrayTypeSymbol symbol)
        {
            return (Cci.IArrayTypeReference)GetCciAdapter(symbol);
        }

        internal Cci.IPointerTypeReference Translate(PointerTypeSymbol symbol)
        {
            return (Cci.IPointerTypeReference)GetCciAdapter(symbol);
        }

        internal Cci.IFunctionPointerTypeReference Translate(FunctionPointerTypeSymbol symbol)
        {
            return (Cci.IFunctionPointerTypeReference)GetCciAdapter(symbol);
        }

        /// <summary>
        /// Set the underlying implementation type for a given fixed-size buffer field.
        /// </summary>
        public NamedTypeSymbol SetFixedImplementationType(SourceMemberFieldSymbol field)
        {
            if (_fixedImplementationTypes == null)
            {
                Interlocked.CompareExchange(ref _fixedImplementationTypes, new Dictionary<FieldSymbol, NamedTypeSymbol>(), null);
            }

            lock (_fixedImplementationTypes)
            {
                NamedTypeSymbol result;
                if (_fixedImplementationTypes.TryGetValue(field, out result))
                {
                    return result;
                }

                result = new FixedFieldImplementationType(field);
                _fixedImplementationTypes.Add(field, result);
                AddSynthesizedDefinition(result.ContainingType, result.GetCciAdapter());
                return result;
            }
        }

        protected override Cci.IMethodDefinition CreatePrivateImplementationDetailsStaticConstructor(SyntaxNode syntaxOpt, DiagnosticBag diagnostics)
        {
            return new SynthesizedPrivateImplementationDetailsStaticConstructor(GetPrivateImplClass(syntaxOpt, diagnostics), GetUntranslatedSpecialType(SpecialType.System_Void, syntaxOpt, diagnostics)).GetCciAdapter();
        }

        internal abstract SynthesizedAttributeData SynthesizeEmbeddedAttribute();

        internal SynthesizedAttributeData SynthesizeIsReadOnlyAttribute(Symbol symbol)
        {
            if ((object)Compilation.SourceModule != symbol.ContainingModule)
            {
                // For symbols that are not defined in the same compilation (like NoPia), don't synthesize this attribute.
                return null;
            }

            return TrySynthesizeIsReadOnlyAttribute();
        }

        internal SynthesizedAttributeData SynthesizeRequiresLocationAttribute(ParameterSymbol symbol)
        {
            if ((object)Compilation.SourceModule != symbol.ContainingModule)
            {
                // For symbols that are not defined in the same compilation (like NoPia), don't synthesize this attribute.
                return null;
            }

            return TrySynthesizeRequiresLocationAttribute();
        }

        internal SynthesizedAttributeData SynthesizeIsUnmanagedAttribute(Symbol symbol)
        {
            if ((object)Compilation.SourceModule != symbol.ContainingModule)
            {
                // For symbols that are not defined in the same compilation (like NoPia), don't synthesize this attribute.
                return null;
            }

            return TrySynthesizeIsUnmanagedAttribute();
        }

        internal SynthesizedAttributeData SynthesizeIsByRefLikeAttribute(Symbol symbol)
        {
            if ((object)Compilation.SourceModule != symbol.ContainingModule)
            {
                // For symbols that are not defined in the same compilation (like NoPia), don't synthesize this attribute.
                return null;
            }

            return TrySynthesizeIsByRefLikeAttribute();
        }

        /// <summary>
        /// Given a type <paramref name="type"/>, which is either a nullable reference type OR
        /// is a constructed type with a nullable reference type present in its type argument tree,
        /// returns a synthesized NullableAttribute with encoded nullable transforms array.
        /// </summary>
        internal SynthesizedAttributeData SynthesizeNullableAttributeIfNecessary(Symbol symbol, byte? nullableContextValue, TypeWithAnnotations type)
        {
            if ((object)Compilation.SourceModule != symbol.ContainingModule)
            {
                // For symbols that are not defined in the same compilation (like NoPia), don't synthesize this attribute.
                return null;
            }

            var flagsBuilder = ArrayBuilder<byte>.GetInstance();
            type.AddNullableTransforms(flagsBuilder);

            SynthesizedAttributeData attribute;
            if (!flagsBuilder.Any())
            {
                attribute = null;
            }
            else
            {
                Debug.Assert(flagsBuilder.All(f => f <= 2));
                byte? commonValue = MostCommonNullableValueBuilder.GetCommonValue(flagsBuilder);
                if (commonValue != null)
                {
                    attribute = SynthesizeNullableAttributeIfNecessary(nullableContextValue, commonValue.GetValueOrDefault());
                }
                else
                {
                    NamedTypeSymbol byteType = Compilation.GetSpecialType(SpecialType.System_Byte);
                    var byteArrayType = ArrayTypeSymbol.CreateSZArray(byteType.ContainingAssembly, TypeWithAnnotations.Create(byteType));
                    var value = flagsBuilder.SelectAsArray((flag, byteType) => new TypedConstant(byteType, TypedConstantKind.Primitive, flag), byteType);
                    attribute = SynthesizeNullableAttribute(
                        WellKnownMember.System_Runtime_CompilerServices_NullableAttribute__ctorTransformFlags,
                        ImmutableArray.Create(new TypedConstant(byteArrayType, value)));
                }
            }

            flagsBuilder.Free();
            return attribute;
        }

        internal SynthesizedAttributeData SynthesizeNullableAttributeIfNecessary(byte? nullableContextValue, byte nullableValue)
        {
            if (nullableValue == nullableContextValue ||
                (nullableContextValue == null && nullableValue == 0))
            {
                return null;
            }

            NamedTypeSymbol byteType = Compilation.GetSpecialType(SpecialType.System_Byte);
            return SynthesizeNullableAttribute(
                WellKnownMember.System_Runtime_CompilerServices_NullableAttribute__ctorByte,
                ImmutableArray.Create(new TypedConstant(byteType, TypedConstantKind.Primitive, nullableValue)));
        }

        internal virtual SynthesizedAttributeData SynthesizeNullableAttribute(WellKnownMember member, ImmutableArray<TypedConstant> arguments)
        {
            // For modules, this attribute should be present. Only assemblies generate and embed this type.
            // https://github.com/dotnet/roslyn/issues/30062 Should not be optional.
            return Compilation.TrySynthesizeAttribute(member, arguments, isOptionalUse: true);
        }

        internal SynthesizedAttributeData SynthesizeNullableContextAttribute(Symbol symbol, byte value)
        {
            var module = Compilation.SourceModule;
            if ((object)module != symbol && (object)module != symbol.ContainingModule)
            {
                // For symbols that are not defined in the same compilation (like NoPia), don't synthesize this attribute.
                return null;
            }

            return SynthesizeNullableContextAttribute(
                ImmutableArray.Create(new TypedConstant(Compilation.GetSpecialType(SpecialType.System_Byte), TypedConstantKind.Primitive, value)));
        }

        internal virtual SynthesizedAttributeData SynthesizeNullableContextAttribute(ImmutableArray<TypedConstant> arguments)
        {
            // For modules, this attribute should be present. Only assemblies generate and embed this type.
            // https://github.com/dotnet/roslyn/issues/30062 Should not be optional.
            return Compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_NullableContextAttribute__ctor, arguments, isOptionalUse: true);
        }

        internal SynthesizedAttributeData SynthesizePreserveBaseOverridesAttribute()
        {
            return Compilation.TrySynthesizeAttribute(SpecialMember.System_Runtime_CompilerServices_PreserveBaseOverridesAttribute__ctor, isOptionalUse: true);
        }

        internal SynthesizedAttributeData SynthesizeNativeIntegerAttribute(Symbol symbol, TypeSymbol type)
        {
            Debug.Assert((object)type != null);
            Debug.Assert(type.ContainsNativeIntegerWrapperType());
            Debug.Assert(Compilation.ShouldEmitNativeIntegerAttributes());

            if ((object)Compilation.SourceModule != symbol.ContainingModule)
            {
                // For symbols that are not defined in the same compilation (like NoPia), don't synthesize this attribute.
                return null;
            }

            var builder = ArrayBuilder<bool>.GetInstance();
            CSharpCompilation.NativeIntegerTransformsEncoder.Encode(builder, type);

            Debug.Assert(builder.Any());
            Debug.Assert(builder.Contains(true));

            SynthesizedAttributeData attribute;
            if (builder.Count == 1 && builder[0])
            {
                attribute = SynthesizeNativeIntegerAttribute(WellKnownMember.System_Runtime_CompilerServices_NativeIntegerAttribute__ctor, ImmutableArray<TypedConstant>.Empty);
            }
            else
            {
                NamedTypeSymbol booleanType = Compilation.GetSpecialType(SpecialType.System_Boolean);
                Debug.Assert((object)booleanType != null);
                var transformFlags = builder.SelectAsArray((flag, constantType) => new TypedConstant(constantType, TypedConstantKind.Primitive, flag), booleanType);
                var boolArray = ArrayTypeSymbol.CreateSZArray(booleanType.ContainingAssembly, TypeWithAnnotations.Create(booleanType));
                var arguments = ImmutableArray.Create(new TypedConstant(boolArray, transformFlags));
                attribute = SynthesizeNativeIntegerAttribute(WellKnownMember.System_Runtime_CompilerServices_NativeIntegerAttribute__ctorTransformFlags, arguments);
            }

            builder.Free();
            return attribute;
        }

        internal virtual SynthesizedAttributeData SynthesizeNativeIntegerAttribute(WellKnownMember member, ImmutableArray<TypedConstant> arguments)
        {
            Debug.Assert(Compilation.ShouldEmitNativeIntegerAttributes());

            // For modules, this attribute should be present. Only assemblies generate and embed this type.
            // https://github.com/dotnet/roslyn/issues/30062 Should not be optional.
            return Compilation.TrySynthesizeAttribute(member, arguments, isOptionalUse: true);
        }

        internal SynthesizedAttributeData SynthesizeScopedRefAttribute(ParameterSymbol symbol, ScopedKind scope)
        {
            Debug.Assert(scope != ScopedKind.None);
            Debug.Assert(!ParameterHelpers.IsRefScopedByDefault(symbol) || scope == ScopedKind.ScopedValue);
            Debug.Assert(!symbol.IsThis);

            if ((object)Compilation.SourceModule != symbol.ContainingModule)
            {
                // For symbols that are not defined in the same compilation (like NoPia), don't synthesize this attribute.
                return null;
            }

            return SynthesizeScopedRefAttribute(WellKnownMember.System_Runtime_CompilerServices_ScopedRefAttribute__ctor);
        }

        internal virtual SynthesizedAttributeData SynthesizeScopedRefAttribute(WellKnownMember member)
        {
            // For modules, this attribute should be present. Only assemblies generate and embed this type.
            // https://github.com/dotnet/roslyn/issues/30062 Should not be optional.
            return Compilation.TrySynthesizeAttribute(member, isOptionalUse: true);
        }

        internal virtual SynthesizedAttributeData SynthesizeRefSafetyRulesAttribute(ImmutableArray<TypedConstant> arguments)
        {
            // For modules, this attribute should be present. Only assemblies generate and embed this type.
            // https://github.com/dotnet/roslyn/issues/30062 Should not be optional.
            return Compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_RefSafetyRulesAttribute__ctor, arguments, isOptionalUse: true);
        }

        internal bool ShouldEmitNullablePublicOnlyAttribute()
        {
            // No need to look at this.GetNeedsGeneratedAttributes() since those bits are
            // only set for members generated by the rewriter which are not public.
            return Compilation.GetUsesNullableAttributes() && Compilation.EmitNullablePublicOnly;
        }

        internal virtual SynthesizedAttributeData SynthesizeNullablePublicOnlyAttribute(ImmutableArray<TypedConstant> arguments)
        {
            // For modules, this attribute should be present. Only assemblies generate and embed this type.
            return Compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_NullablePublicOnlyAttribute__ctor, arguments);
        }

        protected virtual SynthesizedAttributeData TrySynthesizeIsReadOnlyAttribute()
        {
            // For modules, this attribute should be present. Only assemblies generate and embed this type.
            return Compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_IsReadOnlyAttribute__ctor);
        }

        protected virtual SynthesizedAttributeData TrySynthesizeRequiresLocationAttribute()
        {
            // For modules, this attribute should be present. Only assemblies generate and embed this type.
            return Compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_RequiresLocationAttribute__ctor);
        }

        protected virtual SynthesizedAttributeData TrySynthesizeIsUnmanagedAttribute()
        {
            // For modules, this attribute should be present. Only assemblies generate and embed this type.
            return Compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_IsUnmanagedAttribute__ctor);
        }

        protected virtual SynthesizedAttributeData TrySynthesizeIsByRefLikeAttribute()
        {
            // For modules, this attribute should be present. Only assemblies generate and embed this type.
            return Compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_IsByRefLikeAttribute__ctor);
        }

        private void EnsureEmbeddableAttributeExists(EmbeddableAttributes attribute)
        {
            Debug.Assert(!_needsGeneratedAttributes_IsFrozen);

            if ((GetNeedsGeneratedAttributesInternal() & attribute) != 0)
            {
                return;
            }

            // Don't report any errors. They should be reported during binding.
            if (Compilation.CheckIfAttributeShouldBeEmbedded(attribute, diagnosticsOpt: null, locationOpt: null))
            {
                SetNeedsGeneratedAttributes(attribute);
            }
        }

        internal void EnsureIsReadOnlyAttributeExists()
        {
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.IsReadOnlyAttribute);
        }

        internal void EnsureRequiresLocationAttributeExists()
        {
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.RequiresLocationAttribute);
        }

        internal void EnsureIsUnmanagedAttributeExists()
        {
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.IsUnmanagedAttribute);
        }

        internal void EnsureNullableAttributeExists()
        {
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.NullableAttribute);
        }

        internal void EnsureNullableContextAttributeExists()
        {
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.NullableContextAttribute);
        }

        internal void EnsureNativeIntegerAttributeExists()
        {
            Debug.Assert(Compilation.ShouldEmitNativeIntegerAttributes());
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.NativeIntegerAttribute);
        }

        internal void EnsureScopedRefAttributeExists()
        {
            EnsureEmbeddableAttributeExists(EmbeddableAttributes.ScopedRefAttribute);
        }

#nullable enable

        /// <summary>
        /// Creates the ThrowSwitchExpressionException helper if needed.
        /// </summary>
        internal MethodSymbol EnsureThrowSwitchExpressionExceptionExists(SyntaxNode syntaxNode, SyntheticBoundNodeFactory factory, DiagnosticBag diagnostics)
        {
            return EnsurePrivateImplClassMethodExists(syntaxNode, PrivateImplementationDetails.SynthesizedThrowSwitchExpressionExceptionFunctionName,
                                                      static (privateImplClass, factory) =>
                                                      {
                                                          TypeSymbol returnType = factory.SpecialType(SpecialType.System_Void);
                                                          TypeSymbol unmatchedValueType = factory.SpecialType(SpecialType.System_Object);
                                                          return new SynthesizedThrowSwitchExpressionExceptionMethod(privateImplClass, returnType, unmatchedValueType);
                                                      },
                                                      factory,
                                                      diagnostics);
        }

        private MethodSymbol EnsurePrivateImplClassMethodExists<TArg>(SyntaxNode syntaxNode, string methodName, Func<SynthesizedPrivateImplementationDetailsType, TArg, MethodSymbol> createMethodSymbol, TArg arg, DiagnosticBag diagnostics)
        {
            var privateImplClass = GetPrivateImplClass(syntaxNode, diagnostics);
            var methodAdapter = privateImplClass.PrivateImplementationDetails.GetMethod(methodName);
            if (methodAdapter is not null)
            {
                Debug.Assert(methodAdapter.Name == methodName);
                return (MethodSymbol)methodAdapter.GetInternalSymbol()!;
            }

            MethodSymbol methodSymbol = createMethodSymbol(privateImplClass, arg);
            Debug.Assert(methodSymbol.Name == methodName);

            // use add-then-get pattern to ensure the symbol exists, and then ensure we use the single "canonical" instance added by whichever thread won the race.
            privateImplClass.PrivateImplementationDetails.TryAddSynthesizedMethod(methodSymbol.GetCciAdapter());
            return (MethodSymbol)privateImplClass.PrivateImplementationDetails.GetMethod(methodName)!.GetInternalSymbol()!;
        }

        internal new SynthesizedPrivateImplementationDetailsType GetPrivateImplClass(SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            if (_lazyPrivateImplementationDetailsClass is null)
            {
                Interlocked.CompareExchange(
                    ref _lazyPrivateImplementationDetailsClass,
                    new SynthesizedPrivateImplementationDetailsType(base.GetPrivateImplClass(syntaxNodeOpt, diagnostics), SourceModule.GlobalNamespace, Compilation.ObjectType),
                    null);
            }

            return _lazyPrivateImplementationDetailsClass;
        }

        /// <summary>
        /// Creates the ThrowSwitchExpressionExceptionParameterless helper if needed.
        /// </summary>
        internal MethodSymbol EnsureThrowSwitchExpressionExceptionParameterlessExists(SyntaxNode syntaxNode, SyntheticBoundNodeFactory factory, DiagnosticBag diagnostics)
        {
            return EnsurePrivateImplClassMethodExists(syntaxNode, PrivateImplementationDetails.SynthesizedThrowSwitchExpressionExceptionParameterlessFunctionName,
                                                      static (privateImplClass, factory) =>
                                                      {
                                                          TypeSymbol returnType = factory.SpecialType(SpecialType.System_Void);

                                                          return new SynthesizedParameterlessThrowMethod(
                                                              privateImplClass,
                                                              returnType,
                                                              PrivateImplementationDetails.SynthesizedThrowSwitchExpressionExceptionParameterlessFunctionName,
                                                              factory.WellKnownMethod(WellKnownMember.System_Runtime_CompilerServices_SwitchExpressionException__ctor));
                                                      },
                                                      factory,
                                                      diagnostics);
        }

        /// <summary>
        /// Creates the ThrowInvalidOperationException helper if needed.
        /// </summary>
        internal MethodSymbol EnsureThrowInvalidOperationExceptionExists(SyntaxNode syntaxNode, SyntheticBoundNodeFactory factory, DiagnosticBag diagnostics)
        {
            return EnsurePrivateImplClassMethodExists(syntaxNode, PrivateImplementationDetails.SynthesizedThrowInvalidOperationExceptionFunctionName,
                                                      static (privateImplClass, factory) =>
                                                      {
                                                          TypeSymbol returnType = factory.SpecialType(SpecialType.System_Void);

                                                          return new SynthesizedParameterlessThrowMethod(
                                                              privateImplClass,
                                                              returnType,
                                                              PrivateImplementationDetails.SynthesizedThrowInvalidOperationExceptionFunctionName,
                                                              factory.WellKnownMethod(WellKnownMember.System_InvalidOperationException__ctor));
                                                      },
                                                      factory,
                                                      diagnostics);
        }

        internal MethodSymbol EnsureInlineArrayAsSpanExists(SyntaxNode syntaxNode, NamedTypeSymbol spanType, NamedTypeSymbol intType, DiagnosticBag diagnostics)
        {
            Debug.Assert(intType.SpecialType == SpecialType.System_Int32);

            return EnsurePrivateImplClassMethodExists(syntaxNode, PrivateImplementationDetails.SynthesizedInlineArrayAsSpanName,
                                                      static (privateImplClass, arg) =>
                                                      {
                                                          return new SynthesizedInlineArrayAsSpanMethod(
                                                              privateImplClass,
                                                              PrivateImplementationDetails.SynthesizedInlineArrayAsSpanName,
                                                              arg.spanType,
                                                              arg.intType);
                                                      },
                                                      (spanType, intType),
                                                      diagnostics);
        }

        internal NamedTypeSymbol EnsureInlineArrayTypeExists(SyntaxNode syntaxNode, SyntheticBoundNodeFactory factory, int arrayLength, DiagnosticBag diagnostics)
        {
            Debug.Assert(Compilation.Assembly.RuntimeSupportsInlineArrayTypes);
            Debug.Assert(arrayLength > 0);

            string typeName = GeneratedNames.MakeSynthesizedInlineArrayName(arrayLength, CurrentGenerationOrdinal);
            var privateImplClass = GetPrivateImplClass(syntaxNode, diagnostics).PrivateImplementationDetails;
            var typeAdapter = privateImplClass.GetSynthesizedType(typeName);

            if (typeAdapter is null)
            {
                var attributeConstructor = (MethodSymbol)factory.SpecialMember(SpecialMember.System_Runtime_CompilerServices_InlineArrayAttribute__ctor);
                Debug.Assert(attributeConstructor is { });

                var typeSymbol = new SynthesizedInlineArrayTypeSymbol(SourceModule, typeName, arrayLength, attributeConstructor);
                privateImplClass.TryAddSynthesizedType(typeSymbol.GetCciAdapter());
                typeAdapter = privateImplClass.GetSynthesizedType(typeName)!;
            }

            Debug.Assert(typeAdapter.Name == typeName);
            return (NamedTypeSymbol)typeAdapter.GetInternalSymbol()!;
        }

        internal NamedTypeSymbol EnsureReadOnlyListTypeExists(SyntaxNode syntaxNode, bool hasKnownLength, DiagnosticBag diagnostics)
        {
            Debug.Assert(syntaxNode is { });
            Debug.Assert(diagnostics is { });

            string typeName = GeneratedNames.MakeSynthesizedReadOnlyListName(hasKnownLength, CurrentGenerationOrdinal);
            var privateImplClass = GetPrivateImplClass(syntaxNode, diagnostics).PrivateImplementationDetails;
            var typeAdapter = privateImplClass.GetSynthesizedType(typeName);
            NamedTypeSymbol typeSymbol;

            if (typeAdapter is null)
            {
                typeSymbol = SynthesizedReadOnlyListTypeSymbol.Create(SourceModule, typeName, hasKnownLength);
                privateImplClass.TryAddSynthesizedType(typeSymbol.GetCciAdapter());
                typeAdapter = privateImplClass.GetSynthesizedType(typeName)!;
            }

            Debug.Assert(typeAdapter.Name == typeName);
            typeSymbol = (NamedTypeSymbol)typeAdapter.GetInternalSymbol()!;

            var info = typeSymbol.GetUseSiteInfo().DiagnosticInfo;
            if (info is { })
            {
                Symbol.ReportUseSiteDiagnostic(info, diagnostics, syntaxNode.Location);
            }

            return typeSymbol;
        }

        internal MethodSymbol EnsureInlineArrayAsReadOnlySpanExists(SyntaxNode syntaxNode, NamedTypeSymbol spanType, NamedTypeSymbol intType, DiagnosticBag diagnostics)
        {
            Debug.Assert(intType.SpecialType == SpecialType.System_Int32);

            return EnsurePrivateImplClassMethodExists(syntaxNode, PrivateImplementationDetails.SynthesizedInlineArrayAsReadOnlySpanName,
                                                      static (privateImplClass, arg) =>
                                                      {
                                                          return new SynthesizedInlineArrayAsReadOnlySpanMethod(
                                                              privateImplClass,
                                                              PrivateImplementationDetails.SynthesizedInlineArrayAsReadOnlySpanName,
                                                              arg.spanType,
                                                              arg.intType);
                                                      },
                                                      (spanType, intType),
                                                      diagnostics);
        }

        internal MethodSymbol EnsureInlineArrayElementRefExists(SyntaxNode syntaxNode, NamedTypeSymbol intType, DiagnosticBag diagnostics)
        {
            Debug.Assert(intType.SpecialType == SpecialType.System_Int32);

            return EnsurePrivateImplClassMethodExists(syntaxNode, PrivateImplementationDetails.SynthesizedInlineArrayElementRefName,
                                                      static (privateImplClass, intType) =>
                                                      {
                                                          return new SynthesizedInlineArrayElementRefMethod(
                                                              privateImplClass,
                                                              PrivateImplementationDetails.SynthesizedInlineArrayElementRefName,
                                                              intType);
                                                      },
                                                      intType,
                                                      diagnostics);
        }

        internal MethodSymbol EnsureInlineArrayElementRefReadOnlyExists(SyntaxNode syntaxNode, NamedTypeSymbol intType, DiagnosticBag diagnostics)
        {
            Debug.Assert(intType.SpecialType == SpecialType.System_Int32);

            return EnsurePrivateImplClassMethodExists(syntaxNode, PrivateImplementationDetails.SynthesizedInlineArrayElementRefReadOnlyName,
                                                      static (privateImplClass, intType) =>
                                                      {
                                                          return new SynthesizedInlineArrayElementRefReadOnlyMethod(
                                                              privateImplClass,
                                                              PrivateImplementationDetails.SynthesizedInlineArrayElementRefReadOnlyName,
                                                              intType);
                                                      },
                                                      intType,
                                                      diagnostics);
        }

        internal MethodSymbol EnsureInlineArrayFirstElementRefExists(SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            return EnsurePrivateImplClassMethodExists(syntaxNode, PrivateImplementationDetails.SynthesizedInlineArrayFirstElementRefName,
                                                      static (privateImplClass, _) =>
                                                      {
                                                          return new SynthesizedInlineArrayFirstElementRefMethod(
                                                              privateImplClass,
                                                              PrivateImplementationDetails.SynthesizedInlineArrayFirstElementRefName);
                                                      },
                                                      (object?)null,
                                                      diagnostics);
        }

        internal MethodSymbol EnsureInlineArrayFirstElementRefReadOnlyExists(SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            return EnsurePrivateImplClassMethodExists(syntaxNode, PrivateImplementationDetails.SynthesizedInlineArrayFirstElementRefReadOnlyName,
                                                      static (privateImplClass, _) =>
                                                      {
                                                          return new SynthesizedInlineArrayFirstElementRefReadOnlyMethod(
                                                              privateImplClass,
                                                              PrivateImplementationDetails.SynthesizedInlineArrayFirstElementRefReadOnlyName);
                                                      },
                                                      (object?)null,
                                                      diagnostics);
        }

#nullable disable

        public override IEnumerable<Cci.INamespaceTypeDefinition> GetAdditionalTopLevelTypeDefinitions(EmitContext context)
        {
            return GetAdditionalTopLevelTypes()
#if DEBUG
                   .Select(type => type.GetCciAdapter())
#endif
                   ;
        }

        public override IEnumerable<Cci.INamespaceTypeDefinition> GetEmbeddedTypeDefinitions(EmitContext context)
        {
            return GetEmbeddedTypes(context.Diagnostics)
#if DEBUG
                   .Select(type => type.GetCciAdapter())
#endif
                   ;
        }

        public sealed override ImmutableArray<NamedTypeSymbol> GetEmbeddedTypes(DiagnosticBag diagnostics)
        {
            var bindingDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);

            var result = GetEmbeddedTypes(bindingDiagnostics);

            diagnostics.AddRange(bindingDiagnostics.DiagnosticBag);
            bindingDiagnostics.Free();
            return result;
        }

        internal virtual ImmutableArray<NamedTypeSymbol> GetEmbeddedTypes(BindingDiagnosticBag diagnostics)
        {
            return base.GetEmbeddedTypes(diagnostics.DiagnosticBag);
        }

        internal bool TryGetTranslatedImports(ImportChain chain, out ImmutableArray<Cci.UsedNamespaceOrType> imports)
        {
            return _translatedImportsMap.TryGetValue(chain, out imports);
        }

        internal ImmutableArray<Cci.UsedNamespaceOrType> GetOrAddTranslatedImports(ImportChain chain, ImmutableArray<Cci.UsedNamespaceOrType> imports)
        {
            return _translatedImportsMap.GetOrAdd(chain, imports);
        }
    }
}
