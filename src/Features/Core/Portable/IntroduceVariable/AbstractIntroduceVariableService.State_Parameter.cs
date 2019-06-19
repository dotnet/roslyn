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
