// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
