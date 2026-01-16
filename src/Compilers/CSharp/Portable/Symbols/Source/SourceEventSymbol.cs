// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// This class represents an event declared in source.  It may be either
    /// field-like (see <see cref="SourceFieldLikeEventSymbol"/>) or property-like (see
    /// <see cref="SourceCustomEventSymbol"/>).
    /// </summary>
    internal abstract class SourceEventSymbol : EventSymbol, IAttributeTargetSymbol
    {
        private SourceEventSymbol? _otherPartOfPartial;

        private readonly Location _location;
        private readonly SyntaxReference _syntaxRef;
        private readonly DeclarationModifiers _modifiers;
        private readonly bool _hasExplicitAccessModifier;
        internal readonly SourceMemberContainerTypeSymbol containingType;

        private SymbolCompletionState _state;
        private CustomAttributesBag<CSharpAttributeData>? _lazyCustomAttributesBag;
        private string? _lazyDocComment;
        private string? _lazyExpandedDocComment;
        private OverriddenOrHiddenMembersResult? _lazyOverriddenOrHiddenMembers;
        private ThreeState _lazyIsWindowsRuntimeEvent = ThreeState.Unknown;

        // TODO: CLSCompliantAttribute

        internal SourceEventSymbol(
            SourceMemberContainerTypeSymbol containingType,
            CSharpSyntaxNode syntax,
            SyntaxTokenList modifiers,
            bool isFieldLike,
            ExplicitInterfaceSpecifierSyntax? interfaceSpecifierSyntaxOpt,
            SyntaxToken nameTokenSyntax,
            BindingDiagnosticBag diagnostics)
        {
            _location = nameTokenSyntax.GetLocation();

            this.containingType = containingType;

            _syntaxRef = syntax.GetReference();

            var isExplicitInterfaceImplementation = interfaceSpecifierSyntaxOpt != null;
            _modifiers = MakeModifiers(modifiers, isExplicitInterfaceImplementation, isFieldLike, _location, diagnostics, out _, out _hasExplicitAccessModifier);
            this.CheckAccessibility(_location, diagnostics, isExplicitInterfaceImplementation);
        }

        public Location Location
            => _location;

        internal sealed override bool RequiresCompletion
        {
            get { return true; }
        }

        internal sealed override bool HasComplete(CompletionPart part)
        {
            return _state.HasComplete(part);
        }

        internal override void ForceComplete(SourceLocation? locationOpt, Predicate<Symbol>? filter, CancellationToken cancellationToken)
        {
            SourcePartialImplementationPart?.ForceComplete(locationOpt, filter, cancellationToken);

            if (filter?.Invoke(this) == false)
            {
                return;
            }

            _state.DefaultForceComplete(this, cancellationToken);
        }

        public abstract override string Name { get; }

        public abstract override MethodSymbol? AddMethod { get; }

        public abstract override MethodSymbol? RemoveMethod { get; }

        public abstract override ImmutableArray<EventSymbol> ExplicitInterfaceImplementations { get; }

        public abstract override TypeWithAnnotations TypeWithAnnotations { get; }

        public sealed override Symbol ContainingSymbol
        {
            get
            {
                return containingType;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return this.containingType;
            }
        }

        internal override LexicalSortKey GetLexicalSortKey()
        {
            return new LexicalSortKey(_location, this.DeclaringCompilation);
        }

        public sealed override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray.Create(_location);
            }
        }

        public override Location TryGetFirstLocation()
            => _location;

        public sealed override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray.Create<SyntaxReference>(_syntaxRef);
            }
        }

        /// <summary>
        /// Gets the syntax list of custom attributes applied on the event symbol.
        /// </summary>
        internal SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList
        {
            get
            {
                if (this.containingType.AnyMemberHasAttributes && MemberSyntax is { } memberSyntax)
                {
                    return memberSyntax.AttributeLists;
                }

                return default;
            }
        }

        internal MemberDeclarationSyntax? MemberSyntax
        {
            get
            {
                if (this.CSharpSyntaxNode is { } syntax)
                {
                    switch (syntax.Kind())
                    {
                        case SyntaxKind.EventDeclaration:
                            return (EventDeclarationSyntax)syntax;
                        case SyntaxKind.VariableDeclarator:
                            Debug.Assert(syntax.Parent?.Parent is not null);
                            return (EventFieldDeclarationSyntax)syntax.Parent.Parent;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(syntax.Kind());
                    }
                }

                return null;
            }
        }

        IAttributeTargetSymbol IAttributeTargetSymbol.AttributesOwner
        {
            get { return this; }
        }

        AttributeLocation IAttributeTargetSymbol.DefaultAttributeLocation
        {
            get { return AttributeLocation.Event; }
        }

        AttributeLocation IAttributeTargetSymbol.AllowedAttributeLocations
        {
            get
            {
                return this.AllowedAttributeLocations;
            }
        }

        protected abstract AttributeLocation AllowedAttributeLocations
        {
            get;
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

            bool bagCreatedOnThisThread;

            if (SourcePartialDefinitionPart is { } definitionPart)
            {
                Debug.Assert(!ReferenceEquals(definitionPart, this));
                bag = definitionPart.GetAttributesBag();
                bagCreatedOnThisThread = Interlocked.CompareExchange(ref _lazyCustomAttributesBag, bag, null) == null;
            }
            else
            {
                bagCreatedOnThisThread = LoadAndValidateAttributes(this.GetAttributeDeclarations(), ref _lazyCustomAttributesBag);
            }

            if (bagCreatedOnThisThread)
            {
                DeclaringCompilation.SymbolDeclaredEvent(this);
                var wasCompletedThisThread = _state.NotePartComplete(CompletionPart.Attributes);
                Debug.Assert(wasCompletedThisThread);
            }

            RoslynDebug.AssertNotNull(_lazyCustomAttributesBag);
            return _lazyCustomAttributesBag;
        }

        private OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            // Attributes on partial events are owned by the definition part.
            // If this symbol has a non-null PartialDefinitionPart, we should have accessed this method through that definition symbol instead.
            Debug.Assert(PartialDefinitionPart is null);

            if (SourcePartialImplementationPart is { } implementationPart)
            {
                return OneOrMany.Create(this.AttributeDeclarationSyntaxList, implementationPart.AttributeDeclarationSyntaxList);
            }

            return OneOrMany.Create(this.AttributeDeclarationSyntaxList);
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
        protected CommonEventWellKnownAttributeData GetDecodedWellKnownAttributeData()
        {
            var attributesBag = _lazyCustomAttributesBag;
            if (attributesBag == null || !attributesBag.IsDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetAttributesBag();
            }

            return (CommonEventWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData;
        }

        /// <summary>
        /// Returns data decoded from special early bound well-known attributes applied to the symbol or null if there are no applied attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        internal CommonEventEarlyWellKnownAttributeData GetEarlyDecodedWellKnownAttributeData()
        {
            var attributesBag = _lazyCustomAttributesBag;

            if (attributesBag == null || !attributesBag.IsEarlyDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetAttributesBag();
            }

            return (CommonEventEarlyWellKnownAttributeData)attributesBag.EarlyDecodedWellKnownAttributeData;
        }

        internal override (CSharpAttributeData?, BoundAttribute?) EarlyDecodeWellKnownAttribute(ref EarlyDecodeWellKnownAttributeArguments<EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation> arguments)
        {
            CSharpAttributeData? attributeData;
            BoundAttribute? boundAttribute;
            ObsoleteAttributeData? obsoleteData;

            if (EarlyDecodeDeprecatedOrExperimentalOrObsoleteAttribute(ref arguments, out attributeData, out boundAttribute, out obsoleteData))
            {
                if (obsoleteData != null)
                {
                    arguments.GetOrCreateData<CommonEventEarlyWellKnownAttributeData>().ObsoleteAttributeData = obsoleteData;
                }

                return (attributeData, boundAttribute);
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
                if (!this.containingType.AnyMemberHasAttributes)
                {
                    return null;
                }

                var lazyCustomAttributesBag = _lazyCustomAttributesBag;
                if (lazyCustomAttributesBag != null && lazyCustomAttributesBag.IsEarlyDecodedWellKnownAttributeDataComputed)
                {
                    var data = (CommonEventEarlyWellKnownAttributeData)lazyCustomAttributesBag.EarlyDecodedWellKnownAttributeData;
                    return data != null ? data.ObsoleteAttributeData : null;
                }

                return ObsoleteAttributeData.Uninitialized;
            }
        }

        protected sealed override void DecodeWellKnownAttributeImpl(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);
            Debug.Assert(arguments.SymbolPart == AttributeLocation.None);
            var diagnostics = (BindingDiagnosticBag)arguments.Diagnostics;

            if (attribute.IsTargetAttribute(AttributeDescription.SpecialNameAttribute))
            {
                arguments.GetOrCreateData<CommonEventWellKnownAttributeData>().HasSpecialNameAttribute = true;
            }
            else if (ReportExplicitUseOfReservedAttributes(in arguments,
                ReservedAttributes.NullableAttribute
                | ReservedAttributes.NativeIntegerAttribute
                | ReservedAttributes.TupleElementNamesAttribute
                | ReservedAttributes.RequiresUnsafeAttribute
                | ReservedAttributes.ExtensionMarkerAttribute))
            {
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.ExcludeFromCodeCoverageAttribute))
            {
                arguments.GetOrCreateData<CommonEventWellKnownAttributeData>().HasExcludeFromCodeCoverageAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.SkipLocalsInitAttribute))
            {
                CSharpAttributeData.DecodeSkipLocalsInitAttribute<CommonEventWellKnownAttributeData>(DeclaringCompilation, ref arguments);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.UnscopedRefAttribute))
            {
                diagnostics.Add(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, arguments.AttributeSyntaxOpt!.Location);
            }
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData>? attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = this.DeclaringCompilation;
            var type = this.TypeWithAnnotations;

            if (type.Type.ContainsDynamic())
            {
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDynamicAttribute(type.Type, type.CustomModifiers.Length));
            }

            if (compilation.ShouldEmitNativeIntegerAttributes(type.Type))
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeNativeIntegerAttribute(this, type.Type));
            }

            if (type.Type.ContainsTupleNames())
            {
                AddSynthesizedAttribute(ref attributes,
                    DeclaringCompilation.SynthesizeTupleNamesAttribute(type.Type));
            }

            if (compilation.ShouldEmitNullableAttributes(this))
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeNullableAttributeIfNecessary(this, containingType.GetNullableContextValue(), type));
            }

            if (CallerUnsafeMode.NeedsRequiresUnsafeAttribute())
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.TrySynthesizeRequiresUnsafeAttribute(this));
            }
        }

        internal sealed override bool IsDirectlyExcludedFromCodeCoverage =>
            GetDecodedWellKnownAttributeData()?.HasExcludeFromCodeCoverageAttribute == true;

        internal sealed override bool HasSpecialName
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasSpecialNameAttribute;
            }
        }

        public bool HasSkipLocalsInitAttribute
            => GetDecodedWellKnownAttributeData()?.HasSkipLocalsInitAttribute == true;

        public sealed override bool IsAbstract
        {
            get { return (_modifiers & DeclarationModifiers.Abstract) != 0; }
        }

        private bool HasExternModifier
        {
            get { return (_modifiers & DeclarationModifiers.Extern) != 0; }
        }

        public sealed override bool IsExtern => PartialImplementationPart is { } implementation ? implementation.IsExtern : HasExternModifier;

        public sealed override bool IsStatic
        {
            get { return (_modifiers & DeclarationModifiers.Static) != 0; }
        }

        public sealed override bool IsOverride
        {
            get { return (_modifiers & DeclarationModifiers.Override) != 0; }
        }

        public sealed override bool IsSealed
        {
            get { return (_modifiers & DeclarationModifiers.Sealed) != 0; }
        }

        public sealed override bool IsVirtual
        {
            get { return (_modifiers & DeclarationModifiers.Virtual) != 0; }
        }

        internal bool IsReadOnly
        {
            get { return (_modifiers & DeclarationModifiers.ReadOnly) != 0; }
        }

        private bool IsUnsafe
        {
            get { return (_modifiers & DeclarationModifiers.Unsafe) != 0; }
        }

        internal sealed override CallerUnsafeMode CallerUnsafeMode
        {
            get
            {
                if (ContainingModule.UseUpdatedMemorySafetyRules)
                {
                    return IsUnsafe || IsExtern
                        ? CallerUnsafeMode.Explicit
                        : CallerUnsafeMode.None;
                }

                return Type.ContainsPointerOrFunctionPointer()
                    ? CallerUnsafeMode.Implicit : CallerUnsafeMode.None;
            }
        }

        public sealed override Accessibility DeclaredAccessibility
        {
            get { return ModifierUtils.EffectiveAccessibility(_modifiers); }
        }

        internal sealed override bool MustCallMethodsDirectly
        {
            get { return false; } // always false for source events
        }

        internal SyntaxReference SyntaxReference
        {
            get { return _syntaxRef; }
        }

        internal CSharpSyntaxNode CSharpSyntaxNode
        {
            get { return (CSharpSyntaxNode)_syntaxRef.GetSyntax(); }
        }

        internal SyntaxTree SyntaxTree
        {
            get { return _syntaxRef.SyntaxTree; }
        }

        internal bool IsNew
        {
            get { return (_modifiers & DeclarationModifiers.New) != 0; }
        }

        internal DeclarationModifiers Modifiers
        {
            get { return _modifiers; }
        }

        private void CheckAccessibility(Location location, BindingDiagnosticBag diagnostics, bool isExplicitInterfaceImplementation)
        {
            ModifierUtils.CheckAccessibility(_modifiers, this, isExplicitInterfaceImplementation, diagnostics, location);
        }

        private DeclarationModifiers MakeModifiers(SyntaxTokenList modifiers, bool explicitInterfaceImplementation,
                                                   bool isFieldLike, Location location,
                                                   BindingDiagnosticBag diagnostics, out bool modifierErrors,
                                                   out bool hasExplicitAccessModifier)
        {
            bool isInterface = this.ContainingType.IsInterface;
            var defaultAccess = isInterface && !explicitInterfaceImplementation ? DeclarationModifiers.Public : DeclarationModifiers.Private;
            var defaultInterfaceImplementationModifiers = DeclarationModifiers.None;

            // Check that the set of modifiers is allowed
            var allowedModifiers = DeclarationModifiers.Partial | DeclarationModifiers.Unsafe;
            if (!explicitInterfaceImplementation)
            {
                allowedModifiers |= DeclarationModifiers.New |
                                    DeclarationModifiers.Sealed |
                                    DeclarationModifiers.Abstract |
                                    DeclarationModifiers.Static |
                                    DeclarationModifiers.Virtual |
                                    DeclarationModifiers.AccessibilityMask;

                if (!isInterface)
                {
                    allowedModifiers |= DeclarationModifiers.Override;
                }
                else
                {
                    // This is needed to make sure we can detect 'public' modifier specified explicitly and
                    // check it against language version below.
                    defaultAccess = DeclarationModifiers.None;

                    allowedModifiers |= DeclarationModifiers.Extern;
                    defaultInterfaceImplementationModifiers |= DeclarationModifiers.Sealed |
                                                               DeclarationModifiers.Abstract |
                                                               DeclarationModifiers.Static |
                                                               DeclarationModifiers.Virtual |
                                                               DeclarationModifiers.Extern |
                                                               DeclarationModifiers.AccessibilityMask;
                }
            }
            else
            {
                Debug.Assert(explicitInterfaceImplementation);

                if (isInterface)
                {
                    allowedModifiers |= DeclarationModifiers.Abstract;
                }

                allowedModifiers |= DeclarationModifiers.Static;
            }

            if (this.ContainingType.IsStructType())
            {
                allowedModifiers |= DeclarationModifiers.ReadOnly;
            }

            if (!isInterface)
            {
                allowedModifiers |= DeclarationModifiers.Extern;
            }

            var mods = ModifierUtils.MakeAndCheckNonTypeMemberModifiers(isOrdinaryMethod: false, isForInterfaceMember: isInterface,
                                                                        modifiers, defaultAccess, allowedModifiers, location, diagnostics, out modifierErrors,
                                                                        out hasExplicitAccessModifier);

            ModifierUtils.CheckFeatureAvailabilityForStaticAbstractMembersInInterfacesIfNeeded(mods, explicitInterfaceImplementation, location, diagnostics);

            this.CheckUnsafeModifier(mods, diagnostics);

            ModifierUtils.ReportDefaultInterfaceImplementationModifiers(!isFieldLike, mods,
                                                                        defaultInterfaceImplementationModifiers,
                                                                        location, diagnostics);

            // Let's overwrite modifiers for interface events with what they are supposed to be. 
            // Proper errors must have been reported by now.
            if (isInterface)
            {
                mods = ModifierUtils.AdjustModifiersForAnInterfaceMember(mods, !isFieldLike, explicitInterfaceImplementation, forMethod: false);
            }

            return mods;
        }

        protected void CheckModifiersAndType(BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(!IsStatic || !IsOverride); // Otherwise should have been reported and cleared earlier.
            Debug.Assert(!IsStatic || ContainingType.IsInterface || (!IsAbstract && !IsVirtual)); // Otherwise should have been reported and cleared earlier.

            Location location = this.GetFirstLocation();
            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);
            bool isExplicitInterfaceImplementationInInterface = ContainingType.IsInterface && IsExplicitInterfaceImplementation;

            if (this.DeclaredAccessibility == Accessibility.Private && (IsVirtual || (IsAbstract && !isExplicitInterfaceImplementationInInterface) || IsOverride))
            {
                diagnostics.Add(ErrorCode.ERR_VirtualPrivate, location, this);
            }
            else if (IsReadOnly && IsStatic)
            {
                // Static member '{0}' cannot be marked 'readonly'.
                diagnostics.Add(ErrorCode.ERR_StaticMemberCantBeReadOnly, location, this);
            }
            else if (IsReadOnly && HasAssociatedField)
            {
                // Field-like event '{0}' cannot be 'readonly'.
                diagnostics.Add(ErrorCode.ERR_FieldLikeEventCantBeReadOnly, location, this);
            }
            else if (IsOverride && (IsNew || IsVirtual))
            {
                // A member '{0}' marked as override cannot be marked as new or virtual
                diagnostics.Add(ErrorCode.ERR_OverrideNotNew, location, this);
            }
            else if (IsSealed && !IsOverride && !(isExplicitInterfaceImplementationInInterface && IsAbstract))
            {
                // '{0}' cannot be sealed because it is not an override
                diagnostics.Add(ErrorCode.ERR_SealedNonOverride, location, this);
            }
            else if (IsPartial && !ContainingType.IsPartial())
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberOnlyInPartialClass, location);
            }
            else if (IsPartial && IsExplicitInterfaceImplementation)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberNotExplicit, location);
            }
            else if (IsPartial && IsAbstract)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberCannotBeAbstract, location);
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
                diagnostics.Add(ErrorCode.ERR_InstanceMemberInStaticClass, location, Name);
            }
            else if (this.Type.IsVoidType())
            {
                // Diagnostic reported by parser.
            }
            else if (!this.IsNoMoreVisibleThan(this.Type, ref useSiteInfo) && (CSharpSyntaxNode as EventDeclarationSyntax)?.ExplicitInterfaceSpecifier == null)
            {
                // Dev10 reports different errors for field-like events (ERR_BadVisFieldType) and custom events (ERR_BadVisPropertyType).
                // Both seem odd, so add a new one.

                diagnostics.Add(ErrorCode.ERR_BadVisEventType, location, this, this.Type);
            }
            else if (!this.Type.IsDelegateType() && !this.Type.IsErrorType())
            {
                // Suppressed for error types.
                diagnostics.Add(ErrorCode.ERR_EventNotDelegate, location, this);
            }
            else if (IsAbstract && !ContainingType.IsAbstract && (ContainingType.TypeKind == TypeKind.Class || ContainingType.TypeKind == TypeKind.Submission))
            {
                // '{0}' is abstract but it is contained in non-abstract type '{1}'
                diagnostics.Add(ErrorCode.ERR_AbstractInConcreteClass, location, this, ContainingType);
            }
            else if (IsVirtual && ContainingType.IsSealed)
            {
                // '{0}' is a new virtual member in sealed type '{1}'
                diagnostics.Add(ErrorCode.ERR_NewVirtualInSealed, location, this, ContainingType);
            }

            if (IsPartial)
            {
                ModifierUtils.CheckFeatureAvailabilityForPartialEventsAndConstructors(_location, diagnostics);
            }

            diagnostics.Add(location, useSiteInfo);
        }

        public override string GetDocumentationCommentXml(CultureInfo? preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default)
        {
            ref var lazyDocComment = ref expandIncludes ? ref _lazyExpandedDocComment : ref _lazyDocComment;
            return SourceDocumentationCommentUtils.GetAndCacheDocumentationComment(this, expandIncludes, ref lazyDocComment);
        }

        protected static void CopyEventCustomModifiers(EventSymbol eventWithCustomModifiers, ref TypeWithAnnotations type, AssemblySymbol containingAssembly)
        {
            RoslynDebug.Assert((object)eventWithCustomModifiers != null);

            TypeSymbol overriddenEventType = eventWithCustomModifiers.Type;

            // We do an extra check before copying the type to handle the case where the overriding
            // event (incorrectly) has a different type than the overridden event.  In such cases,
            // we want to retain the original (incorrect) type to avoid hiding the type given in source.
            if (type.Type.Equals(overriddenEventType, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes | TypeCompareKind.IgnoreDynamic))
            {
                type = type.WithTypeAndModifiers(CustomModifierUtils.CopyTypeCustomModifiers(overriddenEventType, type.Type, containingAssembly),
                                   eventWithCustomModifiers.TypeWithAnnotations.CustomModifiers);
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

        public sealed override bool IsWindowsRuntimeEvent
        {
            get
            {
                if (!_lazyIsWindowsRuntimeEvent.HasValue())
                {
                    // This would be a CompareExchange if there was an overload for ThreeState.
                    _lazyIsWindowsRuntimeEvent = ComputeIsWindowsRuntimeEvent().ToThreeState();
                }
                Debug.Assert(_lazyIsWindowsRuntimeEvent.HasValue());
                return _lazyIsWindowsRuntimeEvent.Value();
            }
        }

        private bool ComputeIsWindowsRuntimeEvent()
        {
            if (PartialDefinitionPart is { } partialDefinitionPart)
            {
                return partialDefinitionPart.IsWindowsRuntimeEvent;
            }

            // If you explicitly implement an event, then you're a WinRT event if and only if it's a WinRT event.
            ImmutableArray<EventSymbol> explicitInterfaceImplementations = this.ExplicitInterfaceImplementations;
            if (!explicitInterfaceImplementations.IsEmpty)
            {
                // If there could be more than one, we'd have to worry about conflicts, but that's impossible for source events.
                Debug.Assert(explicitInterfaceImplementations.Length == 1);
                // Don't have to worry about conflicting with the override rule, since explicit impls are never overrides (in source).
                Debug.Assert((object?)this.OverriddenEvent == null);

                return explicitInterfaceImplementations[0].IsWindowsRuntimeEvent;
            }

            // Interface events don't override or implicitly implement other events, so they only
            // depend on the output kind at this point.
            if (this.containingType.IsInterfaceType())
            {
                return this.IsCompilationOutputWinMdObj();
            }

            // If you override an event, then you're a WinRT event if and only if it's a WinRT event.
            EventSymbol? overriddenEvent = this.OverriddenEvent;
            if ((object?)overriddenEvent != null)
            {
                return overriddenEvent.IsWindowsRuntimeEvent;
            }

            // If you implicitly implement one or more interface events (for yourself, not for a derived type),
            // then you're a WinRT event if and only if at least one is a WinRT event.
            //
            // NOTE: it's possible that we returned false above even though we would have returned true
            // below.  Whenever this occurs, we need to report a diagnostic (because an event can't be
            // both WinRT and non-WinRT), but we'll do that when we're checking interface implementations
            // (see SourceMemberContainerTypeSymbol.ComputeInterfaceImplementations).
            bool sawImplicitImplementation = false;
            foreach (NamedTypeSymbol @interface in this.containingType.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Keys)
            {
                foreach (Symbol interfaceMember in @interface.GetMembers(this.Name))
                {
                    if (interfaceMember.Kind == SymbolKind.Event && //quick check (necessary, not sufficient)
                        interfaceMember.IsImplementableInterfaceMember() &&
                        // We are passing ignoreImplementationInInterfacesIfResultIsNotReady: true to avoid a cycle. If false is passed, FindImplementationForInterfaceMemberInNonInterface
                        // will look how event accessors are implemented and we end up here again since we will need to know their signature for that.
                        this == this.containingType.FindImplementationForInterfaceMemberInNonInterface(interfaceMember, ignoreImplementationInInterfacesIfResultIsNotReady: true)) //slow check (necessary and sufficient)
                    {
                        sawImplicitImplementation = true;

                        if (((EventSymbol)interfaceMember).IsWindowsRuntimeEvent)
                        {
                            return true;
                        }
                    }
                }
            }

            // If you implement one or more interface events and none of them are WinRT events, then you
            // are not a WinRT event.
            if (sawImplicitImplementation)
            {
                return false;
            }

            // If you're not constrained by your relationships with other members, then you're a WinRT event
            // if and only if this compilation will produce a ".winmdobj" file.
            return this.IsCompilationOutputWinMdObj();
        }

        internal static string GetAccessorName(string eventName, bool isAdder)
        {
            return (isAdder ? "add_" : "remove_") + eventName;
        }

        protected TypeWithAnnotations BindEventType(Binder binder, TypeSyntax typeSyntax, BindingDiagnosticBag diagnostics)
        {
            // NOTE: no point in reporting unsafe errors in the return type - anything unsafe will either
            //       fail to be a delegate or will be (invalidly) passed as a type argument.
            //       Actually, this is wrong (e.g., `Action<int*[]>` is valid): https://github.com/dotnet/roslyn/issues/81944.
            // Prevent constraint checking.
            binder = binder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks | BinderFlags.SuppressUnsafeDiagnostics, this);

            return binder.BindType(typeSyntax, diagnostics);
        }

        internal override void AfterAddingTypeMembersChecks(ConversionsBase conversions, BindingDiagnosticBag diagnostics)
        {
            var compilation = DeclaringCompilation;
            var location = this.GetFirstLocation();

            this.CheckModifiersAndType(diagnostics);
            this.Type.CheckAllConstraints(compilation, conversions, location, diagnostics);

            if (compilation.ShouldEmitNativeIntegerAttributes(Type))
            {
                compilation.EnsureNativeIntegerAttributeExists(diagnostics, location, modifyCompilation: true);
            }

            if (compilation.ShouldEmitNullableAttributes(this) &&
                TypeWithAnnotations.NeedsNullableAttribute())
            {
                compilation.EnsureNullableAttributeExists(diagnostics, location, modifyCompilation: true);
            }

            if (CallerUnsafeMode.NeedsRequiresUnsafeAttribute())
            {
                MessageID.IDS_FeatureUnsafeEvolution.CheckFeatureAvailability(diagnostics, compilation, location);
                compilation.EnsureRequiresUnsafeAttributeExists(diagnostics, location, modifyCompilation: true);
            }

            EventSymbol? explicitlyImplementedEvent = ExplicitInterfaceImplementations.FirstOrDefault();

            if (explicitlyImplementedEvent is object)
            {
                CheckExplicitImplementationAccessor(AddMethod, explicitlyImplementedEvent.AddMethod, explicitlyImplementedEvent, diagnostics);
                CheckExplicitImplementationAccessor(RemoveMethod, explicitlyImplementedEvent.RemoveMethod, explicitlyImplementedEvent, diagnostics);
            }

            if (IsPartialDefinition && OtherPartOfPartial is { } implementation)
            {
                PartialEventChecks(implementation, diagnostics);
            }
        }

        private void CheckExplicitImplementationAccessor(MethodSymbol? thisAccessor, MethodSymbol? otherAccessor, EventSymbol explicitlyImplementedEvent, BindingDiagnosticBag diagnostics)
        {
            if (!otherAccessor.IsImplementable() && thisAccessor is object)
            {
                diagnostics.Add(ErrorCode.ERR_ExplicitPropertyAddingAccessor, thisAccessor.GetFirstLocation(), thisAccessor, explicitlyImplementedEvent);
            }
        }

        private void PartialEventChecks(SourceEventSymbol implementation, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(this.IsPartialDefinition);
            Debug.Assert(!ReferenceEquals(this, implementation));
            Debug.Assert(ReferenceEquals(this.OtherPartOfPartial, implementation));

            if (!TypeWithAnnotations.Equals(implementation.TypeWithAnnotations, TypeCompareKind.AllIgnoreOptions))
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberTypeDifference, implementation.GetFirstLocation());
            }
            else if (MemberSignatureComparer.ConsideringTupleNamesCreatesDifference(this, implementation))
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberInconsistentTupleNames, implementation.GetFirstLocation(), this, implementation);
            }
            else if (!MemberSignatureComparer.PartialMethodsStrictComparer.Equals(this, implementation))
            {
                diagnostics.Add(ErrorCode.WRN_PartialMemberSignatureDifference, implementation.GetFirstLocation(),
                    new FormattedSymbol(this, SymbolDisplayFormat.MinimallyQualifiedFormat),
                    new FormattedSymbol(implementation, SymbolDisplayFormat.MinimallyQualifiedFormat));
            }

            if (IsStatic != implementation.IsStatic)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberStaticDifference, implementation.GetFirstLocation());
            }

            if (IsUnsafe != implementation.IsUnsafe && this.CompilationAllowsUnsafe())
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberUnsafeDifference, implementation.GetFirstLocation());
            }

            if (DeclaredAccessibility != implementation.DeclaredAccessibility
                || _hasExplicitAccessModifier != implementation._hasExplicitAccessModifier)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberAccessibilityDifference, implementation.GetFirstLocation());
            }

            if (IsVirtual != implementation.IsVirtual
                || IsOverride != implementation.IsOverride
                || IsSealed != implementation.IsSealed
                || IsNew != implementation.IsNew)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberExtendedModDifference, implementation.GetFirstLocation());
            }
        }

        internal bool IsPartial => (this.Modifiers & DeclarationModifiers.Partial) != 0;

        /// <summary>
        /// <see langword="false"/> if this symbol corresponds to a semi-colon body declaration.
        /// <see langword="true"/> if this symbol corresponds to a declaration with custom <see langword="add"/> and <see langword="remove"/> accessors.
        /// </summary>
        protected abstract bool AccessorsHaveImplementation { get; }

        internal sealed override bool IsPartialDefinition => IsPartial && !AccessorsHaveImplementation && !HasExternModifier;

        internal bool IsPartialImplementation => IsPartial && (AccessorsHaveImplementation || HasExternModifier);

        internal SourceEventSymbol? OtherPartOfPartial => _otherPartOfPartial;

        internal SourceEventSymbol? SourcePartialDefinitionPart => IsPartialImplementation ? OtherPartOfPartial : null;

        internal SourceEventSymbol? SourcePartialImplementationPart => IsPartialDefinition ? OtherPartOfPartial : null;

        internal sealed override EventSymbol? PartialDefinitionPart => SourcePartialDefinitionPart;

        internal sealed override EventSymbol? PartialImplementationPart => SourcePartialImplementationPart;

        internal static void InitializePartialEventParts(SourceEventSymbol definition, SourceEventSymbol implementation)
        {
            Debug.Assert(definition.IsPartialDefinition);
            Debug.Assert(implementation.IsPartialImplementation);

            Debug.Assert(definition._otherPartOfPartial is not { } alreadySetImplPart || ReferenceEquals(alreadySetImplPart, implementation));
            Debug.Assert(implementation._otherPartOfPartial is not { } alreadySetDefPart || ReferenceEquals(alreadySetDefPart, definition));

            definition._otherPartOfPartial = implementation;
            implementation._otherPartOfPartial = definition;

            Debug.Assert(ReferenceEquals(definition._otherPartOfPartial, implementation));
            Debug.Assert(ReferenceEquals(implementation._otherPartOfPartial, definition));
        }
    }
}
