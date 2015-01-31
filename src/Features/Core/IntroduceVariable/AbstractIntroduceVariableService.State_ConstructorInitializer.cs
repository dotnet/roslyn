// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.IntroduceVariable
{
    internal partial class AbstractIntroduceVariableService<TService, TExpressionSyntax, TTypeSyntax, TTypeDeclarationSyntax, TQueryExpressionSyntax>
    {
        private partial class State
        {
            private bool IsInConstructorInitializerContext(
                CancellationToken cancellationToken)
            {
                // Note: if we're in a lambda that has a block body, then we don't ever get here
                // because of the early check for IsInBlockContext.
                if (!_service.IsInConstructorInitializer(this.Expression))
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
                var info = this.Document.SemanticModel.GetTypeInfo(this.Expression, cancellationToken);
                if (info.Type.ContainsAnonymousType())
                {
                    return false;
                }

                return true;
            }
        }
    }
}
