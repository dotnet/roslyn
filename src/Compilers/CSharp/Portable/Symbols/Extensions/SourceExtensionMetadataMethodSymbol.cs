// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceExtensionMetadataMethodSymbol : RewrittenExtensionMethodSymbol
    {
        public SourceExtensionMetadataMethodSymbol(MethodSymbol sourceMethod) : base(sourceMethod)
        {
            Debug.Assert(!sourceMethod.IsStatic);
            Debug.Assert(sourceMethod.ContainingSymbol is SourceExtensionTypeSymbol);
        }

        internal override int ParameterCount => UnderlyingMethod.ParameterCount + 1;
        public override bool IsStatic => true;
        public override bool RequiresInstanceReceiver => false;

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get
            {
                return _originalMethod.CallingConvention & (~Cci.CallingConvention.HasThis);
            }
        }

        public override Symbol? AssociatedSymbol
        {
            get
            {
                if (_originalMethod.AssociatedSymbol is Symbol associatedSymbol)
                {
                    return ((SourceExtensionTypeSymbol)_originalMethod.ContainingSymbol).TryGetCorrespondingStaticMetadataExtensionMember(associatedSymbol);
                }

                return null;
            }
        }

        protected override ImmutableArray<ParameterSymbol> MakeParameters()
        {
            var sourceParameters = _originalMethod.Parameters;
            var parameters = ArrayBuilder<ParameterSymbol>.GetInstance(ParameterCount);

            // PROTOTYPE(roles): Need to confirm if this rewrite going to break LocalStateTracingInstrumenter
            //                   Specifically BoundParameterId, etc.   
            parameters.Add(new SynthesizedExtensionThisParameterMetadataSymbol(this));

            foreach (var parameter in sourceParameters)
            {
                parameters.Add(new ExtensionMetadataMethodParameterSymbol(this, parameter));
            }

            return parameters.ToImmutableAndFree();
        }

        internal override bool TryGetThisParameter(out ParameterSymbol? thisParameter)
        {
            thisParameter = null;
            return true;
        }

        private sealed class ExtensionMetadataMethodParameterSymbol : RewrittenMethodParameterSymbol
        {
            public ExtensionMetadataMethodParameterSymbol(SourceExtensionMetadataMethodSymbol containingMethod, ParameterSymbol sourceParameter) :
                base(containingMethod, sourceParameter)
            {
            }

            public override int Ordinal
            {
                get { return this._underlyingParameter.Ordinal + 1; }
            }
        }
    }
}
