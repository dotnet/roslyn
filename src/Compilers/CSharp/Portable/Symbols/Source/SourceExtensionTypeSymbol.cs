// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceExtensionTypeSymbol : SourceNamedTypeSymbol
    {
        private ExtensionInfo _lazyDeclaredExtensionInfo = ExtensionInfo.Sentinel;
        // PROTOTYPE consider renaming ExtensionUnderlyingType->ExtendedType (here and elsewhere)
        private TypeSymbol? _lazyExtensionUnderlyingType = ErrorTypeSymbol.UnknownResultType;

        internal SourceExtensionTypeSymbol(NamespaceOrTypeSymbol containingSymbol, MergedTypeDeclaration declaration, BindingDiagnosticBag diagnostics)
            : base(containingSymbol, declaration, diagnostics)
        {
            Debug.Assert(declaration.Kind == DeclarationKind.Extension);
        }

        // PROTOTYPE(inheritance) restore base extensions or remove this wrapper type
        private class ExtensionInfo
        {
            public readonly TypeSymbol? UnderlyingType;

            internal static readonly ExtensionInfo Sentinel =
                new ExtensionInfo(underlyingType: null);

            public ExtensionInfo(TypeSymbol? underlyingType)
            {
                UnderlyingType = underlyingType;
            }
        }

        internal override bool IsExtension => true;

        internal override bool IsExplicitExtension
            => this.declaration.Declarations[0].IsExplicitExtension;

        protected override void CheckUnderlyingType(BindingDiagnosticBag diagnostics)
        {
            var underlyingType = this.ExtendedTypeNoUseSiteDiagnostics;

            if (underlyingType is null)
                return;

            var singleDeclaration = this.FirstDeclarationWithExplicitUnderlyingType();
            if (singleDeclaration != null)
            {
                var corLibrary = this.ContainingAssembly.CorLibrary;
                var conversions = new TypeConversions(corLibrary);
                var location = singleDeclaration.NameLocation;

                underlyingType.CheckAllConstraints(DeclaringCompilation, conversions, location, diagnostics);

                if (!IsExplicitExtension)
                {
                    var usedTypeParameters = PooledHashSet<TypeParameterSymbol>.GetInstance();
                    underlyingType.VisitType(collectTypeParameters, arg: usedTypeParameters);

                    foreach (var typeParameter in TypeParameters)
                    {
                        if (!usedTypeParameters.Contains(typeParameter))
                        {
                            diagnostics.Add(ErrorCode.ERR_UnderspecifiedImplicitExtension, location, underlyingType, this, typeParameter);
                        }
                    }

                    usedTypeParameters.Free();
                }
            }

            return;

            static bool collectTypeParameters(TypeSymbol type, PooledHashSet<TypeParameterSymbol> typeParameters, bool b)
            {
                if (type is TypeParameterSymbol typeParameter)
                {
                    typeParameters.Add(typeParameter);
                }

                return false;
            }
        }

        internal sealed override TypeSymbol? GetDeclaredExtensionUnderlyingType()
            => GetDeclaredExtensionInfo(basesBeingResolved: null).UnderlyingType;

        internal sealed override TypeSymbol? ExtendedTypeNoUseSiteDiagnostics
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
                    TypeSymbol? declaredUnderlyingType = GetDeclaredExtensionInfo(basesBeingResolved: null).UnderlyingType;

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
                        // PROTOTYPE(static) should this should check declaring module rather than compilations?
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
                        // PROTOTYPE(static) Are we dropping dependencies if we are not getting into this 'if'?
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

        private ExtensionInfo GetDeclaredExtensionInfo(ConsList<TypeSymbol>? basesBeingResolved)
        {
            if (ReferenceEquals(_lazyDeclaredExtensionInfo, ExtensionInfo.Sentinel))
            {
                BindingDiagnosticBag diagnostics = BindingDiagnosticBag.GetInstance();

                var original = Interlocked.CompareExchange(ref _lazyDeclaredExtensionInfo,
                    this.MakeDeclaredExtensionInfo(basesBeingResolved, diagnostics),
                    ExtensionInfo.Sentinel);

                if (ReferenceEquals(original, ExtensionInfo.Sentinel))
                {
                    AddDeclarationDiagnostics(diagnostics);
                }

                diagnostics.Free();
            }

            return _lazyDeclaredExtensionInfo;
        }

        /// <summary> Bind the underlying type for all parts of an extension.</summary>
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
                    diagnostics.Add(ErrorCode.ERR_PartialDifferentExtensionModifiers, GetFirstLocationOrNone(), this);
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
                            var info = diagnostics.Add(ErrorCode.ERR_PartialMultipleUnderlyingTypes, GetFirstLocationOrNone(), this);
                            underlyingType = new ExtendedErrorTypeSymbol(underlyingType, LookupResultKind.Ambiguous, info);
                            underlyingTypeLocation = declaration.NameLocation;
                            reportedUnderlyingTypeConflict = true;
                        }
                    }
                }
            }

            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);

            if (!sawUnderlyingType)
            {
                diagnostics.Add(ErrorCode.ERR_ExtensionMissingUnderlyingType, GetFirstLocationOrNone(), this);
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

            diagnostics.Add(GetFirstLocationOrNone(), useSiteInfo);

            return new ExtensionInfo(underlyingType);
        }

        /// <summary>
        /// Bind the base extensions for one part of a partial extension.
        /// Validation in this method should be in sync with:
        /// - <see cref="Metadata.PE.PENamedTypeSymbol.EnsureExtensionTypeDecoded"/>
        /// - <see cref="Retargeting.RetargetingNamedTypeSymbol.GetDeclaredExtensionUnderlyingType"/>
        /// </summary>
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
                // PROTOTYPE(static) are nullable annotations allowed on extended types?
                if (AreStaticIncompatible(extendedType: underlyingType, extensionType: this))
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

            isExplicit = syntax.ImplicitOrExplicitKeyword.IsKind(SyntaxKind.ExplicitKeyword);
            return new ExtensionInfo(partUnderlyingType);

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

        internal static bool AreStaticIncompatible(TypeSymbol extendedType, NamedTypeSymbol extensionType)
        {
            return extendedType.IsStatic && !extensionType.IsStatic;
        }

        internal static bool IsRestrictedExtensionUnderlyingType(TypeSymbol type)
        {
            if (type.IsDynamic() || type.IsPointerOrFunctionPointer() || type.IsRefLikeType || type.IsExtension)
            {
                return true;
            }

            return false;
        }
    }
}
