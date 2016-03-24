// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal abstract class PEModuleBuilder : PEModuleBuilder<CSharpCompilation, SourceModuleSymbol, AssemblySymbol, TypeSymbol, NamedTypeSymbol, MethodSymbol, CSharpSyntaxNode, NoPia.EmbeddedTypesManager, ModuleCompilationState>
    {
        // TODO: Need to estimate amount of elements for this map and pass that value to the constructor. 
        protected readonly ConcurrentDictionary<Symbol, Cci.IModuleReference> AssemblyOrModuleSymbolToModuleRefMap = new ConcurrentDictionary<Symbol, Cci.IModuleReference>();
        private readonly ConcurrentDictionary<Symbol, object> _genericInstanceMap = new ConcurrentDictionary<Symbol, object>();
        private readonly ConcurrentSet<ErrorTypeSymbol> _reportedErrorTypesMap = new ConcurrentSet<ErrorTypeSymbol>();

        private readonly NoPia.EmbeddedTypesManager _embeddedTypesManagerOpt;
        public override NoPia.EmbeddedTypesManager EmbeddedTypesManagerOpt
        {
            get { return _embeddedTypesManagerOpt; }
        }

        // Gives the name of this module (may not reflect the name of the underlying symbol).
        // See Assembly.MetadataName.
        private readonly string _metadataName;

        private ImmutableArray<NamedTypeSymbol> _lazyExportedTypes;

        /// <summary>
        /// The compiler-generated implementation type for each fixed-size buffer.
        /// </summary>
        private Dictionary<FieldSymbol, NamedTypeSymbol> _fixedImplementationTypes;

        // These fields will only be set when running tests.  They allow realized IL for a given method to be looked up by method display name.
        private ConcurrentDictionary<string, CompilationTestData.MethodData> _testData;

        private SymbolDisplayFormat _testDataKeyFormat;
        private SymbolDisplayFormat _testDataOperatorKeyFormat;

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

        internal override string Name
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

        internal sealed override IEnumerable<Cci.ICustomAttribute> GetSourceAssemblyAttributes()
        {
            return SourceModule.ContainingSourceAssembly.GetCustomAttributesToEmit(this.CompilationState, emittingAssemblyAttributesInNetModule: OutputKind.IsNetModule());
        }

        internal sealed override IEnumerable<Cci.SecurityAttribute> GetSourceAssemblySecurityAttributes()
        {
            return SourceModule.ContainingSourceAssembly.GetSecurityAttributes();
        }

        internal sealed override IEnumerable<Cci.ICustomAttribute> GetSourceModuleAttributes()
        {
            return SourceModule.GetCustomAttributesToEmit(this.CompilationState);
        }

        internal sealed override AssemblySymbol CorLibrary
        {
            get { return SourceModule.ContainingSourceAssembly.CorLibrary; }
        }

        protected sealed override bool GenerateVisualBasicStylePdb => false;

        // C# doesn't emit linked assembly names into PDBs.
        protected sealed override IEnumerable<string> LinkedAssembliesDebugInfo => SpecializedCollections.EmptyEnumerable<string>();

        // C# currently doesn't emit compilation level imports (TODO: scripting).
        protected override ImmutableArray<Cci.UsedNamespaceOrType> GetImports() => ImmutableArray<Cci.UsedNamespaceOrType>.Empty;

        // C# doesn't allow to define default namespace for compilation.
        protected override string DefaultNamespace => null;

        protected override IEnumerable<Cci.IAssemblyReference> GetAssemblyReferencesFromAddedModules(DiagnosticBag diagnostics)
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

        protected override MultiDictionary<Cci.DebugSourceDocument, Cci.DefinitionWithLocation> GetSymbolToLocationMap()
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
                            AddSymbolLocation(result, location, (Cci.IDefinition)symbol);

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
                                        if (method.IsDefaultValueTypeConstructor() ||
                                            method.IsPartialMethod() && (object)method.PartialImplementationPart == null)
                                        {
                                            break;
                                        }

                                        AddSymbolLocation(result, member);
                                        break;

                                    case SymbolKind.Property:
                                    case SymbolKind.Field:
                                        // NOTE: Dev11 does not add synthesized backing fields for properties,
                                        //       but adds backing fields for events, Roslyn adds both
                                        AddSymbolLocation(result, member);
                                        break;

                                    case SymbolKind.Event:
                                        AddSymbolLocation(result, member);
                                        //  event backing fields do not show up in GetMembers
                                        FieldSymbol field = ((EventSymbol)member).AssociatedField;
                                        if ((object)field != null)
                                        {
                                            AddSymbolLocation(result, field);
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
                AddSymbolLocation(result, location, (Cci.IDefinition)symbol);
            }
        }

        private void AddSymbolLocation(MultiDictionary<Cci.DebugSourceDocument, Cci.DefinitionWithLocation> result, Location location, Cci.IDefinition definition)
        {
            FileLinePositionSpan span = location.GetLineSpan();

            Cci.DebugSourceDocument doc = TryGetDebugDocument(span.Path, basePath: location.SourceTree.FilePath);

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
        internal virtual NamedTypeSymbol DynamicOperationContextType => null;

        internal virtual VariableSlotAllocator TryCreateVariableSlotAllocator(MethodSymbol method, MethodSymbol topLevelMethod)
        {
            return null;
        }

        internal virtual ImmutableArray<AnonymousTypeKey> GetPreviousAnonymousTypes()
        {
            return ImmutableArray<AnonymousTypeKey>.Empty;
        }

        internal virtual int GetNextAnonymousTypeIndex()
        {
            return 0;
        }

        internal virtual bool TryGetAnonymousTypeName(NamedTypeSymbol template, out string name, out int index)
        {
            Debug.Assert(Compilation == template.DeclaringCompilation);

            name = null;
            index = -1;
            return false;
        }

        internal override ImmutableArray<Cci.INamespaceTypeDefinition> GetAnonymousTypes()
        {
            if (EmitOptions.EmitMetadataOnly)
            {
                return ImmutableArray<Cci.INamespaceTypeDefinition>.Empty;
            }

            return StaticCast<Cci.INamespaceTypeDefinition>.From(Compilation.AnonymousTypeManager.GetAllCreatedTemplates());
        }

        /// <summary>
        /// True if this module is an ENC update.
        /// </summary>
        internal virtual bool IsEncDelta
        {
            get { return false; }
        }

        internal override IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypesCore(CodeAnalysis.Emit.EmitContext context)
        {
            foreach (var type in GetAdditionalTopLevelTypes())
            {
                yield return type;
            }

            var namespacesToProcess = new Stack<NamespaceSymbol>();
            namespacesToProcess.Push(SourceModule.GlobalNamespace);

            while (namespacesToProcess.Count > 0)
            {
                var ns = namespacesToProcess.Pop();
                foreach (var member in ns.GetMembers())
                {
                    var memberNamespace = member as NamespaceSymbol;
                    if ((object)memberNamespace != null)
                    {
                        namespacesToProcess.Push(memberNamespace);
                    }
                    else
                    {
                        var type = (NamedTypeSymbol)member;
                        yield return type;
                    }
                }
            }
        }

        internal virtual ImmutableArray<NamedTypeSymbol> GetAdditionalTopLevelTypes()
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        private void GetExportedTypes(NamespaceOrTypeSymbol sym, ArrayBuilder<NamedTypeSymbol> builder)
        {
            if (sym.Kind == SymbolKind.NamedType)
            {
                if (sym.DeclaredAccessibility == Accessibility.Public)
                {
                    Debug.Assert(sym.IsDefinition);
                    builder.Add((NamedTypeSymbol)sym);
                }
                else
                {
                    return;
                }
            }

            foreach (var t in sym.GetMembers())
            {
                NamespaceOrTypeSymbol nortsym = t as NamespaceOrTypeSymbol;

                if ((object)nortsym != null)
                {
                    GetExportedTypes(nortsym, builder);
                }
            }
        }

        public override IEnumerable<Cci.ITypeReference> GetExportedTypes(EmitContext context)
        {
            Debug.Assert(HaveDeterminedTopLevelTypes);

            if (_lazyExportedTypes.IsDefault)
            {
                SourceAssemblySymbol sourceAssembly = SourceModule.ContainingSourceAssembly;
                var builder = ArrayBuilder<NamedTypeSymbol>.GetInstance();

                if (!OutputKind.IsNetModule())
                {
                    var modules = sourceAssembly.Modules;
                    for (int i = 1; i < modules.Length; i++) //NOTE: skipping modules[0]
                    {
                        GetExportedTypes(modules[i].GlobalNamespace, builder);
                    }
                }

                HashSet<NamedTypeSymbol> seenTopLevelForwardedTypes = new HashSet<NamedTypeSymbol>();
                GetForwardedTypes(seenTopLevelForwardedTypes, sourceAssembly.GetSourceDecodedWellKnownAttributeData(), builder);

                if (!OutputKind.IsNetModule())
                {
                    GetForwardedTypes(seenTopLevelForwardedTypes, sourceAssembly.GetNetModuleDecodedWellKnownAttributeData(), builder);
                }

                Debug.Assert(_lazyExportedTypes.IsDefault);

                _lazyExportedTypes = builder.ToImmutableAndFree();

                if (_lazyExportedTypes.Length > 0)
                {
                    var exportedNamesMap = new Dictionary<string, NamedTypeSymbol>();

                    // Report name collisions.
                    foreach (var exportedType in _lazyExportedTypes)
                    {
                        Debug.Assert(exportedType.IsDefinition);

                        if (exportedType.IsTopLevelType())
                        {
                            string fullEmittedName = MetadataHelpers.BuildQualifiedName(
                                ((Cci.INamespaceTypeReference)exportedType).NamespaceName,
                                Cci.MetadataWriter.GetMangledName(exportedType));

                            // First check against types declared in the primary module
                            if (ContainsTopLevelType(fullEmittedName))
                            {
                                if (exportedType.ContainingAssembly == sourceAssembly)
                                {
                                    context.Diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(
                                        ErrorCode.ERR_ExportedTypeConflictsWithDeclaration, exportedType, exportedType.ContainingModule), NoLocation.Singleton));
                                }
                                else
                                {
                                    context.Diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(
                                        ErrorCode.ERR_ForwardedTypeConflictsWithDeclaration, exportedType), NoLocation.Singleton));
                                }

                                continue;
                            }

                            NamedTypeSymbol contender;

                            // Now check against other exported types
                            if (exportedNamesMap.TryGetValue(fullEmittedName, out contender))
                            {
                                if (exportedType.ContainingAssembly == sourceAssembly)
                                {
                                    // all exported types precede forwarded types, therefore contender cannot be a forwarded type.
                                    Debug.Assert(contender.ContainingAssembly == sourceAssembly);

                                    context.Diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(
                                        ErrorCode.ERR_ExportedTypesConflict, exportedType, exportedType.ContainingModule, contender, contender.ContainingModule), NoLocation.Singleton));
                                }
                                else
                                {
                                    if (contender.ContainingAssembly == sourceAssembly)
                                    {
                                        // Forwarded type conflicts with exported type
                                        context.Diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(
                                            ErrorCode.ERR_ForwardedTypeConflictsWithExportedType, exportedType, exportedType.ContainingAssembly, contender, contender.ContainingModule), NoLocation.Singleton));
                                    }
                                    else
                                    {
                                        // Forwarded type conflicts with another forwarded type
                                        context.Diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(
                                            ErrorCode.ERR_ForwardedTypesConflict, exportedType, exportedType.ContainingAssembly, contender, contender.ContainingAssembly), NoLocation.Singleton));
                                    }
                                }

                                continue;
                            }

                            exportedNamesMap.Add(fullEmittedName, exportedType);
                        }
                    }
                }
            }

            return _lazyExportedTypes;
        }

        private static void GetForwardedTypes(
            HashSet<NamedTypeSymbol> seenTopLevelTypes,
            CommonAssemblyWellKnownAttributeData<NamedTypeSymbol> wellKnownAttributeData,
            ArrayBuilder<NamedTypeSymbol> builder)
        {
            if (wellKnownAttributeData != null && wellKnownAttributeData.ForwardedTypes != null)
            {
                foreach (NamedTypeSymbol forwardedType in wellKnownAttributeData.ForwardedTypes)
                {
                    NamedTypeSymbol originalDefinition = forwardedType.OriginalDefinition;
                    Debug.Assert((object)originalDefinition.ContainingType == null, "How did a nested type get forwarded?");

                    // Since we need to allow multiple constructions of the same generic type at the source
                    // level, we need to de-dup the original definitions before emitting.
                    if (!seenTopLevelTypes.Add(originalDefinition)) continue;

                    // Return all nested types.
                    // Note the order: depth first, children in reverse order (to match dev10, not a requirement).
                    Stack<NamedTypeSymbol> stack = new Stack<NamedTypeSymbol>();
                    stack.Push(originalDefinition);

                    while (stack.Count > 0)
                    {
                        NamedTypeSymbol current = stack.Pop();

                        // In general, we don't want private types to appear in the ExportedTypes table.
                        // BREAK: dev11 emits these types.  The problem was discovered in dev10, but failed
                        // to meet the bar Bug: Dev10/258038 and was left as-is.
                        if (current.DeclaredAccessibility == Accessibility.Private)
                        {
                            // NOTE: this will also exclude nested types of curr.
                            continue;
                        }

                        // NOTE: not bothering to put nested types in seenTypes - the top-level type is adequate protection.

                        builder.Add(current);

                        // Iterate backwards so they get popped in forward order.
                        ImmutableArray<NamedTypeSymbol> nested = current.GetTypeMembers(); // Ordered.
                        for (int i = nested.Length - 1; i >= 0; i--)
                        {
                            stack.Push(nested[i]);
                        }
                    }
                }
            }
        }

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

        internal sealed override Cci.INamedTypeReference GetSpecialType(SpecialType specialType, CSharpSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            Debug.Assert(diagnostics != null);

            var typeSymbol = SourceModule.ContainingAssembly.GetSpecialType(specialType);

            DiagnosticInfo info = typeSymbol.GetUseSiteDiagnostic();
            if (info != null)
            {
                Symbol.ReportUseSiteDiagnostic(info,
                                               diagnostics,
                                               syntaxNodeOpt != null ? syntaxNodeOpt.Location : NoLocation.Singleton);
            }

            return Translate(typeSymbol,
                             diagnostics: diagnostics,
                             syntaxNodeOpt: syntaxNodeOpt,
                             needDeclaration: true);
        }
        
        internal sealed override Cci.INamedTypeReference GetSystemType(CSharpSyntaxNode syntaxOpt, DiagnosticBag diagnostics)
        {
             NamedTypeSymbol typeSymbol = Compilation.GetWellKnownType(WellKnownType.System_Type);

            DiagnosticInfo info = typeSymbol.GetUseSiteDiagnostic();
            if (info != null)
            {
                Symbol.ReportUseSiteDiagnostic(info,
                                               diagnostics,
                                               syntaxOpt != null ? syntaxOpt.Location : NoLocation.Singleton);
            }

            return Translate(typeSymbol, syntaxOpt, diagnostics, needDeclaration: true);
        }

        public sealed override Cci.IMethodReference GetInitArrayHelper()
        {
            return (MethodSymbol)Compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__InitializeArrayArrayRuntimeFieldHandle);
        }

        protected sealed override bool IsPlatformType(Cci.ITypeReference typeRef, Cci.PlatformType platformType)
        {
            var namedType = typeRef as NamedTypeSymbol;
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
            CSharpSyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics,
            bool fromImplements = false,
            bool needDeclaration = false)
        {
            Debug.Assert(namedTypeSymbol.IsDefinitionOrDistinct());
            Debug.Assert(diagnostics != null);

            // Anonymous type being translated
            if (namedTypeSymbol.IsAnonymousType)
            {
                namedTypeSymbol = AnonymousTypeManager.TranslateAnonymousTypeSymbol(namedTypeSymbol);
            }

            // Substitute error types with a special singleton object.
            // Unreported bad types can come through NoPia embedding, for example.
            if (namedTypeSymbol.OriginalDefinition.Kind == SymbolKind.ErrorType)
            {
                ErrorTypeSymbol errorType = (ErrorTypeSymbol)namedTypeSymbol.OriginalDefinition;
                DiagnosticInfo diagInfo = errorType.GetUseSiteDiagnostic() ?? errorType.ErrorInfo;

                if (diagInfo == null && namedTypeSymbol.Kind == SymbolKind.ErrorType)
                {
                    errorType = (ErrorTypeSymbol)namedTypeSymbol;
                    diagInfo = errorType.GetUseSiteDiagnostic() ?? errorType.ErrorInfo;
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
                    return namedTypeSymbol;
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
            }

            // NoPia: See if this is a type, which definition we should copy into our assembly.
            Debug.Assert(namedTypeSymbol.IsDefinition);

            if (_embeddedTypesManagerOpt != null)
            {
                return _embeddedTypesManagerOpt.EmbedTypeIfNeedTo(namedTypeSymbol, fromImplements, syntaxNodeOpt, diagnostics);
            }

            return namedTypeSymbol;
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

            return param;
        }

        internal sealed override Cci.ITypeReference Translate(
            TypeSymbol typeSymbol,
            CSharpSyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(diagnostics != null);

            switch (typeSymbol.Kind)
            {
                case SymbolKind.DynamicType:
                    return Translate((DynamicTypeSymbol)typeSymbol, syntaxNodeOpt, diagnostics);

                case SymbolKind.ArrayType:
                    return Translate((ArrayTypeSymbol)typeSymbol);

                case SymbolKind.ErrorType:
                case SymbolKind.NamedType:
                    return Translate((NamedTypeSymbol)typeSymbol, syntaxNodeOpt, diagnostics);

                case SymbolKind.PointerType:
                    return Translate((PointerTypeSymbol)typeSymbol);

                case SymbolKind.TypeParameter:
                    return Translate((TypeParameterSymbol)typeSymbol);
            }

            throw ExceptionUtilities.UnexpectedValue(typeSymbol.Kind);
        }

        internal Cci.IFieldReference Translate(
            FieldSymbol fieldSymbol,
            CSharpSyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics,
            bool needDeclaration = false)
        {
            Debug.Assert(fieldSymbol.IsDefinitionOrDistinct());

            if (!fieldSymbol.IsDefinition)
            {
                Debug.Assert(!needDeclaration);

                return fieldSymbol;
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

            if (_embeddedTypesManagerOpt != null)
            {
                return _embeddedTypesManagerOpt.EmbedFieldIfNeedTo(fieldSymbol, syntaxNodeOpt, diagnostics);
            }

            return fieldSymbol;
        }

        public static Cci.TypeMemberVisibility MemberVisibility(Symbol symbol)
        {
            //
            // We need to relax visibility of members in interactive submissions since they might be emitted into multiple assemblies.
            // 
            // Top-level:
            //   private                       -> public
            //   protected                     -> public (compiles with a warning)
            //   public                         
            //   internal                      -> public
            // 
            // In a nested class:
            //   
            //   private                       
            //   protected                     
            //   public                         
            //   internal                      -> public
            //
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    return Cci.TypeMemberVisibility.Public;

                case Accessibility.Private:
                    if (symbol.ContainingType?.TypeKind == TypeKind.Submission)
                    {
                        // top-level private member:
                        return Cci.TypeMemberVisibility.Public;
                    }
                    else
                    {
                        return Cci.TypeMemberVisibility.Private;
                    }

                case Accessibility.Internal:
                    if (symbol.ContainingAssembly.IsInteractive)
                    {
                        // top-level or nested internal member:
                        return Cci.TypeMemberVisibility.Public;
                    }
                    else
                    {
                        return Cci.TypeMemberVisibility.Assembly;
                    }

                case Accessibility.Protected:
                    if (symbol.ContainingType.TypeKind == TypeKind.Submission)
                    {
                        // top-level protected member:
                        return Cci.TypeMemberVisibility.Public;
                    }
                    else
                    {
                        return Cci.TypeMemberVisibility.Family;
                    }

                case Accessibility.ProtectedAndInternal: // Not supported by language, but we should be able to import it.
                    Debug.Assert(symbol.ContainingType.TypeKind != TypeKind.Submission);
                    return Cci.TypeMemberVisibility.FamilyAndAssembly;

                case Accessibility.ProtectedOrInternal:
                    if (symbol.ContainingAssembly.IsInteractive)
                    {
                        // top-level or nested protected internal member:
                        return Cci.TypeMemberVisibility.Public;
                    }
                    else
                    {
                        return Cci.TypeMemberVisibility.FamilyOrAssembly;
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.DeclaredAccessibility);
            }
        }

        internal override Cci.IMethodReference Translate(MethodSymbol symbol, DiagnosticBag diagnostics, bool needDeclaration)
        {
            return Translate(symbol, null, diagnostics, null, needDeclaration);
        }

        internal Cci.IMethodReference Translate(
            MethodSymbol methodSymbol,
            CSharpSyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics,
            BoundArgListOperator optArgList = null,
            bool needDeclaration = false)
        {
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

        private Cci.IMethodReference Translate(
            MethodSymbol methodSymbol,
            CSharpSyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics,
            bool needDeclaration)
        {
            object reference;
            Cci.IMethodReference methodRef;
            NamedTypeSymbol container = methodSymbol.ContainingType;

            Debug.Assert(methodSymbol.IsDefinitionOrDistinct());

            // Method of anonymous type being translated
            if (container.IsAnonymousType)
            {
                methodSymbol = AnonymousTypeManager.TranslateAnonymousTypeMethodSymbol(methodSymbol);
            }

            if (!methodSymbol.IsDefinition)
            {
                Debug.Assert(!needDeclaration);

                return methodSymbol;
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
            }

            if (_embeddedTypesManagerOpt != null)
            {
                return _embeddedTypesManagerOpt.EmbedMethodIfNeedTo(methodSymbol, syntaxNodeOpt, diagnostics);
            }

            return methodSymbol;
        }

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
                    methodRef = _embeddedTypesManagerOpt.EmbedMethodIfNeedTo(methodSymbol, syntaxNodeOpt, diagnostics);
                }
                else
                {
                    methodRef = methodSymbol;
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
                return StaticCast<Cci.IParameterTypeInformation>.From(@params);
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
            DynamicTypeSymbol symbol,
            CSharpSyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics)
        {
            // Translate the dynamic type to System.Object special type to avoid duplicate entries in TypeRef table. 
            // We don't need to recursively replace the dynamic type with Object since the DynamicTypeSymbol adapter 
            // masquerades the TypeRef as System.Object when used to encode signatures.
            return GetSpecialType(SpecialType.System_Object, syntaxNodeOpt, diagnostics);
        }

        internal static Cci.IArrayTypeReference Translate(ArrayTypeSymbol symbol)
        {
            return symbol;
        }

        internal static Cci.IPointerTypeReference Translate(PointerTypeSymbol symbol)
        {
            return symbol;
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
                AddSynthesizedDefinition(result.ContainingType, result);
                return result;
            }
        }

        internal NamedTypeSymbol GetFixedImplementationType(FieldSymbol field)
        {
            // Note that this method is called only after ALL fixed buffer types have been placed in the map.
            // At that point the map is all filled in and will not change further.  Therefore it is safe to
            // pull values from the map without locking.
            NamedTypeSymbol result;
            var found = _fixedImplementationTypes.TryGetValue(field, out result);
            Debug.Assert(found);
            return result;
        }

        #region Test Hooks

        internal bool SaveTestData
        {
            get
            {
                return _testData != null;
            }
        }

        internal void SetMethodTestData(MethodSymbol methodSymbol, ILBuilder builder)
        {
            if (_testData == null)
            {
                throw new InvalidOperationException(CSharpResources.MustCallSetMethodTestData);
            }

            // If this ever throws "ArgumentException: An item with the same key has already been added.", then
            // the ilBuilderMapKeyFormat will need to be updated to provide a unique key (see SetMethodTestData).
            _testData.Add(
                GetMethodKey(methodSymbol),
                new CompilationTestData.MethodData(builder, methodSymbol));
        }

        private string GetMethodKey(MethodSymbol methodSymbol)
        {
            // It is possible for two methods to have the same symbol display string.  For example,
            //
            //   extern alias A;
            //   extern alias B;
            //   
            //   class C : A::I, B::I
            //   {
            //       void A::I.M() { }
            //       void B::I.M() { }
            //   }
            //
            // Note that symbol display does not incorporate the extern aliases, since they are specific to
            // declaration context.
            //
            // We compensate by prepending the alias qualifier to unique-ify the method key.
            string aliasQualifierOpt = null;
            if (methodSymbol.IsExplicitInterfaceImplementation)
            {
                Symbol symbol = methodSymbol.AssociatedSymbol ?? methodSymbol;
                Debug.Assert(symbol.DeclaringSyntaxReferences.Length < 2, "Can't have a partial explicit interface implementation");
                SyntaxReference reference = symbol.DeclaringSyntaxReferences.FirstOrDefault();
                if (reference != null)
                {
                    var syntax = reference.GetSyntax();
                    switch (syntax.Kind())
                    {
                        case SyntaxKind.MethodDeclaration:
                            MethodDeclarationSyntax methodDecl = (MethodDeclarationSyntax)syntax;
                            aliasQualifierOpt = methodDecl.ExplicitInterfaceSpecifier.Name.GetAliasQualifierOpt();
                            break;
                        case SyntaxKind.IndexerDeclaration:
                        case SyntaxKind.PropertyDeclaration:
                        case SyntaxKind.EventDeclaration:
                            BasePropertyDeclarationSyntax propertyDecl = (BasePropertyDeclarationSyntax)syntax;
                            aliasQualifierOpt = propertyDecl.ExplicitInterfaceSpecifier.Name.GetAliasQualifierOpt();
                            break;
                        case SyntaxKind.EventFieldDeclaration: // Field-like events are never explicit interface implementations
                        default:
                            throw ExceptionUtilities.UnexpectedValue(syntax.Kind());
                    }
                }
            }

            string result = methodSymbol.ToDisplayString(methodSymbol.IsOperator() ? _testDataOperatorKeyFormat : _testDataKeyFormat);

            if (aliasQualifierOpt != null)
            {
                // Not using the colon-colon syntax since it won't be in the right place anyway.
                result = aliasQualifierOpt + "$$" + result;
            }

            return result;
        }

        internal void SetMethodTestData(ConcurrentDictionary<string, CompilationTestData.MethodData> methods)
        {
            _testData = methods;
            _testDataKeyFormat = new SymbolDisplayFormat(
                compilerInternalOptions: SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames,
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeType,
                // Not showing the name is important because we visit parameters to display their
                // types.  If we visited their types directly, we wouldn't get ref/out/params.
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                    SymbolDisplayMiscellaneousOptions.UseAsterisksInMultiDimensionalArrays |
                    SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName);
            // most methods don't need return type to disambiguate signatures, however,
            // it is necessary to disambiguate user defined operators:
            //   int op_Implicit(Type)
            //   float op_Implicit(Type)
            //   ... etc ...
            _testDataOperatorKeyFormat = new SymbolDisplayFormat(
                _testDataKeyFormat.CompilerInternalOptions,
                _testDataKeyFormat.GlobalNamespaceStyle,
                _testDataKeyFormat.TypeQualificationStyle,
                _testDataKeyFormat.GenericsOptions,
                _testDataKeyFormat.MemberOptions | SymbolDisplayMemberOptions.IncludeType,
                _testDataKeyFormat.ParameterOptions,
                _testDataKeyFormat.DelegateStyle,
                _testDataKeyFormat.ExtensionMethodStyle,
                _testDataKeyFormat.PropertyStyle,
                _testDataKeyFormat.LocalOptions,
                _testDataKeyFormat.KindOptions,
                _testDataKeyFormat.MiscellaneousOptions);
        }

        #endregion
    }
}
