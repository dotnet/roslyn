// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class EventSymbol : Symbol, IEventSymbol
    {
        private readonly Symbols.EventSymbol _underlying;
        private ITypeSymbol _lazyType;

        public EventSymbol(Symbols.EventSymbol underlying)
        {
            Debug.Assert(underlying is object);
            _underlying = underlying;
        }

        internal override CSharp.Symbol UnderlyingSymbol => _underlying;
        internal Symbols.EventSymbol UnderlyingEventSymbol => _underlying;

        ITypeSymbol IEventSymbol.Type
        {
            get
            {
                if (_lazyType is null)
                {
                    Interlocked.CompareExchange(ref _lazyType, _underlying.TypeWithAnnotations.GetITypeSymbol(), null);
                }

                return _lazyType;
            }
        }

        CodeAnalysis.NullableAnnotation IEventSymbol.NullableAnnotation => _underlying.TypeWithAnnotations.ToPublicAnnotation();

        IMethodSymbol IEventSymbol.AddMethod
        {
            get
            {
                return _underlying.AddMethod.GetPublicSymbol();
            }
        }

        IMethodSymbol IEventSymbol.RemoveMethod
        {
            get
            {
                return _underlying.RemoveMethod.GetPublicSymbol();
            }
        }

        IMethodSymbol IEventSymbol.RaiseMethod
        {
            get
            {
                // C# doesn't have raise methods for events.
                return null;
            }
        }

        IEventSymbol IEventSymbol.OriginalDefinition
        {
            get
            {
                return _underlying.OriginalDefinition.GetPublicSymbol();
            }
        }

        IEventSymbol IEventSymbol.OverriddenEvent
        {
            get
            {
                return _underlying.OverriddenEvent.GetPublicSymbol();
            }
        }

        ImmutableArray<IEventSymbol> IEventSymbol.ExplicitInterfaceImplementations
        {
            get
            {
                return _underlying.ExplicitInterfaceImplementations.GetPublicSymbols();
            }
        }

        bool IEventSymbol.IsWindowsRuntimeEvent => _underlying.IsWindowsRuntimeEvent;

        #region ISymbol Members

        protected override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitEvent(this);
        }

        protected override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitEvent(this);
        }

        #endregion
    }
}
