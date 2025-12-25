// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class PropertySymbol : Symbol, IPropertySymbol
    {
        private readonly Symbols.PropertySymbol _underlying;
        private ITypeSymbol _lazyType;

        public PropertySymbol(Symbols.PropertySymbol underlying)
        {
            Debug.Assert(underlying is object);
            _underlying = underlying;
        }

        internal override CSharp.Symbol UnderlyingSymbol => _underlying;

        bool IPropertySymbol.IsIndexer
        {
            get { return _underlying.IsIndexer; }
        }

        ITypeSymbol IPropertySymbol.Type
        {
            get
            {
                if (_lazyType is null)
                {
                    Interlocked.CompareExchange(ref _lazyType, _underlying.TypeWithAnnotations.GetPublicSymbol(), null);
                }

                return _lazyType;
            }
        }

        CodeAnalysis.NullableAnnotation IPropertySymbol.NullableAnnotation => _underlying.TypeWithAnnotations.ToPublicAnnotation();

        ImmutableArray<IParameterSymbol> IPropertySymbol.Parameters
        {
            get { return _underlying.Parameters.GetPublicSymbols(); }
        }

        IMethodSymbol IPropertySymbol.GetMethod
        {
            get { return _underlying.GetMethod.GetPublicSymbol(); }
        }

        IMethodSymbol IPropertySymbol.SetMethod
        {
            get { return _underlying.SetMethod.GetPublicSymbol(); }
        }

        IPropertySymbol IPropertySymbol.OriginalDefinition
        {
            get
            {
                return _underlying.OriginalDefinition.GetPublicSymbol();
            }
        }

        IPropertySymbol IPropertySymbol.OverriddenProperty
        {
            get { return _underlying.OverriddenProperty.GetPublicSymbol(); }
        }

        ImmutableArray<IPropertySymbol> IPropertySymbol.ExplicitInterfaceImplementations
        {
            get { return _underlying.ExplicitInterfaceImplementations.GetPublicSymbols(); }
        }

        bool IPropertySymbol.IsReadOnly
        {
            get { return _underlying.IsReadOnly; }
        }

        bool IPropertySymbol.IsWriteOnly
        {
            get { return _underlying.IsWriteOnly; }
        }

        bool IPropertySymbol.IsWithEvents
        {
            get { return false; }
        }

        bool IPropertySymbol.IsRequired => _underlying.IsRequired;

        ImmutableArray<CustomModifier> IPropertySymbol.TypeCustomModifiers
        {
            get { return _underlying.TypeWithAnnotations.CustomModifiers; }
        }

        ImmutableArray<CustomModifier> IPropertySymbol.RefCustomModifiers
        {
            get { return _underlying.RefCustomModifiers; }
        }

        bool IPropertySymbol.ReturnsByRef => _underlying.ReturnsByRef;

        bool IPropertySymbol.ReturnsByRefReadonly => _underlying.ReturnsByRefReadonly;

        RefKind IPropertySymbol.RefKind => _underlying.RefKind;

#nullable enable
        IPropertySymbol? IPropertySymbol.PartialDefinitionPart => _underlying.PartialDefinitionPart.GetPublicSymbol();

        IPropertySymbol? IPropertySymbol.PartialImplementationPart => _underlying.PartialImplementationPart.GetPublicSymbol();

        bool IPropertySymbol.IsPartialDefinition => (_underlying as SourcePropertySymbol)?.IsPartialDefinition ?? false;

        IPropertySymbol? IPropertySymbol.ReduceExtensionMember(ITypeSymbol receiverType)
        {
            if (_underlying.IsExtensionBlockMember() && SourceMemberContainerTypeSymbol.IsAllowedExtensionMember(_underlying, LanguageVersion.Preview))
            {
                var csharpReceiver = receiverType.EnsureCSharpSymbolOrNull(nameof(receiverType));
                return (IPropertySymbol?)SourceNamedTypeSymbol.ReduceExtensionMember(compilation: null, _underlying, csharpReceiver, wasExtensionFullyInferred: out _).GetPublicSymbol();
            }

            return null;
        }
#nullable disable

        #region ISymbol Members

        protected override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitProperty(this);
        }

        protected override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitProperty(this);
        }

        protected override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitProperty(this, argument);
        }

        #endregion
    }
}
