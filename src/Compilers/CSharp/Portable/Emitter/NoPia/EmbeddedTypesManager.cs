// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Emit.NoPia;

#if !DEBUG
using SymbolAdapter = Microsoft.CodeAnalysis.CSharp.Symbol;
using NamedTypeSymbolAdapter = Microsoft.CodeAnalysis.CSharp.Symbols.NamedTypeSymbol;
using FieldSymbolAdapter = Microsoft.CodeAnalysis.CSharp.Symbols.FieldSymbol;
using MethodSymbolAdapter = Microsoft.CodeAnalysis.CSharp.Symbols.MethodSymbol;
using EventSymbolAdapter = Microsoft.CodeAnalysis.CSharp.Symbols.EventSymbol;
using PropertySymbolAdapter = Microsoft.CodeAnalysis.CSharp.Symbols.PropertySymbol;
using ParameterSymbolAdapter = Microsoft.CodeAnalysis.CSharp.Symbols.ParameterSymbol;
using TypeParameterSymbolAdapter = Microsoft.CodeAnalysis.CSharp.Symbols.TypeParameterSymbol;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Emit.NoPia
{
    internal sealed class EmbeddedTypesManager :
        EmbeddedTypesManager<PEModuleBuilder, ModuleCompilationState, EmbeddedTypesManager, SyntaxNode, CSharpAttributeData,
            SymbolAdapter,
            AssemblySymbol,
            NamedTypeSymbolAdapter, FieldSymbolAdapter, MethodSymbolAdapter, EventSymbolAdapter, PropertySymbolAdapter, ParameterSymbolAdapter, TypeParameterSymbolAdapter,
            EmbeddedType, EmbeddedField, EmbeddedMethod, EmbeddedEvent, EmbeddedProperty, EmbeddedParameter, EmbeddedTypeParameter>
    {
        private readonly ConcurrentDictionary<AssemblySymbol, string> _assemblyGuidMap = new ConcurrentDictionary<AssemblySymbol, string>(ReferenceEqualityComparer.Instance);
        private readonly ConcurrentDictionary<Symbol, bool> _reportedSymbolsMap = new ConcurrentDictionary<Symbol, bool>(ReferenceEqualityComparer.Instance);
        private NamedTypeSymbol _lazySystemStringType = ErrorTypeSymbol.UnknownResultType;
        private readonly MethodSymbol[] _lazyWellKnownTypeMethods;

        public EmbeddedTypesManager(PEModuleBuilder moduleBeingBuilt) :
            base(moduleBeingBuilt)
        {
            _lazyWellKnownTypeMethods = new MethodSymbol[(int)WellKnownMember.Count];

            for (int i = 0; i < _lazyWellKnownTypeMethods.Length; i++)
            {
                _lazyWellKnownTypeMethods[i] = ErrorMethodSymbol.UnknownMethod;
            }
        }

        public NamedTypeSymbol GetSystemStringType(SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            if ((object)_lazySystemStringType == (object)ErrorTypeSymbol.UnknownResultType)
            {
                var typeSymbol = ModuleBeingBuilt.Compilation.GetSpecialType(SpecialType.System_String);

                UseSiteInfo<AssemblySymbol> info = typeSymbol.GetUseSiteInfo();

                if (typeSymbol.IsErrorType())
                {
                    typeSymbol = null;
                }

                if (TypeSymbol.Equals(Interlocked.CompareExchange(ref _lazySystemStringType, typeSymbol, ErrorTypeSymbol.UnknownResultType), ErrorTypeSymbol.UnknownResultType, TypeCompareKind.ConsiderEverything2))
                {
                    if (info.DiagnosticInfo != null)
                    {
                        Symbol.ReportUseSiteDiagnostic(info.DiagnosticInfo,
                                                       diagnostics,
                                                       syntaxNodeOpt != null ? syntaxNodeOpt.Location : NoLocation.Singleton);
                    }
                }
            }

            return _lazySystemStringType;
        }

        public MethodSymbol GetWellKnownMethod(WellKnownMember method, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            return LazyGetWellKnownTypeMethod(ref _lazyWellKnownTypeMethods[(int)method],
                                              method,
                                              syntaxNodeOpt,
                                              diagnostics);
        }

        private MethodSymbol LazyGetWellKnownTypeMethod(ref MethodSymbol lazyMethod, WellKnownMember member, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            if ((object)lazyMethod == (object)ErrorMethodSymbol.UnknownMethod)
            {
                UseSiteInfo<AssemblySymbol> info;
                var symbol = (MethodSymbol)Binder.GetWellKnownTypeMember(ModuleBeingBuilt.Compilation,
                                                                         member,
                                                                         out info,
                                                                         isOptional: false);

                if (info.DiagnosticInfo?.Severity == DiagnosticSeverity.Error)
                {
                    symbol = null;
                }

                if (Interlocked.CompareExchange(ref lazyMethod, symbol, ErrorMethodSymbol.UnknownMethod) == ErrorMethodSymbol.UnknownMethod)
                {
                    if (info.DiagnosticInfo != null)
                    {
                        Symbol.ReportUseSiteDiagnostic(info.DiagnosticInfo,
                                                       diagnostics,
                                                       syntaxNodeOpt != null ? syntaxNodeOpt.Location : NoLocation.Singleton);
                    }
                }
            }

            return lazyMethod;
        }

        internal override int GetTargetAttributeSignatureIndex(SymbolAdapter underlyingSymbol, CSharpAttributeData attrData, AttributeDescription description)
        {
            return attrData.GetTargetAttributeSignatureIndex(underlyingSymbol.AdaptedSymbol, description);
        }

        internal override CSharpAttributeData CreateSynthesizedAttribute(WellKnownMember constructor, CSharpAttributeData attrData, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            var ctor = GetWellKnownMethod(constructor, syntaxNodeOpt, diagnostics);
            if ((object)ctor == null)
            {
                return null;
            }

            switch (constructor)
            {
                case WellKnownMember.System_Runtime_InteropServices_ComEventInterfaceAttribute__ctor:
                    // When emitting a com event interface, we have to tweak the parameters: the spec requires that we use
                    // the original source interface as both source interface and event provider. Otherwise, we'd have to embed
                    // the event provider class too.
                    return new SynthesizedAttributeData(ctor,
                        ImmutableArray.Create<TypedConstant>(attrData.CommonConstructorArguments[0], attrData.CommonConstructorArguments[0]),
                        ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);

                case WellKnownMember.System_Runtime_InteropServices_CoClassAttribute__ctor:
                    // The interface needs to have a coclass attribute so that we can tell at runtime that it should be
                    // instantiatable. The attribute cannot refer directly to the coclass, however, because we can't embed
                    // classes, and we can't emit a reference to the PIA. We don't actually need
                    // the class name at runtime: we will instead emit a reference to System.Object, as a placeholder.
                    return new SynthesizedAttributeData(ctor,
                        ImmutableArray.Create(new TypedConstant(ctor.Parameters[0].Type, TypedConstantKind.Type, ctor.ContainingAssembly.GetSpecialType(SpecialType.System_Object))),
                        ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);

                default:
                    return new SynthesizedAttributeData(ctor, attrData.CommonConstructorArguments, attrData.CommonNamedArguments);
            }
        }

        internal string GetAssemblyGuidString(AssemblySymbol assembly)
        {
            Debug.Assert(!IsFrozen); // After we freeze the set of types, we might add additional assemblies into this map without actual guid values.

            string guidString;

            if (_assemblyGuidMap.TryGetValue(assembly, out guidString))
            {
                return guidString;
            }

            Debug.Assert(guidString == null);

            assembly.GetGuidString(out guidString);
            return _assemblyGuidMap.GetOrAdd(assembly, guidString);
        }

        protected override void OnGetTypesCompleted(ImmutableArray<EmbeddedType> types, DiagnosticBag diagnostics)
        {
            foreach (EmbeddedType t in types)
            {
                // Note, once we reached this point we are no longer interested in guid values, using null.
                _assemblyGuidMap.TryAdd(t.UnderlyingNamedType.AdaptedSymbol.ContainingAssembly, null);
            }

            foreach (AssemblySymbol a in ModuleBeingBuilt.GetReferencedAssembliesUsedSoFar())
            {
                ReportIndirectReferencesToLinkedAssemblies(a, diagnostics);
            }
        }

        protected override void ReportNameCollisionBetweenEmbeddedTypes(EmbeddedType typeA, EmbeddedType typeB, DiagnosticBag diagnostics)
        {
            var underlyingTypeA = typeA.UnderlyingNamedType;
            var underlyingTypeB = typeB.UnderlyingNamedType;
            Error(diagnostics, ErrorCode.ERR_InteropTypesWithSameNameAndGuid, null,
                                underlyingTypeA.AdaptedNamedTypeSymbol,
                                underlyingTypeA.AdaptedSymbol.ContainingAssembly,
                                underlyingTypeB.AdaptedSymbol.ContainingAssembly);
        }

        protected override void ReportNameCollisionWithAlreadyDeclaredType(EmbeddedType type, DiagnosticBag diagnostics)
        {
            var underlyingType = type.UnderlyingNamedType;
            Error(diagnostics, ErrorCode.ERR_LocalTypeNameClash, null,
                            underlyingType.AdaptedNamedTypeSymbol,
                            underlyingType.AdaptedSymbol.ContainingAssembly);
        }

        internal override void ReportIndirectReferencesToLinkedAssemblies(AssemblySymbol a, DiagnosticBag diagnostics)
        {
            Debug.Assert(IsFrozen);

            // We are emitting an assembly, A, which /references some assembly, B, and 
            // /links some other assembly, C, so that it can use C's types (by embedding them)
            // without having an assemblyref to C itself.
            // We can say that A has an indirect reference to each assembly that B references. 
            // In this function, we are looking for the situation where B has an assemblyref to C,
            // thus giving A an indirect reference to C. If so, we will report a warning.

            foreach (ModuleSymbol m in a.Modules)
            {
                foreach (AssemblySymbol indirectRef in m.GetReferencedAssemblySymbols())
                {
                    if (!indirectRef.IsMissing && indirectRef.IsLinked && _assemblyGuidMap.ContainsKey(indirectRef))
                    {
                        // WRNID_IndirectRefToLinkedAssembly2/WRN_ReferencedAssemblyReferencesLinkedPIA
                        Error(diagnostics, ErrorCode.WRN_ReferencedAssemblyReferencesLinkedPIA, null,
                                           indirectRef, a);
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the type can be embedded. If the type is defined in a linked (/l-ed)
        /// assembly, but doesn't meet embeddable type requirements, this function returns false
        /// and reports appropriate diagnostics.
        /// </summary>
        internal static bool IsValidEmbeddableType(
            NamedTypeSymbol namedType,
            SyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics,
            EmbeddedTypesManager optTypeManager = null)
        {
            // We do not embed SpecialTypes (they must be defined in Core assembly), error types and 
            // types from assemblies that aren't linked.
            if (namedType.SpecialType != SpecialType.None || namedType.IsErrorType() || !namedType.ContainingAssembly.IsLinked)
            {
                // Assuming that we already complained about an error type, no additional diagnostics necessary.
                return false;
            }

            ErrorCode error = ErrorCode.Unknown;

            switch (namedType.TypeKind)
            {
                case TypeKind.Interface:
                    foreach (Symbol member in namedType.GetMembersUnordered())
                    {
                        if (member.Kind != SymbolKind.NamedType)
                        {
                            if (!member.IsAbstract)
                            {
                                error = ErrorCode.ERR_DefaultInterfaceImplementationInNoPIAType;
                                break;
                            }
                            else if (member.IsSealed)
                            {
                                error = ErrorCode.ERR_ReAbstractionInNoPIAType;
                                break;
                            }
                        }
                    }

                    if (error != ErrorCode.Unknown)
                    {
                        break;
                    }

                    goto case TypeKind.Struct;
                case TypeKind.Struct:
                case TypeKind.Delegate:
                case TypeKind.Enum:

                    // We do not support nesting for embedded types.
                    // ERRID.ERR_InvalidInteropType/ERR_NoPIANestedType
                    if ((object)namedType.ContainingType != null)
                    {
                        error = ErrorCode.ERR_NoPIANestedType;
                        break;
                    }

                    // We do not support generic embedded types.
                    // ERRID.ERR_CannotEmbedInterfaceWithGeneric/ERR_GenericsUsedInNoPIAType
                    if (namedType.IsGenericType)
                    {
                        error = ErrorCode.ERR_GenericsUsedInNoPIAType;
                        break;
                    }

                    break;
                default:
                    // ERRID.ERR_CannotLinkClassWithNoPIA1/ERR_NewCoClassOnLink
                    error = ErrorCode.ERR_NewCoClassOnLink;
                    break;
            }

            if (error != ErrorCode.Unknown)
            {
                ReportNotEmbeddableSymbol(error, namedType, syntaxNodeOpt, diagnostics, optTypeManager);
                return false;
            }

            return true;
        }

        private static void ReportNotEmbeddableSymbol(ErrorCode error, Symbol symbol, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics, EmbeddedTypesManager optTypeManager)
        {
            // Avoid complaining about the same symbol too much.
            if (optTypeManager == null || optTypeManager._reportedSymbolsMap.TryAdd(symbol.OriginalDefinition, true))
            {
                Error(diagnostics, error, syntaxNodeOpt, symbol.OriginalDefinition);
            }
        }

        internal static void Error(DiagnosticBag diagnostics, ErrorCode code, SyntaxNode syntaxOpt, params object[] args)
        {
            Error(diagnostics, syntaxOpt, new CSDiagnosticInfo(code, args));
        }

        private static void Error(DiagnosticBag diagnostics, SyntaxNode syntaxOpt, DiagnosticInfo info)
        {
            diagnostics.Add(new CSDiagnostic(info, syntaxOpt == null ? NoLocation.Singleton : syntaxOpt.Location));
        }

        internal Cci.INamedTypeReference EmbedTypeIfNeedTo(
            NamedTypeSymbol namedType,
            bool fromImplements,
            SyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(namedType.IsDefinition);
            Debug.Assert(ModuleBeingBuilt.SourceModule.AnyReferencedAssembliesAreLinked);

            if (IsValidEmbeddableType(namedType, syntaxNodeOpt, diagnostics, this))
            {
                return EmbedType(namedType, fromImplements, syntaxNodeOpt, diagnostics);
            }

            return null;
        }

        private EmbeddedType EmbedType(
            NamedTypeSymbol namedType,
            bool fromImplements,
            SyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(namedType.IsDefinition);

            var adapter = namedType.GetCciAdapter();
            EmbeddedType embedded = new EmbeddedType(this, adapter);
            EmbeddedType cached = EmbeddedTypesMap.GetOrAdd(adapter, embedded);

            bool isInterface = (namedType.IsInterface);

            if (isInterface && fromImplements)
            {
                // Note, we must use 'cached' here because we might drop 'embedded' below.
                cached.EmbedAllMembersOfImplementedInterface(syntaxNodeOpt, diagnostics);
            }

            if (embedded != cached)
            {
                return cached;
            }

            // We do not expect this method to be called on a different thread once GetTypes is called.
            // Therefore, the following check can be as simple as:
            Debug.Assert(!IsFrozen, "Set of embedded types is frozen.");

            var noPiaIndexer = new Cci.TypeReferenceIndexer(new EmitContext(ModuleBeingBuilt, syntaxNodeOpt, diagnostics, metadataOnly: false, includePrivateMembers: true));

            // Make sure we embed all types referenced by the type declaration: implemented interfaces, etc.
            noPiaIndexer.VisitTypeDefinitionNoMembers(embedded);

            if (!isInterface)
            {
                Debug.Assert(namedType.TypeKind == TypeKind.Struct || namedType.TypeKind == TypeKind.Enum || namedType.TypeKind == TypeKind.Delegate);
                // For structures, enums and delegates we embed all members.

                if (namedType.TypeKind == TypeKind.Struct || namedType.TypeKind == TypeKind.Enum)
                {
                    // TODO: When building debug versions in the IDE, the compiler will insert some extra members
                    // that support ENC. These make no sense in local types, so we will skip them. We have to
                    // check for them explicitly or they will trip the member-validity check that follows.
                }

                foreach (FieldSymbol f in namedType.GetFieldsToEmit())
                {
                    EmbedField(embedded, f.GetCciAdapter(), syntaxNodeOpt, diagnostics);
                }

                foreach (MethodSymbol m in namedType.GetMethodsToEmit())
                {
                    if ((object)m != null)
                    {
                        EmbedMethod(embedded, m.GetCciAdapter(), syntaxNodeOpt, diagnostics);
                    }
                }

                // We also should embed properties and events, but we don't need to do this explicitly here
                // because accessors embed them automatically.
            }

            return embedded;
        }

        internal override EmbeddedField EmbedField(
            EmbeddedType type,
            FieldSymbolAdapter field,
            SyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(field.AdaptedSymbol.IsDefinition);

            EmbeddedField embedded = new EmbeddedField(type, field);
            EmbeddedField cached = EmbeddedFieldsMap.GetOrAdd(field, embedded);

            if (embedded != cached)
            {
                return cached;
            }

            // We do not expect this method to be called on a different thread once GetTypes is called.
            // Therefore, the following check can be as simple as:
            Debug.Assert(!IsFrozen, "Set of embedded fields is frozen.");

            // Embed types referenced by this field declaration.
            EmbedReferences(embedded, syntaxNodeOpt, diagnostics);

            var containerKind = field.AdaptedFieldSymbol.ContainingType.TypeKind;

            // Structures may contain only public instance fields. 
            if (containerKind == TypeKind.Interface || containerKind == TypeKind.Delegate ||
                (containerKind == TypeKind.Struct && (field.AdaptedFieldSymbol.IsStatic || field.AdaptedFieldSymbol.DeclaredAccessibility != Accessibility.Public)))
            {
                // ERRID.ERR_InvalidStructMemberNoPIA1/ERR_InteropStructContainsMethods 
                ReportNotEmbeddableSymbol(ErrorCode.ERR_InteropStructContainsMethods, field.AdaptedFieldSymbol.ContainingType, syntaxNodeOpt, diagnostics, this);
            }

            return embedded;
        }

        internal override EmbeddedMethod EmbedMethod(
            EmbeddedType type,
            MethodSymbolAdapter method,
            SyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(method.AdaptedSymbol.IsDefinition);
            Debug.Assert(!method.AdaptedMethodSymbol.IsDefaultValueTypeConstructor());

            EmbeddedMethod embedded = new EmbeddedMethod(type, method);
            EmbeddedMethod cached = EmbeddedMethodsMap.GetOrAdd(method, embedded);

            if (embedded != cached)
            {
                return cached;
            }

            // We do not expect this method to be called on a different thread once GetTypes is called.
            // Therefore, the following check can be as simple as:
            Debug.Assert(!IsFrozen, "Set of embedded methods is frozen.");

            // Embed types referenced by this method declaration.
            EmbedReferences(embedded, syntaxNodeOpt, diagnostics);

            switch (type.UnderlyingNamedType.AdaptedNamedTypeSymbol.TypeKind)
            {
                case TypeKind.Struct:
                case TypeKind.Enum:
                    // ERRID.ERR_InvalidStructMemberNoPIA1/ERR_InteropStructContainsMethods
                    ReportNotEmbeddableSymbol(ErrorCode.ERR_InteropStructContainsMethods, type.UnderlyingNamedType.AdaptedNamedTypeSymbol, syntaxNodeOpt, diagnostics, this);
                    break;

                default:
                    if (Cci.Extensions.HasBody(embedded))
                    {
                        // ERRID.ERR_InteropMethodWithBody1/ERR_InteropMethodWithBody
                        Error(diagnostics, ErrorCode.ERR_InteropMethodWithBody, syntaxNodeOpt, method.AdaptedMethodSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                    }
                    break;
            }

            // If this proc happens to belong to a property/event, we should include the property/event as well.
            Symbol propertyOrEvent = method.AdaptedMethodSymbol.AssociatedSymbol;
            if ((object)propertyOrEvent != null)
            {
                switch (propertyOrEvent.Kind)
                {
                    case SymbolKind.Property:
                        EmbedProperty(type, ((PropertySymbol)propertyOrEvent).GetCciAdapter(), syntaxNodeOpt, diagnostics);
                        break;
                    case SymbolKind.Event:
                        EmbedEvent(type, ((EventSymbol)propertyOrEvent).GetCciAdapter(), syntaxNodeOpt, diagnostics, isUsedForComAwareEventBinding: false);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(propertyOrEvent.Kind);
                }
            }

            return embedded;
        }

        internal override EmbeddedProperty EmbedProperty(
            EmbeddedType type,
            PropertySymbolAdapter property,
            SyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(property.AdaptedPropertySymbol.IsDefinition);

            // Make sure accessors are embedded.
            var getMethod = property.AdaptedPropertySymbol.GetMethod?.GetCciAdapter();
            var setMethod = property.AdaptedPropertySymbol.SetMethod?.GetCciAdapter();

            EmbeddedMethod embeddedGet = (object)getMethod != null ? EmbedMethod(type, getMethod, syntaxNodeOpt, diagnostics) : null;
            EmbeddedMethod embeddedSet = (object)setMethod != null ? EmbedMethod(type, setMethod, syntaxNodeOpt, diagnostics) : null;

            EmbeddedProperty embedded = new EmbeddedProperty(property, embeddedGet, embeddedSet);
            EmbeddedProperty cached = EmbeddedPropertiesMap.GetOrAdd(property, embedded);

            if (embedded != cached)
            {
                return cached;
            }

            // We do not expect this method to be called on a different thread once GetTypes is called.
            // Therefore, the following check can be as simple as:
            Debug.Assert(!IsFrozen, "Set of embedded properties is frozen.");

            // Embed types referenced by this property declaration.
            // This should also embed accessors.
            EmbedReferences(embedded, syntaxNodeOpt, diagnostics);

            return embedded;
        }

        internal override EmbeddedEvent EmbedEvent(
            EmbeddedType type,
            EventSymbolAdapter @event,
            SyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics,
            bool isUsedForComAwareEventBinding)
        {
            Debug.Assert(@event.AdaptedSymbol.IsDefinition);

            // Make sure accessors are embedded.
            var addMethod = @event.AdaptedEventSymbol.AddMethod?.GetCciAdapter();
            var removeMethod = @event.AdaptedEventSymbol.RemoveMethod?.GetCciAdapter();

            EmbeddedMethod embeddedAdd = (object)addMethod != null ? EmbedMethod(type, addMethod, syntaxNodeOpt, diagnostics) : null;
            EmbeddedMethod embeddedRemove = (object)removeMethod != null ? EmbedMethod(type, removeMethod, syntaxNodeOpt, diagnostics) : null;

            EmbeddedEvent embedded = new EmbeddedEvent(@event, embeddedAdd, embeddedRemove);
            EmbeddedEvent cached = EmbeddedEventsMap.GetOrAdd(@event, embedded);

            if (embedded != cached)
            {
                if (isUsedForComAwareEventBinding)
                {
                    cached.EmbedCorrespondingComEventInterfaceMethod(syntaxNodeOpt, diagnostics, isUsedForComAwareEventBinding);
                }

                return cached;
            }

            // We do not expect this method to be called on a different thread once GetTypes is called.
            // Therefore, the following check can be as simple as:
            Debug.Assert(!IsFrozen, "Set of embedded events is frozen.");

            // Embed types referenced by this event declaration.
            // This should also embed accessors.
            EmbedReferences(embedded, syntaxNodeOpt, diagnostics);

            embedded.EmbedCorrespondingComEventInterfaceMethod(syntaxNodeOpt, diagnostics, isUsedForComAwareEventBinding);

            return embedded;
        }

        protected override EmbeddedType GetEmbeddedTypeForMember(SymbolAdapter member, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            Debug.Assert(member.AdaptedSymbol.IsDefinition);
            Debug.Assert(ModuleBeingBuilt.SourceModule.AnyReferencedAssembliesAreLinked);

            if (member.AdaptedSymbol.OriginalDefinition is SynthesizedGlobalMethodSymbol)
            {
                // No need to embed an internal type from current assembly
                return null;
            }

            NamedTypeSymbol namedType = member.AdaptedSymbol.ContainingType;

            if (IsValidEmbeddableType(namedType, syntaxNodeOpt, diagnostics, this))
            {
                // It is possible that we have found a reference to a member before
                // encountering a reference to its container; make sure the container gets included.
                const bool fromImplements = false;
                return EmbedType(namedType, fromImplements, syntaxNodeOpt, diagnostics);
            }

            return null;
        }

        internal static ImmutableArray<EmbeddedParameter> EmbedParameters(
            CommonEmbeddedMember containingPropertyOrMethod,
            ImmutableArray<ParameterSymbol> underlyingParameters)
        {
            return underlyingParameters.SelectAsArray((p, c) => new EmbeddedParameter(c, p.GetCciAdapter()), containingPropertyOrMethod);
        }

        protected override CSharpAttributeData CreateCompilerGeneratedAttribute()
        {
            Debug.Assert(WellKnownMembers.IsSynthesizedAttributeOptional(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
            var compilation = ModuleBeingBuilt.Compilation;
            return compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor);
        }
    }
}
