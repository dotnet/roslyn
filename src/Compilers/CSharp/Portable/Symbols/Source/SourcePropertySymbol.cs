// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    internal sealed class SourcePropertySymbol : SourcePropertySymbolBase
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
        private readonly DeclarationModifiers _modifiers;
        private readonly ImmutableArray<CustomModifier> _refCustomModifiers;
        private readonly SourcePropertyAccessorSymbol _getMethod;
        private readonly SourcePropertyAccessorSymbol _setMethod;
        private readonly TypeSymbol _explicitInterfaceType;
        private readonly ImmutableArray<PropertySymbol> _explicitInterfaceImplementations;
        private readonly Flags _propertyFlags;
        private readonly RefKind _refKind;

        private ImmutableArray<ParameterSymbol> _lazyParameters;
        private TypeWithAnnotations.Boxed _lazyType;

        /// <summary>
        /// Set in constructor, might be changed while decoding <see cref="IndexerNameAttribute"/>.
        /// </summary>
        private readonly string _sourceName;

        private string _lazyDocComment;
        private string _lazyExpandedDocComment;
        private SynthesizedSealedPropertyAccessor _lazySynthesizedSealedAccessor;

        // CONSIDER: if the parameters were computed lazily, ParameterCount could be overridden to fall back on the syntax (as in SourceMemberMethodSymbol).

        private SourcePropertySymbol(
           SourceMemberContainerTypeSymbol containingType,
           Binder bodyBinder,
           BasePropertyDeclarationSyntax syntax,
           string name,
           Location location,
           DiagnosticBag diagnostics)
            : base(containingType, syntax.GetReference(), location)
        {
            // This has the value that IsIndexer will ultimately have, once we've populated the fields of this object.
            bool isIndexer = syntax.Kind() == SyntaxKind.IndexerDeclaration;
            var interfaceSpecifier = syntax.ExplicitInterfaceSpecifier;
            bool isExplicitInterfaceImplementation = interfaceSpecifier != null;
            if (isExplicitInterfaceImplementation)
            {
                _propertyFlags |= Flags.IsExplicitInterfaceImplementation;
            }

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
            bool hasInitializer = !isIndexer && propertySyntax.Initializer != null;

            GetAcessorDeclarations(syntax, diagnostics, out bool isAutoProperty, out bool hasAccessorList,
                                   out AccessorDeclarationSyntax getSyntax, out AccessorDeclarationSyntax setSyntax);

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
                CustomAttributesBag<CSharpAttributeData> temp = null;
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
                CheckInitializer(isAutoProperty, containingType.IsInterface, IsStatic, location, diagnostics);
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
                    bool isInitOnly = !IsStatic && setSyntax?.Keyword.IsKind(SyntaxKind.InitKeyword) == true;
                    BackingField = new SynthesizedBackingFieldSymbol(this,
                                                                          fieldName,
                                                                          isReadOnly: isGetterOnly || isInitOnly,
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

            PropertySymbol explicitlyImplementedProperty = null;
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
                var type = this.ComputeType(bodyBinder, syntax.Type, diagnostics);
                _lazyType = new TypeWithAnnotations.Boxed(type);
                _lazyParameters = this.ComputeParameters(bodyBinder, syntax, diagnostics);

                bool isOverride = false;
                PropertySymbol overriddenOrImplementedProperty = null;

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

                if ((object)overriddenOrImplementedProperty != null)
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
                            diagnostics.Add(ErrorCode.ERR_AutoPropertyMustHaveGetAccessor, accessor.Locations[0], accessor);
                        }
                    }
                }

                // Check accessor accessibility is more restrictive than property accessibility.
                CheckAccessibilityMoreRestrictive(_getMethod, diagnostics);
                CheckAccessibilityMoreRestrictive(_setMethod, diagnostics);

                if (((object)_getMethod != null) && ((object)_setMethod != null))
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
                        if ((object)accessor != null)
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

            if ((object)explicitlyImplementedProperty != null)
            {
                CheckExplicitImplementationAccessor(this.GetMethod, explicitlyImplementedProperty.GetMethod, explicitlyImplementedProperty, diagnostics);
                CheckExplicitImplementationAccessor(this.SetMethod, explicitlyImplementedProperty.SetMethod, explicitlyImplementedProperty, diagnostics);
            }

            _explicitInterfaceImplementations =
                (object)explicitlyImplementedProperty == null ?
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
                                                   out AccessorDeclarationSyntax getSyntax, out AccessorDeclarationSyntax setSyntax)
        {
            isAutoProperty = true;
            hasAccessorList = syntax.AccessorList != null;
            getSyntax = null;
            setSyntax = null;

            if (hasAccessorList)
            {
                foreach (var accessor in syntax.AccessorList.Accessors)
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
                        case SyntaxKind.InitAccessorDeclaration:
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
            bool isInterface,
            bool isStatic,
            Location location,
            DiagnosticBag diagnostics)
        {
            if (isInterface && !isStatic)
            {
                diagnostics.Add(ErrorCode.ERR_InstancePropertyInitializerInInterface, location, this);
            }
            else if (!isAutoProperty)
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
                    var result = this.ComputeType(binder, syntax.Type, diagnostics);
                    if (Interlocked.CompareExchange(ref _lazyType, new TypeWithAnnotations.Boxed(result), null) == null)
                    {
                        this.AddDeclarationDiagnostics(diagnostics);
                    }
                    diagnostics.Free();
                }

                return _lazyType.Value;
            }
        }

        internal override bool HasPointerType
        {
            get
            {
                if (_lazyType != null)
                {

                    var hasPointerType = _lazyType.Value.DefaultType.IsPointerOrFunctionPointer();
                    Debug.Assert(hasPointerType == hasPointerTypeSyntactically());
                    return hasPointerType;
                }

                return hasPointerTypeSyntactically();

                bool hasPointerTypeSyntactically()
                {
                    var syntax = (BasePropertyDeclarationSyntax)_syntaxRef.GetSyntax();
                    var typeSyntax = syntax.Type.SkipRef(out _);
                    return typeSyntax.Kind() switch { SyntaxKind.PointerType => true, SyntaxKind.FunctionPointerType => true, _ => false };
                }
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

        internal override LexicalSortKey GetLexicalSortKey()
        {
            return new LexicalSortKey(Location, this.DeclaringCompilation);
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

        public override MethodSymbol GetMethod
        {
            get { return _getMethod; }
        }

        public override MethodSymbol SetMethod
        {
            get { return _setMethod; }
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

        internal override bool IsAutoProperty
            => (_propertyFlags & Flags.IsAutoProperty) != 0;

        /// <summary>
        /// Backing field for automatically implemented property, or
        /// for a property with an initializer.
        /// </summary>
        internal override SynthesizedBackingFieldSymbol BackingField { get; }

        internal BasePropertyDeclarationSyntax CSharpSyntaxNode
        {
            get
            {
                return (BasePropertyDeclarationSyntax)_syntaxRef.GetSyntax();
            }
        }

        protected override Location TypeLocation => CSharpSyntaxNode.Type.Location;

        public override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList
            => CSharpSyntaxNode.AttributeLists;

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
                var explicitInterfaceSpecifier = CSharpSyntaxNode.ExplicitInterfaceSpecifier;
                Debug.Assert(explicitInterfaceSpecifier != null);
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

            if (Type.ContainsNativeInteger())
            {
                compilation.EnsureNativeIntegerAttributeExists(diagnostics, location, modifyCompilation: true);
            }

            ParameterHelpers.EnsureNativeIntegerAttributeExists(compilation, Parameters, diagnostics, modifyCompilation: true);

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
            Binder binder, SourcePropertySymbol owner, BaseParameterListSyntax parameterSyntaxOpt, DiagnosticBag diagnostics, bool addRefReadOnlyModifier)
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
        private SourcePropertyAccessorSymbol CreateAccessorSymbol(AccessorDeclarationSyntax syntaxOpt,
            PropertySymbol explicitlyImplementedPropertyOpt, string aliasQualifierOpt, bool isAutoPropertyAccessor, bool isExplicitInterfaceImplementation, DiagnosticBag diagnostics)
        {
            if (syntaxOpt == null)
            {
                return null;
            }
            return SourcePropertyAccessorSymbol.CreateAccessorSymbol(_containingType, this, _modifiers, _sourceName, syntaxOpt,
                explicitlyImplementedPropertyOpt, aliasQualifierOpt, isAutoPropertyAccessor, isExplicitInterfaceImplementation, diagnostics);
        }

        private void CheckAccessibilityMoreRestrictive(SourcePropertyAccessorSymbol accessor, DiagnosticBag diagnostics)
        {
            if (((object)accessor != null) &&
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

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            ref var lazyDocComment = ref expandIncludes ? ref _lazyExpandedDocComment : ref _lazyDocComment;
            return SourceDocumentationCommentUtils.GetAndCacheDocumentationComment(this, expandIncludes, ref lazyDocComment);
        }

        // Separate these checks out of FindExplicitlyImplementedProperty because they depend on the accessor symbols,
        // which depend on the explicitly implemented property
        private void CheckExplicitImplementationAccessor(MethodSymbol thisAccessor, MethodSymbol otherAccessor, PropertySymbol explicitlyImplementedProperty, DiagnosticBag diagnostics)
        {
            var thisHasAccessor = (object)thisAccessor != null;
            var otherHasAccessor = otherAccessor.IsImplementable();

            if (otherHasAccessor && !thisHasAccessor)
            {
                diagnostics.Add(ErrorCode.ERR_ExplicitPropertyMissingAccessor, this.Location, this, otherAccessor);
            }
            else if (!otherHasAccessor && thisHasAccessor)
            {
                diagnostics.Add(ErrorCode.ERR_ExplicitPropertyAddingAccessor, thisAccessor.Locations[0], thisAccessor, explicitlyImplementedProperty);
            }
            else if (TypeSymbol.HaveInitOnlyMismatch(thisAccessor, otherAccessor))
            {
                Debug.Assert(thisAccessor.MethodKind == MethodKind.PropertySet);
                diagnostics.Add(ErrorCode.ERR_ExplicitPropertyMismatchInitOnly, thisAccessor.Locations[0], thisAccessor, otherAccessor);
            }
        }

        /// <summary>
        /// If this property is sealed, then we have to emit both accessors - regardless of whether
        /// they are present in the source - so that they can be marked final. (i.e. sealed).
        /// </summary>
        internal SynthesizedSealedPropertyAccessor SynthesizedSealedAccessorOpt
        {
            get
            {
                bool hasGetter = (object)_getMethod != null;
                bool hasSetter = (object)_setMethod != null;
                if (!this.IsSealed || (hasGetter && hasSetter))
                {
                    return null;
                }

                // This has to be cached because the CCI layer depends on reference equality.
                // However, there's no point in having more than one field, since we don't
                // expect to have to synthesize more than one accessor.
                if ((object)_lazySynthesizedSealedAccessor == null)
                {
                    Interlocked.CompareExchange(ref _lazySynthesizedSealedAccessor, MakeSynthesizedSealedAccessor(), null);
                }
                return _lazySynthesizedSealedAccessor;
            }
        }

        /// <remarks>
        /// Only non-null for sealed properties without both accessors.
        /// </remarks>
        private SynthesizedSealedPropertyAccessor MakeSynthesizedSealedAccessor()
        {
            Debug.Assert(this.IsSealed && ((object)_getMethod == null || (object)_setMethod == null));

            if ((object)_getMethod != null)
            {
                // need to synthesize setter
                MethodSymbol overriddenAccessor = this.GetOwnOrInheritedSetMethod();
                return (object)overriddenAccessor == null ? null : new SynthesizedSealedPropertyAccessor(this, overriddenAccessor);
            }
            else if ((object)_setMethod != null)
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

        protected override IAttributeTargetSymbol AttributesOwner => this;

        protected override AttributeLocation AllowedAttributeLocations
            => (_propertyFlags & Flags.IsAutoProperty) != 0
                ? AttributeLocation.Property | AttributeLocation.Field
                : AttributeLocation.Property;

        protected override AttributeLocation DefaultAttributeLocation => AttributeLocation.Property;

        private ImmutableArray<ParameterSymbol> ComputeParameters(Binder binder, BasePropertyDeclarationSyntax syntax, DiagnosticBag diagnostics)
        {
            var parameterSyntaxOpt = GetParameterListSyntax(syntax);
            var parameters = MakeParameters(binder, this, parameterSyntaxOpt, diagnostics, addRefReadOnlyModifier: IsVirtual || IsAbstract);
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            foreach (ParameterSymbol param in parameters)
            {
                if (syntax.ExplicitInterfaceSpecifier == null && !this.IsNoMoreVisibleThan(param.Type, ref useSiteDiagnostics))
                {
                    diagnostics.Add(ErrorCode.ERR_BadVisIndexerParam, Location, this, param.Type);
                }
                else if ((object)_setMethod != null && param.Name == ParameterSymbol.ValueParameterName)
                {
                    diagnostics.Add(ErrorCode.ERR_DuplicateGeneratedName, param.Locations.FirstOrDefault() ?? Location, param.Name);
                }
            }

            diagnostics.Add(Location, useSiteDiagnostics);
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

        private static BaseParameterListSyntax GetParameterListSyntax(BasePropertyDeclarationSyntax syntax)
        {
            return (syntax.Kind() == SyntaxKind.IndexerDeclaration) ? ((IndexerDeclarationSyntax)syntax).ParameterList : null;
        }
    }
}
