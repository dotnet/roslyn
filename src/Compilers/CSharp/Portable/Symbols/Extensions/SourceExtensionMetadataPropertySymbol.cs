// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceExtensionMetadataPropertySymbol : RewrittenExtensionPropertySymbol
    {
        public SourceExtensionMetadataPropertySymbol(PropertySymbol sourceProperty) : base(sourceProperty)
        {
            Debug.Assert(!sourceProperty.IsStatic);
            Debug.Assert(!sourceProperty.MustCallMethodsDirectly);
            Debug.Assert(sourceProperty.ContainingSymbol is SourceExtensionTypeSymbol);
        }

        public override bool IsStatic => true;
        public override bool RequiresInstanceReceiver => false;

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get
            {
                return UnderlyingProperty.CallingConvention & (~Cci.CallingConvention.HasThis);
            }
        }

        public override MethodSymbol? GetMethod
        {
            get
            {
                if (UnderlyingProperty.GetMethod is { } accessor)
                {
                    return (MethodSymbol?)ContainingType.TryGetCorrespondingStaticMetadataExtensionMember(accessor);
                }

                return null;
            }
        }

        public override MethodSymbol? SetMethod
        {
            get
            {
                if (UnderlyingProperty.SetMethod is { } accessor)
                {
                    return (MethodSymbol?)ContainingType.TryGetCorrespondingStaticMetadataExtensionMember(accessor);
                }

                return null;
            }
        }

        protected override ImmutableArray<ParameterSymbol> MakeParameters()
        {
            var sourceParameters = UnderlyingProperty.Parameters;
            var parameters = ArrayBuilder<ParameterSymbol>.GetInstance(sourceParameters.Length + 1);

            parameters.Add(new SynthesizedExtensionThisParameterMetadataSymbol(this));

            foreach (var parameter in sourceParameters)
            {
                parameters.Add(new ExtensionMetadataPropertyParameterSymbol(this, parameter));
            }

            return parameters.ToImmutableAndFree();
        }

        private sealed class ExtensionMetadataPropertyParameterSymbol : RewrittenParameterSymbol
        {
            private readonly SourceExtensionMetadataPropertySymbol _containingProperty;

            public ExtensionMetadataPropertyParameterSymbol(SourceExtensionMetadataPropertySymbol containingProperty, ParameterSymbol sourceParameter) :
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
                get { return this._underlyingParameter.Ordinal + 1; }
            }
        }
    }
}
