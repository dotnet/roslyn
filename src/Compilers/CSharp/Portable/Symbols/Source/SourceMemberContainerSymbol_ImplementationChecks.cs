// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class SourceMemberContainerTypeSymbol
    {
        /// <summary>
        /// In some circumstances (e.g. implicit implementation of an interface method by a non-virtual method in a 
        /// base type from another assembly) it is necessary for the compiler to generate explicit implementations for
        /// some interface methods.  They don't go in the symbol table, but if we are emitting, then we should
        /// generate code for them.
        /// </summary>
        internal ImmutableArray<SynthesizedExplicitImplementationForwardingMethod> GetSynthesizedExplicitImplementations(
            CancellationToken cancellationToken)
        {
            if (_lazySynthesizedExplicitImplementations.IsDefault)
            {
                var diagnostics = DiagnosticBag.GetInstance();
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    CheckMembersAgainstBaseType(diagnostics, cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();
                    CheckAbstractClassImplementations(diagnostics);

                    cancellationToken.ThrowIfCancellationRequested();
                    CheckInterfaceUnification(diagnostics);

                    if (this.IsInterface)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        this.CheckInterfaceVarianceSafety(diagnostics);
                    }

                    if (ImmutableInterlocked.InterlockedCompareExchange(
                            ref _lazySynthesizedExplicitImplementations,
                            ComputeInterfaceImplementations(diagnostics, cancellationToken),
                            default(ImmutableArray<SynthesizedExplicitImplementationForwardingMethod>)).IsDefault)
                    {
                        // Do not cancel from this point on.  We've assigned the member, so we must add
                        // the diagnostics.
                        AddDeclarationDiagnostics(diagnostics);

                        state.NotePartComplete(CompletionPart.SynthesizedExplicitImplementations);
                    }
                }
                finally
                {
                    diagnostics.Free();
                }
            }

            return _lazySynthesizedExplicitImplementations;
        }

        private void CheckAbstractClassImplementations(DiagnosticBag diagnostics)
        {
            NamedTypeSymbol baseType = this.BaseTypeNoUseSiteDiagnostics;

            if (this.IsAbstract || (object)baseType == null || !baseType.IsAbstract)
            {
                return;
            }

            // CONSIDER: We know that no-one will ask for NotOverriddenAbstractMembers again
            // (since this class is concrete), so we could just call the construction method
            // directly to avoid storing the result.
            foreach (var abstractMember in this.AbstractMembers)
            {
                // Dev10 reports failure to implement properties/events in terms of the accessors
                if (abstractMember.Kind == SymbolKind.Method)
                {
                    diagnostics.Add(ErrorCode.ERR_UnimplementedAbstractMethod, this.Locations[0], this, abstractMember);
                }
            }
        }

        private ImmutableArray<SynthesizedExplicitImplementationForwardingMethod> ComputeInterfaceImplementations(
            DiagnosticBag diagnostics,
            CancellationToken cancellationToken)
        {
            if (this.IsInterface)
            {
                return ImmutableArray<SynthesizedExplicitImplementationForwardingMethod>.Empty;
            }

            var synthesizedImplementations = ArrayBuilder<SynthesizedExplicitImplementationForwardingMethod>.GetInstance();

            // NOTE: We can't iterator over this collection directly, since it is not ordered.  Instead we 
            // iterate over AllInterfaces and filter out the interfaces that are not in this set.  This is 
            // preferable to doing the DFS ourselves because both AllInterfaces and 
            // InterfacesAndTheirBaseInterfaces are cached and used in multiple places.
            ImmutableHashSet<NamedTypeSymbol> interfacesAndTheirBases = this.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics;

            foreach (var @interface in this.AllInterfacesNoUseSiteDiagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!interfacesAndTheirBases.Contains(@interface))
                {
                    continue;
                }

                bool? hasImportedBaseTypeDeclaringInterface = null;

                foreach (var interfaceMember in @interface.GetMembersUnordered())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Only require implementations for members that can be implemented in C#.
                    SymbolKind interfaceMemberKind = interfaceMember.Kind;
                    switch (interfaceMemberKind)
                    {
                        case SymbolKind.Method:
                        case SymbolKind.Property:
                        case SymbolKind.Event:
                            if (interfaceMember.IsStatic)
                            {
                                continue;
                            }
                            break;
                        default:
                            continue;
                    }

                    var implementingMemberAndDiagnostics = this.FindImplementationForInterfaceMemberWithDiagnostics(interfaceMember);
                    var implementingMember = implementingMemberAndDiagnostics.Symbol;
                    var synthesizedImplementation = this.SynthesizeInterfaceMemberImplementation(implementingMemberAndDiagnostics, interfaceMember);

                    bool wasImplementingMemberFound = (object)implementingMember != null;

                    if ((object)synthesizedImplementation != null)
                    {
                        synthesizedImplementations.Add(synthesizedImplementation);
                    }

                    if (wasImplementingMemberFound && interfaceMemberKind == SymbolKind.Event)
                    {
                        // NOTE: unlike dev11, we're including a related location for the implementing type, because
                        // otherwise the only error location will be in the containing type of the implementing event
                        // (i.e. no indication of which type's interface list is actually problematic).

                        EventSymbol interfaceEvent = (EventSymbol)interfaceMember;
                        EventSymbol implementingEvent = (EventSymbol)implementingMember;
                        EventSymbol maybeWinRTEvent;
                        EventSymbol maybeRegularEvent;

                        if (interfaceEvent.IsWindowsRuntimeEvent)
                        {
                            maybeWinRTEvent = interfaceEvent; // Definitely WinRT.
                            maybeRegularEvent = implementingEvent; // Maybe regular.
                        }
                        else
                        {
                            maybeWinRTEvent = implementingEvent; // Maybe WinRT.
                            maybeRegularEvent = interfaceEvent; // Definitely regular.
                        }

                        if (interfaceEvent.IsWindowsRuntimeEvent != implementingEvent.IsWindowsRuntimeEvent)
                        {
                            // At this point (and not before), we know that maybeWinRTEvent is definitely a WinRT event and maybeRegularEvent is definitely a regular event.
                            var args = new object[] { implementingEvent, interfaceEvent, maybeWinRTEvent, maybeRegularEvent };
                            var info = new CSDiagnosticInfo(ErrorCode.ERR_MixingWinRTEventWithRegular, args, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<Location>(this.Locations[0]));
                            diagnostics.Add(info, implementingEvent.Locations[0]);
                        }
                    }

                    // Dev10: If a whole property is missing, report the property.  If the property is present, but an accessor
                    // is missing, report just the accessor.

                    var associatedPropertyOrEvent = interfaceMemberKind == SymbolKind.Method ? ((MethodSymbol)interfaceMember).AssociatedSymbol : null;
                    if ((object)associatedPropertyOrEvent == null ||
                        ReportAccessorOfInterfacePropertyOrEvent(associatedPropertyOrEvent) ||
                        (wasImplementingMemberFound && !implementingMember.IsAccessor()))
                    {
                        //we're here because 
                        //(a) the interface member is not an accessor, or 
                        //(b) the interface member is an accessor of an interesting (see ReportAccessorOfInterfacePropertyOrEvent) property or event, or
                        //(c) the implementing member exists and is not an accessor.

                        if (implementingMemberAndDiagnostics.Diagnostics.Any())
                        {
                            diagnostics.AddRange(implementingMemberAndDiagnostics.Diagnostics);
                        }
                        else if (!wasImplementingMemberFound)
                        {
                            // NOTE: An alternative approach would be to keep track of this while searching for the implementing member.
                            // In some cases, we might even be able to stop looking and just accept that a base type has things covered
                            // (though we'd have to be careful about losing diagnostics and we might produce fewer bridge methods).
                            // However, this approach has the advantage that there is no cost unless we encounter a base type that
                            // claims to implement an interface, but we can't figure out how (i.e. free in nearly all cases).
                            hasImportedBaseTypeDeclaringInterface = hasImportedBaseTypeDeclaringInterface ?? HasImportedBaseTypeDeclaringInterface(@interface);

                            // If a base type from metadata declares that it implements the interface, we'll just trust it.
                            // (See fFoundImport in SymbolPreparer::CheckInterfaceMethodImplementation.)
                            if (!hasImportedBaseTypeDeclaringInterface.GetValueOrDefault())
                            {
                                // CONSIDER: Dev10 does not emit this diagnostic for interface properties if the
                                // derived type attempts to implement an accessor directly as a method.

                                // Suppress for bogus properties and events and for indexed properties.
                                if (!interfaceMember.MustCallMethodsDirectly() && !interfaceMember.IsIndexedProperty())
                                {
                                    DiagnosticInfo useSiteDiagnostic = interfaceMember.GetUseSiteDiagnostic();

                                    if (useSiteDiagnostic != null && useSiteDiagnostic.DefaultSeverity == DiagnosticSeverity.Error)
                                    {
                                        diagnostics.Add(useSiteDiagnostic, GetImplementsLocation(@interface));
                                    }
                                    else
                                    {
                                        diagnostics.Add(ErrorCode.ERR_UnimplementedInterfaceMember, GetImplementsLocation(@interface) ?? this.Locations[0], this, interfaceMember);
                                    }
                                }
                            }
                        }
                        else if (interfaceMemberKind == SymbolKind.Method)
                        {
                            // Don't report use site errors on properties - we'll report them on each of their accessors.

                            // Don't report use site errors for implementations in other types unless 
                            // a synthesized implementation is needed that invokes the base method.
                            // We can do so only if there are no use-site errors.

                            if ((object)synthesizedImplementation != null || implementingMember.ContainingType == this)
                            {
                                DiagnosticInfo useSiteDiagnostic = interfaceMember.GetUseSiteDiagnostic();
                                // CAVEAT: don't report ERR_ByRefReturnUnsupported since by-ref return types are 
                                // specifically allowed for the purposes of interface implementation (for C++ interop).
                                // However, if there's a reference to the interface member in source, then we do want
                                // to produce a use site error.
                                if (useSiteDiagnostic != null && (ErrorCode)useSiteDiagnostic.Code != ErrorCode.ERR_ByRefReturnUnsupported)
                                {
                                    // Don't report a use site error with a location in another compilation.  For example,
                                    // if the error is that a base type in another assembly implemented an interface member
                                    // on our behalf and the use site error is that the current assembly does not reference
                                    // some required assembly, then we want to report the error in the current assembly -
                                    // not in the implementing assembly.
                                    Location location = implementingMember.IsFromCompilation(this.DeclaringCompilation)
                                        ? implementingMember.Locations[0]
                                        : this.Locations[0];
                                    Symbol.ReportUseSiteDiagnostic(useSiteDiagnostic, diagnostics, location);
                                }
                            }
                        }
                    }
                }
            }

            return synthesizedImplementations.ToImmutableAndFree();
        }

        protected abstract Location GetCorrespondingBaseListLocation(NamedTypeSymbol @base);

        internal Location GetImplementsLocation(NamedTypeSymbol implementedInterface)
        {
            // We ideally want to identify the interface location in the base list with an exact match but
            // will fall back and use the first derived interface if exact interface is not present.
            // this is the similar logic as the VB implementation.
            Debug.Assert(this.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Contains(implementedInterface));
            HashSet<DiagnosticInfo> unuseddiagnostics = null;

            NamedTypeSymbol directInterface = null;
            foreach (var iface in this.InterfacesNoUseSiteDiagnostics())
            {
                if (iface == implementedInterface)
                {
                    directInterface = iface;
                    break;
                }
                else if ((object)directInterface == null && iface.ImplementsInterface(implementedInterface, ref unuseddiagnostics))
                {
                    directInterface = iface;
                }
            }

            Debug.Assert((object)directInterface != null);
            return GetCorrespondingBaseListLocation(directInterface);
        }

        /// <summary>
        /// It's not interesting to report diagnostics on implementation of interface accessors
        /// if the corresponding events or properties are not implemented (i.e. we want to suppress
        /// cascading diagnostics).
        /// Caveat: Indexed property accessors are always interesting.
        /// Caveat: It's also uninteresting if a WinRT event is implemented by a non-WinRT event,
        /// or vice versa.
        /// </summary>
        private bool ReportAccessorOfInterfacePropertyOrEvent(Symbol interfacePropertyOrEvent)
        {
            Debug.Assert((object)interfacePropertyOrEvent != null);

            // Accessors of indexed properties are always interesting.
            if (interfacePropertyOrEvent.IsIndexedProperty())
            {
                return true;
            }

            Symbol implementingPropertyOrEvent = this.FindImplementationForInterfaceMemberWithDiagnostics(interfacePropertyOrEvent).Symbol;

            // If the property or event wasn't implemented, then we'd prefer to report diagnostics about that.
            if ((object)implementingPropertyOrEvent == null)
            {
                return false;
            }

            // If the property or event was an event and was implemented, but the WinRT-ness didn't agree,
            // then we'd prefer to report diagnostics about that.
            if (interfacePropertyOrEvent.Kind == SymbolKind.Event && implementingPropertyOrEvent.Kind == SymbolKind.Event &&
                ((EventSymbol)interfacePropertyOrEvent).IsWindowsRuntimeEvent != ((EventSymbol)implementingPropertyOrEvent).IsWindowsRuntimeEvent)
            {
                return false;
            }

            return true;
        }

        private bool HasImportedBaseTypeDeclaringInterface(NamedTypeSymbol @interface)
        {
            CSharpCompilation compilation = this.DeclaringCompilation;
            for (NamedTypeSymbol currType = this; (object)currType != null; currType = currType.BaseTypeNoUseSiteDiagnostics)
            {
                if (!currType.OriginalDefinition.IsFromCompilation(compilation) && currType.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Contains(@interface))
                {
                    return true;
                }
            }
            return false;
        }

        private void CheckMembersAgainstBaseType(
            DiagnosticBag diagnostics,
            CancellationToken cancellationToken)
        {
            switch (this.TypeKind)
            {
                // These checks don't make sense for enums and delegates:
                case TypeKind.Enum:
                case TypeKind.Delegate:
                    return;

                case TypeKind.Class:
                case TypeKind.Struct:
                case TypeKind.Interface:
                case TypeKind.Submission: // we have to check that "override" is not used 
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(this.TypeKind);
            }

            foreach (var member in this.GetMembersUnordered())
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool suppressAccessors;
                switch (member.Kind)
                {
                    case SymbolKind.Method:
                        var method = (MethodSymbol)member;
                        if (MethodSymbol.CanOverrideOrHide(method.MethodKind) && !method.IsAccessor())
                        {
                            if (member.IsOverride)
                            {
                                CheckOverrideMember(method, method.OverriddenOrHiddenMembers, diagnostics, out suppressAccessors);
                            }
                            else
                            {
                                var sourceMethod = method as SourceMethodSymbol;
                                if ((object)sourceMethod != null) // skip submission initializer
                                {
                                    var isNew = sourceMethod.IsNew;
                                    CheckNonOverrideMember(method, isNew, method.OverriddenOrHiddenMembers, diagnostics, out suppressAccessors);
                                }
                            }
                        }
                        else if (method.MethodKind == MethodKind.Destructor)
                        {
                            // NOTE: Normal finalize methods CanOverrideOrHide and will go through the normal code path.

                            // First is fine, since there should only be one, since there are no parameters.
                            MethodSymbol overridden = method.GetFirstRuntimeOverriddenMethodIgnoringNewSlot(ignoreInterfaceImplementationChanges: true);

                            // NOTE: Dev11 doesn't expose symbols, so it can treat destructors as override and let them go through the normal
                            // checks.  Roslyn can't, since the language says they are not virtual/override and that's what we need to expose
                            // in the symbol model.  Having said that, Dev11 doesn't seem to produce override errors other than this one
                            // (see SymbolPreparer::prepareOperator).
                            if ((object)overridden != null && overridden.IsMetadataFinal)
                            {
                                diagnostics.Add(ErrorCode.ERR_CantOverrideSealed, method.Locations[0], method, overridden);
                            }
                        }
                        break;
                    case SymbolKind.Property:
                        var property = (PropertySymbol)member;
                        var getMethod = property.GetMethod;
                        var setMethod = property.SetMethod;

                        // Handle the accessors here, instead of in the loop, so that we can ensure that
                        // they're checked *after* the corresponding property.
                        if (member.IsOverride)
                        {
                            CheckOverrideMember(property, property.OverriddenOrHiddenMembers, diagnostics, out suppressAccessors);

                            if (!suppressAccessors)
                            {
                                if ((object)getMethod != null)
                                {
                                    CheckOverrideMember(getMethod, getMethod.OverriddenOrHiddenMembers, diagnostics, out suppressAccessors);
                                }
                                if ((object)setMethod != null)
                                {
                                    CheckOverrideMember(setMethod, setMethod.OverriddenOrHiddenMembers, diagnostics, out suppressAccessors);
                                }
                            }
                        }
                        else
                        {
                            var isNewProperty = ((SourcePropertySymbol)property).IsNew;
                            CheckNonOverrideMember(property, isNewProperty, property.OverriddenOrHiddenMembers, diagnostics, out suppressAccessors);

                            if (!suppressAccessors)
                            {
                                if ((object)getMethod != null)
                                {
                                    CheckNonOverrideMember(getMethod, isNewProperty, getMethod.OverriddenOrHiddenMembers, diagnostics, out suppressAccessors);
                                }
                                if ((object)setMethod != null)
                                {
                                    CheckNonOverrideMember(setMethod, isNewProperty, setMethod.OverriddenOrHiddenMembers, diagnostics, out suppressAccessors);
                                }
                            }
                        }
                        break;
                    case SymbolKind.Event:
                        var @event = (EventSymbol)member;
                        var addMethod = @event.AddMethod;
                        var removeMethod = @event.RemoveMethod;

                        // Handle the accessors here, instead of in the loop, so that we can ensure that
                        // they're checked *after* the corresponding event.
                        if (member.IsOverride)
                        {
                            CheckOverrideMember(@event, @event.OverriddenOrHiddenMembers, diagnostics, out suppressAccessors);

                            if (!suppressAccessors)
                            {
                                if ((object)addMethod != null)
                                {
                                    CheckOverrideMember(addMethod, addMethod.OverriddenOrHiddenMembers, diagnostics, out suppressAccessors);
                                }
                                if ((object)removeMethod != null)
                                {
                                    CheckOverrideMember(removeMethod, removeMethod.OverriddenOrHiddenMembers, diagnostics, out suppressAccessors);
                                }
                            }
                        }
                        else
                        {
                            var isNewEvent = ((SourceEventSymbol)@event).IsNew;
                            CheckNonOverrideMember(@event, isNewEvent, @event.OverriddenOrHiddenMembers, diagnostics, out suppressAccessors);

                            if (!suppressAccessors)
                            {
                                if ((object)addMethod != null)
                                {
                                    CheckNonOverrideMember(addMethod, isNewEvent, addMethod.OverriddenOrHiddenMembers, diagnostics, out suppressAccessors);
                                }
                                if ((object)removeMethod != null)
                                {
                                    CheckNonOverrideMember(removeMethod, isNewEvent, removeMethod.OverriddenOrHiddenMembers, diagnostics, out suppressAccessors);
                                }
                            }
                        }
                        break;
                    case SymbolKind.Field:
                        var sourceField = member as SourceFieldSymbol;
                        var isNewField = (object)sourceField != null && sourceField.IsNew;

                        // We don't want to report diagnostics for field-like event backing fields (redundant),
                        // but that shouldn't be an issue since they shouldn't be in the member list.
                        Debug.Assert((object)sourceField == null || (object)sourceField.AssociatedSymbol == null ||
                            sourceField.AssociatedSymbol.Kind != SymbolKind.Event);

                        CheckNewModifier(member, isNewField, diagnostics);
                        break;
                    case SymbolKind.NamedType:
                        CheckNewModifier(member, ((SourceMemberContainerTypeSymbol)member).IsNew, diagnostics);
                        break;
                }
            }
        }

        private void CheckNewModifier(Symbol symbol, bool isNew, DiagnosticBag diagnostics)
        {
            // for error cases
            if ((object)this.BaseTypeNoUseSiteDiagnostics == null)
            {
                return;
            }

            // Do not give warnings about missing 'new' modifier for implicitly declared members,
            // e.g. backing fields for auto-properties
            if (symbol.IsImplicitlyDeclared)
            {
                return;
            }

            int symbolArity = symbol.GetMemberArity();
            Location symbolLocation = symbol.Locations.FirstOrDefault();
            bool unused = false;

            NamedTypeSymbol currType = this.BaseTypeNoUseSiteDiagnostics;
            while ((object)currType != null)
            {
                foreach (var hiddenMember in currType.GetMembers(symbol.Name))
                {
                    if (hiddenMember.Kind == SymbolKind.Method && !((MethodSymbol)hiddenMember).CanBeHiddenByMemberKind(symbol.Kind))
                    {
                        continue;
                    }

                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    bool isAccessible = AccessCheck.IsSymbolAccessible(hiddenMember, this, ref useSiteDiagnostics);
                    diagnostics.Add(symbolLocation, useSiteDiagnostics);

                    if (isAccessible && hiddenMember.GetMemberArity() == symbolArity)
                    {
                        if (!isNew)
                        {
                            diagnostics.Add(ErrorCode.WRN_NewRequired, symbolLocation, symbol, hiddenMember);
                        }

                        AddHidingAbstractDiagnostic(symbol, symbolLocation, hiddenMember, diagnostics, ref unused);

                        return;
                    }
                }

                currType = currType.BaseTypeNoUseSiteDiagnostics;
            }

            if (isNew)
            {
                diagnostics.Add(ErrorCode.WRN_NewNotRequired, symbolLocation, symbol);
            }
        }

        private static void CheckOverrideMember(Symbol overridingMember, OverriddenOrHiddenMembersResult overriddenOrHiddenMembers,
            DiagnosticBag diagnostics, out bool suppressAccessors)
        {
            Debug.Assert((object)overridingMember != null);
            Debug.Assert(overriddenOrHiddenMembers != null);

            suppressAccessors = false;

            var overridingMemberIsMethod = overridingMember.Kind == SymbolKind.Method;
            var overridingMemberIsProperty = overridingMember.Kind == SymbolKind.Property;
            var overridingMemberIsEvent = overridingMember.Kind == SymbolKind.Event;

            Debug.Assert(overridingMemberIsMethod ^ overridingMemberIsProperty ^ overridingMemberIsEvent);

            var overridingMemberLocation = overridingMember.Locations[0];

            var overriddenMembers = overriddenOrHiddenMembers.OverriddenMembers;
            Debug.Assert(!overriddenMembers.IsDefault);

            if (overriddenMembers.Length == 0)
            {
                var hiddenMembers = overriddenOrHiddenMembers.HiddenMembers;
                Debug.Assert(!hiddenMembers.IsDefault);

                if (hiddenMembers.Any())
                {
                    ErrorCode errorCode =
                        overridingMemberIsMethod ? ErrorCode.ERR_CantOverrideNonFunction :
                        overridingMemberIsProperty ? ErrorCode.ERR_CantOverrideNonProperty :
                        ErrorCode.ERR_CantOverrideNonEvent;

                    diagnostics.Add(errorCode, overridingMemberLocation, overridingMember, hiddenMembers[0]);
                }
                else
                {
                    Symbol associatedPropertyOrEvent = null;
                    if (overridingMemberIsMethod)
                    {
                        associatedPropertyOrEvent = ((MethodSymbol)overridingMember).AssociatedSymbol;
                    }

                    if ((object)associatedPropertyOrEvent == null)
                    {
                        diagnostics.Add(ErrorCode.ERR_OverrideNotExpected, overridingMemberLocation, overridingMember);
                    }
                    else if (associatedPropertyOrEvent.Kind == SymbolKind.Property) //no specific errors for event accessors
                    {
                        PropertySymbol associatedProperty = (PropertySymbol)associatedPropertyOrEvent;
                        PropertySymbol overriddenProperty = associatedProperty.OverriddenProperty;

                        if ((object)overriddenProperty == null)
                        {
                            //skip remaining checks
                        }
                        else if (associatedProperty.GetMethod == overridingMember && (object)overriddenProperty.GetMethod == null)
                        {
                            diagnostics.Add(ErrorCode.ERR_NoGetToOverride, overridingMemberLocation, overridingMember, overriddenProperty);
                        }
                        else if (associatedProperty.SetMethod == overridingMember && (object)overriddenProperty.SetMethod == null)
                        {
                            diagnostics.Add(ErrorCode.ERR_NoSetToOverride, overridingMemberLocation, overridingMember, overriddenProperty);
                        }
                        else
                        {
                            diagnostics.Add(ErrorCode.ERR_OverrideNotExpected, overridingMemberLocation, overridingMember);
                        }
                    }
                }
            }
            else
            {
                NamedTypeSymbol overridingType = overridingMember.ContainingType;
                if (overriddenMembers.Length > 1)
                {
                    diagnostics.Add(ErrorCode.ERR_AmbigOverride, overridingMemberLocation,
                        overriddenMembers[0].OriginalDefinition, overriddenMembers[1].OriginalDefinition, overridingType);
                    suppressAccessors = true;
                }
                else
                {
                    var overriddenMember = overriddenMembers[0];

                    //otherwise, it would have been excluded during lookup
                    HashSet<DiagnosticInfo> useSiteDiagnosticsNotUsed = null;
                    Debug.Assert(AccessCheck.IsSymbolAccessible(overriddenMember, overridingType, ref useSiteDiagnosticsNotUsed));

                    Debug.Assert(overriddenMember.Kind == overridingMember.Kind);

                    if (overriddenMember.MustCallMethodsDirectly())
                    {
                        diagnostics.Add(ErrorCode.ERR_CantOverrideBogusMethod, overridingMemberLocation, overridingMember, overriddenMember);
                        suppressAccessors = true;
                    }
                    else if (!overriddenMember.IsVirtual && !overriddenMember.IsAbstract && !overriddenMember.IsOverride &&
                        !(overridingMemberIsMethod && ((MethodSymbol)overriddenMember).MethodKind == MethodKind.Destructor)) //destructors are metadata virtual
                    {
                        // CONSIDER: To match Dev10, skip the error for properties, and don't suppressAccessors
                        diagnostics.Add(ErrorCode.ERR_CantOverrideNonVirtual, overridingMemberLocation, overridingMember, overriddenMember);
                        suppressAccessors = true;
                    }
                    else if (overriddenMember.IsSealed)
                    {
                        // CONSIDER: To match Dev10, skip the error for properties, and don't suppressAccessors
                        diagnostics.Add(ErrorCode.ERR_CantOverrideSealed, overridingMemberLocation, overridingMember, overriddenMember);
                        suppressAccessors = true;
                    }
                    else if (!overridingMember.IsPartialMethod() && !OverrideHasCorrectAccessibility(overriddenMember, overridingMember))
                    {
                        var accessibility = SyntaxFacts.GetText(overriddenMember.DeclaredAccessibility);
                        diagnostics.Add(ErrorCode.ERR_CantChangeAccessOnOverride, overridingMemberLocation, overridingMember, accessibility, overriddenMember);
                        suppressAccessors = true;
                    }
                    else
                    {
                        // As in dev11, we don't compare obsoleteness to the immediately-overridden member,
                        // but to the least-overridden member.
                        var leastOverriddenMember = overriddenMember.GetLeastOverriddenMember(overriddenMember.ContainingType);

                        overridingMember.ForceCompleteObsoleteAttribute();
                        leastOverriddenMember.ForceCompleteObsoleteAttribute();

                        Debug.Assert(overridingMember.ObsoleteState != ThreeState.Unknown);
                        Debug.Assert(leastOverriddenMember.ObsoleteState != ThreeState.Unknown);

                        bool overridingMemberIsObsolete = overridingMember.ObsoleteState == ThreeState.True;
                        bool leastOverriddenMemberIsObsolete = leastOverriddenMember.ObsoleteState == ThreeState.True;

                        if (overridingMemberIsObsolete != leastOverriddenMemberIsObsolete)
                        {
                            ErrorCode code = overridingMemberIsObsolete
                                ? ErrorCode.WRN_ObsoleteOverridingNonObsolete
                                : ErrorCode.WRN_NonObsoleteOverridingObsolete;

                            diagnostics.Add(code, overridingMemberLocation, overridingMember, leastOverriddenMember);
                        }
                        else if (overridingMemberIsProperty)
                        {
                            PropertySymbol overridingProperty = (PropertySymbol)overridingMember;
                            PropertySymbol overriddenProperty = (PropertySymbol)overriddenMember;

                            TypeSymbol overridingMemberType = overridingProperty.Type;
                            TypeSymbol overriddenMemberType = overriddenProperty.Type;

                            // Check for mismatched byref returns and return type. Ignore custom modifiers, because this diagnostic is based on the C# semantics.
                            if ((overridingProperty.RefKind != RefKind.None) != (overriddenProperty.RefKind != RefKind.None))
                            {
                                diagnostics.Add(ErrorCode.ERR_CantChangeRefReturnOnOverride, overridingMemberLocation, overridingMember, overriddenMember, overridingProperty.RefKind != RefKind.None ? "not " : "");
                                suppressAccessors = true; //we get really unhelpful errors from the accessor if the ref kind is mismatched
                            }
                            else if (!overridingMemberType.Equals(overriddenMemberType, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true))
                            {
                                diagnostics.Add(ErrorCode.ERR_CantChangeTypeOnOverride, overridingMemberLocation, overridingMember, overriddenMember, overriddenMemberType);
                                suppressAccessors = true; //we get really unhelpful errors from the accessor if the type is mismatched
                            }

                            // If the overriding property is sealed, then the overridden accessors cannot be inaccessible, since we
                            // have to override them to make them sealed in metadata.
                            // CONSIDER: It might be nice if this had its own error code(s) since it's an implementation restriction,
                            // rather than a language restriction as above.
                            if (overridingProperty.IsSealed)
                            {
                                MethodSymbol ownOrInheritedGetMethod = overridingProperty.GetOwnOrInheritedGetMethod();
                                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                                if (overridingProperty.GetMethod != ownOrInheritedGetMethod && !AccessCheck.IsSymbolAccessible(ownOrInheritedGetMethod, overridingType, ref useSiteDiagnostics))
                                {
                                    diagnostics.Add(ErrorCode.ERR_NoGetToOverride, overridingMemberLocation, overridingProperty, overriddenProperty);
                                }

                                MethodSymbol ownOrInheritedSetMethod = overridingProperty.GetOwnOrInheritedSetMethod();
                                if (overridingProperty.SetMethod != ownOrInheritedSetMethod && !AccessCheck.IsSymbolAccessible(ownOrInheritedSetMethod, overridingType, ref useSiteDiagnostics))
                                {
                                    diagnostics.Add(ErrorCode.ERR_NoSetToOverride, overridingMemberLocation, overridingProperty, overriddenProperty);
                                }

                                diagnostics.Add(overridingMemberLocation, useSiteDiagnostics);
                            }
                        }
                        else if (overridingMemberIsEvent)
                        {
                            EventSymbol overridingEvent = (EventSymbol)overridingMember;
                            EventSymbol overriddenEvent = (EventSymbol)overriddenMember;

                            TypeSymbol overridingMemberType = overridingEvent.Type;
                            TypeSymbol overriddenMemberType = overriddenEvent.Type;

                            // Ignore custom modifiers because this diagnostic is based on the C# semantics.
                            if (!overridingMemberType.Equals(overriddenMemberType, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true))
                            {
                                diagnostics.Add(ErrorCode.ERR_CantChangeTypeOnOverride, overridingMemberLocation, overridingMember, overriddenMember, overriddenMemberType);
                                suppressAccessors = true; //we get really unhelpful errors from the accessor if the type is mismatched
                            }
                        }
                        else
                        {
                            Debug.Assert(overridingMemberIsMethod);

                            var overridingMethod = (MethodSymbol)overridingMember;
                            var overriddenMethod = (MethodSymbol)overriddenMember;

                            // Check for mismatched byref returns and return type. Ignore custom modifiers, because this diagnostic is based on the C# semantics.
                            if ((overridingMethod.RefKind != RefKind.None) != (overriddenMethod.RefKind != RefKind.None))
                            {
                                diagnostics.Add(ErrorCode.ERR_CantChangeRefReturnOnOverride, overridingMemberLocation, overridingMember, overriddenMember, overridingMethod.RefKind != RefKind.None ? "not " : "");
                            }
                            else if (!MemberSignatureComparer.HaveSameReturnTypes(overridingMethod, overriddenMethod, considerCustomModifiers: false))
                            {
                                // Suppose we have a virtual base class method M<T>() that returns C<T>, and the overriding
                                // method M<V> returns void. The error should be "return type must be C<V>", not 
                                // "return type must be C<T>".

                                TypeSymbol returnType = overriddenMethod.IsGenericMethod ?
                                    overriddenMethod.Construct(overridingMethod.TypeParameters.Cast<TypeParameterSymbol, TypeSymbol>()).ReturnType :
                                    overriddenMethod.ReturnType;

                                // error CS0508: return type must be 'C<V>' to match overridden member 'M<T>()'
                                diagnostics.Add(ErrorCode.ERR_CantChangeReturnTypeOnOverride, overridingMemberLocation, overridingMember, overriddenMember, returnType);
                            }
                            else if (overriddenMethod.IsRuntimeFinalizer())
                            {
                                diagnostics.Add(ErrorCode.ERR_OverrideFinalizeDeprecated, overridingMemberLocation);
                            }
                        }

                        // NOTE: this error may be redundant (if an error has already been reported
                        // for the return type or parameter type in question), but the scenario is
                        // too rare to justify complicated checks.
                        DiagnosticInfo useSiteDiagnostic = overriddenMember.GetUseSiteDiagnostic();
                        if (useSiteDiagnostic != null)
                        {
                            suppressAccessors = ReportUseSiteDiagnostic(useSiteDiagnostic, diagnostics, overridingMember.Locations[0]);
                        }
                    }
                }
            }

            // From: SymbolPreparer.cpp
            // DevDiv Bugs 115384: Both out and ref parameters are implemented as references. In addition, out parameters are 
            // decorated with OutAttribute. In CLR when a signature is looked up in virtual dispatch, CLR does not distinguish
            // between these to parameter types. The choice is the last method in the vtable. Therefore we check and warn if 
            // there would potentially be a mismatch in CLRs and C#s choice of the overridden method. Unfortunately we have no 
            // way of communicating to CLR which method is the overridden one. We only run into this problem when the 
            // parameters are generic.
            var runtimeOverriddenMembers = overriddenOrHiddenMembers.RuntimeOverriddenMembers;
            Debug.Assert(!runtimeOverriddenMembers.IsDefault);
            if (runtimeOverriddenMembers.Length > 1 && overridingMember.Kind == SymbolKind.Method) // The runtime doesn't define overriding for properties or events.
            {
                // CONSIDER: Dev10 doesn't seem to report this warning for indexers.
                var ambiguousMethod = runtimeOverriddenMembers[0];
                diagnostics.Add(ErrorCode.WRN_MultipleRuntimeOverrideMatches, ambiguousMethod.Locations[0], ambiguousMethod, overridingMember);
                suppressAccessors = true;
            }
        }

        private static void CheckNonOverrideMember(
            Symbol hidingMember,
            bool hidingMemberIsNew,
            OverriddenOrHiddenMembersResult overriddenOrHiddenMembers,
            DiagnosticBag diagnostics, out bool suppressAccessors)
        {
            suppressAccessors = false;

            var hidingMemberLocation = hidingMember.Locations[0];

            Debug.Assert(overriddenOrHiddenMembers != null);
            Debug.Assert(!overriddenOrHiddenMembers.OverriddenMembers.Any()); //since hidingMethod.IsOverride is false
            Debug.Assert(!overriddenOrHiddenMembers.RuntimeOverriddenMembers.Any()); //since hidingMethod.IsOverride is false

            var hiddenMembers = overriddenOrHiddenMembers.HiddenMembers;
            Debug.Assert(!hiddenMembers.IsDefault);

            if (hiddenMembers.Length == 0)
            {
                if (hidingMemberIsNew && !hidingMember.IsAccessor())
                {
                    diagnostics.Add(ErrorCode.WRN_NewNotRequired, hidingMemberLocation, hidingMember);
                }
            }
            else
            {
                var diagnosticAdded = false;

                //for interfaces, we always report WRN_NewRequired
                //if we went into the loop, the pseudo-abstract nature of interfaces would throw off the other checks
                if (!hidingMember.ContainingType.IsInterface)
                {
                    foreach (var hiddenMember in hiddenMembers)
                    {
                        diagnosticAdded |= AddHidingAbstractDiagnostic(hidingMember, hidingMemberLocation, hiddenMember, diagnostics, ref suppressAccessors);

                        //can actually get both, so don't use else if
                        if (!hidingMemberIsNew && hiddenMember.Kind == hidingMember.Kind &&
                            !hidingMember.IsAccessor() &&
                            (hiddenMember.IsAbstract || hiddenMember.IsVirtual || hiddenMember.IsOverride))
                        {
                            diagnostics.Add(ErrorCode.WRN_NewOrOverrideExpected, hidingMemberLocation, hidingMember, hiddenMember);
                            diagnosticAdded = true;
                        }

                        if (diagnosticAdded)
                        {
                            break;
                        }
                    }
                }

                if (!hidingMemberIsNew && !diagnosticAdded && !hidingMember.IsAccessor() && !hidingMember.IsOperator())
                {
                    diagnostics.Add(ErrorCode.WRN_NewRequired, hidingMemberLocation, hidingMember, hiddenMembers[0]);
                }
            }
        }

        /// <summary>
        /// If necessary, report a diagnostic for a hidden abstract member.
        /// </summary>
        /// <returns>True if a diagnostic was reported.</returns>
        private static bool AddHidingAbstractDiagnostic(Symbol hidingMember, Location hidingMemberLocation, Symbol hiddenMember, DiagnosticBag diagnostics, ref bool suppressAccessors)
        {
            switch (hiddenMember.Kind)
            {
                case SymbolKind.Method:
                case SymbolKind.Property:
                case SymbolKind.Event:
                    break; // Can result in diagnostic
                default:
                    return false; // Cannot result in diagnostic
            }

            // If the hidden member isn't abstract, the diagnostic doesn't apply.
            // If the hiding member is in a non-abstract type, then suppress this cascading error.
            if (!hiddenMember.IsAbstract || !hidingMember.ContainingType.IsAbstract)
            {
                return false;
            }

            switch (hidingMember.DeclaredAccessibility)
            {
                case Accessibility.Internal:
                case Accessibility.Private:
                    break;
                case Accessibility.Public:
                case Accessibility.ProtectedOrInternal:
                case Accessibility.Protected:
                    {
                        // At this point we know we're going to report ERR_HidingAbstractMethod, we just have to
                        // figure out the substitutions.

                        switch (hidingMember.Kind)
                        {
                            case SymbolKind.Method:
                                var associatedPropertyOrEvent = ((MethodSymbol)hidingMember).AssociatedSymbol;
                                if ((object)associatedPropertyOrEvent != null)
                                {
                                    //Dev10 reports that the property/event is doing the hiding, rather than the method
                                    diagnostics.Add(ErrorCode.ERR_HidingAbstractMethod, associatedPropertyOrEvent.Locations[0], associatedPropertyOrEvent, hiddenMember);
                                    break;
                                }

                                goto default;
                            case SymbolKind.Property:
                            case SymbolKind.Event:
                                // NOTE: We used to let the accessors take care of this case, but then we weren't handling the case
                                // where a hiding and hidden properties did not have any accessors in common.

                                // CONSIDER: Dev10 actually reports an error for each accessor of a hidden property/event, but that seems unnecessary.
                                suppressAccessors = true;

                                goto default;
                            default:
                                diagnostics.Add(ErrorCode.ERR_HidingAbstractMethod, hidingMemberLocation, hidingMember, hiddenMember);
                                break;
                        }

                        return true;
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(hidingMember.DeclaredAccessibility);
            }
            return false;
        }

        private static bool OverrideHasCorrectAccessibility(Symbol overridden, Symbol overriding)
        {
            // Check declared accessibility rather than effective accessibility since there's a different
            // check (CS0560) that determines whether the containing types have compatible accessibility.
            if (!overriding.ContainingAssembly.HasInternalAccessTo(overridden.ContainingAssembly) &&
                overridden.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
            {
                return overriding.DeclaredAccessibility == Accessibility.Protected;
            }
            else
            {
                return overridden.DeclaredAccessibility == overriding.DeclaredAccessibility;
            }
        }

        /// <summary>
        /// It is invalid for a type to directly (vs through a base class) implement two interfaces that
        /// unify (i.e. are the same for some substitution of type parameters).
        /// </summary>
        /// <remarks>
        /// CONSIDER: check this while building up InterfacesAndTheirBaseInterfaces (only in the SourceNamedTypeSymbol case).
        /// </remarks>
        private void CheckInterfaceUnification(DiagnosticBag diagnostics)
        {
            if (!this.IsGenericType)
            {
                return;
            }

            // CONSIDER: filtering the list to only include generic types would save iterations.

            int numInterfaces = this.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Count;

            if (numInterfaces < 2)
            {
                return;
            }

            // NOTE: a typical approach to finding duplicates in less than quadratic time
            // is to use a HashSet with an appropriate comparer.  Unfortunately, this approach
            // does not apply (at least, not straightforwardly), because CanUnifyWith is not
            // transitive and, thus, is not an equivalence relation.

            NamedTypeSymbol[] interfaces = this.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.ToArray();

            for (int i1 = 0; i1 < numInterfaces; i1++)
            {
                for (int i2 = i1 + 1; i2 < numInterfaces; i2++)
                {
                    NamedTypeSymbol interface1 = interfaces[i1];
                    NamedTypeSymbol interface2 = interfaces[i2];

                    // CanUnifyWith is the real check - the others just short-circuit
                    if (interface1.IsGenericType && interface2.IsGenericType &&
                        interface1.OriginalDefinition == interface2.OriginalDefinition &&
                        interface1.CanUnifyWith(interface2))
                    {
                        if (GetImplementsLocation(interface1).SourceSpan.Start > GetImplementsLocation(interface2).SourceSpan.Start)
                        {
                            // Mention interfaces in order of their appearance in the base list, for consistency.
                            var temp = interface1;
                            interface1 = interface2;
                            interface2 = temp;
                        }

                        diagnostics.Add(ErrorCode.ERR_UnifyingInterfaceInstantiations, this.Locations[0], this, interface1, interface2);
                    }
                }
            }
        }

        /// <summary>
        /// Though there is a method that C# considers to be an implementation of the interface method, that
        /// method may not be considered an implementation by the CLR.  In particular, implicit implementation
        /// methods that are non-virtual or that have different (usually fewer) custom modifiers than the
        /// interface method, will not be considered CLR overrides.  To address this problem, we either make
        /// them virtual (in metadata, not in C#), or we introduce an explicit interface implementation that
        /// delegates to the implicit implementation.
        /// </summary>
        /// <param name="implementingMemberAndDiagnostics">Returned from FindImplementationForInterfaceMemberWithDiagnostics.</param>
        /// <param name="interfaceMember">The interface method or property that is being implemented.</param>
        /// <returns>Synthesized implementation or null if not needed.</returns>
        private SynthesizedExplicitImplementationForwardingMethod SynthesizeInterfaceMemberImplementation(SymbolAndDiagnostics implementingMemberAndDiagnostics, Symbol interfaceMember)
        {
            foreach (Diagnostic diagnostic in implementingMemberAndDiagnostics.Diagnostics)
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                {
                    return null;
                }
            }

            Symbol implementingMember = implementingMemberAndDiagnostics.Symbol;

            //don't worry about properties or events - we'll catch them through their accessors
            if ((object)implementingMember == null || implementingMember.Kind != SymbolKind.Method)
            {
                return null;
            }

            MethodSymbol interfaceMethod = (MethodSymbol)interfaceMember;
            MethodSymbol implementingMethod = (MethodSymbol)implementingMember;

            //explicit implementations are always respected by the CLR
            if (implementingMethod.ExplicitInterfaceImplementations.Contains(interfaceMethod))
            {
                return null;
            }

            MethodSymbol implementingMethodOriginalDefinition = implementingMethod.OriginalDefinition;

            bool needSynthesizedImplementation = true;

            // If the implementing method is from a source file in the same module and the
            // override is correct from the runtime's perspective (esp the custom modifiers
            // match), then we can just twiddle the metadata virtual bit.  Otherwise, we need
            // to create an explicit implementation that delegates to the real implementation.
            if (MemberSignatureComparer.RuntimeImplicitImplementationComparer.Equals(implementingMethod, interfaceMethod) &&
                IsOverrideOfPossibleImplementationUnderRuntimeRules(implementingMethod, @interfaceMethod.ContainingType))
            {
                if (ReferenceEquals(this.ContainingModule, implementingMethodOriginalDefinition.ContainingModule))
                {
                    SourceMethodSymbol sourceImplementMethodOriginalDefinition = implementingMethodOriginalDefinition as SourceMethodSymbol;
                    if ((object)sourceImplementMethodOriginalDefinition != null)
                    {
                        sourceImplementMethodOriginalDefinition.EnsureMetadataVirtual();
                        needSynthesizedImplementation = false;
                    }
                }
                else if (implementingMethod.IsMetadataVirtual(ignoreInterfaceImplementationChanges: true))
                {
                    // If the signatures match and the implementation method is definitely virtual, then we're set.
                    needSynthesizedImplementation = false;
                }
            }

            if (!needSynthesizedImplementation)
            {
                return null;
            }

            return new SynthesizedExplicitImplementationForwardingMethod(interfaceMethod, implementingMethod, this);
        }

        /// <summary>
        /// The CLR will only look for an implementation of an interface method in a type that
        ///   1) declares that it implements that interface; or
        ///   2) is a base class of a type that declares that it implements the interface but not
        ///        a subtype of a class that declares that it implements the interface.
        ///        
        /// For example,
        /// 
        ///   interface I
        ///   class A
        ///   class B : A, I
        ///   class C : B
        ///   class D : C, I
        /// 
        /// Suppose the runtime is looking for D's implementation of a member of I.  It will look in 
        /// D because of (1), will not look in C, will look in B because of (1), and will look in A
        /// because of (2).
        /// 
        /// The key point is that it does not look in C, which C# *does*.
        /// </summary>
        private static bool IsPossibleImplementationUnderRuntimeRules(MethodSymbol implementingMethod, NamedTypeSymbol @interface)
        {
            NamedTypeSymbol type = implementingMethod.ContainingType;
            if (type.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Contains(@interface))
            {
                return true;
            }

            NamedTypeSymbol baseType = type.BaseTypeNoUseSiteDiagnostics;
            return (object)baseType == null || !baseType.AllInterfacesNoUseSiteDiagnostics.Contains(@interface);
        }

        /// <summary>
        /// If C# picks a different implementation than the CLR (see IsPossibleImplementationUnderClrRules), then we might
        /// still be okay, but dynamic dispatch might result in C#'s choice getting called anyway.
        /// </summary>
        /// <remarks>
        /// This is based on SymbolPreparer::IsCLRMethodImplSame in the native compiler.
        /// 
        /// ACASEY: What the native compiler actually does is compute the C# answer, compute the CLR answer,
        /// and then confirm that they override the same method.  What I've done here is check for the situations
        /// where the answers could disagree.  I believe the results will be equivalent.  If in doubt, a more conservative
        /// check would be implementingMethod.ContainingType.InterfacesAndTheirBaseInterfaces.Contains(@interface).
        /// </remarks>
        private static bool IsOverrideOfPossibleImplementationUnderRuntimeRules(MethodSymbol implementingMethod, NamedTypeSymbol @interface)
        {
            MethodSymbol curr = implementingMethod;
            while ((object)curr != null)
            {
                if (IsPossibleImplementationUnderRuntimeRules(curr, @interface))
                {
                    return true;
                }

                curr = curr.OverriddenMethod;
            }
            return false;
        }

        internal sealed override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            return CalculateInterfacesToEmit();
        }
    }
}
