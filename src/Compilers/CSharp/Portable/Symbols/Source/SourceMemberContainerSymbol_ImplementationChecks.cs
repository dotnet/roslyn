// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal delegate void ReportMismatchInReturnType<TArg>(BindingDiagnosticBag bag, MethodSymbol overriddenMethod, MethodSymbol overridingMethod, bool topLevel, TArg arg);
    internal delegate void ReportMismatchInParameterType<TArg>(BindingDiagnosticBag bag, MethodSymbol overriddenMethod, MethodSymbol overridingMethod, ParameterSymbol parameter, bool topLevel, TArg arg);

    internal partial class SourceMemberContainerTypeSymbol
    {
        /// <summary>
        /// In some circumstances (e.g. implicit implementation of an interface method by a non-virtual method in a
        /// base type from another assembly) it is necessary for the compiler to generate explicit implementations for
        /// some interface methods.  They don't go in the symbol table, but if we are emitting, then we should
        /// generate code for them.
        /// </summary>
        internal SynthesizedExplicitImplementations GetSynthesizedExplicitImplementations(
            CancellationToken cancellationToken)
        {
            if (_lazySynthesizedExplicitImplementations is null)
            {
                var diagnostics = BindingDiagnosticBag.GetInstance();
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

                    if (Interlocked.CompareExchange(
                            ref _lazySynthesizedExplicitImplementations,
                            ComputeInterfaceImplementations(diagnostics, cancellationToken),
                            null) is null)
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

        internal sealed override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls()
        {
            SynthesizedExplicitImplementations synthesizedImplementations = GetSynthesizedExplicitImplementations(cancellationToken: default);

            foreach (var methodImpl in synthesizedImplementations.MethodImpls)
            {
                yield return methodImpl;
            }

            foreach (var forwardingMethod in synthesizedImplementations.ForwardingMethods)
            {
                yield return (forwardingMethod.ImplementingMethod, forwardingMethod.ExplicitInterfaceImplementations.Single());
            }
        }

        private void CheckAbstractClassImplementations(BindingDiagnosticBag diagnostics)
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
                if (abstractMember.Kind == SymbolKind.Method && abstractMember is not SynthesizedRecordOrdinaryMethod)
                {
                    diagnostics.Add(ErrorCode.ERR_UnimplementedAbstractMethod, this.GetFirstLocation(), this, abstractMember);
                }
            }
        }

        private SynthesizedExplicitImplementations ComputeInterfaceImplementations(
            BindingDiagnosticBag diagnostics,
            CancellationToken cancellationToken)
        {
            var forwardingMethods = ArrayBuilder<SynthesizedExplicitImplementationForwardingMethod>.GetInstance();
            var methodImpls = ArrayBuilder<(MethodSymbol Body, MethodSymbol Implemented)>.GetInstance();

            // NOTE: We can't iterate over this collection directly, since it is not ordered.  Instead we
            // iterate over AllInterfaces and filter out the interfaces that are not in this set.  This is
            // preferable to doing the DFS ourselves because both AllInterfaces and
            // InterfacesAndTheirBaseInterfaces are cached and used in multiple places.
            MultiDictionary<NamedTypeSymbol, NamedTypeSymbol> interfacesAndTheirBases = this.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics;

            foreach (var @interface in this.AllInterfacesNoUseSiteDiagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!interfacesAndTheirBases[@interface].Contains(@interface))
                {
                    continue;
                }

                HasBaseTypeDeclaringInterfaceResult? hasBaseClassDeclaringInterface = null;

                foreach (var interfaceMember in @interface.GetMembers())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Only require implementations for members that can be implemented in C#.
                    SymbolKind interfaceMemberKind = interfaceMember.Kind;
                    switch (interfaceMemberKind)
                    {
                        case SymbolKind.Method:
                        case SymbolKind.Property:
                        case SymbolKind.Event:
                            if (!interfaceMember.IsImplementableInterfaceMember())
                            {
                                continue;
                            }
                            break;
                        default:
                            continue;
                    }

                    SymbolAndDiagnostics implementingMemberAndDiagnostics;

                    if (this.IsInterface)
                    {
                        MultiDictionary<Symbol, Symbol>.ValueSet explicitImpl = this.GetExplicitImplementationForInterfaceMember(interfaceMember);

                        switch (explicitImpl.Count)
                        {
                            case 0:
                                continue; // There is no requirement to implement anything in an interface
                            case 1:
                                implementingMemberAndDiagnostics = new SymbolAndDiagnostics(explicitImpl.Single(), ReadOnlyBindingDiagnostic<AssemblySymbol>.Empty);
                                break;
                            default:
                                Diagnostic diag = new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_DuplicateExplicitImpl, interfaceMember), this.GetFirstLocation());
                                implementingMemberAndDiagnostics = new SymbolAndDiagnostics(null, new ReadOnlyBindingDiagnostic<AssemblySymbol>(ImmutableArray.Create(diag), default));
                                break;
                        }
                    }
                    else
                    {
                        implementingMemberAndDiagnostics = this.FindImplementationForInterfaceMemberInNonInterfaceWithDiagnostics(interfaceMember);
                    }

                    var implementingMember = implementingMemberAndDiagnostics.Symbol;
                    var synthesizedImplementation = this.SynthesizeInterfaceMemberImplementation(implementingMemberAndDiagnostics, interfaceMember);

                    bool wasImplementingMemberFound = (object)implementingMember != null;

                    if (synthesizedImplementation.ForwardingMethod is SynthesizedExplicitImplementationForwardingMethod forwardingMethod)
                    {
                        if (forwardingMethod.IsVararg)
                        {
                            diagnostics.Add(
                                ErrorCode.ERR_InterfaceImplementedImplicitlyByVariadic,
                                GetImplicitImplementationDiagnosticLocation(interfaceMember, this, implementingMember), implementingMember, interfaceMember, this);
                        }
                        else
                        {
                            forwardingMethods.Add(forwardingMethod);
                        }
                    }

                    if (synthesizedImplementation.MethodImpl is { } methodImpl)
                    {
                        Debug.Assert(methodImpl is { Body: not null, Implemented: not null });
                        methodImpls.Add(methodImpl);
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
                            var info = new CSDiagnosticInfo(ErrorCode.ERR_MixingWinRTEventWithRegular, args, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<Location>(this.GetFirstLocation()));
                            diagnostics.Add(info, implementingEvent.GetFirstLocation());
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
                        bool reportedAnError = false;
                        if (implementingMemberAndDiagnostics.Diagnostics.Diagnostics.Any())
                        {
                            diagnostics.AddRange(implementingMemberAndDiagnostics.Diagnostics);
                            reportedAnError = implementingMemberAndDiagnostics.Diagnostics.Diagnostics.Any(static d => d.Severity == DiagnosticSeverity.Error);
                        }

                        if (!reportedAnError)
                        {
                            if (!wasImplementingMemberFound ||
                                (!implementingMember.ContainingType.Equals(this, TypeCompareKind.ConsiderEverything) &&
                                implementingMember.GetExplicitInterfaceImplementations().Contains(interfaceMember, ExplicitInterfaceImplementationTargetMemberEqualityComparer.Instance)))
                            {
                                // NOTE: An alternative approach would be to keep track of this while searching for the implementing member.
                                // In some cases, we might even be able to stop looking and just accept that a base type has things covered
                                // (though we'd have to be careful about losing diagnostics and we might produce fewer bridge methods).
                                // However, this approach has the advantage that there is no cost unless we encounter a base type that
                                // claims to implement an interface, but we can't figure out how (i.e. free in nearly all cases).
                                hasBaseClassDeclaringInterface = hasBaseClassDeclaringInterface ?? HasBaseClassDeclaringInterface(@interface);

                                HasBaseTypeDeclaringInterfaceResult matchResult = hasBaseClassDeclaringInterface.GetValueOrDefault();

                                if (matchResult != HasBaseTypeDeclaringInterfaceResult.ExactMatch &&
                                    wasImplementingMemberFound && implementingMember.ContainingType.IsInterface)
                                {
                                    HasBaseInterfaceDeclaringInterface(implementingMember.ContainingType, @interface, ref matchResult);
                                }

                                // If a base type from metadata declares that it implements the interface, we'll just trust it.
                                // (See fFoundImport in SymbolPreparer::CheckInterfaceMethodImplementation.)
                                switch (matchResult)
                                {
                                    case HasBaseTypeDeclaringInterfaceResult.NoMatch:
                                        {
                                            // CONSIDER: Dev10 does not emit this diagnostic for interface properties if the
                                            // derived type attempts to implement an accessor directly as a method.

                                            // Suppress for bogus properties and events and for indexed properties.
                                            if (!interfaceMember.MustCallMethodsDirectly() && !interfaceMember.IsIndexedProperty())
                                            {
                                                DiagnosticInfo useSiteDiagnostic = interfaceMember.GetUseSiteInfo().DiagnosticInfo;

                                                if (useSiteDiagnostic != null && useSiteDiagnostic.DefaultSeverity == DiagnosticSeverity.Error)
                                                {
                                                    diagnostics.Add(useSiteDiagnostic, GetImplementsLocationOrFallback(@interface));
                                                }
                                                else
                                                {
                                                    diagnostics.Add(ErrorCode.ERR_UnimplementedInterfaceMember, GetImplementsLocationOrFallback(@interface), this, interfaceMember);
                                                }
                                            }
                                        }
                                        break;

                                    case HasBaseTypeDeclaringInterfaceResult.ExactMatch:
                                        break;

                                    case HasBaseTypeDeclaringInterfaceResult.IgnoringNullableMatch:
                                        diagnostics.Add(ErrorCode.WRN_NullabilityMismatchInInterfaceImplementedByBase, GetImplementsLocationOrFallback(@interface), this, interfaceMember);
                                        break;

                                    default:
                                        throw ExceptionUtilities.UnexpectedValue(matchResult);
                                }
                            }

                            if (wasImplementingMemberFound && interfaceMemberKind == SymbolKind.Method)
                            {
                                // Don't report use site errors on properties - we'll report them on each of their accessors.

                                // Don't report use site errors for implementations in other types unless
                                // a synthesized implementation is needed that invokes the base method.
                                // We can do so only if there are no use-site errors.

                                if (synthesizedImplementation.ForwardingMethod is not null || TypeSymbol.Equals(implementingMember.ContainingType, this, TypeCompareKind.ConsiderEverything2))
                                {
                                    UseSiteInfo<AssemblySymbol> useSiteInfo = interfaceMember.GetUseSiteInfo();
                                    // Don't report a use site error with a location in another compilation.  For example,
                                    // if the error is that a base type in another assembly implemented an interface member
                                    // on our behalf and the use site error is that the current assembly does not reference
                                    // some required assembly, then we want to report the error in the current assembly -
                                    // not in the implementing assembly.
                                    Location location = implementingMember.IsFromCompilation(this.DeclaringCompilation)
                                        ? implementingMember.GetFirstLocation()
                                        : this.GetFirstLocation();
                                    diagnostics.Add(useSiteInfo, location);
                                }
                            }
                        }
                    }
                }
            }

            return SynthesizedExplicitImplementations.Create(forwardingMethods.ToImmutableAndFree(), methodImpls.ToImmutableAndFree());
        }

        protected abstract Location GetCorrespondingBaseListLocation(NamedTypeSymbol @base);

#nullable enable
        private Location GetImplementsLocationOrFallback(NamedTypeSymbol implementedInterface)
        {
            return GetImplementsLocation(implementedInterface) ?? this.GetFirstLocation();
        }

        internal Location? GetImplementsLocation(NamedTypeSymbol implementedInterface)
#nullable disable
        {
            // We ideally want to identify the interface location in the base list with an exact match but
            // will fall back and use the first derived interface if exact interface is not present.
            // this is the similar logic as the VB implementation.
            Debug.Assert(this.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics[implementedInterface].Contains(implementedInterface));
            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;

            NamedTypeSymbol directInterface = null;
            foreach (var iface in this.InterfacesNoUseSiteDiagnostics())
            {
                if (TypeSymbol.Equals(iface, implementedInterface, TypeCompareKind.ConsiderEverything2))
                {
                    directInterface = iface;
                    break;
                }
                else if ((object)directInterface == null && iface.ImplementsInterface(implementedInterface, ref discardedUseSiteInfo))
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

            Symbol implementingPropertyOrEvent;

            if (this.IsInterface)
            {
                MultiDictionary<Symbol, Symbol>.ValueSet explicitImpl = this.GetExplicitImplementationForInterfaceMember(interfacePropertyOrEvent);

                switch (explicitImpl.Count)
                {
                    case 0:
                        return true;
                    case 1:
                        implementingPropertyOrEvent = explicitImpl.Single();
                        break;
                    default:
                        implementingPropertyOrEvent = null;
                        break;
                }
            }
            else
            {
                implementingPropertyOrEvent = this.FindImplementationForInterfaceMemberInNonInterface(interfacePropertyOrEvent);
            }

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

        private enum HasBaseTypeDeclaringInterfaceResult
        {
            NoMatch,
            IgnoringNullableMatch,
            ExactMatch,
        }

        private HasBaseTypeDeclaringInterfaceResult HasBaseClassDeclaringInterface(NamedTypeSymbol @interface)
        {
            HasBaseTypeDeclaringInterfaceResult result = HasBaseTypeDeclaringInterfaceResult.NoMatch;

            for (NamedTypeSymbol currType = this.BaseTypeNoUseSiteDiagnostics; (object)currType != null; currType = currType.BaseTypeNoUseSiteDiagnostics)
            {
                if (DeclaresBaseInterface(currType, @interface, ref result))
                {
                    break;
                }
            }

            return result;
        }

        private static bool DeclaresBaseInterface(NamedTypeSymbol currType, NamedTypeSymbol @interface, ref HasBaseTypeDeclaringInterfaceResult result)
        {
            MultiDictionary<NamedTypeSymbol, NamedTypeSymbol>.ValueSet set = currType.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics[@interface];

            if (set.Count != 0)
            {
                if (set.Contains(@interface))
                {
                    result = HasBaseTypeDeclaringInterfaceResult.ExactMatch;
                    return true;
                }
                else if (result == HasBaseTypeDeclaringInterfaceResult.NoMatch && set.Contains(@interface, Symbols.SymbolEqualityComparer.IgnoringNullable))
                {
                    result = HasBaseTypeDeclaringInterfaceResult.IgnoringNullableMatch;
                }
            }

            return false;
        }

        private void HasBaseInterfaceDeclaringInterface(NamedTypeSymbol baseInterface, NamedTypeSymbol @interface, ref HasBaseTypeDeclaringInterfaceResult matchResult)
        {
            // Let's check for the trivial case first
            if (DeclaresBaseInterface(baseInterface, @interface, ref matchResult))
            {
                return;
            }

            foreach (var interfaceType in this.AllInterfacesNoUseSiteDiagnostics)
            {
                if ((object)interfaceType == baseInterface)
                {
                    continue;
                }

                if (interfaceType.Equals(baseInterface, TypeCompareKind.CLRSignatureCompareOptions) &&
                    DeclaresBaseInterface(interfaceType, @interface, ref matchResult))
                {
                    return;
                }
            }
        }

        private void CheckMembersAgainstBaseType(
            BindingDiagnosticBag diagnostics,
            CancellationToken cancellationToken)
        {
            if (this.BaseTypeNoUseSiteDiagnostics?.IsErrorType() == true)
            {
                // Avoid cascading diagnostics
                return;
            }

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
                                var sourceMethod = method as SourceMemberMethodSymbol;
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
                            MethodSymbol overridden = method.GetFirstRuntimeOverriddenMethodIgnoringNewSlot(out _);

                            // NOTE: Dev11 doesn't expose symbols, so it can treat destructors as override and let them go through the normal
                            // checks.  Roslyn can't, since the language says they are not virtual/override and that's what we need to expose
                            // in the symbol model.  Having said that, Dev11 doesn't seem to produce override errors other than this one
                            // (see SymbolPreparer::prepareOperator).
                            if ((object)overridden != null && overridden.IsMetadataFinal)
                            {
                                diagnostics.Add(ErrorCode.ERR_CantOverrideSealed, method.GetFirstLocation(), method, overridden);
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
                        else if (property is SourcePropertySymbolBase sourceProperty)
                        {
                            var isNewProperty = sourceProperty.IsNew;
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

        private void CheckNewModifier(Symbol symbol, bool isNew, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(symbol.Kind == SymbolKind.Field || symbol.Kind == SymbolKind.NamedType);

            // Do not give warnings about missing 'new' modifier for implicitly declared members,
            // e.g. backing fields for auto-properties
            if (symbol.IsImplicitlyDeclared)
            {
                return;
            }

            if (symbol.ContainingType.IsInterface)
            {
                CheckNonOverrideMember(symbol, isNew,
                                       OverriddenOrHiddenMembersHelpers.MakeInterfaceOverriddenOrHiddenMembers(symbol, memberIsFromSomeCompilation: true),
                                       diagnostics, out _);
                return;
            }

            // for error cases
            if ((object)this.BaseTypeNoUseSiteDiagnostics == null)
            {
                return;
            }

            int symbolArity = symbol.GetMemberArity();
            Location symbolLocation = symbol.TryGetFirstLocation();
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

                    var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);
                    bool isAccessible = AccessCheck.IsSymbolAccessible(hiddenMember, this, ref useSiteInfo);
                    diagnostics.Add(symbolLocation, useSiteInfo);

                    if (isAccessible && hiddenMember.GetMemberArity() == symbolArity)
                    {
                        if (!isNew)
                        {
                            diagnostics.Add(ErrorCode.WRN_NewRequired, symbolLocation, symbol, hiddenMember);
                        }

                        AddHidingAbstractDiagnostic(symbol, symbolLocation, hiddenMember, diagnostics, ref unused);

                        if (hiddenMember.IsRequired())
                        {
                            // Required member '{0}' cannot be hidden by '{1}'.
                            diagnostics.Add(ErrorCode.ERR_RequiredMemberCannotBeHidden, symbolLocation, hiddenMember, symbol);
                        }

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

        private void CheckOverrideMember(
            Symbol overridingMember,
            OverriddenOrHiddenMembersResult overriddenOrHiddenMembers,
            BindingDiagnosticBag diagnostics,
            out bool suppressAccessors)
        {
            Debug.Assert((object)overridingMember != null);
            Debug.Assert(overriddenOrHiddenMembers != null);

            suppressAccessors = false;

            var overridingMemberIsMethod = overridingMember.Kind == SymbolKind.Method;
            var overridingMemberIsProperty = overridingMember.Kind == SymbolKind.Property;
            var overridingMemberIsEvent = overridingMember.Kind == SymbolKind.Event;

            Debug.Assert(overridingMemberIsMethod ^ overridingMemberIsProperty ^ overridingMemberIsEvent);

            var overridingMemberLocation = overridingMember.GetFirstLocation();

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
                        bool suppressError = false;
                        if (overridingMemberIsMethod || overridingMember.IsIndexer())
                        {
                            var parameterTypes = overridingMemberIsMethod
                                ? ((MethodSymbol)overridingMember).ParameterTypesWithAnnotations
                                : ((PropertySymbol)overridingMember).ParameterTypesWithAnnotations;

                            foreach (var parameterType in parameterTypes)
                            {
                                if (IsOrContainsErrorType(parameterType.Type))
                                {
                                    suppressError = true; // The parameter type must be fixed before the override can be found, so suppress error
                                    break;
                                }
                            }
                        }

                        if (!suppressError)
                        {
                            diagnostics.Add(ErrorCode.ERR_OverrideNotExpected, overridingMemberLocation, overridingMember);
                        }
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
                    checkSingleOverriddenMember(overridingMember, overriddenMembers[0], diagnostics, ref suppressAccessors);
                }
            }

            // Both `ref` and `out` parameters (and `in` too) are implemented as references and are not distinguished by the runtime
            // when resolving overrides. Similarly, distinctions between types that would map together because of generic substitution
            // in the derived type where the override appears are the same from the runtime's point of view. In these cases we will
            // need to produce a methodimpl to disambiguate. See the call to `RequiresExplicitOverride` below. It produces a boolean
            // `warnAmbiguous` if the methodimpl could be misinterpreted due to a bug in the runtime
            // (https://github.com/dotnet/runtime/issues/38119) in which case we produce a warning regarding that ambiguity.
            // See https://github.com/dotnet/roslyn/issues/45453 for details.
            if (!this.ContainingAssembly.RuntimeSupportsCovariantReturnsOfClasses && overridingMember is MethodSymbol overridingMethod)
            {
                overridingMethod.RequiresExplicitOverride(out bool warnAmbiguous);
                if (warnAmbiguous)
                {
                    var ambiguousMethod = overridingMethod.OverriddenMethod;
                    diagnostics.Add(ErrorCode.WRN_MultipleRuntimeOverrideMatches, ambiguousMethod.GetFirstLocation(), ambiguousMethod, overridingMember);
                    suppressAccessors = true;
                }
            }

            return;

            void checkSingleOverriddenMember(Symbol overridingMember, Symbol overriddenMember, BindingDiagnosticBag diagnostics, ref bool suppressAccessors)
            {
                var overridingMemberLocation = overridingMember.GetFirstLocation();
                var overridingMemberIsMethod = overridingMember.Kind == SymbolKind.Method;
                var overridingMemberIsProperty = overridingMember.Kind == SymbolKind.Property;
                var overridingMemberIsEvent = overridingMember.Kind == SymbolKind.Event;
                var overridingType = overridingMember.ContainingType;

                //otherwise, it would have been excluded during lookup
#if DEBUG
                {
                    var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                    Debug.Assert(AccessCheck.IsSymbolAccessible(overriddenMember, overridingType, ref discardedUseSiteInfo));
                }
#endif

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
                else if (!OverrideHasCorrectAccessibility(overriddenMember, overridingMember))
                {
                    var accessibility = SyntaxFacts.GetText(overriddenMember.DeclaredAccessibility);
                    diagnostics.Add(ErrorCode.ERR_CantChangeAccessOnOverride, overridingMemberLocation, overridingMember, accessibility, overriddenMember);
                    suppressAccessors = true;
                }
                else if (overridingMember.ContainsTupleNames() &&
                    MemberSignatureComparer.ConsideringTupleNamesCreatesDifference(overridingMember, overriddenMember))
                {
                    // it is ok to override with no tuple names, for compatibility with C# 6, but otherwise names should match
                    diagnostics.Add(ErrorCode.ERR_CantChangeTupleNamesOnOverride, overridingMemberLocation, overridingMember, overriddenMember);
                }
                else if (overriddenMember is PropertySymbol { IsRequired: true } && overridingMember is PropertySymbol { IsRequired: false })
                {
                    // '{0}' must be required because it overrides required member '{1}'
                    diagnostics.Add(ErrorCode.ERR_OverrideMustHaveRequired, overridingMemberLocation, overridingMember, overriddenMember);
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

                    if (overridingMemberIsProperty)
                    {
                        checkOverriddenProperty((PropertySymbol)overridingMember, (PropertySymbol)overriddenMember, diagnostics, ref suppressAccessors);
                    }
                    else if (overridingMemberIsEvent)
                    {
                        EventSymbol overridingEvent = (EventSymbol)overridingMember;
                        EventSymbol overriddenEvent = (EventSymbol)overriddenMember;

                        TypeWithAnnotations overridingMemberType = overridingEvent.TypeWithAnnotations;
                        TypeWithAnnotations overriddenMemberType = overriddenEvent.TypeWithAnnotations;

                        // Ignore custom modifiers because this diagnostic is based on the C# semantics.
                        if (!overridingMemberType.Equals(overriddenMemberType, TypeCompareKind.AllIgnoreOptions))
                        {
                            // if the type is or contains an error type, the type must be fixed before the override can be found, so suppress error
                            if (!IsOrContainsErrorType(overridingMemberType.Type))
                            {
                                diagnostics.Add(ErrorCode.ERR_CantChangeTypeOnOverride, overridingMemberLocation, overridingMember, overriddenMember, overriddenMemberType.Type);
                            }
                            suppressAccessors = true; //we get really unhelpful errors from the accessor if the type is mismatched
                        }
                        else
                        {
                            CheckValidNullableEventOverride(overridingEvent.DeclaringCompilation, overriddenEvent, overridingEvent,
                                                            diagnostics,
                                                            (diagnostics, overriddenEvent, overridingEvent, location) => diagnostics.Add(ErrorCode.WRN_NullabilityMismatchInTypeOnOverride, location),
                                                            overridingMemberLocation);
                        }
                    }
                    else
                    {
                        Debug.Assert(overridingMemberIsMethod);

                        var overridingMethod = (MethodSymbol)overridingMember;
                        var overriddenMethod = (MethodSymbol)overriddenMember;

                        if (overridingMethod.IsGenericMethod)
                        {
                            overriddenMethod = overriddenMethod.Construct(TypeMap.TypeParametersAsTypeSymbolsWithIgnoredAnnotations(overridingMethod.TypeParameters));
                        }

                        // Check for mismatched byref returns and return type. Ignore custom modifiers, because this diagnostic is based on the C# semantics.
                        if (overridingMethod.RefKind != overriddenMethod.RefKind)
                        {
                            diagnostics.Add(ErrorCode.ERR_CantChangeRefReturnOnOverride, overridingMemberLocation, overridingMember, overriddenMember);
                        }
                        else if (!IsValidOverrideReturnType(overridingMethod, overridingMethod.ReturnTypeWithAnnotations, overriddenMethod.ReturnTypeWithAnnotations, diagnostics))
                        {
                            // if the Return type is or contains an error type, the return type must be fixed before the override can be found, so suppress error
                            if (!IsOrContainsErrorType(overridingMethod.ReturnType))
                            {
                                // If the return type would be a valid covariant return, suggest using covariant return feature.
                                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                                if (DeclaringCompilation.Conversions.HasIdentityOrImplicitReferenceConversion(overridingMethod.ReturnTypeWithAnnotations.Type, overriddenMethod.ReturnTypeWithAnnotations.Type, ref discardedUseSiteInfo))
                                {
                                    if (!overridingMethod.ContainingAssembly.RuntimeSupportsCovariantReturnsOfClasses)
                                    {
                                        diagnostics.Add(ErrorCode.ERR_RuntimeDoesNotSupportCovariantReturnsOfClasses, overridingMemberLocation, overridingMember, overriddenMember, overriddenMethod.ReturnType);
                                    }
                                    else if (MessageID.IDS_FeatureCovariantReturnsForOverrides.GetFeatureAvailabilityDiagnosticInfo(this.DeclaringCompilation) is { } diagnosticInfo)
                                    {
                                        diagnostics.Add(diagnosticInfo, overridingMemberLocation);
                                    }
                                    else
                                    {
                                        throw ExceptionUtilities.Unreachable();
                                    }
                                }
                                else
                                {
                                    // error CS0508: return type must be 'C<V>' to match overridden member 'M<T>()'
                                    diagnostics.Add(ErrorCode.ERR_CantChangeReturnTypeOnOverride, overridingMemberLocation, overridingMember, overriddenMember, overriddenMethod.ReturnType);
                                }
                            }
                        }
                        else if (overriddenMethod.IsRuntimeFinalizer())
                        {
                            diagnostics.Add(ErrorCode.ERR_OverrideFinalizeDeprecated, overridingMemberLocation);
                        }
                        else if (!overridingMethod.IsAccessor())
                        {
                            // Accessors will have already been checked above
                            checkValidMethodOverride(
                                overridingMemberLocation,
                                overriddenMethod,
                                overridingMethod,
                                diagnostics);
                        }
                    }

                    // NOTE: this error may be redundant (if an error has already been reported
                    // for the return type or parameter type in question), but the scenario is
                    // too rare to justify complicated checks.
                    if (Binder.ReportUseSite(overriddenMember, diagnostics, overridingMember.GetFirstLocation()))
                    {
                        suppressAccessors = true;
                    }
                }

                void checkOverriddenProperty(PropertySymbol overridingProperty, PropertySymbol overriddenProperty, BindingDiagnosticBag diagnostics, ref bool suppressAccessors)
                {
                    var overridingMemberLocation = overridingProperty.GetFirstLocation();
                    var overridingType = overridingProperty.ContainingType;

                    TypeWithAnnotations overridingMemberType = overridingProperty.TypeWithAnnotations;
                    TypeWithAnnotations overriddenMemberType = overriddenProperty.TypeWithAnnotations;

                    // Check for mismatched byref returns and return type. Ignore custom modifiers, because this diagnostic is based on the C# semantics.
                    if (overridingProperty.RefKind != overriddenProperty.RefKind)
                    {
                        diagnostics.Add(ErrorCode.ERR_CantChangeRefReturnOnOverride, overridingMemberLocation, overridingProperty, overriddenProperty);
                        suppressAccessors = true; //we get really unhelpful errors from the accessor if the ref kind is mismatched
                    }
                    else if (overridingProperty.SetMethod is null ?
                        !IsValidOverrideReturnType(overridingProperty, overridingMemberType, overriddenMemberType, diagnostics) :
                        !overridingMemberType.Equals(overriddenMemberType, TypeCompareKind.AllIgnoreOptions))
                    {
                        // if the type is or contains an error type, the type must be fixed before the override can be found, so suppress error
                        if (!IsOrContainsErrorType(overridingMemberType.Type))
                        {
                            // If the type would be a valid covariant return, suggest using covariant return feature.
                            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                            if (overridingProperty.SetMethod is null &&
                                DeclaringCompilation.Conversions.HasIdentityOrImplicitReferenceConversion(overridingMemberType.Type, overriddenMemberType.Type, ref discardedUseSiteInfo))
                            {
                                if (!overridingProperty.ContainingAssembly.RuntimeSupportsCovariantReturnsOfClasses)
                                {
                                    diagnostics.Add(ErrorCode.ERR_RuntimeDoesNotSupportCovariantPropertiesOfClasses, overridingMemberLocation, overridingMember, overriddenMember, overriddenMemberType.Type);
                                }
                                else
                                {
                                    var diagnosticInfo = MessageID.IDS_FeatureCovariantReturnsForOverrides.GetFeatureAvailabilityDiagnosticInfo(this.DeclaringCompilation);
                                    Debug.Assert(diagnosticInfo is { });
                                    diagnostics.Add(diagnosticInfo, overridingMemberLocation);
                                }
                            }
                            else
                            {
                                // error CS1715: 'Derived.M': type must be 'object' to match overridden member 'Base.M'
                                diagnostics.Add(ErrorCode.ERR_CantChangeTypeOnOverride, overridingMemberLocation, overridingMember, overriddenMember, overriddenMemberType.Type);
                                // https://github.com/dotnet/roslyn/issues/44207 when overriddenMemberType.Type is an inheritable reference type and the covariant return
                                // feature is enabled, and the platform supports it, and there is no setter, we can say it has to be 'object' **or a derived type**.
                                // That would probably be a new error code.
                            }
                        }

                        suppressAccessors = true; //we get really unhelpful errors from the accessor if the type is mismatched
                    }
                    else
                    {
                        if (overridingProperty.GetMethod is object)
                        {
                            MethodSymbol overriddenGetMethod = overriddenProperty.GetOwnOrInheritedGetMethod();
                            checkValidMethodOverride(
                                overridingProperty.GetMethod.GetFirstLocation(),
                                overriddenGetMethod,
                                overridingProperty.GetMethod,
                                diagnostics);
                        }

                        if (overridingProperty.SetMethod is object)
                        {
                            var ownOrInheritedOverriddenSetMethod = overriddenProperty.GetOwnOrInheritedSetMethod();
                            checkValidMethodOverride(
                                overridingProperty.SetMethod.GetFirstLocation(),
                                ownOrInheritedOverriddenSetMethod,
                                overridingProperty.SetMethod,
                                diagnostics);

                            if (ownOrInheritedOverriddenSetMethod is object &&
                                overridingProperty.SetMethod.IsInitOnly != ownOrInheritedOverriddenSetMethod.IsInitOnly)
                            {
                                diagnostics.Add(ErrorCode.ERR_CantChangeInitOnlyOnOverride, overridingMemberLocation, overridingProperty, overriddenProperty);
                            }
                        }
                    }

                    // If the overriding property is sealed, then the overridden accessors cannot be inaccessible, since we
                    // have to override them to make them sealed in metadata.
                    // CONSIDER: It might be nice if this had its own error code(s) since it's an implementation restriction,
                    // rather than a language restriction as above.
                    if (overridingProperty.IsSealed)
                    {
                        MethodSymbol ownOrInheritedGetMethod = overridingProperty.GetOwnOrInheritedGetMethod();
                        var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, overridingProperty.ContainingAssembly);
                        if (overridingProperty.GetMethod != ownOrInheritedGetMethod && !AccessCheck.IsSymbolAccessible(ownOrInheritedGetMethod, overridingType, ref useSiteInfo))
                        {
                            diagnostics.Add(ErrorCode.ERR_NoGetToOverride, overridingMemberLocation, overridingProperty, overriddenProperty);
                        }

                        MethodSymbol ownOrInheritedSetMethod = overridingProperty.GetOwnOrInheritedSetMethod();
                        if (overridingProperty.SetMethod != ownOrInheritedSetMethod && !AccessCheck.IsSymbolAccessible(ownOrInheritedSetMethod, overridingType, ref useSiteInfo))
                        {
                            diagnostics.Add(ErrorCode.ERR_NoSetToOverride, overridingMemberLocation, overridingProperty, overriddenProperty);
                        }

                        diagnostics.Add(overridingMemberLocation, useSiteInfo);
                    }
                }
            }

            static void checkValidMethodOverride(
                Location overridingMemberLocation,
                MethodSymbol overriddenMethod,
                MethodSymbol overridingMethod,
                BindingDiagnosticBag diagnostics)
            {
                if (RequiresValidScopedOverrideForRefSafety(overriddenMethod))
                {
                    CheckValidScopedOverride(
                        overriddenMethod,
                        overridingMethod,
                        diagnostics,
                        static (diagnostics, overriddenMethod, overridingMethod, overridingParameter, _, location) =>
                            {
                                diagnostics.Add(
                                    ReportInvalidScopedOverrideAsError(overriddenMethod, overridingMethod) ?
                                        ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation :
                                        ErrorCode.WRN_ScopedMismatchInParameterOfOverrideOrImplementation,
                                    location,
                                    new FormattedSymbol(overridingParameter, SymbolDisplayFormat.ShortFormat));
                            },
                        overridingMemberLocation,
                        allowVariance: true,
                        invokedAsExtensionMethod: false);
                }

                CheckValidNullableMethodOverride(overridingMethod.DeclaringCompilation, overriddenMethod, overridingMethod, diagnostics,
                                                 ReportBadReturn,
                                                 ReportBadParameter,
                                                 overridingMemberLocation);

                CheckRefReadonlyInMismatch(
                    overriddenMethod, overridingMethod, diagnostics,
                    static (diagnostics, _, _, overridingParameter, _, arg) =>
                    {
                        var (overriddenParameter, location) = arg;
                        // Reference kind modifier of parameter '{0}' doesn't match the corresponding parameter '{1}' in overridden or implemented member.
                        diagnostics.Add(ErrorCode.WRN_OverridingDifferentRefness, location, overridingParameter, overriddenParameter);
                    },
                    overridingMemberLocation,
                    invokedAsExtensionMethod: false);
            }
        }

        internal static bool IsOrContainsErrorType(TypeSymbol typeSymbol)
        {
            return (object)typeSymbol.VisitType((currentTypeSymbol, unused1, unused2) => currentTypeSymbol.IsErrorType(), (object)null) != null;
        }

        /// <summary>
        /// Return true if <paramref name="overridingReturnType"/> is valid for the return type of an override method when the overridden method's return type is <paramref name="overriddenReturnType"/>.
        /// </summary>
        private bool IsValidOverrideReturnType(Symbol overridingSymbol, TypeWithAnnotations overridingReturnType, TypeWithAnnotations overriddenReturnType, BindingDiagnosticBag diagnostics)
        {
            if (overridingSymbol.ContainingAssembly.RuntimeSupportsCovariantReturnsOfClasses &&
                DeclaringCompilation.LanguageVersion >= MessageID.IDS_FeatureCovariantReturnsForOverrides.RequiredVersion())
            {
                var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);
                var result = DeclaringCompilation.Conversions.HasIdentityOrImplicitReferenceConversion(overridingReturnType.Type, overriddenReturnType.Type, ref useSiteInfo);
                Location symbolLocation = overridingSymbol.TryGetFirstLocation();
                diagnostics.Add(symbolLocation, useSiteInfo);

                return result;
            }
            else
            {
                return overridingReturnType.Equals(overriddenReturnType, TypeCompareKind.AllIgnoreOptions);
            }
        }

        private static readonly ReportMismatchInReturnType<Location> ReportBadReturn =
            (BindingDiagnosticBag diagnostics, MethodSymbol overriddenMethod, MethodSymbol overridingMethod, bool topLevel, Location location)
            => diagnostics.Add(topLevel ?
                ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnOverride :
                ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride,
                location);

        private static readonly ReportMismatchInParameterType<Location> ReportBadParameter =
            (BindingDiagnosticBag diagnostics, MethodSymbol overriddenMethod, MethodSymbol overridingMethod, ParameterSymbol overridingParameter, bool topLevel, Location location)
            => diagnostics.Add(
                topLevel ? ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnOverride : ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride,
                location,
                new FormattedSymbol(overridingParameter, SymbolDisplayFormat.ShortFormat));

        /// <returns>
        /// <see langword="true"/> if a diagnostic was added. Otherwise, <see langword="false"/>.
        /// </returns>
        internal static bool CheckValidNullableMethodOverride<TArg>(
            CSharpCompilation compilation,
            MethodSymbol baseMethod,
            MethodSymbol overrideMethod,
            BindingDiagnosticBag diagnostics,
            ReportMismatchInReturnType<TArg> reportMismatchInReturnType,
            ReportMismatchInParameterType<TArg> reportMismatchInParameterType,
            TArg extraArgument,
            bool invokedAsExtensionMethod = false)
        {
            if (!PerformValidNullableOverrideCheck(compilation, baseMethod, overrideMethod))
            {
                return false;
            }

            bool hasErrors = false;

            if ((baseMethod.FlowAnalysisAnnotations & FlowAnalysisAnnotations.DoesNotReturn) == FlowAnalysisAnnotations.DoesNotReturn &&
                (overrideMethod.FlowAnalysisAnnotations & FlowAnalysisAnnotations.DoesNotReturn) != FlowAnalysisAnnotations.DoesNotReturn)
            {
                diagnostics.Add(ErrorCode.WRN_DoesNotReturnMismatch, overrideMethod.GetFirstLocation(), new FormattedSymbol(overrideMethod, SymbolDisplayFormat.MinimallyQualifiedFormat));
                hasErrors = true;
            }

            var conversions = compilation.Conversions.WithNullability(true);
            var baseParameters = baseMethod.Parameters;
            var overrideParameters = overrideMethod.Parameters;
            var overrideParameterOffset = invokedAsExtensionMethod ? 1 : 0;
            Debug.Assert(baseMethod.ParameterCount == overrideMethod.ParameterCount - overrideParameterOffset);
            if (reportMismatchInReturnType != null)
            {
                var overrideReturnType = getNotNullIfNotNullOutputType(overrideMethod.ReturnTypeWithAnnotations, overrideMethod.ReturnNotNullIfParameterNotNull);
                // check nested nullability
                if (!isValidNullableConversion(
                        conversions,
                        overrideMethod.RefKind,
                        overrideReturnType.Type,
                        baseMethod.ReturnTypeWithAnnotations.Type))
                {
                    reportMismatchInReturnType(diagnostics, baseMethod, overrideMethod, false, extraArgument);
                    return true;
                }

                // check top-level nullability including flow analysis annotations
                if (!NullableWalker.AreParameterAnnotationsCompatible(
                        overrideMethod.RefKind == RefKind.Ref ? RefKind.Ref : RefKind.Out,
                        baseMethod.ReturnTypeWithAnnotations,
                        baseMethod.ReturnTypeFlowAnalysisAnnotations,
                        overrideReturnType,
                        overrideMethod.ReturnTypeFlowAnalysisAnnotations))
                {
                    reportMismatchInReturnType(diagnostics, baseMethod, overrideMethod, true, extraArgument);
                    return true;
                }
            }

            if (reportMismatchInParameterType != null)
            {
                for (int i = 0; i < baseParameters.Length; i++)
                {
                    var baseParameter = baseParameters[i];
                    var baseParameterType = baseParameter.TypeWithAnnotations;
                    int parameterIndex = i + overrideParameterOffset;
                    var overrideParameter = overrideParameters[parameterIndex];
                    var overrideParameterType = getNotNullIfNotNullOutputType(overrideParameter.TypeWithAnnotations, overrideParameter.NotNullIfParameterNotNull);
                    // check nested nullability
                    if (!isValidNullableConversion(
                            conversions,
                            overrideParameter.RefKind,
                            baseParameterType.Type,
                            overrideParameterType.Type))
                    {
                        reportMismatchInParameterType(diagnostics, baseMethod, overrideMethod, overrideParameter, false, extraArgument);
                        hasErrors = true;
                    }
                    // check top-level nullability including flow analysis annotations
                    else if (!NullableWalker.AreParameterAnnotationsCompatible(
                            overrideParameter.RefKind,
                            baseParameterType,
                            baseParameter.FlowAnalysisAnnotations,
                            overrideParameterType,
                            overrideParameter.FlowAnalysisAnnotations))
                    {
                        reportMismatchInParameterType(diagnostics, baseMethod, overrideMethod, overrideParameter, true, extraArgument);
                        hasErrors = true;
                    }
                }
            }

            return hasErrors;

            TypeWithAnnotations getNotNullIfNotNullOutputType(TypeWithAnnotations outputType, ImmutableHashSet<string> notNullIfParameterNotNull)
            {
                if (!notNullIfParameterNotNull.IsEmpty)
                {
                    for (var i = 0; i < baseParameters.Length; i++)
                    {
                        var overrideParam = overrideParameters[i + overrideParameterOffset];
                        var baseParam = baseParameters[i];
                        if (notNullIfParameterNotNull.Contains(overrideParam.Name) && NullableWalker.GetParameterState(baseParam.TypeWithAnnotations, baseParam.FlowAnalysisAnnotations).IsNotNull)
                        {
                            return outputType.AsNotAnnotated();
                        }
                    }
                }

                return outputType;
            }

            static bool isValidNullableConversion(
                ConversionsBase conversions,
                RefKind refKind,
                TypeSymbol sourceType,
                TypeSymbol targetType)
            {
                switch (refKind)
                {
                    case RefKind.Ref:
                        // ref variables are invariant
                        return sourceType.Equals(
                            targetType,
                            TypeCompareKind.AllIgnoreOptions & ~(TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));

                    case RefKind.Out:
                        // out variables have inverted variance
                        (sourceType, targetType) = (targetType, sourceType);
                        break;

                    default:
                        break;
                }

                Debug.Assert(conversions.IncludeNullability);
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                return conversions.ClassifyImplicitConversionFromType(sourceType, targetType, ref discardedUseSiteInfo).Kind != ConversionKind.NoConversion;
            }
        }

#nullable enable
        /// <summary>
        /// Returns true if the method signature must match, with respect to scoped for ref safety,
        /// in overrides, interface implementations, or delegate conversions.
        /// </summary>
        internal static bool RequiresValidScopedOverrideForRefSafety(MethodSymbol? method)
        {
            if (method is null)
            {
                return false;
            }

            var parameters = method.Parameters;

            // https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/low-level-struct-improvements.md#scoped-mismatch
            // The compiler will report a diagnostic for _unsafe scoped mismatches_ across overrides, interface implementations, and delegate conversions when:
            // - The method returns a `ref struct` or returns a `ref` or `ref readonly`, or the method has a `ref` or `out` parameter of `ref struct` type, and
            // ...
            int nRefParametersRequired;
            if (method.ReturnType.IsRefLikeOrAllowsRefLikeType() ||
                (method.RefKind is RefKind.Ref or RefKind.RefReadOnly))
            {
                nRefParametersRequired = 1;
            }
            else if (parameters.Any(p => (p.RefKind is RefKind.Ref or RefKind.Out) && p.Type.IsRefLikeOrAllowsRefLikeType()))
            {
                nRefParametersRequired = 2; // including the parameter found above
            }
            else
            {
                return false;
            }

            // ...
            // - The method has at least one additional `ref`, `in`, `ref readonly`, or `out` parameter, or a parameter of `ref struct` type.
            int nRefParameters = parameters.Count(p => p.RefKind is RefKind.Ref or RefKind.In or RefKind.RefReadOnlyParameter or RefKind.Out);
            if (nRefParameters >= nRefParametersRequired)
            {
                return true;
            }
            else if (parameters.Any(p => p.RefKind == RefKind.None && p.Type.IsRefLikeOrAllowsRefLikeType()))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if a scoped mismatch should be reported as an error rather than a warning.
        /// </summary>
        internal static bool ReportInvalidScopedOverrideAsError(MethodSymbol baseMethod, MethodSymbol overrideMethod)
        {
            // https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/low-level-struct-improvements.md#scoped-mismatch
            // The diagnostic is reported as an error if the mismatched signatures are both using C#11 ref safety rules; otherwise, the diagnostic is a warning.
            return baseMethod.UseUpdatedEscapeRules && overrideMethod.UseUpdatedEscapeRules;
        }

        /// <summary>
        /// Returns true if a diagnostic was added.
        /// </summary>
        internal static bool CheckValidScopedOverride<TArg>(
            MethodSymbol? baseMethod,
            MethodSymbol? overrideMethod,
            BindingDiagnosticBag diagnostics,
            ReportMismatchInParameterType<TArg> reportMismatchInParameterType,
            TArg extraArgument,
            bool allowVariance,
            bool invokedAsExtensionMethod)
        {
            Debug.Assert(reportMismatchInParameterType is { });

            if (baseMethod is null || overrideMethod is null)
            {
                return false;
            }

            bool hasErrors = false;
            var baseParameters = baseMethod.Parameters;
            var overrideParameters = overrideMethod.Parameters;
            var overrideParameterOffset = invokedAsExtensionMethod ? 1 : 0;
            Debug.Assert(baseMethod.ParameterCount == overrideMethod.ParameterCount - overrideParameterOffset);

            for (int i = 0; i < baseParameters.Length; i++)
            {
                var baseParameter = baseParameters[i];
                var overrideParameter = overrideParameters[i + overrideParameterOffset];
                if (!isValidScopedConversion(allowVariance, baseParameter.EffectiveScope, baseParameter.HasUnscopedRefAttribute, overrideParameter.EffectiveScope, overrideParameter.HasUnscopedRefAttribute))
                {
                    reportMismatchInParameterType(diagnostics, baseMethod, overrideMethod, overrideParameter, topLevel: true, extraArgument);
                    hasErrors = true;
                }
            }
            return hasErrors;

            static bool isValidScopedConversion(
                bool allowVariance,
                ScopedKind baseScope,
                bool baseHasUnscopedRefAttribute,
                ScopedKind overrideScope,
                bool overrideHasUnscopedRefAttribute)
            {
                if (baseScope == overrideScope)
                {
                    if (baseHasUnscopedRefAttribute == overrideHasUnscopedRefAttribute)
                    {
                        return true;
                    }
                    return allowVariance && !overrideHasUnscopedRefAttribute;
                }
                return allowVariance && baseScope == ScopedKind.None;
            }
        }

        internal static void CheckRefReadonlyInMismatch<TArg>(
            MethodSymbol? baseMethod,
            MethodSymbol? overrideMethod,
            BindingDiagnosticBag diagnostics,
            ReportMismatchInParameterType<(ParameterSymbol BaseParameter, TArg Arg)> reportMismatchInParameterType,
            TArg extraArgument,
            bool invokedAsExtensionMethod)
        {
            Debug.Assert(reportMismatchInParameterType is { });

            if (baseMethod is null || overrideMethod is null)
            {
                return;
            }

            var baseParameters = baseMethod.Parameters;
            var overrideParameters = overrideMethod.Parameters;
            var overrideParameterOffset = invokedAsExtensionMethod ? 1 : 0;
            Debug.Assert(baseMethod.ParameterCount == overrideMethod.ParameterCount - overrideParameterOffset);

            for (int i = 0; i < baseParameters.Length; i++)
            {
                var baseParameter = baseParameters[i];
                var overrideParameter = overrideParameters[i + overrideParameterOffset];
                if (baseParameter.RefKind != overrideParameter.RefKind)
                {
                    reportMismatchInParameterType(diagnostics, baseMethod, overrideMethod, overrideParameter, topLevel: true, (baseParameter, extraArgument));
                }
            }
        }
#nullable disable

        private static bool PerformValidNullableOverrideCheck(
            CSharpCompilation compilation,
            Symbol overriddenMember,
            Symbol overridingMember)
        {
            // Don't do any validation if the nullable feature is not enabled or
            // the override is not written directly in source
            return overriddenMember is object &&
                   overridingMember is object &&
                   compilation is object &&
                   compilation.IsFeatureEnabled(MessageID.IDS_FeatureNullableReferenceTypes);
        }

        internal static void CheckValidNullableEventOverride<TArg>(
            CSharpCompilation compilation,
            EventSymbol overriddenEvent,
            EventSymbol overridingEvent,
            BindingDiagnosticBag diagnostics,
            Action<BindingDiagnosticBag, EventSymbol, EventSymbol, TArg> reportMismatch,
            TArg extraArgument)
        {
            if (!PerformValidNullableOverrideCheck(compilation, overriddenEvent, overridingEvent))
            {
                return;
            }

            var conversions = compilation.Conversions.WithNullability(true);
            if (!conversions.HasAnyNullabilityImplicitConversion(overriddenEvent.TypeWithAnnotations, overridingEvent.TypeWithAnnotations))
            {
                reportMismatch(diagnostics, overriddenEvent, overridingEvent, extraArgument);
            }
        }

        private static void CheckNonOverrideMember(
            Symbol hidingMember,
            bool hidingMemberIsNew,
            OverriddenOrHiddenMembersResult overriddenOrHiddenMembers,
            BindingDiagnosticBag diagnostics, out bool suppressAccessors)
        {
            suppressAccessors = false;

            var hidingMemberLocation = hidingMember.GetFirstLocation();

            Debug.Assert(overriddenOrHiddenMembers != null);
            Debug.Assert(!overriddenOrHiddenMembers.OverriddenMembers.Any()); //since hidingMethod.IsOverride is false

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
                            (hiddenMember.IsAbstract || hiddenMember.IsVirtual || hiddenMember.IsOverride) &&
                            !IsShadowingSynthesizedRecordMember(hidingMember))
                        {
                            diagnostics.Add(ErrorCode.WRN_NewOrOverrideExpected, hidingMemberLocation, hidingMember, hiddenMember);
                            diagnosticAdded = true;
                        }

                        if (hiddenMember.IsRequired())
                        {
                            // Required member '{0}' cannot be hidden by '{1}'.
                            diagnostics.Add(ErrorCode.ERR_RequiredMemberCannotBeHidden, hidingMemberLocation, hiddenMember, hidingMember);
                            diagnosticAdded = true;
                        }

                        if (diagnosticAdded)
                        {
                            break;
                        }
                    }
                }

                if (!hidingMemberIsNew && !IsShadowingSynthesizedRecordMember(hidingMember) && !diagnosticAdded && !hidingMember.IsAccessor() && !hidingMember.IsOperator())
                {
                    diagnostics.Add(ErrorCode.WRN_NewRequired, hidingMemberLocation, hidingMember, hiddenMembers[0]);
                }

                if (hidingMember is MethodSymbol hidingMethod && hiddenMembers[0] is MethodSymbol hiddenMethod)
                {
                    CheckRefReadonlyInMismatch(
                        hiddenMethod, hidingMethod, diagnostics,
                        static (diagnostics, _, _, hidingParameter, _, arg) =>
                        {
                            var (hiddenParameter, location) = arg;
                            // Reference kind modifier of parameter '{0}' doesn't match the corresponding parameter '{1}' in hidden member.
                            diagnostics.Add(ErrorCode.WRN_HidingDifferentRefness, location, hidingParameter, hiddenParameter);
                        },
                        hidingMemberLocation,
                        invokedAsExtensionMethod: false);
                }
            }
        }

        private static bool IsShadowingSynthesizedRecordMember(Symbol hidingMember)
        {
            return hidingMember is SynthesizedRecordEquals || hidingMember is SynthesizedRecordDeconstruct || hidingMember is SynthesizedRecordClone;
        }

        /// <summary>
        /// If necessary, report a diagnostic for a hidden abstract member.
        /// </summary>
        /// <returns>True if a diagnostic was reported.</returns>
        private static bool AddHidingAbstractDiagnostic(Symbol hidingMember, Location hidingMemberLocation, Symbol hiddenMember, BindingDiagnosticBag diagnostics, ref bool suppressAccessors)
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
                case Accessibility.ProtectedAndInternal:
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
                                    diagnostics.Add(ErrorCode.ERR_HidingAbstractMethod, associatedPropertyOrEvent.GetFirstLocation(), associatedPropertyOrEvent, hiddenMember);
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

#nullable enable

        /// <summary>
        /// It is invalid for a type to directly (vs through a base class) implement two interfaces that
        /// unify (i.e. are the same for some substitution of type parameters).
        /// </summary>
        /// <remarks>
        /// CONSIDER: check this while building up InterfacesAndTheirBaseInterfaces (only in the SourceNamedTypeSymbol case).
        /// </remarks>
        private void CheckInterfaceUnification(BindingDiagnosticBag diagnostics)
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

            NamedTypeSymbol[] interfaces = this.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Keys.ToArray();

            for (int i1 = 0; i1 < numInterfaces; i1++)
            {
                for (int i2 = i1 + 1; i2 < numInterfaces; i2++)
                {
                    NamedTypeSymbol interface1 = interfaces[i1];
                    NamedTypeSymbol interface2 = interfaces[i2];

                    // CanUnifyWith is the real check - the others just short-circuit
                    if (interface1.IsGenericType && interface2.IsGenericType &&
                        TypeSymbol.Equals(interface1.OriginalDefinition, interface2.OriginalDefinition, TypeCompareKind.ConsiderEverything2) &&
                        interface1.CanUnifyWith(interface2))
                    {
                        if (GetImplementsLocationOrFallback(interface1).SourceSpan.Start > GetImplementsLocationOrFallback(interface2).SourceSpan.Start)
                        {
                            // Mention interfaces in order of their appearance in the base list, for consistency.
                            var temp = interface1;
                            interface1 = interface2;
                            interface2 = temp;
                        }

                        diagnostics.Add(ErrorCode.ERR_UnifyingInterfaceInstantiations, this.GetFirstLocation(), this, interface1, interface2);
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
        /// <returns>
        /// A synthesized forwarding method for the implementation, or information about MethodImpl entry that should be emitted,
        /// or default if neither needed.
        /// </returns>
        private (SynthesizedExplicitImplementationForwardingMethod? ForwardingMethod, (MethodSymbol Body, MethodSymbol Implemented)? MethodImpl)
            SynthesizeInterfaceMemberImplementation(SymbolAndDiagnostics implementingMemberAndDiagnostics, Symbol interfaceMember)
        {
            foreach (Diagnostic diagnostic in implementingMemberAndDiagnostics.Diagnostics.Diagnostics)
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Code is not ((int)ErrorCode.ERR_ImplicitImplementationOfNonPublicInterfaceMember or (int)ErrorCode.ERR_ImplicitImplementationOfInaccessibleInterfaceMember))
                {
                    return default;
                }
            }

            Symbol implementingMember = implementingMemberAndDiagnostics.Symbol;

            //don't worry about properties or events - we'll catch them through their accessors
            if ((object)implementingMember == null || implementingMember.Kind != SymbolKind.Method)
            {
                return default;
            }

            MethodSymbol interfaceMethod = (MethodSymbol)interfaceMember;
            MethodSymbol implementingMethod = (MethodSymbol)implementingMember;

            //explicit implementations are always respected by the CLR
            if (implementingMethod.ExplicitInterfaceImplementations.Contains(interfaceMethod, ExplicitInterfaceImplementationTargetMemberEqualityComparer.Instance))
            {
                return default;
            }

            if (!interfaceMethod.IsStatic)
            {
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
                        if (implementingMethodOriginalDefinition is SourceMemberMethodSymbol sourceImplementMethodOriginalDefinition)
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
                    return default;
                }
            }
            else
            {
                if (implementingMethod.ContainingType != (object)this)
                {
                    if (implementingMethod.ContainingType.IsInterface ||
                        implementingMethod.Equals(this.BaseTypeNoUseSiteDiagnostics?.FindImplementationForInterfaceMemberInNonInterfaceWithDiagnostics(interfaceMethod).Symbol, TypeCompareKind.CLRSignatureCompareOptions))
                    {
                        return default;
                    }
                }
                else if (MemberSignatureComparer.RuntimeExplicitImplementationSignatureComparer.Equals(implementingMethod, interfaceMethod))
                {
                    return (null, (implementingMethod, interfaceMethod));
                }
            }

            return (new SynthesizedExplicitImplementationForwardingMethod(interfaceMethod, implementingMethod, this), null);
        }

#nullable disable

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
            if (type.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.ContainsKey(@interface))
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
