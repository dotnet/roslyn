// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// The class to represent all events imported from a PE/module.
    /// </summary>
    internal sealed class PEEventSymbol : EventSymbol
    {
        private readonly string _name;
        private readonly PENamedTypeSymbol _containingType;
        private readonly EventDefinitionHandle _handle;
        private readonly TypeWithAnnotations _eventTypeWithAnnotations;
        private readonly PEMethodSymbol _addMethod;
        private readonly PEMethodSymbol _removeMethod;
        private readonly PEFieldSymbol? _associatedFieldOpt;
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;
        private Tuple<CultureInfo, string>? _lazyDocComment;
        private CachedUseSiteInfo<AssemblySymbol> _lazyCachedUseSiteInfo = CachedUseSiteInfo<AssemblySymbol>.Uninitialized;

        private ObsoleteAttributeData _lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized;

        // Distinct accessibility value to represent unset.
        private const int UnsetAccessibility = -1;
        private int _lazyDeclaredAccessibility = UnsetAccessibility;

        private byte _lazyRequiresUnsafe;

        private readonly Flags _flags;
        [Flags]
        private enum Flags : byte
        {
            IsSpecialName = 1,
            IsRuntimeSpecialName = 2,
            CallMethodsDirectly = 4
        }

        internal PEEventSymbol(
            PEModuleSymbol moduleSymbol,
            PENamedTypeSymbol containingType,
            EventDefinitionHandle handle,
            PEMethodSymbol addMethod,
            PEMethodSymbol removeMethod,
            MultiDictionary<string, PEFieldSymbol> privateFieldNameToSymbols)
        {
            RoslynDebug.Assert((object)moduleSymbol != null);
            RoslynDebug.Assert((object)containingType != null);
            Debug.Assert(!handle.IsNil);
            RoslynDebug.Assert((object)addMethod != null);
            RoslynDebug.Assert((object)removeMethod != null);

            _addMethod = addMethod;
            _removeMethod = removeMethod;
            _handle = handle;
            _containingType = containingType;

            EventAttributes mdFlags = 0;
            EntityHandle eventType = default(EntityHandle);

            try
            {
                var module = moduleSymbol.Module;
                module.GetEventDefPropsOrThrow(handle, out _name, out mdFlags, out eventType);
            }
            catch (BadImageFormatException mrEx)
            {
                _name = _name ?? string.Empty;
                _lazyCachedUseSiteInfo.Initialize(new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this));

                if (eventType.IsNil)
                {
                    _eventTypeWithAnnotations = TypeWithAnnotations.Create(new UnsupportedMetadataTypeSymbol(mrEx));
                }
            }

            TypeSymbol originalEventType = _eventTypeWithAnnotations.Type;
            if (!_eventTypeWithAnnotations.HasType)
            {
                var metadataDecoder = new MetadataDecoder(moduleSymbol, containingType);
                originalEventType = metadataDecoder.GetTypeOfToken(eventType);

                const int targetSymbolCustomModifierCount = 0;
                var typeSymbol = DynamicTypeDecoder.TransformType(originalEventType, targetSymbolCustomModifierCount, handle, moduleSymbol);
                typeSymbol = NativeIntegerTypeDecoder.TransformType(typeSymbol, handle, moduleSymbol, _containingType);

                // We start without annotation (they will be decoded below)
                var type = TypeWithAnnotations.Create(typeSymbol);

                // Decode nullable before tuple types to avoid converting between
                // NamedTypeSymbol and TupleTypeSymbol unnecessarily.

                // The containing type is passed to NullableTypeDecoder.TransformType to determine access
                // because the event does not have explicit accessibility in metadata.
                type = NullableTypeDecoder.TransformType(type, handle, moduleSymbol, accessSymbol: _containingType, nullableContext: _containingType);
                type = TupleTypeDecoder.DecodeTupleTypesIfApplicable(type, handle, moduleSymbol);
                _eventTypeWithAnnotations = type;
            }

            // IsWindowsRuntimeEvent checks the signatures, so we just have to check the accessors.
            bool isWindowsRuntimeEvent = IsWindowsRuntimeEvent;
            bool callMethodsDirectly = isWindowsRuntimeEvent
                ? !DoModifiersMatch(_addMethod, _removeMethod)
                : !DoSignaturesMatch(moduleSymbol, originalEventType, _addMethod, _removeMethod);

            if (callMethodsDirectly)
            {
                _flags |= Flags.CallMethodsDirectly;
            }
            else
            {
                _addMethod.SetAssociatedEvent(this, MethodKind.EventAdd);
                _removeMethod.SetAssociatedEvent(this, MethodKind.EventRemove);

                PEFieldSymbol? associatedField = GetAssociatedField(privateFieldNameToSymbols, isWindowsRuntimeEvent);
                if ((object?)associatedField != null)
                {
                    _associatedFieldOpt = associatedField;
                    associatedField.SetAssociatedEvent(this);
                }
            }

            if ((mdFlags & EventAttributes.SpecialName) != 0)
            {
                _flags |= Flags.IsSpecialName;
            }

            if ((mdFlags & EventAttributes.RTSpecialName) != 0)
            {
                _flags |= Flags.IsRuntimeSpecialName;
            }
        }

        /// <summary>
        /// Look for a field with the same name and an appropriate type (i.e. the same type, except in WinRT).
        /// If one is found, the caller will assume that this event was originally field-like and associate
        /// the two symbols.
        /// </summary>
        /// <remarks>
        /// Perf impact: If we find a field with the same name, we will eagerly evaluate its type.
        /// </remarks>
        private PEFieldSymbol? GetAssociatedField(MultiDictionary<string, PEFieldSymbol> privateFieldNameToSymbols, bool isWindowsRuntimeEvent)
        {
            // NOTE: Neither the name nor the accessibility of a PEFieldSymbol is lazy.
            foreach (PEFieldSymbol candidateAssociatedField in privateFieldNameToSymbols[_name])
            {
                Debug.Assert(candidateAssociatedField.DeclaredAccessibility == Accessibility.Private);

                // Unfortunately, this will cause us to realize the type of the field, which would
                // otherwise have been lazy.
                TypeSymbol candidateAssociatedFieldType = candidateAssociatedField.Type;

                if (isWindowsRuntimeEvent)
                {
                    NamedTypeSymbol eventRegistrationTokenTable_T = ((PEModuleSymbol)(this.ContainingModule)).EventRegistrationTokenTable_T;
                    if (TypeSymbol.Equals(eventRegistrationTokenTable_T, candidateAssociatedFieldType.OriginalDefinition, TypeCompareKind.ConsiderEverything2) &&
                        TypeSymbol.Equals(_eventTypeWithAnnotations.Type, ((NamedTypeSymbol)candidateAssociatedFieldType).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type, TypeCompareKind.ConsiderEverything2))
                    {
                        return candidateAssociatedField;
                    }
                }
                else
                {
                    if (TypeSymbol.Equals(candidateAssociatedFieldType, _eventTypeWithAnnotations.Type, TypeCompareKind.ConsiderEverything2))
                    {
                        return candidateAssociatedField;
                    }
                }
            }

            return null;
        }

        public override bool IsWindowsRuntimeEvent
        {
            get
            {
                NamedTypeSymbol token = ((PEModuleSymbol)(this.ContainingModule)).EventRegistrationToken;

                // If the addMethod returns an EventRegistrationToken
                // and the removeMethod accepts an EventRegistrationToken
                // then the Event is a WinRT type event and can be called
                // using += and -=.
                // NOTE: this check mimics the one in the native compiler
                // (see IMPORTER::ImportEvent).  In particular, it specifically
                // does not check whether the containing type is a WinRT type -
                // it was a design goal to accept any events of this form.
                return
                    TypeSymbol.Equals(_addMethod.ReturnType, token, TypeCompareKind.ConsiderEverything2) &&
                    _addMethod.ParameterCount == 1 &&
                    _removeMethod.ParameterCount == 1 &&
                    TypeSymbol.Equals(_removeMethod.Parameters[0].Type, token, TypeCompareKind.ConsiderEverything2);
            }
        }

        internal override FieldSymbol? AssociatedField
        {
            get
            {
                return _associatedFieldOpt;
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

        public override string Name
        {
            get { return _name; }
        }

        public override int MetadataToken
        {
            get { return MetadataTokens.GetToken(_handle); }
        }

        internal override bool HasSpecialName
        {
            get { return (_flags & Flags.IsSpecialName) != 0; }
        }

        internal override bool HasRuntimeSpecialName
        {
            get { return (_flags & Flags.IsRuntimeSpecialName) != 0; }
        }

        internal EventDefinitionHandle Handle
        {
            get
            {
                return _handle;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                if (_lazyDeclaredAccessibility == UnsetAccessibility)
                {
                    Accessibility accessibility = PEPropertyOrEventHelpers.GetDeclaredAccessibilityFromAccessors(_addMethod, _removeMethod);
                    Interlocked.CompareExchange(ref _lazyDeclaredAccessibility, (int)accessibility, UnsetAccessibility);
                }

                return (Accessibility)_lazyDeclaredAccessibility;
            }
        }

        public override bool IsExtern
        {
            get
            {
                // Some accessor extern.
                return _addMethod.IsExtern || _removeMethod.IsExtern;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                // Some accessor abstract.
                return _addMethod.IsAbstract || _removeMethod.IsAbstract;
            }
        }

        public override bool IsSealed
        {
            get
            {
                // Some accessor sealed. (differs from properties)
                return _addMethod.IsSealed || _removeMethod.IsSealed;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                // Some accessor virtual (as long as another isn't override or abstract).
                return !IsOverride && !IsAbstract && (_addMethod.IsVirtual || _removeMethod.IsVirtual);
            }
        }

        public override bool IsOverride
        {
            get
            {
                // Some accessor override.
                return _addMethod.IsOverride || _removeMethod.IsOverride;
            }
        }

        public override bool IsStatic
        {
            get
            {
                // All accessors static.
                return _addMethod.IsStatic && _removeMethod.IsStatic;
            }
        }

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get { return _eventTypeWithAnnotations; }
        }

        public override MethodSymbol AddMethod
        {
            get { return _addMethod; }
        }

        public override MethodSymbol RemoveMethod
        {
            get { return _removeMethod; }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _containingType.ContainingPEModule.MetadataLocation.Cast<MetadataLocation, Location>();
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (_lazyCustomAttributes.IsDefault)
            {
                var containingPEModuleSymbol = (PEModuleSymbol)this.ContainingModule;

                var requiresUnsafeState = (ThreeState)Volatile.Read(ref _lazyRequiresUnsafe);
                bool checkForRequiresUnsafe = requiresUnsafeState != ThreeState.False;

                if (checkForRequiresUnsafe)
                {
                    ImmutableArray<CSharpAttributeData> attributes = containingPEModuleSymbol.GetCustomAttributesForToken(
                          _handle,
                          out CustomAttributeHandle requiresUnsafe,
                          AttributeDescription.RequiresUnsafeAttribute);

                    bool hasRequiresUnsafeAttribute = !requiresUnsafe.IsNil;
                    _lazyRequiresUnsafe = (byte)ComputeRequiresUnsafe(hasRequiresUnsafeAttribute).ToThreeState();

                    ImmutableInterlocked.InterlockedInitialize(ref _lazyCustomAttributes, attributes);
                }
                else
                {
                    containingPEModuleSymbol.LoadCustomAttributes(_handle, ref _lazyCustomAttributes);
                }
            }
            return _lazyCustomAttributes;
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            return GetAttributes();
        }

        /// <summary>
        /// Intended behavior: this event, E, explicitly implements an interface event, IE, 
        /// if E.add explicitly implements IE.add and E.remove explicitly implements IE.remove.
        /// </summary>
        public override ImmutableArray<EventSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                if (_addMethod.ExplicitInterfaceImplementations.Length == 0 &&
                    _removeMethod.ExplicitInterfaceImplementations.Length == 0)
                {
                    return ImmutableArray<EventSymbol>.Empty;
                }

                var implementedEvents = PEPropertyOrEventHelpers.GetEventsForExplicitlyImplementedAccessor(_addMethod);

                if (implementedEvents.Count != 0)
                {
                    implementedEvents.IntersectWith(PEPropertyOrEventHelpers.GetEventsForExplicitlyImplementedAccessor(_removeMethod));
                }

                var builder = ArrayBuilder<EventSymbol>.GetInstance();

                foreach (var @event in implementedEvents)
                {
                    builder.Add(@event);
                }

                return builder.ToImmutableAndFree();
            }
        }

        internal override bool MustCallMethodsDirectly
        {
            get { return (_flags & Flags.CallMethodsDirectly) != 0; }
        }

        private static bool DoSignaturesMatch(
            PEModuleSymbol moduleSymbol,
            TypeSymbol eventType,
            PEMethodSymbol addMethod,
            PEMethodSymbol removeMethod)
        {
            return
                (eventType.IsDelegateType() || eventType.IsErrorType()) &&
                DoesSignatureMatch(moduleSymbol, eventType, addMethod) &&
                DoesSignatureMatch(moduleSymbol, eventType, removeMethod) &&
                DoModifiersMatch(addMethod, removeMethod);
        }

        private static bool DoModifiersMatch(PEMethodSymbol addMethod, PEMethodSymbol removeMethod)
        {
            // CONSIDER: unlike for properties, a non-bogus event can have one abstract accessor
            // and one sealed accessor.  Since the event is not bogus, the abstract accessor cannot
            // be overridden separately.  Consequently, the type cannot be extended.

            return
                (addMethod.IsExtern == removeMethod.IsExtern) &&
                // (addMethod.IsAbstract == removeMethod.IsAbstract) && // NOTE: Dev10 accepts one abstract accessor (same as for events)
                // (addMethod.IsSealed == removeMethod.IsSealed) && // NOTE: Dev10 accepts one sealed accessor (for events, not for properties)
                // (addMethod.IsOverride == removeMethod.IsOverride) && // NOTE: Dev10 accepts one override accessor (for events, not for properties)
                (addMethod.IsStatic == removeMethod.IsStatic);
        }

        private static bool DoesSignatureMatch(
            PEModuleSymbol moduleSymbol,
            TypeSymbol eventType,
            PEMethodSymbol method)
        {
            // CONSIDER: It would be nice if we could reuse this signature information in the PEMethodSymbol.
            var metadataDecoder = new MetadataDecoder(moduleSymbol, method);
            SignatureHeader signatureHeader;
            BadImageFormatException? mrEx;
            var methodParams = metadataDecoder.GetSignatureForMethod(method.Handle, out signatureHeader, out mrEx, setParamHandles: false);

            if (mrEx != null)
            {
                return false;
            }

            return metadataDecoder.DoesSignatureMatchEvent(eventType, methodParams);
        }

        public override string GetDocumentationCommentXml(CultureInfo? preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return PEDocumentationCommentUtils.GetDocumentationComment(this, _containingType.ContainingPEModule, preferredCulture, cancellationToken, ref _lazyDocComment);
        }

        internal override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            AssemblySymbol primaryDependency = PrimaryDependency;

            if (!_lazyCachedUseSiteInfo.IsInitialized)
            {
                UseSiteInfo<AssemblySymbol> result = new UseSiteInfo<AssemblySymbol>(primaryDependency);
                CalculateUseSiteDiagnostic(ref result);
                deriveCompilerFeatureRequiredUseSiteInfo(ref result);
                _lazyCachedUseSiteInfo.Initialize(primaryDependency, result);
            }

            return _lazyCachedUseSiteInfo.ToUseSiteInfo(primaryDependency);

            void deriveCompilerFeatureRequiredUseSiteInfo(ref UseSiteInfo<AssemblySymbol> result)
            {
                var containingType = (PENamedTypeSymbol)ContainingType;
                PEModuleSymbol containingPEModule = _containingType.ContainingPEModule;
                var diag = PEUtilities.DeriveCompilerFeatureRequiredAttributeDiagnostic(
                    this,
                    containingPEModule,
                    Handle,
                    allowedFeatures: CompilerFeatureRequiredFeatures.None,
                    new MetadataDecoder(containingPEModule, containingType));

                diag ??= containingType.GetCompilerFeatureRequiredDiagnostic();

                if (diag != null)
                {
                    result = new UseSiteInfo<AssemblySymbol>(diag);
                }
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                ObsoleteAttributeHelpers.InitializeObsoleteDataFromMetadata(ref _lazyObsoleteAttributeData, _handle, (PEModuleSymbol)(this.ContainingModule), ignoreByRefLikeMarker: false, ignoreRequiredMemberMarker: false);
                return _lazyObsoleteAttributeData;
            }
        }

        private bool RequiresUnsafe
        {
            get
            {
                var requiresUnsafeState = (ThreeState)Volatile.Read(ref _lazyRequiresUnsafe);
                if (!requiresUnsafeState.HasValue())
                {
                    var containingPEModuleSymbol = (PEModuleSymbol)this.ContainingModule;
                    bool hasRequiresUnsafeAttribute = containingPEModuleSymbol.Module.HasAttribute(_handle, AttributeDescription.RequiresUnsafeAttribute);
                    bool requiresUnsafe = ComputeRequiresUnsafe(hasRequiresUnsafeAttribute);
                    _lazyRequiresUnsafe = (byte)requiresUnsafe.ToThreeState();
                    return requiresUnsafe;
                }

                return requiresUnsafeState.Value();
            }
        }

        private bool ComputeRequiresUnsafe(bool hasRequiresUnsafeAttribute)
        {
            return ContainingModule.UseUpdatedMemorySafetyRules
                ? hasRequiresUnsafeAttribute
                // This might be expensive, so we cache it in flags.
                : Type.ContainsPointerOrFunctionPointer();
        }

        internal override CallerUnsafeMode CallerUnsafeMode
        {
            get
            {
                if (!RequiresUnsafe)
                {
                    return CallerUnsafeMode.None;
                }

                return ContainingModule.UseUpdatedMemorySafetyRules
                    ? CallerUnsafeMode.Explicit
                    : CallerUnsafeMode.Implicit;
            }
        }

        internal sealed override CSharpCompilation? DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}
