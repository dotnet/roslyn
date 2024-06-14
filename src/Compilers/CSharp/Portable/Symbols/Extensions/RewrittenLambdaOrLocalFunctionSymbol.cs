// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class RewrittenLambdaOrLocalFunctionSymbol : RewrittenMethodSymbol
    {
        private readonly RewrittenMethodSymbol _containingMethod;

        public RewrittenLambdaOrLocalFunctionSymbol(MethodSymbol lambdaOrLocalFunctionSymbol, RewrittenMethodSymbol containingMethod) : base(lambdaOrLocalFunctionSymbol, containingMethod.TypeMap)
        {
            Debug.Assert(lambdaOrLocalFunctionSymbol.AssociatedSymbol is null);
            Debug.Assert(lambdaOrLocalFunctionSymbol.TryGetThisParameter(out var thisParameter) && thisParameter is null);
            _containingMethod = containingMethod;
        }

        public override Symbol? AssociatedSymbol => null;

        public override Symbol ContainingSymbol => _containingMethod;

        internal override bool TryGetThisParameter(out ParameterSymbol? thisParameter)
        {
            thisParameter = null;
            return true;
        }

        protected override ImmutableArray<ParameterSymbol> MakeParameters()
        {
            var sourceParameters = _originalMethod.Parameters;
            var parameters = ArrayBuilder<ParameterSymbol>.GetInstance(ParameterCount);

            foreach (var parameter in sourceParameters)
            {
                parameters.Add(new RewrittenMethodParameterSymbol(this, parameter));
            }

            return ImmutableArray<ParameterSymbol>.CastUp(_originalMethod.Parameters.SelectAsArray(static (p, @this) => new RewrittenMethodParameterSymbol(@this, p), this));
        }
    }
}
