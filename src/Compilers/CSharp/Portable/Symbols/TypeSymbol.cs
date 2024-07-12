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
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

#pragma warning disable CS0660

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A TypeSymbol is a base class for all the symbols that represent a type
    /// in C#.
    /// </summary>
    internal abstract partial class TypeSymbol : NamespaceOrTypeSymbol, ITypeSymbolInternal
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

            internal static readonly MultiDictionary<NamedTypeSymbol, NamedTypeSymbol> EmptyInterfacesAndTheirBaseInterfaces =
                                                new MultiDictionary<NamedTypeSymbol, NamedTypeSymbol>(0, SymbolEqualityComparer.CLRSignature);

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
#nullable enable
            internal ImmutableDictionary<MethodSymbol, MethodSymbol>? synthesizedMethodImplMap;
#nullable disable
            internal bool IsDefaultValue()
            {
                return allInterfaces.IsDefault &&
                    interfacesAndTheirBaseInterfaces == null &&
                    _implementationForInterfaceMemberMap == null &&
                    explicitInterfaceImplementationMap == null &&
                    synthesizedMethodImplMap == null;
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

        protected sealed override Symbol OriginalSymbolDefinition
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

        internal NamedTypeSymbol BaseTypeWithDefinitionUseSiteDiagnostics(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            var result = BaseTypeNoUseSiteDiagnostics;

            if ((object)result != null)
            {
                result.OriginalDefinition.AddUseSiteInfo(ref useSiteInfo);
            }

            return result;
        }

        internal NamedTypeSymbol BaseTypeOriginalDefinition(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            var result = BaseTypeNoUseSiteDiagnostics;

            if ((object)result != null)
            {
                result = result.OriginalDefinition;
                result.AddUseSiteInfo(ref useSiteInfo);
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

        internal ImmutableArray<NamedTypeSymbol> AllInterfacesWithDefinitionUseSiteDiagnostics(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            var result = AllInterfacesNoUseSiteDiagnostics;

            // Since bases affect content of AllInterfaces set, we need to make sure they all are good.
            var current = this;

            do
            {
                current = current.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteInfo);
            }
            while ((object)current != null);

            foreach (var iface in result)
            {
                iface.OriginalDefinition.AddUseSiteInfo(ref useSiteInfo);
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

        internal TypeSymbol EffectiveType(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            return this.IsTypeParameter() ? ((TypeParameterSymbol)this).EffectiveBaseClass(ref useSiteInfo) : this;
        }

        /// <summary>
        /// Returns true if this type derives from a given type.
        /// </summary>
        internal bool IsDerivedFrom(TypeSymbol type, TypeCompareKind comparison, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert((object)type != null);
            Debug.Assert(!type.IsTypeParameter());

            if ((object)this == (object)type)
            {
                return false;
            }

            var t = this.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteInfo);
            while ((object)t != null)
            {
                if (type.Equals(t, comparison))
                {
                    return true;
                }

                t = t.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteInfo);
            }

            return false;
        }

        /// <summary>
        /// Returns true if this type is equal or derives from a given type.
        /// </summary>
        internal bool IsEqualToOrDerivedFrom(TypeSymbol type, TypeCompareKind comparison, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            return this.Equals(type, comparison) || this.IsDerivedFrom(type, comparison, ref useSiteInfo);
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
        /// <returns>True if the types are equivalent.</returns>
        internal virtual bool Equals(TypeSymbol t2, TypeCompareKind compareKind)
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
                    addAllInterfaces(interfaces[i], visited, result);
                }
            }

            result.ReverseContents();
            return result.ToImmutableAndFree();

            static void addAllInterfaces(NamedTypeSymbol @interface, HashSet<NamedTypeSymbol> visited, ArrayBuilder<NamedTypeSymbol> result)
            {
                if (visited.Add(@interface))
                {
                    ImmutableArray<NamedTypeSymbol> baseInterfaces = @interface.InterfacesNoUseSiteDiagnostics();
                    for (int i = baseInterfaces.Length - 1; i >= 0; i--)
                    {
                        var baseInterface = baseInterfaces[i];
                        addAllInterfaces(baseInterface, visited, result);
                    }

                    result.Add(@interface);
                }
            }
        }

        /// <summary>
        /// Gets the set of interfaces that this type directly implements, plus the base interfaces
        /// of all such types. Keys are compared using <see cref="SymbolEqualityComparer.CLRSignature"/>,
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

        internal MultiDictionary<NamedTypeSymbol, NamedTypeSymbol> InterfacesAndTheirBaseInterfacesWithDefinitionUseSiteDiagnostics(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            var result = InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics;

            foreach (var iface in result.Keys)
            {
                iface.OriginalDefinition.AddUseSiteInfo(ref useSiteInfo);
            }

            return result;
        }

        // Note: Unlike MakeAllInterfaces, this doesn't need to be virtual. It depends on
        // AllInterfaces for its implementation, so it will pick up all changes to MakeAllInterfaces
        // indirectly.
        private static MultiDictionary<NamedTypeSymbol, NamedTypeSymbol> MakeInterfacesAndTheirBaseInterfaces(ImmutableArray<NamedTypeSymbol> declaredInterfaces)
        {
            var resultBuilder = new MultiDictionary<NamedTypeSymbol, NamedTypeSymbol>(declaredInterfaces.Length, SymbolEqualityComparer.CLRSignature, SymbolEqualityComparer.ConsiderEverything);
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
        public Symbol FindImplementationForInterfaceMember(Symbol interfaceMember)
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
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                return FindMostSpecificImplementation(interfaceMember, (NamedTypeSymbol)this, ref discardedUseSiteInfo);
            }

            return FindImplementationForInterfaceMemberInNonInterface(interfaceMember);
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
        public virtual ExtendedSpecialType ExtendedSpecialType
        {
            get
            {
                return default;
            }
        }

        public SpecialType SpecialType => (SpecialType)ExtendedSpecialType;

        /// <summary>
        /// Gets corresponding primitive type code for this type declaration.
        /// </summary>
        internal Microsoft.Cci.PrimitiveTypeCode PrimitiveTypeCode
            => TypeKind switch
            {
                TypeKind.Pointer => Microsoft.Cci.PrimitiveTypeCode.Pointer,
                TypeKind.FunctionPointer => Microsoft.Cci.PrimitiveTypeCode.FunctionPointer,
                _ => SpecialTypes.GetTypeCode(SpecialType)
            };

        #region Use-Site Diagnostics

        /// <summary>
        /// Returns true if the error code is highest priority while calculating use site error for this symbol. 
        /// </summary>
        protected sealed override bool IsHighestPriorityUseSiteErrorCode(int code)
            => code is (int)ErrorCode.ERR_UnsupportedCompilerFeature or (int)ErrorCode.ERR_BogusType;

        public override bool HasUnsupportedMetadata
        {
            get
            {
                DiagnosticInfo info = GetUseSiteInfo().DiagnosticInfo;
                return (object)info != null && info.Code is (int)ErrorCode.ERR_UnsupportedCompilerFeature or (int)ErrorCode.ERR_BogusType;
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
        /// True if the type represents a native integer. In C#, the types represented
        /// by language keywords 'nint' and 'nuint' on platforms where they are not unified
        /// with 'System.IntPtr' and 'System.UIntPtr'.
        /// </summary>
        internal virtual bool IsNativeIntegerWrapperType => false;

        internal bool IsNativeIntegerType => IsNativeIntegerWrapperType
            || (SpecialType is SpecialType.System_IntPtr or SpecialType.System_UIntPtr && this.ContainingAssembly.RuntimeSupportsNumericIntPtr);

        /// <summary>
        /// Verify if the given type is a tuple of a given cardinality, or can be used to back a tuple type 
        /// with the given cardinality. 
        /// </summary>
        internal bool IsTupleTypeOfCardinality(int targetCardinality)
        {
            if (IsTupleType)
            {
                return TupleElementTypesWithAnnotations.Length == targetCardinality;
            }

            return false;
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

#nullable enable
        /// <summary>
        /// Is this type a managed type (false for everything but enum, pointer, and
        /// some struct types).
        /// </summary>
        /// <remarks>
        /// See Type::computeManagedType.
        /// </remarks>
        internal bool IsManagedType(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo) => GetManagedKind(ref useSiteInfo) == ManagedKind.Managed;

        internal bool IsManagedTypeNoUseSiteDiagnostics
        {
            get
            {
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                return IsManagedType(ref discardedUseSiteInfo);
            }
        }

        /// <summary>
        /// Indicates whether a type is managed or not (i.e. you can take a pointer to it).
        /// Contains additional cases to help implement FeatureNotAvailable diagnostics.
        /// </summary>
        internal abstract ManagedKind GetManagedKind(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo);

        internal ManagedKind ManagedKindNoUseSiteDiagnostics
        {
            get
            {
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                return GetManagedKind(ref discardedUseSiteInfo);
            }
        }
#nullable disable

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

        private static readonly Func<TypeWithAnnotations, TypeWithAnnotations> s_setUnknownNullability =
            (type) => type.SetUnknownNullabilityForReferenceTypes();

        /// <summary>
        /// Merges features of the type with another type where there is an identity conversion between them.
        /// The features to be merged are
        /// object vs dynamic (dynamic wins), tuple names (dropped in case of conflict), and nullable
        /// annotations (e.g. in type arguments).
        /// </summary>
        internal abstract TypeSymbol MergeEquivalentTypes(TypeSymbol other, VarianceKind variance);

        /// <summary>
        /// Returns true if the type may contain embedded references
        /// </summary>
        public abstract bool IsRefLikeType { get; }

        /// <summary>
        /// Returns true if the type is a readonly struct
        /// </summary>
        public abstract bool IsReadOnly { get; }

        public string ToDisplayString(CodeAnalysis.NullableFlowState topLevelNullability, SymbolDisplayFormat format = null)
        {
            return SymbolDisplay.ToDisplayString((ITypeSymbol)ISymbol, topLevelNullability, format);
        }

        public ImmutableArray<SymbolDisplayPart> ToDisplayParts(CodeAnalysis.NullableFlowState topLevelNullability, SymbolDisplayFormat format = null)
        {
            return SymbolDisplay.ToDisplayParts((ITypeSymbol)ISymbol, topLevelNullability, format);
        }

        public string ToMinimalDisplayString(
            SemanticModel semanticModel,
            CodeAnalysis.NullableFlowState topLevelNullability,
            int position,
            SymbolDisplayFormat format = null)
        {
            return SymbolDisplay.ToMinimalDisplayString((ITypeSymbol)ISymbol, topLevelNullability, semanticModel, position, format);
        }

        public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(
            SemanticModel semanticModel,
            CodeAnalysis.NullableFlowState topLevelNullability,
            int position,
            SymbolDisplayFormat format = null)
        {
            return SymbolDisplay.ToMinimalDisplayParts((ITypeSymbol)ISymbol, topLevelNullability, semanticModel, position, format);
        }

        #region Interface member checks

        /// <summary>
        /// Locate implementation of the <paramref name="interfaceMember"/> in context of the current type.
        /// The method is using cache to optimize subsequent calls for the same <paramref name="interfaceMember"/>.
        /// </summary>
        /// <param name="interfaceMember">Member for which an implementation should be found.</param>
        /// <param name="ignoreImplementationInInterfacesIfResultIsNotReady">
        /// The process of looking up an implementation for an accessor can involve figuring out how corresponding event/property is implemented,
        /// <see cref="CheckForImplementationOfCorrespondingPropertyOrEvent"/>. And the process of looking up an implementation for a property can
        /// involve figuring out how corresponding accessors are implemented, <see cref="FindMostSpecificImplementationInInterfaces"/>. This can 
        /// lead to cycles, which could be avoided if we ignore the presence of implementations in interfaces for the purpose of
        /// <see cref="CheckForImplementationOfCorrespondingPropertyOrEvent"/>. Fortunately, logic in it allows us to ignore the presence of
        /// implementations in interfaces and we use that.
        /// When the value of this parameter is true and the result that takes presence of implementations in interfaces into account is not
        /// available from the cache, the lookup will be performed ignoring the presence of implementations in interfaces. Otherwise, result from
        /// the cache is returned.
        /// When the value of the parameter is false, the result from the cache is returned, or calculated, taking presence of implementations
        /// in interfaces into account and then cached.
        /// This means that:
        ///  - A symbol from an interface can still be returned even when <paramref name="ignoreImplementationInInterfacesIfResultIsNotReady"/> is true.
        ///    A subsequent call with <paramref name="ignoreImplementationInInterfacesIfResultIsNotReady"/> false will return the same value. 
        ///  - If symbol from a non-interface is returned when <paramref name="ignoreImplementationInInterfacesIfResultIsNotReady"/> is true. A subsequent
        ///    call with <paramref name="ignoreImplementationInInterfacesIfResultIsNotReady"/> false will return the same value.
        ///  - If no symbol is returned for <paramref name="ignoreImplementationInInterfacesIfResultIsNotReady"/> true. A subsequent call with
        ///    <paramref name="ignoreImplementationInInterfacesIfResultIsNotReady"/> might return a symbol, but that symbol guaranteed to be from an interface.
        ///  - If the first request is done with <paramref name="ignoreImplementationInInterfacesIfResultIsNotReady"/> false. A subsequent call
        ///    is guaranteed to return the same result regardless of <paramref name="ignoreImplementationInInterfacesIfResultIsNotReady"/> value.
        /// </param>
        internal SymbolAndDiagnostics FindImplementationForInterfaceMemberInNonInterfaceWithDiagnostics(Symbol interfaceMember, bool ignoreImplementationInInterfacesIfResultIsNotReady = false)
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

                    result = ComputeImplementationAndDiagnosticsForInterfaceMember(interfaceMember, ignoreImplementationInInterfaces: ignoreImplementationInInterfacesIfResultIsNotReady,
                                                                                   out bool implementationInInterfacesMightChangeResult);

                    Debug.Assert(ignoreImplementationInInterfacesIfResultIsNotReady || !implementationInInterfacesMightChangeResult);
                    Debug.Assert(!implementationInInterfacesMightChangeResult || result.Symbol is null);
                    if (!implementationInInterfacesMightChangeResult)
                    {
                        map.TryAdd(interfaceMember, result);
                    }
                    return result;

                default:
                    return SymbolAndDiagnostics.Empty;
            }
        }

        internal Symbol FindImplementationForInterfaceMemberInNonInterface(Symbol interfaceMember, bool ignoreImplementationInInterfacesIfResultIsNotReady = false)
        {
            return FindImplementationForInterfaceMemberInNonInterfaceWithDiagnostics(interfaceMember, ignoreImplementationInInterfacesIfResultIsNotReady).Symbol;
        }

        private SymbolAndDiagnostics ComputeImplementationAndDiagnosticsForInterfaceMember(Symbol interfaceMember, bool ignoreImplementationInInterfaces, out bool implementationInInterfacesMightChangeResult)
        {
            var diagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: this.DeclaringCompilation is object);
            var implementingMember = ComputeImplementationForInterfaceMember(interfaceMember, this, diagnostics, ignoreImplementationInInterfaces, out implementationInInterfacesMightChangeResult);
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
        /// <param name="ignoreImplementationInInterfaces">Do not consider implementation in an interface as a valid candidate for the purpose of this computation.</param>
        /// <param name="implementationInInterfacesMightChangeResult">
        /// Returns true when <paramref name="ignoreImplementationInInterfaces"/> is true, the method fails to locate an implementation and an implementation in
        /// an interface, if any (its presence is not checked), could potentially be a candidate. Returns false otherwise.
        /// When true is returned, a different call with <paramref name="ignoreImplementationInInterfaces"/> false might return a symbol. That symbol, if any,
        /// is guaranteed to be from an interface.
        /// This parameter is used to optimize caching in <see cref="FindImplementationForInterfaceMemberInNonInterfaceWithDiagnostics"/>.
        /// </param>
        /// <returns>The implementing property or null, if there isn't one.</returns>
        private static Symbol ComputeImplementationForInterfaceMember(Symbol interfaceMember, TypeSymbol implementingType, BindingDiagnosticBag diagnostics,
                                                                      bool ignoreImplementationInInterfaces, out bool implementationInInterfacesMightChangeResult)
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
            bool implementingTypeIsFromSomeCompilation = false;

            Symbol implicitImpl = null;
            Symbol closestMismatch = null;
            bool canBeImplementedImplicitlyInCSharp9 = interfaceMember.DeclaredAccessibility == Accessibility.Public && !interfaceMember.IsEventOrPropertyWithImplementableNonPublicAccessor();
            TypeSymbol implementingBaseOpt = null; // Calculated only if canBeImplementedImplicitly == false
            bool implementingTypeImplementsInterface = false;
            CSharpCompilation compilation = implementingType.DeclaringCompilation;
            var useSiteInfo = compilation is object ? new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, compilation.Assembly) : CompoundUseSiteInfo<AssemblySymbol>.DiscardedDependencies;

            for (TypeSymbol currType = implementingType; (object)currType != null; currType = currType.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteInfo))
            {
                // NOTE: In the case of PE symbols, it is possible to see an explicit implementation
                // on a type that does not declare the corresponding interface (or one of its
                // subinterfaces).  In such cases, we want to return the explicit implementation,
                // even if it doesn't participate in interface mapping according to the C# rules.

                // pass 1: check for explicit impls (can't assume name matches)
                MultiDictionary<Symbol, Symbol>.ValueSet explicitImpl = currType.GetExplicitImplementationForInterfaceMember(interfaceMember);
                if (explicitImpl.Count == 1)
                {
                    implementationInInterfacesMightChangeResult = false;
                    return explicitImpl.Single();
                }
                else if (explicitImpl.Count > 1)
                {
                    if ((object)currType == implementingType || implementingTypeImplementsInterface)
                    {
                        diagnostics.Add(ErrorCode.ERR_DuplicateExplicitImpl, implementingType.GetFirstLocation(), interfaceMember);
                    }

                    implementationInInterfacesMightChangeResult = false;
                    return null;
                }

                bool checkPendingExplicitImplementations = ((object)currType != implementingType || !currType.IsDefinition);

                if (checkPendingExplicitImplementations && interfaceMember is MethodSymbol interfaceMethod &&
                    currType.InterfacesAndTheirBaseInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteInfo).ContainsKey(interfaceType))
                {
                    // Check for implementations that are going to be explicit once types are emitted
                    MethodSymbol bodyOfSynthesizedMethodImpl = currType.GetBodyOfSynthesizedInterfaceMethodImpl(interfaceMethod);

                    if (bodyOfSynthesizedMethodImpl is object)
                    {
                        implementationInInterfacesMightChangeResult = false;
                        return bodyOfSynthesizedMethodImpl;
                    }
                }

                if (IsExplicitlyImplementedViaAccessors(checkPendingExplicitImplementations, interfaceMember, currType, ref useSiteInfo, out Symbol currTypeExplicitImpl))
                {
                    // We are looking for a property or event implementation and found an explicit implementation
                    // for its accessor(s) in this type. Stop the process and return event/property associated
                    // with the accessor(s), if any.
                    implementationInInterfacesMightChangeResult = false;
                    // NOTE: may be null.
                    return currTypeExplicitImpl;
                }

                if (!seenTypeDeclaringInterface ||
                    (!canBeImplementedImplicitlyInCSharp9 && (object)implementingBaseOpt == null))
                {
                    if (currType.InterfacesAndTheirBaseInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteInfo).ContainsKey(interfaceType))
                    {
                        if (!seenTypeDeclaringInterface)
                        {
                            implementingTypeIsFromSomeCompilation = currType.OriginalDefinition.ContainingModule is not PEModuleSymbol;
                            seenTypeDeclaringInterface = true;
                        }

                        if ((object)currType == implementingType)
                        {
                            implementingTypeImplementsInterface = true;
                        }
                        else if (!canBeImplementedImplicitlyInCSharp9 && (object)implementingBaseOpt == null)
                        {
                            implementingBaseOpt = currType;
                        }
                    }
                }

                // We want the implementation from the most derived type at or above the first one to
                // include the interface (or a subinterface) in its interface list
                if (seenTypeDeclaringInterface &&
                    (!interfaceMember.IsStatic || implementingTypeIsFromSomeCompilation))
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

            Debug.Assert(!canBeImplementedImplicitlyInCSharp9 || (object)implementingBaseOpt == null);

            bool tryDefaultInterfaceImplementation = true;

            // Dev10 has some extra restrictions and extra wiggle room when finding implicit
            // implementations for interface accessors.  Perform some extra checks and possibly
            // update the result (i.e. implicitImpl).
            if (interfaceMember.IsAccessor())
            {
                Symbol originalImplicitImpl = implicitImpl;
                CheckForImplementationOfCorrespondingPropertyOrEvent((MethodSymbol)interfaceMember, implementingType, implementingTypeIsFromSomeCompilation, ref implicitImpl);

                // If we discarded the candidate, we don't want default interface implementation to take over later, since runtime might still use the discarded candidate.
                if (originalImplicitImpl is object && implicitImpl is null)
                {
                    tryDefaultInterfaceImplementation = false;
                }
            }

            Symbol defaultImpl = null;

            if ((object)implicitImpl == null && seenTypeDeclaringInterface && tryDefaultInterfaceImplementation)
            {
                if (ignoreImplementationInInterfaces)
                {
                    implementationInInterfacesMightChangeResult = true;
                }
                else
                {
                    // Check for default interface implementations
                    defaultImpl = FindMostSpecificImplementationInInterfaces(interfaceMember, implementingType, ref useSiteInfo, diagnostics);
                    implementationInInterfacesMightChangeResult = false;
                }
            }
            else
            {
                implementationInInterfacesMightChangeResult = false;
            }

            diagnostics.Add(
#if !DEBUG
                // Don't optimize in DEBUG for better coverage for the GetInterfaceLocation function. 
                useSiteInfo.Diagnostics is null || !implementingTypeImplementsInterface ? Location.None :
#endif
                GetInterfaceLocation(interfaceMember, implementingType),
                useSiteInfo);

            if (defaultImpl is object)
            {
                if (implementingTypeImplementsInterface)
                {
                    ReportDefaultInterfaceImplementationMatchDiagnostics(interfaceMember, implementingType, defaultImpl, diagnostics);
                }

                return defaultImpl;
            }

            if (implementingTypeImplementsInterface)
            {
                if ((object)implicitImpl != null)
                {
                    bool suppressRegularValidation = false;

                    if (!canBeImplementedImplicitlyInCSharp9 && interfaceMember.Kind == SymbolKind.Method &&
                        (object)implementingBaseOpt == null)  // Otherwise any appropriate errors are going to be reported for the base.
                    {
                        var useSiteInfo2 = compilation is object ? new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, compilation.Assembly) : CompoundUseSiteInfo<AssemblySymbol>.DiscardedDependencies;

                        if (implementingType is NamedTypeSymbol named &&
                            !AccessCheck.IsSymbolAccessible(interfaceMember, named, ref useSiteInfo2, throughTypeOpt: null))
                        {
                            diagnostics.Add(ErrorCode.ERR_ImplicitImplementationOfInaccessibleInterfaceMember, GetImplicitImplementationDiagnosticLocation(interfaceMember, implementingType, implicitImpl), implementingType, interfaceMember, implicitImpl);
                            suppressRegularValidation = true;
                        }
                        else if (!interfaceMember.IsStatic)
                        {
                            LanguageVersion requiredVersion = MessageID.IDS_FeatureImplicitImplementationOfNonPublicMembers.RequiredVersion();
                            LanguageVersion? availableVersion = implementingType.DeclaringCompilation?.LanguageVersion;
                            if (requiredVersion > availableVersion)
                            {
                                diagnostics.Add(ErrorCode.ERR_ImplicitImplementationOfNonPublicInterfaceMember, GetImplicitImplementationDiagnosticLocation(interfaceMember, implementingType, implicitImpl),
                                                implementingType, interfaceMember, implicitImpl,
                                                availableVersion.GetValueOrDefault().ToDisplayString(), new CSharpRequiredLanguageVersion(requiredVersion));
                            }
                        }

                        diagnostics.Add(
#if !DEBUG
                            // Don't optimize in DEBUG for better coverage for the GetInterfaceLocation function. 
                            useSiteInfo2.Diagnostics is null ? Location.None :
#endif
                            GetImplicitImplementationDiagnosticLocation(interfaceMember, implementingType, implicitImpl),
                            useSiteInfo2);
                    }

                    if (!suppressRegularValidation)
                    {
                        ReportImplicitImplementationMatchDiagnostics(interfaceMember, implementingType, implicitImpl, diagnostics);
                    }
                }
                else if ((object)closestMismatch != null)
                {
                    ReportImplicitImplementationMismatchDiagnostics(interfaceMember, implementingType, closestMismatch, diagnostics);
                }
            }

            return implicitImpl;
        }

        private static Symbol FindMostSpecificImplementationInInterfaces(Symbol interfaceMember, TypeSymbol implementingType,
                                                                         ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
                                                                         BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(!implementingType.IsInterfaceType());

            // If we are dealing with a property or event and an implementation of at least one accessor is not from an interface, it 
            // wouldn't be right to say that the event/property is implemented in an interface because its accessor isn't.
            (MethodSymbol interfaceAccessor1, MethodSymbol interfaceAccessor2) = GetImplementableAccessors(interfaceMember);

            if (stopLookup(interfaceAccessor1, implementingType) || stopLookup(interfaceAccessor2, implementingType))
            {
                return null;
            }

            Symbol defaultImpl = FindMostSpecificImplementationInBases(interfaceMember,
                                                                       implementingType,
                                                                       ref useSiteInfo, out Symbol conflict1, out Symbol conflict2);

            if ((object)conflict1 != null)
            {
                Debug.Assert((object)defaultImpl == null);
                Debug.Assert((object)conflict2 != null);
                diagnostics.Add(ErrorCode.ERR_MostSpecificImplementationIsNotFound, GetInterfaceLocation(interfaceMember, implementingType),
                                interfaceMember, conflict1, conflict2);
            }
            else
            {
                Debug.Assert(((object)conflict2 == null));
            }

            return defaultImpl;

            static bool stopLookup(MethodSymbol interfaceAccessor, TypeSymbol implementingType)
            {
                if (interfaceAccessor is null)
                {
                    return false;
                }

                SymbolAndDiagnostics symbolAndDiagnostics = implementingType.FindImplementationForInterfaceMemberInNonInterfaceWithDiagnostics(interfaceAccessor);

                if (symbolAndDiagnostics.Symbol is object)
                {
                    return !symbolAndDiagnostics.Symbol.ContainingType.IsInterface;
                }

                // It is still possible that we actually looked for the accessor in interfaces, but failed due to an ambiguity.
                // Let's try to look for a property to improve diagnostics in this scenario.
                return !symbolAndDiagnostics.Diagnostics.Diagnostics.Any(static d => d.Code == (int)ErrorCode.ERR_MostSpecificImplementationIsNotFound);
            }
        }

        private static Symbol FindMostSpecificImplementation(Symbol interfaceMember, NamedTypeSymbol implementingInterface, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            MultiDictionary<Symbol, Symbol>.ValueSet implementingMember = FindImplementationInInterface(interfaceMember, implementingInterface);

            switch (implementingMember.Count)
            {
                case 0:
                    (MethodSymbol interfaceAccessor1, MethodSymbol interfaceAccessor2) = GetImplementableAccessors(interfaceMember);

                    // If interface actually implements an event or property accessor, but doesn't implement the event/property,
                    // do not look for its implementation in bases.
                    if ((interfaceAccessor1 is object && FindImplementationInInterface(interfaceAccessor1, implementingInterface).Count != 0) ||
                        (interfaceAccessor2 is object && FindImplementationInInterface(interfaceAccessor2, implementingInterface).Count != 0))
                    {
                        return null;
                    }

                    return FindMostSpecificImplementationInBases(interfaceMember, implementingInterface,
                                                                 ref useSiteInfo,
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
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
            out Symbol conflictingImplementation1,
            out Symbol conflictingImplementation2)
        {
            ImmutableArray<NamedTypeSymbol> allInterfaces = implementingType.AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteInfo);

            if (allInterfaces.IsEmpty)
            {
                conflictingImplementation1 = null;
                conflictingImplementation2 = null;
                return null;
            }

            // Properties or events can be implemented in an unconventional manner, i.e. implementing accessors might not be tied to a property/event.
            // If we simply look for a more specific implementing property/event, we might find one with not most specific implementing accessors.
            // Returning a property/event like that would be incorrect because runtime will use most specific accessor, or it will fail because there will
            // be an ambiguity for the accessor implementation.
            // So, for events and properties we look for most specific implementation of corresponding accessors and then try to tie them back to
            // an event/property, if any.
            (MethodSymbol interfaceAccessor1, MethodSymbol interfaceAccessor2) = GetImplementableAccessors(interfaceMember);

            if (interfaceAccessor1 is null && interfaceAccessor2 is null)
            {
                return findMostSpecificImplementationInBases(interfaceMember, allInterfaces, ref useSiteInfo, out conflictingImplementation1, out conflictingImplementation2);
            }

            Symbol accessorImpl1 = findMostSpecificImplementationInBases(interfaceAccessor1 ?? interfaceAccessor2, allInterfaces, ref useSiteInfo,
                                                                         out Symbol conflictingAccessorImplementation11, out Symbol conflictingAccessorImplementation12);

            if (accessorImpl1 is null && conflictingAccessorImplementation11 is null) // implementation of accessor is not found
            {
                conflictingImplementation1 = null;
                conflictingImplementation2 = null;
                return null;
            }

            if (interfaceAccessor1 is null || interfaceAccessor2 is null)
            {
                if (accessorImpl1 is object)
                {
                    conflictingImplementation1 = null;
                    conflictingImplementation2 = null;
                    return findImplementationInInterface(interfaceMember, accessorImpl1);
                }

                conflictingImplementation1 = findImplementationInInterface(interfaceMember, conflictingAccessorImplementation11);
                conflictingImplementation2 = findImplementationInInterface(interfaceMember, conflictingAccessorImplementation12);

                if ((conflictingImplementation1 is null) != (conflictingImplementation2 is null))
                {
                    conflictingImplementation1 = null;
                    conflictingImplementation2 = null;
                }

                return null;
            }

            Symbol accessorImpl2 = findMostSpecificImplementationInBases(interfaceAccessor2, allInterfaces, ref useSiteInfo,
                                                                         out Symbol conflictingAccessorImplementation21, out Symbol conflictingAccessorImplementation22);

            if ((accessorImpl2 is null && conflictingAccessorImplementation21 is null) || // implementation of accessor is not found
                (accessorImpl1 is null) != (accessorImpl2 is null)) // there is most specific implementation for one accessor and an ambiguous implementation for the other accessor. 
            {
                conflictingImplementation1 = null;
                conflictingImplementation2 = null;
                return null;
            }

            if (accessorImpl1 is object)
            {
                conflictingImplementation1 = null;
                conflictingImplementation2 = null;
                return findImplementationInInterface(interfaceMember, accessorImpl1, accessorImpl2);
            }

            conflictingImplementation1 = findImplementationInInterface(interfaceMember, conflictingAccessorImplementation11, conflictingAccessorImplementation21);
            conflictingImplementation2 = findImplementationInInterface(interfaceMember, conflictingAccessorImplementation12, conflictingAccessorImplementation22);

            if ((conflictingImplementation1 is null) != (conflictingImplementation2 is null))
            {
                // One pair of conflicting accessors can be tied to an event/property, but the other cannot be tied to an event/property.
                // Dropping conflict information since it only affects diagnostic.
                conflictingImplementation1 = null;
                conflictingImplementation2 = null;
            }

            return null;

            static Symbol findImplementationInInterface(Symbol interfaceMember, Symbol inplementingAccessor1, Symbol implementingAccessor2 = null)
            {
                NamedTypeSymbol implementingInterface = inplementingAccessor1.ContainingType;

                if (implementingAccessor2 is object && !implementingInterface.Equals(implementingAccessor2.ContainingType, TypeCompareKind.ConsiderEverything))
                {
                    // Implementing accessors are from different types, they cannot be tied to the same event/property.
                    return null;
                }

                MultiDictionary<Symbol, Symbol>.ValueSet implementingMember = FindImplementationInInterface(interfaceMember, implementingInterface);

                switch (implementingMember.Count)
                {
                    case 1:
                        return implementingMember.Single();
                    default:
                        return null;
                }
            }

            static Symbol findMostSpecificImplementationInBases(
                Symbol interfaceMember,
                ImmutableArray<NamedTypeSymbol> allInterfaces,
                ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo,
                out Symbol conflictingImplementation1,
                out Symbol conflictingImplementation2)
            {
                var implementations = ArrayBuilder<(MultiDictionary<Symbol, Symbol>.ValueSet MethodSet, MultiDictionary<NamedTypeSymbol, NamedTypeSymbol> Bases)>.GetInstance();

                foreach (var interfaceType in allInterfaces)
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
                            bases = previousContainingType.InterfacesAndTheirBaseInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteInfo);
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
                        MultiDictionary<NamedTypeSymbol, NamedTypeSymbol> bases = interfaceType.InterfacesAndTheirBaseInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteInfo);

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

        private static (MethodSymbol interfaceAccessor1, MethodSymbol interfaceAccessor2) GetImplementableAccessors(Symbol interfaceMember)
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
                        interfaceAccessor1 = null;
                        interfaceAccessor2 = null;
                        break;
                    }
            }

            if (!interfaceAccessor1.IsImplementable())
            {
                interfaceAccessor1 = null;
            }

            if (!interfaceAccessor2.IsImplementable())
            {
                interfaceAccessor2 = null;
            }

            return (interfaceAccessor1, interfaceAccessor2);
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
        /// for the interface event, it found the protected event and reported an appropriate diagnostic.  What it should have done
        /// (and does do now) is recognize that no event associated with the accessors explicitly implementing the interface accessors
        /// and returned null.
        /// 
        /// We resolved this issue by introducing a new step into the interface mapping algorithm: after failing to find an explicit
        /// implementation in a type, but before searching for an implicit implementation in that type, check for an explicit implementation
        /// of an associated accessor.  If there is such an implementation, then immediately return the associated property or event,
        /// even if it is null.  That is, never attempt to find an implicit implementation for an interface property or event with an
        /// explicitly implemented accessor.
        /// </summary>
        private static bool IsExplicitlyImplementedViaAccessors(bool checkPendingExplicitImplementations, Symbol interfaceMember, TypeSymbol currType, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo, out Symbol implementingMember)
        {
            (MethodSymbol interfaceAccessor1, MethodSymbol interfaceAccessor2) = GetImplementableAccessors(interfaceMember);

            Symbol associated1;
            Symbol associated2;

            if (TryGetExplicitImplementationAssociatedPropertyOrEvent(checkPendingExplicitImplementations, interfaceAccessor1, currType, ref useSiteInfo, out associated1) |  // NB: not ||
                TryGetExplicitImplementationAssociatedPropertyOrEvent(checkPendingExplicitImplementations, interfaceAccessor2, currType, ref useSiteInfo, out associated2))
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
                    if ((object)implementingMember != null && implementingMember.OriginalDefinition.ContainingModule is not PEModuleSymbol && implementingMember.IsExplicitInterfaceImplementation())
                    {
                        implementingMember = null;
                    }
                }
                else
                {
                    implementingMember = null;
                }

                return true;
            }

            implementingMember = null;
            return false;
        }

        private static bool TryGetExplicitImplementationAssociatedPropertyOrEvent(bool checkPendingExplicitImplementations, MethodSymbol interfaceAccessor, TypeSymbol currType, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo, out Symbol associated)
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

                if (checkPendingExplicitImplementations &&
                    currType.InterfacesAndTheirBaseInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteInfo).ContainsKey(interfaceAccessor.ContainingType))
                {
                    // Check for implementations that are going to be explicit once types are emitted
                    MethodSymbol bodyOfSynthesizedMethodImpl = currType.GetBodyOfSynthesizedInterfaceMethodImpl(interfaceAccessor);

                    if (bodyOfSynthesizedMethodImpl is object)
                    {
                        associated = bodyOfSynthesizedMethodImpl.AssociatedSymbol;
                        return true;
                    }
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
                                                                                 ref Symbol implicitImpl)
        {
            Debug.Assert(!implementingType.IsInterfaceType());
            Debug.Assert(interfaceMethod.IsAccessor());

            Symbol associatedInterfacePropertyOrEvent = interfaceMethod.AssociatedSymbol;

            // Do not make any adjustments based on presence of default interface implementation for the property or event.
            // We don't want an addition of default interface implementation to change an error situation to success for
            // scenarios where the default interface implementation wouldn't actually be used at runtime.
            // When we find an implicit implementation candidate, we don't want to not discard it if we would discard it when
            // default interface implementation was missing. Why would presence of default interface implementation suddenly
            // make the candidate suiatable to implement the interface? Also, if we discard the candidate, we don't want default interface 
            // implementation to take over later, since runtime might still use the discarded candidate.
            // When we don't find any implicit implementation candidate, returning accessor of default interface implementation
            // doesn't actually help much because we would find it anyway (it is implemented explicitly).
            Symbol implementingPropertyOrEvent = implementingType.FindImplementationForInterfaceMemberInNonInterface(associatedInterfacePropertyOrEvent,
                                                                                                                     ignoreImplementationInInterfacesIfResultIsNotReady: true); // NB: uses cache

            MethodSymbol correspondingImplementingAccessor = null;
            if ((object)implementingPropertyOrEvent != null && !implementingPropertyOrEvent.ContainingType.IsInterface)
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
                    interfaceMethod.IsInitOnly,
                    interfaceMethod.IsStatic,
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

        private static void ReportDefaultInterfaceImplementationMatchDiagnostics(Symbol interfaceMember, TypeSymbol implementingType, Symbol implicitImpl, BindingDiagnosticBag diagnostics)
        {
            if (interfaceMember.Kind == SymbolKind.Method)
            {
                bool isStatic = implicitImpl.IsStatic;

                if (!isStatic && implementingType.IsRefLikeType)
                {
                    diagnostics.Add(ErrorCode.ERR_RefStructDoesNotSupportDefaultInterfaceImplementationForMember,
                                    GetInterfaceLocation(interfaceMember, implementingType),
                                    implicitImpl, interfaceMember, implementingType);
                }
                else if (implementingType.ContainingModule != implicitImpl.ContainingModule)
                {
                    // The default implementation is coming from a different module, which means that we probably didn't check
                    // for the required runtime capability or language version
                    var feature = isStatic ? MessageID.IDS_FeatureStaticAbstractMembersInInterfaces : MessageID.IDS_DefaultInterfaceImplementation;

                    LanguageVersion requiredVersion = feature.RequiredVersion();
                    LanguageVersion? availableVersion = implementingType.DeclaringCompilation?.LanguageVersion;
                    if (requiredVersion > availableVersion)
                    {
                        diagnostics.Add(ErrorCode.ERR_LanguageVersionDoesNotSupportInterfaceImplementationForMember,
                                        GetInterfaceLocation(interfaceMember, implementingType),
                                        implicitImpl, interfaceMember, implementingType,
                                        feature.Localize(),
                                        availableVersion.GetValueOrDefault().ToDisplayString(),
                                        new CSharpRequiredLanguageVersion(requiredVersion));
                    }

                    if (!(isStatic ?
                              implementingType.ContainingAssembly.RuntimeSupportsStaticAbstractMembersInInterfaces :
                              implementingType.ContainingAssembly.RuntimeSupportsDefaultInterfaceImplementation))
                    {
                        diagnostics.Add(isStatic ?
                                            ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfacesForMember :
                                            ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember,
                                        GetInterfaceLocation(interfaceMember, implementingType),
                                        implicitImpl, interfaceMember, implementingType);
                    }
                }
            }
        }

        /// <summary>
        /// These diagnostics are for members that do implicitly implement an interface member, but do so
        /// in an undesirable way.
        /// </summary>
        private static void ReportImplicitImplementationMatchDiagnostics(Symbol interfaceMember, TypeSymbol implementingType, Symbol implicitImpl, BindingDiagnosticBag diagnostics)
        {
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
                    else if (implicitImplMethod.IsStatic && implicitImplMethod.MethodKind == MethodKind.Ordinary && implicitImplMethod.GetUnmanagedCallersOnlyAttributeData(forceComplete: true) is not null)
                    {
                        diagnostics.Add(ErrorCode.ERR_InterfaceImplementedByUnmanagedCallersOnlyMethod, GetImplicitImplementationDiagnosticLocation(interfaceMember, implementingType, implicitImpl), implicitImpl, interfaceMethod, implementingType);
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
                CheckModifierMismatchOnImplementingMember(implementingType, implicitImpl, interfaceMember, isExplicit: false, diagnostics);
            }

            // In constructed types, it is possible to see multiple members with the same (runtime) signature.
            // Now that we know which member will implement the interface member, confirm that it is the only
            // such member.
            if (!implicitImpl.ContainingType.IsDefinition)
            {
                foreach (Symbol member in implicitImpl.ContainingType.GetMembers(implicitImpl.Name))
                {
                    if (member.DeclaredAccessibility != Accessibility.Public || member == implicitImpl)
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

            if (implicitImpl.IsStatic && interfaceMember.ContainingModule != implementingType.ContainingModule)
            {
                LanguageVersion requiredVersion = MessageID.IDS_FeatureStaticAbstractMembersInInterfaces.RequiredVersion();
                LanguageVersion? availableVersion = implementingType.DeclaringCompilation?.LanguageVersion;
                if (requiredVersion > availableVersion)
                {
                    diagnostics.Add(ErrorCode.ERR_LanguageVersionDoesNotSupportInterfaceImplementationForMember,
                                    GetImplicitImplementationDiagnosticLocation(interfaceMember, implementingType, implicitImpl),
                                    implicitImpl, interfaceMember, implementingType,
                                    MessageID.IDS_FeatureStaticAbstractMembersInInterfaces.Localize(),
                                    availableVersion.GetValueOrDefault().ToDisplayString(),
                                    new CSharpRequiredLanguageVersion(requiredVersion));
                }

                if (!implementingType.ContainingAssembly.RuntimeSupportsStaticAbstractMembersInInterfaces)
                {
                    diagnostics.Add(ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfacesForMember,
                                    GetImplicitImplementationDiagnosticLocation(interfaceMember, implementingType, implicitImpl),
                                    implicitImpl, interfaceMember, implementingType);
                }
            }
        }

        /// <summary>
        /// Reports warnings for some mismatches in parameter or return type modifiers (nullability, scoped, refness) between implementing and implemented member.
        /// </summary>
        internal static void CheckModifierMismatchOnImplementingMember(TypeSymbol implementingType, Symbol implementingMember, Symbol interfaceMember, bool isExplicit, BindingDiagnosticBag diagnostics)
        {
            if (!implementingMember.IsImplicitlyDeclared && !implementingMember.IsAccessor())
            {
                if (interfaceMember.Kind == SymbolKind.Event)
                {
                    CSharpCompilation compilation = implementingType.DeclaringCompilation;
                    var implementingEvent = (EventSymbol)implementingMember;
                    var implementedEvent = (EventSymbol)interfaceMember;
                    SourceMemberContainerTypeSymbol.CheckValidNullableEventOverride(compilation, implementedEvent, implementingEvent,
                                                                                    diagnostics,
                                                                                    reportMismatch: (diagnostics, implementedEvent, implementingEvent, arg) =>
                                                                                    {
                                                                                        if (arg.isExplicit)
                                                                                        {
                                                                                            diagnostics.Add(ErrorCode.WRN_NullabilityMismatchInTypeOnExplicitImplementation,
                                                                                                            implementingEvent.GetFirstLocation(), new FormattedSymbol(implementedEvent, SymbolDisplayFormat.MinimallyQualifiedFormat));
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            diagnostics.Add(ErrorCode.WRN_NullabilityMismatchInTypeOnImplicitImplementation,
                                                                                                            GetImplicitImplementationDiagnosticLocation(implementedEvent, arg.implementingType, implementingEvent),
                                                                                                            new FormattedSymbol(implementingEvent, SymbolDisplayFormat.MinimallyQualifiedFormat),
                                                                                                            new FormattedSymbol(implementedEvent, SymbolDisplayFormat.MinimallyQualifiedFormat));
                                                                                        }
                                                                                    },
                                                                                    extraArgument: (implementingType, isExplicit));
                }
                else
                {
                    static void checkMethodOverride(
                        TypeSymbol implementingType,
                        MethodSymbol implementedMethod,
                        MethodSymbol implementingMethod,
                        bool isExplicit,
                        BindingDiagnosticBag diagnostics)
                    {
                        ReportMismatchInReturnType<(TypeSymbol implementingType, bool isExplicit)> reportMismatchInReturnType =
                            static (diagnostics, implementedMethod, implementingMethod, topLevel, arg) =>
                            {
                                if (arg.isExplicit)
                                {
                                    // We use ConstructedFrom symbols here and below to not leak methods with Ignored annotations in type arguments
                                    // into diagnostics
                                    diagnostics.Add(topLevel ?
                                                        ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnExplicitImplementation :
                                                        ErrorCode.WRN_NullabilityMismatchInReturnTypeOnExplicitImplementation,
                                                    implementingMethod.GetFirstLocation(), new FormattedSymbol(implementedMethod.ConstructedFrom, SymbolDisplayFormat.MinimallyQualifiedFormat));
                                }
                                else
                                {
                                    diagnostics.Add(topLevel ?
                                                        ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnImplicitImplementation :
                                                        ErrorCode.WRN_NullabilityMismatchInReturnTypeOnImplicitImplementation,
                                                    GetImplicitImplementationDiagnosticLocation(implementedMethod, arg.implementingType, implementingMethod),
                                                    new FormattedSymbol(implementingMethod, SymbolDisplayFormat.MinimallyQualifiedFormat),
                                                    new FormattedSymbol(implementedMethod.ConstructedFrom, SymbolDisplayFormat.MinimallyQualifiedFormat));
                                }
                            };

                        ReportMismatchInParameterType<(TypeSymbol implementingType, bool isExplicit)> reportMismatchInParameterType =
                            static (diagnostics, implementedMethod, implementingMethod, implementingParameter, topLevel, arg) =>
                            {
                                if (arg.isExplicit)
                                {
                                    diagnostics.Add(topLevel ?
                                                        ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnExplicitImplementation :
                                                        ErrorCode.WRN_NullabilityMismatchInParameterTypeOnExplicitImplementation,
                                                    implementingMethod.GetFirstLocation(),
                                                    new FormattedSymbol(implementingParameter, SymbolDisplayFormat.ShortFormat),
                                                    new FormattedSymbol(implementedMethod.ConstructedFrom, SymbolDisplayFormat.MinimallyQualifiedFormat));
                                }
                                else
                                {
                                    diagnostics.Add(topLevel ?
                                                        ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnImplicitImplementation :
                                                        ErrorCode.WRN_NullabilityMismatchInParameterTypeOnImplicitImplementation,
                                                    GetImplicitImplementationDiagnosticLocation(implementedMethod, arg.implementingType, implementingMethod),
                                                    new FormattedSymbol(implementingParameter, SymbolDisplayFormat.ShortFormat),
                                                    new FormattedSymbol(implementingMethod, SymbolDisplayFormat.MinimallyQualifiedFormat),
                                                    new FormattedSymbol(implementedMethod.ConstructedFrom, SymbolDisplayFormat.MinimallyQualifiedFormat));
                                }
                            };

                        CSharpCompilation compilation = implementingType.DeclaringCompilation;
                        SourceMemberContainerTypeSymbol.CheckValidNullableMethodOverride(
                            compilation,
                            implementedMethod,
                            implementingMethod,
                            diagnostics,
                            reportMismatchInReturnType: reportMismatchInReturnType,
                            reportMismatchInParameterType: reportMismatchInParameterType,
                            extraArgument: (implementingType, isExplicit));

                        if (SourceMemberContainerTypeSymbol.RequiresValidScopedOverrideForRefSafety(implementedMethod))
                        {
                            SourceMemberContainerTypeSymbol.CheckValidScopedOverride(
                                implementedMethod,
                                implementingMethod,
                                diagnostics,
                                reportMismatchInParameterType: static (diagnostics, implementedMethod, implementingMethod, implementingParameter, _, arg) =>
                                    {
                                        diagnostics.Add(
                                            SourceMemberContainerTypeSymbol.ReportInvalidScopedOverrideAsError(implementedMethod, implementingMethod) ?
                                                ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation :
                                                ErrorCode.WRN_ScopedMismatchInParameterOfOverrideOrImplementation,
                                            GetImplicitImplementationDiagnosticLocation(implementedMethod, arg.implementingType, implementingMethod),
                                            new FormattedSymbol(implementingParameter, SymbolDisplayFormat.ShortFormat));
                                    },
                                extraArgument: (implementingType, isExplicit),
                                allowVariance: true,
                                invokedAsExtensionMethod: false);
                        }
                        SourceMemberContainerTypeSymbol.CheckRefReadonlyInMismatch(
                            implementedMethod, implementingMethod, diagnostics,
                            static (diagnostics, implementedMethod, implementingMethod, implementingParameter, _, arg) =>
                            {
                                var (implementedParameter, implementingType) = arg;
                                var location = GetImplicitImplementationDiagnosticLocation(implementedMethod, implementingType, implementingMethod);
                                // Reference kind modifier of parameter '{0}' doesn't match the corresponding parameter '{1}' in overridden or implemented member.
                                diagnostics.Add(ErrorCode.WRN_OverridingDifferentRefness, location, implementingParameter, implementedParameter);
                            },
                            implementingType,
                            invokedAsExtensionMethod: false);

                        if (implementingMethod.HasUnscopedRefAttributeOnMethodOrProperty())
                        {
                            if (implementedMethod.HasUnscopedRefAttributeOnMethodOrProperty())
                            {
                                if (!implementingMethod.IsExplicitInterfaceImplementation && implementingMethod is SourceMethodSymbolWithAttributes &&
                                    implementedMethod.ContainingModule != implementingMethod.ContainingModule)
                                {
                                    checkRefStructInterfacesFeatureAvailabilityOnUnscopedRefAttribute(implementingMethod.HasUnscopedRefAttribute ? implementingMethod : implementingMethod.AssociatedSymbol, diagnostics);
                                }
                            }
                            else
                            {
                                diagnostics.Add(
                                    ErrorCode.ERR_UnscopedRefAttributeInterfaceImplementation,
                                    GetImplicitImplementationDiagnosticLocation(implementedMethod, implementingType, implementingMethod),
                                    implementedMethod);
                            }
                        }
                    }

                    switch (interfaceMember.Kind)
                    {
                        case SymbolKind.Property:
                            var implementingProperty = (PropertySymbol)implementingMember;
                            var implementedProperty = (PropertySymbol)interfaceMember;
                            var implementingGetMethod = implementedProperty.GetMethod.IsImplementable() ?
                                implementingProperty.GetOwnOrInheritedGetMethod() :
                                null;
                            var implementingSetMethod = implementedProperty.SetMethod.IsImplementable() ?
                                implementingProperty.GetOwnOrInheritedSetMethod() :
                                null;
                            if (implementingGetMethod is { })
                            {
                                checkMethodOverride(
                                    implementingType,
                                    implementedProperty.GetMethod,
                                    implementingGetMethod,
                                    isExplicit: isExplicit,
                                    diagnostics);
                            }

                            if (implementingSetMethod is { })
                            {
                                checkMethodOverride(
                                    implementingType,
                                    implementedProperty.SetMethod,
                                    implementingSetMethod,
                                    isExplicit: isExplicit,
                                    diagnostics);
                            }
                            break;
                        case SymbolKind.Method:
                            var implementingMethod = (MethodSymbol)implementingMember;
                            var implementedMethod = (MethodSymbol)interfaceMember;

                            if (implementedMethod.IsGenericMethod)
                            {
                                implementedMethod = implementedMethod.Construct(TypeMap.TypeParametersAsTypeSymbolsWithIgnoredAnnotations(implementingMethod.TypeParameters));
                            }

                            checkMethodOverride(
                                implementingType,
                                implementedMethod,
                                implementingMethod,
                                isExplicit: isExplicit,
                                diagnostics);
                            break;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(interfaceMember.Kind);
                    }
                }
            }

            static void checkRefStructInterfacesFeatureAvailabilityOnUnscopedRefAttribute(Symbol implementingSymbol, BindingDiagnosticBag diagnostics)
            {
                foreach (var attributeData in implementingSymbol.GetAttributes())
                {
                    if (attributeData is SourceAttributeData { ApplicationSyntaxReference: { } applicationSyntaxReference } &&
                        attributeData.IsTargetAttribute(AttributeDescription.UnscopedRefAttribute))
                    {
                        MessageID.IDS_FeatureRefStructInterfaces.CheckFeatureAvailability(diagnostics, implementingSymbol.DeclaringCompilation, applicationSyntaxReference.GetLocation());
                        return;
                    }
                }

                Debug.Assert(false);
            }
        }

        /// <summary>
        /// These diagnostics are for members that almost, but not actually, implicitly implement an interface member.
        /// </summary>
        private static void ReportImplicitImplementationMismatchDiagnostics(Symbol interfaceMember, TypeSymbol implementingType, Symbol closestMismatch, BindingDiagnosticBag diagnostics)
        {
            // Determine  a better location for diagnostic squiggles.  Squiggle the interface rather than the class.
            Location interfaceLocation = GetInterfaceLocation(interfaceMember, implementingType);

            if (closestMismatch.IsStatic != interfaceMember.IsStatic)
            {
                diagnostics.Add(closestMismatch.IsStatic ? ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic : ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotStatic,
                                interfaceLocation, implementingType, interfaceMember, closestMismatch);
            }
            else if (closestMismatch.DeclaredAccessibility != Accessibility.Public)
            {
                ErrorCode errorCode = interfaceMember.IsAccessor() ? ErrorCode.ERR_UnimplementedInterfaceAccessor : ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic;
                diagnostics.Add(errorCode, interfaceLocation, implementingType, interfaceMember, closestMismatch);
            }
            else if (HaveInitOnlyMismatch(interfaceMember, closestMismatch))
            {
                diagnostics.Add(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, interfaceLocation, implementingType, interfaceMember, closestMismatch);
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
                    (useSiteDiagnostic = interfaceMemberReturnType.GetUseSiteInfo().DiagnosticInfo) != null &&
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

        internal static bool HaveInitOnlyMismatch(Symbol one, Symbol other)
        {
            if (!(one is MethodSymbol oneMethod))
            {
                return false;
            }

            if (!(other is MethodSymbol otherMethod))
            {
                return false;
            }

            return oneMethod.IsInitOnly != otherMethod.IsInitOnly;
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

            return snt?.GetImplementsLocation(@interface) ?? implementingType.GetFirstLocationOrNone();
        }

        private static bool ReportAnyMismatchedConstraints(MethodSymbol interfaceMethod, TypeSymbol implementingType, MethodSymbol implicitImpl, BindingDiagnosticBag diagnostics)
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
                return member.GetFirstLocation();
            }
            else
            {
                var @interface = interfaceMember.ContainingType;
                SourceMemberContainerTypeSymbol snt = implementingType as SourceMemberContainerTypeSymbol;
                return snt?.GetImplementsLocation(@interface) ?? implementingType.GetFirstLocation();
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

            bool? isOperator = null;

            if (interfaceMember is MethodSymbol interfaceMethod)
            {
                isOperator = interfaceMethod.MethodKind is MethodKind.UserDefinedOperator or MethodKind.Conversion;
            }

            foreach (Symbol member in currType.GetMembers(interfaceMember.Name))
            {
                if (member.Kind == interfaceMember.Kind)
                {
                    if (isOperator.HasValue &&
                        (((MethodSymbol)member).MethodKind is MethodKind.UserDefinedOperator or MethodKind.Conversion) != isOperator.GetValueOrDefault())
                    {
                        continue;
                    }

                    if (IsInterfaceMemberImplementation(member, interfaceMember, implementingTypeIsFromSomeCompilation))
                    {
                        implicitImpl = member;
                        return;
                    }

                    // If we haven't found a match, do a weaker comparison that ignores static-ness, accessibility, and return type.
                    else if ((object)closeMismatch == null && implementingTypeIsFromSomeCompilation)
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
            if (candidateMember.DeclaredAccessibility != Accessibility.Public || candidateMember.IsStatic != interfaceMember.IsStatic)
            {
                return false;
            }
            else if (HaveInitOnlyMismatch(candidateMember, interfaceMember))
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

#nullable enable
        /// <summary>
        /// If implementation of an interface method <paramref name="interfaceMethod"/> will be accompanied with 
        /// a MethodImpl entry in metadata, information about which isn't already exposed through
        /// <see cref="MethodSymbol.ExplicitInterfaceImplementations"/> API, this method returns the "Body" part
        /// of the MethodImpl entry, i.e. the method that implements the <paramref name="interfaceMethod"/>.
        /// Some of the MethodImpl entries could require synthetic forwarding methods. In such cases,
        /// the result is the method that the language considers to implement the <paramref name="interfaceMethod"/>,
        /// rather than the forwarding method. In other words, it is the method that the forwarding method forwards to.
        /// </summary>
        /// <param name="interfaceMethod">The interface method that is going to be implemented by using synthesized MethodImpl entry.</param>
        /// <returns></returns>
        protected MethodSymbol? GetBodyOfSynthesizedInterfaceMethodImpl(MethodSymbol interfaceMethod)
        {
            var info = this.GetInterfaceInfo();
            if (info == s_noInterfaces)
            {
                return null;
            }

            if (info.synthesizedMethodImplMap == null)
            {
                Interlocked.CompareExchange(ref info.synthesizedMethodImplMap, makeSynthesizedMethodImplMap(), null);
            }

            if (info.synthesizedMethodImplMap.TryGetValue(interfaceMethod, out MethodSymbol? result))
            {
                return result;
            }

            return null;

            ImmutableDictionary<MethodSymbol, MethodSymbol> makeSynthesizedMethodImplMap()
            {
                var map = ImmutableDictionary.CreateBuilder<MethodSymbol, MethodSymbol>(ExplicitInterfaceImplementationTargetMemberEqualityComparer.Instance);
                foreach ((MethodSymbol body, MethodSymbol implemented) in this.SynthesizedInterfaceMethodImpls())
                {
                    map.Add(implemented, body);
                }

                return map.ToImmutable();
            }
        }

        /// <summary>
        /// Returns information about interface method implementations that will be accompanied with 
        /// MethodImpl entries in metadata, information about which isn't already exposed through
        /// <see cref="MethodSymbol.ExplicitInterfaceImplementations"/> API. The "Body" is the method that
        /// implements the interface method "Implemented". 
        /// Some of the MethodImpl entries could require synthetic forwarding methods. In such cases,
        /// the "Body" is the method that the language considers to implement the interface method,
        /// the "Implemented", rather than the forwarding method. In other words, it is the method that 
        /// the forwarding method forwards to.
        /// </summary>
        internal abstract IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls();
#nullable disable

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
            throw ExceptionUtilities.Unreachable();
        }

#nullable enable
        public static bool Equals(TypeSymbol? left, TypeSymbol? right, TypeCompareKind comparison)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right, comparison);
        }
#nullable disable

        [Obsolete("Use 'TypeSymbol.Equals(TypeSymbol, TypeSymbol, TypeCompareKind)' method.", true)]
        public static bool operator ==(TypeSymbol left, TypeSymbol right)
            => throw ExceptionUtilities.Unreachable();

        [Obsolete("Use 'TypeSymbol.Equals(TypeSymbol, TypeSymbol, TypeCompareKind)' method.", true)]
        public static bool operator !=(TypeSymbol left, TypeSymbol right)
            => throw ExceptionUtilities.Unreachable();

        [Obsolete("Use 'TypeSymbol.Equals(TypeSymbol, TypeSymbol, TypeCompareKind)' method.", true)]
        public static bool operator ==(Symbol left, TypeSymbol right)
            => throw ExceptionUtilities.Unreachable();

        [Obsolete("Use 'TypeSymbol.Equals(TypeSymbol, TypeSymbol, TypeCompareKind)' method.", true)]
        public static bool operator !=(Symbol left, TypeSymbol right)
            => throw ExceptionUtilities.Unreachable();

        [Obsolete("Use 'TypeSymbol.Equals(TypeSymbol, TypeSymbol, TypeCompareKind)' method.", true)]
        public static bool operator ==(TypeSymbol left, Symbol right)
            => throw ExceptionUtilities.Unreachable();

        [Obsolete("Use 'TypeSymbol.Equals(TypeSymbol, TypeSymbol, TypeCompareKind)' method.", true)]
        public static bool operator !=(TypeSymbol left, Symbol right)
            => throw ExceptionUtilities.Unreachable();

        internal ITypeSymbol GetITypeSymbol(CodeAnalysis.NullableAnnotation nullableAnnotation)
        {
            if (nullableAnnotation == DefaultNullableAnnotation)
            {
                return (ITypeSymbol)this.ISymbol;
            }

            return CreateITypeSymbol(nullableAnnotation);
        }

        internal CodeAnalysis.NullableAnnotation DefaultNullableAnnotation => NullableAnnotationExtensions.ToPublicAnnotation(this, NullableAnnotation.Oblivious);

        protected abstract ITypeSymbol CreateITypeSymbol(CodeAnalysis.NullableAnnotation nullableAnnotation);

        TypeKind ITypeSymbolInternal.TypeKind => this.TypeKind;

        SpecialType ITypeSymbolInternal.SpecialType => this.SpecialType;

        bool ITypeSymbolInternal.IsReferenceType => this.IsReferenceType;

        bool ITypeSymbolInternal.IsValueType => this.IsValueType;

        ITypeSymbol ITypeSymbolInternal.GetITypeSymbol()
        {
            return GetITypeSymbol(DefaultNullableAnnotation);
        }

        internal abstract bool IsRecord { get; }

        internal abstract bool IsRecordStruct { get; }

        internal abstract bool HasInlineArrayAttribute(out int length);

#nullable enable
        internal FieldSymbol? TryGetPossiblyUnsupportedByLanguageInlineArrayElementField()
        {
            Debug.Assert(HasInlineArrayAttribute(out var length) && length > 0);

            FieldSymbol? elementField = null;

            if (this.TypeKind == TypeKind.Struct)
            {
                foreach (FieldSymbol field in ((NamedTypeSymbol)this).OriginalDefinition.GetFieldsToEmit())
                {
                    if (!field.IsStatic)
                    {
                        if (elementField is not null)
                        {
                            return null;
                        }
                        else
                        {
                            elementField = field;
                        }
                    }
                }
            }

            if (elementField is not null && elementField.ContainingType.IsGenericType)
            {
                elementField = elementField.AsMember((NamedTypeSymbol)this);
            }

            return elementField;
        }

        internal FieldSymbol? TryGetInlineArrayElementField()
        {
            return TryGetPossiblyUnsupportedByLanguageInlineArrayElementField() is { } field && IsInlineArrayElementFieldSupported(field) ? field : null;
        }

        internal static bool IsInlineArrayElementFieldSupported(FieldSymbol elementField)
        {
            return elementField is { RefKind: RefKind.None, IsFixedSizeBuffer: false };
        }
    }
}
