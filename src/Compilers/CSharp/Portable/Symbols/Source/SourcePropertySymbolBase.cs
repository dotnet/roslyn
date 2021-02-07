// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            HasGetAccessor = 1 << 3,
            HasSetAccessor = 1 << 4,
            HasInitializer = 1 << 5,
            IsAutoPropertyWithGetAccessor = IsAutoProperty | HasGetAccessor,
        }

        // TODO (tomat): consider splitting into multiple subclasses/rare data.

        private readonly SourceMemberContainerTypeSymbol _containingType;
        private readonly string _name;
        private readonly SyntaxReference _syntaxRef;
        protected readonly DeclarationModifiers _modifiers;
        private ImmutableArray<CustomModifier> _lazyRefCustomModifiers;
#nullable enable
        private SourcePropertyAccessorSymbol? _lazyGetMethod;
        private SourcePropertyAccessorSymbol? _lazySetMethod;
        private DiagnosticBag? _lazyDiagnosticBag;
#nullable disable
        private readonly TypeSymbol _explicitInterfaceType;
        private ImmutableArray<PropertySymbol> _lazyExplicitInterfaceImplementations;
        private readonly Flags _propertyFlags;
        private readonly RefKind _refKind;

        private SymbolCompletionState _state;
        private ImmutableArray<ParameterSymbol> _lazyParameters;
        private TypeWithAnnotations.Boxed _lazyType;

        /// <summary>
        /// Set in constructor, might be changed while decoding <see cref="IndexerNameAttribute"/>.
        /// </summary>
        private readonly string _sourceName;

        private string _lazyDocComment;
        private string _lazyExpandedDocComment;
        private OverriddenOrHiddenMembersResult _lazyOverriddenOrHiddenMembers;
        private SynthesizedSealedPropertyAccessor _lazySynthesizedSealedAccessor;
        private CustomAttributesBag<CSharpAttributeData> _lazyCustomAttributesBag;

        // CONSIDER: if the parameters were computed lazily, ParameterCount could be overridden to fall back on the syntax (as in SourceMemberMethodSymbol).

        public Location Location { get; }

#nullable enable
        protected SourcePropertySymbolBase(
            SourceMemberContainerTypeSymbol containingType,
            CSharpSyntaxNode syntax,
            bool hasGetAccessor,
            bool hasSetAccessor,
            bool isExplicitInterfaceImplementation,
            TypeSymbol? explicitInterfaceType,
            string? aliasQualifierOpt,
            DeclarationModifiers modifiers,
            bool hasInitializer,
            bool isAutoProperty,
            bool isExpressionBodied,
            bool isInitOnly,
            RefKind refKind,
            string memberName,
            SyntaxList<AttributeListSyntax> indexerNameAttributeLists,
            Location location)
        {
            Debug.Assert(!isExpressionBodied || !isAutoProperty);
            Debug.Assert(!isExpressionBodied || !hasInitializer);

            _syntaxRef = syntax.GetReference();
            Location = location;
            _containingType = containingType;
            _refKind = refKind;
            _modifiers = modifiers;
            _explicitInterfaceType = explicitInterfaceType;

            if (isExplicitInterfaceImplementation)
            {
                _propertyFlags |= Flags.IsExplicitInterfaceImplementation;
            }
            else
            {
                _lazyExplicitInterfaceImplementations = ImmutableArray<PropertySymbol>.Empty;
            }

            bool isIndexer = IsIndexer;
            isAutoProperty = isAutoProperty && !(containingType.IsInterface && !IsStatic) && !IsAbstract && !IsExtern && !isIndexer;

            if (isAutoProperty)
            {
                _propertyFlags |= Flags.IsAutoProperty;
            }

            if (hasGetAccessor)
            {
                _propertyFlags |= Flags.HasGetAccessor;
            }

            if (hasSetAccessor)
            {
                _propertyFlags |= Flags.HasSetAccessor;
            }

            if (hasInitializer)
            {
                _propertyFlags |= Flags.HasInitializer;
            }

            if (isExpressionBodied)
            {
                _propertyFlags |= Flags.IsExpressionBodied;
            }

            if (isIndexer && indexerNameAttributeLists.Count != 0 && !isExplicitInterfaceImplementation)
            {
                // Evaluate the attributes immediately in case the IndexerNameAttribute has been applied.
                // NOTE: we want IsExplicitInterfaceImplementation, IsOverride, Locations, and the syntax reference
                // to be initialized before we pass this symbol to LoadCustomAttributes.

                // CONSIDER: none of the information from this early binding pass is cached.  Everything will
                // be re-bound when someone calls GetAttributes.  If this gets to be a problem, we could
                // always use the real attribute bag of this symbol and modify LoadAndValidateAttributes to
                // handle partially filled bags.
                CustomAttributesBag<CSharpAttributeData>? temp = null;
                LoadAndValidateAttributes(OneOrMany.Create(indexerNameAttributeLists), ref temp, earlyDecodingOnly: true);
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

            _sourceName = _sourceName ?? memberName; // _sourceName may have been set while loading attributes
            _name = isIndexer ? ExplicitInterfaceHelpers.GetMemberName(WellKnownMemberNames.Indexer, _explicitInterfaceType, aliasQualifierOpt) : _sourceName;

            if ((isAutoProperty && hasGetAccessor) || hasInitializer)
            {
                string fieldName = GeneratedNames.MakeBackingFieldName(_sourceName);
                BackingField = new SynthesizedBackingFieldSymbol(this,
                                                                      fieldName,
                                                                      isReadOnly: (hasGetAccessor && !hasSetAccessor) || isInitOnly,
                                                                      this.IsStatic,
                                                                      hasInitializer);
            }
        }

        private void EnsureSignatureGuarded(DiagnosticBag diagnostics)
        {
            PropertySymbol? explicitlyImplementedProperty = null;
            _lazyRefCustomModifiers = ImmutableArray<CustomModifier>.Empty;

            TypeWithAnnotations type;
            (type, _lazyParameters) = MakeParametersAndBindType(diagnostics);
            _lazyType = new TypeWithAnnotations.Boxed(type);

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
            bool isExplicitInterfaceImplementation = IsExplicitInterfaceImplementation;
            if (isExplicitInterfaceImplementation || this.IsOverride)
            {
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
                    CSharpSyntaxNode syntax = CSharpSyntaxNode;
                    string interfacePropertyName = IsIndexer ? WellKnownMemberNames.Indexer : ((PropertyDeclarationSyntax)syntax).Identifier.ValueText;
                    explicitlyImplementedProperty = this.FindExplicitlyImplementedProperty(_explicitInterfaceType, interfacePropertyName, GetExplicitInterfaceSpecifier(), diagnostics);
                    this.FindExplicitlyImplementedMemberVerification(explicitlyImplementedProperty, diagnostics);
                    overriddenOrImplementedProperty = explicitlyImplementedProperty;
                }

                if ((object)overriddenOrImplementedProperty != null)
                {
                    _lazyRefCustomModifiers = _refKind != RefKind.None ? overriddenOrImplementedProperty.RefCustomModifiers : ImmutableArray<CustomModifier>.Empty;

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

                _lazyRefCustomModifiers = ImmutableArray.Create(CSharpCustomModifier.CreateRequired(modifierType));
            }

            Debug.Assert(isExplicitInterfaceImplementation || _lazyExplicitInterfaceImplementations.IsEmpty);
            _lazyExplicitInterfaceImplementations =
                explicitlyImplementedProperty is null ?
                    ImmutableArray<PropertySymbol>.Empty :
                    ImmutableArray.Create(explicitlyImplementedProperty);
        }

        protected abstract Location TypeLocation { get; }

#nullable disable

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

        public sealed override RefKind RefKind
        {
            get
            {
                return _refKind;
            }
        }

        public sealed override TypeWithAnnotations TypeWithAnnotations
        {
            get
            {
                EnsureSignature();
                return _lazyType.Value;
            }
        }

#nullable enable 

        private void EnsureSignature()
        {
            if (!_state.HasComplete(CompletionPart.FinishPropertyEnsureSignature))
            {
                // If this lock ever encloses a potential call to Debugger.NotifyOfCrossThreadDependency,
                // then we should call DebuggerUtilities.CallBeforeAcquiringLock() (see method comment for more
                // details).

                lock (_syntaxRef)
                {
                    if (_state.NotePartComplete(CompletionPart.StartPropertyEnsureSignature))
                    {
                        // By setting StartPropertyEnsureSignature, we've committed to doing the work and setting
                        // FinishPropertyEnsureSignature.  So there is no cancellation supported between one and the other.
                        var diagnostics = DiagnosticBag.GetInstance();
                        try
                        {
                            EnsureSignatureGuarded(diagnostics);
                            AddDiagnostics(diagnostics);
                        }
                        finally
                        {
                            _state.NotePartComplete(CompletionPart.FinishPropertyEnsureSignature);
                            diagnostics.Free();
                        }
                    }
                    else
                    {
                        // Either (1) this thread is in the process of completing the method,
                        // or (2) some other thread has beat us to the punch and completed the method.
                        // We can distinguish the two cases here by checking for the FinishPropertyEnsureSignature
                        // part to be complete, which would only occur if another thread completed this
                        // method.
                        //
                        // The other case, in which this thread is in the process of completing the method,
                        // requires that we return here even though the work is not complete. That's because
                        // signature is processed by first populating the return type and parameters by binding
                        // the syntax from source.  Those values are visible to the same thread for the purpose
                        // of computing which methods are implemented and overridden.  But then those values
                        // may be rewritten (by the same thread) to copy down custom modifiers. In order to
                        // allow the same thread to see the return type and parameters from the syntax (though
                        // they do not yet take on their final values), we return here.

                        // Due to the fact that this method is potentially reentrant, we must use a 
                        // reentrant lock to avoid deadlock and cannot assert that at this point the work
                        // has completed (_state.HasComplete(CompletionPart.FinishPropertyEnsureSignature)).
                    }
                }
            }
        }

        private void AddDiagnostics(DiagnosticBag diagnostics)
        {
            if (!diagnostics.IsEmptyWithoutResolution)
            {
                DiagnosticBag? destination = _lazyDiagnosticBag;
                if (destination is null)
                {
                    var newBag = new DiagnosticBag();
                    destination = Interlocked.CompareExchange(ref _lazyDiagnosticBag, newBag, null!) ?? newBag;
                }

                destination.AddRange(diagnostics);
            }
        }

#nullable disable

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

        internal string SourceName
        {
            get
            {
                return _sourceName;
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

#nullable enable
        protected abstract SourcePropertyAccessorSymbol CreateGetAccessorSymbol(
            bool isAutoPropertyAccessor,
            DiagnosticBag diagnostics);

        protected abstract SourcePropertyAccessorSymbol CreateSetAccessorSymbol(
            bool isAutoPropertyAccessor,
            DiagnosticBag diagnostics);

        public sealed override MethodSymbol? GetMethod
        {
            get
            {
                if (_lazyGetMethod is null && (_propertyFlags & Flags.HasGetAccessor) != 0)
                {
                    var diagnostics = DiagnosticBag.GetInstance();
                    var result = CreateGetAccessorSymbol(isAutoPropertyAccessor: IsAutoProperty,
                                                         diagnostics);
                    if (Interlocked.CompareExchange(ref _lazyGetMethod, result, null) == null)
                    {
                        AddDiagnostics(diagnostics);
                    }

                    diagnostics.Free();
                }

                Debug.Assert((_lazyGetMethod is object) == ((_propertyFlags & Flags.HasGetAccessor) != 0));
                return _lazyGetMethod;
            }
        }

        public sealed override MethodSymbol? SetMethod
        {
            get
            {
                if (_lazySetMethod is null && (_propertyFlags & Flags.HasSetAccessor) != 0)
                {
                    var diagnostics = DiagnosticBag.GetInstance();
                    var result = CreateSetAccessorSymbol(isAutoPropertyAccessor: IsAutoProperty,
                                                         diagnostics);
                    if (Interlocked.CompareExchange(ref _lazySetMethod, result, null) == null)
                    {
                        AddDiagnostics(diagnostics);
                    }

                    diagnostics.Free();
                }

                Debug.Assert((_lazySetMethod is object) == ((_propertyFlags & Flags.HasSetAccessor) != 0));
                return _lazySetMethod;
            }
        }

#nullable disable

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get { return (IsStatic ? 0 : Microsoft.Cci.CallingConvention.HasThis); }
        }

        public sealed override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                EnsureSignature();
                return _lazyParameters;
            }
        }

        internal override bool IsExplicitInterfaceImplementation
            => (_propertyFlags & Flags.IsExplicitInterfaceImplementation) != 0;

        public sealed override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations
        {
            get
            {
                if (IsExplicitInterfaceImplementation)
                {
                    EnsureSignature();
                }
                else
                {
                    Debug.Assert(_lazyExplicitInterfaceImplementations.IsEmpty);
                }

                return _lazyExplicitInterfaceImplementations;
            }
        }

        public sealed override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get
            {
                EnsureSignature();
                return _lazyRefCustomModifiers;
            }
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

        internal bool IsAutoPropertyWithGetAccessor
            => (_propertyFlags & Flags.IsAutoPropertyWithGetAccessor) == Flags.IsAutoPropertyWithGetAccessor;

        protected bool IsAutoProperty
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
#nullable enable
            bool isExplicitInterfaceImplementation = IsExplicitInterfaceImplementation;
            this.CheckAccessibility(Location, diagnostics, isExplicitInterfaceImplementation);
            this.CheckModifiers(isExplicitInterfaceImplementation, Location, IsIndexer, diagnostics);

            bool hasInitializer = (_propertyFlags & Flags.HasInitializer) != 0;
            if (hasInitializer)
            {
                CheckInitializer(IsAutoProperty, ContainingType.IsInterface, IsStatic, Location, diagnostics);
            }

            if (IsAutoPropertyWithGetAccessor)
            {
                Debug.Assert(GetMethod is object);

                if (!IsStatic && SetMethod is { IsInitOnly: false })
                {
                    if (ContainingType.IsReadOnly)
                    {
                        diagnostics.Add(ErrorCode.ERR_AutoPropsInRoStruct, Location);
                    }
                    else if (HasReadOnlyModifier)
                    {
                        diagnostics.Add(ErrorCode.ERR_AutoPropertyWithSetterCantBeReadOnly, Location, this);
                    }
                }

                //issue a diagnostic if the compiler generated attribute ctor is not found.
                Binder.ReportUseSiteDiagnosticForSynthesizedAttribute(DeclaringCompilation,
                    WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor, diagnostics, location: Location);

                if (this.RefKind != RefKind.None)
                {
                    diagnostics.Add(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, Location, this);
                }

                // get-only auto property should not override settable properties
                if (SetMethod is null && !this.IsReadOnly)
                {
                    diagnostics.Add(ErrorCode.ERR_AutoPropertyMustOverrideSet, Location, this);
                }
            }

            if (!IsExpressionBodied)
            {
                bool hasGetAccessor = GetMethod is object;
                bool hasSetAccessor = SetMethod is object;

                if (hasGetAccessor && hasSetAccessor)
                {
                    Debug.Assert(_lazyGetMethod is object);
                    Debug.Assert(_lazySetMethod is object);

                    if (_refKind != RefKind.None)
                    {
                        diagnostics.Add(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, _lazySetMethod.Locations[0], _lazySetMethod);
                    }
                    else if ((_lazyGetMethod.LocalAccessibility != Accessibility.NotApplicable) &&
                        (_lazySetMethod.LocalAccessibility != Accessibility.NotApplicable))
                    {
                        // Check accessibility is set on at most one accessor.
                        diagnostics.Add(ErrorCode.ERR_DuplicatePropertyAccessMods, Location, this);
                    }
                    else if (_lazyGetMethod.LocalDeclaredReadOnly && _lazySetMethod.LocalDeclaredReadOnly)
                    {
                        diagnostics.Add(ErrorCode.ERR_DuplicatePropertyReadOnlyMods, Location, this);
                    }
                    else if (this.IsAbstract)
                    {
                        // Check abstract property accessors are not private.
                        CheckAbstractPropertyAccessorNotPrivate(_lazyGetMethod, diagnostics);
                        CheckAbstractPropertyAccessorNotPrivate(_lazySetMethod, diagnostics);
                    }
                }
                else
                {
                    if (!hasGetAccessor && !hasSetAccessor)
                    {
                        diagnostics.Add(ErrorCode.ERR_PropertyWithNoAccessors, Location, this);
                    }
                    else if (RefKind != RefKind.None)
                    {
                        if (!hasGetAccessor)
                        {
                            diagnostics.Add(ErrorCode.ERR_RefPropertyMustHaveGetAccessor, Location, this);
                        }
                    }
                    else if (!hasGetAccessor && IsAutoProperty)
                    {
                        diagnostics.Add(ErrorCode.ERR_AutoPropertyMustHaveGetAccessor, _lazySetMethod!.Locations[0], _lazySetMethod);
                    }

                    if (!this.IsOverride)
                    {
                        var accessor = _lazyGetMethod ?? _lazySetMethod;
                        if (accessor is object)
                        {
                            // Check accessibility is not set on the one accessor.
                            if (accessor.LocalAccessibility != Accessibility.NotApplicable)
                            {
                                diagnostics.Add(ErrorCode.ERR_AccessModMissingAccessor, Location, this);
                            }

                            // Check that 'readonly' is not set on the one accessor.
                            if (accessor.LocalDeclaredReadOnly)
                            {
                                diagnostics.Add(ErrorCode.ERR_ReadOnlyModMissingAccessor, Location, this);
                            }
                        }
                    }
                }

                // Check accessor accessibility is more restrictive than property accessibility.
                CheckAccessibilityMoreRestrictive(_lazyGetMethod, diagnostics);
                CheckAccessibilityMoreRestrictive(_lazySetMethod, diagnostics);
            }

            PropertySymbol? explicitlyImplementedProperty = ExplicitInterfaceImplementations.FirstOrDefault();

            if (explicitlyImplementedProperty is object)
            {
                CheckExplicitImplementationAccessor(GetMethod, explicitlyImplementedProperty.GetMethod, explicitlyImplementedProperty, diagnostics);
                CheckExplicitImplementationAccessor(SetMethod, explicitlyImplementedProperty.SetMethod, explicitlyImplementedProperty, diagnostics);
            }

#nullable disable

            Location location = TypeLocation;
            var compilation = DeclaringCompilation;

            Debug.Assert(location != null);

            // Check constraints on return type and parameters. Note: Dev10 uses the
            // property name location for any such errors. We'll do the same for return
            // type errors but for parameter errors, we'll use the parameter location.

            if ((object)_explicitInterfaceType != null)
            {
                var explicitInterfaceSpecifier = GetExplicitInterfaceSpecifier();
                Debug.Assert(explicitInterfaceSpecifier != null);
                _explicitInterfaceType.CheckAllConstraints(compilation, conversions, new SourceLocation(explicitInterfaceSpecifier.Name), diagnostics);

                // Note: we delayed nullable-related checks that could pull on NonNullTypes
                if (explicitlyImplementedProperty is object)
                {
                    TypeSymbol.CheckNullableReferenceTypeMismatchOnImplementingMember(this.ContainingType, this, explicitlyImplementedProperty, isExplicit: true, diagnostics);
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
                bool hasGetter = GetMethod is object;
                bool hasSetter = SetMethod is object;
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
            Debug.Assert(this.IsSealed && (GetMethod is null || SetMethod is null));

            if (GetMethod is object)
            {
                // need to synthesize setter
                MethodSymbol overriddenAccessor = this.GetOwnOrInheritedSetMethod();
                return (object)overriddenAccessor == null ? null : new SynthesizedSealedPropertyAccessor(this, overriddenAccessor);
            }
            else if (SetMethod is object)
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
            => IsAutoPropertyWithGetAccessor
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

                    case CompletionPart.StartPropertyEnsureSignature:
                    case CompletionPart.FinishPropertyEnsureSignature:
                        EnsureSignature();
                        Debug.Assert(_state.HasComplete(CompletionPart.FinishPropertyEnsureSignature));
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

                    case CompletionPart.StartPropertyFinalCompletion:
                    case CompletionPart.FinishPropertyFinalCompletion:
                        {
                            if (_state.NotePartComplete(CompletionPart.StartPropertyFinalCompletion))
                            {
                                DiagnosticBag diagnostic = _lazyDiagnosticBag;
                                if (diagnostic is object)
                                {
                                    Debug.Assert(!diagnostic.IsEmptyWithoutResolution);
                                    this.AddDeclarationDiagnostics(diagnostic);
                                    _lazyDiagnosticBag = null;
                                }

                                var completedOnThisThread = _state.NotePartComplete(CompletionPart.FinishPropertyFinalCompletion);
                                Debug.Assert(completedOnThisThread);
                            }
                            else
                            {
                                // StartPropertyFinalCompletion was completed by another thread. Wait for it to finish the work.
                                _state.SpinWaitComplete(CompletionPart.FinishPropertyFinalCompletion, cancellationToken);
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
            else if (this.IsAutoPropertyWithGetAccessor && type.IsRefLikeType && (this.IsStatic || !this.ContainingType.IsRefLikeType))
            {
                diagnostics.Add(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, TypeLocation, type);
            }
        }

        #endregion

#nullable enable

        protected abstract (TypeWithAnnotations Type, ImmutableArray<ParameterSymbol> Parameters) MakeParametersAndBindType(DiagnosticBag diagnostics);

        protected static ExplicitInterfaceSpecifierSyntax? GetExplicitInterfaceSpecifier(SyntaxNode syntax)
            => (syntax as BasePropertyDeclarationSyntax)?.ExplicitInterfaceSpecifier;

        internal ExplicitInterfaceSpecifierSyntax? GetExplicitInterfaceSpecifier()
            => GetExplicitInterfaceSpecifier(CSharpSyntaxNode);
#nullable disable
    }
}
