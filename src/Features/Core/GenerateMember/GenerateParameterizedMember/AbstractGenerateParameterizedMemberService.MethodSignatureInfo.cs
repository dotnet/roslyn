// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
{
    internal partial class AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
    {
        protected class MethodSignatureInfo : SignatureInfo
        {
            private readonly IMethodSymbol methodSymbol;

            public MethodSignatureInfo(
                SemanticDocument document,
                State state,
                IMethodSymbol methodSymbol)
                : base(document, state)
            {
                this.methodSymbol = methodSymbol;
            }

            protected override ITypeSymbol DetermineReturnTypeWorker(CancellationToken cancellationToken)
            {
                if (State.IsInConditionalAccessExpression)
                {
                    return methodSymbol.ReturnType.RemoveNullableIfPresent();
                }

                return methodSymbol.ReturnType;
            }

            public override IList<ITypeParameterSymbol> DetermineTypeParameters(CancellationToken cancellationToken)
            {
                return methodSymbol.TypeParameters;
            }

            protected override IList<RefKind> DetermineParameterModifiers(CancellationToken cancellationToken)
            {
                return methodSymbol.Parameters.Select(p => p.RefKind).ToList();
            }

            protected override IList<bool> DetermineParameterOptionality(CancellationToken cancellationToken)
            {
                return methodSymbol.Parameters.Select(p => p.IsOptional).ToList();
            }

            protected override IList<ITypeSymbol> DetermineParameterTypes(CancellationToken cancellationToken)
            {
                return methodSymbol.Parameters.Select(p => p.Type).ToList();
            }

            protected override IList<string> DetermineParameterNames(CancellationToken cancellationToken)
            {
                return methodSymbol.Parameters.Select(p => p.Name).ToList();
            }
        }
    }
}
