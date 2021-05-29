// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.IntroduceVariable
{
    internal partial class AbstractIntroduceVariableService<TService, TExpressionSyntax, TTypeSyntax, TTypeDeclarationSyntax, TQueryExpressionSyntax, TNameSyntax>
    {
        private partial class State
        {
            private bool IsInQueryContext(
                CancellationToken cancellationToken)
            {
                if (!_service.IsInNonFirstQueryClause(Expression))
                {
                    return false;
                }

                var semanticMap = GetSemanticMap(cancellationToken);
                if (!semanticMap.AllReferencedSymbols.Any(s => s is IRangeVariableSymbol))
                {
                    return false;
                }

                var info = Document.SemanticModel.GetTypeInfo(Expression, cancellationToken);
                if (info.Type == null || info.Type.SpecialType == SpecialType.System_Void)
                {
                    return false;
                }

                return true;
            }
        }
    }
}
