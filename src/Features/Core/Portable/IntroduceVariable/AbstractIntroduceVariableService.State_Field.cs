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
            private bool IsInFieldContext(
                CancellationToken cancellationToken)
            {
                // Note: if we're in a lambda that has a block body, then we don't ever get here
                // because of the early check for IsInBlockContext.
                if (!_service.IsInFieldInitializer(this.Expression))
                {
                    return false;
                }

                if (!IsInTypeDeclarationOrValidCompilationUnit())
                {
                    return false;
                }

                // if the expression in the field references any parameters then that means it was
                // either an expression inside a lambda in the field, or it was an expression in a
                // query inside the field.  Either of which cannot be extracted out further by this
                // fix.
                var bindingMap = GetSemanticMap(cancellationToken);
                if (bindingMap.AllReferencedSymbols.OfType<IParameterSymbol>().Any())
                {
                    return false;
                }

                // Can't extract out an anonymous type used in a field initializer.
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
