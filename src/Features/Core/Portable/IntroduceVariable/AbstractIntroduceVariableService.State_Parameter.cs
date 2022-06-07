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
            private bool IsInParameterContext(
                CancellationToken cancellationToken)
            {
                if (!_service.IsInParameterInitializer(Expression))
                {
                    return false;
                }

                // The default value for a parameter is a constant.  So we always allow it unless it
                // happens to capture one of the method's type parameters.
                var bindingMap = GetSemanticMap(cancellationToken);
                if (bindingMap.AllReferencedSymbols.OfType<ITypeParameterSymbol>()
                                                    .Where(tp => tp.TypeParameterKind == TypeParameterKind.Method)
                                                    .Any())
                {
                    return false;
                }

                return true;
            }
        }
    }
}
