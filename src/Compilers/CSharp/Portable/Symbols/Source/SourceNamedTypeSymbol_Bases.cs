// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class SourceNamedTypeSymbol
    {
        private Tuple<NamedTypeSymbol, ImmutableArray<NamedTypeSymbol>> _lazyDeclaredBases;

        private NamedTypeSymbol _lazyBaseType = ErrorTypeSymbol.UnknownResultType;
        private ImmutableArray<NamedTypeSymbol> _lazyInterfaces;

        /// <summary>
        /// Gets the BaseType of this type. If the base type could not be determined, then 
        /// an instance of ErrorType is returned. If this kind of type does not have a base type
        /// (for example, interfaces), null is returned. Also the special class System.Object
        /// always has a BaseType of null.
        /// </summary>
        internal sealed override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get
            {
                if (ReferenceEquals(_lazyBaseType, ErrorTypeSymbol.UnknownResultType))
                {
                    // force resolution of bases in containing type
                    // to make base resolution errors more deterministic
                    if ((object)ContainingType != null)
                    {
                        var tmp = ContainingType.BaseTypeNoUseSiteDiagnostics;
                    }

                    var diagnostics = BindingDiagnosticBag.GetInstance();
                    var acyclicBase = this.MakeAcyclicBaseType(diagnostics);
                    if (ReferenceEquals(Interlocked.CompareExchange(ref _lazyBaseType, acyclicBase, ErrorTypeSymbol.UnknownResultType), ErrorTypeSymbol.UnknownResultType))
                    {
                        AddDeclarationDiagnostics(diagnostics);
                    }
                    diagnostics.Free();
                }

                return _lazyBaseType;
            }
        }

        /// <summary>
        /// Gets the set of interfaces that this type directly implements. This set does not include
        /// interfaces that are base interfaces of directly implemented interfaces.
        /// </summary>
        internal sealed override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved)
        {
            if (_lazyInterfaces.IsDefault)
            {
                if (basesBeingResolved != null && basesBeingResolved.ContainsReference(this.OriginalDefinition))
                {
                    return ImmutableArray<NamedTypeSymbol>.Empty;
                }

                var diagnostics = BindingDiagnosticBag.GetInstance();
                var acyclicInterfaces = MakeAcyclicInterfaces(basesBeingResolved, diagnostics);
                if (ImmutableInterlocked.InterlockedCompareExchange(ref _lazyInterfaces, acyclicInterfaces, default(ImmutableArray<NamedTypeSymbol>)).IsDefault)
                {
                    AddDeclarationDiagnostics(diagnostics);
                }
                diagnostics.Free();
            }

            return _lazyInterfaces;
        }

        protected override void CheckBase(BindingDiagnosticBag diagnostics)
        {
            var localBase = this.BaseTypeNoUseSiteDiagnostics;

            if ((object)localBase == null)
            {
                // nothing to verify
                return;
            }

            Location baseLocation = null;
            bool baseContainsErrorTypes = localBase.ContainsErrorType();

            if (!baseContainsErrorTypes)
            {
                baseLocation = FindBaseRefSyntax(localBase);
                Debug.Assert(!this.IsClassType() || localBase.IsObjectType() || baseLocation != null);
            }

            // you need to know all bases before you can ask this question... (asking this causes a cycle)
            if (this.IsGenericType && !baseContainsErrorTypes && this.DeclaringCompilation.IsAttributeType(localBase))
            {
                MessageID.IDS_FeatureGenericAttributes.CheckFeatureAvailability(diagnostics, this.DeclaringCompilation, baseLocation);
            }

            // Check constraints on the first declaration with explicit bases.
            var singleDeclaration = this.FirstDeclarationWithExplicitBases();
            if (singleDeclaration != null)
            {
                var corLibrary = this.ContainingAssembly.CorLibrary;
                var conversions = new TypeConversions(corLibrary);
                var location = singleDeclaration.NameLocation;

                localBase.CheckAllConstraints(DeclaringCompilation, conversions, location, diagnostics);
            }

            // Records can only inherit from other records or object
            if (this.IsClassType() && !localBase.IsObjectType() && !baseContainsErrorTypes)
            {
                var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);

                if (declaration.Kind == DeclarationKind.Record)
                {
                    if (SynthesizedRecordClone.FindValidCloneMethod(localBase, ref useSiteInfo) is null)
                    {
                        diagnostics.Add(ErrorCode.ERR_BadRecordBase, baseLocation);
                    }
                }
                else if (SynthesizedRecordClone.FindValidCloneMethod(localBase, ref useSiteInfo) is object)
                {
                    diagnostics.Add(ErrorCode.ERR_BadInheritanceFromRecord, baseLocation);
                }

                diagnostics.Add(baseLocation, useSiteInfo);
            }
        }

        protected override void CheckInterfaces(BindingDiagnosticBag diagnostics)
        {
            // Check declared interfaces and all base interfaces. This is necessary
            // since references to all interfaces will be emitted to metadata
            // and it's possible to define derived interfaces with weaker
            // constraints than the base interfaces, at least in metadata.
            var interfaces = this.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics;

            if (interfaces.IsEmpty)
            {
                // nothing to verify
                return;
            }

            // Check constraints on the first declaration with explicit bases.
            var singleDeclaration = this.FirstDeclarationWithExplicitBases();
            if (singleDeclaration != null)
            {
                var corLibrary = this.ContainingAssembly.CorLibrary;
                var conversions = new TypeConversions(corLibrary);
                var location = singleDeclaration.NameLocation;

                foreach (var pair in interfaces)
                {
                    MultiDictionary<NamedTypeSymbol, NamedTypeSymbol>.ValueSet set = pair.Value;

                    foreach (var @interface in set)
                    {
                        @interface.CheckAllConstraints(DeclaringCompilation, conversions, location, diagnostics);
                    }

                    if (set.Count > 1)
                    {
                        NamedTypeSymbol other = pair.Key;
                        foreach (var @interface in set)
                        {
                            if ((object)other == @interface)
                            {
                                continue;
                            }

                            // InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics populates the set with interfaces that match by CLR signature.
                            Debug.Assert(!other.Equals(@interface, TypeCompareKind.ConsiderEverything));
                            Debug.Assert(other.Equals(@interface, TypeCompareKind.CLRSignatureCompareOptions));

                            if (other.Equals(@interface, TypeCompareKind.IgnoreNullableModifiersForReferenceTypes))
                            {
                                if (!other.Equals(@interface, TypeCompareKind.ObliviousNullableModifierMatchesAny))
                                {
                                    diagnostics.Add(ErrorCode.WRN_DuplicateInterfaceWithNullabilityMismatchInBaseList, location, @interface, this);
                                }
                            }
                            else if (other.Equals(@interface, TypeCompareKind.IgnoreTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes))
                            {
                                diagnostics.Add(ErrorCode.ERR_DuplicateInterfaceWithTupleNamesInBaseList, location, @interface, other, this);
                            }
                            else
                            {
                                diagnostics.Add(ErrorCode.ERR_DuplicateInterfaceWithDifferencesInBaseList, location, @interface, other, this);
                            }
                        }
                    }
                }
            }
        }

        // finds syntax location where given type was inherited
        // should be used for error reporting on unexpected inherited types.
        private SourceLocation FindBaseRefSyntax(NamedTypeSymbol baseSym)
        {
            foreach (var decl in this.declaration.Declarations)
            {
                BaseListSyntax bases = GetBaseListOpt(decl);
                if (bases != null)
                {
                    var baseBinder = this.DeclaringCompilation.GetBinder(bases);
                    // Wrap base binder in a location-specific binder that will avoid generic constraint checks.
                    baseBinder = baseBinder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks, this);

                    foreach (var baseTypeSyntax in bases.Types)
                    {
                        var b = baseTypeSyntax.Type;
                        var curBaseSym = baseBinder.BindType(b, BindingDiagnosticBag.Discarded).Type;

                        if (baseSym.Equals(curBaseSym))
                        {
                            return new SourceLocation(b);
                        }
                    }
                }
            }

            return null;
        }

        // Returns the first declaration in the merged declarations list that includes
        // base types or interfaces. Returns null if there are no such declarations.
        private SingleTypeDeclaration FirstDeclarationWithExplicitBases()
        {
            foreach (var singleDeclaration in this.declaration.Declarations)
            {
                var bases = GetBaseListOpt(singleDeclaration);
                if (bases != null)
                {
                    return singleDeclaration;
                }
            }

            return null;
        }

        internal Tuple<NamedTypeSymbol, ImmutableArray<NamedTypeSymbol>> GetDeclaredBases(ConsList<TypeSymbol> basesBeingResolved)
        {
            if (ReferenceEquals(_lazyDeclaredBases, null))
            {
                var diagnostics = BindingDiagnosticBag.GetInstance();
                if (Interlocked.CompareExchange(ref _lazyDeclaredBases, MakeDeclaredBases(basesBeingResolved, diagnostics), null) == null)
                {
                    AddDeclarationDiagnostics(diagnostics);
                }
                diagnostics.Free();
            }

            return _lazyDeclaredBases;
        }

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved)
        {
            return GetDeclaredBases(basesBeingResolved).Item1;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved)
        {
            return GetDeclaredBases(basesBeingResolved).Item2;
        }

        private Tuple<NamedTypeSymbol, ImmutableArray<NamedTypeSymbol>> MakeDeclaredBases(ConsList<TypeSymbol> basesBeingResolved, BindingDiagnosticBag diagnostics)
        {
            if (this.TypeKind == TypeKind.Enum)
            {
                // Handled by GetEnumUnderlyingType().
                return new Tuple<NamedTypeSymbol, ImmutableArray<NamedTypeSymbol>>(null, ImmutableArray<NamedTypeSymbol>.Empty);
            }

            var reportedPartialConflict = false;
            Debug.Assert(basesBeingResolved == null || !basesBeingResolved.ContainsReference(this.OriginalDefinition));
            var newBasesBeingResolved = basesBeingResolved.Prepend(this.OriginalDefinition);
            var baseInterfaces = ArrayBuilder<NamedTypeSymbol>.GetInstance();

            NamedTypeSymbol baseType = null;
            SourceLocation baseTypeLocation = null;

            var interfaceLocations = SpecializedSymbolCollections.GetPooledSymbolDictionaryInstance<NamedTypeSymbol, SourceLocation>();

            foreach (var decl in this.declaration.Declarations)
            {
                Tuple<NamedTypeSymbol, ImmutableArray<NamedTypeSymbol>> one = MakeOneDeclaredBases(newBasesBeingResolved, decl, diagnostics);
                if ((object)one == null) continue;

                var partBase = one.Item1;
                var partInterfaces = one.Item2;
                if (!reportedPartialConflict)
                {
                    if ((object)baseType == null)
                    {
                        baseType = partBase;
                        baseTypeLocation = decl.NameLocation;
                    }
                    else if (baseType.TypeKind == TypeKind.Error && (object)partBase != null)
                    {
                        // if the old base was an error symbol, copy it to the interfaces list so it doesn't get lost
                        partInterfaces = partInterfaces.Add(baseType);
                        baseType = partBase;
                        baseTypeLocation = decl.NameLocation;
                    }
                    else if ((object)partBase != null && !TypeSymbol.Equals(partBase, baseType, TypeCompareKind.ConsiderEverything) && partBase.TypeKind != TypeKind.Error)
                    {
                        // the parts do not agree
                        if (partBase.Equals(baseType, TypeCompareKind.ObliviousNullableModifierMatchesAny))
                        {
                            if (containsOnlyOblivious(baseType))
                            {
                                baseType = partBase;
                                baseTypeLocation = decl.NameLocation;
                                continue;
                            }
                            else if (containsOnlyOblivious(partBase))
                            {
                                continue;
                            }
                        }

                        var info = diagnostics.Add(ErrorCode.ERR_PartialMultipleBases, Locations[0], this);
                        baseType = new ExtendedErrorTypeSymbol(baseType, LookupResultKind.Ambiguous, info);
                        baseTypeLocation = decl.NameLocation;
                        reportedPartialConflict = true;

                        static bool containsOnlyOblivious(TypeSymbol type)
                        {
                            return TypeWithAnnotations.Create(type).VisitType(
                                type: null,
                                static (type, arg, flag) => !type.Type.IsValueType && !type.NullableAnnotation.IsOblivious(),
                                typePredicate: null,
                                arg: (object)null) is null;
                        }
                    }
                }

                foreach (var t in partInterfaces)
                {
                    if (!interfaceLocations.ContainsKey(t))
                    {
                        baseInterfaces.Add(t);
                        interfaceLocations.Add(t, decl.NameLocation);
                    }
                }
            }

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);

            if (declaration.Kind is DeclarationKind.Record or DeclarationKind.RecordStruct)
            {
                var type = DeclaringCompilation.GetWellKnownType(WellKnownType.System_IEquatable_T).Construct(this);
                if (baseInterfaces.IndexOf(type, SymbolEqualityComparer.AllIgnoreOptions) < 0)
                {
                    baseInterfaces.Add(type);
                    type.AddUseSiteInfo(ref useSiteInfo);
                }
            }

            if ((object)baseType != null)
            {
                Debug.Assert(baseTypeLocation != null);
                if (baseType.IsStatic)
                {
                    // '{1}': cannot derive from static class '{0}'
                    diagnostics.Add(ErrorCode.ERR_StaticBaseClass, baseTypeLocation, baseType, this);
                }

                if (!this.IsNoMoreVisibleThan(baseType, ref useSiteInfo))
                {
                    // Inconsistent accessibility: base class '{1}' is less accessible than class '{0}'
                    diagnostics.Add(ErrorCode.ERR_BadVisBaseClass, baseTypeLocation, this, baseType);
                }
            }

            var baseInterfacesRO = baseInterfaces.ToImmutableAndFree();
            if (DeclaredAccessibility != Accessibility.Private && IsInterface)
            {
                foreach (var i in baseInterfacesRO)
                {
                    if (!i.IsAtLeastAsVisibleAs(this, ref useSiteInfo))
                    {
                        // Inconsistent accessibility: base interface '{1}' is less accessible than interface '{0}'
                        diagnostics.Add(ErrorCode.ERR_BadVisBaseInterface, interfaceLocations[i], this, i);
                    }
                }
            }

            interfaceLocations.Free();

            diagnostics.Add(Locations[0], useSiteInfo);

            return new Tuple<NamedTypeSymbol, ImmutableArray<NamedTypeSymbol>>(baseType, baseInterfacesRO);
        }

        private static BaseListSyntax GetBaseListOpt(SingleTypeDeclaration decl)
        {
            if (decl.HasBaseDeclarations)
            {
                var typeDeclaration = (BaseTypeDeclarationSyntax)decl.SyntaxReference.GetSyntax();
                return typeDeclaration.BaseList;
            }

            return null;
        }

        // process the base list for one part of a partial class, or for the only part of any other type declaration.
        private Tuple<NamedTypeSymbol, ImmutableArray<NamedTypeSymbol>> MakeOneDeclaredBases(ConsList<TypeSymbol> newBasesBeingResolved, SingleTypeDeclaration decl, BindingDiagnosticBag diagnostics)
        {
            BaseListSyntax bases = GetBaseListOpt(decl);
            if (bases == null)
            {
                return null;
            }

            NamedTypeSymbol localBase = null;
            var localInterfaces = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            var baseBinder = this.DeclaringCompilation.GetBinder(bases);

            // Wrap base binder in a location-specific binder that will avoid generic constraint checks
            // (to avoid cycles if the constraint types are not bound yet). Instead, constraint checks
            // are handled by the caller.
            baseBinder = baseBinder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks, this);

            int i = -1;
            foreach (var baseTypeSyntax in bases.Types)
            {
                i++;
                var typeSyntax = baseTypeSyntax.Type;
                if (typeSyntax.Kind() != SyntaxKind.PredefinedType && !SyntaxFacts.IsName(typeSyntax.Kind()))
                {
                    diagnostics.Add(ErrorCode.ERR_BadBaseType, typeSyntax.GetLocation());
                }

                var location = new SourceLocation(typeSyntax);

                TypeSymbol baseType;

                if (i == 0 && TypeKind == TypeKind.Class) // allow class in the first position
                {
                    baseType = baseBinder.BindType(typeSyntax, diagnostics, newBasesBeingResolved).Type;

                    SpecialType baseSpecialType = baseType.SpecialType;
                    if (IsRestrictedBaseType(baseSpecialType))
                    {
                        // check for one of the specific exceptions required for compiling mscorlib
                        if (this.SpecialType == SpecialType.System_Enum && baseSpecialType == SpecialType.System_ValueType ||
                            this.SpecialType == SpecialType.System_MulticastDelegate && baseSpecialType == SpecialType.System_Delegate)
                        {
                            // allowed
                        }
                        else if (baseSpecialType == SpecialType.System_Array && this.ContainingAssembly.CorLibrary == this.ContainingAssembly)
                        {
                            // Specific exception for System.ArrayContracts, which is only built when CONTRACTS_FULL is defined.
                            // (See InheritanceResolver::CheckForBaseClassErrors).
                        }
                        else
                        {
                            // '{0}' cannot derive from special class '{1}'
                            diagnostics.Add(ErrorCode.ERR_DeriveFromEnumOrValueType, location, this, baseType);
                            continue;
                        }
                    }

                    if (baseType.IsSealed && !this.IsStatic) // Give precedence to ERR_StaticDerivedFromNonObject
                    {
                        diagnostics.Add(ErrorCode.ERR_CantDeriveFromSealedType, location, this, baseType);
                        continue;
                    }

                    bool baseTypeIsErrorWithoutInterfaceGuess = false;

                    // If baseType is an error symbol and our best guess is that the desired symbol
                    // is an interface, then put baseType in the interfaces list, rather than the
                    // base type slot, to avoid the frustrating scenario where an error message
                    // indicates that the symbol being returned as the base type was elsewhere
                    // interpreted as an interface.
                    if (baseType.TypeKind == TypeKind.Error)
                    {
                        baseTypeIsErrorWithoutInterfaceGuess = true;

                        TypeKind guessTypeKind = baseType.GetNonErrorTypeKindGuess();
                        if (guessTypeKind == TypeKind.Interface)
                        {
                            //base type is an error *with* a guessed interface
                            baseTypeIsErrorWithoutInterfaceGuess = false;
                        }
                    }

                    if ((baseType.TypeKind == TypeKind.Class ||
                         baseType.TypeKind == TypeKind.Delegate ||
                         baseType.TypeKind == TypeKind.Struct ||
                         baseTypeIsErrorWithoutInterfaceGuess) &&
                        ((object)localBase == null))
                    {
                        localBase = (NamedTypeSymbol)baseType;
                        Debug.Assert((object)localBase != null);
                        if (this.IsStatic && localBase.SpecialType != SpecialType.System_Object)
                        {
                            // Static class '{0}' cannot derive from type '{1}'. Static classes must derive from object.
                            var info = diagnostics.Add(ErrorCode.ERR_StaticDerivedFromNonObject, location, this, localBase);
                            localBase = new ExtendedErrorTypeSymbol(localBase, LookupResultKind.NotReferencable, info);
                        }
                        checkPrimaryConstructorBaseType(baseTypeSyntax, localBase);
                        continue;
                    }
                }
                else
                {
                    baseType = baseBinder.BindType(typeSyntax, diagnostics, newBasesBeingResolved).Type;
                }

                if (i == 0)
                {
                    checkPrimaryConstructorBaseType(baseTypeSyntax, baseType);
                }

                switch (baseType.TypeKind)
                {
                    case TypeKind.Interface:
                        foreach (var t in localInterfaces)
                        {
                            if (t.Equals(baseType, TypeCompareKind.ConsiderEverything))
                            {
                                diagnostics.Add(ErrorCode.ERR_DuplicateInterfaceInBaseList, location, baseType);
                            }
                            else if (t.Equals(baseType, TypeCompareKind.ObliviousNullableModifierMatchesAny))
                            {
                                // duplicates with ?/! differences are reported later, we report local differences between oblivious and ?/! here
                                diagnostics.Add(ErrorCode.WRN_DuplicateInterfaceWithNullabilityMismatchInBaseList, location, baseType, this);
                            }
                        }

                        if (this.IsStatic)
                        {
                            // '{0}': static classes cannot implement interfaces
                            diagnostics.Add(ErrorCode.ERR_StaticClassInterfaceImpl, location, this);
                        }

                        if (this.IsRefLikeType)
                        {
                            // '{0}': ref structs cannot implement interfaces
                            diagnostics.Add(ErrorCode.ERR_RefStructInterfaceImpl, location, this);
                        }

                        if (baseType.ContainsDynamic())
                        {
                            diagnostics.Add(ErrorCode.ERR_DeriveFromConstructedDynamic, location, this, baseType);
                        }

                        localInterfaces.Add((NamedTypeSymbol)baseType);
                        continue;

                    case TypeKind.Class:
                        if (TypeKind == TypeKind.Class)
                        {
                            if ((object)localBase == null)
                            {
                                localBase = (NamedTypeSymbol)baseType;
                                diagnostics.Add(ErrorCode.ERR_BaseClassMustBeFirst, location, baseType);
                                continue;
                            }
                            else
                            {
                                diagnostics.Add(ErrorCode.ERR_NoMultipleInheritance, location, this, localBase, baseType);
                                continue;
                            }
                        }
                        goto default;

                    case TypeKind.TypeParameter:
                        diagnostics.Add(ErrorCode.ERR_DerivingFromATyVar, location, baseType);
                        continue;

                    case TypeKind.Error:
                        // put the error type in the interface list so we don't lose track of it
                        localInterfaces.Add((NamedTypeSymbol)baseType);
                        continue;

                    case TypeKind.Dynamic:
                        diagnostics.Add(ErrorCode.ERR_DeriveFromDynamic, location, this);
                        continue;

                    case TypeKind.Submission:
                        throw ExceptionUtilities.UnexpectedValue(baseType.TypeKind);

                    default:
                        diagnostics.Add(ErrorCode.ERR_NonInterfaceInInterfaceList, location, baseType);
                        continue;
                }
            }

            if (this.SpecialType == SpecialType.System_Object && ((object)localBase != null || localInterfaces.Count != 0))
            {
                var name = GetName(bases.Parent);
                diagnostics.Add(ErrorCode.ERR_ObjectCantHaveBases, new SourceLocation(name));
            }

            return new Tuple<NamedTypeSymbol, ImmutableArray<NamedTypeSymbol>>(localBase, localInterfaces.ToImmutableAndFree());

            void checkPrimaryConstructorBaseType(BaseTypeSyntax baseTypeSyntax, TypeSymbol baseType)
            {
                if (baseTypeSyntax is PrimaryConstructorBaseTypeSyntax primaryConstructorBaseType &&
                    (!IsRecord || TypeKind != TypeKind.Class || baseType.TypeKind == TypeKind.Interface || ((RecordDeclarationSyntax)decl.SyntaxReference.GetSyntax()).ParameterList is null))
                {
                    diagnostics.Add(ErrorCode.ERR_UnexpectedArgumentList, primaryConstructorBaseType.ArgumentList.Location);
                }
            }
        }

        /// <summary>
        /// Returns true if the type cannot be used as an explicit base class.
        /// </summary>
        private static bool IsRestrictedBaseType(SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_Array:
                case SpecialType.System_Enum:
                case SpecialType.System_Delegate:
                case SpecialType.System_MulticastDelegate:
                case SpecialType.System_ValueType:
                    return true;
            }

            return false;
        }

        private ImmutableArray<NamedTypeSymbol> MakeAcyclicInterfaces(ConsList<TypeSymbol> basesBeingResolved, BindingDiagnosticBag diagnostics)
        {
            var typeKind = this.TypeKind;

            if (typeKind == TypeKind.Enum)
            {
                Debug.Assert(GetDeclaredInterfaces(basesBeingResolved: null).IsEmpty, "Computation skipped for enums");
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            var declaredInterfaces = GetDeclaredInterfaces(basesBeingResolved: basesBeingResolved);
            bool isInterface = (typeKind == TypeKind.Interface);

            ArrayBuilder<NamedTypeSymbol> result = isInterface ? ArrayBuilder<NamedTypeSymbol>.GetInstance() : null;
            foreach (var t in declaredInterfaces)
            {
                if (isInterface)
                {
                    if (BaseTypeAnalysis.TypeDependsOn(depends: t, on: this))
                    {
                        result.Add(new ExtendedErrorTypeSymbol(t, LookupResultKind.NotReferencable,
                            diagnostics.Add(ErrorCode.ERR_CycleInInterfaceInheritance, Locations[0], this, t)));
                        continue;
                    }
                    else
                    {
                        result.Add(t);
                    }
                }

                var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);

                if (t.DeclaringCompilation != this.DeclaringCompilation)
                {
                    t.AddUseSiteInfo(ref useSiteInfo);

                    foreach (var @interface in t.AllInterfacesNoUseSiteDiagnostics)
                    {
                        if (@interface.DeclaringCompilation != this.DeclaringCompilation)
                        {
                            @interface.AddUseSiteInfo(ref useSiteInfo);
                        }
                    }
                }

                diagnostics.Add(Locations[0], useSiteInfo);
            }

            return isInterface ? result.ToImmutableAndFree() : declaredInterfaces;
        }

        private NamedTypeSymbol MakeAcyclicBaseType(BindingDiagnosticBag diagnostics)
        {
            var typeKind = this.TypeKind;
            var compilation = this.DeclaringCompilation;
            NamedTypeSymbol declaredBase;
            if (typeKind == TypeKind.Enum)
            {
                Debug.Assert((object)GetDeclaredBaseType(basesBeingResolved: null) == null, "Computation skipped for enums");
                declaredBase = compilation.GetSpecialType(SpecialType.System_Enum);
            }
            else
            {
                declaredBase = GetDeclaredBaseType(basesBeingResolved: null);
            }

            if ((object)declaredBase == null)
            {
                switch (typeKind)
                {
                    case TypeKind.Class:

                        if (this.SpecialType == SpecialType.System_Object)
                        {
                            return null;
                        }

                        declaredBase = compilation.GetSpecialType(SpecialType.System_Object);
                        break;

                    case TypeKind.Struct:
                        declaredBase = compilation.GetSpecialType(SpecialType.System_ValueType);
                        break;

                    case TypeKind.Interface:
                        return null;

                    case TypeKind.Delegate:
                        declaredBase = compilation.GetSpecialType(SpecialType.System_MulticastDelegate);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(typeKind);
                }
            }

            if (BaseTypeAnalysis.TypeDependsOn(declaredBase, this))
            {
                return new ExtendedErrorTypeSymbol(declaredBase, LookupResultKind.NotReferencable,
                    diagnostics.Add(ErrorCode.ERR_CircularBase, Locations[0], declaredBase, this));
            }

            this.SetKnownToHaveNoDeclaredBaseCycles();

            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);
            NamedTypeSymbol current = declaredBase;

            do
            {
                if (current.DeclaringCompilation == this.DeclaringCompilation)
                {
                    break;
                }

                current.AddUseSiteInfo(ref useSiteInfo);
                current = current.BaseTypeNoUseSiteDiagnostics;
            }
            while ((object)current != null);

            diagnostics.Add(useSiteInfo.Diagnostics.IsNullOrEmpty() ? Location.None : (FindBaseRefSyntax(declaredBase) ?? Locations[0]), useSiteInfo);

            return declaredBase;
        }
    }
}
