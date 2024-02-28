// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.IntroduceVariable;

internal partial class AbstractIntroduceVariableService<TService, TExpressionSyntax, TTypeSyntax, TTypeDeclarationSyntax, TQueryExpressionSyntax, TNameSyntax>
{
    private partial class State
    {
        private bool IsInAttributeContext()
        {
            if (!_service.IsInAttributeArgumentInitializer(Expression))
            {
                return false;
            }

            // Have to make sure we're on or inside a type decl so that we have some place to
            // put the result.
            return IsInTypeDeclarationOrValidCompilationUnit();
        }
    }
}
