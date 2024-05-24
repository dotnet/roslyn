// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember;

internal partial class AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
{
    protected class MethodSignatureInfo(
        SemanticDocument document,
        State state,
        IMethodSymbol methodSymbol,
        ImmutableArray<string> parameterNames = default) : SignatureInfo(document, state)
    {
        private readonly IMethodSymbol _methodSymbol = methodSymbol;
        private readonly ImmutableArray<string> _parameterNames = parameterNames;

        protected override ITypeSymbol DetermineReturnTypeWorker(CancellationToken cancellationToken)
            => _methodSymbol.ReturnType;

        protected override RefKind DetermineRefKind(CancellationToken cancellationToken)
            => _methodSymbol.RefKind;

        protected override ImmutableArray<ITypeParameterSymbol> DetermineTypeParametersWorker(CancellationToken cancellationToken)
            => _methodSymbol.TypeParameters;

        protected override ImmutableArray<RefKind> DetermineParameterModifiers(CancellationToken cancellationToken)
            => _methodSymbol.Parameters.SelectAsArray(p => p.RefKind);

        protected override ImmutableArray<bool> DetermineParameterOptionality(CancellationToken cancellationToken)
            => _methodSymbol.Parameters.SelectAsArray(p => p.IsOptional);

        protected override ImmutableArray<ITypeSymbol> DetermineParameterTypes(CancellationToken cancellationToken)
            => _methodSymbol.Parameters.SelectAsArray(p => p.Type);

        protected override ImmutableArray<ParameterName> DetermineParameterNames(CancellationToken cancellationToken)
            => _parameterNames.IsDefault
                ? _methodSymbol.Parameters.SelectAsArray(p => new ParameterName(p.Name, isFixed: true))
                : _parameterNames.SelectAsArray(p => new ParameterName(p, isFixed: true));

        protected override ImmutableArray<ITypeSymbol> DetermineTypeArguments(CancellationToken cancellationToken)
            => [];
    }
}
