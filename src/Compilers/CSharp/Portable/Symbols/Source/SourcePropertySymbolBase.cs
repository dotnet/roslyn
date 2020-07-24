// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
    internal abstract class SourcePropertySymbolBase : PropertySymbol, IAttributeTargetSymbol
    {
        /// <summary>
        /// Condensed flags storing useful information about the <see cref="SourcePropertySymbolBase"/>
        /// so that we do not have to go back to source to compute this data.
        /// </summary>
        [Flags]
        private enum Flags : byte
        {
            IsExpressionBodied = 1 << 0,
            IsAutoProperty = 1 << 1,
            IsExplicitInterfaceImplementation = 1 << 2,
        }

        // TODO (tomat): consider splitting into multiple subclasses/rare data.

        private readonly SourceMemberContainerTypeSymbol _containingType;
        private readonly string _name;
        private readonly SyntaxReference _syntaxRef;
        protected readonly DeclarationModifiers _modifiers;
        private readonly ImmutableArray<CustomModifier> _refCustomModifiers;
        private readonly SourcePropertyAccessorSymbol _getMethod;
        private readonly SourcePropertyAccessorSymbol _setMethod;
        private readonly TypeSymbol _explicitInterfaceType;
        private readonly ImmutableArray<PropertySymbol> _explicitInterfaceImplementations;
        private readonly Flags _propertyFlags;
        private readonly RefKind _refKind;

        private SymbolCompletionState _state;
        private ImmutableArray<ParameterSymbol> _lazyParameters;
        private TypeWithAnnotations.Boxed _lazyType;

        /// <summary>
        /// Set in constructor, might be changed while decoding <see cref="IndexerNameAttribute"/>.
        /// </summary>
        protected readonly string _sourceName;

        private string _lazyDocComment;
        private string _lazyExpandedDocComment;
        private OverriddenOrHiddenMembersResult _lazyOverriddenOrHiddenMembers;
        private SynthesizedSealedPropertyAccessor _lazySynthesizedSealedAccessor;
        private CustomAttributesBag<CSharpAttributeData> _lazyCustomAttributesBag;

        // CONSIDER: if the parameters were computed lazily, ParameterCount could be overridden to fall back on the syntax (as in SourceMemberMethodSymbol).

        public Location Location { get; }

#nullable enable
        /// <remarks>
        /// The <paramref name="binder" /> is passed for explicit interface implementations or
        /// overrides to <see cref="ComputeType" />. If a binder is not required in this situation
        /// the parameter can be null.
        /// </remarks>
        protected SourcePropertySymbolBase(
            SourceMemberContainerTypeSymbol containingType,
            Binder? binder,
            CSharpSyntaxNode syntax,
            CSharpSyntaxNode? getSyntax,
            CSharpSyntaxNode? setSyntax,
            ArrowExpressionClauseSyntax? arrowExpression,
            ExplicitInterfaceSpecifierSyntax? interfaceSpecifier,
            DeclarationModifiers modifiers,
            bool isIndexer,
            bool hasInitializer,
            bool isAutoProperty,
            bool hasAccessorList,
            bool isInitOnly,
            RefKind refKind,
            string name,
            Location location,
            TypeWithAnnotations typeOpt,
            bool hasParameters,
            DiagnosticBag diagnostics)
        {
            _syntaxRef = syntax.GetReference();
            Location = location;

            bool isExplicitInterfaceImplementation = interfaceSpecifier != null;
            if (isExplicitInterfaceImplementation)
            {
                _propertyFlags |= Flags.IsExplicitInterfaceImplementation;
            }

            _containingType = containingType;
            _refKind = refKind;

            if (binder is object)
            {
                binder = binder.WithUnsafeRegionIfNecessary(GetModifierTokens(syntax));
                binder = binder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks, this);
            }

            _modifiers = modifiers;
            this.CheckAccessibility(location, diagnostics, isExplicitInterfaceImplementation);

            this.CheckModifiers(isExplicitInterfaceImplementation, location, isIndexer, diagnostics);

            isAutoProperty = isAutoProperty && !(containingType.IsInterface && !IsStatic) && !IsAbstract && !IsExtern && !isIndexer;

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
                LoadAndValidateAttributes(OneOrMany.Create(AttributeDeclarationSyntaxList), ref temp, earlyDecodingOnly: true);
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
            string memberName = ExplicitInterfaceHelpers.GetMemberNameAndInterfaceSymbol(binder, interfaceSpecifier, name, diagnostics, out _explicitInterfaceType, out aliasQualifierOpt);
            _sourceName = _sourceName ?? memberName; //sourceName may have been set while loading attributes
            _name = isIndexer ? ExplicitInterfaceHelpers.GetMemberName(WellKnownMemberNames.Indexer, _explicitInterfaceType, aliasQualifierOpt) : _sourceName;

            if (hasInitializer)
            {
                CheckInitializer(isAutoProperty, containingType.IsInterface, IsStatic, location, diagnostics);
            }

            var hasGetAccessor = getSyntax is object;
            var hasSetAccessor = setSyntax is object;

            if (isAutoProperty || hasInitializer)
            {
                var isAutoPropertyWithGetSyntax = isAutoProperty && hasGetAccessor;
                if (isAutoPropertyWithGetSyntax)
                {
                    _propertyFlags |= Flags.IsAutoProperty;
                }

                bool isGetterOnly = hasGetAccessor && !hasSetAccessor;

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
                        Binder.ReportUseSiteDiagnosticForSynthesizedAttribute(DeclaringCompilation,
                        WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor, diagnostics, syntax: syntax);

                        if (this._refKind != RefKind.None && !_containingType.IsInterface)
                        {
                            diagnostics.Add(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, location, this);
                        }
                    }

                    string fieldName = GeneratedNames.MakeBackingFieldName(_sourceName);
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
                var type = typeOpt.HasType ? typeOpt : this.ComputeType(binder, syntax, diagnostics);
                _lazyType = new TypeWithAnnotations.Boxed(type);
                _lazyParameters = !hasParameters ? ImmutableArray<ParameterSymbol>.Empty : this.ComputeParameters(binder, syntax, diagnostics);

                bool isOverride = false;
                PropertySymbol? overriddenOrImplementedProperty;

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
                var modifierType = Binder.GetWellKnownType(DeclaringCompilation, WellKnownType.System_Runtime_InteropServices_InAttribute, diagnostics, TypeLocation);

                _refCustomModifiers = ImmutableArray.Create(CSharpCustomModifier.CreateRequired(modifierType));
            }

            if (!hasAccessorList && arrowExpression != null)
            {
                Debug.Assert(arrowExpression is object);
                _propertyFlags |= Flags.IsExpressionBodied;
                _getMethod = CreateExpressionBodiedAccessor(
                    arrowExpression,
                    explicitlyImplementedProperty,
                    aliasQualifierOpt,
                    isExplicitInterfaceImplementation,
                    diagnostics);
                _setMethod = null;
            }
            else
            {
                _getMethod = CreateAccessorSymbol(isGet: true, getSyntax, explicitlyImplementedProperty, aliasQualifierOpt, isAutoProperty, isExplicitInterfaceImplementation, diagnostics);
                _setMethod = CreateAccessorSymbol(isGet: false, setSyntax, explicitlyImplementedProperty, aliasQualifierOpt, isAutoProperty, isExplicitInterfaceImplementation, diagnostics);

                if (!hasGetAccessor || !hasSetAccessor)
                {
                    if (!hasGetAccessor && !hasSetAccessor)
                    {
                        diagnostics.Add(ErrorCode.ERR_PropertyWithNoAccessors, location, this);
                    }
                    else if (_refKind != RefKind.None)
                    {
                        if (!hasGetAccessor)
                        {
                            diagnostics.Add(ErrorCode.ERR_RefPropertyMustHaveGetAccessor, location, this);
                        }
                    }
                    else if (isAutoProperty)
                    {
                        var accessor = _getMethod ?? _setMethod;
                        if (!hasGetAccessor)
                        {
                            diagnostics.Add(ErrorCode.ERR_AutoPropertyMustHaveGetAccessor, accessor!.Locations[0], accessor);
                        }
                    }
                }

                // Check accessor accessibility is more restrictive than property accessibility.
                CheckAccessibilityMoreRestrictive(_getMethod, diagnostics);
                CheckAccessibilityMoreRestrictive(_setMethod, diagnostics);

                if ((_getMethod is object) && (_setMethod is object))
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
                        if (accessor is object)
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

            if (explicitlyImplementedProperty is object)
            {
                CheckExplicitImplementationAccessor(_getMethod, explicitlyImplementedProperty.GetMethod, explicitlyImplementedProperty, diagnostics);
                CheckExplicitImplementationAccessor(_setMethod, explicitlyImplementedProperty.SetMethod, explicitlyImplementedProperty, diagnostics);
            }

            _explicitInterfaceImplementations =
                explicitlyImplementedProperty is null ?
                    ImmutableArray<PropertySymbol>.Empty :
                    ImmutableArray.Create(explicitlyImplementedProperty);

            // get-only auto property should not override settable properties
            if ((_propertyFlags & Flags.IsAutoProperty) != 0)
            {
                if (_setMethod is null && !this.IsReadOnly)
                {
                    diagnostics.Add(ErrorCode.ERR_AutoPropertyMustOverrideSet, location, this);
                }

                CheckForFieldTargetedAttribute(diagnostics);
            }

            CheckForBlockAndExpressionBody(syntax, diagnostics);
        }

        protected abstract Location TypeLocation { get; }

        protected abstract SyntaxTokenList GetModifierTokens(SyntaxNode syntax);

        private void CheckForFieldTargetedAttribute(DiagnosticBag diagnostics)
        {
            var languageVersion = this.DeclaringCompilation.LanguageVersion;
            if (languageVersion.AllowAttributesOnBackingFields())
            {
                return;
            }

            foreach (var attribute in AttributeDeclarationSyntaxList)
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

        protected abstract void CheckForBlockAndExpressionBody(CSharpSyntaxNode syntax, DiagnosticBag diagnostics);

#nullable restore

        internal sealed override ImmutableArray<string> NotNullMembers =>
            GetDecodedWellKnownAttributeData()?.NotNullMembers ?? ImmutableArray<string>.Empty;

        internal sealed override ImmutableArray<string> NotNullWhenTrueMembers =>
            GetDecodedWellKnownAttributeData()?.NotNullWhenTrueMembers ?? ImmutableArray<string>.Empty;

        internal sealed override ImmutableArray<string> NotNullWhenFalseMembers =>
            GetDecodedWellKnownAttributeData()?.NotNullWhenFalseMembers ?? ImmutableArray<string>.Empty;

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
                    var result = this.ComputeType(binder: null, CSharpSyntaxNode, diagnostics);
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

                    var hasPointerType = _lazyType.Value.DefaultType.IsPointerOrFunctionPointer();
                    Debug.Assert(hasPointerType == HasPointerTypeSyntactically);
                    return hasPointerType;
                }

                return HasPointerTypeSyntactically;
            }
        }

        protected abstract bool HasPointerTypeSyntactically { get; }

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
            return new LexicalSortKey(Location, this.DeclaringCompilation);
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray.Create(Location);
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

        public override MethodSymbol GetMethod
        {
            get { return _getMethod; }
        }

        public override MethodSymbol SetMethod
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
                    var result = this.ComputeParameters(binder: null, CSharpSyntaxNode, diagnostics);
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

        public bool HasSkipLocalsInitAttribute
        {
            get
            {
                var data = this.GetDecodedWellKnownAttributeData();
                return data?.HasSkipLocalsInitAttribute == true;
            }
        }

        internal bool IsAutoProperty
            => (_propertyFlags & Flags.IsAutoProperty) != 0;

        /// <summary>
        /// Backing field for automatically implemented property, or
        /// for a property with an initializer.
        /// </summary>
        internal SynthesizedBackingFieldSymbol BackingField { get; }

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

        internal CSharpSyntaxNode CSharpSyntaxNode
        {
            get
            {
                return (CSharpSyntaxNode)_syntaxRef.GetSyntax();
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
            Location location = TypeLocation;
            var compilation = DeclaringCompilation;

            Debug.Assert(location != null);

            // Check constraints on return type and parameters. Note: Dev10 uses the
            // property name location for any such errors. We'll do the same for return
            // type errors but for parameter errors, we'll use the parameter location.

            if ((object)_explicitInterfaceType != null)
            {
                var explicitInterfaceSpecifier = GetExplicitInterfaceSpecifier(this.CSharpSyntaxNode);
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

#nullable enable
        protected abstract SourcePropertyAccessorSymbol? CreateAccessorSymbol(
            bool isGet,
            CSharpSyntaxNode? syntaxOpt,
            PropertySymbol? explicitlyImplementedPropertyOpt,
            string? aliasQualifierOpt,
            bool isAutoPropertyAccessor,
            bool isExplicitInterfaceImplementation,
            DiagnosticBag diagnostics);

        protected abstract SourcePropertyAccessorSymbol CreateExpressionBodiedAccessor(
            ArrowExpressionClauseSyntax syntax,
            PropertySymbol? explicitlyImplementedPropertyOpt,
            string? aliasQualifierOpt,
            bool isExplicitInterfaceImplementation,
            DiagnosticBag diagnostics);
#nullable restore

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

        #region Attributes

        public abstract SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList { get; }

        public abstract IAttributeTargetSymbol AttributesOwner { get; }

        IAttributeTargetSymbol IAttributeTargetSymbol.AttributesOwner => AttributesOwner;

        AttributeLocation IAttributeTargetSymbol.DefaultAttributeLocation => AttributeLocation.Property;

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
            _ = BackingField?.GetAttributes();

            if (LoadAndValidateAttributes(OneOrMany.Create(AttributeDeclarationSyntaxList), ref _lazyCustomAttributesBag))
            {
                var completed = _state.NotePartComplete(CompletionPart.Attributes);
                Debug.Assert(completed);
            }

            Debug.Assert(_lazyCustomAttributesBag.IsSealed);
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

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = this.DeclaringCompilation;
            var type = this.TypeWithAnnotations;

            if (type.Type.ContainsDynamic())
            {
                AddSynthesizedAttribute(ref attributes,
                    compilation.SynthesizeDynamicAttribute(type.Type, type.CustomModifiers.Length + RefCustomModifiers.Length, _refKind));
            }

            if (type.Type.ContainsNativeInteger())
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeNativeIntegerAttribute(this, type.Type));
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

        internal override CSharpAttributeData EarlyDecodeWellKnownAttribute(ref EarlyDecodeWellKnownAttributeArguments<EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation> arguments)
        {
            CSharpAttributeData boundAttribute;
            ObsoleteAttributeData obsoleteData;

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
                    string indexerName = boundAttribute.CommonConstructorArguments[0].DecodeValue<string>(SpecialType.System_String);
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
        internal override ObsoleteAttributeData ObsoleteAttributeData
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
            Debug.Assert(arguments.AttributeSyntaxOpt != null);

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
            else if (attribute.IsTargetAttribute(this, AttributeDescription.SkipLocalsInitAttribute))
            {
                CSharpAttributeData.DecodeSkipLocalsInitAttribute<PropertyWellKnownAttributeData>(DeclaringCompilation, ref arguments);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.DynamicAttribute))
            {
                // DynamicAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitDynamicAttr, arguments.AttributeSyntaxOpt.Location);
            }
            else if (ReportExplicitUseOfReservedAttributes(in arguments,
                ReservedAttributes.DynamicAttribute | ReservedAttributes.IsReadOnlyAttribute | ReservedAttributes.IsUnmanagedAttribute | ReservedAttributes.IsByRefLikeAttribute | ReservedAttributes.TupleElementNamesAttribute | ReservedAttributes.NullableAttribute | ReservedAttributes.NativeIntegerAttribute))
            {
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
            else if (attribute.IsTargetAttribute(this, AttributeDescription.MemberNotNullAttribute))
            {
                MessageID.IDS_FeatureMemberNotNull.CheckFeatureAvailability(arguments.Diagnostics, arguments.AttributeSyntaxOpt);
                CSharpAttributeData.DecodeMemberNotNullAttribute<PropertyWellKnownAttributeData>(ContainingType, ref arguments);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.MemberNotNullWhenAttribute))
            {
                MessageID.IDS_FeatureMemberNotNull.CheckFeatureAvailability(arguments.Diagnostics, arguments.AttributeSyntaxOpt);
                CSharpAttributeData.DecodeMemberNotNullWhenAttribute<PropertyWellKnownAttributeData>(ContainingType, ref arguments);
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

        internal ImmutableArray<SourceAttributeData> MemberNotNullAttributeIfExists
            => FindAttributes(AttributeDescription.MemberNotNullAttribute);

        internal ImmutableArray<SourceAttributeData> MemberNotNullWhenAttributeIfExists
            => FindAttributes(AttributeDescription.MemberNotNullWhenAttribute);

        private SourceAttributeData FindAttribute(AttributeDescription attributeDescription)
            => (SourceAttributeData)GetAttributes().First(a => a.IsTargetAttribute(this, attributeDescription));

        private ImmutableArray<SourceAttributeData> FindAttributes(AttributeDescription attributeDescription)
            => GetAttributes().Where(a => a.IsTargetAttribute(this, attributeDescription)).Cast<SourceAttributeData>().ToImmutableArray();

        internal override void PostDecodeWellKnownAttributes(ImmutableArray<CSharpAttributeData> boundAttributes, ImmutableArray<AttributeSyntax> allAttributeSyntaxNodes, DiagnosticBag diagnostics, AttributeLocation symbolPart, WellKnownAttributeData decodedData)
        {
            Debug.Assert(!boundAttributes.IsDefault);
            Debug.Assert(!allAttributeSyntaxNodes.IsDefault);
            Debug.Assert(boundAttributes.Length == allAttributeSyntaxNodes.Length);
            Debug.Assert(_lazyCustomAttributesBag != null);
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
                string indexerName = attribute.CommonConstructorArguments[0].DecodeValue<string>(SpecialType.System_String);
                if (indexerName == null || !SyntaxFacts.IsValidIdentifier(indexerName))
                {
                    diagnostics.Add(ErrorCode.ERR_BadArgumentToAttribute, node.ArgumentList.Arguments[0].Location, node.GetErrorDisplayName());
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

        internal override void ForceComplete(SourceLocation locationOpt, CancellationToken cancellationToken)
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
                                this.Type.CheckAllConstraints(DeclaringCompilation, conversions, Location, diagnostics);

                                ValidatePropertyType(diagnostics);

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

        protected virtual void ValidatePropertyType(DiagnosticBag diagnostics)
        {
            var type = this.Type;
            if (type.IsRestrictedType(ignoreSpanLikeTypes: true))
            {
                diagnostics.Add(ErrorCode.ERR_FieldCantBeRefAny, TypeLocation, type);
            }
            else if (this.IsAutoProperty && type.IsRefLikeType && (this.IsStatic || !this.ContainingType.IsRefLikeType))
            {
                diagnostics.Add(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, TypeLocation, type);
            }
        }

        #endregion

#nullable enable
        protected abstract ImmutableArray<ParameterSymbol> ComputeParameters(Binder? binder, CSharpSyntaxNode syntax, DiagnosticBag diagnostics);

        protected abstract TypeWithAnnotations ComputeType(Binder? binder, SyntaxNode syntax, DiagnosticBag diagnostics);

        protected static ExplicitInterfaceSpecifierSyntax? GetExplicitInterfaceSpecifier(SyntaxNode syntax)
            => (syntax as BasePropertyDeclarationSyntax)?.ExplicitInterfaceSpecifier;
#nullable restore
    }
}
