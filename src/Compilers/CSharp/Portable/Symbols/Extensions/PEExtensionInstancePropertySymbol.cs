// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class PEExtensionInstancePropertySymbol : RewrittenExtensionPropertySymbol
    {
        private readonly PEExtensionInstanceMethodSymbol? _getMethod;
        private readonly PEExtensionInstanceMethodSymbol? _setMethod;

        public PEExtensionInstancePropertySymbol(PEPropertySymbol metadataProperty, PEExtensionInstanceMethodSymbol? getMethod, PEExtensionInstanceMethodSymbol? setMethod) : base(metadataProperty)
        {
            Debug.Assert(metadataProperty.IsStatic);
            _getMethod = getMethod;
            _setMethod = setMethod;
        }

        public override bool IsStatic => false;
        public override bool RequiresInstanceReceiver => true;

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get
            {
                return UnderlyingProperty.CallingConvention | Cci.CallingConvention.HasThis;
            }
        }

        public override MethodSymbol? GetMethod => _getMethod;

        public override MethodSymbol? SetMethod => _setMethod;

        public override bool IsIndexedProperty => (this.ParameterCount > 0) && ContainingType.IsComImport;

        protected override ImmutableArray<ParameterSymbol> MakeParameters()
        {
            var sourceParameters = UnderlyingProperty.Parameters;
            var parameters = ArrayBuilder<ParameterSymbol>.GetInstance(sourceParameters.Length - 1);

            foreach (var parameter in sourceParameters[1..])
            {
                parameters.Add(new PEExtensionInstancePropertyParameterSymbol(this, parameter));
            }

            return parameters.ToImmutableAndFree();
        }

        private sealed class PEExtensionInstancePropertyParameterSymbol : RewrittenParameterSymbol
        {
            private readonly PEExtensionInstancePropertySymbol _containingProperty;

            public PEExtensionInstancePropertyParameterSymbol(PEExtensionInstancePropertySymbol containingProperty, ParameterSymbol sourceParameter) :
                base(sourceParameter)
            {
                _containingProperty = containingProperty;
            }

            public sealed override Symbol ContainingSymbol
            {
                get { return _containingProperty; }
            }

            public override int Ordinal
            {
                get { return this._underlyingParameter.Ordinal - 1; }
            }
        }
    }
}
