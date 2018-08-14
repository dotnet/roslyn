// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.IntroduceVariable
{
    internal partial class AbstractIntroduceVariableService<TService, TExpressionSyntax, TTypeSyntax, TTypeDeclarationSyntax, TQueryExpressionSyntax, TNameSyntax>
    {
        private class IntroduceVariableCodeAction : AbstractIntroduceVariableCodeAction
        {
            internal IntroduceVariableCodeAction(
                TService service,
                SemanticDocument document,
                TExpressionSyntax expression,
                bool allOccurrences,
                bool isConstant,
                bool isLocal,
                bool isQueryLocal)
                : base(service, document, expression, allOccurrences, isConstant, isLocal, isQueryLocal)
            {
            }
        }
    }
}
