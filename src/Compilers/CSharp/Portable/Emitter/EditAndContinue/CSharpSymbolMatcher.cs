// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal sealed class CSharpSymbolMatcher : SymbolMatcher
    {
        private static readonly StringComparer s_nameComparer = StringComparer.Ordinal;

        private readonly MatchDefs _defs;
        private readonly MatchSymbols _symbols;

        public CSharpSymbolMatcher(
            IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> anonymousTypeMap,
            SourceAssemblySymbol sourceAssembly,
            EmitContext sourceContext,
            SourceAssemblySymbol otherAssembly,
            EmitContext otherContext,
            ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> otherSynthesizedMembersOpt)
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
            var symbol = definition as Symbol;
            if ((object)symbol != null)
            {
                return (Cci.IDefinition)_symbols.Visit(symbol);
            }

            return _defs.VisitDef(definition);
        }

        public override Cci.ITypeReference MapReference(Cci.ITypeReference reference)
        {
            var symbol = reference as Symbol;
            if ((object)symbol != null)
            {
                return (Cci.ITypeReference)_symbols.Visit(symbol);
            }

            return null;
        }

        internal bool TryGetAnonymousTypeName(NamedTypeSymbol template, out string name, out int index)
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

                    return this.VisitTypeMembers(otherContainer, nestedType, GetNestedTypes, (a, b) => s_nameComparer.Equals(a.Name, b.Name));
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
                        return this.VisitTypeMembers(otherContainer, field, GetFields, (a, b) => s_nameComparer.Equals(a.Name, b.Name));
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

                var topLevelTypes = this.GetTopLevelTypesByName();
                Cci.INamespaceTypeDefinition otherDef;
                topLevelTypes.TryGetValue(def.Name, out otherDef);
                return otherDef;
            }

            private IReadOnlyDictionary<string, Cci.INamespaceTypeDefinition> GetTopLevelTypesByName()
            {
                if (_lazyTopLevelTypes == null)
                {
                    var typesByName = new Dictionary<string, Cci.INamespaceTypeDefinition>(s_nameComparer);
                    foreach (var type in this.GetTopLevelTypes())
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

            private T VisitTypeMembers<T>(
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
                return _otherContext.Module.GetTopLevelTypes(_otherContext);
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
            private readonly ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> _otherSynthesizedMembersOpt;

            private readonly SymbolComparer _comparer;
            private readonly ConcurrentDictionary<Symbol, Symbol> _matches;

            // A cache of members per type, populated when the first member for a given
            // type is needed. Within each type, members are indexed by name. The reason
            // for caching, and indexing by name, is to avoid searching sequentially
            // through all members of a given kind each time a member is matched.
            private readonly ConcurrentDictionary<NamedTypeSymbol, IReadOnlyDictionary<string, ImmutableArray<Cci.ITypeDefinitionMember>>> _otherTypeMembers;

            public MatchSymbols(
                IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> anonymousTypeMap,
                SourceAssemblySymbol sourceAssembly,
                AssemblySymbol otherAssembly,
                ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> otherSynthesizedMembersOpt,
                DeepTranslator deepTranslatorOpt)
            {
                _anonymousTypeMap = anonymousTypeMap;
                _sourceAssembly = sourceAssembly;
                _otherAssembly = otherAssembly;
                _otherSynthesizedMembersOpt = otherSynthesizedMembersOpt;
                _comparer = new SymbolComparer(this, deepTranslatorOpt);
                _matches = new ConcurrentDictionary<Symbol, Symbol>(ReferenceEqualityComparer.Instance);
                _otherTypeMembers = new ConcurrentDictionary<NamedTypeSymbol, IReadOnlyDictionary<string, ImmutableArray<Cci.ITypeDefinitionMember>>>();
            }

            internal bool TryGetAnonymousTypeName(NamedTypeSymbol type, out string name, out int index)
            {
                AnonymousTypeValue otherType;
                if (this.TryFindAnonymousType(type, out otherType))
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
                var otherElementType = (TypeSymbol)this.Visit(symbol.ElementType.TypeSymbol);
                if ((object)otherElementType == null)
                {
                    // For a newly added type, there is no match in the previous generation, so it could be null.
                    return null;
                }
                var otherModifiers = VisitCustomModifiers(symbol.ElementType.CustomModifiers);

                if (symbol.IsSZArray)
                {
                    return ArrayTypeSymbol.CreateSZArray(_otherAssembly, symbol.ElementType.Update(otherElementType, otherModifiers));
                }

                return ArrayTypeSymbol.CreateMDArray(_otherAssembly, symbol.ElementType.Update(otherElementType, otherModifiers), symbol.Rank, symbol.Sizes, symbol.LowerBounds);
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
                if ((object)otherAssembly == null)
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

            public override Symbol VisitAssembly(AssemblySymbol symbol)
            {
                if (symbol.IsLinked)
                {
                    return symbol;
                }

                // the current source assembly:
                if (symbol.Identity.Equals(_sourceAssembly.Identity))
                {
                    return _otherAssembly;
                }

                // find a referenced assembly with the exactly same identity:
                foreach (var otherReferencedAssembly in _otherAssembly.Modules[0].ReferencedAssemblySymbols)
                {
                    if (symbol.Identity.Equals(otherReferencedAssembly.Identity))
                    {
                        return otherReferencedAssembly;
                    }
                }

                return null;
            }

            public override Symbol VisitNamespace(NamespaceSymbol @namespace)
            {
                var otherContainer = this.Visit(@namespace.ContainingSymbol);
                Debug.Assert((object)otherContainer != null);

                switch (otherContainer.Kind)
                {
                    case SymbolKind.NetModule:
                        Debug.Assert(@namespace.IsGlobalNamespace);
                        return ((ModuleSymbol)otherContainer).GlobalNamespace;

                    case SymbolKind.Namespace:
                        return FindMatchingNamespaceMember((NamespaceSymbol)otherContainer, @namespace, (s, o) => true);

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

                    var otherDef = (NamedTypeSymbol)this.Visit(originalDef);
                    if ((object)otherDef == null)
                    {
                        return null;
                    }

                    var otherTypeParameters = otherDef.GetAllTypeParameters();
                    bool translationFailed = false;

                    var otherTypeArguments = typeArguments.SelectAsArray((t, v) =>
                                                                            {
                                                                                var newType = (TypeSymbol)v.Visit(t.TypeSymbol);

                                                                                if ((object)newType == null)
                                                                                {
                                                                                    // For a newly added type, there is no match in the previous generation, so it could be null.
                                                                                    translationFailed = true;
                                                                                    newType = t.TypeSymbol;
                                                                                }

                                                                                return t.Update(newType, v.VisitCustomModifiers(t.CustomModifiers));
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

                Debug.Assert(sourceType.IsDefinition);

                var otherContainer = this.Visit(sourceType.ContainingSymbol);
                // Containing type will be missing from other assembly
                // if the type was added in the (newer) source assembly.
                if ((object)otherContainer == null)
                {
                    return null;
                }

                switch (otherContainer.Kind)
                {
                    case SymbolKind.Namespace:
                        if (AnonymousTypeManager.IsAnonymousTypeTemplate(sourceType))
                        {
                            Debug.Assert((object)otherContainer == (object)_otherAssembly.GlobalNamespace);
                            AnonymousTypeValue value;
                            this.TryFindAnonymousType(sourceType, out value);
                            return (NamedTypeSymbol)value.Type;
                        }
                        else if (sourceType.IsAnonymousType)
                        {
                            return this.Visit(AnonymousTypeManager.TranslateAnonymousTypeSymbol(sourceType));
                        }
                        else
                        {
                            return FindMatchingNamespaceMember((NamespaceSymbol)otherContainer, sourceType, AreNamedTypesEqual);
                        }

                    case SymbolKind.NamedType:
                        return FindMatchingNamedTypeMember((NamedTypeSymbol)otherContainer, sourceType, AreNamedTypesEqual);

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
                var otherPointedAtType = (TypeSymbol)this.Visit(symbol.PointedAtType.TypeSymbol);
                if ((object)otherPointedAtType == null)
                {
                    // For a newly added type, there is no match in the previous generation, so it could be null.
                    return null;
                }
                var otherModifiers = VisitCustomModifiers(symbol.PointedAtType.CustomModifiers);
                return new PointerTypeSymbol(symbol.PointedAtType.Update(otherPointedAtType, otherModifiers));
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
                var type = (NamedTypeSymbol)this.Visit((Symbol)modifier.Modifier);
                Debug.Assert((object)type != null);
                return modifier.IsOptional ?
                    CSharpCustomModifier.CreateOptional(type) :
                    CSharpCustomModifier.CreateRequired(type);
            }

            internal bool TryFindAnonymousType(NamedTypeSymbol type, out AnonymousTypeValue otherType)
            {
                Debug.Assert((object)type.ContainingSymbol == (object)_sourceAssembly.GlobalNamespace);
                Debug.Assert(AnonymousTypeManager.IsAnonymousTypeTemplate(type));

                var key = AnonymousTypeManager.GetAnonymousTypeKey(type);
                return _anonymousTypeMap.TryGetValue(key, out otherType);
            }

            private static T FindMatchingNamespaceMember<T>(NamespaceSymbol otherNamespace, T sourceMember, Func<T, T, bool> predicate)
                where T : Symbol
            {
                Debug.Assert(!string.IsNullOrEmpty(sourceMember.Name));

                foreach (var otherMember in otherNamespace.GetMembers(sourceMember.Name))
                {
                    if (sourceMember.Kind != otherMember.Kind)
                    {
                        continue;
                    }

                    var other = (T)otherMember;
                    if (predicate(sourceMember, other))
                    {
                        return other;
                    }
                }

                return null;
            }

            private Symbol VisitNamedTypeMember<T>(T member, Func<T, T, bool> predicate)
                where T : Symbol
            {
                var otherType = (NamedTypeSymbol)this.Visit(member.ContainingType);
                // Containing type may be null for synthesized
                // types such as iterators.
                if ((object)otherType == null)
                {
                    return null;
                }

                return FindMatchingNamedTypeMember(otherType, member, predicate);
            }

            private T FindMatchingNamedTypeMember<T>(NamedTypeSymbol otherType, T sourceMember, Func<T, T, bool> predicate)
                where T : Symbol
            {
                Debug.Assert(!string.IsNullOrEmpty(sourceMember.Name));

                var otherMembersByName = _otherTypeMembers.GetOrAdd(otherType, GetOtherTypeMembers);

                ImmutableArray<Cci.ITypeDefinitionMember> otherMembers;
                if (otherMembersByName.TryGetValue(sourceMember.Name, out otherMembers))
                {
                    foreach (var otherMember in otherMembers)
                    {
                        T other = otherMember as T;
                        if (other != null && predicate(sourceMember, other))
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
                Debug.Assert(type.ElementType.CustomModifiers.IsEmpty);
                Debug.Assert(other.ElementType.CustomModifiers.IsEmpty);

                return type.HasSameShapeAs(other) &&
                    AreTypesEqual(type.ElementType.TypeSymbol, other.ElementType.TypeSymbol);
            }

            private bool AreEventsEqual(EventSymbol @event, EventSymbol other)
            {
                Debug.Assert(s_nameComparer.Equals(@event.Name, other.Name));
                return _comparer.Equals(@event.Type.TypeSymbol, other.Type.TypeSymbol);
            }

            private bool AreFieldsEqual(FieldSymbol field, FieldSymbol other)
            {
                Debug.Assert(s_nameComparer.Equals(field.Name, other.Name));
                return _comparer.Equals(field.Type.TypeSymbol, other.Type.TypeSymbol);
            }

            private bool AreMethodsEqual(MethodSymbol method, MethodSymbol other)
            {
                Debug.Assert(s_nameComparer.Equals(method.Name, other.Name));

                Debug.Assert(method.IsDefinition);
                Debug.Assert(other.IsDefinition);

                method = SubstituteTypeParameters(method);
                other = SubstituteTypeParameters(other);

                return _comparer.Equals(method.ReturnType.TypeSymbol, other.ReturnType.TypeSymbol) &&
                    method.Parameters.SequenceEqual(other.Parameters, AreParametersEqual) &&
                    method.TypeArguments.SequenceEqual(other.TypeArguments, AreTypesEqual);
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
                Debug.Assert(s_nameComparer.Equals(type.Name, other.Name));
                // TODO: Test with overloads (from PE base class?) that have modifiers.
                return type.TypeArgumentsNoUseSiteDiagnostics.SequenceEqual(other.TypeArgumentsNoUseSiteDiagnostics, AreTypesEqual);
            }

            private bool AreParametersEqual(ParameterSymbol parameter, ParameterSymbol other)
            {
                Debug.Assert(parameter.Ordinal == other.Ordinal);
                return s_nameComparer.Equals(parameter.Name, other.Name) &&
                    (parameter.RefKind == other.RefKind) &&
                    _comparer.Equals(parameter.Type.TypeSymbol, other.Type.TypeSymbol);
            }

            private bool ArePointerTypesEqual(PointerTypeSymbol type, PointerTypeSymbol other)
            {
                // TODO: Test with overloads (from PE base class?) that have modifiers.
                Debug.Assert(type.PointedAtType.CustomModifiers.IsEmpty);
                Debug.Assert(other.PointedAtType.CustomModifiers.IsEmpty);

                return AreTypesEqual(type.PointedAtType.TypeSymbol, other.PointedAtType.TypeSymbol);
            }

            private bool ArePropertiesEqual(PropertySymbol property, PropertySymbol other)
            {
                Debug.Assert(s_nameComparer.Equals(property.Name, other.Name));
                return _comparer.Equals(property.Type.TypeSymbol, other.Type.TypeSymbol) &&
                    property.Parameters.SequenceEqual(other.Parameters, AreParametersEqual);
            }

            private bool AreTypeParametersEqual(TypeParameterSymbol type, TypeParameterSymbol other)
            {
                Debug.Assert(type.Ordinal == other.Ordinal);
                Debug.Assert(s_nameComparer.Equals(type.Name, other.Name));
                // Comparing constraints is unnecessary: two methods cannot differ by
                // constraints alone and changing the signature of a method is a rude
                // edit. Furthermore, comparing constraint types might lead to a cycle.
                Debug.Assert(type.HasConstructorConstraint == other.HasConstructorConstraint);
                Debug.Assert(type.HasValueTypeConstraint == other.HasValueTypeConstraint);
                Debug.Assert(type.HasReferenceTypeConstraint == other.HasReferenceTypeConstraint);
                Debug.Assert(type.ConstraintTypesNoUseSiteDiagnostics.Length == other.ConstraintTypesNoUseSiteDiagnostics.Length);
                return true;
            }

            private bool AreTypesEqual(TypeSymbolWithAnnotations type, TypeSymbolWithAnnotations other)
            {
                Debug.Assert(type.CustomModifiers.IsDefaultOrEmpty);
                Debug.Assert(other.CustomModifiers.IsDefaultOrEmpty);
                return AreTypesEqual(type.TypeSymbol, other.TypeSymbol);
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

            private IReadOnlyDictionary<string, ImmutableArray<Cci.ITypeDefinitionMember>> GetOtherTypeMembers(NamedTypeSymbol otherType)
            {
                var members = ArrayBuilder<Cci.ITypeDefinitionMember>.GetInstance();

                members.AddRange(otherType.GetEventsToEmit());
                members.AddRange(otherType.GetFieldsToEmit());
                members.AddRange(otherType.GetMethodsToEmit());
                members.AddRange(otherType.GetTypeMembers());
                members.AddRange(otherType.GetPropertiesToEmit());

                ImmutableArray<Cci.ITypeDefinitionMember> synthesizedMembers;
                if (_otherSynthesizedMembersOpt != null && _otherSynthesizedMembersOpt.TryGetValue(otherType, out synthesizedMembers))
                {
                    members.AddRange(synthesizedMembers);
                }

                var result = members.ToDictionary(s => ((Symbol)s).Name, s_nameComparer);
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

                    return visitedSource?.Equals(visitedOther, ignoreDynamic: true) == true;
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
                var translatedElementType = (TypeSymbol)this.Visit(symbol.ElementType.TypeSymbol);
                var translatedModifiers = VisitCustomModifiers(symbol.ElementType.CustomModifiers);

                if (symbol.IsSZArray)
                {
                    return ArrayTypeSymbol.CreateSZArray(symbol.BaseTypeNoUseSiteDiagnostics.ContainingAssembly, symbol.ElementType.Update(translatedElementType, translatedModifiers));
                }

                return ArrayTypeSymbol.CreateMDArray(symbol.BaseTypeNoUseSiteDiagnostics.ContainingAssembly, symbol.ElementType.Update(translatedElementType, translatedModifiers), symbol.Rank, symbol.Sizes, symbol.LowerBounds);
            }

            public override Symbol VisitDynamicType(DynamicTypeSymbol symbol)
            {
                return _systemObject;
            }

            public override Symbol VisitNamedType(NamedTypeSymbol type)
            {
                var originalDef = type.OriginalDefinition;
                if ((object)originalDef != type)
                {
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    var translatedTypeArguments = type.GetAllTypeArguments(ref useSiteDiagnostics).SelectAsArray((t, v) => t.Update((TypeSymbol)v.Visit(t.TypeSymbol), 
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
                var translatedPointedAtType = (TypeSymbol)this.Visit(symbol.PointedAtType.TypeSymbol);
                var translatedModifiers = VisitCustomModifiers(symbol.PointedAtType.CustomModifiers);
                return new PointerTypeSymbol(symbol.PointedAtType.Update(translatedPointedAtType, translatedModifiers));
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
                var translatedType = (NamedTypeSymbol)this.Visit((Symbol)modifier.Modifier);
                Debug.Assert((object)translatedType != null);
                return modifier.IsOptional ?
                    CSharpCustomModifier.CreateOptional(translatedType) :
                    CSharpCustomModifier.CreateRequired(translatedType);
            }
        }
    }
}
