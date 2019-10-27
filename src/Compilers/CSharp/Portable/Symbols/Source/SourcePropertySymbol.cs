// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourcePropertySymbol : PropertySymbol, IAttributeTargetSymbol
    {
        /// <summary>
        /// Condensed flags storing useful information about the <see cref="SourcePropertySymbol"/> 
        /// so that we do not have to go back to source to compute this data.
        /// </summary>
        [Flags]
        private enum Flags : byte
        {
            IsExpressionBodied = 1 << 0,
            IsAutoProperty = 1 << 1,
            IsExplicitInterfaceImplementation = 1 << 2,
        }

        private const string DefaultIndexerName = "Item";

        // TODO (tomat): consider splitting into multiple subclasses/rare data.

        private readonly SourceMemberContainerTypeSymbol _containingType;
        private readonly string _name;
        private readonly SyntaxReference _syntaxRef;
        private readonly Location _location;
        private readonly DeclarationModifiers _modifiers;
        private readonly ImmutableArray<CustomModifier> _refCustomModifiers;
        private readonly SourcePropertyAccessorSymbol? _getMethod;
        private readonly SourcePropertyAccessorSymbol? _setMethod;
        private readonly SynthesizedBackingFieldSymbol? _backingField;
        private readonly TypeSymbol _explicitInterfaceType;
        private readonly ImmutableArray<PropertySymbol> _explicitInterfaceImplementations;
        private readonly Flags _propertyFlags;
        private readonly RefKind _refKind;

        private SymbolCompletionState _state;
        private ImmutableArray<ParameterSymbol> _lazyParameters;
        private TypeWithAnnotations.Boxed? _lazyType;

        /// <summary>
        /// Set in constructor, might be changed while decoding <see cref="IndexerNameAttribute"/>.
        /// </summary>
        private readonly string _sourceName;

        private string? _lazyDocComment;
        private string? _lazyExpandedDocComment;
        private OverriddenOrHiddenMembersResult? _lazyOverriddenOrHiddenMembers;
        private SynthesizedSealedPropertyAccessor? _lazySynthesizedSealedAccessor;
        private CustomAttributesBag<CSharpAttributeData>? _lazyCustomAttributesBag;

        // CONSIDER: if the parameters were computed lazily, ParameterCount could be overridden to fall back on the syntax (as in SourceMemberMethodSymbol).

        private SourcePropertySymbol(
           SourceMemberContainerTypeSymbol containingType,
           Binder bodyBinder,
           BasePropertyDeclarationSyntax syntax,
           string name,
           Location location,
           DiagnosticBag diagnostics)
        {
            // This has the value that IsIndexer will ultimately have, once we've populated the fields of this object.
            bool isIndexer = syntax.Kind() == SyntaxKind.IndexerDeclaration;
            var interfaceSpecifier = GetExplicitInterfaceSpecifier(syntax);
            bool isExplicitInterfaceImplementation = interfaceSpecifier != null;
            if (isExplicitInterfaceImplementation)
            {
                _propertyFlags |= Flags.IsExplicitInterfaceImplementation;
            }

            _location = location;
            _containingType = containingType;
            _syntaxRef = syntax.GetReference();
            _refKind = syntax.Type.GetRefKind();

            SyntaxTokenList modifiers = syntax.Modifiers;
            bodyBinder = bodyBinder.WithUnsafeRegionIfNecessary(modifiers);
            bodyBinder = bodyBinder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks, this);

            var propertySyntax = syntax as PropertyDeclarationSyntax;
            var arrowExpression = propertySyntax != null
                ? propertySyntax.ExpressionBody
                : ((IndexerDeclarationSyntax)syntax).ExpressionBody;
            bool hasExpressionBody = arrowExpression != null;
            bool hasInitializer = !isIndexer && propertySyntax!.Initializer != null;

            GetAcessorDeclarations(syntax, diagnostics, out bool isAutoProperty, out bool hasAccessorList,
                                   out AccessorDeclarationSyntax? getSyntax, out AccessorDeclarationSyntax? setSyntax);

            bool accessorsHaveImplementation;
            if (hasAccessorList)
            {
                accessorsHaveImplementation = (getSyntax != null && (getSyntax.Body != null || getSyntax.ExpressionBody != null)) ||
                                              (setSyntax != null && (setSyntax.Body != null || setSyntax.ExpressionBody != null));
            }
            else
            {
                accessorsHaveImplementation = hasExpressionBody;
            }

            bool modifierErrors;
            _modifiers = MakeModifiers(modifiers, isExplicitInterfaceImplementation, isIndexer,
                                       accessorsHaveImplementation, location,
                                       diagnostics, out modifierErrors);
            this.CheckAccessibility(location, diagnostics, isExplicitInterfaceImplementation);

            this.CheckModifiers(isExplicitInterfaceImplementation, location, isIndexer, diagnostics);

            isAutoProperty = isAutoProperty && (!(containingType.IsInterface && !IsStatic) && !IsAbstract && !IsExtern && !isIndexer && hasAccessorList);

            if (isIndexer && !isExplicitInterfaceImplementation)
            {
                // Evaluate the attributes immediately in case the IndexerNameAttribute has been applied.
                // NOTE: we want IsExplicitInterfaceImplementation, IsOverride, Locations, and the syntax reference
                // to be initialized before we pass this symbol to LoadCustomAttributes.

                // CONSIDER: none of the information from this early binding pass is cached.  Everything will
                // be re-bound when someone calls GetAttributes.  If this gets to be a problem, we could
                // always use the real attribute bag of this symbol and modify LoadAndValidateAttributes to
                // handle partially filled bags.
                CustomAttributesBag<CSharpAttributeData>? temp = null;
                LoadAndValidateAttributes(OneOrMany.Create(this.CSharpSyntaxNode.AttributeLists), ref temp, earlyDecodingOnly: true);
                if (temp != null)
                {
                    Debug.Assert(temp.IsEarlyDecodedWellKnownAttributeDataComputed);
                    var propertyData = (PropertyEarlyWellKnownAttributeData)temp.EarlyDecodedWellKnownAttributeData;
                    if (propertyData != null)
                    {
                        _sourceName = propertyData.IndexerName;
                    }
                }
            }

            string aliasQualifierOpt;
            string memberName = ExplicitInterfaceHelpers.GetMemberNameAndInterfaceSymbol(bodyBinder, interfaceSpecifier, name, diagnostics, out _explicitInterfaceType, out aliasQualifierOpt);
            _sourceName = _sourceName ?? memberName; //sourceName may have been set while loading attributes
            _name = isIndexer ? ExplicitInterfaceHelpers.GetMemberName(WellKnownMemberNames.Indexer, _explicitInterfaceType, aliasQualifierOpt) : _sourceName;

            if (hasInitializer)
            {
                CheckInitializer(isAutoProperty, location, diagnostics);
            }

            if (isAutoProperty || hasInitializer)
            {
                var hasGetSyntax = getSyntax != null;
                var isAutoPropertyWithGetSyntax = isAutoProperty && hasGetSyntax;
                if (isAutoPropertyWithGetSyntax)
                {
                    _propertyFlags |= Flags.IsAutoProperty;
                }

                bool isGetterOnly = hasGetSyntax && setSyntax == null;

                if (isAutoPropertyWithGetSyntax && !IsStatic && !isGetterOnly)
                {
                    if (ContainingType.IsReadOnly)
                    {
                        diagnostics.Add(ErrorCode.ERR_AutoPropsInRoStruct, location);
                    }
                    else if (HasReadOnlyModifier)
                    {
                        diagnostics.Add(ErrorCode.ERR_AutoPropertyWithSetterCantBeReadOnly, location, this);
                    }
                }

                if (isAutoPropertyWithGetSyntax || hasInitializer)
                {
                    if (isAutoPropertyWithGetSyntax)
                    {
                        //issue a diagnostic if the compiler generated attribute ctor is not found.
                        Binder.ReportUseSiteDiagnosticForSynthesizedAttribute(bodyBinder.Compilation,
                        WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor, diagnostics, syntax: syntax);

                        if (this._refKind != RefKind.None && !_containingType.IsInterface)
                        {
                            diagnostics.Add(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, location, this);
                        }
                    }

                    string fieldName = GeneratedNames.MakeBackingFieldName(_sourceName);
                    _backingField = new SynthesizedBackingFieldSymbol(this,
                                                                          fieldName,
                                                                          isGetterOnly,
                                                                          this.IsStatic,
                                                                          hasInitializer);
                }

                if (isAutoProperty)
                {
                    Binder.CheckFeatureAvailability(
                        syntax,
                        isGetterOnly ? MessageID.IDS_FeatureReadonlyAutoImplementedProperties : MessageID.IDS_FeatureAutoImplementedProperties,
                        diagnostics,
                        location);
                }
            }

            PropertySymbol? explicitlyImplementedProperty = null;
            _refCustomModifiers = ImmutableArray<CustomModifier>.Empty;

            // The runtime will not treat the accessors of this property as overrides or implementations
            // of those of another property unless both the signatures and the custom modifiers match.
            // Hence, in the case of overrides and *explicit* implementations, we need to copy the custom
            // modifiers that are in the signatures of the overridden/implemented property accessors.
            // (From source, we know that there can only be one overridden/implemented property, so there
            // are no conflicts.)  This is unnecessary for implicit implementations because, if the custom
            // modifiers don't match, we'll insert bridge methods for the accessors (explicit implementations 
            // that delegate to the implicit implementations) with the correct custom modifiers
            // (see SourceMemberContainerTypeSymbol.SynthesizeInterfaceMemberImplementation).

            // Note: we're checking if the syntax indicates explicit implementation rather,
            // than if explicitInterfaceType is null because we don't want to look for an
            // overridden property if this is supposed to be an explicit implementation.
            if (isExplicitInterfaceImplementation || this.IsOverride)
            {
                // Type and parameters for overrides and explicit implementations cannot be bound
                // lazily since the property name depends on the metadata name of the base property,
                // and the property name is required to add the property to the containing type, and
                // the type and parameters are required to determine the override or implementation.
                var type = this.ComputeType(bodyBinder, syntax, diagnostics);
                _lazyType = new TypeWithAnnotations.Boxed(type);
                _lazyParameters = this.ComputeParameters(bodyBinder, syntax, diagnostics);

                bool isOverride = false;
                PropertySymbol? overriddenOrImplementedProperty = null;

                if (!isExplicitInterfaceImplementation)
                {
                    // If this property is an override, we may need to copy custom modifiers from
                    // the overridden property (so that the runtime will recognize it as an override).
                    // We check for this case here, while we can still modify the parameters and
                    // return type without losing the appearance of immutability.
                    isOverride = true;
                    overriddenOrImplementedProperty = this.OverriddenProperty;
                }
                else
                {
                    string interfacePropertyName = isIndexer ? WellKnownMemberNames.Indexer : name;
                    explicitlyImplementedProperty = this.FindExplicitlyImplementedProperty(_explicitInterfaceType, interfacePropertyName, interfaceSpecifier, diagnostics);
                    this.FindExplicitlyImplementedMemberVerification(explicitlyImplementedProperty, diagnostics);
                    overriddenOrImplementedProperty = explicitlyImplementedProperty;
                }

                if ((object?)overriddenOrImplementedProperty != null)
                {
                    _refCustomModifiers = _refKind != RefKind.None ? overriddenOrImplementedProperty.RefCustomModifiers : ImmutableArray<CustomModifier>.Empty;

                    TypeWithAnnotations overriddenPropertyType = overriddenOrImplementedProperty.TypeWithAnnotations;

                    // We do an extra check before copying the type to handle the case where the overriding
                    // property (incorrectly) has a different type than the overridden property.  In such cases,
                    // we want to retain the original (incorrect) type to avoid hiding the type given in source.
                    if (type.Type.Equals(overriddenPropertyType.Type, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes | TypeCompareKind.IgnoreDynamic))
                    {
                        type = type.WithTypeAndModifiers(
                            CustomModifierUtils.CopyTypeCustomModifiers(overriddenPropertyType.Type, type.Type, this.ContainingAssembly),
                            overriddenPropertyType.CustomModifiers);

                        _lazyType = new TypeWithAnnotations.Boxed(type);
                    }

                    _lazyParameters = CustomModifierUtils.CopyParameterCustomModifiers(overriddenOrImplementedProperty.Parameters, _lazyParameters, alsoCopyParamsModifier: isOverride);
                }
            }
            else if (_refKind == RefKind.RefReadOnly)
            {
                var modifierType = bodyBinder.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_InAttribute, diagnostics, syntax.Type);

                _refCustomModifiers = ImmutableArray.Create(CSharpCustomModifier.CreateRequired(modifierType));
            }

            if (!hasAccessorList)
            {
                if (hasExpressionBody)
                {
                    _propertyFlags |= Flags.IsExpressionBodied;
                    _getMethod = SourcePropertyAccessorSymbol.CreateAccessorSymbol(
                        containingType,
                        this,
                        _modifiers,
                        _sourceName,
                        arrowExpression,
                        explicitlyImplementedProperty,
                        aliasQualifierOpt,
                        isExplicitInterfaceImplementation,
                        diagnostics);
                }
                else
                {
                    _getMethod = null;
                }
                _setMethod = null;
            }
            else
            {
                _getMethod = CreateAccessorSymbol(getSyntax, explicitlyImplementedProperty, aliasQualifierOpt, isAutoProperty, isExplicitInterfaceImplementation, diagnostics);
                _setMethod = CreateAccessorSymbol(setSyntax, explicitlyImplementedProperty, aliasQualifierOpt, isAutoProperty, isExplicitInterfaceImplementation, diagnostics);

                if ((getSyntax == null) || (setSyntax == null))
                {
                    if ((getSyntax == null) && (setSyntax == null))
                    {
                        diagnostics.Add(ErrorCode.ERR_PropertyWithNoAccessors, location, this);
                    }
                    else if (_refKind != RefKind.None)
                    {
                        if (getSyntax == null)
                        {
                            diagnostics.Add(ErrorCode.ERR_RefPropertyMustHaveGetAccessor, location, this);
                        }
                    }
                    else if (isAutoProperty)
                    {
                        var accessor = _getMethod ?? _setMethod;
                        if (getSyntax == null)
                        {
#nullable disable // Can 'accessor' be null? https://github.com/dotnet/roslyn/issues/39166
                            diagnostics.Add(ErrorCode.ERR_AutoPropertyMustHaveGetAccessor, accessor.Locations[0], accessor);
#nullable enable
                        }
                    }
                }

                // Check accessor accessibility is more restrictive than property accessibility.
                CheckAccessibilityMoreRestrictive(_getMethod, diagnostics);
                CheckAccessibilityMoreRestrictive(_setMethod, diagnostics);

                if (((object?)_getMethod != null) && ((object?)_setMethod != null))
                {
                    if (_refKind != RefKind.None)
                    {
                        diagnostics.Add(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, _setMethod.Locations[0], _setMethod);
                    }
                    else if ((_getMethod.LocalAccessibility != Accessibility.NotApplicable) &&
                        (_setMethod.LocalAccessibility != Accessibility.NotApplicable))
                    {
                        // Check accessibility is set on at most one accessor.
                        diagnostics.Add(ErrorCode.ERR_DuplicatePropertyAccessMods, location, this);
                    }
                    else if (_getMethod.LocalDeclaredReadOnly && _setMethod.LocalDeclaredReadOnly)
                    {
                        diagnostics.Add(ErrorCode.ERR_DuplicatePropertyReadOnlyMods, location, this);
                    }
                    else if (this.IsAbstract)
                    {
                        // Check abstract property accessors are not private.
                        CheckAbstractPropertyAccessorNotPrivate(_getMethod, diagnostics);
                        CheckAbstractPropertyAccessorNotPrivate(_setMethod, diagnostics);
                    }
                }
                else
                {
                    if (!this.IsOverride)
                    {
                        var accessor = _getMethod ?? _setMethod;
                        if ((object?)accessor != null)
                        {
                            // Check accessibility is not set on the one accessor.
                            if (accessor.LocalAccessibility != Accessibility.NotApplicable)
                            {
                                diagnostics.Add(ErrorCode.ERR_AccessModMissingAccessor, location, this);
                            }

                            // Check that 'readonly' is not set on the one accessor.
                            if (accessor.LocalDeclaredReadOnly)
                            {
                                diagnostics.Add(ErrorCode.ERR_ReadOnlyModMissingAccessor, location, this);
                            }
                        }
                    }
                }
            }

            if ((object?)explicitlyImplementedProperty != null)
            {
                CheckExplicitImplementationAccessor(this.GetMethod, explicitlyImplementedProperty.GetMethod, explicitlyImplementedProperty, diagnostics);
                CheckExplicitImplementationAccessor(this.SetMethod, explicitlyImplementedProperty.SetMethod, explicitlyImplementedProperty, diagnostics);
            }

            _explicitInterfaceImplementations =
                (object?)explicitlyImplementedProperty == null ?
                    ImmutableArray<PropertySymbol>.Empty :
                    ImmutableArray.Create(explicitlyImplementedProperty);

            // get-only auto property should not override settable properties
            if ((_propertyFlags & Flags.IsAutoProperty) != 0)
            {
                if (_setMethod is null && !this.IsReadOnly)
                {
                    diagnostics.Add(ErrorCode.ERR_AutoPropertyMustOverrideSet, location, this);
                }

                CheckForFieldTargetedAttribute(syntax, diagnostics);
            }

            CheckForBlockAndExpressionBody(
                syntax.AccessorList, syntax.GetExpressionBodySyntax(), syntax, diagnostics);
        }

        private void CheckForFieldTargetedAttribute(BasePropertyDeclarationSyntax syntax, DiagnosticBag diagnostics)
        {
            var languageVersion = this.DeclaringCompilation.LanguageVersion;
            if (languageVersion.AllowAttributesOnBackingFields())
            {
                return;
            }

            foreach (var attribute in syntax.AttributeLists)
            {
                if (attribute.Target?.GetAttributeLocation() == AttributeLocation.Field)
                {
                    diagnostics.Add(
                        new CSDiagnosticInfo(ErrorCode.WRN_AttributesOnBackingFieldsNotAvailable,
                            languageVersion.ToDisplayString(),
                            new CSharpRequiredLanguageVersion(MessageID.IDS_FeatureAttributesOnBackingFields.RequiredVersion())),
                        attribute.Target.Location);
                }
            }
        }

        private static void GetAcessorDeclarations(BasePropertyDeclarationSyntax syntax, DiagnosticBag diagnostics,
                                                   out bool isAutoProperty, out bool hasAccessorList,
                                                   out AccessorDeclarationSyntax? getSyntax, out AccessorDeclarationSyntax? setSyntax)
        {
            isAutoProperty = true;
            hasAccessorList = syntax.AccessorList != null;
            getSyntax = null;
            setSyntax = null;

            if (hasAccessorList)
            {
                foreach (var accessor in syntax.AccessorList!.Accessors)
                {
                    switch (accessor.Kind())
                    {
                        case SyntaxKind.GetAccessorDeclaration:
                            if (getSyntax == null)
                            {
                                getSyntax = accessor;
                            }
                            else
                            {
                                diagnostics.Add(ErrorCode.ERR_DuplicateAccessor, accessor.Keyword.GetLocation());
                            }
                            break;
                        case SyntaxKind.SetAccessorDeclaration:
                            if (setSyntax == null)
                            {
                                setSyntax = accessor;
                            }
                            else
                            {
                                diagnostics.Add(ErrorCode.ERR_DuplicateAccessor, accessor.Keyword.GetLocation());
                            }
                            break;
                        case SyntaxKind.AddAccessorDeclaration:
                        case SyntaxKind.RemoveAccessorDeclaration:
                            diagnostics.Add(ErrorCode.ERR_GetOrSetExpected, accessor.Keyword.GetLocation());
                            continue;
                        case SyntaxKind.UnknownAccessorDeclaration:
                            // We don't need to report an error here as the parser will already have
                            // done that for us.
                            continue;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(accessor.Kind());
                    }

                    if (accessor.Body != null || accessor.ExpressionBody != null)
                    {
                        isAutoProperty = false;
                    }
                }
            }
            else
            {
                isAutoProperty = false;
            }
        }

        internal bool IsExpressionBodied
            => (_propertyFlags & Flags.IsExpressionBodied) != 0;

        private void CheckInitializer(
            bool isAutoProperty,
            Location location,
            DiagnosticBag diagnostics)
        {
            if (!isAutoProperty)
            {
                diagnostics.Add(ErrorCode.ERR_InitializerOnNonAutoProperty, location, this);
            }
        }

        internal static SourcePropertySymbol Create(SourceMemberContainerTypeSymbol containingType, Binder bodyBinder, PropertyDeclarationSyntax syntax, DiagnosticBag diagnostics)
        {
            var nameToken = syntax.Identifier;
            var location = nameToken.GetLocation();
            return new SourcePropertySymbol(containingType, bodyBinder, syntax, nameToken.ValueText, location, diagnostics);
        }

        internal static SourcePropertySymbol Create(SourceMemberContainerTypeSymbol containingType, Binder bodyBinder, IndexerDeclarationSyntax syntax, DiagnosticBag diagnostics)
        {
            var location = syntax.ThisKeyword.GetLocation();
            return new SourcePropertySymbol(containingType, bodyBinder, syntax, DefaultIndexerName, location, diagnostics);
        }

        public override RefKind RefKind
        {
            get
            {
                return _refKind;
            }
        }

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get
            {
                if (_lazyType == null)
                {
                    var diagnostics = DiagnosticBag.GetInstance();
                    var binder = this.CreateBinderForTypeAndParameters();
                    var syntax = (BasePropertyDeclarationSyntax)_syntaxRef.GetSyntax();
                    var result = this.ComputeType(binder, syntax, diagnostics);
                    if (Interlocked.CompareExchange(ref _lazyType, new TypeWithAnnotations.Boxed(result), null) == null)
                    {
                        this.AddDeclarationDiagnostics(diagnostics);
                    }
                    diagnostics.Free();
                }

                return _lazyType.Value;
            }
        }

        internal bool HasPointerType
        {
            get
            {
                if (_lazyType != null)
                {
                    return _lazyType.Value.DefaultType.IsPointerType();
                }

                var syntax = (BasePropertyDeclarationSyntax)_syntaxRef.GetSyntax();
                RefKind refKind;
                var typeSyntax = syntax.Type.SkipRef(out refKind);
                return typeSyntax.Kind() == SyntaxKind.PointerType;
            }
        }

        /// <remarks>
        /// To facilitate lookup, all indexer symbols have the same name.
        /// Check the MetadataName property to find the name that will be
        /// emitted (based on IndexerNameAttribute, or the default "Item").
        /// </remarks>
        public override string Name
        {
            get
            {
                return _name;
            }
        }

        public override string MetadataName
        {
            get
            {
                // Explicit implementation names may have spaces if the interface
                // is generic (between the type arguments).
                return _sourceName.Replace(" ", "");
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _containingType;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return _containingType;
            }
        }

        internal override LexicalSortKey GetLexicalSortKey()
        {
            return new LexicalSortKey(_location, this.DeclaringCompilation);
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray.Create(_location);
            }
        }

        internal Location Location
        {
            get
            {
                return _location;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray.Create(_syntaxRef);
            }
        }

        public override bool IsAbstract
        {
            get { return (_modifiers & DeclarationModifiers.Abstract) != 0; }
        }

        public override bool IsExtern
        {
            get { return (_modifiers & DeclarationModifiers.Extern) != 0; }
        }

        public override bool IsStatic
        {
            get { return (_modifiers & DeclarationModifiers.Static) != 0; }
        }

        internal bool IsFixed
        {
            get { return false; }
        }

        /// <remarks>
        /// Even though it is declared with an IndexerDeclarationSyntax, an explicit
        /// interface implementation is not an indexer because it will not cause the
        /// containing type to be emitted with a DefaultMemberAttribute (and even if
        /// there is another indexer, the name of the explicit implementation won't
        /// match).  This is important for round-tripping.
        /// </remarks>
        public override bool IsIndexer
        {
            get { return (_modifiers & DeclarationModifiers.Indexer) != 0; }
        }

        public override bool IsOverride
        {
            get { return (_modifiers & DeclarationModifiers.Override) != 0; }
        }

        public override bool IsSealed
        {
            get { return (_modifiers & DeclarationModifiers.Sealed) != 0; }
        }

        public override bool IsVirtual
        {
            get { return (_modifiers & DeclarationModifiers.Virtual) != 0; }
        }

        internal bool IsNew
        {
            get { return (_modifiers & DeclarationModifiers.New) != 0; }
        }

        internal bool HasReadOnlyModifier => (_modifiers & DeclarationModifiers.ReadOnly) != 0;

        public override MethodSymbol? GetMethod
        {
            get { return _getMethod; }
        }

        public override MethodSymbol? SetMethod
        {
            get { return _setMethod; }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get { return (IsStatic ? 0 : Microsoft.Cci.CallingConvention.HasThis); }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_lazyParameters.IsDefault)
                {
                    var diagnostics = DiagnosticBag.GetInstance();
                    var binder = this.CreateBinderForTypeAndParameters();
                    var syntax = (BasePropertyDeclarationSyntax)_syntaxRef.GetSyntax();
                    var result = this.ComputeParameters(binder, syntax, diagnostics);
                    if (ImmutableInterlocked.InterlockedInitialize(ref _lazyParameters, result))
                    {
                        this.AddDeclarationDiagnostics(diagnostics);
                    }
                    diagnostics.Free();
                }

                return _lazyParameters;
            }
        }

        internal override bool IsExplicitInterfaceImplementation
            => (_propertyFlags & Flags.IsExplicitInterfaceImplementation) != 0;

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations
        {
            get { return _explicitInterfaceImplementations; }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return _refCustomModifiers; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return ModifierUtils.EffectiveAccessibility(_modifiers);
            }
        }

        internal bool IsAutoProperty
            => (_propertyFlags & Flags.IsAutoProperty) != 0;

        /// <summary>
        /// Backing field for automatically implemented property, or
        /// for a property with an initializer.
        /// </summary>
        internal SynthesizedBackingFieldSymbol? BackingField
        {
            get { return _backingField; }
        }

        internal override bool MustCallMethodsDirectly
        {
            get { return false; }
        }

        internal SyntaxReference SyntaxReference
        {
            get
            {
                return _syntaxRef;
            }
        }

        internal BasePropertyDeclarationSyntax CSharpSyntaxNode
        {
            get
            {
                return (BasePropertyDeclarationSyntax)_syntaxRef.GetSyntax();
            }
        }

        internal SyntaxTree SyntaxTree
        {
            get
            {
                return _syntaxRef.SyntaxTree;
            }
        }

        internal override void AfterAddingTypeMembersChecks(ConversionsBase conversions, DiagnosticBag diagnostics)
        {
            Location location = CSharpSyntaxNode.Type.Location;
            var compilation = DeclaringCompilation;

            Debug.Assert(location != null);

            // Check constraints on return type and parameters. Note: Dev10 uses the
            // property name location for any such errors. We'll do the same for return
            // type errors but for parameter errors, we'll use the parameter location.

            if ((object)_explicitInterfaceType != null)
            {
                var explicitInterfaceSpecifier = GetExplicitInterfaceSpecifier(this.CSharpSyntaxNode);
                RoslynDebug.Assert(explicitInterfaceSpecifier != null);
                _explicitInterfaceType.CheckAllConstraints(compilation, conversions, new SourceLocation(explicitInterfaceSpecifier.Name), diagnostics);

                // Note: we delayed nullable-related checks that could pull on NonNullTypes
                if (!_explicitInterfaceImplementations.IsEmpty)
                {
                    TypeSymbol.CheckNullableReferenceTypeMismatchOnImplementingMember(this.ContainingType, this, _explicitInterfaceImplementations[0], isExplicit: true, diagnostics);
                }
            }

            if (_refKind == RefKind.RefReadOnly)
            {
                compilation.EnsureIsReadOnlyAttributeExists(diagnostics, location, modifyCompilation: true);
            }

            ParameterHelpers.EnsureIsReadOnlyAttributeExists(compilation, Parameters, diagnostics, modifyCompilation: true);

            if (compilation.ShouldEmitNullableAttributes(this) &&
                this.TypeWithAnnotations.NeedsNullableAttribute())
            {
                compilation.EnsureNullableAttributeExists(diagnostics, location, modifyCompilation: true);
            }

            ParameterHelpers.EnsureNullableAttributeExists(compilation, this, Parameters, diagnostics, modifyCompilation: true);
        }

        private void CheckAccessibility(Location location, DiagnosticBag diagnostics, bool isExplicitInterfaceImplementation)
        {
            var info = ModifierUtils.CheckAccessibility(_modifiers, this, isExplicitInterfaceImplementation);
            if (info != null)
            {
                diagnostics.Add(new CSDiagnostic(info, location));
            }
        }

        private DeclarationModifiers MakeModifiers(SyntaxTokenList modifiers, bool isExplicitInterfaceImplementation,
                                                   bool isIndexer, bool accessorsHaveImplementation,
                                                   Location location, DiagnosticBag diagnostics, out bool modifierErrors)
        {
            bool isInterface = this.ContainingType.IsInterface;
            var defaultAccess = isInterface && !isExplicitInterfaceImplementation ? DeclarationModifiers.Public : DeclarationModifiers.Private;

            // Check that the set of modifiers is allowed
            var allowedModifiers = DeclarationModifiers.Unsafe;
            var defaultInterfaceImplementationModifiers = DeclarationModifiers.None;

            if (!isExplicitInterfaceImplementation)
            {
                allowedModifiers |= DeclarationModifiers.New |
                                    DeclarationModifiers.Sealed |
                                    DeclarationModifiers.Abstract |
                                    DeclarationModifiers.Virtual |
                                    DeclarationModifiers.AccessibilityMask;

                if (!isIndexer)
                {
                    allowedModifiers |= DeclarationModifiers.Static;
                }

                if (!isInterface)
                {
                    allowedModifiers |= DeclarationModifiers.Override;
                }
                else
                {
                    // This is needed to make sure we can detect 'public' modifier specified explicitly and
                    // check it against language version below.
                    defaultAccess = DeclarationModifiers.None;

                    defaultInterfaceImplementationModifiers |= DeclarationModifiers.Sealed |
                                                               DeclarationModifiers.Abstract |
                                                               (isIndexer ? 0 : DeclarationModifiers.Static) |
                                                               DeclarationModifiers.Virtual |
                                                               DeclarationModifiers.Extern |
                                                               DeclarationModifiers.AccessibilityMask;
                }
            }
            else if (isInterface)
            {
                Debug.Assert(isExplicitInterfaceImplementation);
                allowedModifiers |= DeclarationModifiers.Abstract;
            }

            if (ContainingType.IsStructType())
            {
                allowedModifiers |= DeclarationModifiers.ReadOnly;
            }

            allowedModifiers |= DeclarationModifiers.Extern;

            var mods = ModifierUtils.MakeAndCheckNontypeMemberModifiers(modifiers, defaultAccess, allowedModifiers, location, diagnostics, out modifierErrors);

            this.CheckUnsafeModifier(mods, diagnostics);

            ModifierUtils.ReportDefaultInterfaceImplementationModifiers(accessorsHaveImplementation, mods,
                                                                        defaultInterfaceImplementationModifiers,
                                                                        location, diagnostics);

            // Let's overwrite modifiers for interface properties with what they are supposed to be. 
            // Proper errors must have been reported by now.
            if (isInterface)
            {
                mods = ModifierUtils.AdjustModifiersForAnInterfaceMember(mods, accessorsHaveImplementation, isExplicitInterfaceImplementation);
            }

            if (isIndexer)
            {
                mods |= DeclarationModifiers.Indexer;
            }

            return mods;
        }

        private static ImmutableArray<ParameterSymbol> MakeParameters(
            Binder binder, SourcePropertySymbol owner, BaseParameterListSyntax? parameterSyntaxOpt, DiagnosticBag diagnostics, bool addRefReadOnlyModifier)
        {
            if (parameterSyntaxOpt == null)
            {
                return ImmutableArray<ParameterSymbol>.Empty;
            }

            if (parameterSyntaxOpt.Parameters.Count < 1)
            {
                diagnostics.Add(ErrorCode.ERR_IndexerNeedsParam, parameterSyntaxOpt.GetLastToken().GetLocation());
            }

            SyntaxToken arglistToken;
            var parameters = ParameterHelpers.MakeParameters(
                binder, owner, parameterSyntaxOpt, out arglistToken,
                allowRefOrOut: false,
                allowThis: false,
                addRefReadOnlyModifier: addRefReadOnlyModifier,
                diagnostics: diagnostics);

            if (arglistToken.Kind() != SyntaxKind.None)
            {
                diagnostics.Add(ErrorCode.ERR_IllegalVarArgs, arglistToken.GetLocation());
            }

            // There is a special warning for an indexer with exactly one parameter, which is optional.
            // ParameterHelpers already warns for default values on explicit interface implementations.
            if (parameters.Length == 1 && !owner.IsExplicitInterfaceImplementation)
            {
                ParameterSyntax parameterSyntax = parameterSyntaxOpt.Parameters[0];
                if (parameterSyntax.Default != null)
                {
                    SyntaxToken paramNameToken = parameterSyntax.Identifier;
                    diagnostics.Add(ErrorCode.WRN_DefaultValueForUnconsumedLocation, paramNameToken.GetLocation(), paramNameToken.ValueText);
                }
            }

            return parameters;
        }

        private void CheckModifiers(bool isExplicitInterfaceImplementation, Location location, bool isIndexer, DiagnosticBag diagnostics)
        {
            bool isExplicitInterfaceImplementationInInterface = isExplicitInterfaceImplementation && ContainingType.IsInterface;

            if (this.DeclaredAccessibility == Accessibility.Private && (IsVirtual || (IsAbstract && !isExplicitInterfaceImplementationInInterface) || IsOverride))
            {
                diagnostics.Add(ErrorCode.ERR_VirtualPrivate, location, this);
            }
            else if (IsStatic && (IsOverride || IsVirtual || IsAbstract))
            {
                // A static member '{0}' cannot be marked as override, virtual, or abstract
                diagnostics.Add(ErrorCode.ERR_StaticNotVirtual, location, this);
            }
            else if (IsStatic && HasReadOnlyModifier)
            {
                // Static member '{0}' cannot be marked 'readonly'.
                diagnostics.Add(ErrorCode.ERR_StaticMemberCantBeReadOnly, location, this);
            }
            else if (IsOverride && (IsNew || IsVirtual))
            {
                // A member '{0}' marked as override cannot be marked as new or virtual
                diagnostics.Add(ErrorCode.ERR_OverrideNotNew, location, this);
            }
            else if (IsSealed && !IsOverride && !(IsAbstract && isExplicitInterfaceImplementationInInterface))
            {
                // '{0}' cannot be sealed because it is not an override
                diagnostics.Add(ErrorCode.ERR_SealedNonOverride, location, this);
            }
            else if (IsAbstract && ContainingType.TypeKind == TypeKind.Struct)
            {
                // The modifier '{0}' is not valid for this item
                diagnostics.Add(ErrorCode.ERR_BadMemberFlag, location, SyntaxFacts.GetText(SyntaxKind.AbstractKeyword));
            }
            else if (IsVirtual && ContainingType.TypeKind == TypeKind.Struct)
            {
                // The modifier '{0}' is not valid for this item
                diagnostics.Add(ErrorCode.ERR_BadMemberFlag, location, SyntaxFacts.GetText(SyntaxKind.VirtualKeyword));
            }
            else if (IsAbstract && IsExtern)
            {
                diagnostics.Add(ErrorCode.ERR_AbstractAndExtern, location, this);
            }
            else if (IsAbstract && IsSealed && !isExplicitInterfaceImplementationInInterface)
            {
                diagnostics.Add(ErrorCode.ERR_AbstractAndSealed, location, this);
            }
            else if (IsAbstract && IsVirtual)
            {
                diagnostics.Add(ErrorCode.ERR_AbstractNotVirtual, location, this.Kind.Localize(), this);
            }
            else if (ContainingType.IsSealed && this.DeclaredAccessibility.HasProtected() && !this.IsOverride)
            {
                diagnostics.Add(AccessCheck.GetProtectedMemberInSealedTypeError(ContainingType), location, this);
            }
            else if (ContainingType.IsStatic && !IsStatic)
            {
                ErrorCode errorCode = isIndexer ? ErrorCode.ERR_IndexerInStaticClass : ErrorCode.ERR_InstanceMemberInStaticClass;
                diagnostics.Add(errorCode, location, this);
            }
        }

        // Create AccessorSymbol for AccessorDeclarationSyntax
        private SourcePropertyAccessorSymbol? CreateAccessorSymbol(AccessorDeclarationSyntax? syntaxOpt,
            PropertySymbol? explicitlyImplementedPropertyOpt, string aliasQualifierOpt, bool isAutoPropertyAccessor, bool isExplicitInterfaceImplementation, DiagnosticBag diagnostics)
        {
            if (syntaxOpt == null)
            {
                return null;
            }
            return SourcePropertyAccessorSymbol.CreateAccessorSymbol(_containingType, this, _modifiers, _sourceName, syntaxOpt,
                explicitlyImplementedPropertyOpt, aliasQualifierOpt, isAutoPropertyAccessor, isExplicitInterfaceImplementation, diagnostics);
        }

        private void CheckAccessibilityMoreRestrictive(SourcePropertyAccessorSymbol? accessor, DiagnosticBag diagnostics)
        {
            if (((object?)accessor != null) &&
                !IsAccessibilityMoreRestrictive(this.DeclaredAccessibility, accessor.LocalAccessibility))
            {
                diagnostics.Add(ErrorCode.ERR_InvalidPropertyAccessMod, accessor.Locations[0], accessor, this);
            }
        }

        /// <summary>
        /// Return true if the accessor accessibility is more restrictive
        /// than the property accessibility, otherwise false.
        /// </summary>
        private static bool IsAccessibilityMoreRestrictive(Accessibility property, Accessibility accessor)
        {
            if (accessor == Accessibility.NotApplicable)
            {
                return true;
            }
            return (accessor < property) &&
                ((accessor != Accessibility.Protected) || (property != Accessibility.Internal));
        }

        private static void CheckAbstractPropertyAccessorNotPrivate(SourcePropertyAccessorSymbol accessor, DiagnosticBag diagnostics)
        {
            if (accessor.LocalAccessibility == Accessibility.Private)
            {
                diagnostics.Add(ErrorCode.ERR_PrivateAbstractAccessor, accessor.Locations[0], accessor);
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo? preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            ref var lazyDocComment = ref expandIncludes ? ref _lazyExpandedDocComment : ref _lazyDocComment;
            return SourceDocumentationCommentUtils.GetAndCacheDocumentationComment(this, expandIncludes, ref lazyDocComment);
        }

        // Separate these checks out of FindExplicitlyImplementedProperty because they depend on the accessor symbols,
        // which depend on the explicitly implemented property
        private void CheckExplicitImplementationAccessor(MethodSymbol? thisAccessor, MethodSymbol? otherAccessor, PropertySymbol explicitlyImplementedProperty, DiagnosticBag diagnostics)
        {
            var thisHasAccessor = (object?)thisAccessor != null;
            var otherHasAccessor = otherAccessor.IsImplementable();

            if (otherHasAccessor && !thisHasAccessor)
            {
                diagnostics.Add(ErrorCode.ERR_ExplicitPropertyMissingAccessor, this.Location, this, otherAccessor);
            }
            else if (!otherHasAccessor && thisHasAccessor)
            {
                diagnostics.Add(ErrorCode.ERR_ExplicitPropertyAddingAccessor, thisAccessor!.Locations[0], thisAccessor, explicitlyImplementedProperty);
            }
        }

        internal override OverriddenOrHiddenMembersResult OverriddenOrHiddenMembers
        {
            get
            {
                if (_lazyOverriddenOrHiddenMembers == null)
                {
                    Interlocked.CompareExchange(ref _lazyOverriddenOrHiddenMembers, this.MakeOverriddenOrHiddenMembers(), null);
                }
                return _lazyOverriddenOrHiddenMembers;
            }
        }

        /// <summary>
        /// If this property is sealed, then we have to emit both accessors - regardless of whether
        /// they are present in the source - so that they can be marked final. (i.e. sealed).
        /// </summary>
        internal SynthesizedSealedPropertyAccessor? SynthesizedSealedAccessorOpt
        {
            get
            {
                bool hasGetter = (object?)_getMethod != null;
                bool hasSetter = (object?)_setMethod != null;
                if (!this.IsSealed || (hasGetter && hasSetter))
                {
                    return null;
                }

                // This has to be cached because the CCI layer depends on reference equality.
                // However, there's no point in having more than one field, since we don't
                // expect to have to synthesize more than one accessor.
                if ((object?)_lazySynthesizedSealedAccessor == null)
                {
                    Interlocked.CompareExchange(ref _lazySynthesizedSealedAccessor, MakeSynthesizedSealedAccessor(), null);
                }
                return _lazySynthesizedSealedAccessor;
            }
        }

        /// <remarks>
        /// Only non-null for sealed properties without both accessors.
        /// </remarks>
        private SynthesizedSealedPropertyAccessor? MakeSynthesizedSealedAccessor()
        {
            Debug.Assert(this.IsSealed && ((object?)_getMethod == null || (object?)_setMethod == null));

            if ((object?)_getMethod != null)
            {
                // need to synthesize setter
                MethodSymbol overriddenAccessor = this.GetOwnOrInheritedSetMethod();
                return (object)overriddenAccessor == null ? null : new SynthesizedSealedPropertyAccessor(this, overriddenAccessor);
            }
            else if ((object?)_setMethod != null)
            {
                // need to synthesize getter
                MethodSymbol overriddenAccessor = this.GetOwnOrInheritedGetMethod();
                return (object)overriddenAccessor == null ? null : new SynthesizedSealedPropertyAccessor(this, overriddenAccessor);
            }
            else
            {
                // Arguably, it would be more correct to return an array containing two
                // synthesized accessors, but we're already in an error case, so we'll
                // minimize the cascading error behavior by suppressing synthesis.
                return null;
            }
        }

        #region Attributes

        IAttributeTargetSymbol IAttributeTargetSymbol.AttributesOwner
        {
            get { return this; }
        }

        AttributeLocation IAttributeTargetSymbol.DefaultAttributeLocation
        {
            get { return AttributeLocation.Property; }
        }

        AttributeLocation IAttributeTargetSymbol.AllowedAttributeLocations
            => (_propertyFlags & Flags.IsAutoProperty) != 0
                ? AttributeLocation.Property | AttributeLocation.Field
                : AttributeLocation.Property;

        /// <summary>
        /// Returns a bag of custom attributes applied on the property and data decoded from well-known attributes. Returns null if there are no attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        private CustomAttributesBag<CSharpAttributeData> GetAttributesBag()
        {
            var bag = _lazyCustomAttributesBag;
            if (bag != null && bag.IsSealed)
            {
                return bag;
            }

            // The property is responsible for completion of the backing field
            _ = _backingField?.GetAttributes();

            if (LoadAndValidateAttributes(OneOrMany.Create(this.CSharpSyntaxNode.AttributeLists), ref _lazyCustomAttributesBag))
            {
                var completed = _state.NotePartComplete(CompletionPart.Attributes);
                Debug.Assert(completed);
            }

#nullable disable // '_lazyCustomAttributesBag' can't be null here because 'earlyDecodingOnly' was not passed as false to 'LoadAndValidateAttributes', but the compiler can't tell https://github.com/dotnet/roslyn/issues/39166
            Debug.Assert(_lazyCustomAttributesBag.IsSealed);
#nullable enable
            return _lazyCustomAttributesBag;
        }

        /// <summary>
        /// Gets the attributes applied on this symbol.
        /// Returns an empty array if there are no attributes.
        /// </summary>
        /// <remarks>
        /// NOTE: This method should always be kept as a sealed override.
        /// If you want to override attribute binding logic for a sub-class, then override <see cref="GetAttributesBag"/> method.
        /// </remarks>
        public sealed override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.GetAttributesBag().Attributes;
        }

        /// <summary>
        /// Returns data decoded from well-known attributes applied to the symbol or null if there are no applied attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        private PropertyWellKnownAttributeData GetDecodedWellKnownAttributeData()
        {
            var attributesBag = _lazyCustomAttributesBag;
            if (attributesBag == null || !attributesBag.IsDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetAttributesBag();
            }

            return (PropertyWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData;
        }

        /// <summary>
        /// Returns data decoded from special early bound well-known attributes applied to the symbol or null if there are no applied attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        internal PropertyEarlyWellKnownAttributeData GetEarlyDecodedWellKnownAttributeData()
        {
            var attributesBag = _lazyCustomAttributesBag;
            if (attributesBag == null || !attributesBag.IsEarlyDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetAttributesBag();
            }

            return (PropertyEarlyWellKnownAttributeData)attributesBag.EarlyDecodedWellKnownAttributeData;
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData>? attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = this.DeclaringCompilation;
            var type = this.TypeWithAnnotations;

            if (type.Type.ContainsDynamic())
            {
                AddSynthesizedAttribute(ref attributes,
                    compilation.SynthesizeDynamicAttribute(type.Type, type.CustomModifiers.Length + RefCustomModifiers.Length, _refKind));
            }

            if (type.Type.ContainsTupleNames())
            {
                AddSynthesizedAttribute(ref attributes,
                    compilation.SynthesizeTupleNamesAttribute(type.Type));
            }

            if (compilation.ShouldEmitNullableAttributes(this))
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeNullableAttributeIfNecessary(this, ContainingType.GetNullableContextValue(), type));
            }

            if (this.ReturnsByRefReadonly)
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeIsReadOnlyAttribute(this));
            }
        }

        internal sealed override bool IsDirectlyExcludedFromCodeCoverage =>
            GetDecodedWellKnownAttributeData()?.HasExcludeFromCodeCoverageAttribute == true;

        internal override bool HasSpecialName
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasSpecialNameAttribute;
            }
        }

        internal override CSharpAttributeData? EarlyDecodeWellKnownAttribute(ref EarlyDecodeWellKnownAttributeArguments<EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation> arguments)
        {
            CSharpAttributeData? boundAttribute;
            ObsoleteAttributeData? obsoleteData;

            if (EarlyDecodeDeprecatedOrExperimentalOrObsoleteAttribute(ref arguments, out boundAttribute, out obsoleteData))
            {
                if (obsoleteData != null)
                {
                    arguments.GetOrCreateData<PropertyEarlyWellKnownAttributeData>().ObsoleteAttributeData = obsoleteData;
                }

                return boundAttribute;
            }

            if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.IndexerNameAttribute))
            {
                bool hasAnyDiagnostics;
                boundAttribute = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, out hasAnyDiagnostics);
                if (!boundAttribute.HasErrors)
                {
                    string? indexerName = boundAttribute.CommonConstructorArguments[0].DecodeValue<string>(SpecialType.System_String);
                    if (indexerName != null)
                    {
                        arguments.GetOrCreateData<PropertyEarlyWellKnownAttributeData>().IndexerName = indexerName;
                    }

                    if (!hasAnyDiagnostics)
                    {
                        return boundAttribute;
                    }
                }

                return null;
            }

            return base.EarlyDecodeWellKnownAttribute(ref arguments);
        }

        /// <summary>
        /// Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        /// This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        /// </summary>
        internal override ObsoleteAttributeData? ObsoleteAttributeData
        {
            get
            {
                if (!_containingType.AnyMemberHasAttributes)
                {
                    return null;
                }

                var lazyCustomAttributesBag = _lazyCustomAttributesBag;
                if (lazyCustomAttributesBag != null && lazyCustomAttributesBag.IsEarlyDecodedWellKnownAttributeDataComputed)
                {
                    return ((PropertyEarlyWellKnownAttributeData)lazyCustomAttributesBag.EarlyDecodedWellKnownAttributeData)?.ObsoleteAttributeData;
                }

                return ObsoleteAttributeData.Uninitialized;
            }
        }

        internal override void DecodeWellKnownAttribute(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            RoslynDebug.Assert(arguments.AttributeSyntaxOpt != null);

            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);
            Debug.Assert(arguments.SymbolPart == AttributeLocation.None);

            if (attribute.IsTargetAttribute(this, AttributeDescription.IndexerNameAttribute))
            {
                //NOTE: decoding was done by EarlyDecodeWellKnownAttribute.
                ValidateIndexerNameAttribute(attribute, arguments.AttributeSyntaxOpt, arguments.Diagnostics);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.SpecialNameAttribute))
            {
                arguments.GetOrCreateData<PropertyWellKnownAttributeData>().HasSpecialNameAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.ExcludeFromCodeCoverageAttribute))
            {
                arguments.GetOrCreateData<PropertyWellKnownAttributeData>().HasExcludeFromCodeCoverageAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.DynamicAttribute))
            {
                // DynamicAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitDynamicAttr, arguments.AttributeSyntaxOpt.Location);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.IsReadOnlyAttribute))
            {
                // IsReadOnlyAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitReservedAttr, arguments.AttributeSyntaxOpt.Location, AttributeDescription.IsReadOnlyAttribute.FullName);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.IsUnmanagedAttribute))
            {
                // IsUnmanagedAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitReservedAttr, arguments.AttributeSyntaxOpt.Location, AttributeDescription.IsUnmanagedAttribute.FullName);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.IsByRefLikeAttribute))
            {
                // IsByRefLikeAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitReservedAttr, arguments.AttributeSyntaxOpt.Location, AttributeDescription.IsByRefLikeAttribute.FullName);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.TupleElementNamesAttribute))
            {
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitTupleElementNamesAttribute, arguments.AttributeSyntaxOpt.Location);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.NullableAttribute))
            {
                // NullableAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitNullableAttribute, arguments.AttributeSyntaxOpt.Location);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.DisallowNullAttribute))
            {
                arguments.GetOrCreateData<PropertyWellKnownAttributeData>().HasDisallowNullAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.AllowNullAttribute))
            {
                arguments.GetOrCreateData<PropertyWellKnownAttributeData>().HasAllowNullAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.MaybeNullAttribute))
            {
                arguments.GetOrCreateData<PropertyWellKnownAttributeData>().HasMaybeNullAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.NotNullAttribute))
            {
                arguments.GetOrCreateData<PropertyWellKnownAttributeData>().HasNotNullAttribute = true;
            }
        }

        internal bool HasDisallowNull
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasDisallowNullAttribute;
            }
        }

        internal bool HasAllowNull
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasAllowNullAttribute;
            }
        }

        internal bool HasMaybeNull
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasMaybeNullAttribute;
            }
        }

        internal bool HasNotNull
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasNotNullAttribute;
            }
        }

        internal SourceAttributeData DisallowNullAttributeIfExists
            => FindAttribute(AttributeDescription.DisallowNullAttribute);

        internal SourceAttributeData AllowNullAttributeIfExists
            => FindAttribute(AttributeDescription.AllowNullAttribute);

        internal SourceAttributeData MaybeNullAttributeIfExists
            => FindAttribute(AttributeDescription.MaybeNullAttribute);

        internal SourceAttributeData NotNullAttributeIfExists
            => FindAttribute(AttributeDescription.NotNullAttribute);

        private SourceAttributeData FindAttribute(AttributeDescription attributeDescription)
            => (SourceAttributeData)GetAttributes().First(a => a.IsTargetAttribute(this, attributeDescription));

        internal override void PostDecodeWellKnownAttributes(ImmutableArray<CSharpAttributeData> boundAttributes, ImmutableArray<AttributeSyntax> allAttributeSyntaxNodes, DiagnosticBag diagnostics, AttributeLocation symbolPart, WellKnownAttributeData? decodedData)
        {
            Debug.Assert(!boundAttributes.IsDefault);
            Debug.Assert(!allAttributeSyntaxNodes.IsDefault);
            Debug.Assert(boundAttributes.Length == allAttributeSyntaxNodes.Length);
            RoslynDebug.Assert(_lazyCustomAttributesBag != null);
            Debug.Assert(_lazyCustomAttributesBag.IsDecodedWellKnownAttributeDataComputed);
            Debug.Assert(symbolPart == AttributeLocation.None);

            base.PostDecodeWellKnownAttributes(boundAttributes, allAttributeSyntaxNodes, diagnostics, symbolPart, decodedData);
        }

        private void ValidateIndexerNameAttribute(CSharpAttributeData attribute, AttributeSyntax node, DiagnosticBag diagnostics)
        {
            if (!this.IsIndexer || this.IsExplicitInterfaceImplementation)
            {
                diagnostics.Add(ErrorCode.ERR_BadIndexerNameAttr, node.Name.Location, node.GetErrorDisplayName());
            }
            else
            {
                string? indexerName = attribute.CommonConstructorArguments[0].DecodeValue<string>(SpecialType.System_String);
                if (indexerName == null || !SyntaxFacts.IsValidIdentifier(indexerName))
                {
#nullable disable // Can 'node.ArgumentList' be null? https://github.com/dotnet/roslyn/issues/39166
                    diagnostics.Add(ErrorCode.ERR_BadArgumentToAttribute, node.ArgumentList.Arguments[0].Location, node.GetErrorDisplayName());
#nullable enable
                }
            }
        }

        #endregion

        #region Completion

        internal sealed override bool RequiresCompletion
        {
            get { return true; }
        }

        internal sealed override bool HasComplete(CompletionPart part)
        {
            return _state.HasComplete(part);
        }

        internal override void ForceComplete(SourceLocation? locationOpt, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var incompletePart = _state.NextIncompletePart;
                switch (incompletePart)
                {
                    case CompletionPart.Attributes:
                        GetAttributes();
                        break;

                    case CompletionPart.StartPropertyParameters:
                    case CompletionPart.FinishPropertyParameters:
                        {
                            if (_state.NotePartComplete(CompletionPart.StartPropertyParameters))
                            {
                                var parameters = this.Parameters;
                                if (parameters.Length > 0)
                                {
                                    var diagnostics = DiagnosticBag.GetInstance();
                                    var conversions = new TypeConversions(this.ContainingAssembly.CorLibrary);
                                    foreach (var parameter in this.Parameters)
                                    {
                                        parameter.ForceComplete(locationOpt, cancellationToken);
                                        parameter.Type.CheckAllConstraints(DeclaringCompilation, conversions, parameter.Locations[0], diagnostics);
                                    }

                                    this.AddDeclarationDiagnostics(diagnostics);
                                    diagnostics.Free();
                                }

                                DeclaringCompilation.SymbolDeclaredEvent(this);
                                var completedOnThisThread = _state.NotePartComplete(CompletionPart.FinishPropertyParameters);
                                Debug.Assert(completedOnThisThread);
                            }
                            else
                            {
                                // StartPropertyParameters was completed by another thread. Wait for it to finish the parameters.
                                _state.SpinWaitComplete(CompletionPart.FinishPropertyParameters, cancellationToken);
                            }
                        }
                        break;

                    case CompletionPart.StartPropertyType:
                    case CompletionPart.FinishPropertyType:
                        {
                            if (_state.NotePartComplete(CompletionPart.StartPropertyType))
                            {
                                var diagnostics = DiagnosticBag.GetInstance();
                                var conversions = new TypeConversions(this.ContainingAssembly.CorLibrary);
                                this.Type.CheckAllConstraints(DeclaringCompilation, conversions, _location, diagnostics);

                                var type = this.Type;
                                if (type.IsRestrictedType(ignoreSpanLikeTypes: true))
                                {
                                    diagnostics.Add(ErrorCode.ERR_FieldCantBeRefAny, this.CSharpSyntaxNode.Type.Location, type);
                                }
                                else if (this.IsAutoProperty && type.IsRefLikeType && (this.IsStatic || !this.ContainingType.IsRefLikeType))
                                {
                                    diagnostics.Add(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, this.CSharpSyntaxNode.Type.Location, type);
                                }

                                this.AddDeclarationDiagnostics(diagnostics);
                                var completedOnThisThread = _state.NotePartComplete(CompletionPart.FinishPropertyType);
                                Debug.Assert(completedOnThisThread);
                                diagnostics.Free();
                            }
                            else
                            {
                                // StartPropertyType was completed by another thread. Wait for it to finish the type.
                                _state.SpinWaitComplete(CompletionPart.FinishPropertyType, cancellationToken);
                            }
                        }
                        break;

                    case CompletionPart.None:
                        return;

                    default:
                        // any other values are completion parts intended for other kinds of symbols
                        _state.NotePartComplete(CompletionPart.All & ~CompletionPart.PropertySymbolAll);
                        break;
                }

                _state.SpinWaitComplete(incompletePart, cancellationToken);
            }
        }

        #endregion

        private TypeWithAnnotations ComputeType(Binder binder, BasePropertyDeclarationSyntax syntax, DiagnosticBag diagnostics)
        {
            RefKind refKind;
            var typeSyntax = syntax.Type.SkipRef(out refKind);
            var type = binder.BindType(typeSyntax, diagnostics);
            HashSet<DiagnosticInfo>? useSiteDiagnostics = null;

            if (syntax.ExplicitInterfaceSpecifier == null && !this.IsNoMoreVisibleThan(type, ref useSiteDiagnostics))
            {
                // "Inconsistent accessibility: indexer return type '{1}' is less accessible than indexer '{0}'"
                // "Inconsistent accessibility: property type '{1}' is less accessible than property '{0}'"
                diagnostics.Add((this.IsIndexer ? ErrorCode.ERR_BadVisIndexerReturn : ErrorCode.ERR_BadVisPropertyType), _location, this, type.Type);
            }

            diagnostics.Add(_location, useSiteDiagnostics);

            if (type.IsVoidType())
            {
                ErrorCode errorCode = this.IsIndexer ? ErrorCode.ERR_IndexerCantHaveVoidType : ErrorCode.ERR_PropertyCantHaveVoidType;
                diagnostics.Add(errorCode, _location, this);
            }

            return type;
        }

        private ImmutableArray<ParameterSymbol> ComputeParameters(Binder binder, BasePropertyDeclarationSyntax syntax, DiagnosticBag diagnostics)
        {
            var parameterSyntaxOpt = GetParameterListSyntax(syntax);
            var parameters = MakeParameters(binder, this, parameterSyntaxOpt, diagnostics, addRefReadOnlyModifier: IsVirtual || IsAbstract);
            HashSet<DiagnosticInfo>? useSiteDiagnostics = null;

            foreach (ParameterSymbol param in parameters)
            {
                if (syntax.ExplicitInterfaceSpecifier == null && !this.IsNoMoreVisibleThan(param.Type, ref useSiteDiagnostics))
                {
                    diagnostics.Add(ErrorCode.ERR_BadVisIndexerParam, _location, this, param.Type);
                }
                else if ((object?)_setMethod != null && param.Name == ParameterSymbol.ValueParameterName)
                {
                    diagnostics.Add(ErrorCode.ERR_DuplicateGeneratedName, param.Locations.FirstOrDefault() ?? _location, param.Name);
                }
            }

            diagnostics.Add(_location, useSiteDiagnostics);
            return parameters;
        }

        private Binder CreateBinderForTypeAndParameters()
        {
            var compilation = this.DeclaringCompilation;
            var syntaxTree = _syntaxRef.SyntaxTree;
            var syntax = (BasePropertyDeclarationSyntax)_syntaxRef.GetSyntax();
            var binderFactory = compilation.GetBinderFactory(syntaxTree);
            var binder = binderFactory.GetBinder(syntax, syntax, this);
            SyntaxTokenList modifiers = syntax.Modifiers;
            binder = binder.WithUnsafeRegionIfNecessary(modifiers);
            return binder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks, this);
        }

        private static ExplicitInterfaceSpecifierSyntax? GetExplicitInterfaceSpecifier(BasePropertyDeclarationSyntax syntax)
        {
            switch (syntax.Kind())
            {
                case SyntaxKind.PropertyDeclaration:
                    return ((PropertyDeclarationSyntax)syntax).ExplicitInterfaceSpecifier;
                case SyntaxKind.IndexerDeclaration:
                    return ((IndexerDeclarationSyntax)syntax).ExplicitInterfaceSpecifier;
                default:
                    throw ExceptionUtilities.UnexpectedValue(syntax.Kind());
            }
        }

        private static BaseParameterListSyntax? GetParameterListSyntax(BasePropertyDeclarationSyntax syntax)
        {
            return (syntax.Kind() == SyntaxKind.IndexerDeclaration) ? ((IndexerDeclarationSyntax)syntax).ParameterList : null;
        }
    }
}
