// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class PEExtensionInstanceMethodSymbol : RewrittenExtensionMethodSymbol
    {
        private ThisParameterSymbol? _lazyThisParameter;
        private Symbol? _associatedPropertyOrEventOpt;

        public PEExtensionInstanceMethodSymbol(PEMethodSymbol metadataMethod) : base(metadataMethod)
        {
            Debug.Assert(metadataMethod.IsStatic);
        }

        internal override int ParameterCount => UnderlyingMethod.ParameterCount - 1;
        public override bool IsStatic => false;
        public override bool RequiresInstanceReceiver => true;

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get
            {
                return _originalMethod.CallingConvention | Cci.CallingConvention.HasThis;
            }
        }

        public override Symbol? AssociatedSymbol => _associatedPropertyOrEventOpt;

        internal void SetAssociatedProperty(PEExtensionInstancePropertySymbol propertySymbol)
        {
            this.SetAssociatedPropertyOrEvent(propertySymbol);
        }

        internal void SetAssociatedEvent(PEExtensionInstanceEventSymbol eventSymbol)
        {
            this.SetAssociatedPropertyOrEvent(eventSymbol);
        }

        private void SetAssociatedPropertyOrEvent(Symbol propertyOrEventSymbol)
        {
            if (_associatedPropertyOrEventOpt is null)
            {
                Debug.Assert((object)propertyOrEventSymbol.ContainingType == ContainingType);

                // No locking required since SetAssociatedProperty/SetAssociatedEvent will only be called
                // by the thread that created the method symbol (and will be called before the method
                // symbol is added to the containing type members and available to other threads).
                _associatedPropertyOrEventOpt = propertyOrEventSymbol;
            }
        }

        protected override ImmutableArray<ParameterSymbol> MakeParameters()
        {
            var sourceParameters = _originalMethod.Parameters;
            var parameters = ArrayBuilder<ParameterSymbol>.GetInstance(ParameterCount);

            foreach (var parameter in sourceParameters[1..])
            {
                parameters.Add(new PEExtensionInstanceMethodParameterSymbol(this, parameter));
            }

            return parameters.ToImmutableAndFree();
        }

        internal override bool TryGetThisParameter(out ParameterSymbol? thisParameter)
        {
            thisParameter = _lazyThisParameter ?? InterlockedOperations.Initialize(ref _lazyThisParameter, new ThisParameterSymbol(this));
            return true;
        }

        private sealed class PEExtensionInstanceMethodParameterSymbol : RewrittenMethodParameterSymbol
        {
            public PEExtensionInstanceMethodParameterSymbol(PEExtensionInstanceMethodSymbol containingMethod, ParameterSymbol sourceParameter) :
                base(containingMethod, sourceParameter)
            {
            }

            public override int Ordinal
            {
                get { return this._underlyingParameter.Ordinal - 1; }
            }
        }
    }
}
