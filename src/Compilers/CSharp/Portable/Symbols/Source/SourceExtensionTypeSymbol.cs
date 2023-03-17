﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceExtensionTypeSymbol : SourceNamedTypeSymbol
    {
        private ExtensionInfo _lazyDeclaredExtensionInfo = ExtensionInfo.Sentinel;
        private TypeSymbol? _lazyExtensionUnderlyingType = ErrorTypeSymbol.UnknownResultType;
        private ImmutableArray<NamedTypeSymbol> _lazyBaseExtensions;

        internal SourceExtensionTypeSymbol(NamespaceOrTypeSymbol containingSymbol, MergedTypeDeclaration declaration, BindingDiagnosticBag diagnostics)
            : base(containingSymbol, declaration, diagnostics)
        {
            Debug.Assert(declaration.Kind == DeclarationKind.Extension);
        }

        private class ExtensionInfo
        {
            public readonly TypeSymbol? UnderlyingType;
            public readonly ImmutableArray<NamedTypeSymbol> BaseExtensions;

            internal static readonly ExtensionInfo Sentinel =
                new ExtensionInfo(underlyingType: null, baseExtensions: default);

            public ExtensionInfo(TypeSymbol? underlyingType, ImmutableArray<NamedTypeSymbol> baseExtensions)
            {
                UnderlyingType = underlyingType;
                BaseExtensions = baseExtensions;
            }
        }

        internal override bool IsExtension => true;

        protected override void CheckUnderlyingType(BindingDiagnosticBag diagnostics)
        {
            var underlyingType = this.ExtensionUnderlyingTypeNoUseSiteDiagnostics;

            if (underlyingType is null)
                return;

            var singleDeclaration = this.FirstDeclarationWithExplicitUnderlyingType();
            if (singleDeclaration != null)
            {
                var corLibrary = this.ContainingAssembly.CorLibrary;
                var conversions = new TypeConversions(corLibrary);
                var location = singleDeclaration.NameLocation;

                underlyingType.CheckAllConstraints(DeclaringCompilation, conversions, location, diagnostics);
            }
        }

        protected override void CheckBaseExtensions(BindingDiagnosticBag diagnostics)
        {
            // PROTOTYPE confirm and test this once extensions can be loaded from metadata
            // Check all base extensions. This is necessary
            // since references to all extensions will be emitted to metadata
            // and it's possible to define derived extensions with weaker
            // constraints than the base extensions, at least in metadata.
            var allBaseExtensions = this.GetExtensionsAndTheirBaseExtensionsNoUseSiteDiagnostics();
            if (allBaseExtensions.IsEmpty)
                return;

            var singleDeclaration = this.FirstDeclarationWithExplicitBases();
            Debug.Assert(singleDeclaration != null);

            var corLibrary = this.ContainingAssembly.CorLibrary;
            var conversions = new TypeConversions(corLibrary);
            var location = singleDeclaration.NameLocation;
            var underlyingType = this.ExtensionUnderlyingTypeNoUseSiteDiagnostics;

            foreach (var pair in allBaseExtensions)
            {
                NamedTypeSymbol referenceBaseExtension = pair.Key;
                MultiDictionary<NamedTypeSymbol, NamedTypeSymbol>.ValueSet allBaseExtensionPerCLRSignature = pair.Value;

                foreach (var baseExtension in allBaseExtensionPerCLRSignature)
                {
                    baseExtension.CheckAllConstraints(DeclaringCompilation, conversions, location, diagnostics);

                    // PROTOTYPE confirm what we allow in terms of variation between various underlying types
                    var baseUnderlyingType = baseExtension.ExtensionUnderlyingTypeNoUseSiteDiagnostics;
                    if (baseUnderlyingType?.Equals(underlyingType, TypeCompareKind.ConsiderEverything) == false)
                    {
                        diagnostics.Add(ErrorCode.ERR_UnderlyingTypesMismatch, location, this, underlyingType!, baseUnderlyingType);
                    }

                    if (!ReferenceEquals(referenceBaseExtension, baseExtension))
                    {
                        Debug.Assert(!referenceBaseExtension.Equals(baseExtension, TypeCompareKind.ConsiderEverything));
                        Debug.Assert(referenceBaseExtension.Equals(baseExtension, TypeCompareKind.CLRSignatureCompareOptions));

                        ReportDuplicate(referenceBaseExtension, baseExtension, location, diagnostics, forBaseExtension: true);
                    }
                }
            }
        }

        // PROTOTYPE consider caching and using a property like InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics
        private MultiDictionary<NamedTypeSymbol, NamedTypeSymbol> GetExtensionsAndTheirBaseExtensionsNoUseSiteDiagnostics()
        {
            var baseExtensions = this.BaseExtensionsNoUseSiteDiagnostics;
            var resultBuilder = new MultiDictionary<NamedTypeSymbol, NamedTypeSymbol>(baseExtensions.Length,
                SymbolEqualityComparer.CLRSignature, SymbolEqualityComparer.ConsiderEverything);

            foreach (var baseExtension in baseExtensions)
            {
                if (resultBuilder.Add(baseExtension, baseExtension))
                {
                    // PROTOTYPE we need to collect all base extensions from baseExtension too
                }
            }

            return resultBuilder;
        }

        internal sealed override TypeSymbol? GetDeclaredExtensionUnderlyingType()
            => GetDeclaredExtensionInfo().UnderlyingType;

        internal sealed override ImmutableArray<NamedTypeSymbol> GetDeclaredBaseExtensions()
            => GetDeclaredExtensionInfo().BaseExtensions;

        internal sealed override TypeSymbol? ExtensionUnderlyingTypeNoUseSiteDiagnostics
        {
            get
            {
                if (ReferenceEquals(_lazyExtensionUnderlyingType, ErrorTypeSymbol.UnknownResultType))
                {
                    var diagnostics = BindingDiagnosticBag.GetInstance();
                    TypeSymbol? acyclicBase = makeAcyclicUnderlyingType(diagnostics);

                    if (ReferenceEquals(Interlocked.CompareExchange(ref _lazyExtensionUnderlyingType, acyclicBase, ErrorTypeSymbol.UnknownResultType),
                            ErrorTypeSymbol.UnknownResultType))
                    {
                        AddDeclarationDiagnostics(diagnostics);
                    }

                    diagnostics.Free();
                }

                return _lazyExtensionUnderlyingType;

                TypeSymbol? makeAcyclicUnderlyingType(BindingDiagnosticBag diagnostics)
                {
                    TypeSymbol? declaredUnderlyingType = GetDeclaredExtensionInfo().UnderlyingType;

                    if (declaredUnderlyingType is null)
                    {
                        return null;
                    }

                    if (BaseTypeAnalysis.TypeDependsOn(depends: declaredUnderlyingType, on: this))
                    {
                        return new ExtendedErrorTypeSymbol(declaredUnderlyingType, LookupResultKind.NotReferencable,
                            diagnostics.Add(ErrorCode.ERR_CircularBase, Locations[0], declaredUnderlyingType, this));
                    }

                    var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);
                    var current = declaredUnderlyingType;
                    do
                    {
                        if (ReferenceEquals(current.DeclaringCompilation, this.DeclaringCompilation))
                        {
                            break;
                        }

                        current.AddUseSiteInfo(ref useSiteInfo);
                        current = current.BaseTypeNoUseSiteDiagnostics;
                    }
                    while (current is not null);

                    if (!useSiteInfo.Diagnostics.IsNullOrEmpty())
                    {
                        var location = FindUnderlyingTypeSyntax(declaredUnderlyingType) ?? Locations[0];
                        diagnostics.Add(location, useSiteInfo);
                    }

                    return declaredUnderlyingType;
                }
            }
        }

        private SourceLocation? FindUnderlyingTypeSyntax(TypeSymbol underlyingType)
        {
            foreach (var declaration in this.declaration.Declarations)
            {
                TypeSyntax? currentSyntax = GetUnderlyingTypeSyntax(declaration);
                if (currentSyntax != null)
                {
                    var baseBinder = this.DeclaringCompilation.GetBinder(currentSyntax);
                    // Wrap base binder in a location-specific binder that will avoid generic constraint checks.
                    baseBinder = baseBinder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks, this);

                    var currentType = baseBinder.BindType(currentSyntax, BindingDiagnosticBag.Discarded).Type;

                    if (underlyingType.Equals(currentType, TypeCompareKind.ConsiderEverything))
                    {
                        return new SourceLocation(currentSyntax);
                    }
                }
            }

            return null;
        }

        private SingleTypeDeclaration? FirstDeclarationWithExplicitUnderlyingType()
        {
            foreach (var singleDeclaration in this.declaration.Declarations)
            {
                var underlyingType = GetUnderlyingTypeSyntax(singleDeclaration);
                if (underlyingType != null)
                {
                    return singleDeclaration;
                }
            }

            return null;
        }

        static TypeSyntax? GetUnderlyingTypeSyntax(SingleTypeDeclaration decl)
        {
            var syntax = (ExtensionDeclarationSyntax)decl.SyntaxReference.GetSyntax();
            return syntax.ForUnderlyingType?.UnderlyingType;
        }

        internal override ImmutableArray<NamedTypeSymbol> BaseExtensionsNoUseSiteDiagnostics
        {
            get
            {
                if (_lazyBaseExtensions.IsDefault)
                {
                    var diagnostics = BindingDiagnosticBag.GetInstance();
                    var acyclicBaseExtensions = makeAcyclicBaseExtensions(diagnostics);

                    if (ImmutableInterlocked.InterlockedInitialize(ref _lazyBaseExtensions, acyclicBaseExtensions))
                    {
                        AddDeclarationDiagnostics(diagnostics);
                    }
                    diagnostics.Free();
                }

                return _lazyBaseExtensions;

                ImmutableArray<NamedTypeSymbol> makeAcyclicBaseExtensions(BindingDiagnosticBag diagnostics)
                {
                    ImmutableArray<NamedTypeSymbol> declaredBaseExtensions = GetDeclaredExtensionInfo().BaseExtensions;

                    var result = ArrayBuilder<NamedTypeSymbol>.GetInstance();
                    foreach (var declaredBaseExtension in declaredBaseExtensions)
                    {
                        if (BaseTypeAnalysis.TypeDependsOn(depends: declaredBaseExtension, on: this))
                        {
                            result.Add(new ExtendedErrorTypeSymbol(declaredBaseExtension, LookupResultKind.NotReferencable,
                                diagnostics.Add(ErrorCode.ERR_CycleInBaseExtensions, Locations[0], this, declaredBaseExtension)));
                            continue;
                        }

                        result.Add(declaredBaseExtension);

                        if (declaredBaseExtension.DeclaringCompilation != this.DeclaringCompilation)
                        {
                            // PROTOTYPE Validate use-site errors on base extensions once we can emit extensions to metadata
                            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);
                            declaredBaseExtension.AddUseSiteInfo(ref useSiteInfo);

                            foreach (var extension in declaredBaseExtension.BaseExtensionsNoUseSiteDiagnostics)
                            {
                                if (extension.DeclaringCompilation != this.DeclaringCompilation)
                                {
                                    extension.AddUseSiteInfo(ref useSiteInfo);
                                }
                            }
                            diagnostics.Add(Locations[0], useSiteInfo);
                        }
                    }

                    return result.ToImmutableAndFree();
                }
            }
        }

        private ExtensionInfo GetDeclaredExtensionInfo()
        {
            if (ReferenceEquals(_lazyDeclaredExtensionInfo, ExtensionInfo.Sentinel))
            {
                BindingDiagnosticBag diagnostics = BindingDiagnosticBag.GetInstance();

                var original = Interlocked.CompareExchange(ref _lazyDeclaredExtensionInfo,
                    this.MakeDeclaredExtensionInfo(basesBeingResolved: null, diagnostics),
                    ExtensionInfo.Sentinel);

                if (ReferenceEquals(original, ExtensionInfo.Sentinel))
                {
                    AddDeclarationDiagnostics(diagnostics);
                }

                diagnostics.Free();
            }

            return _lazyDeclaredExtensionInfo;
        }

        /// <summary> Bind the base extensions for all parts of an extension.</summary>
        private ExtensionInfo MakeDeclaredExtensionInfo(ConsList<TypeSymbol>? basesBeingResolved, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(this.IsExtension);

            bool isExplicit = false;

            bool sawUnderlyingType = false;
            bool reportedUnderlyingTypeConflict = false;
            TypeSymbol? underlyingType = null;
            SourceLocation? underlyingTypeLocation = null;

            Debug.Assert(basesBeingResolved == null || !basesBeingResolved.ContainsReference(this.OriginalDefinition));
            var newBasesBeingResolved = basesBeingResolved.Prepend(this.OriginalDefinition);
            var baseExtensionsBuilder = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            var baseExtensionLocations = SpecializedSymbolCollections.GetPooledSymbolDictionaryInstance<NamedTypeSymbol, SourceLocation>();

            for (int i = 0; i < this.declaration.Declarations.Length; i++)
            {
                var declaration = this.declaration.Declarations[i];
                ExtensionInfo one = MakeOneDeclaredExtensionInfo(newBasesBeingResolved, declaration, diagnostics, out bool sawPartUnderlyingType, out bool oneExplicit);
                sawUnderlyingType |= sawPartUnderlyingType;

                if (i == 0)
                {
                    isExplicit = oneExplicit;
                }
                else if (isExplicit != oneExplicit)
                {
                    diagnostics.Add(ErrorCode.ERR_PartialDifferentExtensionModifiers, Locations.FirstOrNone(), this);
                }

                var partUnderlyingType = one.UnderlyingType;
                if (!reportedUnderlyingTypeConflict)
                {
                    if (underlyingType is null)
                    {
                        underlyingType = partUnderlyingType;
                        underlyingTypeLocation = declaration.NameLocation;
                    }
                    else if (partUnderlyingType is not null
                        && partUnderlyingType.TypeKind != TypeKind.Error
                        && !TypeSymbol.Equals(partUnderlyingType, underlyingType, TypeCompareKind.ConsiderEverything))
                    {
                        // the parts do not agree
                        bool shouldReportUnderlyingTypeConflict = false;
                        if (partUnderlyingType.Equals(underlyingType, TypeCompareKind.ObliviousNullableModifierMatchesAny))
                        {
                            if (ContainsOnlyOblivious(underlyingType))
                            {
                                underlyingType = partUnderlyingType;
                                underlyingTypeLocation = declaration.NameLocation;
                            }
                            else if (!ContainsOnlyOblivious(partUnderlyingType))
                            {
                                shouldReportUnderlyingTypeConflict = true;
                            }
                        }
                        else
                        {
                            shouldReportUnderlyingTypeConflict = true;
                        }

                        if (shouldReportUnderlyingTypeConflict)
                        {
                            var info = diagnostics.Add(ErrorCode.ERR_PartialMultipleUnderlyingTypes, Locations.FirstOrNone(), this);
                            underlyingType = new ExtendedErrorTypeSymbol(underlyingType, LookupResultKind.Ambiguous, info);
                            underlyingTypeLocation = declaration.NameLocation;
                            reportedUnderlyingTypeConflict = true;
                        }
                    }
                }

                foreach (NamedTypeSymbol partBaseExtension in one.BaseExtensions)
                {
                    if (!baseExtensionLocations.ContainsKey(partBaseExtension))
                    {
                        baseExtensionsBuilder.Add(partBaseExtension);
                        baseExtensionLocations.Add(partBaseExtension, declaration.NameLocation);
                    }
                }
            }

            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);

            if (!sawUnderlyingType)
            {
                diagnostics.Add(ErrorCode.ERR_ExtensionMissingUnderlyingType, Locations.FirstOrNone(), this);
            }
            else if (underlyingType is not null)
            {
                Debug.Assert(underlyingTypeLocation != null);

                if (!this.IsNoMoreVisibleThan(underlyingType, ref useSiteInfo))
                {
                    diagnostics.Add(ErrorCode.ERR_BadVisUnderlyingType, underlyingTypeLocation, this, underlyingType);
                }

                if (underlyingType.HasFileLocalTypes() && !this.IsFileLocal)
                {
                    diagnostics.Add(ErrorCode.ERR_FileTypeUnderlying, underlyingTypeLocation, underlyingType, this);
                }
            }

            var baseExtensions = baseExtensionsBuilder.ToImmutableAndFree();
            foreach (var baseExtension in baseExtensions)
            {
                if (!baseExtension.IsAtLeastAsVisibleAs(this, ref useSiteInfo))
                {
                    diagnostics.Add(ErrorCode.ERR_BadVisBaseExtension, baseExtensionLocations[baseExtension], this, baseExtension);
                }

                if (baseExtension.HasFileLocalTypes() && !this.IsFileLocal)
                {
                    diagnostics.Add(ErrorCode.ERR_FileTypeBase, baseExtensionLocations[baseExtension], baseExtension, this);
                }
            }

            baseExtensionLocations.Free();

            diagnostics.Add(Locations.FirstOrNone(), useSiteInfo);

            return new ExtensionInfo(underlyingType, baseExtensions);
        }

        /// <summary> Bind the base extensions for one part of a partial extension.</summary>
        private ExtensionInfo MakeOneDeclaredExtensionInfo(ConsList<TypeSymbol> basesBeingResolved, SingleTypeDeclaration decl, BindingDiagnosticBag diagnostics,
            out bool sawUnderlyingType, out bool isExplicit)
        {
            var syntax = (ExtensionDeclarationSyntax)decl.SyntaxReference.GetSyntax();
            TypeSymbol? partUnderlyingType = null;
            if (GetUnderlyingTypeSyntax(decl) is { } underlyingTypeSyntax)
            {
                sawUnderlyingType = true;
                var location = underlyingTypeSyntax.Location;
                var underlyingTypeBinder = this.DeclaringCompilation.GetBinder(underlyingTypeSyntax);
                underlyingTypeBinder = adjustBinder(syntax, underlyingTypeBinder);
                var underlyingTypeWithAnnotations = underlyingTypeBinder.BindType(underlyingTypeSyntax, diagnostics, basesBeingResolved);

                TypeSymbol underlyingType = underlyingTypeWithAnnotations.Type;
                if (underlyingType.IsStatic && !this.IsStatic)
                {
                    diagnostics.Add(ErrorCode.ERR_StaticBaseTypeOnInstanceExtension, location, this, underlyingType);
                }

                if (IsRestrictedExtensionUnderlyingType(underlyingType))
                {
                    diagnostics.Add(ErrorCode.ERR_BadExtensionUnderlyingType, location);
                }
                else
                {
                    partUnderlyingType = underlyingType;
                }
            }
            else
            {
                sawUnderlyingType = false;
            }

            var bases = syntax.BaseList;
            var partBaseExtensions = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            if (bases != null)
            {
                Binder baseBinder = this.DeclaringCompilation.GetBinder(bases);
                baseBinder = adjustBinder(syntax, baseBinder);

                foreach (var baseTypeSyntax in bases.Types)
                {
                    TypeSyntax typeSyntax = baseTypeSyntax.Type;
                    var location = new SourceLocation(typeSyntax);

                    TypeWithAnnotations baseTypeWithAnnotations = baseBinder.BindType(typeSyntax, diagnostics, basesBeingResolved);
                    TypeSymbol baseType = baseTypeWithAnnotations.Type;

                    switch (baseType.TypeKind)
                    {
                        case TypeKind.Extension:
                            if (baseTypeWithAnnotations.NullableAnnotation == NullableAnnotation.Annotated)
                            {
                                diagnostics.Add(ErrorCode.ERR_OnlyBaseExtensionAllowed, location);
                            }

                            foreach (var baseExtension in partBaseExtensions)
                            {
                                ReportDuplicateLocally(baseExtension, baseType, location, diagnostics, forBaseExtension: true);
                            }
                            partBaseExtensions.Add((NamedTypeSymbol)baseType);
                            break;

                        case TypeKind.Error:
                            partBaseExtensions.Add((NamedTypeSymbol)baseType);
                            break;

                        default:
                            diagnostics.Add(ErrorCode.ERR_OnlyBaseExtensionAllowed, location);
                            break;
                    }
                }
            }

            isExplicit = syntax.ImplicitOrExplicitKeyword.IsKind(SyntaxKind.ExplicitKeyword);
            return new ExtensionInfo(partUnderlyingType, partBaseExtensions.ToImmutableAndFree());

            Binder adjustBinder(ExtensionDeclarationSyntax syntax, Binder baseBinder)
            {
                // Wrap base binder in a location-specific binder that will avoid generic constraint checks
                // (to avoid cycles if the constraint types are not bound yet). Instead, constraint checks
                // are handled by the caller.
                baseBinder = baseBinder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks, this);

                baseBinder = baseBinder.WithUnsafeRegionIfNecessary(syntax.Modifiers);
                return baseBinder;
            }
        }

        private static bool IsRestrictedExtensionUnderlyingType(TypeSymbol type)
        {
            if (type.IsDynamic() || type.IsPointerOrFunctionPointer() || type.IsRefLikeType || type.IsExtension)
            {
                return true;
            }

            return false;
        }
    }
}
