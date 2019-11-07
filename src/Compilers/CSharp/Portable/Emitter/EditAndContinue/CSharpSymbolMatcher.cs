// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal sealed class CSharpSymbolMatcher : SymbolMatcher
    {
        private readonly MatchDefs _defs;
        private readonly MatchSymbols _symbols;

        public CSharpSymbolMatcher(
            IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> anonymousTypeMap,
            SourceAssemblySymbol sourceAssembly,
            EmitContext sourceContext,
            SourceAssemblySymbol otherAssembly,
            EmitContext otherContext,
            ImmutableDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>> otherSynthesizedMembersOpt)
        {
            _defs = new MatchDefsToSource(sourceContext, otherContext);
            _symbols = new MatchSymbols(anonymousTypeMap, sourceAssembly, otherAssembly, otherSynthesizedMembersOpt, new DeepTranslator(otherAssembly.GetSpecialType(SpecialType.System_Object)));
        }

        public CSharpSymbolMatcher(
            IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> anonymousTypeMap,
            SourceAssemblySymbol sourceAssembly,
            EmitContext sourceContext,
            PEAssemblySymbol otherAssembly)
        {
            _defs = new MatchDefsToMetadata(sourceContext, otherAssembly);

            _symbols = new MatchSymbols(
                anonymousTypeMap,
                sourceAssembly,
                otherAssembly,
                otherSynthesizedMembersOpt: null,
                deepTranslatorOpt: null);
        }

        public override Cci.IDefinition MapDefinition(Cci.IDefinition definition)
        {
            if (definition is Symbol symbol)
            {
                return (Cci.IDefinition)_symbols.Visit(symbol);
            }

            return _defs.VisitDef(definition);
        }

        public override Cci.INamespace MapNamespace(Cci.INamespace @namespace)
        {
            return (Cci.INamespace)_symbols.Visit((NamespaceSymbol)@namespace);
        }

        public override Cci.ITypeReference MapReference(Cci.ITypeReference reference)
        {
            if (reference is Symbol symbol)
            {
                return (Cci.ITypeReference)_symbols.Visit(symbol);
            }

            return null;
        }

        internal bool TryGetAnonymousTypeName(AnonymousTypeManager.AnonymousTypeTemplateSymbol template, out string name, out int index)
        {
            return _symbols.TryGetAnonymousTypeName(template, out name, out index);
        }

        private abstract class MatchDefs
        {
            private readonly EmitContext _sourceContext;
            private readonly ConcurrentDictionary<Cci.IDefinition, Cci.IDefinition> _matches;
            private IReadOnlyDictionary<string, Cci.INamespaceTypeDefinition> _lazyTopLevelTypes;

            public MatchDefs(EmitContext sourceContext)
            {
                _sourceContext = sourceContext;
                _matches = new ConcurrentDictionary<Cci.IDefinition, Cci.IDefinition>(ReferenceEqualityComparer.Instance);
            }

            public Cci.IDefinition VisitDef(Cci.IDefinition def)
            {
                return _matches.GetOrAdd(def, this.VisitDefInternal);
            }

            private Cci.IDefinition VisitDefInternal(Cci.IDefinition def)
            {
                var type = def as Cci.ITypeDefinition;
                if (type != null)
                {
                    var namespaceType = type.AsNamespaceTypeDefinition(_sourceContext);
                    if (namespaceType != null)
                    {
                        return VisitNamespaceType(namespaceType);
                    }

                    var nestedType = type.AsNestedTypeDefinition(_sourceContext);
                    Debug.Assert(nestedType != null);

                    var otherContainer = (Cci.ITypeDefinition)this.VisitDef(nestedType.ContainingTypeDefinition);
                    if (otherContainer == null)
                    {
                        return null;
                    }

                    return VisitTypeMembers(otherContainer, nestedType, GetNestedTypes, (a, b) => StringOrdinalComparer.Equals(a.Name, b.Name));
                }

                var member = def as Cci.ITypeDefinitionMember;
                if (member != null)
                {
                    var otherContainer = (Cci.ITypeDefinition)this.VisitDef(member.ContainingTypeDefinition);
                    if (otherContainer == null)
                    {
                        return null;
                    }

                    var field = def as Cci.IFieldDefinition;
                    if (field != null)
                    {
                        return VisitTypeMembers(otherContainer, field, GetFields, (a, b) => StringOrdinalComparer.Equals(a.Name, b.Name));
                    }
                }

                // We are only expecting types and fields currently.
                throw ExceptionUtilities.UnexpectedValue(def);
            }

            protected abstract IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypes();
            protected abstract IEnumerable<Cci.INestedTypeDefinition> GetNestedTypes(Cci.ITypeDefinition def);
            protected abstract IEnumerable<Cci.IFieldDefinition> GetFields(Cci.ITypeDefinition def);

            private Cci.INamespaceTypeDefinition VisitNamespaceType(Cci.INamespaceTypeDefinition def)
            {
                // All generated top-level types are assumed to be in the global namespace.
                // However, this may be an embedded NoPIA type within a namespace.
                // Since we do not support edits that include references to NoPIA types
                // (see #855640), it's reasonable to simply drop such cases.
                if (!string.IsNullOrEmpty(def.NamespaceName))
                {
                    return null;
                }

                var topLevelTypes = GetTopLevelTypesByName();
                Cci.INamespaceTypeDefinition otherDef;
                topLevelTypes.TryGetValue(def.Name, out otherDef);
                return otherDef;
            }

            private IReadOnlyDictionary<string, Cci.INamespaceTypeDefinition> GetTopLevelTypesByName()
            {
                if (_lazyTopLevelTypes == null)
                {
                    var typesByName = new Dictionary<string, Cci.INamespaceTypeDefinition>(StringOrdinalComparer.Instance);
                    foreach (var type in GetTopLevelTypes())
                    {
                        // All generated top-level types are assumed to be in the global namespace.
                        if (string.IsNullOrEmpty(type.NamespaceName))
                        {
                            typesByName.Add(type.Name, type);
                        }
                    }

                    Interlocked.CompareExchange(ref _lazyTopLevelTypes, typesByName, null);
                }

                return _lazyTopLevelTypes;
            }

            private static T VisitTypeMembers<T>(
                Cci.ITypeDefinition otherContainer,
                T member,
                Func<Cci.ITypeDefinition, IEnumerable<T>> getMembers,
                Func<T, T, bool> predicate)
                where T : class, Cci.ITypeDefinitionMember
            {
                // We could cache the members by name (see Matcher.VisitNamedTypeMembers)
                // but the assumption is this class is only used for types with few members
                // so caching is not necessary and linear search is acceptable.
                return getMembers(otherContainer).FirstOrDefault(otherMember => predicate(member, otherMember));
            }
        }

        private sealed class MatchDefsToMetadata : MatchDefs
        {
            private readonly PEAssemblySymbol _otherAssembly;

            public MatchDefsToMetadata(EmitContext sourceContext, PEAssemblySymbol otherAssembly) :
                base(sourceContext)
            {
                _otherAssembly = otherAssembly;
            }

            protected override IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypes()
            {
                var builder = ArrayBuilder<Cci.INamespaceTypeDefinition>.GetInstance();
                GetTopLevelTypes(builder, _otherAssembly.GlobalNamespace);
                return builder.ToArrayAndFree();
            }

            protected override IEnumerable<Cci.INestedTypeDefinition> GetNestedTypes(Cci.ITypeDefinition def)
            {
                var type = (PENamedTypeSymbol)def;
                return type.GetTypeMembers().Cast<Cci.INestedTypeDefinition>();
            }

            protected override IEnumerable<Cci.IFieldDefinition> GetFields(Cci.ITypeDefinition def)
            {
                var type = (PENamedTypeSymbol)def;
                return type.GetFieldsToEmit().Cast<Cci.IFieldDefinition>();
            }

            private static void GetTopLevelTypes(ArrayBuilder<Cci.INamespaceTypeDefinition> builder, NamespaceSymbol @namespace)
            {
                foreach (var member in @namespace.GetMembers())
                {
                    if (member.Kind == SymbolKind.Namespace)
                    {
                        GetTopLevelTypes(builder, (NamespaceSymbol)member);
                    }
                    else
                    {
                        builder.Add((Cci.INamespaceTypeDefinition)member);
                    }
                }
            }
        }

        private sealed class MatchDefsToSource : MatchDefs
        {
            private readonly EmitContext _otherContext;

            public MatchDefsToSource(
                EmitContext sourceContext,
                EmitContext otherContext) :
                base(sourceContext)
            {
                _otherContext = otherContext;
            }

            protected override IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypes()
            {
                return _otherContext.Module.GetTopLevelTypeDefinitions(_otherContext);
            }

            protected override IEnumerable<Cci.INestedTypeDefinition> GetNestedTypes(Cci.ITypeDefinition def)
            {
                return def.GetNestedTypes(_otherContext);
            }

            protected override IEnumerable<Cci.IFieldDefinition> GetFields(Cci.ITypeDefinition def)
            {
                return def.GetFields(_otherContext);
            }
        }

        private sealed class MatchSymbols : CSharpSymbolVisitor<Symbol>
        {
            private readonly IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> _anonymousTypeMap;
            private readonly SourceAssemblySymbol _sourceAssembly;

            // metadata or source assembly:
            private readonly AssemblySymbol _otherAssembly;

            /// <summary>
            /// Members that are not listed directly on their containing type or namespace symbol as they were synthesized in a lowering phase,
            /// after the symbol has been created.
            /// </summary>
            private readonly ImmutableDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>> _otherSynthesizedMembersOpt;

            private readonly SymbolComparer _comparer;
            private readonly ConcurrentDictionary<Symbol, Symbol> _matches;

            /// <summary>
            /// A cache of members per type, populated when the first member for a given
            /// type is needed. Within each type, members are indexed by name. The reason
            /// for caching, and indexing by name, is to avoid searching sequentially
            /// through all members of a given kind each time a member is matched.
            /// </summary>
            private readonly ConcurrentDictionary<ISymbolInternal, IReadOnlyDictionary<string, ImmutableArray<ISymbolInternal>>> _otherMembers;

            public MatchSymbols(
                IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> anonymousTypeMap,
                SourceAssemblySymbol sourceAssembly,
                AssemblySymbol otherAssembly,
                ImmutableDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>> otherSynthesizedMembersOpt,
                DeepTranslator deepTranslatorOpt)
            {
                _anonymousTypeMap = anonymousTypeMap;
                _sourceAssembly = sourceAssembly;
                _otherAssembly = otherAssembly;
                _otherSynthesizedMembersOpt = otherSynthesizedMembersOpt;
                _comparer = new SymbolComparer(this, deepTranslatorOpt);
                _matches = new ConcurrentDictionary<Symbol, Symbol>(ReferenceEqualityComparer.Instance);
                _otherMembers = new ConcurrentDictionary<ISymbolInternal, IReadOnlyDictionary<string, ImmutableArray<ISymbolInternal>>>(ReferenceEqualityComparer.Instance);
            }

            internal bool TryGetAnonymousTypeName(AnonymousTypeManager.AnonymousTypeTemplateSymbol type, out string name, out int index)
            {
                if (TryFindAnonymousType(type, out var otherType))
                {
                    name = otherType.Name;
                    index = otherType.UniqueIndex;
                    return true;
                }

                name = null;
                index = -1;
                return false;
            }

            public override Symbol DefaultVisit(Symbol symbol)
            {
                // Symbol should have been handled elsewhere.
                throw ExceptionUtilities.Unreachable;
            }

            public override Symbol Visit(Symbol symbol)
            {
                Debug.Assert((object)symbol.ContainingAssembly != (object)_otherAssembly);

                // Add an entry for the match, even if there is no match, to avoid
                // matching the same symbol unsuccessfully multiple times.
                return _matches.GetOrAdd(symbol, base.Visit);
            }

            public override Symbol VisitArrayType(ArrayTypeSymbol symbol)
            {
                var otherElementType = (TypeSymbol)this.Visit(symbol.ElementType);
                if (otherElementType is null)
                {
                    // For a newly added type, there is no match in the previous generation, so it could be null.
                    return null;
                }

                var otherModifiers = VisitCustomModifiers(symbol.ElementTypeWithAnnotations.CustomModifiers);

                if (symbol.IsSZArray)
                {
                    return ArrayTypeSymbol.CreateSZArray(_otherAssembly, symbol.ElementTypeWithAnnotations.WithTypeAndModifiers(otherElementType, otherModifiers));
                }

                return ArrayTypeSymbol.CreateMDArray(_otherAssembly, symbol.ElementTypeWithAnnotations.WithTypeAndModifiers(otherElementType, otherModifiers), symbol.Rank, symbol.Sizes, symbol.LowerBounds);
            }

            public override Symbol VisitEvent(EventSymbol symbol)
            {
                return this.VisitNamedTypeMember(symbol, AreEventsEqual);
            }

            public override Symbol VisitField(FieldSymbol symbol)
            {
                return this.VisitNamedTypeMember(symbol, AreFieldsEqual);
            }

            public override Symbol VisitMethod(MethodSymbol symbol)
            {
                // Not expecting constructed method.
                Debug.Assert(symbol.IsDefinition);
                return this.VisitNamedTypeMember(symbol, AreMethodsEqual);
            }

            public override Symbol VisitModule(ModuleSymbol module)
            {
                var otherAssembly = (AssemblySymbol)Visit(module.ContainingAssembly);
                if (otherAssembly is null)
                {
                    return null;
                }

                // manifest module:
                if (module.Ordinal == 0)
                {
                    return otherAssembly.Modules[0];
                }

                // match non-manifest module by name:
                for (int i = 1; i < otherAssembly.Modules.Length; i++)
                {
                    var otherModule = otherAssembly.Modules[i];

                    // use case sensitive comparison -- modules whose names differ in casing are considered distinct:
                    if (StringComparer.Ordinal.Equals(otherModule.Name, module.Name))
                    {
                        return otherModule;
                    }
                }

                return null;
            }

            public override Symbol VisitAssembly(AssemblySymbol assembly)
            {
                if (assembly.IsLinked)
                {
                    return assembly;
                }

                // When we map synthesized symbols from previous generations to the latest compilation 
                // we might encounter a symbol that is defined in arbitrary preceding generation, 
                // not just the immediately preceding generation. If the source assembly uses time-based 
                // versioning assemblies of preceding generations might differ in their version number.
                if (IdentityEqualIgnoringVersionWildcard(assembly, _sourceAssembly))
                {
                    return _otherAssembly;
                }

                // find a referenced assembly with the same source identity (modulo assembly version patterns):
                foreach (var otherReferencedAssembly in _otherAssembly.Modules[0].ReferencedAssemblySymbols)
                {
                    if (IdentityEqualIgnoringVersionWildcard(assembly, otherReferencedAssembly))
                    {
                        return otherReferencedAssembly;
                    }
                }

                return null;
            }

            private static bool IdentityEqualIgnoringVersionWildcard(AssemblySymbol left, AssemblySymbol right)
            {
                var leftIdentity = left.Identity;
                var rightIdentity = right.Identity;

                return AssemblyIdentityComparer.SimpleNameComparer.Equals(leftIdentity.Name, rightIdentity.Name) &&
                       (left.AssemblyVersionPattern ?? leftIdentity.Version).Equals(right.AssemblyVersionPattern ?? rightIdentity.Version) &&
                       AssemblyIdentity.EqualIgnoringNameAndVersion(leftIdentity, rightIdentity);
            }

            public override Symbol VisitNamespace(NamespaceSymbol @namespace)
            {
                var otherContainer = Visit(@namespace.ContainingSymbol);
                Debug.Assert(otherContainer is object);

                switch (otherContainer.Kind)
                {
                    case SymbolKind.NetModule:
                        Debug.Assert(@namespace.IsGlobalNamespace);
                        return ((ModuleSymbol)otherContainer).GlobalNamespace;

                    case SymbolKind.Namespace:
                        return FindMatchingMember(otherContainer, @namespace, AreNamespacesEqual);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(otherContainer.Kind);
                }
            }

            public override Symbol VisitDynamicType(DynamicTypeSymbol symbol)
            {
                return _otherAssembly.GetSpecialType(SpecialType.System_Object);
            }

            public override Symbol VisitNamedType(NamedTypeSymbol sourceType)
            {
                var originalDef = sourceType.OriginalDefinition;
                if ((object)originalDef != (object)sourceType)
                {
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    var typeArguments = sourceType.GetAllTypeArguments(ref useSiteDiagnostics);

                    var otherDef = (NamedTypeSymbol)Visit(originalDef);
                    if (otherDef is null)
                    {
                        return null;
                    }

                    var otherTypeParameters = otherDef.GetAllTypeParameters();
                    bool translationFailed = false;

                    var otherTypeArguments = typeArguments.SelectAsArray((t, v) =>
                    {
                        var newType = (TypeSymbol)v.Visit(t.Type);

                        if (newType is null)
                        {
                            // For a newly added type, there is no match in the previous generation, so it could be null.
                            translationFailed = true;
                            newType = t.Type;
                        }

                        return t.WithTypeAndModifiers(newType, v.VisitCustomModifiers(t.CustomModifiers));
                    }, this);

                    if (translationFailed)
                    {
                        // For a newly added type, there is no match in the previous generation, so it could be null.
                        return null;
                    }

                    // TODO: LambdaFrame has alpha renamed type parameters, should we rather fix that?
                    var typeMap = new TypeMap(otherTypeParameters, otherTypeArguments, allowAlpha: true);
                    return typeMap.SubstituteNamedType(otherDef);
                }
                else if (sourceType.IsTupleType)
                {
                    var otherDef = (NamedTypeSymbol)Visit(sourceType.TupleUnderlyingType);
                    if (otherDef is null || !otherDef.IsTupleOrCompatibleWithTupleOfCardinality(sourceType.TupleElementTypesWithAnnotations.Length))
                    {
                        return null;
                    }

                    return otherDef;
                }

                Debug.Assert(sourceType.IsDefinition);

                var otherContainer = this.Visit(sourceType.ContainingSymbol);
                // Containing type will be missing from other assembly
                // if the type was added in the (newer) source assembly.
                if (otherContainer is null)
                {
                    return null;
                }

                switch (otherContainer.Kind)
                {
                    case SymbolKind.Namespace:
                        if (sourceType is AnonymousTypeManager.AnonymousTypeTemplateSymbol template)
                        {
                            Debug.Assert((object)otherContainer == (object)_otherAssembly.GlobalNamespace);
                            TryFindAnonymousType(template, out var value);
                            return (NamedTypeSymbol)value.Type;
                        }

                        if (sourceType.IsAnonymousType)
                        {
                            return Visit(AnonymousTypeManager.TranslateAnonymousTypeSymbol(sourceType));
                        }

                        return FindMatchingMember(otherContainer, sourceType, AreNamedTypesEqual);

                    case SymbolKind.NamedType:
                        return FindMatchingMember(otherContainer, sourceType, AreNamedTypesEqual);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(otherContainer.Kind);
                }
            }

            public override Symbol VisitParameter(ParameterSymbol parameter)
            {
                // Should never reach here. Should be matched as a result of matching the container.
                throw ExceptionUtilities.Unreachable;
            }

            public override Symbol VisitPointerType(PointerTypeSymbol symbol)
            {
                var otherPointedAtType = (TypeSymbol)Visit(symbol.PointedAtType);
                if (otherPointedAtType is null)
                {
                    // For a newly added type, there is no match in the previous generation, so it could be null.
                    return null;
                }
                var otherModifiers = VisitCustomModifiers(symbol.PointedAtTypeWithAnnotations.CustomModifiers);
                return new PointerTypeSymbol(symbol.PointedAtTypeWithAnnotations.WithTypeAndModifiers(otherPointedAtType, otherModifiers));
            }

            public override Symbol VisitProperty(PropertySymbol symbol)
            {
                return this.VisitNamedTypeMember(symbol, ArePropertiesEqual);
            }

            public override Symbol VisitTypeParameter(TypeParameterSymbol symbol)
            {
                var indexed = symbol as IndexedTypeParameterSymbol;
                if ((object)indexed != null)
                {
                    return indexed;
                }

                ImmutableArray<TypeParameterSymbol> otherTypeParameters;
                var otherContainer = this.Visit(symbol.ContainingSymbol);
                Debug.Assert((object)otherContainer != null);

                switch (otherContainer.Kind)
                {
                    case SymbolKind.NamedType:
                    case SymbolKind.ErrorType:
                        otherTypeParameters = ((NamedTypeSymbol)otherContainer).TypeParameters;
                        break;
                    case SymbolKind.Method:
                        otherTypeParameters = ((MethodSymbol)otherContainer).TypeParameters;
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(otherContainer.Kind);
                }

                return otherTypeParameters[symbol.Ordinal];
            }

            private ImmutableArray<CustomModifier> VisitCustomModifiers(ImmutableArray<CustomModifier> modifiers)
            {
                return modifiers.SelectAsArray(VisitCustomModifier);
            }

            private CustomModifier VisitCustomModifier(CustomModifier modifier)
            {
                var type = (NamedTypeSymbol)this.Visit(((CSharpCustomModifier)modifier).ModifierSymbol);
                Debug.Assert((object)type != null);
                return modifier.IsOptional ?
                    CSharpCustomModifier.CreateOptional(type) :
                    CSharpCustomModifier.CreateRequired(type);
            }

            internal bool TryFindAnonymousType(AnonymousTypeManager.AnonymousTypeTemplateSymbol type, out AnonymousTypeValue otherType)
            {
                Debug.Assert((object)type.ContainingSymbol == (object)_sourceAssembly.GlobalNamespace);

                return _anonymousTypeMap.TryGetValue(type.GetAnonymousTypeKey(), out otherType);
            }

            private Symbol VisitNamedTypeMember<T>(T member, Func<T, T, bool> predicate)
                where T : Symbol
            {
                var otherType = (NamedTypeSymbol)Visit(member.ContainingType);

                // Containing type may be null for synthesized
                // types such as iterators.
                if (otherType is null)
                {
                    return null;
                }

                return FindMatchingMember(otherType, member, predicate);
            }

            private T FindMatchingMember<T>(ISymbolInternal otherTypeOrNamespace, T sourceMember, Func<T, T, bool> predicate)
                where T : Symbol
            {
                Debug.Assert(!string.IsNullOrEmpty(sourceMember.MetadataName));

                var otherMembersByName = _otherMembers.GetOrAdd(otherTypeOrNamespace, GetAllEmittedMembers);
                if (otherMembersByName.TryGetValue(sourceMember.MetadataName, out var otherMembers))
                {
                    foreach (var otherMember in otherMembers)
                    {
                        if (otherMember is T other && predicate(sourceMember, other))
                        {
                            return other;
                        }
                    }
                }

                return null;
            }

            private bool AreArrayTypesEqual(ArrayTypeSymbol type, ArrayTypeSymbol other)
            {
                // TODO: Test with overloads (from PE base class?) that have modifiers.
                Debug.Assert(type.ElementTypeWithAnnotations.CustomModifiers.IsEmpty);
                Debug.Assert(other.ElementTypeWithAnnotations.CustomModifiers.IsEmpty);

                return type.HasSameShapeAs(other) &&
                    AreTypesEqual(type.ElementType, other.ElementType);
            }

            private bool AreEventsEqual(EventSymbol @event, EventSymbol other)
            {
                Debug.Assert(StringOrdinalComparer.Equals(@event.Name, other.Name));
                return _comparer.Equals(@event.Type, other.Type);
            }

            private bool AreFieldsEqual(FieldSymbol field, FieldSymbol other)
            {
                Debug.Assert(StringOrdinalComparer.Equals(field.Name, other.Name));
                return _comparer.Equals(field.Type, other.Type);
            }

            private bool AreMethodsEqual(MethodSymbol method, MethodSymbol other)
            {
                Debug.Assert(StringOrdinalComparer.Equals(method.Name, other.Name));

                Debug.Assert(method.IsDefinition);
                Debug.Assert(other.IsDefinition);

                method = SubstituteTypeParameters(method);
                other = SubstituteTypeParameters(other);

                return _comparer.Equals(method.ReturnType, other.ReturnType) &&
                    method.RefKind.Equals(other.RefKind) &&
                    method.Parameters.SequenceEqual(other.Parameters, AreParametersEqual) &&
                    method.TypeParameters.SequenceEqual(other.TypeParameters, AreTypesEqual);
            }

            private static MethodSymbol SubstituteTypeParameters(MethodSymbol method)
            {
                Debug.Assert(method.IsDefinition);

                var typeParameters = method.TypeParameters;
                int n = typeParameters.Length;
                if (n == 0)
                {
                    return method;
                }

                return method.Construct(IndexedTypeParameterSymbol.Take(n).Cast<TypeParameterSymbol, TypeSymbol>());
            }

            private bool AreNamedTypesEqual(NamedTypeSymbol type, NamedTypeSymbol other)
            {
                Debug.Assert(StringOrdinalComparer.Equals(type.MetadataName, other.MetadataName));

                // TODO: Test with overloads (from PE base class?) that have modifiers.
                Debug.Assert(type.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.All(t => t.CustomModifiers.IsEmpty));
                Debug.Assert(other.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.All(t => t.CustomModifiers.IsEmpty));

                // Tuple types should be unwrapped to their underlying type before getting here (see MatchSymbols.VisitNamedType)
                Debug.Assert(!type.IsTupleType);
                Debug.Assert(!other.IsTupleType);

                return type.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.SequenceEqual(other.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics, AreTypesEqual);
            }

            private bool AreNamespacesEqual(NamespaceSymbol @namespace, NamespaceSymbol other)
            {
                Debug.Assert(StringOrdinalComparer.Equals(@namespace.MetadataName, other.MetadataName));
                return true;
            }

            private bool AreParametersEqual(ParameterSymbol parameter, ParameterSymbol other)
            {
                Debug.Assert(parameter.Ordinal == other.Ordinal);
                return StringOrdinalComparer.Equals(parameter.MetadataName, other.MetadataName) &&
                    (parameter.RefKind == other.RefKind) &&
                    _comparer.Equals(parameter.Type, other.Type);
            }

            private bool ArePointerTypesEqual(PointerTypeSymbol type, PointerTypeSymbol other)
            {
                // TODO: Test with overloads (from PE base class?) that have modifiers.
                Debug.Assert(type.PointedAtTypeWithAnnotations.CustomModifiers.IsEmpty);
                Debug.Assert(other.PointedAtTypeWithAnnotations.CustomModifiers.IsEmpty);

                return AreTypesEqual(type.PointedAtType, other.PointedAtType);
            }

            private bool ArePropertiesEqual(PropertySymbol property, PropertySymbol other)
            {
                Debug.Assert(StringOrdinalComparer.Equals(property.MetadataName, other.MetadataName));
                return _comparer.Equals(property.Type, other.Type) &&
                    property.RefKind.Equals(other.RefKind) &&
                    property.Parameters.SequenceEqual(other.Parameters, AreParametersEqual);
            }

            private static bool AreTypeParametersEqual(TypeParameterSymbol type, TypeParameterSymbol other)
            {
                Debug.Assert(type.Ordinal == other.Ordinal);
                Debug.Assert(StringOrdinalComparer.Equals(type.Name, other.Name));
                // Comparing constraints is unnecessary: two methods cannot differ by
                // constraints alone and changing the signature of a method is a rude
                // edit. Furthermore, comparing constraint types might lead to a cycle.
                Debug.Assert(type.HasConstructorConstraint == other.HasConstructorConstraint);
                Debug.Assert(type.HasValueTypeConstraint == other.HasValueTypeConstraint);
                Debug.Assert(type.HasUnmanagedTypeConstraint == other.HasUnmanagedTypeConstraint);
                Debug.Assert(type.HasReferenceTypeConstraint == other.HasReferenceTypeConstraint);
                Debug.Assert(type.ConstraintTypesNoUseSiteDiagnostics.Length == other.ConstraintTypesNoUseSiteDiagnostics.Length);
                return true;
            }

            private bool AreTypesEqual(TypeWithAnnotations type, TypeWithAnnotations other)
            {
                Debug.Assert(type.CustomModifiers.IsDefaultOrEmpty);
                Debug.Assert(other.CustomModifiers.IsDefaultOrEmpty);
                return AreTypesEqual(type.Type, other.Type);
            }

            private bool AreTypesEqual(TypeSymbol type, TypeSymbol other)
            {
                if (type.Kind != other.Kind)
                {
                    return false;
                }

                switch (type.Kind)
                {
                    case SymbolKind.ArrayType:
                        return AreArrayTypesEqual((ArrayTypeSymbol)type, (ArrayTypeSymbol)other);

                    case SymbolKind.PointerType:
                        return ArePointerTypesEqual((PointerTypeSymbol)type, (PointerTypeSymbol)other);

                    case SymbolKind.NamedType:
                    case SymbolKind.ErrorType:
                        return AreNamedTypesEqual((NamedTypeSymbol)type, (NamedTypeSymbol)other);

                    case SymbolKind.TypeParameter:
                        return AreTypeParametersEqual((TypeParameterSymbol)type, (TypeParameterSymbol)other);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(type.Kind);
                }
            }

            private IReadOnlyDictionary<string, ImmutableArray<ISymbolInternal>> GetAllEmittedMembers(ISymbolInternal symbol)
            {
                var members = ArrayBuilder<ISymbolInternal>.GetInstance();

                if (symbol.Kind == SymbolKind.NamedType)
                {
                    var type = (NamedTypeSymbol)symbol;
                    members.AddRange(type.GetEventsToEmit());
                    members.AddRange(type.GetFieldsToEmit());
                    members.AddRange(type.GetMethodsToEmit());
                    members.AddRange(type.GetTypeMembers());
                    members.AddRange(type.GetPropertiesToEmit());
                }
                else
                {
                    members.AddRange(((NamespaceSymbol)symbol).GetMembers());
                }

                if (_otherSynthesizedMembersOpt != null && _otherSynthesizedMembersOpt.TryGetValue(symbol, out var synthesizedMembers))
                {
                    members.AddRange(synthesizedMembers);
                }

                var result = members.ToDictionary(s => s.MetadataName, StringOrdinalComparer.Instance);
                members.Free();
                return result;
            }

            private sealed class SymbolComparer
            {
                private readonly MatchSymbols _matcher;
                private readonly DeepTranslator _deepTranslatorOpt;

                public SymbolComparer(MatchSymbols matcher, DeepTranslator deepTranslatorOpt)
                {
                    Debug.Assert(matcher != null);
                    _matcher = matcher;
                    _deepTranslatorOpt = deepTranslatorOpt;
                }

                public bool Equals(TypeSymbol source, TypeSymbol other)
                {
                    var visitedSource = (TypeSymbol)_matcher.Visit(source);
                    var visitedOther = (_deepTranslatorOpt != null) ? (TypeSymbol)_deepTranslatorOpt.Visit(other) : other;

                    return visitedSource?.Equals(visitedOther, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes) == true;
                }
            }
        }

        internal sealed class DeepTranslator : CSharpSymbolVisitor<Symbol>
        {
            private readonly ConcurrentDictionary<Symbol, Symbol> _matches;
            private readonly NamedTypeSymbol _systemObject;

            public DeepTranslator(NamedTypeSymbol systemObject)
            {
                _matches = new ConcurrentDictionary<Symbol, Symbol>(ReferenceEqualityComparer.Instance);
                _systemObject = systemObject;
            }

            public override Symbol DefaultVisit(Symbol symbol)
            {
                // Symbol should have been handled elsewhere.
                throw ExceptionUtilities.Unreachable;
            }

            public override Symbol Visit(Symbol symbol)
            {
                return _matches.GetOrAdd(symbol, base.Visit(symbol));
            }

            public override Symbol VisitArrayType(ArrayTypeSymbol symbol)
            {
                var translatedElementType = (TypeSymbol)this.Visit(symbol.ElementType);
                var translatedModifiers = VisitCustomModifiers(symbol.ElementTypeWithAnnotations.CustomModifiers);

                if (symbol.IsSZArray)
                {
                    return ArrayTypeSymbol.CreateSZArray(symbol.BaseTypeNoUseSiteDiagnostics.ContainingAssembly, symbol.ElementTypeWithAnnotations.WithTypeAndModifiers(translatedElementType, translatedModifiers));
                }

                return ArrayTypeSymbol.CreateMDArray(symbol.BaseTypeNoUseSiteDiagnostics.ContainingAssembly, symbol.ElementTypeWithAnnotations.WithTypeAndModifiers(translatedElementType, translatedModifiers), symbol.Rank, symbol.Sizes, symbol.LowerBounds);
            }

            public override Symbol VisitDynamicType(DynamicTypeSymbol symbol)
            {
                return _systemObject;
            }

            public override Symbol VisitNamedType(NamedTypeSymbol type)
            {
                if (type.IsTupleType)
                {
                    type = type.TupleUnderlyingType;
                    Debug.Assert(!type.IsTupleType);
                }

                var originalDef = type.OriginalDefinition;
                if ((object)originalDef != type)
                {
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    var translatedTypeArguments = type.GetAllTypeArguments(ref useSiteDiagnostics).SelectAsArray((t, v) => t.WithTypeAndModifiers((TypeSymbol)v.Visit(t.Type),
                                                                                                                                                  v.VisitCustomModifiers(t.CustomModifiers)),
                                                                                                                 this);

                    var translatedOriginalDef = (NamedTypeSymbol)this.Visit(originalDef);
                    var typeMap = new TypeMap(translatedOriginalDef.GetAllTypeParameters(), translatedTypeArguments, allowAlpha: true);
                    return typeMap.SubstituteNamedType(translatedOriginalDef);
                }

                Debug.Assert(type.IsDefinition);

                if (type.IsAnonymousType)
                {
                    return this.Visit(AnonymousTypeManager.TranslateAnonymousTypeSymbol(type));
                }

                return type;
            }

            public override Symbol VisitPointerType(PointerTypeSymbol symbol)
            {
                var translatedPointedAtType = (TypeSymbol)this.Visit(symbol.PointedAtType);
                var translatedModifiers = VisitCustomModifiers(symbol.PointedAtTypeWithAnnotations.CustomModifiers);
                return new PointerTypeSymbol(symbol.PointedAtTypeWithAnnotations.WithTypeAndModifiers(translatedPointedAtType, translatedModifiers));
            }

            public override Symbol VisitTypeParameter(TypeParameterSymbol symbol)
            {
                return symbol;
            }

            private ImmutableArray<CustomModifier> VisitCustomModifiers(ImmutableArray<CustomModifier> modifiers)
            {
                return modifiers.SelectAsArray(VisitCustomModifier);
            }

            private CustomModifier VisitCustomModifier(CustomModifier modifier)
            {
                var translatedType = (NamedTypeSymbol)this.Visit(((CSharpCustomModifier)modifier).ModifierSymbol);
                Debug.Assert((object)translatedType != null);
                return modifier.IsOptional ?
                    CSharpCustomModifier.CreateOptional(translatedType) :
                    CSharpCustomModifier.CreateRequired(translatedType);
            }
        }
    }
}
