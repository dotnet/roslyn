// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// This class represents an event declared in source.  It may be either
    /// field-like (see <see cref="SourceFieldLikeEventSymbol"/>) or property-like (see
    /// <see cref="SourceCustomEventSymbol"/>).
    /// </summary>
    internal abstract class SourceEventSymbol : EventSymbol, IAttributeTargetSymbol
    {
        private readonly Location _location;
        private readonly SyntaxReference _syntaxRef;
        private readonly DeclarationModifiers _modifiers;
        internal readonly SourceMemberContainerTypeSymbol containingType;

        private SymbolCompletionState _state;
        private CustomAttributesBag<CSharpAttributeData> _lazyCustomAttributesBag;
        private string _lazyDocComment;
        private string _lazyExpandedDocComment;
        private OverriddenOrHiddenMembersResult _lazyOverriddenOrHiddenMembers;
        private ThreeState _lazyIsWindowsRuntimeEvent = ThreeState.Unknown;

        // TODO: CLSCompliantAttribute

        internal SourceEventSymbol(
            SourceMemberContainerTypeSymbol containingType,
            CSharpSyntaxNode syntax,
            SyntaxTokenList modifiers,
            bool isFieldLike,
            ExplicitInterfaceSpecifierSyntax interfaceSpecifierSyntaxOpt,
            SyntaxToken nameTokenSyntax,
            DiagnosticBag diagnostics)
        {
            _location = nameTokenSyntax.GetLocation();

            this.containingType = containingType;

            _syntaxRef = syntax.GetReference();

            var isExplicitInterfaceImplementation = interfaceSpecifierSyntaxOpt != null;
            bool modifierErrors;
            _modifiers = MakeModifiers(modifiers, isExplicitInterfaceImplementation, isFieldLike, _location, diagnostics, out modifierErrors);
            this.CheckAccessibility(_location, diagnostics, isExplicitInterfaceImplementation);
        }

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
            _state.DefaultForceComplete(this, cancellationToken);
        }

        public override abstract string Name { get; }

        public override abstract MethodSymbol AddMethod { get; }

        public override abstract MethodSymbol RemoveMethod { get; }

        public override abstract ImmutableArray<EventSymbol> ExplicitInterfaceImplementations { get; }

        public override abstract TypeWithAnnotations TypeWithAnnotations { get; }

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
                if (this.containingType.AnyMemberHasAttributes)
                {
                    var syntax = this.CSharpSyntaxNode;
                    if (syntax != null)
                    {
                        switch (syntax.Kind())
                        {
                            case SyntaxKind.EventDeclaration:
                                return ((EventDeclarationSyntax)syntax).AttributeLists;
                            case SyntaxKind.VariableDeclarator:
                                return ((EventFieldDeclarationSyntax)syntax.Parent.Parent).AttributeLists;
                            default:
                                throw ExceptionUtilities.UnexpectedValue(syntax.Kind());
                        }
                    }
                }

                return default(SyntaxList<AttributeListSyntax>);
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
            if ((_lazyCustomAttributesBag == null || !_lazyCustomAttributesBag.IsSealed) &&
                LoadAndValidateAttributes(OneOrMany.Create(this.AttributeDeclarationSyntaxList), ref _lazyCustomAttributesBag))
            {
                DeclaringCompilation.SymbolDeclaredEvent(this);
                var wasCompletedThisThread = _state.NotePartComplete(CompletionPart.Attributes);
                Debug.Assert(wasCompletedThisThread);
            }

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

        internal override CSharpAttributeData EarlyDecodeWellKnownAttribute(ref EarlyDecodeWellKnownAttributeArguments<EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation> arguments)
        {
            CSharpAttributeData boundAttribute;
            ObsoleteAttributeData obsoleteData;

            if (EarlyDecodeDeprecatedOrExperimentalOrObsoleteAttribute(ref arguments, out boundAttribute, out obsoleteData))
            {
                if (obsoleteData != null)
                {
                    arguments.GetOrCreateData<CommonEventEarlyWellKnownAttributeData>().ObsoleteAttributeData = obsoleteData;
                }

                return boundAttribute;
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

        internal sealed override void DecodeWellKnownAttribute(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);
            Debug.Assert(arguments.SymbolPart == AttributeLocation.None);

            if (attribute.IsTargetAttribute(this, AttributeDescription.SpecialNameAttribute))
            {
                arguments.GetOrCreateData<CommonEventWellKnownAttributeData>().HasSpecialNameAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.NullableAttribute))
            {
                // NullableAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitNullableAttribute, arguments.AttributeSyntaxOpt.Location);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.ExcludeFromCodeCoverageAttribute))
            {
                arguments.GetOrCreateData<CommonEventWellKnownAttributeData>().HasExcludeFromCodeCoverageAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.TupleElementNamesAttribute))
            {
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitTupleElementNamesAttribute, arguments.AttributeSyntaxOpt.Location);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.SkipLocalsInitAttribute))
            {
                attribute.DecodeSkipLocalsInitAttribute<CommonEventWellKnownAttributeData>(DeclaringCompilation, ref arguments);
            }
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = this.DeclaringCompilation;
            var type = this.TypeWithAnnotations;

            if (type.Type.ContainsDynamic())
            {
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDynamicAttribute(type.Type, type.CustomModifiers.Length));
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

        public sealed override bool IsExtern
        {
            get { return (_modifiers & DeclarationModifiers.Extern) != 0; }
        }

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

        private void CheckAccessibility(Location location, DiagnosticBag diagnostics, bool isExplicitInterfaceImplementation)
        {
            var info = ModifierUtils.CheckAccessibility(_modifiers, this, isExplicitInterfaceImplementation);
            if (info != null)
            {
                diagnostics.Add(new CSDiagnostic(info, location));
            }
        }

        private DeclarationModifiers MakeModifiers(SyntaxTokenList modifiers, bool explicitInterfaceImplementation,
                                                   bool isFieldLike, Location location,
                                                   DiagnosticBag diagnostics, out bool modifierErrors)
        {
            bool isInterface = this.ContainingType.IsInterface;
            var defaultAccess = isInterface && !explicitInterfaceImplementation ? DeclarationModifiers.Public : DeclarationModifiers.Private;
            var defaultInterfaceImplementationModifiers = DeclarationModifiers.None;

            // Check that the set of modifiers is allowed
            var allowedModifiers = DeclarationModifiers.Unsafe;
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
            else if (isInterface)
            {
                Debug.Assert(explicitInterfaceImplementation);
                allowedModifiers |= DeclarationModifiers.Abstract;
            }

            if (this.ContainingType.IsStructType())
            {
                allowedModifiers |= DeclarationModifiers.ReadOnly;
            }

            if (!isInterface)
            {
                allowedModifiers |= DeclarationModifiers.Extern;
            }

            var mods = ModifierUtils.MakeAndCheckNontypeMemberModifiers(modifiers, defaultAccess, allowedModifiers, location, diagnostics, out modifierErrors);

            this.CheckUnsafeModifier(mods, diagnostics);

            ModifierUtils.ReportDefaultInterfaceImplementationModifiers(!isFieldLike, mods,
                                                                        defaultInterfaceImplementationModifiers,
                                                                        location, diagnostics);

            // Let's overwrite modifiers for interface events with what they are supposed to be. 
            // Proper errors must have been reported by now.
            if (isInterface)
            {
                mods = ModifierUtils.AdjustModifiersForAnInterfaceMember(mods, !isFieldLike, explicitInterfaceImplementation);
            }

            return mods;
        }

        protected void CheckModifiersAndType(DiagnosticBag diagnostics)
        {
            Location location = this.Locations[0];
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            bool isExplicitInterfaceImplementationInInterface = ContainingType.IsInterface && IsExplicitInterfaceImplementation;

            if (this.DeclaredAccessibility == Accessibility.Private && (IsVirtual || (IsAbstract && !isExplicitInterfaceImplementationInInterface) || IsOverride))
            {
                diagnostics.Add(ErrorCode.ERR_VirtualPrivate, location, this);
            }
            else if (IsStatic && (IsOverride || IsVirtual || IsAbstract))
            {
                // A static member '{0}' cannot be marked as override, virtual, or abstract
                diagnostics.Add(ErrorCode.ERR_StaticNotVirtual, location, this);
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
            else if (!this.IsNoMoreVisibleThan(this.Type, ref useSiteDiagnostics) && (CSharpSyntaxNode as EventDeclarationSyntax)?.ExplicitInterfaceSpecifier == null)
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
                // '{0}' is abstract but it is contained in non-abstract class '{1}'
                diagnostics.Add(ErrorCode.ERR_AbstractInConcreteClass, location, this, ContainingType);
            }
            else if (IsVirtual && ContainingType.IsSealed)
            {
                // '{0}' is a new virtual member in sealed class '{1}'
                diagnostics.Add(ErrorCode.ERR_NewVirtualInSealed, location, this, ContainingType);
            }

            diagnostics.Add(location, useSiteDiagnostics);
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            ref var lazyDocComment = ref expandIncludes ? ref _lazyExpandedDocComment : ref _lazyDocComment;
            return SourceDocumentationCommentUtils.GetAndCacheDocumentationComment(this, expandIncludes, ref lazyDocComment);
        }

        protected static void CopyEventCustomModifiers(EventSymbol eventWithCustomModifiers, ref TypeWithAnnotations type, AssemblySymbol containingAssembly)
        {
            Debug.Assert((object)eventWithCustomModifiers != null);

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
            // If you explicitly implement an event, then you're a WinRT event if and only if it's a WinRT event.
            ImmutableArray<EventSymbol> explicitInterfaceImplementations = this.ExplicitInterfaceImplementations;
            if (!explicitInterfaceImplementations.IsEmpty)
            {
                // If there could be more than one, we'd have to worry about conflicts, but that's impossible for source events.
                Debug.Assert(explicitInterfaceImplementations.Length == 1);
                // Don't have to worry about conflicting with the override rule, since explicit impls are never overrides (in source).
                Debug.Assert((object)this.OverriddenEvent == null);

                return explicitInterfaceImplementations[0].IsWindowsRuntimeEvent;
            }

            // Interface events don't override or implicitly implement other events, so they only
            // depend on the output kind at this point.
            if (this.containingType.IsInterfaceType())
            {
                return this.IsCompilationOutputWinMdObj();
            }

            // If you override an event, then you're a WinRT event if and only if it's a WinRT event.
            EventSymbol overriddenEvent = this.OverriddenEvent;
            if ((object)overriddenEvent != null)
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

        protected TypeWithAnnotations BindEventType(Binder binder, TypeSyntax typeSyntax, DiagnosticBag diagnostics)
        {
            // NOTE: no point in reporting unsafe errors in the return type - anything unsafe will either
            // fail to be a delegate or will be (invalidly) passed as a type argument.
            // Prevent constraint checking.
            binder = binder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks | BinderFlags.SuppressUnsafeDiagnostics, this);

            return binder.BindType(typeSyntax, diagnostics);
        }

        internal override void AfterAddingTypeMembersChecks(ConversionsBase conversions, DiagnosticBag diagnostics)
        {
            var compilation = DeclaringCompilation;
            var location = this.Locations[0];

            this.CheckModifiersAndType(diagnostics);
            this.Type.CheckAllConstraints(compilation, conversions, location, diagnostics);

            if (compilation.ShouldEmitNullableAttributes(this) &&
                TypeWithAnnotations.NeedsNullableAttribute())
            {
                compilation.EnsureNullableAttributeExists(diagnostics, location, modifyCompilation: true);
            }
        }
    }
}
