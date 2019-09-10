// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

#pragma warning disable CS0660

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A TypeSymbol is a base class for all the symbols that represent a type
    /// in C#.
    /// </summary>
    internal abstract partial class TypeSymbol : NamespaceOrTypeSymbol, ITypeSymbol
    {
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // Changes to the public interface of this class should remain synchronized with the VB version.
        // Do not make any changes to the public interface without making the corresponding change
        // to the VB version.
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        // TODO (tomat): Consider changing this to an empty name. This name shouldn't ever leak to the user in error messages.
        internal const string ImplicitTypeName = "<invalid-global-code>";

        // InterfaceInfo for a common case of a type not implementing anything directly or indirectly.
        private static readonly InterfaceInfo s_noInterfaces = new InterfaceInfo();

        private ImmutableHashSet<Symbol> _lazyAbstractMembers;
        private InterfaceInfo _lazyInterfaceInfo;

        private class InterfaceInfo
        {
            // all directly implemented interfaces, their bases and all interfaces to the bases of the type recursively
            internal ImmutableArray<NamedTypeSymbol> allInterfaces;

            /// <summary>
            /// <see cref="TypeSymbol.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics"/>
            /// </summary>
            internal MultiDictionary<NamedTypeSymbol, NamedTypeSymbol> interfacesAndTheirBaseInterfaces;

            internal readonly static MultiDictionary<NamedTypeSymbol, NamedTypeSymbol> EmptyInterfacesAndTheirBaseInterfaces =
                                                new MultiDictionary<NamedTypeSymbol, NamedTypeSymbol>(0, EqualsCLRSignatureComparer);

            // Key is implemented member (method, property, or event), value is implementing member (from the 
            // perspective of this type).  Don't allocate until someone needs it.
            private ConcurrentDictionary<Symbol, SymbolAndDiagnostics> _implementationForInterfaceMemberMap;

            public ConcurrentDictionary<Symbol, SymbolAndDiagnostics> ImplementationForInterfaceMemberMap
            {
                get
                {
                    var map = _implementationForInterfaceMemberMap;
                    if (map != null)
                    {
                        return map;
                    }

                    // PERF: Avoid over-allocation. In many cases, there's only 1 entry and we don't expect concurrent updates.
                    map = new ConcurrentDictionary<Symbol, SymbolAndDiagnostics>(concurrencyLevel: 1, capacity: 1, comparer: SymbolEqualityComparer.ConsiderEverything);
                    return Interlocked.CompareExchange(ref _implementationForInterfaceMemberMap, map, null) ?? map;
                }
            }

            /// <summary>
            /// key = interface method/property/event compared using <see cref="ExplicitInterfaceImplementationTargetMemberEqualityComparer"/>,
            /// value = explicitly implementing methods/properties/events declared on this type (normally a single value, multiple in case of
            /// an error).
            /// </summary>
            internal MultiDictionary<Symbol, Symbol> explicitInterfaceImplementationMap;

            internal bool IsDefaultValue()
            {
                return allInterfaces.IsDefault &&
                    interfacesAndTheirBaseInterfaces == null &&
                    _implementationForInterfaceMemberMap == null &&
                    explicitInterfaceImplementationMap == null;
            }
        }

        private InterfaceInfo GetInterfaceInfo()
        {
            var info = _lazyInterfaceInfo;
            if (info != null)
            {
                Debug.Assert(info != s_noInterfaces || info.IsDefaultValue(), "default value was modified");
                return info;
            }

            for (var baseType = this; !ReferenceEquals(baseType, null); baseType = baseType.BaseTypeNoUseSiteDiagnostics)
            {
                var interfaces = (baseType.TypeKind == TypeKind.TypeParameter) ? ((TypeParameterSymbol)baseType).EffectiveInterfacesNoUseSiteDiagnostics : baseType.InterfacesNoUseSiteDiagnostics();
                if (!interfaces.IsEmpty)
                {
                    // it looks like we or one of our bases implements something.
                    info = new InterfaceInfo();

                    // NOTE: we are assigning lazyInterfaceInfo via interlocked not for correctness, 
                    // we just do not want to override an existing info that could be partially filled.
                    return Interlocked.CompareExchange(ref _lazyInterfaceInfo, info, null) ?? info;
                }
            }

            // if we have got here it means neither we nor our bases implement anything
            _lazyInterfaceInfo = info = s_noInterfaces;
            return info;
        }

        internal static readonly EqualityComparer<TypeSymbol> EqualsConsiderEverything = new TypeSymbolComparer(TypeCompareKind.ConsiderEverything);

        internal static readonly EqualityComparer<TypeSymbol> EqualsIgnoringTupleNamesAndNullability = new TypeSymbolComparer(TypeCompareKind.IgnoreTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes);

        /// <summary>
        /// A comparer that treats dynamic and object as "the same" types, and also ignores tuple element names differences.
        /// </summary>
        internal static readonly EqualityComparer<TypeSymbol> EqualsIgnoringDynamicTupleNamesAndNullabilityComparer = new TypeSymbolComparer(TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes);

        internal static readonly EqualityComparer<TypeSymbol> EqualsIgnoringNullableComparer = new TypeSymbolComparer(TypeCompareKind.IgnoreNullableModifiersForReferenceTypes);

        internal static readonly EqualityComparer<TypeSymbol> EqualsObliviousNullableModifierMatchesAny = new TypeSymbolComparer(TypeCompareKind.ObliviousNullableModifierMatchesAny);

        internal static readonly EqualityComparer<TypeSymbol> EqualsAllIgnoreOptionsPlusNullableWithUnknownMatchesAnyComparer =
                                                                  new TypeSymbolComparer(TypeCompareKind.AllIgnoreOptions & ~(TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));

        internal static readonly EqualityComparer<TypeSymbol> EqualsCLRSignatureComparer = new TypeSymbolComparer(TypeCompareKind.CLRSignatureCompareOptions);
        /// <summary>
        /// The original definition of this symbol. If this symbol is constructed from another
        /// symbol by type substitution then OriginalDefinition gets the original symbol as it was defined in
        /// source or metadata.
        /// </summary>
        public new TypeSymbol OriginalDefinition
        {
            get
            {
                return OriginalTypeSymbolDefinition;
            }
        }

        protected virtual TypeSymbol OriginalTypeSymbolDefinition
        {
            get
            {
                return this;
            }
        }

        protected override sealed Symbol OriginalSymbolDefinition
        {
            get
            {
                return this.OriginalTypeSymbolDefinition;
            }
        }

        /// <summary>
        /// Gets the BaseType of this type. If the base type could not be determined, then 
        /// an instance of ErrorType is returned. If this kind of type does not have a base type
        /// (for example, interfaces), null is returned. Also the special class System.Object
        /// always has a BaseType of null.
        /// </summary>
        internal abstract NamedTypeSymbol BaseTypeNoUseSiteDiagnostics { get; }

        internal NamedTypeSymbol BaseTypeWithDefinitionUseSiteDiagnostics(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var result = BaseTypeNoUseSiteDiagnostics;

            if ((object)result != null)
            {
                result.OriginalDefinition.AddUseSiteDiagnostics(ref useSiteDiagnostics);
            }

            return result;
        }

        internal NamedTypeSymbol BaseTypeOriginalDefinition(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var result = BaseTypeNoUseSiteDiagnostics;

            if ((object)result != null)
            {
                result = result.OriginalDefinition;
                result.AddUseSiteDiagnostics(ref useSiteDiagnostics);
            }

            return result;
        }

        /// <summary>
        /// Gets the set of interfaces that this type directly implements. This set does not include
        /// interfaces that are base interfaces of directly implemented interfaces.
        /// </summary>
        internal abstract ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved = null);

        /// <summary>
        /// The list of all interfaces of which this type is a declared subtype, excluding this type
        /// itself. This includes all declared base interfaces, all declared base interfaces of base
        /// types, and all declared base interfaces of those results (recursively).  Each result
        /// appears exactly once in the list. This list is topologically sorted by the inheritance
        /// relationship: if interface type A extends interface type B, then A precedes B in the
        /// list. This is not quite the same as "all interfaces of which this type is a proper
        /// subtype" because it does not take into account variance: AllInterfaces for
        /// IEnumerable&lt;string&gt; will not include IEnumerable&lt;object&gt;
        /// </summary>
        internal ImmutableArray<NamedTypeSymbol> AllInterfacesNoUseSiteDiagnostics
        {
            get
            {
                return GetAllInterfaces();
            }
        }

        internal ImmutableArray<NamedTypeSymbol> AllInterfacesWithDefinitionUseSiteDiagnostics(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var result = AllInterfacesNoUseSiteDiagnostics;

            // Since bases affect content of AllInterfaces set, we need to make sure they all are good.
            var current = this;

            do
            {
                current = current.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics);
            }
            while ((object)current != null);

            foreach (var iface in result)
            {
                iface.OriginalDefinition.AddUseSiteDiagnostics(ref useSiteDiagnostics);
            }

            return result;
        }

        /// <summary>
        /// If this is a type parameter returns its effective base class, otherwise returns this type.
        /// </summary>
        internal TypeSymbol EffectiveTypeNoUseSiteDiagnostics
        {
            get
            {
                return this.IsTypeParameter() ? ((TypeParameterSymbol)this).EffectiveBaseClassNoUseSiteDiagnostics : this;
            }
        }

        internal TypeSymbol EffectiveType(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            return this.IsTypeParameter() ? ((TypeParameterSymbol)this).EffectiveBaseClass(ref useSiteDiagnostics) : this;
        }

        /// <summary>
        /// Returns true if this type derives from a given type.
        /// </summary>
        internal bool IsDerivedFrom(TypeSymbol type, TypeCompareKind comparison, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert((object)type != null);
            Debug.Assert(!type.IsTypeParameter());

            if ((object)this == (object)type)
            {
                return false;
            }

            var t = this.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics);
            while ((object)t != null)
            {
                if (type.Equals(t, comparison))
                {
                    return true;
                }

                t = t.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics);
            }

            return false;
        }

        /// <summary>
        /// Returns true if this type is equal or derives from a given type.
        /// </summary>
        internal bool IsEqualToOrDerivedFrom(TypeSymbol type, TypeCompareKind comparison, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            return this.Equals(type, comparison) || this.IsDerivedFrom(type, comparison, ref useSiteDiagnostics);
        }

        /// <summary>
        /// Determines if this type symbol represent the same type as another, according to the language
        /// semantics.
        /// </summary>
        /// <param name="t2">The other type.</param>
        /// <param name="compareKind">
        /// What kind of comparison to use? 
        /// You can ignore custom modifiers, ignore the distinction between object and dynamic, or ignore tuple element names differences.
        /// </param>
        /// <param name="isValueTypeOverrideOpt">
        /// A map from a type parameter symbol to a boolean value that should be used as a replacement for a value returned by
        /// <see cref="TypeParameterSymbol.IsValueType"/> property. Used when accessing the property for a type parameter symbol
        /// that has an entry in the map is not safe and can cause a cycle.  
        /// </param>
        /// <returns>True if the types are equivalent.</returns>
        internal virtual bool Equals(TypeSymbol t2, TypeCompareKind compareKind, IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverrideOpt = null)
        {
            return ReferenceEquals(this, t2);
        }

        public sealed override bool Equals(Symbol other, TypeCompareKind compareKind)
        {
            var t2 = other as TypeSymbol;
            if (t2 is null)
            {
                return false;
            }
            return this.Equals(t2, compareKind);
        }

        /// <summary>
        /// We ignore custom modifiers, and the distinction between dynamic and object, when computing a type's hash code.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        internal sealed class TypeSymbolComparer : EqualityComparer<TypeSymbol>
        {
            private readonly TypeCompareKind _comparison;

            public TypeSymbolComparer(TypeCompareKind comparison)
            {
                _comparison = comparison;
            }

            public override int GetHashCode(TypeSymbol obj)
            {
                return (object)obj == null ? 0 : obj.GetHashCode();
            }

            public override bool Equals(TypeSymbol x, TypeSymbol y)
            {
                return
                    (object)x == null ? (object)y == null :
                    x.Equals(y, _comparison);
            }
        }

        protected virtual ImmutableArray<NamedTypeSymbol> GetAllInterfaces()
        {
            var info = this.GetInterfaceInfo();
            if (info == s_noInterfaces)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            if (info.allInterfaces.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(ref info.allInterfaces, MakeAllInterfaces());
            }

            return info.allInterfaces;
        }

        /// Produce all implemented interfaces in topologically sorted order. We use
        /// TypeSymbol.Interfaces as the source of edge data, which has had cycles and infinitely
        /// long dependency cycles removed. Consequently, it is possible (and we do) use the
        /// simplest version of Tarjan's topological sorting algorithm.
        protected virtual ImmutableArray<NamedTypeSymbol> MakeAllInterfaces()
        {
            var result = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            var visited = new HashSet<NamedTypeSymbol>(SymbolEqualityComparer.ConsiderEverything);

            for (var baseType = this; !ReferenceEquals(baseType, null); baseType = baseType.BaseTypeNoUseSiteDiagnostics)
            {
                var interfaces = (baseType.TypeKind == TypeKind.TypeParameter) ? ((TypeParameterSymbol)baseType).EffectiveInterfacesNoUseSiteDiagnostics : baseType.InterfacesNoUseSiteDiagnostics();
                for (int i = interfaces.Length - 1; i >= 0; i--)
                {
                    AddAllInterfaces(interfaces[i], visited, result);
                }
            }

            result.ReverseContents();
            return result.ToImmutableAndFree();
        }

        private static void AddAllInterfaces(NamedTypeSymbol @interface, HashSet<NamedTypeSymbol> visited, ArrayBuilder<NamedTypeSymbol> result)
        {
            if (visited.Add(@interface))
            {
                ImmutableArray<NamedTypeSymbol> baseInterfaces = @interface.InterfacesNoUseSiteDiagnostics();
                for (int i = baseInterfaces.Length - 1; i >= 0; i--)
                {
                    var baseInterface = baseInterfaces[i];
                    AddAllInterfaces(baseInterface, visited, result);
                }

                result.Add(@interface);
            }
        }

        /// <summary>
        /// Gets the set of interfaces that this type directly implements, plus the base interfaces
        /// of all such types. Keys are compared using <see cref="EqualsCLRSignatureComparer"/>,
        /// values are distinct interfaces corresponding to the key, according to <see cref="TypeCompareKind.ConsiderEverything"/> rules.
        /// </summary>
        /// <remarks>
        /// CONSIDER: it probably isn't truly necessary to cache this.  If space gets tight, consider
        /// alternative approaches (recompute every time, cache on the side, only store on some types,
        /// etc).
        /// </remarks>
        internal MultiDictionary<NamedTypeSymbol, NamedTypeSymbol> InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics
        {
            get
            {
                var info = this.GetInterfaceInfo();
                if (info == s_noInterfaces)
                {
                    Debug.Assert(InterfaceInfo.EmptyInterfacesAndTheirBaseInterfaces.IsEmpty);
                    return InterfaceInfo.EmptyInterfacesAndTheirBaseInterfaces;
                }

                if (info.interfacesAndTheirBaseInterfaces == null)
                {
                    Interlocked.CompareExchange(ref info.interfacesAndTheirBaseInterfaces, MakeInterfacesAndTheirBaseInterfaces(this.InterfacesNoUseSiteDiagnostics()), null);
                }

                return info.interfacesAndTheirBaseInterfaces;
            }
        }

        internal MultiDictionary<NamedTypeSymbol, NamedTypeSymbol> InterfacesAndTheirBaseInterfacesWithDefinitionUseSiteDiagnostics(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var result = InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics;

            foreach (var iface in result.Keys)
            {
                iface.OriginalDefinition.AddUseSiteDiagnostics(ref useSiteDiagnostics);
            }

            return result;
        }

        // Note: Unlike MakeAllInterfaces, this doesn't need to be virtual. It depends on
        // AllInterfaces for its implementation, so it will pick up all changes to MakeAllInterfaces
        // indirectly.
        private static MultiDictionary<NamedTypeSymbol, NamedTypeSymbol> MakeInterfacesAndTheirBaseInterfaces(ImmutableArray<NamedTypeSymbol> declaredInterfaces)
        {
            var resultBuilder = new MultiDictionary<NamedTypeSymbol, NamedTypeSymbol>(declaredInterfaces.Length, EqualsCLRSignatureComparer, EqualsConsiderEverything);
            foreach (var @interface in declaredInterfaces)
            {
                if (resultBuilder.Add(@interface, @interface))
                {
                    foreach (var baseInterface in @interface.AllInterfacesNoUseSiteDiagnostics)
                    {
                        resultBuilder.Add(baseInterface, baseInterface);
                    }
                }
            }

            return resultBuilder;
        }

        /// <summary>
        /// Returns the corresponding symbol in this type or a base type that implements 
        /// interfaceMember (either implicitly or explicitly), or null if no such symbol exists
        /// (which might be either because this type doesn't implement the container of
        /// interfaceMember, or this type doesn't supply a member that successfully implements
        /// interfaceMember).
        /// </summary>
        /// <param name="interfaceMember">
        /// Must be a non-null interface property, method, or event.
        /// </param>
        public Symbol FindImplementationForInterfaceMember(Symbol interfaceMember, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if ((object)interfaceMember == null)
            {
                throw new ArgumentNullException(nameof(interfaceMember));
            }

            if (!interfaceMember.IsImplementableInterfaceMember())
            {
                return null;
            }

            if (this.IsInterfaceType())
            {
                return FindMostSpecificImplementation(interfaceMember, (NamedTypeSymbol)this, ref useSiteDiagnostics);
            }

            return FindImplementationForInterfaceMemberInNonInterfaceWithDiagnostics(interfaceMember).Symbol;
        }

        /// <summary>
        /// Returns true if this type is known to be a reference type. It is never the case that
        /// IsReferenceType and IsValueType both return true. However, for an unconstrained type
        /// parameter, IsReferenceType and IsValueType will both return false.
        /// </summary>
        public abstract bool IsReferenceType { get; }

        /// <summary>
        /// Returns true if this type is known to be a value type. It is never the case that
        /// IsReferenceType and IsValueType both return true. However, for an unconstrained type
        /// parameter, IsReferenceType and IsValueType will both return false.
        /// </summary>
        public abstract bool IsValueType { get; }

        // Only the compiler can create TypeSymbols.
        internal TypeSymbol()
        {
        }

        /// <summary>
        /// Gets the kind of this type.
        /// </summary>
        public abstract TypeKind TypeKind { get; }

        /// <summary>
        /// Gets corresponding special TypeId of this type.
        /// </summary>
        /// <remarks>
        /// Not preserved in types constructed from this one.
        /// </remarks>
        public virtual SpecialType SpecialType
        {
            get
            {
                return SpecialType.None;
            }
        }

        /// <summary>
        /// Gets corresponding primitive type code for this type declaration.
        /// </summary>
        internal Microsoft.Cci.PrimitiveTypeCode PrimitiveTypeCode
        {
            get
            {
                return this.IsPointerType()
                    ? Microsoft.Cci.PrimitiveTypeCode.Pointer
                    : SpecialTypes.GetTypeCode(SpecialType);
            }
        }

        #region Use-Site Diagnostics

        /// <summary>
        /// Return error code that has highest priority while calculating use site error for this symbol. 
        /// </summary>
        protected override int HighestPriorityUseSiteError
        {
            get
            {
                return (int)ErrorCode.ERR_BogusType;
            }
        }


        public sealed override bool HasUnsupportedMetadata
        {
            get
            {
                DiagnosticInfo info = GetUseSiteDiagnostic();
                return (object)info != null && info.Code == (int)ErrorCode.ERR_BogusType;
            }
        }

        internal abstract bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes);

        #endregion

        /// <summary>
        /// Is this a symbol for an anonymous type (including delegate).
        /// </summary>
        public virtual bool IsAnonymousType
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Is this a symbol for a Tuple.
        /// </summary>
        public virtual bool IsTupleType => false;

        /// <summary>
        /// Verify if the given type can be used to back a tuple type 
        /// and return cardinality of that tuple type in <paramref name="tupleCardinality"/>. 
        /// </summary>
        /// <param name="tupleCardinality">If method returns true, contains cardinality of the compatible tuple type.</param>
        /// <returns></returns>
        public virtual bool IsTupleCompatible(out int tupleCardinality)
        {
            tupleCardinality = 0;
            return false;
        }

        /// <summary>
        /// Verify if the given type can be used to back a tuple type. 
        /// </summary>
        public bool IsTupleCompatible()
        {
            int countOfItems;
            return IsTupleCompatible(out countOfItems);
        }

        /// <summary>
        /// Verify if the given type is a tuple of a given cardinality, or can be used to back a tuple type 
        /// with the given cardinality. 
        /// </summary>
        public bool IsTupleOrCompatibleWithTupleOfCardinality(int targetCardinality)
        {
            if (IsTupleType)
            {
                return TupleElementTypesWithAnnotations.Length == targetCardinality;
            }

            int countOfItems;
            return IsTupleCompatible(out countOfItems) && countOfItems == targetCardinality;
        }

        /// <summary>
        /// If this is a tuple type symbol, returns the symbol for its underlying type.
        /// Otherwise, returns null.
        /// The type argument corresponding to the type of the extension field (VT[8].Rest),
        /// which is at the 8th (one based) position is always a symbol for another tuple, 
        /// rather than its underlying type.
        /// </summary>
        public virtual NamedTypeSymbol TupleUnderlyingType
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// If this symbol represents a tuple type, get the types of the tuple's elements.
        /// </summary>
        public virtual ImmutableArray<TypeWithAnnotations> TupleElementTypesWithAnnotations => default(ImmutableArray<TypeWithAnnotations>);

        /// <summary>
        /// If this symbol represents a tuple type, get the names of the tuple's elements.
        /// </summary>
        public virtual ImmutableArray<string> TupleElementNames => default(ImmutableArray<string>);

        /// <summary>
        /// If this symbol represents a tuple type, get the fields for the tuple's elements.
        /// Otherwise, returns default.
        /// </summary>
        public virtual ImmutableArray<FieldSymbol> TupleElements => default(ImmutableArray<FieldSymbol>);

        /// <summary>
        /// Is this type a managed type (false for everything but enum, pointer, and
        /// some struct types).
        /// </summary>
        /// <remarks>
        /// See Type::computeManagedType.
        /// </remarks>
        internal bool IsManagedType => ManagedKind == ManagedKind.Managed;

        /// <summary>
        /// Indicates whether a type is managed or not (i.e. you can take a pointer to it).
        /// Contains additional cases to help implement FeatureNotAvailable diagnostics.
        /// </summary>
        internal abstract ManagedKind ManagedKind { get; }

        bool ITypeSymbol.IsUnmanagedType => !IsManagedType;

        internal bool NeedsNullableAttribute()
        {
            return TypeWithAnnotations.NeedsNullableAttribute(typeWithAnnotationsOpt: default, typeOpt: this);
        }

        internal abstract void AddNullableTransforms(ArrayBuilder<byte> transforms);

        internal abstract bool ApplyNullableTransforms(byte defaultTransformFlag, ImmutableArray<byte> transforms, ref int position, out TypeSymbol result);

        internal abstract TypeSymbol SetNullabilityForReferenceTypes(Func<TypeWithAnnotations, TypeWithAnnotations> transform);

        internal TypeSymbol SetUnknownNullabilityForReferenceTypes()
        {
            return SetNullabilityForReferenceTypes(s_setUnknownNullability);
        }

        private readonly static Func<TypeWithAnnotations, TypeWithAnnotations> s_setUnknownNullability =
            (type) => type.SetUnknownNullabilityForReferenceTypes();

        /// <summary>
        /// Merges nested nullability from an otherwise identical type.
        /// </summary>
        internal abstract TypeSymbol MergeNullability(TypeSymbol other, VarianceKind variance);

        /// <summary>
        /// Returns true if the type may contain embedded references
        /// </summary>
        public abstract bool IsRefLikeType { get; }

        /// <summary>
        /// Returns true if the type is a readonly struct
        /// </summary>
        public abstract bool IsReadOnly { get; }

        #region ITypeSymbol Members

        INamedTypeSymbol ITypeSymbol.BaseType
        {
            get
            {
                return this.BaseTypeNoUseSiteDiagnostics;
            }
        }

        ImmutableArray<INamedTypeSymbol> ITypeSymbol.Interfaces
        {
            get
            {
                return StaticCast<INamedTypeSymbol>.From(this.InterfacesNoUseSiteDiagnostics());
            }
        }

        ImmutableArray<INamedTypeSymbol> ITypeSymbol.AllInterfaces
        {
            get
            {
                return StaticCast<INamedTypeSymbol>.From(this.AllInterfacesNoUseSiteDiagnostics);
            }
        }

        bool ITypeSymbol.IsReferenceType
        {
            get
            {
                return this.IsReferenceType;
            }
        }

        bool ITypeSymbol.IsValueType
        {
            get
            {
                return this.IsValueType;
            }
        }

        ITypeSymbol ITypeSymbol.OriginalDefinition
        {
            get
            {
                return this.OriginalDefinition;
            }
        }

        TypeKind ITypeSymbol.TypeKind
        {
            get
            {
                return TypeKind;
            }
        }

        ISymbol ITypeSymbol.FindImplementationForInterfaceMember(ISymbol interfaceMember)
        {
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            return interfaceMember is Symbol
                ? FindImplementationForInterfaceMember((Symbol)interfaceMember, ref useSiteDiagnostics)
                : null;
        }

        /// <summary>
        /// Is this a symbol for a Tuple.
        /// </summary>
        bool ITypeSymbol.IsTupleType => this.IsTupleType;

        public string ToDisplayString(CodeAnalysis.NullableFlowState topLevelNullability, SymbolDisplayFormat format = null)
        {
            return SymbolDisplay.ToDisplayString(this, topLevelNullability, format);
        }

        public ImmutableArray<SymbolDisplayPart> ToDisplayParts(CodeAnalysis.NullableFlowState topLevelNullability, SymbolDisplayFormat format = null)
        {
            return SymbolDisplay.ToDisplayParts(this, topLevelNullability, format);
        }

        public string ToMinimalDisplayString(
            SemanticModel semanticModel,
            CodeAnalysis.NullableFlowState topLevelNullability,
            int position,
            SymbolDisplayFormat format = null)
        {
            return SymbolDisplay.ToMinimalDisplayString(this, topLevelNullability, semanticModel, position, format);
        }

        public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(
            SemanticModel semanticModel,
            CodeAnalysis.NullableFlowState topLevelNullability,
            int position,
            SymbolDisplayFormat format = null)
        {
            return SymbolDisplay.ToMinimalDisplayParts(this, topLevelNullability, semanticModel, position, format);
        }

        #endregion

        #region Interface member checks

        protected SymbolAndDiagnostics FindImplementationForInterfaceMemberInNonInterfaceWithDiagnostics(Symbol interfaceMember)
        {
            Debug.Assert((object)interfaceMember != null);
            Debug.Assert(!this.IsInterfaceType());

            if (this.IsInterfaceType())
            {
                return SymbolAndDiagnostics.Empty;
            }

            var interfaceType = interfaceMember.ContainingType;
            if ((object)interfaceType == null || !interfaceType.IsInterface)
            {
                return SymbolAndDiagnostics.Empty;
            }

            switch (interfaceMember.Kind)
            {
                case SymbolKind.Method:
                case SymbolKind.Property:
                case SymbolKind.Event:
                    var info = this.GetInterfaceInfo();
                    if (info == s_noInterfaces)
                    {
                        return SymbolAndDiagnostics.Empty;
                    }

                    // PERF: Avoid delegate allocation by splitting GetOrAdd into TryGetValue+TryAdd
                    var map = info.ImplementationForInterfaceMemberMap;
                    SymbolAndDiagnostics result;
                    if (map.TryGetValue(interfaceMember, out result))
                    {
                        return result;
                    }

                    result = ComputeImplementationAndDiagnosticsForInterfaceMember(interfaceMember);
                    map.TryAdd(interfaceMember, result);
                    return result;

                default:
                    return SymbolAndDiagnostics.Empty;
            }
        }

        private SymbolAndDiagnostics ComputeImplementationAndDiagnosticsForInterfaceMember(Symbol interfaceMember)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            var implementingMember = ComputeImplementationForInterfaceMember(interfaceMember, this, diagnostics);
            var implementingMemberAndDiagnostics = new SymbolAndDiagnostics(implementingMember, diagnostics.ToReadOnlyAndFree());
            return implementingMemberAndDiagnostics;
        }

        /// <summary>
        /// Performs interface mapping (spec 13.4.4).
        /// </summary>
        /// <remarks>
        /// CONSIDER: we could probably do less work in the metadata and retargeting cases - we won't use the diagnostics.
        /// </remarks>
        /// <param name="interfaceMember">A non-null implementable member on an interface type.</param>
        /// <param name="implementingType">The type implementing the interface property (usually "this").</param>
        /// <param name="diagnostics">Bag to which to add diagnostics.</param>
        /// <returns>The implementing property or null, if there isn't one.</returns>
        private static Symbol ComputeImplementationForInterfaceMember(Symbol interfaceMember, TypeSymbol implementingType, DiagnosticBag diagnostics)
        {
            Debug.Assert(!implementingType.IsInterfaceType());
            Debug.Assert(interfaceMember.Kind == SymbolKind.Method || interfaceMember.Kind == SymbolKind.Property || interfaceMember.Kind == SymbolKind.Event);
            Debug.Assert(interfaceMember.IsImplementableInterfaceMember());

            NamedTypeSymbol interfaceType = interfaceMember.ContainingType;
            Debug.Assert((object)interfaceType != null && interfaceType.IsInterface);

            bool seenTypeDeclaringInterface = false;

            // NOTE: In other areas of the compiler, we check whether the member is from a specific compilation.
            // We could do the same thing here, but that would mean that callers of the public API would have
            // to pass in a Compilation object when asking about interface implementation.  This extra cost eliminates
            // the small benefit of getting identical answers from "imported" symbols, regardless of whether they
            // are imported as source or metadata symbols.
            //
            // ACASEY: As of 2013/01/24, we are not aware of any cases where the source and metadata behaviors
            // disagree *in code that can be emitted*.  (If there are any, they are likely to involved ambiguous
            // overrides, which typically arise through combinations of ref/out and generics.)  In incorrect code,
            // the source behavior is somewhat more generous (e.g. accepting a method with the wrong return type),
            // but we do not guarantee that incorrect source will be treated in the same way as incorrect metadata.
            // 
            // NOTE: The batch compiler is not affected by this discrepancy, since compilations don't call these
            // APIs on symbols from other compilations.
            bool implementingTypeIsFromSomeCompilation = implementingType.Dangerous_IsFromSomeCompilation;

            Symbol implicitImpl = null;
            Symbol closestMismatch = null;
            bool canBeImplementedImplicitly = interfaceMember.DeclaredAccessibility == Accessibility.Public && !interfaceMember.IsEventOrPropertyWithImplementableNonPublicAccessor();
            TypeSymbol implementingBaseOpt = null; // Calculated only if canBeImplementedImplicitly == true
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            for (TypeSymbol currType = implementingType; (object)currType != null; currType = currType.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
            {
                // NOTE: In the case of PE symbols, it is possible to see an explicit implementation
                // on a type that does not declare the corresponding interface (or one of its
                // subinterfaces).  In such cases, we want to return the explicit implementation,
                // even if it doesn't participate in interface mapping according to the C# rules.

                // pass 1: check for explicit impls (can't assume name matches)
                MultiDictionary<Symbol, Symbol>.ValueSet explicitImpl = currType.GetExplicitImplementationForInterfaceMember(interfaceMember);
                if (explicitImpl.Count == 1)
                {
                    return explicitImpl.Single();
                }
                else if (explicitImpl.Count > 1)
                {
                    diagnostics.Add(ErrorCode.ERR_DuplicateExplicitImpl, implementingType.Locations[0], interfaceMember);
                    return null;
                }

                // WORKAROUND: see comment on method.
                // https://github.com/dotnet/roslyn/issues/34452: Is this workaround still relevant? Do we need it when we are looking
                //                                                for implementations in interfaces?
                if (IsExplicitlyImplementedViaAccessors(interfaceMember, currType, out Symbol currTypeExplicitImpl))
                {
                    // NOTE: may be null.
                    return currTypeExplicitImpl;
                }

                if (!seenTypeDeclaringInterface ||
                    (!canBeImplementedImplicitly && (object)implementingBaseOpt == null))
                {
                    if (currType.InterfacesAndTheirBaseInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics).ContainsKey(interfaceType))
                    {
                        seenTypeDeclaringInterface = true;

                        if (!canBeImplementedImplicitly && (object)implementingBaseOpt == null && (object)currType != implementingType)
                        {
                            implementingBaseOpt = currType;
                        }
                    }
                }


                // We want the implementation from the most derived type at or above the first one to
                // include the interface (or a subinterface) in its interface list
                if (seenTypeDeclaringInterface)
                {
                    //pass 2: check for implicit impls (name must match)
                    Symbol currTypeImplicitImpl;
                    Symbol currTypeCloseMismatch;

                    FindPotentialImplicitImplementationMemberDeclaredInType(
                        interfaceMember,
                        implementingTypeIsFromSomeCompilation,
                        currType,
                        out currTypeImplicitImpl,
                        out currTypeCloseMismatch);

                    if ((object)currTypeImplicitImpl != null)
                    {
                        implicitImpl = currTypeImplicitImpl;
                        break;
                    }

                    if ((object)closestMismatch == null)
                    {
                        closestMismatch = currTypeCloseMismatch;
                    }
                }
            }

            Debug.Assert(!canBeImplementedImplicitly || (object)implementingBaseOpt == null);

            // Dev10 has some extra restrictions and extra wiggle room when finding implicit
            // implementations for interface accessors.  Perform some extra checks and possibly
            // update the result (i.e. implicitImpl).
            if (interfaceMember.IsAccessor())
            {
                // https://github.com/dotnet/roslyn/issues/34453: Do we need to adjust behavior of this function in any way?
                CheckForImplementationOfCorrespondingPropertyOrEvent((MethodSymbol)interfaceMember, implementingType, implementingTypeIsFromSomeCompilation, ref implicitImpl, ref useSiteDiagnostics);
            }

            if ((object)implicitImpl == null && seenTypeDeclaringInterface)
            {
                // Check for default interface implementations
                implicitImpl = FindMostSpecificImplementationInInterfaces(interfaceMember, implementingType, ref useSiteDiagnostics, diagnostics);
            }

#if !DEBUG
            // Don't optimize in DEBUG for better coverage for the GetInterfaceLocation function. 
            if (useSiteDiagnostics != null)
#endif
            {
                diagnostics.Add(GetInterfaceLocation(interfaceMember, implementingType), useSiteDiagnostics);
            }

            if ((object)implicitImpl != null)
            {
                if (!canBeImplementedImplicitly && !implicitImpl.ContainingType.IsInterface)
                {
                    if (interfaceMember.Kind == SymbolKind.Method &&
                        (object)implementingBaseOpt == null) // Otherwise any approprite errors are going to be reported for the base.
                    {
                        diagnostics.Add(ErrorCode.ERR_ImplicitImplementationOfNonPublicInterfaceMember, GetInterfaceLocation(interfaceMember, implementingType),
                                        implementingType, interfaceMember, implicitImpl);
                    }
                }
                else
                {
                    ReportImplicitImplementationMatchDiagnostics(interfaceMember, implementingType, implicitImpl, diagnostics);
                }
            }
            else if ((object)closestMismatch != null)
            {
                Debug.Assert(interfaceMember.DeclaredAccessibility == Accessibility.Public);
                Debug.Assert(!interfaceMember.IsEventOrPropertyWithImplementableNonPublicAccessor());
                ReportImplicitImplementationMismatchDiagnostics(interfaceMember, implementingType, closestMismatch, diagnostics);
            }

            return implicitImpl;
        }

        private static Symbol FindMostSpecificImplementationInInterfaces(Symbol interfaceMember, TypeSymbol implementingType,
                                                                         ref HashSet<DiagnosticInfo> useSiteDiagnostics,
                                                                         DiagnosticBag diagnostics)
        {
            Symbol implicitImpl = FindMostSpecificImplementationInBases(interfaceMember,
                                                                        implementingType,
                                                                        ref useSiteDiagnostics, out Symbol conflict1, out Symbol conflict2);

            if ((object)conflict1 != null)
            {
                Debug.Assert((object)implicitImpl == null);
                Debug.Assert((object)conflict2 != null);
                diagnostics.Add(ErrorCode.ERR_MostSpecificImplementationIsNotFound, GetInterfaceLocation(interfaceMember, implementingType),
                                interfaceMember, conflict1, conflict2);
            }
            else
            {
                Debug.Assert(((object)conflict2 == null));
                // https://github.com/dotnet/roslyn/issues/34453: We might need to do extra consistency check for events/properties and their 
                //                                                accessors. Need to verify if what CheckForImplementationOfCorrespondingPropertyOrEvent
                //                                                is doing should be applicable here as well.
            }

            return implicitImpl;
        }

        private static Symbol FindMostSpecificImplementation(Symbol interfaceMember, NamedTypeSymbol implementingInterface, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            MultiDictionary<Symbol, Symbol>.ValueSet implementingMember = FindImplementationInInterface(interfaceMember, implementingInterface);

            switch (implementingMember.Count)
            {
                case 0:
                    return FindMostSpecificImplementationInBases(interfaceMember, implementingInterface,
                                                                 ref useSiteDiagnostics,
                                                                 out var _, out var _);
                case 1:
                    {
                        Symbol result = implementingMember.Single();

                        if (result.IsAbstract)
                        {
                            return null;
                        }

                        return result;
                    }
                default:
                    return null;
            }
        }

        /// <summary>
        /// One implementation M1 is considered more specific than another implementation M2 
        /// if M1 is declared on interface T1, M2 is declared on interface T2, and 
        /// T1 contains T2 among its direct or indirect interfaces.
        /// </summary>
        private static Symbol FindMostSpecificImplementationInBases(
            Symbol interfaceMember,
            TypeSymbol implementingType,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics,
            out Symbol conflictingImplementation1,
            out Symbol conflictingImplementation2)
        {
            var implementations = ArrayBuilder<(MultiDictionary<Symbol, Symbol>.ValueSet MethodSet, MultiDictionary<NamedTypeSymbol, NamedTypeSymbol> Bases)>.GetInstance();

            foreach (var interfaceType in implementingType.AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
            {
                if (!interfaceType.IsInterface)
                {
                    // this code is reachable in error situations
                    continue;
                }

                MultiDictionary<Symbol, Symbol>.ValueSet candidate = FindImplementationInInterface(interfaceMember, interfaceType);

                if (candidate.Count == 0)
                {
                    continue;
                }

                for (int i = 0; i < implementations.Count; i++)
                {
                    (MultiDictionary<Symbol, Symbol>.ValueSet methodSet, MultiDictionary<NamedTypeSymbol, NamedTypeSymbol> bases) = implementations[i];
                    Symbol previous = methodSet.First();
                    NamedTypeSymbol previousContainingType = previous.ContainingType;

                    if (previousContainingType.Equals(interfaceType, TypeCompareKind.CLRSignatureCompareOptions))
                    {
                        // Last equivalent match wins
                        implementations[i] = (candidate, bases);
                        candidate = default;
                        break;
                    }

                    if (bases == null)
                    {
                        Debug.Assert(implementations.Count == 1);
                        bases = previousContainingType.InterfacesAndTheirBaseInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics);
                        implementations[i] = (methodSet, bases);
                    }

                    if (bases.ContainsKey(interfaceType))
                    {
                        // Previous candidate is more specific
                        candidate = default;
                        break;
                    }
                }

                if (candidate.Count == 0)
                {
                    continue;
                }

                if (implementations.Count != 0)
                {
                    MultiDictionary<NamedTypeSymbol, NamedTypeSymbol> bases = interfaceType.InterfacesAndTheirBaseInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics);

                    for (int i = implementations.Count - 1; i >= 0; i--)
                    {
                        if (bases.ContainsKey(implementations[i].MethodSet.First().ContainingType))
                        {
                            // new candidate is more specific
                            implementations.RemoveAt(i);
                        }
                    }

                    implementations.Add((candidate, bases));
                }
                else
                {
                    implementations.Add((candidate, null));
                }
            }

            Symbol result;

            switch (implementations.Count)
            {
                case 0:
                    result = null;
                    conflictingImplementation1 = null;
                    conflictingImplementation2 = null;
                    break;
                case 1:
                    MultiDictionary<Symbol, Symbol>.ValueSet methodSet = implementations[0].MethodSet;
                    switch (methodSet.Count)
                    {
                        case 1:
                            result = methodSet.Single();
                            if (result.IsAbstract)
                            {
                                result = null;
                            }
                            break;
                        default:
                            result = null;
                            break;
                    }

                    conflictingImplementation1 = null;
                    conflictingImplementation2 = null;
                    break;
                default:
                    result = null;
                    conflictingImplementation1 = implementations[0].MethodSet.First();
                    conflictingImplementation2 = implementations[1].MethodSet.First();
                    break;
            }

            implementations.Free();
            return result;
        }

        internal static MultiDictionary<Symbol, Symbol>.ValueSet FindImplementationInInterface(Symbol interfaceMember, NamedTypeSymbol interfaceType)
        {
            Debug.Assert(interfaceType.IsInterface);

            NamedTypeSymbol containingType = interfaceMember.ContainingType;
            if (containingType.Equals(interfaceType, TypeCompareKind.CLRSignatureCompareOptions))
            {
                if (!interfaceMember.IsAbstract)
                {
                    if (!containingType.Equals(interfaceType, TypeCompareKind.ConsiderEverything))
                    {
                        interfaceMember = interfaceMember.OriginalDefinition.SymbolAsMember(interfaceType);
                    }

                    return new MultiDictionary<Symbol, Symbol>.ValueSet(interfaceMember);
                }

                return default;
            }

            return interfaceType.GetExplicitImplementationForInterfaceMember(interfaceMember);
        }

        /// <summary>
        /// Since dev11 didn't expose a symbol API, it had the luxury of being able to accept a base class's claim that 
        /// it implements an interface.  Roslyn, on the other hand, needs to be able to point to an implementing symbol
        /// for each interface member.
        /// 
        /// DevDiv #718115 was triggered by some unusual metadata in a Microsoft reference assembly (Silverlight System.Windows.dll).
        /// The issue was that a type explicitly implemented the accessors of an interface event, but did not tie them together with
        /// an event declaration.  To make matters worse, it declared its own protected event with the same name as the interface
        /// event (presumably to back the explicit implementation).  As a result, when Roslyn was asked to find the implementing member
        /// for the interface event, it found the protected event and reported an appropriate diagnostic.  Would it should have done
        /// (and does do now) is recognize that no event associated with the accessors explicitly implementing the interface accessors
        /// and returned null.
        /// 
        /// We resolved this issue by introducing a new step into the interface mapping algorithm: after failing to find an explicit
        /// implementation in a type, but before searching for an implicit implementation in that type, check for an explicit implementation
        /// of an associated accessor.  If there is such an implementation, then immediately return the associated property or event,
        /// even if it is null.  That is, never attempt to find an implicit implementation for an interface property or event with an
        /// explicitly implemented accessor.
        /// </summary>
        private static bool IsExplicitlyImplementedViaAccessors(Symbol interfaceMember, TypeSymbol currType, out Symbol implementingMember)
        {
            MethodSymbol interfaceAccessor1;
            MethodSymbol interfaceAccessor2;

            switch (interfaceMember.Kind)
            {
                case SymbolKind.Property:
                    {
                        PropertySymbol interfaceProperty = (PropertySymbol)interfaceMember;
                        interfaceAccessor1 = interfaceProperty.GetMethod;
                        interfaceAccessor2 = interfaceProperty.SetMethod;
                        break;
                    }
                case SymbolKind.Event:
                    {
                        EventSymbol interfaceEvent = (EventSymbol)interfaceMember;
                        interfaceAccessor1 = interfaceEvent.AddMethod;
                        interfaceAccessor2 = interfaceEvent.RemoveMethod;
                        break;
                    }
                default:
                    {
                        implementingMember = null;
                        return false;
                    }
            }

            Symbol associated1;
            Symbol associated2;

            if (TryGetExplicitImplementationAssociatedPropertyOrEvent(interfaceAccessor1, currType, out associated1) |  // NB: not ||
                TryGetExplicitImplementationAssociatedPropertyOrEvent(interfaceAccessor2, currType, out associated2))
            {
                // If there's more than one associated property/event, don't do anything special - just let the algorithm
                // fail in the usual way.
                if ((object)associated1 == null || (object)associated2 == null || associated1 == associated2)
                {
                    implementingMember = associated1 ?? associated2;

                    // In source, we should already have seen an explicit implementation for the interface property/event.
                    // If we haven't then there is no implementation.  We need this check to match dev11 in some edge cases
                    // (e.g. IndexerTests.AmbiguousExplicitIndexerImplementation).  Such cases already fail
                    // to roundtrip correctly, so it's not important to check for a particular compilation.
                    if ((object)implementingMember != null && implementingMember.Dangerous_IsFromSomeCompilation)
                    {
                        implementingMember = null;
                    }

                    return true;
                }
            }

            implementingMember = null;
            return false;
        }

        private static bool TryGetExplicitImplementationAssociatedPropertyOrEvent(MethodSymbol interfaceAccessor, TypeSymbol currType, out Symbol associated)
        {
            if ((object)interfaceAccessor != null)
            {
                // NB: uses a map that was built (and saved) when we checked for an explicit
                // implementation of the interface member.
                MultiDictionary<Symbol, Symbol>.ValueSet set = currType.GetExplicitImplementationForInterfaceMember(interfaceAccessor);
                if (set.Count == 1)
                {
                    Symbol implementation = set.Single();
                    associated = implementation.Kind == SymbolKind.Method
                        ? ((MethodSymbol)implementation).AssociatedSymbol
                        : null;
                    return true;
                }
            }

            associated = null;
            return false;
        }

        /// <summary>
        /// If we were looking for an accessor, then look for an accessor on the implementation of the
        /// corresponding interface property/event.  If it is valid as an implementation (ignoring the name),
        /// then prefer it to our current result if:
        ///   1) our current result is null; or
        ///   2) our current result is on the same type.
        ///   
        /// If there is no corresponding accessor on the implementation of the corresponding interface
        /// property/event and we found an accessor, then the accessor we found is invalid, so clear it.
        /// </summary>
        private static void CheckForImplementationOfCorrespondingPropertyOrEvent(MethodSymbol interfaceMethod, TypeSymbol implementingType, bool implementingTypeIsFromSomeCompilation,
                                                                                 ref Symbol implicitImpl, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(interfaceMethod.IsAccessor());

            Symbol associatedInterfacePropertyOrEvent = interfaceMethod.AssociatedSymbol;
            Symbol implementingPropertyOrEvent = implementingType.FindImplementationForInterfaceMember(associatedInterfacePropertyOrEvent, ref useSiteDiagnostics); // NB: uses cache
            MethodSymbol correspondingImplementingAccessor = null;
            if ((object)implementingPropertyOrEvent != null)
            {
                switch (interfaceMethod.MethodKind)
                {
                    case MethodKind.PropertyGet:
                        correspondingImplementingAccessor = ((PropertySymbol)implementingPropertyOrEvent).GetOwnOrInheritedGetMethod();
                        break;
                    case MethodKind.PropertySet:
                        correspondingImplementingAccessor = ((PropertySymbol)implementingPropertyOrEvent).GetOwnOrInheritedSetMethod();
                        break;
                    case MethodKind.EventAdd:
                        correspondingImplementingAccessor = ((EventSymbol)implementingPropertyOrEvent).GetOwnOrInheritedAddMethod();
                        break;
                    case MethodKind.EventRemove:
                        correspondingImplementingAccessor = ((EventSymbol)implementingPropertyOrEvent).GetOwnOrInheritedRemoveMethod();
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(interfaceMethod.MethodKind);
                }
            }

            if (correspondingImplementingAccessor == implicitImpl)
            {
                return;
            }
            else if ((object)correspondingImplementingAccessor == null && (object)implicitImpl != null && implicitImpl.IsAccessor())
            {
                // If we found an accessor, but it's not (directly or indirectly) on the property implementation,
                // then it's not a valid match.
                implicitImpl = null;
            }
            else if ((object)correspondingImplementingAccessor != null && ((object)implicitImpl == null || TypeSymbol.Equals(correspondingImplementingAccessor.ContainingType, implicitImpl.ContainingType, TypeCompareKind.ConsiderEverything2)))
            {
                // Suppose the interface accessor and the implementing accessor have different names.
                // In Dev10, as long as the corresponding properties have an implementation relationship,
                // then the accessor can be considered an implementation, even though the name is different.
                // Later on, when we check that implementation signatures match exactly
                // (in SourceMemberContainerTypeSymbol.SynthesizeInterfaceMemberImplementation),
                // they won't (because of the names) and an explicit implementation method will be synthesized.

                MethodSymbol interfaceAccessorWithImplementationName = new SignatureOnlyMethodSymbol(
                    correspondingImplementingAccessor.Name,
                    interfaceMethod.ContainingType,
                    interfaceMethod.MethodKind,
                    interfaceMethod.CallingConvention,
                    interfaceMethod.TypeParameters,
                    interfaceMethod.Parameters,
                    interfaceMethod.RefKind,
                    interfaceMethod.ReturnTypeWithAnnotations,
                    interfaceMethod.RefCustomModifiers,
                    interfaceMethod.ExplicitInterfaceImplementations);

                // Make sure that the corresponding accessor is a real implementation.
                if (IsInterfaceMemberImplementation(correspondingImplementingAccessor, interfaceAccessorWithImplementationName, implementingTypeIsFromSomeCompilation))
                {
                    implicitImpl = correspondingImplementingAccessor;
                }
            }
        }

        /// <summary>
        /// These diagnostics are for members that do implicitly implement an interface member, but do so
        /// in an undesirable way.
        /// </summary>
        private static void ReportImplicitImplementationMatchDiagnostics(Symbol interfaceMember, TypeSymbol implementingType, Symbol implicitImpl, DiagnosticBag diagnostics)
        {
            if (implicitImpl.ContainingType.IsInterface)
            {
                if (interfaceMember.Kind == SymbolKind.Method && implementingType.ContainingModule != implicitImpl.ContainingModule)
                {
                    // The default implementation is coming from a different module, which means that we probably didn't check
                    // for the required runtime capability or language version

                    LanguageVersion requiredVersion = MessageID.IDS_DefaultInterfaceImplementation.RequiredVersion();
                    LanguageVersion? availableVersion = implementingType.DeclaringCompilation?.LanguageVersion;
                    if (requiredVersion > availableVersion)
                    {
                        diagnostics.Add(ErrorCode.ERR_LanguageVersionDoesNotSupportDefaultInterfaceImplementationForMember,
                                        GetInterfaceLocation(interfaceMember, implementingType),
                                        implicitImpl, interfaceMember, implementingType,
                                        MessageID.IDS_DefaultInterfaceImplementation.Localize(),
                                        availableVersion.GetValueOrDefault().ToDisplayString(),
                                        new CSharpRequiredLanguageVersion(requiredVersion));
                    }

                    if (!implementingType.ContainingAssembly.RuntimeSupportsDefaultInterfaceImplementation)
                    {
                        diagnostics.Add(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember,
                                        GetInterfaceLocation(interfaceMember, implementingType),
                                        implicitImpl, interfaceMember, implementingType);
                    }
                }

                return;
            }

            bool reportedAnError = false;

            if (interfaceMember.Kind == SymbolKind.Method)
            {
                var interfaceMethod = (MethodSymbol)interfaceMember;
                bool implicitImplIsAccessor = implicitImpl.IsAccessor();
                bool interfaceMethodIsAccessor = interfaceMethod.IsAccessor();

                if (interfaceMethodIsAccessor && !implicitImplIsAccessor && !interfaceMethod.IsIndexedPropertyAccessor())
                {
                    diagnostics.Add(ErrorCode.ERR_MethodImplementingAccessor, GetImplicitImplementationDiagnosticLocation(interfaceMember, implementingType, implicitImpl), implicitImpl, interfaceMethod, implementingType);
                }
                else if (!interfaceMethodIsAccessor && implicitImplIsAccessor)
                {
                    diagnostics.Add(ErrorCode.ERR_AccessorImplementingMethod, GetImplicitImplementationDiagnosticLocation(interfaceMember, implementingType, implicitImpl), implicitImpl, interfaceMethod, implementingType);
                }
                else
                {
                    var implicitImplMethod = (MethodSymbol)implicitImpl;

                    if (implicitImplMethod.IsConditional)
                    {
                        // CS0629: Conditional member '{0}' cannot implement interface member '{1}' in type '{2}'
                        diagnostics.Add(ErrorCode.ERR_InterfaceImplementedByConditional, GetImplicitImplementationDiagnosticLocation(interfaceMember, implementingType, implicitImpl), implicitImpl, interfaceMethod, implementingType);
                    }
                    else if (ReportAnyMismatchedConstraints(interfaceMethod, implementingType, implicitImplMethod, diagnostics))
                    {
                        reportedAnError = true;
                    }
                }
            }

            if (implicitImpl.ContainsTupleNames() && MemberSignatureComparer.ConsideringTupleNamesCreatesDifference(implicitImpl, interfaceMember))
            {
                // it is ok to implement implicitly with no tuple names, for compatibility with C# 6, but otherwise names should match
                diagnostics.Add(ErrorCode.ERR_ImplBadTupleNames, GetImplicitImplementationDiagnosticLocation(interfaceMember, implementingType, implicitImpl), implicitImpl, interfaceMember);
                reportedAnError = true;
            }

            if (!reportedAnError && implementingType.DeclaringCompilation != null)
            {
                CheckNullableReferenceTypeMismatchOnImplementingMember(implementingType, implicitImpl, interfaceMember, isExplicit: false, diagnostics);
            }

            // In constructed types, it is possible to see multiple members with the same (runtime) signature.
            // Now that we know which member will implement the interface member, confirm that it is the only
            // such member.
            if (!implicitImpl.ContainingType.IsDefinition)
            {
                foreach (Symbol member in implicitImpl.ContainingType.GetMembers(implicitImpl.Name))
                {
                    if (member.DeclaredAccessibility != Accessibility.Public || member.IsStatic || member == implicitImpl)
                    {
                        //do nothing - not an ambiguous implementation
                    }
                    else if (MemberSignatureComparer.RuntimeImplicitImplementationComparer.Equals(interfaceMember, member) && !member.IsAccessor())
                    {
                        // CONSIDER: Dev10 does not seem to report this for indexers or their accessors.
                        diagnostics.Add(ErrorCode.WRN_MultipleRuntimeImplementationMatches, GetImplicitImplementationDiagnosticLocation(interfaceMember, implementingType, member), member, interfaceMember, implementingType);
                    }
                }
            }
        }

        internal static void CheckNullableReferenceTypeMismatchOnImplementingMember(TypeSymbol implementingType, Symbol implementingMember, Symbol interfaceMember, bool isExplicit, DiagnosticBag diagnostics)
        {
            if (!implementingMember.IsImplicitlyDeclared && !implementingMember.IsAccessor())
            {
                CSharpCompilation compilation = implementingType.DeclaringCompilation;

                if (interfaceMember.Kind == SymbolKind.Event)
                {
                    var implementingEvent = (EventSymbol)implementingMember;
                    var implementedEvent = (EventSymbol)interfaceMember;
                    SourceMemberContainerTypeSymbol.CheckValidNullableEventOverride(compilation, implementedEvent, implementingEvent,
                                                                                    diagnostics,
                                                                                    (diagnostics, implementedEvent, implementingEvent, arg) =>
                                                                                    {
                                                                                        if (arg.isExplicit)
                                                                                        {
                                                                                            diagnostics.Add(ErrorCode.WRN_NullabilityMismatchInTypeOnExplicitImplementation,
                                                                                                            implementingEvent.Locations[0], new FormattedSymbol(implementedEvent, SymbolDisplayFormat.MinimallyQualifiedFormat));
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            diagnostics.Add(ErrorCode.WRN_NullabilityMismatchInTypeOnImplicitImplementation,
                                                                                                            GetImplicitImplementationDiagnosticLocation(implementedEvent, arg.implementingType, implementingEvent),
                                                                                                            new FormattedSymbol(implementingEvent, SymbolDisplayFormat.MinimallyQualifiedFormat),
                                                                                                            new FormattedSymbol(implementedEvent, SymbolDisplayFormat.MinimallyQualifiedFormat));
                                                                                        }
                                                                                    },
                                                                                    (implementingType, isExplicit));
                }
                else
                {
                    Action<DiagnosticBag, MethodSymbol, MethodSymbol, (TypeSymbol implementingType, bool isExplicit)> reportMismatchInReturnType =
                        (diagnostics, implementedMethod, implementingMethod, arg) =>
                        {
                            if (arg.isExplicit)
                            {
                                diagnostics.Add(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnExplicitImplementation,
                                                implementingMethod.Locations[0], new FormattedSymbol(implementedMethod, SymbolDisplayFormat.MinimallyQualifiedFormat));
                            }
                            else
                            {
                                diagnostics.Add(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnImplicitImplementation,
                                                GetImplicitImplementationDiagnosticLocation(implementedMethod, arg.implementingType, implementingMethod),
                                                new FormattedSymbol(implementingMethod, SymbolDisplayFormat.MinimallyQualifiedFormat),
                                                new FormattedSymbol(implementedMethod, SymbolDisplayFormat.MinimallyQualifiedFormat));
                            }
                        };

                    Action<DiagnosticBag, MethodSymbol, MethodSymbol, ParameterSymbol, (TypeSymbol implementingType, bool isExplicit)> reportMismatchInParameterType =
                        (diagnostics, implementedMethod, implementingMethod, implementingParameter, arg) =>
                        {
                            if (arg.isExplicit)
                            {
                                diagnostics.Add(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnExplicitImplementation,
                                                implementingMethod.Locations[0],
                                                new FormattedSymbol(implementingParameter, SymbolDisplayFormat.ShortFormat),
                                                new FormattedSymbol(implementedMethod, SymbolDisplayFormat.MinimallyQualifiedFormat));
                            }
                            else
                            {
                                diagnostics.Add(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnImplicitImplementation,
                                                GetImplicitImplementationDiagnosticLocation(implementedMethod, arg.implementingType, implementingMethod),
                                                new FormattedSymbol(implementingParameter, SymbolDisplayFormat.ShortFormat),
                                                new FormattedSymbol(implementingMethod, SymbolDisplayFormat.MinimallyQualifiedFormat),
                                                new FormattedSymbol(implementedMethod, SymbolDisplayFormat.MinimallyQualifiedFormat));
                            }
                        };

                    switch (interfaceMember.Kind)
                    {
                        case SymbolKind.Property:
                            var implementingProperty = (PropertySymbol)implementingMember;
                            var implementedProperty = (PropertySymbol)interfaceMember;
                            if (implementedProperty.GetMethod.IsImplementable())
                            {
                                MethodSymbol implementingGetMethod = implementingProperty.GetOwnOrInheritedGetMethod();
                                SourceMemberContainerTypeSymbol.CheckValidNullableMethodOverride(
                                    compilation,
                                    implementedProperty.GetMethod,
                                    implementingGetMethod,
                                    diagnostics,
                                    reportMismatchInReturnType,
                                    // Don't check parameters on the getter if there is a setter
                                    // because they will be a subset of the setter
                                    (!implementedProperty.SetMethod.IsImplementable() ||
                                        implementingGetMethod?.AssociatedSymbol != implementingProperty ||
                                        implementingProperty.GetOwnOrInheritedSetMethod()?.AssociatedSymbol != implementingProperty) ? reportMismatchInParameterType : null,
                                    (implementingType, isExplicit));
                            }

                            if (implementedProperty.SetMethod.IsImplementable())
                            {
                                SourceMemberContainerTypeSymbol.CheckValidNullableMethodOverride(
                                    compilation,
                                    implementedProperty.SetMethod,
                                    implementingProperty.GetOwnOrInheritedSetMethod(),
                                    diagnostics,
                                    null,
                                    reportMismatchInParameterType,
                                    (implementingType, isExplicit));
                            }
                            break;
                        case SymbolKind.Method:
                            var implementingMethod = (MethodSymbol)implementingMember;
                            var implementedMethod = (MethodSymbol)interfaceMember;

                            if (implementedMethod.IsGenericMethod)
                            {
                                implementedMethod = implementedMethod.Construct(implementingMethod.TypeArgumentsWithAnnotations);
                            }

                            SourceMemberContainerTypeSymbol.CheckValidNullableMethodOverride(
                                compilation,
                                implementedMethod,
                                implementingMethod,
                                diagnostics,
                                reportMismatchInReturnType,
                                reportMismatchInParameterType,
                                (implementingType, isExplicit));
                            break;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(interfaceMember.Kind);
                    }
                }
            }
        }

        /// <summary>
        /// These diagnostics are for members that almost, but not actually, implicitly implement an interface member.
        /// </summary>
        private static void ReportImplicitImplementationMismatchDiagnostics(Symbol interfaceMember, TypeSymbol implementingType, Symbol closestMismatch, DiagnosticBag diagnostics)
        {
            // Determine  a better location for diagnostic squiggles.  Squiggle the interface rather than the class.
            Location interfaceLocation = GetInterfaceLocation(interfaceMember, implementingType);

            if (closestMismatch.IsStatic)
            {
                diagnostics.Add(ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, interfaceLocation, implementingType, interfaceMember, closestMismatch);
            }
            else if (closestMismatch.DeclaredAccessibility != Accessibility.Public)
            {
                ErrorCode errorCode = interfaceMember.IsAccessor() ? ErrorCode.ERR_UnimplementedInterfaceAccessor : ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic;
                diagnostics.Add(errorCode, interfaceLocation, implementingType, interfaceMember, closestMismatch);
            }
            else //return ref kind or type doesn't match
            {
                RefKind interfaceMemberRefKind = RefKind.None;
                TypeSymbol interfaceMemberReturnType;
                switch (interfaceMember.Kind)
                {
                    case SymbolKind.Method:
                        var method = (MethodSymbol)interfaceMember;
                        interfaceMemberRefKind = method.RefKind;
                        interfaceMemberReturnType = method.ReturnType;
                        break;
                    case SymbolKind.Property:
                        var property = (PropertySymbol)interfaceMember;
                        interfaceMemberRefKind = property.RefKind;
                        interfaceMemberReturnType = property.Type;
                        break;
                    case SymbolKind.Event:
                        interfaceMemberReturnType = ((EventSymbol)interfaceMember).Type;
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(interfaceMember.Kind);
                }

                bool hasRefReturnMismatch = false;
                switch (closestMismatch.Kind)
                {
                    case SymbolKind.Method:
                        hasRefReturnMismatch = ((MethodSymbol)closestMismatch).RefKind != interfaceMemberRefKind;
                        break;

                    case SymbolKind.Property:
                        hasRefReturnMismatch = ((PropertySymbol)closestMismatch).RefKind != interfaceMemberRefKind;
                        break;
                }

                DiagnosticInfo useSiteDiagnostic;
                if ((object)interfaceMemberReturnType != null &&
                    (useSiteDiagnostic = interfaceMemberReturnType.GetUseSiteDiagnostic()) != null &&
                    useSiteDiagnostic.DefaultSeverity == DiagnosticSeverity.Error)
                {
                    diagnostics.Add(useSiteDiagnostic, interfaceLocation);
                }
                else if (hasRefReturnMismatch)
                {
                    diagnostics.Add(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn, interfaceLocation, implementingType, interfaceMember, closestMismatch);
                }
                else
                {
                    diagnostics.Add(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, interfaceLocation, implementingType, interfaceMember, closestMismatch, interfaceMemberReturnType);
                }
            }
        }

        /// <summary>
        /// Determine a better location for diagnostic squiggles.  Squiggle the interface rather than the class.
        /// </summary>
        private static Location GetInterfaceLocation(Symbol interfaceMember, TypeSymbol implementingType)
        {
            Debug.Assert((object)implementingType != null);
            var @interface = interfaceMember.ContainingType;

            SourceMemberContainerTypeSymbol snt = null;
            if (implementingType.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics[@interface].Contains(@interface))
            {
                snt = implementingType as SourceMemberContainerTypeSymbol;
            }

            return snt?.GetImplementsLocation(@interface) ?? implementingType.Locations.FirstOrNone();
        }

        private static bool ReportAnyMismatchedConstraints(MethodSymbol interfaceMethod, TypeSymbol implementingType, MethodSymbol implicitImpl, DiagnosticBag diagnostics)
        {
            Debug.Assert(interfaceMethod.Arity == implicitImpl.Arity);

            bool result = false;
            var arity = interfaceMethod.Arity;

            if (arity > 0)
            {
                var typeParameters1 = interfaceMethod.TypeParameters;
                var typeParameters2 = implicitImpl.TypeParameters;
                var indexedTypeParameters = IndexedTypeParameterSymbol.Take(arity);

                var typeMap1 = new TypeMap(typeParameters1, indexedTypeParameters, allowAlpha: true);
                var typeMap2 = new TypeMap(typeParameters2, indexedTypeParameters, allowAlpha: true);

                // Report any mismatched method constraints.
                for (int i = 0; i < arity; i++)
                {
                    var typeParameter1 = typeParameters1[i];
                    var typeParameter2 = typeParameters2[i];

                    if (!MemberSignatureComparer.HaveSameConstraints(typeParameter1, typeMap1, typeParameter2, typeMap2))
                    {
                        // If the matching method for the interface member is defined on the implementing type,
                        // the matching method location is used for the error. Otherwise, the location of the
                        // implementing type is used. (This differs from Dev10 which associates the error with
                        // the closest method always. That behavior can be confusing though, since in the case
                        // of "interface I { M; } class A { M; } class B : A, I { }", this means reporting an error on
                        // A.M that it does not satisfy I.M even though A does not implement I. Furthermore if
                        // A is defined in metadata, there is no location for A.M. Instead, we simply report the
                        // error on B if the match to I.M is in a base class.)
                        diagnostics.Add(ErrorCode.ERR_ImplBadConstraints, GetImplicitImplementationDiagnosticLocation(interfaceMethod, implementingType, implicitImpl), typeParameter2.Name, implicitImpl, typeParameter1.Name, interfaceMethod);
                    }
                    else if (!MemberSignatureComparer.HaveSameNullabilityInConstraints(typeParameter1, typeMap1, typeParameter2, typeMap2))
                    {
                        diagnostics.Add(ErrorCode.WRN_NullabilityMismatchInConstraintsOnImplicitImplementation, GetImplicitImplementationDiagnosticLocation(interfaceMethod, implementingType, implicitImpl),
                                        typeParameter2.Name, implicitImpl, typeParameter1.Name, interfaceMethod);
                    }
                }
            }

            return result;
        }

        internal static Location GetImplicitImplementationDiagnosticLocation(Symbol interfaceMember, TypeSymbol implementingType, Symbol member)
        {
            if (TypeSymbol.Equals(member.ContainingType, implementingType, TypeCompareKind.ConsiderEverything2))
            {
                return member.Locations[0];
            }
            else
            {
                var @interface = interfaceMember.ContainingType;
                SourceMemberContainerTypeSymbol snt = implementingType as SourceMemberContainerTypeSymbol;
                return snt?.GetImplementsLocation(@interface) ?? implementingType.Locations[0];
            }
        }

        /// <summary>
        /// Search the declared members of a type for one that could be an implementation
        /// of a given interface member (depending on interface declarations).
        /// </summary>
        /// <param name="interfaceMember">The interface member being implemented.</param>
        /// <param name="implementingTypeIsFromSomeCompilation">True if the implementing type is from some compilation (i.e. not from metadata).</param>
        /// <param name="currType">The type on which we are looking for a declared implementation of the interface member.</param>
        /// <param name="implicitImpl">A member on currType that could implement the interface, or null.</param>
        /// <param name="closeMismatch">A member on currType that could have been an attempt to implement the interface, or null.</param>
        /// <remarks>
        /// There is some similarity between this member and OverriddenOrHiddenMembersHelpers.FindOverriddenOrHiddenMembersInType.
        /// When making changes to this member, think about whether or not they should also be applied in MemberSymbol.
        /// One key difference is that custom modifiers are considered when looking up overridden members, but
        /// not when looking up implicit implementations.  We're preserving this behavior from Dev10.
        /// </remarks>
        private static void FindPotentialImplicitImplementationMemberDeclaredInType(
            Symbol interfaceMember,
            bool implementingTypeIsFromSomeCompilation,
            TypeSymbol currType,
            out Symbol implicitImpl,
            out Symbol closeMismatch)
        {
            implicitImpl = null;
            closeMismatch = null;

            foreach (Symbol member in currType.GetMembers(interfaceMember.Name))
            {
                if (member.Kind == interfaceMember.Kind)
                {
                    if (IsInterfaceMemberImplementation(member, interfaceMember, implementingTypeIsFromSomeCompilation))
                    {
                        implicitImpl = member;
                        return;
                    }

                    // If we haven't found a match, do a weaker comparison that ignores static-ness, accessibility, and return type.
                    // But do this only if interface member is public because language doesn't allow implicit implementations for
                    // non-public members and, since candidate's signature doesn't match, runtime will never pick it up either. 
                    else if ((object)closeMismatch == null && implementingTypeIsFromSomeCompilation &&
                             interfaceMember.DeclaredAccessibility == Accessibility.Public &&
                             !interfaceMember.IsEventOrPropertyWithImplementableNonPublicAccessor())
                    {
                        // We can ignore custom modifiers here, because our goal is to improve the helpfulness
                        // of an error we're already giving, rather than to generate a new error.
                        if (MemberSignatureComparer.CSharpCloseImplicitImplementationComparer.Equals(interfaceMember, member))
                        {
                            closeMismatch = member;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// To implement an interface member, a candidate member must be public, non-static, and have
        /// the same signature.  "Have the same signature" has a looser definition if the type implementing
        /// the interface is from source.
        /// </summary>
        /// <remarks>
        /// PROPERTIES:
        /// NOTE: we're not checking whether this property has at least the accessors
        /// declared in the interface.  Dev10 considers it a match either way and,
        /// reports failure to implement accessors separately.
        ///
        /// If the implementing type (i.e. the type with the interface in its interface
        /// list) is in source, then we can ignore custom modifiers in/on the property
        /// type because they will be copied into the bridge property that explicitly
        /// implements the interface property (or they would be, if we created such
        /// a bridge property).  Bridge *methods* (not properties) are inserted in 
        /// SourceMemberContainerTypeSymbol.SynthesizeInterfaceMemberImplementation.
        ///
        /// CONSIDER: The spec for interface mapping (13.4.4) could be interpreted to mean that this
        /// property is not an implementation unless it has an accessor for each accessor of the
        /// interface property.  For now, we prefer to represent that case as having an implemented
        /// property and an unimplemented accessor because it makes finding accessor implementations
        /// much easier.  If we decide that we want the API to report the property as unimplemented,
        /// then it might be appropriate to keep current result internally and just check the accessors
        /// before returning the value from the public API (similar to the way MethodSymbol.OverriddenMethod
        /// filters MethodSymbol.OverriddenOrHiddenMembers.
        /// </remarks>
        private static bool IsInterfaceMemberImplementation(Symbol candidateMember, Symbol interfaceMember, bool implementingTypeIsFromSomeCompilation)
        {
            if (candidateMember.DeclaredAccessibility != Accessibility.Public || candidateMember.IsStatic)
            {
                return false;
            }
            else if (implementingTypeIsFromSomeCompilation)
            {
                // We're specifically ignoring custom modifiers for source types because that's what Dev10 does.
                // Inexact matches are acceptable because we'll just generate bridge members - explicit implementations
                // with exact signatures that delegate to the inexact match.  This happens automatically in
                // SourceMemberContainerTypeSymbol.SynthesizeInterfaceMemberImplementation.
                return MemberSignatureComparer.CSharpImplicitImplementationComparer.Equals(interfaceMember, candidateMember);
            }
            else
            {
                // NOTE: Dev10 seems to use the C# rules in this case as well, but it doesn't give diagnostics about
                // the failure of a metadata type to implement an interface so there's no problem with reporting the
                // CLI interpretation instead.  For example, using this comparer might allow a member with a ref 
                // parameter to implement a member with an out parameter -  which Dev10 would not allow - but that's
                // okay because Dev10's behavior is not observable.
                return MemberSignatureComparer.RuntimeImplicitImplementationComparer.Equals(interfaceMember, candidateMember);
            }
        }

        protected MultiDictionary<Symbol, Symbol>.ValueSet GetExplicitImplementationForInterfaceMember(Symbol interfaceMember)
        {
            var info = this.GetInterfaceInfo();
            if (info == s_noInterfaces)
            {
                return default;
            }

            if (info.explicitInterfaceImplementationMap == null)
            {
                Interlocked.CompareExchange(ref info.explicitInterfaceImplementationMap, MakeExplicitInterfaceImplementationMap(), null);
            }

            return info.explicitInterfaceImplementationMap[interfaceMember];
        }

        private MultiDictionary<Symbol, Symbol> MakeExplicitInterfaceImplementationMap()
        {
            var map = new MultiDictionary<Symbol, Symbol>(ExplicitInterfaceImplementationTargetMemberEqualityComparer.Instance);
            foreach (var member in this.GetMembersUnordered())
            {
                foreach (var interfaceMember in member.GetExplicitInterfaceImplementations())
                {
                    Debug.Assert(interfaceMember.Kind != SymbolKind.Method || (object)interfaceMember == ((MethodSymbol)interfaceMember).ConstructedFrom);
                    map.Add(interfaceMember, member);
                }
            }
            return map;
        }

        protected class ExplicitInterfaceImplementationTargetMemberEqualityComparer : IEqualityComparer<Symbol>
        {
            public static readonly ExplicitInterfaceImplementationTargetMemberEqualityComparer Instance = new ExplicitInterfaceImplementationTargetMemberEqualityComparer();

            private ExplicitInterfaceImplementationTargetMemberEqualityComparer() { }
            public bool Equals(Symbol x, Symbol y)
            {
                return x.OriginalDefinition == y.OriginalDefinition &&
                       x.ContainingType.Equals(y.ContainingType, TypeCompareKind.CLRSignatureCompareOptions);
            }

            public int GetHashCode(Symbol obj)
            {
                return obj.OriginalDefinition.GetHashCode();
            }
        }

        #endregion Interface member checks

        #region Abstract base type checks

        /// <summary>
        /// The set of abstract members in declared in this type or declared in a base type and not overridden.
        /// </summary>
        internal ImmutableHashSet<Symbol> AbstractMembers
        {
            get
            {
                if (_lazyAbstractMembers == null)
                {
                    Interlocked.CompareExchange(ref _lazyAbstractMembers, ComputeAbstractMembers(), null);
                }
                return _lazyAbstractMembers;
            }
        }

        private ImmutableHashSet<Symbol> ComputeAbstractMembers()
        {
            var abstractMembers = ImmutableHashSet.Create<Symbol>();
            var overriddenMembers = ImmutableHashSet.Create<Symbol>();

            foreach (var member in this.GetMembersUnordered())
            {
                if (this.IsAbstract && member.IsAbstract && member.Kind != SymbolKind.NamedType)
                {
                    abstractMembers = abstractMembers.Add(member);
                }

                Symbol overriddenMember = null;
                switch (member.Kind)
                {
                    case SymbolKind.Method:
                        {
                            overriddenMember = ((MethodSymbol)member).OverriddenMethod;
                            break;
                        }
                    case SymbolKind.Property:
                        {
                            overriddenMember = ((PropertySymbol)member).OverriddenProperty;
                            break;
                        }
                    case SymbolKind.Event:
                        {
                            overriddenMember = ((EventSymbol)member).OverriddenEvent;
                            break;
                        }
                }

                if ((object)overriddenMember != null)
                {
                    overriddenMembers = overriddenMembers.Add(overriddenMember);
                }
            }

            if ((object)this.BaseTypeNoUseSiteDiagnostics != null && this.BaseTypeNoUseSiteDiagnostics.IsAbstract)
            {
                foreach (var baseAbstractMember in this.BaseTypeNoUseSiteDiagnostics.AbstractMembers)
                {
                    if (!overriddenMembers.Contains(baseAbstractMember))
                    {
                        abstractMembers = abstractMembers.Add(baseAbstractMember);
                    }
                }
            }

            return abstractMembers;
        }

        #endregion Abstract base type checks

        [Obsolete("Use TypeWithAnnotations.Is method.", true)]
        internal bool Equals(TypeWithAnnotations other)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public static bool Equals(TypeSymbol left, TypeSymbol right, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverrideOpt = null)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right, comparison, isValueTypeOverrideOpt);
        }

        [Obsolete("Use 'TypeSymbol.Equals(TypeSymbol, TypeSymbol, TypeCompareKind)' method.", true)]
        public static bool operator ==(TypeSymbol left, TypeSymbol right)
            => throw ExceptionUtilities.Unreachable;

        [Obsolete("Use 'TypeSymbol.Equals(TypeSymbol, TypeSymbol, TypeCompareKind)' method.", true)]
        public static bool operator !=(TypeSymbol left, TypeSymbol right)
            => throw ExceptionUtilities.Unreachable;

        [Obsolete("Use 'TypeSymbol.Equals(TypeSymbol, TypeSymbol, TypeCompareKind)' method.", true)]
        public static bool operator ==(Symbol left, TypeSymbol right)
            => throw ExceptionUtilities.Unreachable;

        [Obsolete("Use 'TypeSymbol.Equals(TypeSymbol, TypeSymbol, TypeCompareKind)' method.", true)]
        public static bool operator !=(Symbol left, TypeSymbol right)
            => throw ExceptionUtilities.Unreachable;

        [Obsolete("Use 'TypeSymbol.Equals(TypeSymbol, TypeSymbol, TypeCompareKind)' method.", true)]
        public static bool operator ==(TypeSymbol left, Symbol right)
            => throw ExceptionUtilities.Unreachable;

        [Obsolete("Use 'TypeSymbol.Equals(TypeSymbol, TypeSymbol, TypeCompareKind)' method.", true)]
        public static bool operator !=(TypeSymbol left, Symbol right)
            => throw ExceptionUtilities.Unreachable;
    }
}
