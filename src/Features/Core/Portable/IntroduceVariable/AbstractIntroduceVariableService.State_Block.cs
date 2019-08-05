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
            private bool IsInBlockContext(
                CancellationToken cancellationToken)
            {
                if (!IsInTypeDeclarationOrValidCompilationUnit())
                {
                    return false;
                }

                // If refer to a query property, then we use the query context instead.
                var bindingMap = GetSemanticMap(cancellationToken);
                if (bindingMap.AllReferencedSymbols.Any(s => s is IRangeVariableSymbol))
                {
                    return false;
                }

                var type = GetTypeSymbol(Document, Expression, cancellationToken, objectAsDefault: false);
                if (type == null || type.SpecialType == SpecialType.System_Void)
                {
                    return false;
                }

                return true;
            }
        }
    }
}
