// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
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
        // PROTOTYPE consider renaming ExtensionUnderlyingType->ExtendedType (here and elsewhere)
        private TypeSymbol? _lazyExtensionUnderlyingType = ErrorTypeSymbol.UnknownResultType;

        private ConcurrentDictionary<Symbol, Symbol>? _lazyInstanceMetadataMembers;

        internal SourceExtensionTypeSymbol(NamespaceOrTypeSymbol containingSymbol, MergedTypeDeclaration declaration, BindingDiagnosticBag diagnostics)
            : base(containingSymbol, declaration, diagnostics)
        {
            Debug.Assert(declaration.Kind == DeclarationKind.Extension);
        }

        internal override Symbol? TryGetCorrespondingStaticMetadataExtensionMember(Symbol member)
        {
            Debug.Assert(member.IsDefinition);
            Debug.Assert(member.ContainingSymbol == (object)this);

            if (member.ContainingSymbol != (object)this || member.IsStatic || GetExtendedTypeNoUseSiteDiagnostics(null) is null)
            {
                return null;
            }

            switch (member)
            {
                case SourceMemberMethodSymbol { MethodKind: not MethodKind.Constructor }:

                    return ensureDictionary(ref _lazyInstanceMetadataMembers).GetOrAdd(member, static (member) => new SourceExtensionMetadataMethodSymbol((MethodSymbol)member));

                case SourcePropertySymbol:
                    return ensureDictionary(ref _lazyInstanceMetadataMembers).GetOrAdd(member, static (member) => new SourceExtensionMetadataPropertySymbol((PropertySymbol)member));

                case SourceEventSymbol:
                    return ensureDictionary(ref _lazyInstanceMetadataMembers).GetOrAdd(member, static (member) => new SourceExtensionMetadataEventSymbol((EventSymbol)member));

                default:
                    return null;
            }

            static ConcurrentDictionary<Symbol, Symbol> ensureDictionary(ref ConcurrentDictionary<Symbol, Symbol>? storage)
            {
                if (storage is null)
                {
                    Interlocked.CompareExchange(ref storage, new ConcurrentDictionary<Symbol, Symbol>(Roslyn.Utilities.ReferenceEqualityComparer.Instance), null);
                }

                return storage;
            }
        }

        // PROTOTYPE restore base extensions or remove this wrapper type
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
            var underlyingType = this.GetExtendedTypeNoUseSiteDiagnostics(null);

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
                    CheckUnderspecifiedGenericExtension(underlyingType, TypeParameters, diagnostics, location, this);
                }
            }

            return;
        }

        internal static bool CheckUnderspecifiedGenericExtension(TypeSymbol underlyingType, ImmutableArray<TypeParameterSymbol> typeParameters,
            BindingDiagnosticBag diagnostics, Location location, NamedTypeSymbol extension)
        {
            var usedTypeParameters = PooledHashSet<TypeParameterSymbol>.GetInstance();
            underlyingType.VisitType(collectTypeParameters, arg: usedTypeParameters);
            bool anyUnusedTypeParameter = false;
            foreach (var typeParameter in typeParameters)
            {
                if (!usedTypeParameters.Contains(typeParameter))
                {
                    anyUnusedTypeParameter = true;
                    diagnostics.Add(ErrorCode.ERR_UnderspecifiedImplicitExtension, location, underlyingType, extension, typeParameter);
                }
            }

            usedTypeParameters.Free();
            return anyUnusedTypeParameter;

            static bool collectTypeParameters(TypeSymbol type, PooledHashSet<TypeParameterSymbol> typeParameters, bool ignored1, bool ignored2)
            {
                if (type is TypeParameterSymbol typeParameter)
                {
                    typeParameters.Add(typeParameter);
                }

                return false;
            }
        }

        internal sealed override TypeSymbol? GetDeclaredExtensionUnderlyingType()
        {
            var basesBeingResolved = ConsList<TypeSymbol>.Empty.Prepend(this.OriginalDefinition);
            return GetDeclaredExtensionInfo(basesBeingResolved).UnderlyingType;
        }

        internal sealed override TypeSymbol? GetExtendedTypeNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved)
        {
            if (ReferenceEquals(_lazyExtensionUnderlyingType, ErrorTypeSymbol.UnknownResultType))
            {
                if (basesBeingResolved?.ContainsReference(this.OriginalDefinition) == true)
                {
                    return null;
                }

                var diagnostics = BindingDiagnosticBag.GetInstance();
                TypeSymbol? acyclicBase = makeAcyclicUnderlyingType(basesBeingResolved, diagnostics);
                if (ReferenceEquals(Interlocked.CompareExchange(ref _lazyExtensionUnderlyingType, acyclicBase, ErrorTypeSymbol.UnknownResultType),
                        ErrorTypeSymbol.UnknownResultType))
                {
                    AddDeclarationDiagnostics(diagnostics);
                }

                diagnostics.Free();
            }

            return _lazyExtensionUnderlyingType;

            TypeSymbol? makeAcyclicUnderlyingType(ConsList<TypeSymbol>? basesBeingResolved, BindingDiagnosticBag diagnostics)
            {
                Debug.Assert(basesBeingResolved == null || !basesBeingResolved.ContainsReference(this.OriginalDefinition));
                var newBasesBeingResolved = basesBeingResolved.Prepend(this.OriginalDefinition);
                TypeSymbol? declaredUnderlyingType = GetDeclaredExtensionInfo(newBasesBeingResolved).UnderlyingType;

                if (declaredUnderlyingType is null)
                {
                    return null;
                }

                if (BaseTypeAnalysis.TypeDependsOn(depends: declaredUnderlyingType, on: this))
                {
                    return new ExtendedErrorTypeSymbol(declaredUnderlyingType, LookupResultKind.NotReferencable,
                        diagnostics.Add(ErrorCode.ERR_CircularBase, Locations[0], declaredUnderlyingType, this));
                }

                if (hasSelfReference(declaredUnderlyingType, this, newBasesBeingResolved))
                {
                    // If erasing extension types in the given extended type involves erasing that extended type
                    // then the result of erasure would be unbounded
                    diagnostics.Add(ErrorCode.ERR_CircularBase, Locations[0], declaredUnderlyingType, this);
                }

                var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);
                var current = declaredUnderlyingType;
                do
                {
                    // PROTOTYPE should this should check declaring module rather than compilations?
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
                    // PROTOTYPE Are we dropping dependencies if we are not getting into this 'if'?
                    var location = FindUnderlyingTypeSyntax(declaredUnderlyingType) ?? Locations[0];
                    diagnostics.Add(location, useSiteInfo);
                }

                return declaredUnderlyingType;
            }

            static bool hasSelfReference(TypeSymbol underlyingType, SourceExtensionTypeSymbol definition, ConsList<TypeSymbol>? basesBeingResolved)
            {
                Debug.Assert(definition.IsDefinition);
                PooledHashSet<TypeSymbol> alreadyVisited = PooledHashSet<TypeSymbol>.GetInstance();
                var result = foundSelfReferenceInErasure(underlyingType, definition, alreadyVisited: alreadyVisited, basesBeingResolved: basesBeingResolved, isContainer: false);
                alreadyVisited.Free();
                return result;
            }

            // Returns true if any erasure in the visited type is the given definition
            static bool foundSelfReferenceInErasure(TypeSymbol type, SourceExtensionTypeSymbol definition, PooledHashSet<TypeSymbol> alreadyVisited, ConsList<TypeSymbol>? basesBeingResolved, bool isContainer = false)
            {
                Debug.Assert(definition.IsDefinition);

                if (type is NamedTypeSymbol)
                {
                    if (!isContainer)
                    {
                        if (object.ReferenceEquals(type.OriginalDefinition, definition))
                        {
                            return true;
                        }

                        if (alreadyVisited.Contains(type))
                        {
                            return false;
                        }

                        alreadyVisited.Add(type);

                        if (type.IsExtension)
                        {
                            if (type.GetExtendedTypeNoUseSiteDiagnostics(basesBeingResolved) is { } extendedType)
                            {
                                return foundSelfReferenceInErasure(extendedType, definition, alreadyVisited, basesBeingResolved);
                            }

                            return true;
                        }
                    }

                    if (type.ContainingType is { } containingType
                        && foundSelfReferenceInErasure(containingType, definition, alreadyVisited, basesBeingResolved, isContainer: true))
                    {
                        return true;
                    }

                    foreach (var typeArgument in type.GetMemberTypeArgumentsNoUseSiteDiagnostics())
                    {
                        if (foundSelfReferenceInErasure(typeArgument, definition, alreadyVisited, basesBeingResolved))
                        {
                            return true;
                        }
                    }
                }
                else if (type is ArrayTypeSymbol arrayType)
                {
                    return foundSelfReferenceInErasure(arrayType.ElementType, definition, alreadyVisited, basesBeingResolved);
                }
                else if (type is PointerTypeSymbol pointerType)
                {
                    return foundSelfReferenceInErasure(pointerType.PointedAtType, definition, alreadyVisited, basesBeingResolved);
                }
                else if (type is FunctionPointerTypeSymbol functionPointerType)
                {
                    if (foundSelfReferenceInErasure(functionPointerType.Signature.ReturnType, definition, alreadyVisited, basesBeingResolved))
                    {
                        return true;
                    }

                    foreach (var parameter in functionPointerType.Signature.Parameters)
                    {
                        if (foundSelfReferenceInErasure(parameter.Type, definition, alreadyVisited, basesBeingResolved))
                        {
                            return true;
                        }
                    }
                }

                return false;
            };
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

            for (int i = 0; i < this.declaration.Declarations.Length; i++)
            {
                var declaration = this.declaration.Declarations[i];
                ExtensionInfo one = MakeOneDeclaredExtensionInfo(basesBeingResolved, declaration, diagnostics, out bool sawPartUnderlyingType, out bool oneExplicit);
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
        private ExtensionInfo MakeOneDeclaredExtensionInfo(ConsList<TypeSymbol>? basesBeingResolved, SingleTypeDeclaration decl, BindingDiagnosticBag diagnostics,
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
                // PROTOTYPE are nullable annotations allowed on extended types?
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

                baseBinder = baseBinder.SetOrClearUnsafeRegionIfNecessary(syntax.Modifiers);
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
