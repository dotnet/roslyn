// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class ReceiverParameterSymbol : RewrittenParameterSymbol
    {
        private readonly NamedTypeSymbol _containingType;

        public ReceiverParameterSymbol(NamedTypeSymbol containingType, ParameterSymbol originalParameter) :
            base(originalParameter)
        {
            _containingType = containingType;
        }

        public override Symbol ContainingSymbol
        {
            get { return _containingType; }
        }

        internal override bool HasEnumeratorCancellationAttribute
        {
            get { return _underlyingParameter.HasEnumeratorCancellationAttribute; }
        }
    }
}
