// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourcePropertySymbol : PropertySymbol, IAttributeTargetSymbol
    {
        private const string DefaultIndexerName = "Item";

        // TODO (tomat): consider splitting into multiple subclasses/rare data.

        private readonly SourceMemberContainerTypeSymbol _containingType;
        private readonly string _name;
        private readonly SyntaxReference _syntaxRef;
        private readonly Location _location;
        private readonly DeclarationModifiers _modifiers;
        private readonly ImmutableArray<CustomModifier> _typeCustomModifiers;
        private readonly SourcePropertyAccessorSymbol _getMethod;
        private readonly SourcePropertyAccessorSymbol _setMethod;
        private readonly SynthesizedBackingFieldSymbol _backingField;
        private readonly TypeSymbol _explicitInterfaceType;
        private readonly ImmutableArray<PropertySymbol> _explicitInterfaceImplementations;
        private readonly bool _isExpressionBodied;
        private readonly bool _isAutoProperty;
        private readonly RefKind refKind;

        private SymbolCompletionState _state;
        private ImmutableArray<ParameterSymbol> _lazyParameters;
        private TypeSymbol _lazyType;

        /// <summary>
        /// Set in constructor, might be changed while decoding <see cref="IndexerNameAttribute"/>.
        /// </summary>
        private readonly string _sourceName;

        private string _lazyDocComment;
        private OverriddenOrHiddenMembersResult _lazyOverriddenOrHiddenMembers;
        private SynthesizedSealedPropertyAccessor _lazySynthesizedSealedAccessor;
        private CustomAttributesBag<CSharpAttributeData> _lazyCustomAttributesBag;

        private SourcePropertySymbol _replacedBy;
        private SourcePropertySymbol _replaced;

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
            bool isExplicitInterfaceImplementation = (interfaceSpecifier != null);

            _location = location;
            _containingType = containingType;
            _syntaxRef = syntax.GetReference();

            SyntaxTokenList modifiers = syntax.Modifiers;
            bodyBinder = bodyBinder.WithUnsafeRegionIfNecessary(modifiers);
            bodyBinder = bodyBinder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks, this);

            bool modifierErrors;
            _modifiers = MakeModifiers(modifiers, isExplicitInterfaceImplementation, isIndexer, location, diagnostics, out modifierErrors);
            this.CheckAccessibility(location, diagnostics);

            this.CheckModifiers(location, isIndexer, diagnostics);

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
            _isExpressionBodied = false;

            var refKeyword = default(SyntaxToken);
            switch (syntax.Kind())
            {
                case SyntaxKind.PropertyDeclaration:
                    refKeyword = ((PropertyDeclarationSyntax)syntax).RefKeyword;
                    break;

                case SyntaxKind.IndexerDeclaration:
                    refKeyword = ((IndexerDeclarationSyntax)syntax).RefKeyword;
                    break;
            }
            this.refKind = refKeyword.Kind().GetRefKind();

            bool hasAccessorList = syntax.AccessorList != null;
            var propertySyntax = syntax as PropertyDeclarationSyntax;
            var arrowExpression = propertySyntax != null
                ? propertySyntax.ExpressionBody
                : ((IndexerDeclarationSyntax)syntax).ExpressionBody;
            bool hasExpressionBody = arrowExpression != null;
            bool hasInitializer = !isIndexer && propertySyntax.Initializer != null;

            bool notRegularProperty = (!IsAbstract && !IsExtern && !isIndexer && hasAccessorList);
            AccessorDeclarationSyntax getSyntax = null;
            AccessorDeclarationSyntax setSyntax = null;
            if (hasAccessorList)
            {
                foreach (var accessor in syntax.AccessorList.Accessors)
                {
                    if (accessor.Kind() == SyntaxKind.GetAccessorDeclaration &&
                        (getSyntax == null || getSyntax.Keyword.Span.IsEmpty))
                    {
                        getSyntax = accessor;
                    }
                    else if (accessor.Kind() == SyntaxKind.SetAccessorDeclaration &&
                        (setSyntax == null || setSyntax.Keyword.Span.IsEmpty))
                    {
                        setSyntax = accessor;
                    }
                    else
                    {
                        continue;
                    }

                    if (accessor.Body != null)
                    {
                        notRegularProperty = false;
                    }
                }
            }
            else
            {
                notRegularProperty = false;
            }

            if (hasInitializer)
            {
                CheckInitializer(hasExpressionBody, notRegularProperty, location, diagnostics);
            }

            if (notRegularProperty || hasInitializer)
            {
                var hasGetSyntax = getSyntax != null;
                _isAutoProperty = notRegularProperty && hasGetSyntax;
                bool isReadOnly = hasGetSyntax && setSyntax == null;

                if (_isAutoProperty || hasInitializer)
                {
                    if (_isAutoProperty)
                    {
                        //issue a diagnostic if the compiler generated attribute ctor is not found.
                        Binder.ReportUseSiteDiagnosticForSynthesizedAttribute(bodyBinder.Compilation,
                        WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor, diagnostics, syntax: syntax);

                        if (this.refKind != RefKind.None && !_containingType.IsInterface)
                        {
                            diagnostics.Add(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, location, this);
                        }
                    }

                    string fieldName = GeneratedNames.MakeBackingFieldName(_sourceName);
                    _backingField = new SynthesizedBackingFieldSymbol(this,
                                                                          fieldName,
                                                                          isReadOnly,
                                                                          this.IsStatic,
                                                                          hasInitializer);
                }

                if (notRegularProperty)
                {
                    Binder.CheckFeatureAvailability(location,
                                                    isReadOnly ? MessageID.IDS_FeatureReadonlyAutoImplementedProperties :
                                                                 MessageID.IDS_FeatureAutoImplementedProperties,
                                                    diagnostics);
                }
            }

            PropertySymbol explicitlyImplementedProperty = null;
            _typeCustomModifiers = ImmutableArray<CustomModifier>.Empty;

            // The runtime will not treat the accessors of this property as overrides or implementations
            // of those of another property unless both the signatures and the custom modifiers match.
            // Hence, in the case of overrides and *explicit* implementations, we need to copy the custom
            // modifiers that are in the signatures of the overridden/implemented property accessors.
            // (From source, we know that there can only be one overridden/implemented property, so there
            // are no conflicts.)  This is unnecessary for implicit implementations because, if the custom
            // modifiers don't match, we'll insert bridge methods for the accessors (explicit implementations 
            // that delegate to the implicit implementations) with the correct custom modifiers
            // (see SourceNamedTypeSymbol.ImplementInterfaceMember).

            // Note: we're checking if the syntax indicates explicit implementation rather,
            // than if explicitInterfaceType is null because we don't want to look for an
            // overridden property if this is supposed to be an explicit implementation.
            if (isExplicitInterfaceImplementation || this.IsOverride)
            {
                // Type and parameters for overrides and explicit implementations cannot be bound
                // lazily since the property name depends on the metadata name of the base property,
                // and the property name is required to add the property to the containing type, and
                // the type and parameters are required to determine the override or implementation.
                _lazyType = this.ComputeType(bodyBinder, syntax, diagnostics);
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
                    overriddenOrImplementedProperty = explicitlyImplementedProperty;
                }

                if ((object)overriddenOrImplementedProperty != null)
                {
                    _typeCustomModifiers = overriddenOrImplementedProperty.TypeCustomModifiers;

                    TypeSymbol overriddenPropertyType = overriddenOrImplementedProperty.Type;

                    // We do an extra check before copying the type to handle the case where the overriding
                    // property (incorrectly) has a different type than the overridden property.  In such cases,
                    // we want to retain the original (incorrect) type to avoid hiding the type given in source.
                    if (_lazyType.Equals(overriddenPropertyType, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true))
                    {
                        _lazyType = CustomModifierUtils.CopyTypeCustomModifiers(overriddenPropertyType, _lazyType, RefKind.None, this.ContainingAssembly);
                    }

                    _lazyParameters = CustomModifierUtils.CopyParameterCustomModifiers(overriddenOrImplementedProperty.Parameters, _lazyParameters, alsoCopyParamsModifier: isOverride);
                }
            }

            if (!hasAccessorList)
            {
                if (hasExpressionBody)
                {
                    _isExpressionBodied = true;
                    _getMethod = SourcePropertyAccessorSymbol.CreateAccessorSymbol(
                        containingType,
                        this,
                        _modifiers,
                        _sourceName,
                        arrowExpression,
                        explicitlyImplementedProperty,
                        aliasQualifierOpt,
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
                _getMethod = CreateAccessorSymbol(getSyntax, explicitlyImplementedProperty, aliasQualifierOpt, notRegularProperty, diagnostics);
                _setMethod = CreateAccessorSymbol(setSyntax, explicitlyImplementedProperty, aliasQualifierOpt, notRegularProperty, diagnostics);

                if ((getSyntax == null) || (setSyntax == null))
                {
                    if ((getSyntax == null) && (setSyntax == null))
                    {
                        diagnostics.Add(ErrorCode.ERR_PropertyWithNoAccessors, location, this);
                    }
                    else if (refKind != RefKind.None)
                    {
                        if (getSyntax == null)
                        {
                            diagnostics.Add(ErrorCode.ERR_RefPropertyMustHaveGetAccessor, location, this);
                        }
                    }
                    else if (notRegularProperty)
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
                    if (refKind != RefKind.None)
                    {
                        diagnostics.Add(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, _setMethod.Locations[0], _setMethod);
                    }
                    else if ((_getMethod.LocalAccessibility != Accessibility.NotApplicable) &&
                             (_setMethod.LocalAccessibility != Accessibility.NotApplicable))
                    {
                        // Check accessibility is set on at most one accessor.
                        diagnostics.Add(ErrorCode.ERR_DuplicatePropertyAccessMods, location, this);
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
            if (_isAutoProperty && (object)_setMethod == null && !this.IsReadOnly)
            {
                diagnostics.Add(ErrorCode.ERR_AutoPropertyMustOverrideSet, location, this);
            }
        }

        internal bool IsExpressionBodied
        {
            get
            {
                return _isExpressionBodied;
            }
        }

        private void CheckInitializer(
            bool hasExpressionBody,
            bool isAutoProperty,
            Location location,
            DiagnosticBag diagnostics)
        {
            if (_containingType.IsInterface)
            {
                diagnostics.Add(ErrorCode.ERR_AutoPropertyInitializerInInterface, location, this);
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

        internal override RefKind RefKind
        {
            get
            {
                return this.refKind;
            }
        }

        public override TypeSymbol Type
        {
            get
            {
                if ((object)_lazyType == null)
                {
                    var diagnostics = DiagnosticBag.GetInstance();
                    var binder = this.CreateBinderForTypeAndParameters();
                    var syntax = (BasePropertyDeclarationSyntax)_syntaxRef.GetSyntax();
                    var result = this.ComputeType(binder, syntax, diagnostics);
                    if ((object)Interlocked.CompareExchange(ref _lazyType, result, null) == null)
                    {
                        this.AddDeclarationDiagnostics(diagnostics);
                    }
                    diagnostics.Free();
                }

                return _lazyType;
            }
        }

        internal bool HasPointerType
        {
            get
            {
                if ((object)_lazyType != null)
                {
                    Debug.Assert(_lazyType.IsPointerType() ==
                         (((BasePropertyDeclarationSyntax)_syntaxRef.GetSyntax()).Type.Kind() == SyntaxKind.PointerType));

                    return _lazyType.IsPointerType();
                }

                var syntax = (BasePropertyDeclarationSyntax)_syntaxRef.GetSyntax();
                return syntax.Type.Kind() == SyntaxKind.PointerType;
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

        internal sealed override bool IsReplace
        {
            get { return (_modifiers & DeclarationModifiers.Replace) != 0; }
        }

        internal sealed override Symbol Replaced
        {
            get { return _replaced; }
        }

        internal sealed override Symbol ReplacedBy
        {
            get { return _replacedBy; }
        }

        internal sealed override void SetReplaced(Symbol replaced)
        {
            this._replaced = (SourcePropertySymbol)replaced;
        }

        internal sealed override void SetReplacedBy(Symbol replacedBy)
        {
            this._replacedBy = (SourcePropertySymbol)replacedBy;
        }

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
        {
            get { return this.CSharpSyntaxNode.ExplicitInterfaceSpecifier != null; }
        }

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations
        {
            get { return _explicitInterfaceImplementations; }
        }

        public override ImmutableArray<CustomModifier> TypeCustomModifiers
        {
            get { return _typeCustomModifiers; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return ModifierUtils.EffectiveAccessibility(_modifiers);
            }
        }

        internal bool IsAutoProperty
        {
            get { return _isAutoProperty; }
        }

        /// <summary>
        /// Backing field for automatically implemented property, or
        /// for a property with an initializer.
        /// </summary>
        internal SynthesizedBackingFieldSymbol BackingField
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
            // Check constraints on return type and parameters. Note: Dev10 uses the
            // property name location for any such errors. We'll do the same for return
            // type errors but for parameter errors, we'll use the parameter location.

            if ((object)_explicitInterfaceType != null)
            {
                var explicitInterfaceSpecifier = GetExplicitInterfaceSpecifier(this.CSharpSyntaxNode);
                Debug.Assert(explicitInterfaceSpecifier != null);
                _explicitInterfaceType.CheckAllConstraints(conversions, new SourceLocation(explicitInterfaceSpecifier.Name), diagnostics);
            }
        }

        private void CheckAccessibility(Location location, DiagnosticBag diagnostics)
        {
            var info = ModifierUtils.CheckAccessibility(_modifiers);
            if (info != null)
            {
                diagnostics.Add(new CSDiagnostic(info, location));
            }
        }

        private DeclarationModifiers MakeModifiers(SyntaxTokenList modifiers, bool isExplicitInterfaceImplementation, bool isIndexer, Location location, DiagnosticBag diagnostics, out bool modifierErrors)
        {
            bool isInterface = this.ContainingType.IsInterface;
            var defaultAccess = isInterface ? DeclarationModifiers.Public : DeclarationModifiers.Private;

            // Check that the set of modifiers is allowed
            var allowedModifiers = DeclarationModifiers.Unsafe;
            if (!isExplicitInterfaceImplementation)
            {
                allowedModifiers |= DeclarationModifiers.New;

                if (!isInterface)
                {
                    allowedModifiers |=
                        DeclarationModifiers.AccessibilityMask |
                        DeclarationModifiers.Sealed |
                        DeclarationModifiers.Abstract |
                        DeclarationModifiers.Virtual |
                        DeclarationModifiers.Override;

                    if (!isIndexer)
                    {
                        allowedModifiers |= DeclarationModifiers.Static;
                    }
                }
            }

            if (!isInterface)
            {
                allowedModifiers |=
                    DeclarationModifiers.Extern |
                    DeclarationModifiers.Replace;
            }

            var mods = ModifierUtils.MakeAndCheckNontypeMemberModifiers(modifiers, defaultAccess, allowedModifiers, location, diagnostics, out modifierErrors);

            this.CheckUnsafeModifier(mods, diagnostics);

            // Let's overwrite modifiers for interface methods with what they are supposed to be. 
            // Proper errors must have been reported by now.
            if (isInterface)
            {
                mods = (mods & ~DeclarationModifiers.AccessibilityMask) | DeclarationModifiers.Abstract | DeclarationModifiers.Public;
            }

            if (isIndexer)
            {
                mods |= DeclarationModifiers.Indexer;
            }

            return mods;
        }

        private static ImmutableArray<ParameterSymbol> MakeParameters(Binder binder, SourcePropertySymbol owner, BaseParameterListSyntax parameterSyntaxOpt, DiagnosticBag diagnostics)
        {
            if (parameterSyntaxOpt == null)
            {
                return ImmutableArray<ParameterSymbol>.Empty;
            }

            SyntaxToken arglistToken;
            var parameters = ParameterHelpers.MakeParameters(binder, owner, parameterSyntaxOpt, false, out arglistToken, diagnostics, false);

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

        private void CheckModifiers(Location location, bool isIndexer, DiagnosticBag diagnostics)
        {
            if (this.DeclaredAccessibility == Accessibility.Private && (IsVirtual || IsAbstract || IsOverride))
            {
                diagnostics.Add(ErrorCode.ERR_VirtualPrivate, location, this);
            }
            else if (IsStatic && (IsOverride || IsVirtual || IsAbstract))
            {
                // A static member '{0}' cannot be marked as override, virtual, or abstract
                diagnostics.Add(ErrorCode.ERR_StaticNotVirtual, location, this);
            }
            else if (IsOverride && (IsNew || IsVirtual))
            {
                // A member '{0}' marked as override cannot be marked as new or virtual
                diagnostics.Add(ErrorCode.ERR_OverrideNotNew, location, this);
            }
            else if (IsSealed && !IsOverride)
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
            else if (IsAbstract && IsSealed)
            {
                diagnostics.Add(ErrorCode.ERR_AbstractAndSealed, location, this);
            }
            else if (IsAbstract && IsVirtual)
            {
                diagnostics.Add(ErrorCode.ERR_AbstractNotVirtual, location, this);
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
            PropertySymbol explicitlyImplementedPropertyOpt, string aliasQualifierOpt, bool isAutoPropertyAccessor, DiagnosticBag diagnostics)
        {
            if (syntaxOpt == null)
            {
                return null;
            }
            return SourcePropertyAccessorSymbol.CreateAccessorSymbol(_containingType, this, _modifiers, _sourceName, syntaxOpt,
                explicitlyImplementedPropertyOpt, aliasQualifierOpt, isAutoPropertyAccessor, diagnostics);
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
            return SourceDocumentationCommentUtils.GetAndCacheDocumentationComment(this, expandIncludes, ref _lazyDocComment);
        }

        // Separate these checks out of FindExplicitlyImplementedProperty because they depend on the accessor symbols,
        // which depend on the explicitly implemented property
        private void CheckExplicitImplementationAccessor(MethodSymbol thisAccessor, MethodSymbol otherAccessor, PropertySymbol explicitlyImplementedProperty, DiagnosticBag diagnostics)
        {
            var thisHasAccessor = (object)thisAccessor != null;
            var otherHasAccessor = (object)otherAccessor != null;

            if (otherHasAccessor && !thisHasAccessor)
            {
                diagnostics.Add(ErrorCode.ERR_ExplicitPropertyMissingAccessor, this.Location, this, otherAccessor);
            }
            else if (!otherHasAccessor && thisHasAccessor)
            {
                diagnostics.Add(ErrorCode.ERR_ExplicitPropertyAddingAccessor, thisAccessor.Locations[0], thisAccessor, explicitlyImplementedProperty);
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

        IAttributeTargetSymbol IAttributeTargetSymbol.AttributesOwner
        {
            get { return this; }
        }

        AttributeLocation IAttributeTargetSymbol.DefaultAttributeLocation
        {
            get { return AttributeLocation.Property; }
        }

        AttributeLocation IAttributeTargetSymbol.AllowedAttributeLocations
        {
            get { return AttributeLocation.Property; }
        }

        /// <summary>
        /// Returns a bag of applied custom attributes and data decoded from well-known attributes. Returns null if there are no attributes applied on the symbol.
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

            if (LoadAndValidateAttributes(OneOrMany.Create(this.CSharpSyntaxNode.AttributeLists), ref _lazyCustomAttributesBag))
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
        private CommonPropertyWellKnownAttributeData GetDecodedWellKnownAttributeData()
        {
            var attributesBag = _lazyCustomAttributesBag;
            if (attributesBag == null || !attributesBag.IsDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetAttributesBag();
            }

            return (CommonPropertyWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData;
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

        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(compilationState, ref attributes);

            if (this.Type.ContainsDynamic())
            {
                var compilation = this.DeclaringCompilation;
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDynamicAttribute(this.Type, this.TypeCustomModifiers.Length));
            }
        }

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

            if (EarlyDecodeDeprecatedOrObsoleteAttribute(ref arguments, out boundAttribute, out obsoleteData))
            {
                if (obsoleteData != null)
                {
                    arguments.GetOrCreateData<PropertyEarlyWellKnownAttributeData>().ObsoleteAttributeData = obsoleteData;
                }

                return boundAttribute;
            }

            bool hasAnyDiagnostics;

            if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.IndexerNameAttribute))
            {
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
                arguments.GetOrCreateData<CommonPropertyWellKnownAttributeData>().HasSpecialNameAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.DynamicAttribute))
            {
                // DynamicAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitDynamicAttr, arguments.AttributeSyntaxOpt.Location);
            }
        }

        internal override void PostDecodeWellKnownAttributes(ImmutableArray<CSharpAttributeData> boundAttributes, ImmutableArray<AttributeSyntax> allAttributeSyntaxNodes, DiagnosticBag diagnostics, AttributeLocation symbolPart, WellKnownAttributeData decodedData)
        {
            Debug.Assert(!boundAttributes.IsDefault);
            Debug.Assert(!allAttributeSyntaxNodes.IsDefault);
            Debug.Assert(boundAttributes.Length == allAttributeSyntaxNodes.Length);
            Debug.Assert(_lazyCustomAttributesBag != null);
            Debug.Assert(_lazyCustomAttributesBag.IsDecodedWellKnownAttributeDataComputed);
            Debug.Assert(symbolPart == AttributeLocation.None);

            if (this.IsAutoProperty && !this.IsStatic && this.ContainingType.Layout.Kind == LayoutKind.Explicit)
            {
                // error CS0842: '<property>': Automatically implemented properties cannot be used inside a type marked with StructLayout(LayoutKind.Explicit)
                diagnostics.Add(ErrorCode.ERR_ExplicitLayoutAndAutoImplementedProperty, this.Location, this);
            }

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

                    case CompletionPart.Type:
                        {
                            var diagnostics = DiagnosticBag.GetInstance();
                            var conversions = new TypeConversions(this.ContainingAssembly.CorLibrary);
                            this.Type.CheckAllConstraints(conversions, _location, diagnostics);

                            if (this.Type.IsRestrictedType())
                            {
                                diagnostics.Add(ErrorCode.ERR_FieldCantBeRefAny, this.CSharpSyntaxNode.Type.Location, this.Type);
                            }

                            if (_state.NotePartComplete(CompletionPart.Type))
                            {
                                this.AddDeclarationDiagnostics(diagnostics);
                            }

                            diagnostics.Free();
                        }
                        break;

                    case CompletionPart.Parameters:
                        {
                            var parameters = this.Parameters;
                            if (parameters.Length > 0)
                            {
                                var diagnostics = DiagnosticBag.GetInstance();
                                var conversions = new TypeConversions(this.ContainingAssembly.CorLibrary);
                                foreach (var parameter in this.Parameters)
                                {
                                    parameter.ForceComplete(locationOpt, cancellationToken);
                                    parameter.Type.CheckAllConstraints(conversions, parameter.Locations[0], diagnostics);
                                }

                                if (_state.NotePartComplete(CompletionPart.Parameters))
                                {
                                    this.AddDeclarationDiagnostics(diagnostics);
                                    DeclaringCompilation.SymbolDeclaredEvent(this);
                                }

                                diagnostics.Free();
                            }
                            else
                            {
                                if (_state.NotePartComplete(CompletionPart.Parameters))
                                {
                                    DeclaringCompilation.SymbolDeclaredEvent(this);
                                }
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

        private TypeSymbol ComputeType(Binder binder, BasePropertyDeclarationSyntax syntax, DiagnosticBag diagnostics)
        {
            var type = binder.BindType(syntax.Type, diagnostics);
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            if (!this.IsNoMoreVisibleThan(type, ref useSiteDiagnostics))
            {
                // "Inconsistent accessibility: indexer return type '{1}' is less accessible than indexer '{0}'"
                // "Inconsistent accessibility: property type '{1}' is less accessible than property '{0}'"
                diagnostics.Add((this.IsIndexer ? ErrorCode.ERR_BadVisIndexerReturn : ErrorCode.ERR_BadVisPropertyType), _location, this, type);
            }

            diagnostics.Add(_location, useSiteDiagnostics);

            if (type.SpecialType == SpecialType.System_Void)
            {
                ErrorCode errorCode = this.IsIndexer ? ErrorCode.ERR_IndexerCantHaveVoidType : ErrorCode.ERR_PropertyCantHaveVoidType;
                diagnostics.Add(errorCode, _location, this);
            }

            return type;
        }

        private ImmutableArray<ParameterSymbol> ComputeParameters(Binder binder, BasePropertyDeclarationSyntax syntax, DiagnosticBag diagnostics)
        {
            var parameterSyntaxOpt = GetParameterListSyntax(syntax);
            var parameters = MakeParameters(binder, this, parameterSyntaxOpt, diagnostics);
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            foreach (ParameterSymbol param in parameters)
            {
                if (!this.IsNoMoreVisibleThan(param.Type, ref useSiteDiagnostics))
                {
                    diagnostics.Add(ErrorCode.ERR_BadVisIndexerParam, _location, this, param.Type);
                }
                else if ((object)_setMethod != null && param.Name == ParameterSymbol.ValueParameterName)
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

        private static ExplicitInterfaceSpecifierSyntax GetExplicitInterfaceSpecifier(BasePropertyDeclarationSyntax syntax)
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

        private static BaseParameterListSyntax GetParameterListSyntax(BasePropertyDeclarationSyntax syntax)
        {
            return (syntax.Kind() == SyntaxKind.IndexerDeclaration) ? ((IndexerDeclarationSyntax)syntax).ParameterList : null;
        }
    }
}
