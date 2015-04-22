// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

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
        internal static readonly string ImplicitTypeName = "<invalid-global-code>";

        // InterfaceInfo for a common case of a type not implementing anything directly or indirectly.
        private static readonly InterfaceInfo s_noInterfaces = new InterfaceInfo();

        private ImmutableHashSet<Symbol> _lazyAbstractMembers;
        private InterfaceInfo _lazyInterfaceInfo;

        private class InterfaceInfo
        {
            // all directly implemented interfaces, their bases and all interfaces to the bases of the type recursively
            internal ImmutableArray<NamedTypeSymbol> allInterfaces;

            // same as allInterfaces, but not sorted and does not include interfaces implemented by base types.
            internal ImmutableHashSet<NamedTypeSymbol> interfacesAndTheirBaseInterfaces;

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
                    map = new ConcurrentDictionary<Symbol, SymbolAndDiagnostics>(concurrencyLevel: 1, capacity: 1);
                    return Interlocked.CompareExchange(ref _implementationForInterfaceMemberMap, map, null) ?? map;
                }
            }

            // key = interface method/property/event, value = explicitly implementing method/property/event declared on this type
            internal Dictionary<Symbol, Symbol> explicitInterfaceImplementationMap;

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


        /// <summary>
        /// A comparator that treats dynamic and object as "the same" types.
        /// </summary>
        internal static readonly EqualityComparer<TypeSymbol> EqualsIgnoringDynamicComparer = new EqualsIgnoringComparer(ignoreDynamic: true, ignoreCustomModifiersAndArraySizesAndLowerBounds: false);

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
        public NamedTypeSymbol BaseType
        {
            get
            {
                return BaseTypeNoUseSiteDiagnostics;
            }
        }

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
        public ImmutableArray<NamedTypeSymbol> Interfaces
        {
            get
            {
                return InterfacesNoUseSiteDiagnostics();
            }
        }

        internal abstract ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved = null);

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
        public ImmutableArray<NamedTypeSymbol> AllInterfaces
        {
            get
            {
                return AllInterfacesNoUseSiteDiagnostics;
            }
        }

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
        internal bool IsDerivedFrom(TypeSymbol type, bool ignoreDynamic, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
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
                if (type.Equals(t, ignoreDynamic: ignoreDynamic))
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
        internal bool IsEqualToOrDerivedFrom(TypeSymbol type, bool ignoreDynamic, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            return this.Equals(type, ignoreDynamic: ignoreDynamic) || this.IsDerivedFrom(type, ignoreDynamic, ref useSiteDiagnostics);
        }

        /// <summary>
        /// Determines if this type symbol represent the same type as another, according to the language
        /// semantics.
        /// </summary>
        /// <param name="t2">The other type.</param>
        /// <param name="ignoreCustomModifiersAndArraySizesAndLowerBounds">True to compare without regard to custom modifiers, false by default.</param>
        /// <param name="ignoreDynamic">True to ignore the distinction between object and dynamic, false by default.</param>
        /// <returns>True if the types are equivalent.</returns>
        internal virtual bool Equals(TypeSymbol t2, bool ignoreCustomModifiersAndArraySizesAndLowerBounds = false, bool ignoreDynamic = false)
        {
            return ReferenceEquals(this, t2);
        }

        public sealed override bool Equals(object obj)
        {
            var t2 = obj as TypeSymbol;
            if ((object)t2 == null) return false;
            return this.Equals(t2, false, false);
        }

        /// <summary>
        /// We ignore custom modifiers, and the distinction between dynamic and object, when computing a type's hash code.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        private sealed class EqualsIgnoringComparer : EqualityComparer<TypeSymbol>
        {
            private readonly bool _ignoreDynamic, _ignoreCustomModifiersAndArraySizesAndLowerBounds;

            public EqualsIgnoringComparer(bool ignoreDynamic, bool ignoreCustomModifiersAndArraySizesAndLowerBounds)
            {
                _ignoreDynamic = ignoreDynamic;
                _ignoreCustomModifiersAndArraySizesAndLowerBounds = ignoreCustomModifiersAndArraySizesAndLowerBounds;
            }

            public override int GetHashCode(TypeSymbol obj)
            {
                return obj.GetHashCode();
            }

            public override bool Equals(TypeSymbol x, TypeSymbol y)
            {
                return
                    (object)x == null ? (object)y == null :
                    x.Equals(y, ignoreCustomModifiersAndArraySizesAndLowerBounds: _ignoreCustomModifiersAndArraySizesAndLowerBounds, ignoreDynamic: _ignoreDynamic);
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
            var visited = new HashSet<NamedTypeSymbol>();

            for (var baseType = this; !ReferenceEquals(baseType, null); baseType = baseType.BaseTypeNoUseSiteDiagnostics)
            {
                var interfaces = (baseType.TypeKind == TypeKind.TypeParameter) ? ((TypeParameterSymbol)baseType).EffectiveInterfacesNoUseSiteDiagnostics : baseType.InterfacesNoUseSiteDiagnostics();
                for (int i = interfaces.Length - 1; i >= 0; i--)
                {
                    var @interface = interfaces[i];
                    AddAllInterfaces(@interface, visited, result);
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
        /// of all such types.
        /// </summary>
        /// <remarks>
        /// CONSIDER: it probably isn't truly necessary to cache this.  If space gets tight, consider
        /// alternative approaches (recompute every time, cache on the side, only store on some types,
        /// etc).
        /// </remarks>
        internal ImmutableHashSet<NamedTypeSymbol> InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics
        {
            get
            {
                var info = this.GetInterfaceInfo();
                if (info == s_noInterfaces)
                {
                    return ImmutableHashSet.Create<NamedTypeSymbol>();
                }

                if (info.interfacesAndTheirBaseInterfaces == null)
                {
                    Interlocked.CompareExchange(ref info.interfacesAndTheirBaseInterfaces, MakeInterfacesAndTheirBaseInterfaces(this.InterfacesNoUseSiteDiagnostics()), null);
                }

                return info.interfacesAndTheirBaseInterfaces;
            }
        }

        // Note: Unlike MakeAllInterfaces, this doesn't need to be virtual. It depends on
        // AllInterfaces for its implementation, so it will pick up all changes to MakeAllInterfaces
        // indirectly.
        private static ImmutableHashSet<NamedTypeSymbol> MakeInterfacesAndTheirBaseInterfaces(ImmutableArray<NamedTypeSymbol> declaredInterfaces)
        {
            var resultBuilder = new HashSet<NamedTypeSymbol>();
            foreach (var @interface in declaredInterfaces)
            {
                if (!resultBuilder.Contains(@interface))
                {
                    resultBuilder.Add(@interface);
                    resultBuilder.UnionWith(@interface.AllInterfacesNoUseSiteDiagnostics);
                }
            }

            return resultBuilder.Count == 0 ?
                ImmutableHashSet.Create<NamedTypeSymbol>() : ImmutableHashSet.CreateRange<NamedTypeSymbol>(resultBuilder);
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

            return FindImplementationForInterfaceMemberWithDiagnostics(interfaceMember).Symbol;
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
        /// Is this type a managed type (false for everything but enum, pointer, and
        /// some struct types).
        /// </summary>
        /// <remarks>
        /// See Type::computeManagedType.
        /// </remarks>
        internal abstract bool IsManagedType { get; }

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
            return interfaceMember is Symbol
                ? FindImplementationForInterfaceMember((Symbol)interfaceMember)
                : null;
        }

        #endregion

        #region Interface member checks

        protected SymbolAndDiagnostics FindImplementationForInterfaceMemberWithDiagnostics(Symbol interfaceMember)
        {
            Debug.Assert((object)interfaceMember != null);

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
        /// <param name="interfaceMember">A non-null property on an interface type.</param>
        /// <param name="implementingType">The type implementing the interface property (usually "this").</param>
        /// <param name="diagnostics">Bag to which to add diagnostics.</param>
        /// <returns>The implementing property or null, if there isn't one.</returns>
        private static Symbol ComputeImplementationForInterfaceMember(Symbol interfaceMember, TypeSymbol implementingType, DiagnosticBag diagnostics)
        {
            Debug.Assert(interfaceMember.Kind == SymbolKind.Method || interfaceMember.Kind == SymbolKind.Property || interfaceMember.Kind == SymbolKind.Event);

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

            for (TypeSymbol currType = implementingType; (object)currType != null; currType = currType.BaseTypeNoUseSiteDiagnostics)
            {
                // NOTE: In the case of PE symbols, it is possible to see an explicit implementation
                // on a type that does not declare the corresponding interface (or one of its
                // subinterfaces).  In such cases, we want to return the explicit implementation,
                // even if it doesn't participate in interface mapping according to the C# rules.

                // pass 1: check for explicit impls (can't assume name matches)
                Symbol currTypeExplicitImpl = currType.GetExplicitImplementationForInterfaceMember(interfaceMember);
                if ((object)currTypeExplicitImpl != null)
                {
                    return currTypeExplicitImpl;
                }

                // WORKAROUND: see comment on method.
                if (IsExplicitlyImplementedViaAccessors(interfaceMember, currType, out currTypeExplicitImpl))
                {
                    // NOTE: may be null.
                    return currTypeExplicitImpl;
                }

                seenTypeDeclaringInterface = seenTypeDeclaringInterface || currType.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Contains(interfaceType);

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

            // Dev10 has some extra restrictions and extra wiggle room when finding implicit
            // implementations for interface accessors.  Perform some extra checks and possibly
            // update the result (i.e. implicitImpl).
            if (interfaceMember.IsAccessor())
            {
                CheckForImplementationOfCorrespondingPropertyOrEvent((MethodSymbol)interfaceMember, implementingType, implementingTypeIsFromSomeCompilation, ref implicitImpl);
            }

            if ((object)implicitImpl != null)
            {
                ReportImplicitImplementationMatchDiagnostics(interfaceMember, implementingType, implicitImpl, diagnostics);
            }
            else if ((object)closestMismatch != null)
            {
                ReportImplicitImplementationMismatchDiagnostics(interfaceMember, implementingType, closestMismatch, diagnostics);
            }

            return implicitImpl;
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
                Symbol implementation = currType.GetExplicitImplementationForInterfaceMember(interfaceAccessor);
                if ((object)implementation != null)
                {
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
        private static void CheckForImplementationOfCorrespondingPropertyOrEvent(MethodSymbol interfaceMethod, TypeSymbol implementingType, bool implementingTypeIsFromSomeCompilation, ref Symbol implicitImpl)
        {
            Debug.Assert(interfaceMethod.IsAccessor());

            Symbol associatedInterfacePropertyOrEvent = interfaceMethod.AssociatedSymbol;
            Symbol implementingPropertyOrEvent = implementingType.FindImplementationForInterfaceMember(associatedInterfacePropertyOrEvent); // NB: uses cache
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
            else if ((object)correspondingImplementingAccessor != null && ((object)implicitImpl == null || correspondingImplementingAccessor.ContainingType == implicitImpl.ContainingType))
            {
                // Suppose the interface accessor and the implementing accessor have different names.
                // In Dev10, as long as the corresponding properties have an implementation relationship,
                // then the accessor can be considered an implementation, even though the name is different.
                // Later on, when we check that implementation signatures match exactly
                // (in SourceNamedTypeSymbol.ImplementInterfaceMember), they won't (because of the names)
                // and an explicit implementation method will be synthesized.

                MethodSymbol interfaceAccessorWithImplementationName = new SignatureOnlyMethodSymbol(
                    correspondingImplementingAccessor.Name,
                    interfaceMethod.ContainingType,
                    interfaceMethod.MethodKind,
                    interfaceMethod.CallingConvention,
                    interfaceMethod.TypeParameters,
                    interfaceMethod.Parameters,
                    interfaceMethod.RefKind,
                    interfaceMethod.ReturnType,
                    interfaceMethod.ReturnTypeCustomModifiers,
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
            if (interfaceMember.Kind == SymbolKind.Method)
            {
                var interfaceMethod = (MethodSymbol)interfaceMember;
                bool implicitImplIsAccessor = implicitImpl.IsAccessor();
                bool interfaceMethodIsAccessor = interfaceMethod.IsAccessor();

                if (interfaceMethodIsAccessor && !implicitImplIsAccessor && !interfaceMethod.IsIndexedPropertyAccessor())
                {
                    diagnostics.Add(ErrorCode.ERR_MethodImplementingAccessor, implicitImpl.Locations[0], implicitImpl, interfaceMethod, implementingType);
                }
                else if (!interfaceMethodIsAccessor && implicitImplIsAccessor)
                {
                    diagnostics.Add(ErrorCode.ERR_AccessorImplementingMethod, implicitImpl.Locations[0], implicitImpl, interfaceMethod, implementingType);
                }
                else
                {
                    var implicitImplMethod = (MethodSymbol)implicitImpl;

                    if (implicitImplMethod.IsConditional)
                    {
                        // CS0629: Conditional member '{0}' cannot implement interface member '{1}' in type '{2}'
                        diagnostics.Add(ErrorCode.ERR_InterfaceImplementedByConditional, implicitImpl.Locations[0], implicitImpl, interfaceMethod, implementingType);
                    }
                    else
                    {
                        ReportAnyMismatchedConstraints(interfaceMethod, implementingType, implicitImplMethod, diagnostics);
                    }
                }
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
                        diagnostics.Add(ErrorCode.WRN_MultipleRuntimeImplementationMatches, member.Locations[0], member, interfaceMember, implementingType);
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
            Location interfaceLocation = null;
            if ((object)implementingType != null)
            {
                var @interface = interfaceMember.ContainingType;
                SourceMemberContainerTypeSymbol snt = implementingType as SourceMemberContainerTypeSymbol;
                interfaceLocation = snt.GetImplementsLocation(@interface) ?? implementingType.Locations[0];
            }
            else
            {
                interfaceLocation = implementingType.Locations[0];
            }

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
                        hasRefReturnMismatch = (((MethodSymbol)closestMismatch).RefKind != RefKind.None) != (interfaceMemberRefKind != RefKind.None);
                        break;

                    case SymbolKind.Property:
                        hasRefReturnMismatch = (((PropertySymbol)closestMismatch).RefKind != RefKind.None) != (interfaceMemberRefKind != RefKind.None);
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
                    diagnostics.Add(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn, interfaceLocation, implementingType, interfaceMember, closestMismatch, interfaceMemberRefKind != RefKind.None ? "reference" : "value");
                }
                else
                {
                    diagnostics.Add(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, interfaceLocation, implementingType, interfaceMember, closestMismatch, interfaceMemberReturnType);
                }
            }
        }

        private static void ReportAnyMismatchedConstraints(MethodSymbol interfaceMethod, TypeSymbol implementingType, MethodSymbol implicitImpl, DiagnosticBag diagnostics)
        {
            Debug.Assert(interfaceMethod.Arity == implicitImpl.Arity);

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
                        var location = (implicitImpl.ContainingType == implementingType) ?
                            implicitImpl.Locations[0] :
                            implementingType.Locations[0];
                        diagnostics.Add(ErrorCode.ERR_ImplBadConstraints, location, typeParameter2.Name, implicitImpl, typeParameter1.Name, interfaceMethod);
                    }
                }
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
        /// There is some similarity between this member and MemberSymbol.FindOverriddenOrHiddenMembersInType.
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

                    //if we haven't found a match, do a weaker comparison that ignores static-ness, accessibility, and return type
                    if ((object)closeMismatch == null && implementingTypeIsFromSomeCompilation)
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
        /// SourceNamedTypeSymbol.ImplementInterfaceMember.
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
                // SourceNamedTypeSymbol.ImplementInterfaceMember.
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

        private Symbol GetExplicitImplementationForInterfaceMember(Symbol interfaceMember)
        {
            var info = this.GetInterfaceInfo();
            if (info == s_noInterfaces)
            {
                return null;
            }

            if (info.explicitInterfaceImplementationMap == null)
            {
                Interlocked.CompareExchange(ref info.explicitInterfaceImplementationMap, MakeExplicitInterfaceImplementationMap(), null);
            }

            Symbol implementingMethod;
            info.explicitInterfaceImplementationMap.TryGetValue(interfaceMember, out implementingMethod); //no exception - just return null
            return implementingMethod;
        }

        private Dictionary<Symbol, Symbol> MakeExplicitInterfaceImplementationMap()
        {
            var map = new Dictionary<Symbol, Symbol>();
            foreach (var member in this.GetMembersUnordered())
            {
                foreach (var interfaceMember in member.GetExplicitInterfaceImplementations())
                {
                    if (!map.ContainsKey(interfaceMember))
                    {
                        map[interfaceMember] = member;
                    }
                    else
                    {
                        // Source: just choose the first one - the error will be reported on the duplicate declaration
                        // PE: actually determined at runtime - just choose the first one

                        // CONSIDER: we could map to an error symbol or to a SymbolAndDiagnostics object
                    }
                }
            }
            return map;
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

        [Obsolete("Use TypeWithModifiers.Is method.", true)]
        internal bool Equals(TypeWithModifiers other)
        {
            return other.Is(this);
        }
    }
}
