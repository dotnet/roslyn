// Licensed to the .NET Foundation under one or more agreements.
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
    internal partial class SourceNamedTypeSymbol
    {
        private ExtensionInfo _lazyDeclaredExtensionInfo = ExtensionInfo.Sentinel;
        private TypeSymbol? _lazyExtensionUnderlyingType = ErrorTypeSymbol.UnknownResultType;
        private ImmutableArray<NamedTypeSymbol> _lazyBaseExtensions;

        private class ExtensionInfo
        {
            public TypeSymbol? UnderlyingType;
            public ImmutableArray<NamedTypeSymbol> BaseExtensions;
            public bool IsExplicit;

            internal static readonly ExtensionInfo Sentinel =
                new ExtensionInfo(underlyingType: null, baseExtensions: default, isExplicit: false);

            public ExtensionInfo(TypeSymbol? underlyingType, ImmutableArray<NamedTypeSymbol> baseExtensions, bool isExplicit)
            {
                UnderlyingType = underlyingType;
                BaseExtensions = baseExtensions;
                IsExplicit = isExplicit;
            }
        }

        internal override bool IsExtension
            => this.declaration.Declarations[0].Kind is DeclarationKind.Extension;

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
            var allBaseExtensions = this.ExtensionsAndTheirBaseExtensionsNoUseSiteDiagnostics;
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

        private MultiDictionary<NamedTypeSymbol, NamedTypeSymbol> ExtensionsAndTheirBaseExtensionsNoUseSiteDiagnostics
        {
            get
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
        }

        internal sealed override TypeSymbol? GetDeclaredExtensionUnderlyingType()
            => GetDeclaredExtensionInfo().UnderlyingType;

        internal sealed override ImmutableArray<NamedTypeSymbol> GetDeclaredBaseExtensions()
            => GetDeclaredExtensionInfo().BaseExtensions;

        protected sealed override TypeSymbol? ExtensionUnderlyingTypeNoUseSiteDiagnosticsCore
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

        protected override ImmutableArray<NamedTypeSymbol> BaseExtensionsNoUseSiteDiagnosticsCore
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
            Debug.Assert(this.IsExtension);

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

            bool? anyExplicit = null;

            bool sawUnderlyingType = false;
            bool reportedUnderlyingTypeConflict = false;
            TypeSymbol? underlyingType = null;
            SourceLocation? underlyingTypeLocation = null;

            Debug.Assert(basesBeingResolved == null || !basesBeingResolved.ContainsReference(this.OriginalDefinition));
            var newBasesBeingResolved = basesBeingResolved.Prepend(this.OriginalDefinition);
            var baseExtensionsBuilder = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            var baseExtensionLocations = SpecializedSymbolCollections.GetPooledSymbolDictionaryInstance<NamedTypeSymbol, SourceLocation>();

            foreach (var declaration in this.declaration.Declarations)
            {
                ExtensionInfo one = MakeOneDeclaredExtensionInfo(newBasesBeingResolved, declaration, diagnostics, out bool sawPartUnderlyingType);
                sawUnderlyingType |= sawPartUnderlyingType;

                if (anyExplicit == null)
                {
                    anyExplicit = one.IsExplicit;
                }
                else if (anyExplicit != one.IsExplicit)
                {
                    diagnostics.Add(ErrorCode.ERR_PartialDifferentExtensionModifiers, Locations.FirstOrNone(), this);
                    anyExplicit |= one.IsExplicit;
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

                if (underlyingType.HasFileLocalTypes() && !this.HasFileLocalTypes())
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

                if (baseExtension.HasFileLocalTypes() && !this.HasFileLocalTypes())
                {
                    diagnostics.Add(ErrorCode.ERR_FileTypeBase, baseExtensionLocations[baseExtension], baseExtension, this);
                }
            }

            baseExtensionLocations.Free();

            diagnostics.Add(Locations.FirstOrNone(), useSiteInfo);

            Debug.Assert(anyExplicit is not null);
            return new ExtensionInfo(underlyingType, baseExtensions, isExplicit: anyExplicit.Value);
        }

        /// <summary> Bind the base extensions for one part of a partial extension.</summary>
        private ExtensionInfo MakeOneDeclaredExtensionInfo(ConsList<TypeSymbol> basesBeingResolved, SingleTypeDeclaration decl, BindingDiagnosticBag diagnostics, out bool sawUnderlyingType)
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
                checkStatic(diagnostics, location, underlyingType);
                if (IsRestrictedExtensionUnderlyingType(underlyingTypeWithAnnotations))
                {
                    diagnostics.Add(ErrorCode.ERR_BadExtensionUnderlyingType, location, this, underlyingTypeWithAnnotations);
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
                    checkStatic(diagnostics, location, baseType);

                    switch (baseType.TypeKind)
                    {
                        case TypeKind.Extension when baseTypeWithAnnotations.NullableAnnotation != NullableAnnotation.Annotated:
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
                            diagnostics.Add(ErrorCode.ERR_OnlyBaseExtensionAllowed, location, baseTypeWithAnnotations);
                            break;
                    }
                }
            }

            return new ExtensionInfo(partUnderlyingType, partBaseExtensions.ToImmutableAndFree(),
                isExplicit: syntax.ImplicitOrExplicitKeyword.IsKind(SyntaxKind.ExplicitKeyword));

            void checkStatic(BindingDiagnosticBag diagnostics, Location location, TypeSymbol baseType)
            {
                if (baseType.IsStatic && !this.IsStatic)
                {
                    diagnostics.Add(ErrorCode.ERR_StaticBaseTypeOnInstanceExtension, location, this, baseType);
                }
            }

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

        private static bool IsRestrictedExtensionUnderlyingType(TypeWithAnnotations underlyingType)
        {
            var type = underlyingType.Type;
            if (type.IsDynamic() || type.IsPointerOrFunctionPointer() || type.IsRefLikeType || type.IsExtension
                || underlyingType.NullableAnnotation == NullableAnnotation.Annotated)
            {
                return true;
            }

            return false;
        }
    }
}
