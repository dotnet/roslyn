// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Roslyn.Utilities;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// The class to represent all events imported from a PE/module.
    /// </summary>
    internal sealed class PEEventSymbol : EventSymbol
    {
        private readonly string name;
        private readonly PENamedTypeSymbol containingType;
        private readonly EventHandle handle;
        private readonly TypeSymbol eventType;
        private readonly PEMethodSymbol addMethod;
        private readonly PEMethodSymbol removeMethod;
        private ImmutableArray<CSharpAttributeData> lazyCustomAttributes;
        private Tuple<CultureInfo, string> lazyDocComment;
        private DiagnosticInfo lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 

        private ObsoleteAttributeData lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized;

        // Distinct accessibility value to represent unset.
        private const int UnsetAccessibility = -1;
        private int lazyDeclaredAccessibility = UnsetAccessibility;

        private readonly Flags flags;
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
            EventHandle handle,
            PEMethodSymbol addMethod,
            PEMethodSymbol removeMethod)
        {
            Debug.Assert((object)moduleSymbol != null);
            Debug.Assert((object)containingType != null);
            Debug.Assert(!handle.IsNil);
            Debug.Assert((object)addMethod != null);
            Debug.Assert((object)removeMethod != null);

            this.addMethod = addMethod;
            this.removeMethod = removeMethod;
            this.handle = handle;
            this.containingType = containingType;

            EventAttributes mdFlags = 0;
            Handle eventType = default(Handle);

            try
            {
                var module = moduleSymbol.Module;
                module.GetEventDefPropsOrThrow(handle, out this.name, out mdFlags, out eventType);
            }
            catch (BadImageFormatException mrEx)
            {
                if ((object)this.name == null)
                {
                    this.name = string.Empty;
                }

                lazyUseSiteDiagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this);

                if (eventType.IsNil)
                {
                    this.eventType = new UnsupportedMetadataTypeSymbol(mrEx);
                }
            }

            if ((object)this.eventType == null)
            {
                var metadataDecoder = new MetadataDecoder(moduleSymbol, containingType);
                this.eventType = metadataDecoder.GetTypeOfToken(eventType);
            }

            // IsWindowsRuntimeEvent checks the signatures, so we just have to check the accessors.
            bool callMethodsDirectly = IsWindowsRuntimeEvent
                ? !DoModifiersMatch(this.addMethod, this.removeMethod)
                : !DoSignaturesMatch(moduleSymbol, this.eventType, this.addMethod, this.removeMethod);

            if (callMethodsDirectly)
            {
                flags |= Flags.CallMethodsDirectly;
            }
            else
            {
                this.addMethod.SetAssociatedEvent(this, MethodKind.EventAdd);
                this.removeMethod.SetAssociatedEvent(this, MethodKind.EventRemove);
            }

            if ((mdFlags & EventAttributes.SpecialName) != 0)
            {
                flags |= Flags.IsSpecialName;
            }

            if ((mdFlags & EventAttributes.RTSpecialName) != 0)
            {
                flags |= Flags.IsRuntimeSpecialName;
            }
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
                    addMethod.ReturnType == token &&
                    addMethod.ParameterCount == 1 &&
                    removeMethod.ParameterCount == 1 &&
                    removeMethod.Parameters[0].Type == token;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return this.containingType;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return this.containingType;
            }
        }

        public override string Name
        {
            get { return name; }
        }

        internal override bool HasSpecialName
        {
            get { return (flags & Flags.IsSpecialName) != 0; }
        }

        internal override bool HasRuntimeSpecialName
        {
            get { return (flags & Flags.IsRuntimeSpecialName) != 0; }
        }

        internal EventHandle Handle
        {
            get
            {
                return this.handle;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                if (this.lazyDeclaredAccessibility == UnsetAccessibility)
                {
                    Accessibility accessibility = PEPropertyOrEventHelpers.GetDeclaredAccessibilityFromAccessors(this.addMethod, this.removeMethod);
                    Interlocked.CompareExchange(ref this.lazyDeclaredAccessibility, (int)accessibility, UnsetAccessibility);
                }

                return (Accessibility)this.lazyDeclaredAccessibility;
            }
        }

        public override bool IsExtern
        {
            get
            {
                // Some accessor extern.
                return addMethod.IsExtern || removeMethod.IsExtern;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                // Some accessor abstract.
                return addMethod.IsAbstract || removeMethod.IsAbstract;
            }
        }

        public override bool IsSealed
        {
            get
            {
                // Some accessor sealed. (differs from properties)
                return addMethod.IsSealed || removeMethod.IsSealed;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                // Some accessor virtual (as long as another isn't override or abstract).
                return !IsOverride && !IsAbstract && (addMethod.IsVirtual || removeMethod.IsVirtual);
            }
        }

        public override bool IsOverride
        {
            get
            {
                // Some accessor override.
                return addMethod.IsOverride || removeMethod.IsOverride;
            }
        }

        public override bool IsStatic
        {
            get
            {
                // All accessors static.
                return addMethod.IsStatic && removeMethod.IsStatic;
            }
        }

        public override TypeSymbol Type
        {
            get { return this.eventType; }
        }

        public override MethodSymbol AddMethod
        {
            get { return this.addMethod; }
        }

        public override MethodSymbol RemoveMethod
        {
            get { return this.removeMethod; }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return containingType.ContainingPEModule.MetadataLocation.Cast<MetadataLocation, Location>();
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
            if (this.lazyCustomAttributes.IsDefault)
            {
                var containingPEModuleSymbol = (PEModuleSymbol)this.ContainingModule;
                containingPEModuleSymbol.LoadCustomAttributes(this.handle, ref this.lazyCustomAttributes);
            }
            return this.lazyCustomAttributes;
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
                if (this.addMethod.ExplicitInterfaceImplementations.Length == 0 &&
                    this.removeMethod.ExplicitInterfaceImplementations.Length == 0)
                {
                    return ImmutableArray<EventSymbol>.Empty;
                }

                var implementedEvents = PEPropertyOrEventHelpers.GetEventsForExplicitlyImplementedAccessor(this.addMethod);
                implementedEvents.IntersectWith(PEPropertyOrEventHelpers.GetEventsForExplicitlyImplementedAccessor(this.removeMethod));

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
            get { return (this.flags & Flags.CallMethodsDirectly) != 0; }
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
            byte callingConvention;
            BadImageFormatException mrEx;
            var methodParams = metadataDecoder.GetSignatureForMethod(method.Handle, out callingConvention, out mrEx, setParamHandles: false);

            if (mrEx != null)
            {
                return false;
            }

            return metadataDecoder.DoesSignatureMatchEvent(eventType, methodParams);
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return PEDocumentationCommentUtils.GetDocumentationComment(this, containingType.ContainingPEModule, preferredCulture, cancellationToken, ref lazyDocComment);
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (ReferenceEquals(lazyUseSiteDiagnostic, CSDiagnosticInfo.EmptyErrorInfo))
            {
                DiagnosticInfo result = null;
                CalculateUseSiteDiagnostic(ref result);
                lazyUseSiteDiagnostic = result;
            }

            return lazyUseSiteDiagnostic;
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                ObsoleteAttributeHelpers.InitializeObsoleteDataFromMetadata(ref lazyObsoleteAttributeData, this.handle, (PEModuleSymbol)(this.ContainingModule));
                return lazyObsoleteAttributeData;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}
