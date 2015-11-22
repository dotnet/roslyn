// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Base class for event accessors - synthesized and user defined.
    /// </summary>
    internal abstract class SourceEventAccessorSymbol : SourceMethodSymbol
    {
        private readonly SourceEventSymbol _event;

        private ImmutableArray<ParameterSymbol> _lazyParameters;
        private TypeSymbolWithAnnotations _lazyReturnType;

        public SourceEventAccessorSymbol(
            SourceEventSymbol @event,
            SyntaxReference syntaxReference,
            SyntaxReference blockSyntaxReference,
            ImmutableArray<Location> locations)
            : base(@event.containingType, syntaxReference, blockSyntaxReference, locations)
        {
            _event = @event;
        }

        public SourceEventSymbol AssociatedEvent
        {
            get { return _event; }
        }

        public sealed override Symbol AssociatedSymbol
        {
            get { return _event; }
        }

        protected sealed override void MethodChecks(DiagnosticBag diagnostics)
        {
            Debug.Assert(_lazyParameters.IsDefault == ((object)_lazyReturnType == null));

            // CONSIDER: currently, we're copying the custom modifiers of the event overridden
            // by this method's associated event (by using the associated event's type, which is
            // copied from the overridden event).  It would be more correct to copy them from
            // the specific accessor that this method is overriding (as in SourceMemberMethodSymbol).

            if ((object)_lazyReturnType == null)
            {
                CSharpCompilation compilation = this.DeclaringCompilation;
                Debug.Assert(compilation != null);

                // NOTE: LazyMethodChecks calls us within a lock, so we use regular assignments,
                // rather than Interlocked.CompareExchange.
                if (_event.IsWindowsRuntimeEvent)
                {
                    TypeSymbol eventTokenType = compilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken);
                    Binder.ReportUseSiteDiagnostics(eventTokenType, diagnostics, this.Location);

                    if (this.MethodKind == MethodKind.EventAdd)
                    {
                        // EventRegistrationToken add_E(EventDelegate d);

                        // Leave the returns void bit in this.flags false.
                        _lazyReturnType = TypeSymbolWithAnnotations.Create(eventTokenType);

                        var parameter = new SynthesizedAccessorValueParameterSymbol(this, _event.Type, 0);
                        _lazyParameters = ImmutableArray.Create<ParameterSymbol>(parameter);
                    }
                    else
                    {
                        Debug.Assert(this.MethodKind == MethodKind.EventRemove);

                        // void remove_E(EventRegistrationToken t);

                        TypeSymbol voidType = compilation.GetSpecialType(SpecialType.System_Void);
                        Binder.ReportUseSiteDiagnostics(voidType, diagnostics, this.Location);
                        _lazyReturnType = TypeSymbolWithAnnotations.Create(voidType);
                        this.SetReturnsVoid(returnsVoid: true);

                        var parameter = new SynthesizedAccessorValueParameterSymbol(this, TypeSymbolWithAnnotations.Create(eventTokenType), 0);
                        _lazyParameters = ImmutableArray.Create<ParameterSymbol>(parameter);
                    }
                }
                else
                {
                    // void add_E(EventDelegate d);
                    // void remove_E(EventDelegate d);

                    TypeSymbol voidType = compilation.GetSpecialType(SpecialType.System_Void);
                    Binder.ReportUseSiteDiagnostics(voidType, diagnostics, this.Location);
                    _lazyReturnType = TypeSymbolWithAnnotations.Create(voidType);
                    this.SetReturnsVoid(returnsVoid: true);

                    var parameter = new SynthesizedAccessorValueParameterSymbol(this, _event.Type, 0);
                    _lazyParameters = ImmutableArray.Create<ParameterSymbol>(parameter);
                }
            }
        }

        public sealed override bool ReturnsVoid
        {
            get
            {
                LazyMethodChecks();
                Debug.Assert((object)_lazyReturnType != null);
                return base.ReturnsVoid;
            }
        }

        public sealed override TypeSymbolWithAnnotations ReturnType
        {
            get
            {
                LazyMethodChecks();
                Debug.Assert((object)_lazyReturnType != null);
                return _lazyReturnType;
            }
        }

        public sealed override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                LazyMethodChecks();
                Debug.Assert(!_lazyParameters.IsDefault);
                return _lazyParameters;
            }
        }

        public sealed override bool IsVararg
        {
            get { return false; }
        }

        public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return ImmutableArray<TypeParameterSymbol>.Empty; }
        }

        internal Location Location
        {
            get
            {
                Debug.Assert(this.Locations.Length == 1);
                return this.Locations[0];
            }
        }

        protected string GetOverriddenAccessorName(SourceEventSymbol @event, bool isAdder)
        {
            if (this.IsOverride)
            {
                // NOTE: What we'd really like to do is ask for the OverriddenMethod of this symbol.
                // Unfortunately, we can't do that, because it would inspect the signature of this
                // method, which depends on whether @event is a WinRT event, which depends on
                // interface implementation, which we can't check during construction of the 
                // member list of the type containing this accessor (infinite recursion).  Instead,
                // we inline part of the implementation of OverriddenMethod - we look for the
                // overridden event (which does not depend on WinRT-ness) and then grab the corresponding
                // accessor.
                EventSymbol overriddenEvent = @event.OverriddenEvent;
                if ((object)overriddenEvent != null)
                {
                    // If this accessor is overriding an accessor from metadata, it is possible that
                    // the name of the overridden accessor doesn't follow the C# add_X/remove_X pattern.
                    // We should copy the name so that the runtime will recognize this as an override.
                    MethodSymbol overriddenAccessor = overriddenEvent.GetOwnOrInheritedAccessor(isAdder);
                    return (object)overriddenAccessor == null ? null : overriddenAccessor.Name;
                }
            }

            return null;
        }

        internal override bool IsExpressionBodied
        {
            // Events cannot be expression-bodied
            get { return false; }
        }
    }
}
