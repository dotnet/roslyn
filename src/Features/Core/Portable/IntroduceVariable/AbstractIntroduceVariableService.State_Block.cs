// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.IntroduceVariable;

internal abstract partial class AbstractIntroduceVariableService<TService, TExpressionSyntax, TTypeSyntax, TTypeDeclarationSyntax, TQueryExpressionSyntax, TNameSyntax>
{
    private sealed partial class State
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
