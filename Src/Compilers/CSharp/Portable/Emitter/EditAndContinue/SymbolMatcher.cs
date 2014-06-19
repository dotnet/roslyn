// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Cci;
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

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal sealed class SymbolMatcher
    {
        private static readonly StringComparer NameComparer = StringComparer.Ordinal;

        private readonly MatchDefs defs;
        private readonly MatchSymbols symbols;

        public SymbolMatcher(
            IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> anonymousTypeMap,
            SourceAssemblySymbol sourceAssembly,
            EmitContext sourceContext,
            SourceAssemblySymbol otherAssembly,
            EmitContext otherContext)
        {
            this.defs = new MatchDefsToSource(sourceContext, otherContext);
            this.symbols = new MatchSymbols(anonymousTypeMap, sourceAssembly, otherAssembly);
        }

        public SymbolMatcher(
            IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> anonymousTypeMap,
            SourceAssemblySymbol sourceAssembly,
            EmitContext sourceContext,
            Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE.PEAssemblySymbol otherAssembly)
        {
            this.defs = new MatchDefsToMetadata(sourceContext, otherAssembly);
            this.symbols = new MatchSymbols(anonymousTypeMap, sourceAssembly, otherAssembly);
        }

        internal IDefinition MapDefinition(IDefinition def)
        {
            var symbol = def as Symbol;
            if ((object)symbol != null)
            {
                return (IDefinition)this.symbols.Visit(symbol);
            }
            return this.defs.VisitDef(def);
        }

        internal ITypeReference MapReference(ITypeReference reference)
        {
            var symbol = reference as Symbol;
            if ((object)symbol != null)
            {
                return (ITypeReference)this.symbols.Visit(symbol);
            }
            return null;
        }

        internal bool TryGetAnonymousTypeName(NamedTypeSymbol template, out string name, out int index)
        {
            return this.symbols.TryGetAnonymousTypeName(template, out name, out index);
        }

        private abstract class MatchDefs
        {
            private readonly EmitContext sourceContext;
            private readonly ConcurrentDictionary<IDefinition, IDefinition> matches;
            private IReadOnlyDictionary<string, INamespaceTypeDefinition> lazyTopLevelTypes;

            public MatchDefs(EmitContext sourceContext)
            {
                this.sourceContext = sourceContext;
                this.matches = new ConcurrentDictionary<IDefinition, IDefinition>();
            }

            public IDefinition VisitDef(IDefinition def)
            {
                return this.matches.GetOrAdd(def, this.VisitDefInternal);
            }

            private IDefinition VisitDefInternal(IDefinition def)
            {
                var type = def as ITypeDefinition;
                if (type != null)
                {
                    var namespaceType = type.AsNamespaceTypeDefinition(this.sourceContext);
                    if (namespaceType != null)
                    {
                        return VisitNamespaceType(namespaceType);
                    }

                    var nestedType = type.AsNestedTypeDefinition(this.sourceContext);
                    Debug.Assert(nestedType != null);

                    var otherContainer = (ITypeDefinition)this.VisitDef(nestedType.ContainingTypeDefinition);
                    if (otherContainer == null)
                    {
                        return null;
                    }

                    return this.VisitTypeMembers(otherContainer, nestedType, GetNestedTypes, (a, b) => NameComparer.Equals(a.Name, b.Name));
                }

                var member = def as ITypeDefinitionMember;
                if (member != null)
                {
                    var otherContainer = (ITypeDefinition)this.VisitDef(member.ContainingTypeDefinition);
                    if (otherContainer == null)
                    {
                        return null;
                    }

                    var field = def as IFieldDefinition;
                    if (field != null)
                    {
                        return this.VisitTypeMembers(otherContainer, field, GetFields, (a, b) => NameComparer.Equals(a.Name, b.Name));
                    }
                }

                // We are only expecting types and fields currently.
                throw ExceptionUtilities.UnexpectedValue(def);
            }

            protected abstract IEnumerable<INamespaceTypeDefinition> GetTopLevelTypes();
            protected abstract IEnumerable<INestedTypeDefinition> GetNestedTypes(ITypeDefinition def);
            protected abstract IEnumerable<IFieldDefinition> GetFields(ITypeDefinition def);

            private INamespaceTypeDefinition VisitNamespaceType(INamespaceTypeDefinition def)
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
                INamespaceTypeDefinition otherDef;
                topLevelTypes.TryGetValue(def.Name, out otherDef);
                return otherDef;
            }

            private IReadOnlyDictionary<string, INamespaceTypeDefinition> GetTopLevelTypesByName()
            {
                if (this.lazyTopLevelTypes == null)
                {
                    var typesByName = new Dictionary<string, INamespaceTypeDefinition>(NameComparer);
                    foreach (var type in this.GetTopLevelTypes())
                    {
                        // All generated top-level types are assumed to be in the global namespace.
                        if (string.IsNullOrEmpty(type.NamespaceName))
                        {
                            typesByName.Add(type.Name, type);
                        }
                    }
                    Interlocked.CompareExchange(ref this.lazyTopLevelTypes, typesByName, null);
                }
                return this.lazyTopLevelTypes;
            }

            private T VisitTypeMembers<T>(
                ITypeDefinition otherContainer,
                T member,
                Func<ITypeDefinition, IEnumerable<T>> getMembers,
                Func<T, T, bool> predicate)
                where T : class, ITypeDefinitionMember
            {
                // We could cache the members by name (see Matcher.VisitNamedTypeMembers)
                // but the assumption is this class is only used for types with few members
                // so caching is not necessary and linear search is acceptable.
                return getMembers(otherContainer).FirstOrDefault(otherMember => predicate(member, otherMember));
            }
        }

        private sealed class MatchDefsToMetadata : MatchDefs
        {
            private readonly Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE.PEAssemblySymbol otherAssembly;

            public MatchDefsToMetadata(EmitContext sourceContext, Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE.PEAssemblySymbol otherAssembly) :
                base(sourceContext)
            {
                this.otherAssembly = otherAssembly;
            }

            protected override IEnumerable<INamespaceTypeDefinition> GetTopLevelTypes()
            {
                var builder = ArrayBuilder<INamespaceTypeDefinition>.GetInstance();
                GetTopLevelTypes(builder, this.otherAssembly.GlobalNamespace);
                return builder.ToArrayAndFree();
            }

            protected override IEnumerable<INestedTypeDefinition> GetNestedTypes(ITypeDefinition def)
            {
                var type = (Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE.PENamedTypeSymbol)def;
                return type.GetTypeMembers().Cast<INestedTypeDefinition>();
            }

            protected override IEnumerable<IFieldDefinition> GetFields(ITypeDefinition def)
            {
                var type = (Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE.PENamedTypeSymbol)def;
                return type.GetFieldsToEmit().Cast<IFieldDefinition>();
            }

            private static void GetTopLevelTypes(ArrayBuilder<INamespaceTypeDefinition> builder, NamespaceSymbol @namespace)
            {
                foreach (var member in @namespace.GetMembers())
                {
                    if (member.Kind == SymbolKind.Namespace)
                    {
                        GetTopLevelTypes(builder, (NamespaceSymbol)member);
                    }
                    else
                    {
                        builder.Add((INamespaceTypeDefinition)member);
                    }
                }
            }
        }

        private sealed class MatchDefsToSource : MatchDefs
        {
            private readonly EmitContext otherContext;

            public MatchDefsToSource(
                EmitContext sourceContext,
                EmitContext otherContext) :
                base(sourceContext)
            {
                this.otherContext = otherContext;
            }

            protected override IEnumerable<INamespaceTypeDefinition> GetTopLevelTypes()
            {
                return this.otherContext.Module.GetTopLevelTypes(this.otherContext);
            }

            protected override IEnumerable<INestedTypeDefinition> GetNestedTypes(ITypeDefinition def)
            {
                return def.GetNestedTypes(this.otherContext);
            }

            protected override IEnumerable<IFieldDefinition> GetFields(ITypeDefinition def)
            {
                return def.GetFields(this.otherContext);
            }
        }

        private sealed class MatchSymbols : CSharpSymbolVisitor<Symbol>
        {
            private readonly IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> anonymousTypeMap;
            private readonly SourceAssemblySymbol sourceAssembly;
            private readonly AssemblySymbol otherAssembly;
            private readonly SymbolComparer comparer;
            private readonly ConcurrentDictionary<Symbol, Symbol> matches;
            // A cache of members per type, populated when the first member for a given
            // type is needed. Within each type, members are indexed by name. The reason
            // for caching, and indexing by name, is to avoid searching sequentially
            // through all members of a given kind each time a member is matched.
            private readonly ConcurrentDictionary<NamedTypeSymbol, IReadOnlyDictionary<string, ImmutableArray<Symbol>>> otherTypeMembers;

            public MatchSymbols(
                IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> anonymousTypeMap,
                SourceAssemblySymbol sourceAssembly,
                AssemblySymbol otherAssembly)
            {
                this.anonymousTypeMap = anonymousTypeMap;
                this.sourceAssembly = sourceAssembly;
                this.otherAssembly = otherAssembly;
                this.comparer = new SymbolComparer(this);
                this.matches = new ConcurrentDictionary<Symbol, Symbol>();
                this.otherTypeMembers = new ConcurrentDictionary<NamedTypeSymbol, IReadOnlyDictionary<string, ImmutableArray<Symbol>>>();
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
                throw new NotImplementedException();
            }

            public override Symbol Visit(Symbol symbol)
            {
                Debug.Assert((object)symbol.ContainingAssembly != (object)this.otherAssembly);

                if ((object)symbol.ContainingAssembly != (object)this.sourceAssembly)
                {
                    // The symbol is not from the source assembly. Unless the symbol
                    // is a constructed symbol, no matching is necessary.
                    switch (symbol.Kind)
                    {
                        case SymbolKind.ArrayType:
                        case SymbolKind.PointerType:
                            break;
                        case SymbolKind.NamedType:
                            if (symbol.IsDefinition)
                            {
                                return symbol;
                            }
                            break;
                        default:
                            Debug.Assert(symbol.IsDefinition);
                            return symbol;
                    }
                }

                // Add an entry for the match, even if there is no match, to avoid
                // matching the same symbol unsuccessfully multiple times.
                var otherSymbol = this.matches.GetOrAdd(symbol, base.Visit);
                return otherSymbol;
            }

            public override Symbol VisitArrayType(ArrayTypeSymbol symbol)
            {
                var otherElementType = (TypeSymbol)this.Visit(symbol.ElementType);
                Debug.Assert((object)otherElementType != null);
                var otherModifiers = VisitCustomModifiers(symbol.CustomModifiers);
                return new ArrayTypeSymbol(this.otherAssembly, otherElementType, otherModifiers, symbol.Rank);
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
                Debug.Assert((object)module.ContainingSymbol == (object)this.sourceAssembly);

                return this.otherAssembly.Modules[module.Ordinal];
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
                        return VisitNamespaceMembers((NamespaceSymbol)otherContainer, @namespace, (s, o) => true);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(otherContainer.Kind);
                }
            }

            public override Symbol VisitNamedType(NamedTypeSymbol type)
            {
                var originalDef = (NamedTypeSymbol)type.OriginalDefinition;
                if ((object)originalDef != (object)type)
                {
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    var typeArguments = type.GetAllTypeArguments(ref useSiteDiagnostics);

                    var otherDef = (NamedTypeSymbol)this.Visit(originalDef);
                    if ((object)otherDef == null)
                    {
                        return null;
                    }

                    var otherTypeParameters = otherDef.GetAllTypeParameters();
                    var otherTypeArguments = typeArguments.SelectAsArray((t, v) => (TypeSymbol)v.Visit(t), this);
                    Debug.Assert(otherTypeArguments.All(t => (object)t != null));

                    var typeMap = new TypeMap(otherTypeParameters, otherTypeArguments);
                    return typeMap.SubstituteNamedType(otherDef);
                }

                Debug.Assert(type.IsDefinition);

                var otherContainer = this.Visit(type.ContainingSymbol);
                // Containing type will be missing from other assembly
                // if the type was added in the (newer) source assembly.
                if ((object)otherContainer == null)
                {
                    return null;
                }

                switch (otherContainer.Kind)
                {
                    case SymbolKind.Namespace:
                        if (AnonymousTypeManager.IsAnonymousTypeTemplate(type))
                        {
                            Debug.Assert((object)otherContainer == (object)this.otherAssembly.GlobalNamespace);
                            AnonymousTypeValue value;
                            this.TryFindAnonymousType(type, out value);
                            return (NamedTypeSymbol)value.Type;
                        }
                        else if (type.IsAnonymousType)
                        {
                            return this.Visit(AnonymousTypeManager.TranslateAnonymousTypeSymbol(type));
                        }
                        else
                        {
                            return VisitNamespaceMembers((NamespaceSymbol)otherContainer, type, AreNamedTypesEqual);
                        }

                    case SymbolKind.NamedType:
                        return VisitNamedTypeMembers((NamedTypeSymbol)otherContainer, type, AreNamedTypesEqual);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(otherContainer.Kind);
                }
            }

            public override Symbol VisitParameter(ParameterSymbol parameter)
            {
                // Should never reach here. Should be matched as a result of matching the container.
                throw new InvalidOperationException();
            }

            public override Symbol VisitPointerType(PointerTypeSymbol symbol)
            {
                var otherPointedAtType = (TypeSymbol)this.Visit(symbol.PointedAtType);
                Debug.Assert((object)otherPointedAtType != null);
                var otherModifiers = VisitCustomModifiers(symbol.CustomModifiers);
                return new PointerTypeSymbol(otherPointedAtType, otherModifiers);
            }

            public override Symbol VisitProperty(PropertySymbol symbol)
            {
                return this.VisitNamedTypeMember(symbol, ArePropertiesEqual);
            }

            public override Symbol VisitTypeParameter(TypeParameterSymbol symbol)
            {
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
                Debug.Assert((object)type.ContainingSymbol == (object)this.sourceAssembly.GlobalNamespace);
                Debug.Assert(AnonymousTypeManager.IsAnonymousTypeTemplate(type));

                var key = new AnonymousTypeKey(AnonymousTypeManager.GetTemplatePropertyNames(type));
                return this.anonymousTypeMap.TryGetValue(key, out otherType);
            }

            private static T VisitNamespaceMembers<T>(NamespaceSymbol otherNamespace, T member, Func<T, T, bool> predicate)
                where T : Symbol
            {
                Debug.Assert(!string.IsNullOrEmpty(member.Name));

                foreach (var otherMember in otherNamespace.GetMembers(member.Name))
                {
                    if (member.Kind != otherMember.Kind)
                    {
                        continue;
                    }

                    var other = (T)otherMember;
                    if (predicate(member, other))
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
                return VisitNamedTypeMembers(otherType, member, predicate);
            }

            private T VisitNamedTypeMembers<T>(NamedTypeSymbol otherType, T member, Func<T, T, bool> predicate)
                where T : Symbol
            {
                Debug.Assert(!string.IsNullOrEmpty(member.Name));

                var otherMembersByName = this.otherTypeMembers.GetOrAdd(otherType, GetTypeMembers);

                ImmutableArray<Symbol> otherMembers;
                if (otherMembersByName.TryGetValue(member.Name, out otherMembers))
                {
                    foreach (var otherMember in otherMembers)
                    {
                        if (member.Kind != otherMember.Kind)
                        {
                            continue;
                        }

                        var other = (T)otherMember;
                        if (predicate(member, other))
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
                Debug.Assert(type.CustomModifiers.IsEmpty);
                Debug.Assert(other.CustomModifiers.IsEmpty);

                return (type.Rank == other.Rank) &&
                    AreTypesEqual(type.ElementType, other.ElementType);
            }

            private bool AreEventsEqual(EventSymbol @event, EventSymbol other)
            {
                Debug.Assert(NameComparer.Equals(@event.Name, other.Name));
                return this.comparer.Equals(@event.Type, other.Type);
            }

            private bool AreFieldsEqual(FieldSymbol field, FieldSymbol other)
            {
                Debug.Assert(NameComparer.Equals(field.Name, other.Name));
                return this.comparer.Equals(field.Type, other.Type);
            }

            private bool AreMethodsEqual(MethodSymbol method, MethodSymbol other)
            {
                Debug.Assert(NameComparer.Equals(method.Name, other.Name));

                Debug.Assert(method.IsDefinition);
                Debug.Assert(other.IsDefinition);

                method = SubstituteTypeParameters(method);
                other = SubstituteTypeParameters(other);

                return this.comparer.Equals(method.ReturnType, other.ReturnType) &&
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
                Debug.Assert(NameComparer.Equals(type.Name, other.Name));
                return type.TypeArgumentsNoUseSiteDiagnostics.SequenceEqual(other.TypeArgumentsNoUseSiteDiagnostics, AreTypesEqual);
            }

            private bool AreParametersEqual(ParameterSymbol parameter, ParameterSymbol other)
            {
                Debug.Assert(parameter.Ordinal == other.Ordinal);
                return NameComparer.Equals(parameter.Name, other.Name) &&
                    (parameter.RefKind == other.RefKind) &&
                    this.comparer.Equals(parameter.Type, other.Type);
            }

            private bool ArePointerTypesEqual(PointerTypeSymbol type, PointerTypeSymbol other)
            {
                // TODO: Test with overloads (from PE base class?) that have modifiers.
                Debug.Assert(type.CustomModifiers.IsEmpty);
                Debug.Assert(other.CustomModifiers.IsEmpty);

                return AreTypesEqual(type.PointedAtType, other.PointedAtType);
            }

            private bool ArePropertiesEqual(PropertySymbol property, PropertySymbol other)
            {
                Debug.Assert(NameComparer.Equals(property.Name, other.Name));
                return this.comparer.Equals(property.Type, other.Type) &&
                    property.Parameters.SequenceEqual(other.Parameters, AreParametersEqual);
            }

            private bool AreTypeParametersEqual(TypeParameterSymbol type, TypeParameterSymbol other)
            {
                Debug.Assert(type.Ordinal == other.Ordinal);
                Debug.Assert(NameComparer.Equals(type.Name, other.Name));
                // Comparing constraints is unnecessary: two methods cannot differ by
                // constraints alone and changing the signature of a method is a rude
                // edit. Furthermore, comparing constraint types might lead to a cycle.
                Debug.Assert(type.HasConstructorConstraint == other.HasConstructorConstraint);
                Debug.Assert(type.HasValueTypeConstraint == other.HasValueTypeConstraint);
                Debug.Assert(type.HasReferenceTypeConstraint == other.HasReferenceTypeConstraint);
                Debug.Assert(type.ConstraintTypesNoUseSiteDiagnostics.Length == other.ConstraintTypesNoUseSiteDiagnostics.Length);
                return true;
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

            private static IReadOnlyDictionary<string, ImmutableArray<Symbol>> GetTypeMembers(NamedTypeSymbol type)
            {
                var members = ArrayBuilder<Symbol>.GetInstance();
                members.AddRange(type.GetEventsToEmit());
                members.AddRange(type.GetFieldsToEmit());
                members.AddRange(type.GetMethodsToEmit());
                members.AddRange(type.GetTypeMembers());
                members.AddRange(type.GetPropertiesToEmit());
                var result = members.ToDictionary(s => s.Name, NameComparer);
                members.Free();
                return result;
            }

            private sealed class SymbolComparer : IEqualityComparer<Symbol>
            {
                private readonly MatchSymbols matcher;

                public SymbolComparer(MatchSymbols matcher)
                {
                    this.matcher = matcher;
                }

                public bool Equals(Symbol x, Symbol y)
                {
                    var other = this.matcher.Visit(x);
                    Debug.Assert((object)other != null);
                    return other == y;
                }

                public int GetHashCode(Symbol obj)
                {
                    return obj.GetHashCode();
                }
            }
        }
    }
}
