// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Generic;

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

                    var diagnostics = DiagnosticBag.GetInstance();
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
        internal sealed override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved)
        {
            if (_lazyInterfaces.IsDefault)
            {
                if (basesBeingResolved != null && basesBeingResolved.ContainsReference(this.OriginalDefinition))
                {
                    return ImmutableArray<NamedTypeSymbol>.Empty;
                }

                var diagnostics = DiagnosticBag.GetInstance();
                var acyclicInterfaces = MakeAcyclicInterfaces(basesBeingResolved, diagnostics);
                if (ImmutableInterlocked.InterlockedCompareExchange(ref _lazyInterfaces, acyclicInterfaces, default(ImmutableArray<NamedTypeSymbol>)).IsDefault)
                {
                    AddDeclarationDiagnostics(diagnostics);
                }
                diagnostics.Free();
            }

            return _lazyInterfaces;
        }

        protected override void CheckBase(DiagnosticBag diagnostics)
        {
            var localBase = this.BaseTypeNoUseSiteDiagnostics;

            if ((object)localBase == null)
            {
                // nothing to verify
                return;
            }

            // you need to know all bases before you can ask this question... (asking this causes a cycle)
            if (this.IsGenericType && !localBase.IsErrorType() && this.DeclaringCompilation.IsAttributeType(localBase))
            {
                var baseLocation = FindBaseRefSyntax(localBase);
                Debug.Assert(baseLocation != null);

                // A generic type cannot derive from '{0}' because it is an attribute class
                diagnostics.Add(ErrorCode.ERR_GenericDerivingFromAttribute, baseLocation, localBase);
            }

            // Check constraints on the first declaration with explicit bases.
            var singleDeclaration = this.FirstDeclarationWithExplicitBases();
            if (singleDeclaration != null)
            {
                var corLibrary = this.ContainingAssembly.CorLibrary;
                var conversions = new TypeConversions(corLibrary);
                var location = singleDeclaration.NameLocation;

                localBase.CheckAllConstraints(conversions, location, diagnostics);
            }
        }

        protected override void CheckInterfaces(DiagnosticBag diagnostics)
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

                foreach (var @interface in interfaces)
                {
                    @interface.CheckAllConstraints(conversions, location, diagnostics);
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
                        var tmpDiag = DiagnosticBag.GetInstance();
                        var curBaseSym = baseBinder.BindType(b, tmpDiag).TypeSymbol;
                        tmpDiag.Free();

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

        internal Tuple<NamedTypeSymbol, ImmutableArray<NamedTypeSymbol>> GetDeclaredBases(ConsList<Symbol> basesBeingResolved)
        {
            if (ReferenceEquals(_lazyDeclaredBases, null))
            {
                DiagnosticBag diagnostics = DiagnosticBag.GetInstance();
                if (Interlocked.CompareExchange(ref _lazyDeclaredBases, MakeDeclaredBases(basesBeingResolved, diagnostics), null) == null)
                {
                    AddDeclarationDiagnostics(diagnostics);
                }

                diagnostics.Free();
            }

            return _lazyDeclaredBases;
        }

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<Symbol> basesBeingResolved)
        {
            return GetDeclaredBases(basesBeingResolved).Item1;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            return GetDeclaredBases(basesBeingResolved).Item2;
        }

        private Tuple<NamedTypeSymbol, ImmutableArray<NamedTypeSymbol>> MakeDeclaredBases(ConsList<Symbol> basesBeingResolved, DiagnosticBag diagnostics)
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
                    }
                    else if (baseType.TypeKind == TypeKind.Error && (object)partBase != null)
                    {
                        // if the old base was an error symbol, copy it to the interfaces list so it doesn't get lost
                        partInterfaces = partInterfaces.Add(baseType);
                        baseType = partBase;
                    }
                    else if ((object)partBase != null && partBase != baseType && partBase.TypeKind != TypeKind.Error)
                    {
                        // the parts do not agree
                        var info = diagnostics.Add(ErrorCode.ERR_PartialMultipleBases, Locations[0], this);
                        baseType = new ExtendedErrorTypeSymbol(baseType, LookupResultKind.Ambiguous, info);
                        reportedPartialConflict = true;
                    }
                }

                int n = baseInterfaces.Count;
                foreach (var t in partInterfaces) // this could probably be done more efficiently with a side hash table if it proves necessary
                {
                    for (int i = 0; i < n; i++)
                    {
                        if (t == baseInterfaces[i])
                        {
                            goto alreadyInInterfaceList;
                        }
                    }

                    baseInterfaces.Add(t);
                alreadyInInterfaceList:;
                }
            }

            if ((object)baseType != null && baseType.IsStatic)
            {
                // '{1}': cannot derive from static class '{0}'
                diagnostics.Add(ErrorCode.ERR_StaticBaseClass, Locations[0], baseType, this);
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            if ((object)baseType != null && !this.IsNoMoreVisibleThan(baseType, ref useSiteDiagnostics))
            {
                // Inconsistent accessibility: base class '{1}' is less accessible than class '{0}'
                diagnostics.Add(ErrorCode.ERR_BadVisBaseClass, Locations[0], this, baseType);
            }

            var baseInterfacesRO = baseInterfaces.ToImmutableAndFree();
            if (DeclaredAccessibility != Accessibility.Private && IsInterface)
            {
                foreach (var i in baseInterfacesRO)
                {
                    if (!i.IsAtLeastAsVisibleAs(this, ref useSiteDiagnostics))
                    {
                        // Inconsistent accessibility: base interface '{1}' is less accessible than interface '{0}'
                        diagnostics.Add(ErrorCode.ERR_BadVisBaseInterface, Locations[0], this, i);
                    }
                }
            }

            diagnostics.Add(Locations[0], useSiteDiagnostics);

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
        private Tuple<NamedTypeSymbol, ImmutableArray<NamedTypeSymbol>> MakeOneDeclaredBases(ConsList<Symbol> newBasesBeingResolved, SingleTypeDeclaration decl, DiagnosticBag diagnostics)
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
                var location = new SourceLocation(typeSyntax);

                TypeSymbol baseType;

                if (i == 0 && TypeKind == TypeKind.Class) // allow class in the first position
                {
                    baseType = baseBinder.BindType(typeSyntax, diagnostics, newBasesBeingResolved).TypeSymbol;

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
                            diagnostics.Add(ErrorCode.ERR_DeriveFromEnumOrValueType, Locations[0], this, baseType);
                            continue;
                        }
                    }

                    if (baseType.IsSealed && !this.IsStatic) // Give precedence to ERR_StaticDerivedFromNonObject
                    {
                        diagnostics.Add(ErrorCode.ERR_CantDeriveFromSealedType, Locations[0], this, baseType);
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
                        continue;
                    }
                }
                else
                {
                    baseType = baseBinder.BindType(typeSyntax, diagnostics, newBasesBeingResolved).TypeSymbol;
                }

                switch (baseType.TypeKind)
                {
                    case TypeKind.Interface:
                        foreach (var t in localInterfaces)
                        {
                            if (t == baseType)
                            {
                                diagnostics.Add(ErrorCode.ERR_DuplicateInterfaceInBaseList, location, baseType);
                                continue;
                            }
                        }

                        if (this.IsStatic)
                        {
                            // '{0}': static classes cannot implement interfaces
                            diagnostics.Add(ErrorCode.ERR_StaticClassInterfaceImpl, location, this, baseType);
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

        private ImmutableArray<NamedTypeSymbol> MakeAcyclicInterfaces(ConsList<Symbol> basesBeingResolved, DiagnosticBag diagnostics)
        {
            var typeKind = this.TypeKind;

            if (typeKind == TypeKind.Enum)
            {
                Debug.Assert(GetDeclaredInterfaces(basesBeingResolved: null).IsEmpty, "Computation skipped for enums");
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            var declaredInterfaces = GetDeclaredInterfaces(basesBeingResolved: basesBeingResolved);
            bool isClass = (typeKind == TypeKind.Class);

            ArrayBuilder<NamedTypeSymbol> result = isClass ? null : ArrayBuilder<NamedTypeSymbol>.GetInstance();
            foreach (var t in declaredInterfaces)
            {
                if (!isClass)
                {
                    if (BaseTypeAnalysis.InterfaceDependsOn(depends: t, on: this))
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

                HashSet<DiagnosticInfo> useSiteDiagnostics = null;

                if (t.DeclaringCompilation != this.DeclaringCompilation)
                {
                    t.AddUseSiteDiagnostics(ref useSiteDiagnostics);

                    foreach (var @interface in t.AllInterfacesNoUseSiteDiagnostics)
                    {
                        if (@interface.DeclaringCompilation != this.DeclaringCompilation)
                        {
                            @interface.AddUseSiteDiagnostics(ref useSiteDiagnostics);
                        }
                    }
                }

                if (!useSiteDiagnostics.IsNullOrEmpty())
                {
                    diagnostics.Add(Locations[0], useSiteDiagnostics);
                }
            }

            return isClass ? declaredInterfaces : result.ToImmutableAndFree();
        }

        private NamedTypeSymbol MakeAcyclicBaseType(DiagnosticBag diagnostics)
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

            if (BaseTypeAnalysis.ClassDependsOn(declaredBase, this))
            {
                return new ExtendedErrorTypeSymbol(declaredBase, LookupResultKind.NotReferencable,
                    diagnostics.Add(ErrorCode.ERR_CircularBase, Locations[0], declaredBase, this));
            }

            this.SetKnownToHaveNoDeclaredBaseCycles();

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            NamedTypeSymbol current = declaredBase;

            do
            {
                if (current.DeclaringCompilation == this.DeclaringCompilation)
                {
                    break;
                }

                current.AddUseSiteDiagnostics(ref useSiteDiagnostics);
                current = current.BaseTypeNoUseSiteDiagnostics;
            }
            while ((object)current != null);

            if (!useSiteDiagnostics.IsNullOrEmpty())
            {
                diagnostics.Add(FindBaseRefSyntax(declaredBase) ?? Locations[0], useSiteDiagnostics);
            }

            return declaredBase;
        }
    }
}
