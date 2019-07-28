// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents an event of a tuple type (such as (int, byte).SomeEvent)
    /// that is backed by an event within the tuple underlying type.
    /// </summary>
    internal sealed class TupleEventSymbol : WrappedEventSymbol
    {
        private readonly TupleTypeSymbol _containingType;

        public TupleEventSymbol(TupleTypeSymbol container, EventSymbol underlyingEvent)
            : base(underlyingEvent)
        {
            _containingType = container;
        }

        public override bool IsTupleEvent
        {
            get
            {
                return true;
            }
        }

        public override EventSymbol TupleUnderlyingEvent
        {
            get
            {
                return _underlyingEvent;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _containingType;
            }
        }

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get
            {
                return _underlyingEvent.TypeWithAnnotations;
            }
        }

        public override MethodSymbol AddMethod
        {
            get
            {
                return _containingType.GetTupleMemberSymbolForUnderlyingMember(_underlyingEvent.AddMethod);
            }
        }

        public override MethodSymbol RemoveMethod
        {
            get
            {
                return _containingType.GetTupleMemberSymbolForUnderlyingMember(_underlyingEvent.RemoveMethod);
            }
        }

        internal override FieldSymbol AssociatedField
        {
            get
            {
                return _containingType.GetTupleMemberSymbolForUnderlyingMember(_underlyingEvent.AssociatedField);
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get
            {
                return _underlyingEvent.IsExplicitInterfaceImplementation;
            }
        }

        public override ImmutableArray<EventSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                return _underlyingEvent.ExplicitInterfaceImplementations;
            }
        }

        internal override bool MustCallMethodsDirectly
        {
            get
            {
                return _underlyingEvent.MustCallMethodsDirectly;
            }
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            DiagnosticInfo result = base.GetUseSiteDiagnostic();
            MergeUseSiteDiagnostics(ref result, _underlyingEvent.GetUseSiteDiagnostic());
            return result;
        }

        public override int GetHashCode()
        {
            return _underlyingEvent.GetHashCode();
        }

        public override bool Equals(Symbol obj, TypeCompareKind compareKind)
        {
            return Equals(obj as TupleEventSymbol, compareKind);
        }

        public bool Equals(TupleEventSymbol other, TypeCompareKind compareKind)
        {
            if ((object)other == this)
            {
                return true;
            }

            return (object)other != null && TypeSymbol.Equals(_containingType, other._containingType, compareKind) && _underlyingEvent == other._underlyingEvent;
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _underlyingEvent.GetAttributes();
        }
    }
}
