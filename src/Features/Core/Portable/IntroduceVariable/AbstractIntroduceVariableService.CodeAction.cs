// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeCleanup;

namespace Microsoft.CodeAnalysis.IntroduceVariable;

internal partial class AbstractIntroduceVariableService<TService, TExpressionSyntax, TTypeSyntax, TTypeDeclarationSyntax, TQueryExpressionSyntax, TNameSyntax>
{
    private sealed class IntroduceVariableCodeAction(
        TService service,
        SemanticDocument document,
        CodeCleanupOptions options,
        TExpressionSyntax expression,
        bool allOccurrences,
        bool isConstant,
        bool isLocal,
        bool isQueryLocal) : AbstractIntroduceVariableCodeAction(
            service, document, options, expression, allOccurrences, isConstant, isLocal, isQueryLocal)
    {
    }
}
