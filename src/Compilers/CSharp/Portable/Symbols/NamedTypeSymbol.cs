// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a type other than an array, a pointer, a type parameter, and dynamic.
    /// </summary>
    internal abstract partial class NamedTypeSymbol : TypeSymbol, INamedTypeSymbol
    {
        private bool _hasNoBaseCycles;

        // Only the compiler can create NamedTypeSymbols.
        internal NamedTypeSymbol()
        {
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
        /// If nothing has been substituted for a give type parameters,
        /// then the type parameter itself is consider the type argument.
        /// </summary>
        internal abstract ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics { get; }

        internal ImmutableArray<TypeWithAnnotations> TypeArgumentsWithDefinitionUseSiteDiagnostics(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var result = TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;

            foreach (var typeArgument in result)
            {
                typeArgument.Type.OriginalDefinition.AddUseSiteDiagnostics(ref useSiteDiagnostics);
            }

            return result;
        }

        internal TypeWithAnnotations TypeArgumentWithDefinitionUseSiteDiagnostics(int index, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var result = TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[index];
            result.Type.OriginalDefinition.AddUseSiteDiagnostics(ref useSiteDiagnostics);
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
        internal virtual bool GetGuidString(out string guidString)
        {
            return GetGuidStringDefaultImplementation(out guidString);
        }

        /// <summary>
        /// For delegate types, gets the delegate's invoke method.  Returns null on
        /// all other kinds of types.  Note that it is possible to have an ill-formed
        /// delegate type imported from metadata which does not have an Invoke method.
        /// Such a type will be classified as a delegate but its DelegateInvokeMethod
        /// would be null.
        /// </summary>
        public MethodSymbol DelegateInvokeMethod
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

        /// <summary>
        /// Get the operators for this type by their metadata name
        /// </summary>
        internal ImmutableArray<MethodSymbol> GetOperators(string name)
        {
            ImmutableArray<Symbol> candidates = GetSimpleNonTypeMembers(name);
            if (candidates.IsEmpty)
            {
                return ImmutableArray<MethodSymbol>.Empty;
            }

            ArrayBuilder<MethodSymbol> operators = ArrayBuilder<MethodSymbol>.GetInstance();
            foreach (MethodSymbol candidate in candidates.OfType<MethodSymbol>())
            {
                if (candidate.MethodKind == MethodKind.UserDefinedOperator || candidate.MethodKind == MethodKind.Conversion)
                {
                    operators.Add(candidate);
                }
            }

            return operators.ToImmutableAndFree();
        }

        /// <summary>
        /// Get the instance constructors for this type.
        /// </summary>
        public ImmutableArray<MethodSymbol> InstanceConstructors
        {
            get
            {
                return GetConstructors<MethodSymbol>(includeInstance: true, includeStatic: false);
            }
        }

        /// <summary>
        /// Get the static constructors for this type.
        /// </summary>
        public ImmutableArray<MethodSymbol> StaticConstructors
        {
            get
            {
                return GetConstructors<MethodSymbol>(includeInstance: false, includeStatic: true);
            }
        }

        /// <summary>
        /// Get the instance and static constructors for this type.
        /// </summary>
        public ImmutableArray<MethodSymbol> Constructors
        {
            get
            {
                return GetConstructors<MethodSymbol>(includeInstance: true, includeStatic: true);
            }
        }

        private ImmutableArray<TMethodSymbol> GetConstructors<TMethodSymbol>(bool includeInstance, bool includeStatic) where TMethodSymbol : class, IMethodSymbol
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
                return ImmutableArray<TMethodSymbol>.Empty;
            }

            ArrayBuilder<TMethodSymbol> constructors = ArrayBuilder<TMethodSymbol>.GetInstance();
            foreach (Symbol candidate in instanceCandidates)
            {
                if (candidate.Kind == SymbolKind.Method)
                {
                    TMethodSymbol method = candidate as TMethodSymbol;
                    Debug.Assert((object)method != null);
                    Debug.Assert(method.MethodKind == MethodKind.Constructor);
                    constructors.Add(method);
                }
            }
            foreach (Symbol candidate in staticCandidates)
            {
                if (candidate.Kind == SymbolKind.Method)
                {
                    TMethodSymbol method = candidate as TMethodSymbol;
                    Debug.Assert((object)method != null);
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

        internal void GetExtensionMethods(ArrayBuilder<MethodSymbol> methods, string nameOpt, int arity, LookupOptions options)
        {
            if (this.MightContainExtensionMethods)
            {
                DoGetExtensionMethods(methods, nameOpt, arity, options);
            }
        }

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

                        if ((thisParam.RefKind == RefKind.Ref && !thisParam.Type.IsValueType) ||
                            (thisParam.RefKind == RefKind.In && thisParam.Type.TypeKind != TypeKind.Struct))
                        {
                            // For ref and ref-readonly extension methods, receivers need to be of the correct types to be considered in lookup
                            continue;
                        }

                        Debug.Assert(method.MethodKind != MethodKind.ReducedExtension);
                        methods.Add(method);
                    }
                }
            }
        }

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

        internal override ManagedKind ManagedKind
        {
            get
            {
                // CONSIDER: we could cache this, but it's only expensive for non-special struct types
                // that are pointed to.  For now, only cache on SourceMemberContainerSymbol since it fits
                // nicely into the flags variable.
                return BaseTypeAnalysis.GetManagedKind(this);
            }
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

        /// <summary>
        /// Return the name including the metadata arity suffix.
        /// </summary>
        public override string MetadataName
        {
            get
            {
                return MangleName ? MetadataHelpers.ComposeAritySuffixedMetadataName(Name, Arity) : Name;
            }
        }

        /// <summary>
        /// Should the name returned by Name property be mangled with [`arity] suffix in order to get metadata name.
        /// Must return False for a type with Arity == 0.
        /// </summary>
        internal abstract bool MangleName
        {
            // Intentionally no default implementation to force consideration of appropriate implementation for each new subclass
            get;
        }

        /// <summary>
        /// Collection of names of members declared within this type.
        /// </summary>
        public abstract IEnumerable<string> MemberNames { get; }

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
        /// Get all the members of this symbol that are types that have a particular name, of any arity.
        /// </summary>
        /// <returns>An ImmutableArray containing all the types that are members of this symbol with the given name.
        /// If this symbol has no type members with this name,
        /// returns an empty ImmutableArray. Never returns null.</returns>
        public abstract override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name);

        /// <summary>
        /// Get all the members of this symbol that are types that have a particular name and arity
        /// </summary>
        /// <returns>An ImmutableArray containing all the types that are members of this symbol with the given name and arity.
        /// If this symbol has no type members with this name and arity,
        /// returns an empty ImmutableArray. Never returns null.</returns>
        public abstract override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity);

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
        internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverrideOpt = null)
        {
            Debug.Assert(!this.IsTupleType);

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

            if ((comparison & TypeCompareKind.IgnoreTupleNames) != 0)
            {
                // If ignoring tuple element names, compare underlying tuple types
                if (t2.IsTupleType)
                {
                    t2 = t2.TupleUnderlyingType;
                    if (this.Equals(t2, comparison, isValueTypeOverrideOpt)) return true;
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
                (comparison & (TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.AllNullableIgnoreOptions)) == 0)
            {
                return false;
            }

            // CONSIDER: original definitions are not unique for missing metadata type
            // symbols.  Therefore this code may not behave correctly if 'this' is List<int>
            // where List`1 is a missing metadata type symbol, and other is similarly List<int>
            // but for a reference-distinct List`1.
            if (!TypeSymbol.Equals(thisOriginalDefinition, otherOriginalDefinition, comparison, isValueTypeOverrideOpt))
            {
                return false;
            }

            // The checks above are supposed to handle the vast majority of cases.
            // More complicated cases are handled in a special helper to make the common case scenario simple/fast (fewer locals and smaller stack frame)
            return EqualsComplicatedCases(other, comparison, isValueTypeOverrideOpt);
        }

        /// <summary>
        /// Helper for more complicated cases of Equals like when we have generic instantiations or types nested within them.
        /// </summary>
        private bool EqualsComplicatedCases(NamedTypeSymbol other, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverrideOpt)
        {
            if ((object)this.ContainingType != null &&
                !this.ContainingType.Equals(other.ContainingType, comparison, isValueTypeOverrideOpt))
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
                 (comparison & (TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.AllNullableIgnoreOptions)) == 0)
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
                if (!typeArgument.Equals(otherTypeArgument, comparison, isValueTypeOverrideOpt))
                {
                    return false;
                }
            }

            return true;
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

            if (!haveChanges)
            {
                allTypeArguments.Free();
                result = this;
            }
            else
            {
                TypeMap substitution = new TypeMap(this.OriginalDefinition.GetAllTypeParameters(),
                                                   allTypeArguments.ToImmutableAndFree());

                result = substitution.SubstituteNamedType(this.OriginalDefinition);
            }

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

            TypeSymbol result = this;
            if (haveChanges)
            {
                var definition = this.OriginalDefinition;
                TypeMap substitution = new TypeMap(definition.GetAllTypeParameters(), allTypeArguments.ToImmutable());
                result = substitution.SubstituteNamedType(definition);
            }

            allTypeArguments.Free();
            return result;
        }

        internal override TypeSymbol MergeNullability(TypeSymbol other, VarianceKind variance)
        {
            Debug.Assert(this.Equals(other, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));

            if (!IsGenericType)
            {
                return this;
            }

            var allTypeParameters = ArrayBuilder<TypeParameterSymbol>.GetInstance();
            var allTypeArguments = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            bool haveChanges = MergeTypeArgumentNullability(this, (NamedTypeSymbol)other, variance, allTypeParameters, allTypeArguments);

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
            return result;
        }

        /// <summary>
        /// Merges nullability of all type arguments from the `typeA` and `typeB`.
        /// The type parameters are added to `allTypeParameters`; the merged
        /// type arguments are added to `allTypeArguments`; and the method
        /// returns true if there were changes from the original `typeA`.
        /// </summary>
        private static bool MergeTypeArgumentNullability(
            NamedTypeSymbol typeA,
            NamedTypeSymbol typeB,
            VarianceKind variance,
            ArrayBuilder<TypeParameterSymbol> allTypeParameters,
            ArrayBuilder<TypeWithAnnotations> allTypeArguments)
        {
            Debug.Assert(typeA.IsGenericType);
            Debug.Assert(typeA.Equals(typeB, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));

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
                        VarianceKind typeArgumentVariance = GetTypeArgumentVariance(variance, typeParameters[i].Variance);
                        TypeWithAnnotations merged = typeArgumentA.MergeNullability(typeArgumentB, typeArgumentVariance);
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
        internal void GetAllTypeArguments(ArrayBuilder<TypeSymbol> builder, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var outer = ContainingType;
            if (!ReferenceEquals(outer, null))
            {
                outer.GetAllTypeArguments(builder, ref useSiteDiagnostics);
            }

            foreach (var argument in TypeArgumentsWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
            {
                builder.Add(argument.Type);
            }
        }

        internal ImmutableArray<TypeWithAnnotations> GetAllTypeArguments(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            ArrayBuilder<TypeWithAnnotations> builder = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            GetAllTypeArguments(builder, ref useSiteDiagnostics);
            return builder.ToImmutableAndFree();
        }

        internal void GetAllTypeArguments(ArrayBuilder<TypeWithAnnotations> builder, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var outer = ContainingType;
            if (!ReferenceEquals(outer, null))
            {
                outer.GetAllTypeArguments(builder, ref useSiteDiagnostics);
            }

            builder.AddRange(TypeArgumentsWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics));
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

        protected override sealed TypeSymbol OriginalTypeSymbolDefinition
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

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (this.IsDefinition)
            {
                return base.GetUseSiteDiagnostic();
            }

            DiagnosticInfo result = null;

            // Check definition, type arguments 
            if (DeriveUseSiteDiagnosticFromType(ref result, this.OriginalDefinition) ||
                DeriveUseSiteDiagnosticFromTypeArguments(ref result))
            {
                return result;
            }

            return result;
        }

        private bool DeriveUseSiteDiagnosticFromTypeArguments(ref DiagnosticInfo result)
        {
            foreach (TypeWithAnnotations arg in this.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics)
            {
                if (DeriveUseSiteDiagnosticFromType(ref result, arg))
                {
                    return true;
                }
            }

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
                    return @base.GetUseSiteDiagnostic();
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
        public sealed override bool IsTupleCompatible(out int tupleCardinality)
        {
            if (IsTupleType)
            {
                tupleCardinality = 0;
                return false;
            }

            // Should this be optimized for perf (caching for VT<0> to VT<7>, etc.)?
            if (!IsUnboundGenericType &&
                ContainingSymbol?.Kind == SymbolKind.Namespace &&
                ContainingNamespace.ContainingNamespace?.IsGlobalNamespace == true &&
                Name == TupleTypeSymbol.TupleTypeName &&
                ContainingNamespace.Name == MetadataHelpers.SystemString)
            {
                int arity = Arity;

                if (arity > 0 && arity < TupleTypeSymbol.RestPosition)
                {
                    tupleCardinality = arity;
                    return true;
                }
                else if (arity == TupleTypeSymbol.RestPosition && !IsDefinition)
                {
                    // Skip through "Rest" extensions
                    TypeSymbol typeToCheck = this;
                    int levelsOfNesting = 0;

                    do
                    {
                        levelsOfNesting++;
                        typeToCheck = ((NamedTypeSymbol)typeToCheck).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[TupleTypeSymbol.RestPosition - 1].Type;
                    }
                    while (TypeSymbol.Equals(typeToCheck.OriginalDefinition, this.OriginalDefinition, TypeCompareKind.ConsiderEverything2) && !typeToCheck.IsDefinition);

                    if (typeToCheck.IsTupleType)
                    {
                        var underlying = typeToCheck.TupleUnderlyingType;
                        if (underlying.Arity == TupleTypeSymbol.RestPosition && !TypeSymbol.Equals(underlying.OriginalDefinition, this.OriginalDefinition, TypeCompareKind.ConsiderEverything2))
                        {
                            tupleCardinality = 0;
                            return false;
                        }

                        tupleCardinality = (TupleTypeSymbol.RestPosition - 1) * levelsOfNesting + typeToCheck.TupleElementTypesWithAnnotations.Length;
                        return true;
                    }

                    arity = (typeToCheck as NamedTypeSymbol)?.Arity ?? 0;

                    if (arity > 0 && arity < TupleTypeSymbol.RestPosition && typeToCheck.IsTupleCompatible(out tupleCardinality))
                    {
                        Debug.Assert(tupleCardinality < TupleTypeSymbol.RestPosition);
                        tupleCardinality += (TupleTypeSymbol.RestPosition - 1) * levelsOfNesting;
                        return true;
                    }
                }
            }

            tupleCardinality = 0;
            return false;
        }

        #region INamedTypeSymbol Members

        int INamedTypeSymbol.Arity
        {
            get
            {
                return this.Arity;
            }
        }

        ImmutableArray<IMethodSymbol> INamedTypeSymbol.InstanceConstructors
        {
            get
            {
                return this.GetConstructors<IMethodSymbol>(includeInstance: true, includeStatic: false);
            }
        }

        ImmutableArray<IMethodSymbol> INamedTypeSymbol.StaticConstructors
        {
            get
            {
                return this.GetConstructors<IMethodSymbol>(includeInstance: false, includeStatic: true);
            }
        }

        ImmutableArray<IMethodSymbol> INamedTypeSymbol.Constructors
        {
            get
            {
                return this.GetConstructors<IMethodSymbol>(includeInstance: true, includeStatic: true);
            }
        }

        IEnumerable<string> INamedTypeSymbol.MemberNames
        {
            get
            {
                return this.MemberNames;
            }
        }

        ImmutableArray<ITypeParameterSymbol> INamedTypeSymbol.TypeParameters
        {
            get
            {
                return StaticCast<ITypeParameterSymbol>.From(this.TypeParameters);
            }
        }

        ImmutableArray<ITypeSymbol> INamedTypeSymbol.TypeArguments
        {
            get
            {
                return this.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.SelectAsArray(a => (ITypeSymbol)a.Type);
            }
        }

        ImmutableArray<CodeAnalysis.NullableAnnotation> INamedTypeSymbol.TypeArgumentNullableAnnotations =>
            this.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.SelectAsArray(a => a.ToPublicAnnotation());

        ImmutableArray<CustomModifier> INamedTypeSymbol.GetTypeArgumentCustomModifiers(int ordinal)
        {
            return this.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[ordinal].CustomModifiers;
        }

        INamedTypeSymbol INamedTypeSymbol.OriginalDefinition
        {
            get { return this.OriginalDefinition; }
        }

        IMethodSymbol INamedTypeSymbol.DelegateInvokeMethod
        {
            get
            {
                return this.DelegateInvokeMethod;
            }
        }

        INamedTypeSymbol INamedTypeSymbol.EnumUnderlyingType
        {
            get
            {
                return this.EnumUnderlyingType;
            }
        }

        INamedTypeSymbol INamedTypeSymbol.ConstructedFrom
        {
            get
            {
                return this.ConstructedFrom;
            }
        }

        INamedTypeSymbol INamedTypeSymbol.Construct(params ITypeSymbol[] typeArguments)
        {
            return Construct(ConstructTypeArguments(typeArguments), unbound: false);
        }

        INamedTypeSymbol INamedTypeSymbol.Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<CodeAnalysis.NullableAnnotation> typeArgumentNullableAnnotations)
        {
            return Construct(ConstructTypeArguments(typeArguments, typeArgumentNullableAnnotations), unbound: false);
        }

        INamedTypeSymbol INamedTypeSymbol.ConstructUnboundGenericType()
        {
            return this.ConstructUnboundGenericType();
        }

        ISymbol INamedTypeSymbol.AssociatedSymbol
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Returns fields that represent tuple elements for types that are tuples.
        ///
        /// If this type is not a tuple, then returns default.
        /// </summary>
        ImmutableArray<IFieldSymbol> INamedTypeSymbol.TupleElements => StaticCast<IFieldSymbol>.From(this.TupleElements);

        /// <summary>
        /// If this is a tuple type symbol, returns the symbol for its underlying type.
        /// Otherwise, returns null.
        /// </summary>
        INamedTypeSymbol INamedTypeSymbol.TupleUnderlyingType => this.TupleUnderlyingType;

        bool INamedTypeSymbol.IsComImport => IsComImport;

        #endregion

        #region ISymbol Members

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitNamedType(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitNamedType(this);
        }

        #endregion
    }
}
