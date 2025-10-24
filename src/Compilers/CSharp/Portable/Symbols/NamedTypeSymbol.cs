// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a type other than an array, a pointer, a type parameter, and dynamic.
    /// </summary>
    internal abstract partial class NamedTypeSymbol : TypeSymbol, INamedTypeSymbolInternal
    {
        private bool _hasNoBaseCycles;

        private static readonly ImmutableSegmentedDictionary<string, Symbol> RequiredMembersErrorSentinel = ImmutableSegmentedDictionary<string, Symbol>.Empty.Add("<error sentinel>", null!);

        /// <summary>
        /// <see langword="default"/> if uninitialized. <see cref="RequiredMembersErrorSentinel"/> if there are errors. <see cref="ImmutableSegmentedDictionary{TKey, TValue}.Empty"/> if
        /// there are no required members. Otherwise, the required members.
        /// </summary>
        private ImmutableSegmentedDictionary<string, Symbol> _lazyRequiredMembers = default;

        // Only the compiler can create NamedTypeSymbols.
        internal NamedTypeSymbol(TupleExtraData tupleData = null)
        {
            _lazyTupleData = tupleData;
        }

        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // Changes to the public interface of this class should remain synchronized with the VB version.
        // Do not make any changes to the public interface without making the corresponding change
        // to the VB version.
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        // TODO: How should anonymous types be represented? One possible options: have an
        // IsAnonymous on this type. The Name would then return a unique compiler-generated
        // type that matches the metadata name.

        /// <summary>
        /// Returns the arity of this type, or the number of type parameters it takes.
        /// A non-generic type has zero arity.
        /// </summary>
        public abstract int Arity { get; }

        /// <summary>
        /// Returns the type parameters that this type has. If this is a non-generic type,
        /// returns an empty ImmutableArray.  
        /// </summary>
        public abstract ImmutableArray<TypeParameterSymbol> TypeParameters { get; }

        /// <summary>
        /// Returns the type arguments that have been substituted for the type parameters. 
        /// If nothing has been substituted for a given type parameter,
        /// then the type parameter itself is consider the type argument.
        /// </summary>
        internal abstract ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics { get; }

        internal ImmutableArray<TypeWithAnnotations> TypeArgumentsWithDefinitionUseSiteDiagnostics(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            var result = TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;

            foreach (var typeArgument in result)
            {
                typeArgument.Type.OriginalDefinition.AddUseSiteInfo(ref useSiteInfo);
            }

            return result;
        }

        internal TypeWithAnnotations TypeArgumentWithDefinitionUseSiteDiagnostics(int index, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            var result = TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[index];
            result.Type.OriginalDefinition.AddUseSiteInfo(ref useSiteInfo);
            return result;
        }

        /// <summary>
        /// Returns the type symbol that this type was constructed from. This type symbol
        /// has the same containing type (if any), but has type arguments that are the same
        /// as the type parameters (although its containing type might not).
        /// </summary>
        public abstract NamedTypeSymbol ConstructedFrom { get; }

        /// <summary>
        /// For enum types, gets the underlying type. Returns null on all other
        /// kinds of types.
        /// </summary>
        public virtual NamedTypeSymbol EnumUnderlyingType
        {
            get
            {
                return null;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                // we can do this since if a type does not live directly in a type
                // there is no containing type at all
                // NOTE: many derived types will override this with even better implementation
                //       since most know their containing types/symbols directly
                return this.ContainingSymbol as NamedTypeSymbol;
            }
        }

        /// <summary>
        /// Returns true for a struct type containing a cycle.
        /// This property is intended for flow analysis only
        /// since it is only implemented for source types.
        /// </summary>
        internal virtual bool KnownCircularStruct
        {
            get
            {
                return false;
            }
        }

        internal bool KnownToHaveNoDeclaredBaseCycles
        {
            get
            {
                return _hasNoBaseCycles;
            }
        }

        internal void SetKnownToHaveNoDeclaredBaseCycles()
        {
            _hasNoBaseCycles = true;
        }

        /// <summary>
        /// Is this a NoPia local type explicitly declared in source, i.e.
        /// top level type with a TypeIdentifier attribute on it?
        /// </summary>
        internal virtual bool IsExplicitDefinitionOfNoPiaLocalType
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true and a string from the first GuidAttribute on the type, 
        /// the string might be null or an invalid guid representation. False, 
        /// if there is no GuidAttribute with string argument.
        /// </summary>
        internal abstract bool GetGuidString(out string guidString);

#nullable enable
        /// <summary>
        /// For delegate types, gets the delegate's invoke method.  Returns null on
        /// all other kinds of types.  Note that it is possible to have an ill-formed
        /// delegate type imported from metadata which does not have an Invoke method.
        /// Such a type will be classified as a delegate but its DelegateInvokeMethod
        /// would be null.
        /// </summary>
        public MethodSymbol? DelegateInvokeMethod
        {
            get
            {
                if (TypeKind != TypeKind.Delegate)
                {
                    return null;
                }

                var methods = GetMembers(WellKnownMemberNames.DelegateInvokeName);
                if (methods.Length != 1)
                {
                    return null;
                }

                var method = methods[0] as MethodSymbol;

                //EDMAURER we used to also check 'method.IsVirtual' because section 13.6
                //of the CLI spec dictates that it be virtual, but real world
                //working metadata has been found that contains an Invoke method that is
                //marked as virtual but not newslot (both of those must be combined to
                //meet the C# definition of virtual). Rather than weaken the check
                //I've removed it, as the Dev10 compiler makes no check, and we don't
                //stand to gain anything by having it.

                //return method != null && method.IsVirtual ? method : null;
                return method;
            }
        }
#nullable disable

        /// <summary>
        /// Adds the operators for this type by their metadata name to <paramref name="operators"/>
        /// </summary>
        internal void AddOperators(string name, ArrayBuilder<MethodSymbol> operators)
        {
            ImmutableArray<Symbol> candidates = GetSimpleNonTypeMembers(name);
            if (candidates.IsEmpty)
                return;

            AddOperators(operators, candidates);
        }

        internal static void AddOperators(ArrayBuilder<MethodSymbol> operators, ImmutableArray<Symbol> candidates)
        {
            foreach (var candidate in candidates)
            {
                if (candidate is MethodSymbol { MethodKind: MethodKind.UserDefinedOperator or MethodKind.Conversion } method)
                {
                    operators.Add(method);
                }
            }
        }

        internal static void AddOperators(ArrayBuilder<MethodSymbol> operators, ArrayBuilder<Symbol> candidates)
        {
            foreach (var candidate in candidates)
            {
                if (candidate is MethodSymbol { MethodKind: MethodKind.UserDefinedOperator or MethodKind.Conversion } method)
                {
                    operators.Add(method);
                }
            }
        }

        /// <summary>
        /// Get the instance constructors for this type.
        /// </summary>
        public ImmutableArray<MethodSymbol> InstanceConstructors
        {
            get
            {
                return GetConstructors(includeInstance: true, includeStatic: false);
            }
        }

        /// <summary>
        /// Get the static constructors for this type.
        /// </summary>
        public ImmutableArray<MethodSymbol> StaticConstructors
        {
            get
            {
                return GetConstructors(includeInstance: false, includeStatic: true);
            }
        }

        /// <summary>
        /// Get the instance and static constructors for this type.
        /// </summary>
        public ImmutableArray<MethodSymbol> Constructors
        {
            get
            {
                return GetConstructors(includeInstance: true, includeStatic: true);
            }
        }

        private ImmutableArray<MethodSymbol> GetConstructors(bool includeInstance, bool includeStatic)
        {
            Debug.Assert(includeInstance || includeStatic);

            ImmutableArray<Symbol> instanceCandidates = includeInstance
                ? GetMembers(WellKnownMemberNames.InstanceConstructorName)
                : ImmutableArray<Symbol>.Empty;
            ImmutableArray<Symbol> staticCandidates = includeStatic
                ? GetMembers(WellKnownMemberNames.StaticConstructorName)
                : ImmutableArray<Symbol>.Empty;

            if (instanceCandidates.IsEmpty && staticCandidates.IsEmpty)
            {
                return ImmutableArray<MethodSymbol>.Empty;
            }

            ArrayBuilder<MethodSymbol> constructors = ArrayBuilder<MethodSymbol>.GetInstance();
            foreach (Symbol candidate in instanceCandidates)
            {
                if (candidate is MethodSymbol method)
                {
                    Debug.Assert(method.MethodKind == MethodKind.Constructor);
                    constructors.Add(method);
                }
            }
            foreach (Symbol candidate in staticCandidates)
            {
                if (candidate is MethodSymbol method)
                {
                    Debug.Assert(method.MethodKind == MethodKind.StaticConstructor);
                    constructors.Add(method);
                }
            }
            return constructors.ToImmutableAndFree();
        }

        /// <summary>
        /// Get the indexers for this type.
        /// </summary>
        /// <remarks>
        /// Won't include indexers that are explicit interface implementations.
        /// </remarks>
        public ImmutableArray<PropertySymbol> Indexers
        {
            get
            {
                ImmutableArray<Symbol> candidates = GetSimpleNonTypeMembers(WellKnownMemberNames.Indexer);

                if (candidates.IsEmpty)
                {
                    return ImmutableArray<PropertySymbol>.Empty;
                }

                // The common case will be returning a list with the same elements as "candidates",
                // but we need a list of PropertySymbols, so we're stuck building a new list anyway.
                ArrayBuilder<PropertySymbol> indexers = ArrayBuilder<PropertySymbol>.GetInstance();
                foreach (Symbol candidate in candidates)
                {
                    if (candidate.Kind == SymbolKind.Property)
                    {
                        Debug.Assert(((PropertySymbol)candidate).IsIndexer);
                        indexers.Add((PropertySymbol)candidate);
                    }
                }
                return indexers.ToImmutableAndFree();
            }
        }

        /// <summary>
        /// Returns true if this type might contain extension methods. If this property
        /// returns false, there are no extension methods in this type.
        /// </summary>
        /// <remarks>
        /// This property allows the search for extension methods to be narrowed quickly.
        /// </remarks>
        public abstract bool MightContainExtensionMethods { get; }

        /// <remarks>Does not perform a full viability check</remarks>
        internal void GetExtensionMethods(ArrayBuilder<MethodSymbol> methods, string nameOpt, int arity, LookupOptions options)
        {
            if (this.MightContainExtensionMethods)
            {
                DoGetExtensionMethods(methods, nameOpt, arity, options);
            }
        }

        /// <remarks>Does not perform a full viability check</remarks>
        internal void DoGetExtensionMethods(ArrayBuilder<MethodSymbol> methods, string nameOpt, int arity, LookupOptions options)
        {
            var members = nameOpt == null
                ? this.GetMembersUnordered()
                : this.GetSimpleNonTypeMembers(nameOpt);

            foreach (var member in members)
            {
                if (member.Kind == SymbolKind.Method)
                {
                    var method = (MethodSymbol)member;
                    if (method.IsExtensionMethod &&
                        ((options & LookupOptions.AllMethodsOnArityZero) != 0 || arity == method.Arity))
                    {
                        var thisParam = method.Parameters.First();
                        if (!IsValidExtensionReceiverParameter(thisParam))
                        {
                            continue;
                        }

                        Debug.Assert(method.MethodKind != MethodKind.ReducedExtension);
                        methods.Add(method);
                    }
                }
            }
        }

#nullable enable
        private static bool IsValidExtensionReceiverParameter(ParameterSymbol thisParam)
        {
            Debug.Assert(thisParam is not null);

            if (!thisParam.Type.IsValidExtensionParameterType())
            {
                return false;
            }

            // For ref and ref-readonly extension members and classic extension methods, receivers need to be of the correct types to be considered in lookup
            if (thisParam.RefKind == RefKind.Ref && !thisParam.Type.IsValueType)
            {
                return false;
            }

            if (thisParam.RefKind is RefKind.In or RefKind.RefReadOnlyParameter
                && !thisParam.Type.IsValidInOrRefReadonlyExtensionParameterType())
            {
                return false;
            }

            return true;
        }

        /// <remarks>Does not perform a full viability check</remarks>
        internal void GetExtensionMembers(ArrayBuilder<Symbol> members, string? name, string? alternativeName, int arity, LookupOptions options, ConsList<FieldSymbol> fieldsBeingBound)
        {
            Debug.Assert((options & ~(LookupOptions.IncludeExtensionMembers | LookupOptions.AllMethodsOnArityZero
                | LookupOptions.MustBeInstance | LookupOptions.MustNotBeInstance | LookupOptions.MustBeInvocableIfMember
                | LookupOptions.MustBeOperator | LookupOptions.MustNotBeMethodTypeParameter)) == 0);

            Debug.Assert(name is not null || alternativeName is null);

            if (!this.IsClassType() || !IsStatic || IsGenericType || !MightContainExtensionMethods) return;

            foreach (NamedTypeSymbol nestedType in GetTypeMembersUnordered())
            {
                if (nestedType is not { IsExtension: true, ExtensionParameter: { } extensionParameter }
                    || !IsValidExtensionReceiverParameter(extensionParameter))
                {
                    continue;
                }

                var candidates = name is null || alternativeName is not null
                    ? nestedType.GetMembersUnordered()
                    : nestedType.GetMembers(name);

                foreach (var candidate in candidates)
                {
                    if (!SourceMemberContainerTypeSymbol.IsAllowedExtensionMember(candidate))
                    {
                        // Not supported yet
                        continue;
                    }

                    if (extensionMemberMatches(candidate, name, alternativeName, arity, options, fieldsBeingBound))
                    {
                        members.Add(candidate);
                    }
                }
            }

            return;

            static bool extensionMemberMatches(Symbol member, string? name, string? alternativeName, int arity, LookupOptions options, ConsList<FieldSymbol> fieldsBeingBound)
            {
                if ((options & LookupOptions.MustBeInstance) != 0 && member.IsStatic)
                {
                    return false;
                }

                if ((options & LookupOptions.MustNotBeInstance) != 0 && !member.IsStatic)
                {
                    return false;
                }

                if ((options & LookupOptions.MustBeOperator) != 0 && member is not MethodSymbol { MethodKind: MethodKind.UserDefinedOperator })
                {
                    return false;
                }

                if ((options & LookupOptions.AllMethodsOnArityZero) == 0
                    && arity != member.GetMemberArityIncludingExtension())
                {
                    return false;
                }

                string memberName = member.Name;
                bool namesMatch = name is null
                    || memberName == name
                    || (alternativeName is not null && memberName == alternativeName);

                if (!namesMatch)
                {
                    return false;
                }

                if ((options & LookupOptions.MustBeInvocableIfMember) != 0
                    && !Binder.IsInvocableMember(member, fieldsBeingBound))
                {
                    return false;
                }

                return true;
            }
        }

        public virtual MethodSymbol? TryGetCorrespondingExtensionImplementationMethod(MethodSymbol method)
        {
            throw ExceptionUtilities.Unreachable();
        }

#nullable disable

        // TODO: Probably should provide similar accessors for static constructor, destructor, 
        // TODO: operators, conversions.

        /// <summary>
        /// Returns true if this type is known to be a reference type. It is never the case that
        /// IsReferenceType and IsValueType both return true. However, for an unconstrained type
        /// parameter, IsReferenceType and IsValueType will both return false.
        /// </summary>
        public override bool IsReferenceType
        {
            get
            {
                var kind = TypeKind;
                return kind != TypeKind.Enum && kind != TypeKind.Struct && kind != TypeKind.Error;
            }
        }

        /// <summary>
        /// Returns true if this type is known to be a value type. It is never the case that
        /// IsReferenceType and IsValueType both return true. However, for an unconstrained type
        /// parameter, IsReferenceType and IsValueType will both return false.
        /// </summary>
        public override bool IsValueType
        {
            get
            {
                var kind = TypeKind;
                return kind == TypeKind.Struct || kind == TypeKind.Enum;
            }
        }

        internal override ManagedKind GetManagedKind(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // CONSIDER: we could cache this, but it's only expensive for non-special struct types
            // that are pointed to.  For now, only cache on SourceMemberContainerSymbol since it fits
            // nicely into the flags variable.
            return BaseTypeAnalysis.GetManagedKind(this, ref useSiteInfo);
        }

        /// <summary>
        /// Gets the associated attribute usage info for an attribute type.
        /// </summary>
        internal abstract AttributeUsageInfo GetAttributeUsageInfo();

        /// <summary>
        /// Returns true if the type is a Script class. 
        /// It might be an interactive submission class or a Script class in a csx file.
        /// </summary>
        public virtual bool IsScriptClass
        {
            get
            {
                return false;
            }
        }

        internal bool IsSubmissionClass
        {
            get
            {
                return TypeKind == TypeKind.Submission;
            }
        }

        internal SynthesizedInstanceConstructor GetScriptConstructor()
        {
            Debug.Assert(IsScriptClass);
            return (SynthesizedInstanceConstructor)InstanceConstructors.Single();
        }

        internal SynthesizedInteractiveInitializerMethod GetScriptInitializer()
        {
            Debug.Assert(IsScriptClass);
            return (SynthesizedInteractiveInitializerMethod)GetMembers(SynthesizedInteractiveInitializerMethod.InitializerName).Single();
        }

        internal SynthesizedEntryPointSymbol GetScriptEntryPoint()
        {
            Debug.Assert(IsScriptClass);
            var name = (TypeKind == TypeKind.Submission) ? SynthesizedEntryPointSymbol.FactoryName : SynthesizedEntryPointSymbol.MainName;
            return (SynthesizedEntryPointSymbol)GetMembers(name).Single();
        }

        /// <summary>
        /// Returns true if the type is the implicit class that holds onto invalid global members (like methods or
        /// statements in a non script file).
        /// </summary>
        public virtual bool IsImplicitClass
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the name of this symbol. Symbols without a name return the empty string; null is
        /// never returned.
        /// </summary>
        public abstract override string Name { get; }

#nullable enable
        /// <summary>
        /// Return the name including the metadata arity suffix.
        /// </summary>
        public override string MetadataName
        {
            get
            {
                var fileIdentifier = this.GetFileLocalTypeMetadataNamePrefix();
                // If we have a file prefix, the type will definitely use CLS arity encoding for nonzero arity.
                Debug.Assert(!(fileIdentifier != null && !MangleName && Arity > 0));
                return fileIdentifier != null || MangleName
                    ? MetadataHelpers.ComposeAritySuffixedMetadataName(Name, Arity, fileIdentifier)
                    : Name;
            }
        }

        internal abstract bool IsFileLocal { get; }

        /// <summary>
        /// If this type is a file-local type, returns an identifier for the file this type was declared in. Otherwise, returns null.
        /// </summary>
        internal abstract FileIdentifier? AssociatedFileIdentifier { get; }

        [MemberNotNullWhen(true, nameof(ExtensionGroupingName), nameof(ExtensionMarkerName))]
        public virtual bool IsExtension
            => TypeKind == TypeKind.Extension;

        /// <summary>
        /// For the type representing an extension declaration, returns the receiver parameter symbol.
        /// It may be unnamed.
        /// Note: this may be null even if <see cref="IsExtension"/> is true, in error cases.
        /// </summary>
        internal abstract ParameterSymbol? ExtensionParameter { get; }

        /// <summary>
        /// For extensions, returns the synthesized identifier for the grouping type.
        /// Returns null otherwise.
        /// </summary>
        internal abstract string? ExtensionGroupingName { get; }

        /// <summary>
        /// For extensions, returns the synthesized identifier for the marker type.
        /// Returns null otherwise.
        /// </summary>
        internal abstract string? ExtensionMarkerName { get; }
#nullable disable

        /// <summary>
        /// Should the name returned by Name property be mangled with [`arity] suffix in order to get metadata name.
        /// Must return False for a type with Arity == 0.
        /// </summary>
        /// <remarks>
        /// Some types with Arity > 0 still have MangleName == false. For example, EENamedTypeSymbol.
        /// Note that other differences between source names and metadata names exist and are not controlled by this property,
        /// such as the 'AssociatedFileIdentifier' prefix for file types.
        /// </remarks>
        internal abstract bool MangleName
        {
            // Intentionally no default implementation to force consideration of appropriate implementation for each new subclass
            get;
        }

        /// <summary>
        /// Collection of names of members declared within this type. May return duplicates.
        /// </summary>
        public abstract IEnumerable<string> MemberNames { get; }

        /// <summary>
        /// True if this type declares any required members. It does not recursively check up the tree for _all_ required members.
        /// </summary>
        internal abstract bool HasDeclaredRequiredMembers { get; }

#nullable enable
        /// <summary>
        /// Whether the type encountered an error while trying to build its complete list of required members.
        /// </summary>
        internal bool HasRequiredMembersError
        {
            get
            {
                EnsureRequiredMembersCalculated();
                Debug.Assert(!_lazyRequiredMembers.IsDefault);
                return _lazyRequiredMembers == RequiredMembersErrorSentinel;
            }
        }

        /// <summary>
        /// Returns true if there are any required members. Prefer calling this over checking <see cref="AllRequiredMembers"/> for empty, as
        /// this will avoid calculating base type requirements if not necessary.
        /// </summary>
        internal bool HasAnyRequiredMembers => HasDeclaredRequiredMembers || !AllRequiredMembers.IsEmpty;

        /// <summary>
        /// The full list of all required members for this type, including from base classes. If <see cref="HasRequiredMembersError"/> is true,
        /// this returns empty.
        /// </summary>
        /// <remarks>
        /// Do not call this API if all you need are the required members declared on this type. Use <see cref="GetMembers()"/> instead, filtering for
        /// required members, instead of calling this API. If you only need to determine whether this type or any base types have required members, call
        /// <see cref="HasAnyRequiredMembers"/>, which will avoid calling this API if not required.
        /// </remarks>
        internal ImmutableSegmentedDictionary<string, Symbol> AllRequiredMembers
        {
            get
            {
                EnsureRequiredMembersCalculated();
                Debug.Assert(!_lazyRequiredMembers.IsDefault);
                if (_lazyRequiredMembers == RequiredMembersErrorSentinel)
                {
                    return ImmutableSegmentedDictionary<string, Symbol>.Empty;
                }

                return _lazyRequiredMembers;
            }
        }

        private void EnsureRequiredMembersCalculated()
        {
            if (!_lazyRequiredMembers.IsDefault)
            {
                return;
            }

            bool success = tryCalculateRequiredMembers(out ImmutableSegmentedDictionary<string, Symbol>.Builder? builder);

            var requiredMembers = success
                ? builder?.ToImmutable() ?? BaseTypeNoUseSiteDiagnostics?.AllRequiredMembers ?? ImmutableSegmentedDictionary<string, Symbol>.Empty
                : RequiredMembersErrorSentinel;

            RoslynImmutableInterlocked.InterlockedInitialize(ref _lazyRequiredMembers, requiredMembers);

            bool tryCalculateRequiredMembers(out ImmutableSegmentedDictionary<string, Symbol>.Builder? requiredMembersBuilder)
            {
                requiredMembersBuilder = null;
                if (BaseTypeNoUseSiteDiagnostics?.HasRequiredMembersError == true)
                {
                    return false;
                }

                var baseAllRequiredMembers = BaseTypeNoUseSiteDiagnostics?.AllRequiredMembers ?? ImmutableSegmentedDictionary<string, Symbol>.Empty;
                var hasDeclaredRequiredMembers = HasDeclaredRequiredMembers;

                foreach (var member in GetMembersUnordered())
                {
                    if (member is PropertySymbol { ParameterCount: > 0 } prop)
                    {
                        if (prop.IsRequired)
                        {
                            // Bad metadata. Indexed properties cannot be required.
                            return false;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (baseAllRequiredMembers.TryGetValue(member.Name, out var existingMember))
                    {
                        // This is only permitted if the member is an override of a required member from a base type, and is required itself.
                        if (!member.IsRequired()
                            || member.GetOverriddenMember() is not { } overriddenMember
                            || !overriddenMember.Equals(existingMember, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.AllNullableIgnoreOptions))
                        {
                            return false;
                        }
                    }

                    if (!member.IsRequired())
                    {
                        continue;
                    }

                    if (!hasDeclaredRequiredMembers)
                    {
                        // Bad metadata. Type claimed it didn't declare any required members, but we found one.
                        return false;
                    }

                    requiredMembersBuilder ??= baseAllRequiredMembers.ToBuilder();

                    requiredMembersBuilder[member.Name] = member;
                }

                return true;
            }
        }
#nullable disable

        /// <summary>
        /// Get all the members of this symbol.
        /// </summary>
        /// <returns>An ImmutableArray containing all the members of this symbol. If this symbol has no members,
        /// returns an empty ImmutableArray. Never returns null.</returns>
        public abstract override ImmutableArray<Symbol> GetMembers();

        /// <summary>
        /// Get all the members of this symbol that have a particular name.
        /// </summary>
        /// <returns>An ImmutableArray containing all the members of this symbol with the given name. If there are
        /// no members with this name, returns an empty ImmutableArray. Never returns null.</returns>
        public abstract override ImmutableArray<Symbol> GetMembers(string name);

        /// <summary>
        /// A lightweight check for whether this type has a possible clone method. This is less costly than GetMembers,
        /// particularly for PE symbols, and can be used as a cheap heuristic for whether to fully search through all
        /// members of this type for a valid clone method.
        /// </summary>
        internal abstract bool HasPossibleWellKnownCloneMethod();

        internal virtual ImmutableArray<Symbol> GetSimpleNonTypeMembers(string name)
        {
            return GetMembers(name);
        }

        /// <summary>
        /// Get all the members of this symbol that are types.
        /// </summary>
        /// <returns>An ImmutableArray containing all the types that are members of this symbol. If this symbol has no type members,
        /// returns an empty ImmutableArray. Never returns null.</returns>
        public abstract override ImmutableArray<NamedTypeSymbol> GetTypeMembers();

        /// <summary>
        /// Get all the members of this symbol that are types that have a particular name and arity
        /// </summary>
        /// <returns>An ImmutableArray containing all the types that are members of this symbol with the given name and arity.
        /// If this symbol has no type members with this name and arity,
        /// returns an empty ImmutableArray. Never returns null.</returns>
        public abstract override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity);

        /// <summary>
        /// Get all instance field and event members.
        /// </summary>
        /// <remarks>
        /// For source symbols may be called while calculating
        /// <see cref="NamespaceOrTypeSymbol.GetMembersUnordered"/>.
        /// </remarks>
        internal virtual IEnumerable<Symbol> GetInstanceFieldsAndEvents()
        {
            return GetMembersUnordered().Where(IsInstanceFieldOrEvent);
        }

        protected static Func<Symbol, bool> IsInstanceFieldOrEvent = symbol =>
        {
            if (!symbol.IsStatic)
            {
                switch (symbol.Kind)
                {
                    case SymbolKind.Field:
                    case SymbolKind.Event:
                        return true;
                }
            }
            return false;
        };

        /// <summary>
        /// Get this accessibility that was declared on this symbol. For symbols that do not have
        /// accessibility declared on them, returns NotApplicable.
        /// </summary>
        public abstract override Accessibility DeclaredAccessibility { get; }

        /// <summary>
        /// Used to implement visitor pattern.
        /// </summary>
        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNamedType(this, argument);
        }

        public override void Accept(CSharpSymbolVisitor visitor)
        {
            visitor.VisitNamedType(this);
        }

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            return visitor.VisitNamedType(this);
        }

        /// <summary>
        /// During early attribute decoding, we consider a safe subset of all members that will not
        /// cause cyclic dependencies.  Get all such members for this symbol.
        /// </summary>
        /// <remarks>
        /// Never returns null (empty instead).
        /// Expected implementations: for source, return type and field members; for metadata, return all members.
        /// </remarks>
        internal abstract ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers();

        /// <summary>
        /// During early attribute decoding, we consider a safe subset of all members that will not
        /// cause cyclic dependencies.  Get all such members for this symbol that have a particular name.
        /// </summary>
        /// <remarks>
        /// Never returns null (empty instead).
        /// Expected implementations: for source, return type and field members; for metadata, return all members.
        /// </remarks>
        internal abstract ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name);

        /// <summary>
        /// Gets the kind of this symbol.
        /// </summary>
        public override SymbolKind Kind // Cannot seal this method because of the ErrorSymbol.
        {
            get
            {
                return SymbolKind.NamedType;
            }
        }

        internal abstract NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved);

        internal abstract ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved);

        public override int GetHashCode()
        {
            // return a distinguished value for 'object' so we can return the same value for 'dynamic'.
            // That's because the hash code ignores the distinction between dynamic and object.  It also
            // ignores custom modifiers.
            if (this.SpecialType == SpecialType.System_Object)
            {
                return (int)SpecialType.System_Object;
            }

            // OriginalDefinition must be object-equivalent.
            return RuntimeHelpers.GetHashCode(OriginalDefinition);
        }

        /// <summary>
        /// Compares this type to another type.
        /// </summary>
        internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison)
        {
            if ((object)t2 == this) return true;
            if ((object)t2 == null) return false;

            if ((comparison & TypeCompareKind.IgnoreDynamic) != 0)
            {
                if (t2.TypeKind == TypeKind.Dynamic)
                {
                    // if ignoring dynamic, then treat dynamic the same as the type 'object'
                    if (this.SpecialType == SpecialType.System_Object)
                    {
                        return true;
                    }
                }
            }

            NamedTypeSymbol other = t2 as NamedTypeSymbol;
            if ((object)other == null) return false;

            // Compare OriginalDefinitions.
            var thisOriginalDefinition = this.OriginalDefinition;
            var otherOriginalDefinition = other.OriginalDefinition;

            bool thisIsOriginalDefinition = ((object)this == (object)thisOriginalDefinition);
            bool otherIsOriginalDefinition = ((object)other == (object)otherOriginalDefinition);

            if (thisIsOriginalDefinition && otherIsOriginalDefinition)
            {
                // If we continue, we either return false, or get into a cycle.
                return false;
            }

            if ((thisIsOriginalDefinition || otherIsOriginalDefinition) &&
                (comparison & (TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.AllNullableIgnoreOptions | TypeCompareKind.IgnoreTupleNames)) == 0)
            {
                return false;
            }

            // CONSIDER: original definitions are not unique for missing metadata type
            // symbols.  Therefore this code may not behave correctly if 'this' is List<int>
            // where List`1 is a missing metadata type symbol, and other is similarly List<int>
            // but for a reference-distinct List`1.
            if (!Equals(thisOriginalDefinition, otherOriginalDefinition, comparison))
            {
                return false;
            }

            // The checks above are supposed to handle the vast majority of cases.
            // More complicated cases are handled in a special helper to make the common case scenario simple/fast (fewer locals and smaller stack frame)
            return EqualsComplicatedCases(other, comparison);
        }

        /// <summary>
        /// Helper for more complicated cases of Equals like when we have generic instantiations or types nested within them.
        /// </summary>
        private bool EqualsComplicatedCases(NamedTypeSymbol other, TypeCompareKind comparison)
        {
            if ((object)this.ContainingType != null &&
                !this.ContainingType.Equals(other.ContainingType, comparison))
            {
                return false;
            }

            var thisIsNotConstructed = ReferenceEquals(ConstructedFrom, this);
            var otherIsNotConstructed = ReferenceEquals(other.ConstructedFrom, other);

            if (thisIsNotConstructed && otherIsNotConstructed)
            {
                // Note that the arguments might appear different here due to alpha-renaming.  For example, given
                // class A<T> { class B<U> {} }
                // The type A<int>.B<int> is "constructed from" A<int>.B<1>, which may be a distinct type object
                // with a different alpha-renaming of B's type parameter every time that type expression is bound,
                // but these should be considered the same type each time.
                return true;
            }

            if (this.IsUnboundGenericType != other.IsUnboundGenericType)
            {
                return false;
            }

            if ((thisIsNotConstructed || otherIsNotConstructed) &&
                 (comparison & (TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.AllNullableIgnoreOptions | TypeCompareKind.IgnoreTupleNames)) == 0)
            {
                return false;
            }

            var typeArguments = this.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;
            var otherTypeArguments = other.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;
            int count = typeArguments.Length;

            // since both are constructed from the same (original) type, they must have the same arity
            Debug.Assert(count == otherTypeArguments.Length);

            for (int i = 0; i < count; i++)
            {
                var typeArgument = typeArguments[i];
                var otherTypeArgument = otherTypeArguments[i];
                if (!typeArgument.Equals(otherTypeArgument, comparison))
                {
                    return false;
                }
            }

            if (this.IsTupleType && !tupleNamesEquals(other, comparison))
            {
                return false;
            }

            return true;

            bool tupleNamesEquals(NamedTypeSymbol other, TypeCompareKind comparison)
            {
                // Make sure field names are the same.
                if ((comparison & TypeCompareKind.IgnoreTupleNames) == 0)
                {
                    var elementNames = TupleElementNames;
                    var otherElementNames = other.TupleElementNames;
                    return elementNames.IsDefault ? otherElementNames.IsDefault : !otherElementNames.IsDefault && elementNames.SequenceEqual(otherElementNames);
                }

                return true;
            }
        }

        internal override void AddNullableTransforms(ArrayBuilder<byte> transforms)
        {
            ContainingType?.AddNullableTransforms(transforms);

            foreach (TypeWithAnnotations arg in this.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics)
            {
                arg.AddNullableTransforms(transforms);
            }
        }

        internal override bool ApplyNullableTransforms(byte defaultTransformFlag, ImmutableArray<byte> transforms, ref int position, out TypeSymbol result)
        {
            if (!IsGenericType)
            {
                result = this;
                return true;
            }

            var allTypeArguments = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            GetAllTypeArgumentsNoUseSiteDiagnostics(allTypeArguments);

            bool haveChanges = false;
            for (int i = 0; i < allTypeArguments.Count; i++)
            {
                TypeWithAnnotations oldTypeArgument = allTypeArguments[i];
                TypeWithAnnotations newTypeArgument;
                if (!oldTypeArgument.ApplyNullableTransforms(defaultTransformFlag, transforms, ref position, out newTypeArgument))
                {
                    allTypeArguments.Free();
                    result = this;
                    return false;
                }
                else if (!oldTypeArgument.IsSameAs(newTypeArgument))
                {
                    allTypeArguments[i] = newTypeArgument;
                    haveChanges = true;
                }
            }

            result = haveChanges ? this.WithTypeArguments(allTypeArguments.ToImmutable()) : this;
            allTypeArguments.Free();
            return true;
        }

        internal override TypeSymbol SetNullabilityForReferenceTypes(Func<TypeWithAnnotations, TypeWithAnnotations> transform)
        {
            if (!IsGenericType)
            {
                return this;
            }

            var allTypeArguments = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            GetAllTypeArgumentsNoUseSiteDiagnostics(allTypeArguments);

            bool haveChanges = false;
            for (int i = 0; i < allTypeArguments.Count; i++)
            {
                TypeWithAnnotations oldTypeArgument = allTypeArguments[i];
                TypeWithAnnotations newTypeArgument = transform(oldTypeArgument);
                if (!oldTypeArgument.IsSameAs(newTypeArgument))
                {
                    allTypeArguments[i] = newTypeArgument;
                    haveChanges = true;
                }
            }

            NamedTypeSymbol result = haveChanges ? this.WithTypeArguments(allTypeArguments.ToImmutable()) : this;
            allTypeArguments.Free();
            return result;
        }

        internal NamedTypeSymbol WithTypeArguments(ImmutableArray<TypeWithAnnotations> allTypeArguments)
        {
            var definition = this.OriginalDefinition;
            TypeMap substitution = new TypeMap(definition.GetAllTypeParameters(), allTypeArguments);
            return substitution.SubstituteNamedType(definition).WithTupleDataFrom(this);
        }

        internal override TypeSymbol MergeEquivalentTypes(TypeSymbol other, VarianceKind variance)
        {
            Debug.Assert(this.Equals(other, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));

            if (!IsGenericType)
            {
                return other.IsDynamic() ? other : this;
            }

            var allTypeParameters = ArrayBuilder<TypeParameterSymbol>.GetInstance();
            var allTypeArguments = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            bool haveChanges = MergeEquivalentTypeArguments(this, (NamedTypeSymbol)other, variance, allTypeParameters, allTypeArguments);

            NamedTypeSymbol result;
            if (haveChanges)
            {
                TypeMap substitution = new TypeMap(allTypeParameters.ToImmutable(), allTypeArguments.ToImmutable());
                result = substitution.SubstituteNamedType(this.OriginalDefinition);
            }
            else
            {
                result = this;
            }

            allTypeArguments.Free();
            allTypeParameters.Free();

            return IsTupleType ? MergeTupleNames((NamedTypeSymbol)other, result) : result;
        }

        /// <summary>
        /// Merges nullability of all type arguments from the `typeA` and `typeB`.
        /// The type parameters are added to `allTypeParameters`; the merged
        /// type arguments are added to `allTypeArguments`; and the method
        /// returns true if there were changes from the original `typeA`.
        /// </summary>
        private static bool MergeEquivalentTypeArguments(
            NamedTypeSymbol typeA,
            NamedTypeSymbol typeB,
            VarianceKind variance,
            ArrayBuilder<TypeParameterSymbol> allTypeParameters,
            ArrayBuilder<TypeWithAnnotations> allTypeArguments)
        {
            Debug.Assert(typeA.IsGenericType);
            Debug.Assert(typeA.Equals(typeB, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));

            // Tuple types act as covariant when merging equivalent types.
            bool isTuple = typeA.IsTupleType;

            var definition = typeA.OriginalDefinition;
            bool haveChanges = false;

            while (true)
            {
                var typeParameters = definition.TypeParameters;
                if (typeParameters.Length > 0)
                {
                    var typeArgumentsA = typeA.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;
                    var typeArgumentsB = typeB.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;
                    allTypeParameters.AddRange(typeParameters);
                    for (int i = 0; i < typeArgumentsA.Length; i++)
                    {
                        TypeWithAnnotations typeArgumentA = typeArgumentsA[i];
                        TypeWithAnnotations typeArgumentB = typeArgumentsB[i];
                        VarianceKind typeArgumentVariance = GetTypeArgumentVariance(variance, isTuple ? VarianceKind.Out : typeParameters[i].Variance);
                        TypeWithAnnotations merged = typeArgumentA.MergeEquivalentTypes(typeArgumentB, typeArgumentVariance);
                        allTypeArguments.Add(merged);
                        if (!typeArgumentA.IsSameAs(merged))
                        {
                            haveChanges = true;
                        }
                    }
                }
                definition = definition.ContainingType;
                if (definition is null)
                {
                    break;
                }
                typeA = typeA.ContainingType;
                typeB = typeB.ContainingType;
                variance = VarianceKind.None;
            }

            return haveChanges;
        }

        private static VarianceKind GetTypeArgumentVariance(VarianceKind typeVariance, VarianceKind typeParameterVariance)
        {
            switch (typeVariance)
            {
                case VarianceKind.In:
                    switch (typeParameterVariance)
                    {
                        case VarianceKind.In:
                            return VarianceKind.Out;
                        case VarianceKind.Out:
                            return VarianceKind.In;
                        default:
                            return VarianceKind.None;
                    }
                case VarianceKind.Out:
                    return typeParameterVariance;
                default:
                    return VarianceKind.None;
            }
        }

        /// <summary>
        /// Returns a constructed type given its type arguments.
        /// </summary>
        /// <param name="typeArguments">The immediate type arguments to be replaced for type
        /// parameters in the type.</param>
        public NamedTypeSymbol Construct(params TypeSymbol[] typeArguments)
        {
            // https://github.com/dotnet/roslyn/issues/30064: We should fix the callers to pass TypeWithAnnotations[] instead of TypeSymbol[].
            return ConstructWithoutModifiers(typeArguments.AsImmutableOrNull(), false);
        }

        /// <summary>
        /// Returns a constructed type given its type arguments.
        /// </summary>
        /// <param name="typeArguments">The immediate type arguments to be replaced for type
        /// parameters in the type.</param>
        public NamedTypeSymbol Construct(ImmutableArray<TypeSymbol> typeArguments)
        {
            // https://github.com/dotnet/roslyn/issues/30064: We should fix the callers to pass ImmutableArray<TypeWithAnnotations> instead of ImmutableArray<TypeSymbol>.
            return ConstructWithoutModifiers(typeArguments, false);
        }

        /// <summary>
        /// Returns a constructed type given its type arguments.
        /// </summary>
        /// <param name="typeArguments"></param>
        public NamedTypeSymbol Construct(IEnumerable<TypeSymbol> typeArguments)
        {
            // https://github.com/dotnet/roslyn/issues/30064: We should fix the callers to pass IEnumerable<TypeWithAnnotations> instead of IEnumerable<TypeSymbol>.
            return ConstructWithoutModifiers(typeArguments.AsImmutableOrNull(), false);
        }

        /// <summary>
        /// Returns an unbound generic type of this named type.
        /// </summary>
        public NamedTypeSymbol ConstructUnboundGenericType()
        {
            return OriginalDefinition.AsUnboundGenericType();
        }

        internal NamedTypeSymbol GetUnboundGenericTypeOrSelf()
        {
            if (!this.IsGenericType)
            {
                return this;
            }

            return this.ConstructUnboundGenericType();
        }

        /// <summary>
        /// Gets a value indicating whether this type has an EmbeddedAttribute or not.
        /// </summary>
        internal abstract bool HasCodeAnalysisEmbeddedAttribute { get; }

        internal abstract bool HasAsyncMethodBuilderAttribute(out TypeSymbol builderArgument);

        internal abstract bool HasCompilerLoweringPreserveAttribute { get; }

        /// <summary>
        /// Gets a value indicating whether this type has System.Runtime.CompilerServices.InterpolatedStringHandlerAttribute or not.
        /// </summary>
        internal abstract bool IsInterpolatedStringHandlerType { get; }

        internal static readonly Func<TypeWithAnnotations, bool> TypeWithAnnotationsIsNullFunction = type => !type.HasType;

        internal static readonly Func<TypeWithAnnotations, bool> TypeWithAnnotationsIsErrorType = type => type.HasType && type.Type.IsErrorType();

        private NamedTypeSymbol ConstructWithoutModifiers(ImmutableArray<TypeSymbol> typeArguments, bool unbound)
        {
            ImmutableArray<TypeWithAnnotations> modifiedArguments;

            if (typeArguments.IsDefault)
            {
                modifiedArguments = default(ImmutableArray<TypeWithAnnotations>);
            }
            else
            {
                modifiedArguments = typeArguments.SelectAsArray(t => TypeWithAnnotations.Create(t));
            }

            return Construct(modifiedArguments, unbound);
        }

        internal NamedTypeSymbol Construct(ImmutableArray<TypeWithAnnotations> typeArguments)
        {
            return Construct(typeArguments, unbound: false);
        }

        internal NamedTypeSymbol Construct(ImmutableArray<TypeWithAnnotations> typeArguments, bool unbound)
        {
            if (!ReferenceEquals(this, ConstructedFrom))
            {
                throw new InvalidOperationException(CSharpResources.CannotCreateConstructedFromConstructed);
            }

            if (this.Arity == 0)
            {
                throw new InvalidOperationException(CSharpResources.CannotCreateConstructedFromNongeneric);
            }

            if (typeArguments.IsDefault)
            {
                throw new ArgumentNullException(nameof(typeArguments));
            }

            if (typeArguments.Any(TypeWithAnnotationsIsNullFunction))
            {
                throw new ArgumentException(CSharpResources.TypeArgumentCannotBeNull, nameof(typeArguments));
            }

            if (typeArguments.Length != this.Arity)
            {
                throw new ArgumentException(CSharpResources.WrongNumberOfTypeArguments, nameof(typeArguments));
            }

            Debug.Assert(!unbound || typeArguments.All(TypeWithAnnotationsIsErrorType));

            if (ConstructedNamedTypeSymbol.TypeParametersMatchTypeArguments(this.TypeParameters, typeArguments))
            {
                return this;
            }

            return this.ConstructCore(typeArguments, unbound);
        }

        protected virtual NamedTypeSymbol ConstructCore(ImmutableArray<TypeWithAnnotations> typeArguments, bool unbound)
        {
            return new ConstructedNamedTypeSymbol(this, typeArguments, unbound);
        }

        /// <summary>
        /// True if this type or some containing type has type parameters.
        /// </summary>
        public bool IsGenericType
        {
            get
            {
                for (var current = this; !ReferenceEquals(current, null); current = current.ContainingType)
                {
                    if (current.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Length != 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// True if this is a reference to an <em>unbound</em> generic type.  These occur only
        /// within a <c>typeof</c> expression.  A generic type is considered <em>unbound</em>
        /// if all of the type argument lists in its fully qualified name are empty.
        /// Note that the type arguments of an unbound generic type will be returned as error
        /// types because they do not really have type arguments.  An unbound generic type
        /// yields null for its BaseType and an empty result for its Interfaces.
        /// </summary>
        public virtual bool IsUnboundGenericType
        {
            get
            {
                return false;
            }
        }

        // Given C<int>.D<string, double>, yields { int, string, double }
        internal void GetAllTypeArguments(ref TemporaryArray<TypeSymbol> builder, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            var outer = ContainingType;
            if (!ReferenceEquals(outer, null))
            {
                outer.GetAllTypeArguments(ref builder, ref useSiteInfo);
            }

            foreach (var argument in TypeArgumentsWithDefinitionUseSiteDiagnostics(ref useSiteInfo))
            {
                builder.Add(argument.Type);
            }
        }

        internal ImmutableArray<TypeWithAnnotations> GetAllTypeArguments(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            ArrayBuilder<TypeWithAnnotations> builder = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            GetAllTypeArguments(builder, ref useSiteInfo);
            return builder.ToImmutableAndFree();
        }

        internal void GetAllTypeArguments(ArrayBuilder<TypeWithAnnotations> builder, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            var outer = ContainingType;
            if (!ReferenceEquals(outer, null))
            {
                outer.GetAllTypeArguments(builder, ref useSiteInfo);
            }

            builder.AddRange(TypeArgumentsWithDefinitionUseSiteDiagnostics(ref useSiteInfo));
        }

        internal void GetAllTypeArgumentsNoUseSiteDiagnostics(ArrayBuilder<TypeWithAnnotations> builder)
        {
            ContainingType?.GetAllTypeArgumentsNoUseSiteDiagnostics(builder);
            builder.AddRange(TypeArgumentsWithAnnotationsNoUseSiteDiagnostics);
        }

        internal int AllTypeArgumentCount()
        {
            int count = TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Length;

            var outer = ContainingType;
            if (!ReferenceEquals(outer, null))
            {
                count += outer.AllTypeArgumentCount();
            }

            return count;
        }

        internal ImmutableArray<TypeWithAnnotations> GetTypeParametersAsTypeArguments()
        {
            return TypeMap.TypeParametersAsTypeSymbolsWithAnnotations(this.TypeParameters);
        }

        /// <summary>
        /// The original definition of this symbol. If this symbol is constructed from another
        /// symbol by type substitution then OriginalDefinition gets the original symbol as it was defined in
        /// source or metadata.
        /// </summary>
        public new virtual NamedTypeSymbol OriginalDefinition
        {
            get
            {
                return this;
            }
        }

        protected sealed override TypeSymbol OriginalTypeSymbolDefinition
        {
            get
            {
                return this.OriginalDefinition;
            }
        }

        /// <summary>
        /// Returns the map from type parameters to type arguments.
        /// If this is not a generic type instantiation, returns null.
        /// The map targets the original definition of the type.
        /// </summary>
        internal virtual TypeMap TypeSubstitution
        {
            get { return null; }
        }

        internal virtual NamedTypeSymbol AsMember(NamedTypeSymbol newOwner)
        {
            Debug.Assert(this.IsDefinition);
            Debug.Assert(ReferenceEquals(newOwner.OriginalDefinition, this.ContainingSymbol.OriginalDefinition));
            return newOwner.IsDefinition ? this : new SubstitutedNestedTypeSymbol((SubstitutedNamedTypeSymbol)newOwner, this);
        }

        #region Use-Site Diagnostics

        internal override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            UseSiteInfo<AssemblySymbol> result = new UseSiteInfo<AssemblySymbol>(PrimaryDependency);

            if (this.IsDefinition)
            {
                return result;
            }

            // Check definition, type arguments 
            if (!DeriveUseSiteInfoFromType(ref result, this.OriginalDefinition))
            {
                DeriveUseSiteDiagnosticFromTypeArguments(ref result);
            }

            return result;
        }

        private bool DeriveUseSiteDiagnosticFromTypeArguments(ref UseSiteInfo<AssemblySymbol> result)
        {
            NamedTypeSymbol currentType = this;

            do
            {
                foreach (TypeWithAnnotations arg in currentType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics)
                {
                    if (DeriveUseSiteInfoFromType(ref result, arg, AllowedRequiredModifierType.None))
                    {
                        return true;
                    }
                }

                currentType = currentType.ContainingType;
            }
            while (currentType?.IsDefinition == false);

            return false;
        }

        internal DiagnosticInfo CalculateUseSiteDiagnostic()
        {
            DiagnosticInfo result = null;

            // Check base type.
            if (MergeUseSiteDiagnostics(ref result, DeriveUseSiteDiagnosticFromBase()))
            {
                return result;
            }

            // If we reach a type (Me) that is in an assembly with unified references, 
            // we check if that type definition depends on a type from a unified reference.
            if (this.ContainingModule.HasUnifiedReferences)
            {
                HashSet<TypeSymbol> unificationCheckedTypes = null;
                if (GetUnificationUseSiteDiagnosticRecursive(ref result, this, ref unificationCheckedTypes))
                {
                    return result;
                }
            }

            return result;
        }

        private DiagnosticInfo DeriveUseSiteDiagnosticFromBase()
        {
            NamedTypeSymbol @base = this.BaseTypeNoUseSiteDiagnostics;

            while ((object)@base != null)
            {
                if (@base.IsErrorType() && @base is NoPiaIllegalGenericInstantiationSymbol)
                {
                    return @base.GetUseSiteInfo().DiagnosticInfo;
                }

                @base = @base.BaseTypeNoUseSiteDiagnostics;
            }

            return null;
        }

        internal override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            if (!this.MarkCheckedIfNecessary(ref checkedTypes))
            {
                return false;
            }

            Debug.Assert(owner.ContainingModule.HasUnifiedReferences);
            if (owner.ContainingModule.GetUnificationUseSiteDiagnostic(ref result, this))
            {
                return true;
            }

            // We recurse into base types, interfaces and type *parameters* to check for
            // problems with constraints. We recurse into type *arguments* in the overload
            // in ConstructedNamedTypeSymbol.
            //
            // When we are binding a name with a nested type, Goo.Bar, then we ask for
            // use-site errors to be reported on both Goo and Goo.Bar. Therefore we should
            // not recurse into the containing type here; doing so will result in errors
            // being reported twice if Goo is bad.

            var @base = this.BaseTypeNoUseSiteDiagnostics;
            if ((object)@base != null && @base.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes))
            {
                return true;
            }

            return GetUnificationUseSiteDiagnosticRecursive(ref result, this.InterfacesNoUseSiteDiagnostics(), owner, ref checkedTypes) ||
                   GetUnificationUseSiteDiagnosticRecursive(ref result, this.TypeParameters, owner, ref checkedTypes);
        }

        #endregion

        /// <summary>
        /// True if the type itself is excluded from code coverage instrumentation.
        /// True for source types marked with <see cref="AttributeDescription.ExcludeFromCodeCoverageAttribute"/>.
        /// </summary>
        internal virtual bool IsDirectlyExcludedFromCodeCoverage { get => false; }

        /// <summary>
        /// True if this symbol has a special name (metadata flag SpecialName is set).
        /// </summary>
        internal abstract bool HasSpecialName { get; }

        /// <summary>
        /// Returns a flag indicating whether this symbol is ComImport.
        /// </summary>
        /// <remarks>
        /// A type can me marked as a ComImport type in source by applying the <see cref="System.Runtime.InteropServices.ComImportAttribute"/>
        /// </remarks>
        internal abstract bool IsComImport { get; }

        /// <summary>
        /// True if the type is a Windows runtime type.
        /// </summary>
        /// <remarks>
        /// A type can me marked as a Windows runtime type in source by applying the WindowsRuntimeImportAttribute.
        /// WindowsRuntimeImportAttribute is a pseudo custom attribute defined as an internal class in System.Runtime.InteropServices.WindowsRuntime namespace.
        /// This is needed to mark Windows runtime types which are redefined in mscorlib.dll and System.Runtime.WindowsRuntime.dll.
        /// These two assemblies are special as they implement the CLR's support for WinRT.
        /// </remarks>
        internal abstract bool IsWindowsRuntimeImport { get; }

        /// <summary>
        /// True if the type should have its WinRT interfaces projected onto .NET types and
        /// have missing .NET interface members added to the type.
        /// </summary>
        internal abstract bool ShouldAddWinRTMembers { get; }

        /// <summary>
        /// Returns a flag indicating whether this symbol has at least one applied/inherited conditional attribute.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        internal bool IsConditional
        {
            get
            {
                if (this.GetAppliedConditionalSymbols().Any())
                {
                    return true;
                }

                // Conditional attributes are inherited by derived types.
                var baseType = this.BaseTypeNoUseSiteDiagnostics;
                return (object)baseType != null ? baseType.IsConditional : false;
            }
        }

        /// <summary>
        /// True if the type is serializable (has Serializable metadata flag).
        /// </summary>
        public abstract bool IsSerializable { get; }

        /// <summary>
        /// Returns true if locals are to be initialized
        /// </summary>
        public abstract bool AreLocalsZeroed { get; }

        /// <summary>
        /// Type layout information (ClassLayout metadata and layout kind flags).
        /// </summary>
        internal abstract TypeLayout Layout { get; }

        /// <summary>
        /// The default charset used for type marshalling. 
        /// Can be changed via <see cref="DefaultCharSetAttribute"/> applied on the containing module.
        /// </summary>
        protected CharSet DefaultMarshallingCharSet
        {
            get
            {
                return this.GetEffectiveDefaultMarshallingCharSet() ?? CharSet.Ansi;
            }
        }

        /// <summary>
        /// Marshalling charset of string data fields within the type (string formatting flags in metadata).
        /// </summary>
        internal abstract CharSet MarshallingCharSet { get; }

        /// <summary>
        /// True if the type has declarative security information (HasSecurity flags).
        /// </summary>
        internal abstract bool HasDeclarativeSecurity { get; }

        /// <summary>
        /// Declaration security information associated with this type, or null if there is none.
        /// </summary>
        internal abstract IEnumerable<Cci.SecurityAttribute> GetSecurityInformation();

        /// <summary>
        /// Returns a sequence of preprocessor symbols specified in <see cref="ConditionalAttribute"/> applied on this symbol, or null if there are none.
        /// </summary>
        internal abstract ImmutableArray<string> GetAppliedConditionalSymbols();

        /// <summary>
        /// If <see cref="CoClassAttribute"/> was applied to the type and the attribute argument is a valid named type argument, i.e. accessible class type, then it returns the type symbol for the argument.
        /// Otherwise, returns null.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property invokes force completion of attributes. If you are accessing this property
        /// from the binder, make sure that we are not binding within an Attribute context.
        /// This could lead to a possible cycle in attribute binding.
        /// We can avoid this cycle by first checking if we are within the context of an Attribute argument,
        /// i.e. if(!binder.InAttributeArgument) { ...  namedType.ComImportCoClass ... }
        /// </para>
        /// <para>
        /// CONSIDER: We can remove the above restriction and possibility of cycle if we do an
        /// early binding of some well known attributes.
        /// </para>
        /// </remarks>
        internal virtual NamedTypeSymbol ComImportCoClass
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// If class represents fixed buffer, this property returns the FixedElementField
        /// </summary>
        internal virtual FieldSymbol FixedElementField
        {
            get
            {
                return null;
            }
        }

#nullable enable
        internal abstract bool HasCollectionBuilderAttribute(out TypeSymbol? builderType, out string? methodName);
#nullable disable

        /// <summary>
        /// Requires less computation than <see cref="TypeSymbol.TypeKind"/> == <see cref="TypeKind.Interface"/>.
        /// </summary>
        /// <remarks>
        /// Metadata types need to compute their base types in order to know their TypeKinds, and that can lead
        /// to cycles if base types are already being computed.
        /// </remarks>
        /// <returns>True if this is an interface type.</returns>
        internal abstract bool IsInterface { get; }

        /// <summary>
        /// Verify if the given type can be used to back a tuple type 
        /// and return cardinality of that tuple type in <paramref name="tupleCardinality"/>. 
        /// </summary>
        /// <param name="tupleCardinality">If method returns true, contains cardinality of the compatible tuple type.</param>
        /// <returns></returns>
        internal bool IsTupleTypeOfCardinality(out int tupleCardinality)
        {
            // Should this be optimized for perf (caching for VT<0> to VT<7>, etc.)?
            if (!IsUnboundGenericType &&
                ContainingSymbol?.Kind == SymbolKind.Namespace &&
                ContainingNamespace.ContainingNamespace?.IsGlobalNamespace == true &&
                Name == ValueTupleTypeName &&
                ContainingNamespace.Name == MetadataHelpers.SystemString)
            {
                int arity = Arity;

                if (arity >= 0 && arity < ValueTupleRestPosition)
                {
                    tupleCardinality = arity;
                    return true;
                }
                else if (arity == ValueTupleRestPosition && !IsDefinition)
                {
                    // Skip through "Rest" extensions
                    TypeSymbol typeToCheck = this;
                    int levelsOfNesting = 0;

                    do
                    {
                        levelsOfNesting++;
                        typeToCheck = ((NamedTypeSymbol)typeToCheck).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[ValueTupleRestPosition - 1].Type;
                    }
                    while (Equals(typeToCheck.OriginalDefinition, this.OriginalDefinition, TypeCompareKind.ConsiderEverything) && !typeToCheck.IsDefinition);

                    arity = (typeToCheck as NamedTypeSymbol)?.Arity ?? 0;

                    if (arity > 0 && arity < ValueTupleRestPosition && ((NamedTypeSymbol)typeToCheck).IsTupleTypeOfCardinality(out tupleCardinality))
                    {
                        Debug.Assert(tupleCardinality < ValueTupleRestPosition);
                        tupleCardinality += (ValueTupleRestPosition - 1) * levelsOfNesting;
                        return true;
                    }
                }
            }

            tupleCardinality = 0;
            return false;
        }

        /// <summary>
        /// Returns an instance of a symbol that represents a native integer
        /// if this underlying symbol represents System.IntPtr or System.UIntPtr.
        /// For platforms that support numeric IntPtr/UIntPtr, those types are returned as-is.
        /// For other symbols, throws <see cref="System.InvalidOperationException"/>.
        /// </summary>
        internal abstract NamedTypeSymbol AsNativeInteger();

        /// <summary>
        /// If this is a native integer, returns the symbol for the underlying type,
        /// either <see cref="System.IntPtr"/> or <see cref="System.UIntPtr"/>.
        /// Otherwise, returns null.
        /// </summary>
        internal abstract NamedTypeSymbol NativeIntegerUnderlyingType { get; }

        protected override ISymbol CreateISymbol()
        {
            return new PublicModel.NonErrorNamedTypeSymbol(this, DefaultNullableAnnotation);
        }

        protected override ITypeSymbol CreateITypeSymbol(CodeAnalysis.NullableAnnotation nullableAnnotation)
        {
            Debug.Assert(nullableAnnotation != DefaultNullableAnnotation);
            return new PublicModel.NonErrorNamedTypeSymbol(this, nullableAnnotation);
        }

        INamedTypeSymbolInternal INamedTypeSymbolInternal.EnumUnderlyingType
            => EnumUnderlyingType;

        ImmutableArray<ISymbolInternal> INamedTypeSymbolInternal.GetMembers()
            => GetMembers().CastArray<ISymbolInternal>();

        ImmutableArray<ISymbolInternal> INamedTypeSymbolInternal.GetMembers(string name)
            => GetMembers(name).CastArray<ISymbolInternal>();

    }
}
