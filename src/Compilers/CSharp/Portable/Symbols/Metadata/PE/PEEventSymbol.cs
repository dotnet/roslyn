// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
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
        private readonly TypeSymbol _eventType;
        private readonly PEMethodSymbol _addMethod;
        private readonly PEMethodSymbol _removeMethod;
        private readonly PEFieldSymbol _associatedFieldOpt;
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;
        private Tuple<CultureInfo, string> _lazyDocComment;
        private DiagnosticInfo _lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 

        private ObsoleteAttributeData _lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized;

        // Distinct accessibility value to represent unset.
        private const int UnsetAccessibility = -1;
        private int _lazyDeclaredAccessibility = UnsetAccessibility;

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
            Debug.Assert((object)moduleSymbol != null);
            Debug.Assert((object)containingType != null);
            Debug.Assert(!handle.IsNil);
            Debug.Assert((object)addMethod != null);
            Debug.Assert((object)removeMethod != null);

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
                if ((object)_name == null)
                {
                    _name = string.Empty;
                }

                _lazyUseSiteDiagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this);

                if (eventType.IsNil)
                {
                    _eventType = new UnsupportedMetadataTypeSymbol(mrEx);
                }
            }

            TypeSymbol originalEventType = _eventType;
            if ((object)_eventType == null)
            {
                var metadataDecoder = new MetadataDecoder(moduleSymbol, containingType);
                originalEventType = metadataDecoder.GetTypeOfToken(eventType);

                const int targetSymbolCustomModifierCount = 0;
                _eventType = DynamicTypeDecoder.TransformType(originalEventType, targetSymbolCustomModifierCount, handle, moduleSymbol);
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

                PEFieldSymbol associatedField = GetAssociatedField(privateFieldNameToSymbols, isWindowsRuntimeEvent);
                if ((object)associatedField != null)
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
        private PEFieldSymbol GetAssociatedField(MultiDictionary<string, PEFieldSymbol> privateFieldNameToSymbols, bool isWindowsRuntimeEvent)
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
                    if (eventRegistrationTokenTable_T == candidateAssociatedFieldType.OriginalDefinition &&
                        _eventType == ((NamedTypeSymbol)candidateAssociatedFieldType).TypeArguments[0])
                    {
                        return candidateAssociatedField;
                    }
                }
                else
                {
                    if (candidateAssociatedFieldType == _eventType)
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
                    _addMethod.ReturnType == token &&
                    _addMethod.ParameterCount == 1 &&
                    _removeMethod.ParameterCount == 1 &&
                    _removeMethod.Parameters[0].Type == token;
            }
        }

        internal override FieldSymbol AssociatedField
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

        public override TypeSymbol Type
        {
            get { return _eventType; }
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
                containingPEModuleSymbol.LoadCustomAttributes(_handle, ref _lazyCustomAttributes);
            }
            return _lazyCustomAttributes;
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(ModuleCompilationState compilationState)
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
                implementedEvents.IntersectWith(PEPropertyOrEventHelpers.GetEventsForExplicitlyImplementedAccessor(_removeMethod));

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
            BadImageFormatException mrEx;
            var methodParams = metadataDecoder.GetSignatureForMethod(method.Handle, out signatureHeader, out mrEx, setParamHandles: false);

            if (mrEx != null)
            {
                return false;
            }

            return metadataDecoder.DoesSignatureMatchEvent(eventType, methodParams);
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return PEDocumentationCommentUtils.GetDocumentationComment(this, _containingType.ContainingPEModule, preferredCulture, cancellationToken, ref _lazyDocComment);
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (ReferenceEquals(_lazyUseSiteDiagnostic, CSDiagnosticInfo.EmptyErrorInfo))
            {
                DiagnosticInfo result = null;
                CalculateUseSiteDiagnostic(ref result);
                _lazyUseSiteDiagnostic = result;
            }

            return _lazyUseSiteDiagnostic;
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                ObsoleteAttributeHelpers.InitializeObsoleteDataFromMetadata(ref _lazyObsoleteAttributeData, _handle, (PEModuleSymbol)(this.ContainingModule));
                return _lazyObsoleteAttributeData;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}
