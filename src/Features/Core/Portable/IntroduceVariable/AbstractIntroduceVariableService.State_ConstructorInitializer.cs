// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.IntroduceVariable;

internal abstract partial class AbstractIntroduceVariableService<TService, TExpressionSyntax, TTypeSyntax, TTypeDeclarationSyntax, TQueryExpressionSyntax, TNameSyntax>
{
    private sealed partial class State
    {
        private bool IsInConstructorInitializerContext(
            CancellationToken cancellationToken)
        {
            // Note: if we're in a lambda that has a block body, then we don't ever get here
            // because of the early check for IsInBlockContext.
            if (!_service.IsInConstructorInitializer(Expression))
            {
                return false;
            }

            var bindingMap = GetSemanticMap(cancellationToken);

            // Can't extract out if a parameter is referenced.
            if (bindingMap.AllReferencedSymbols.OfType<IParameterSymbol>().Any())
            {
                return false;
            }

            // Can't extract out an anonymous type used in a constructor initializer.
            var info = Document.SemanticModel.GetTypeInfo(Expression, cancellationToken);
            if (info.Type.ContainsAnonymousType())
            {
                return false;
            }

            return true;
        }
    }
}
