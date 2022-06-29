// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseNullPropagation;

internal abstract partial class AbstractUseNullPropagationDiagnosticAnalyzer<
    TSyntaxKind,
    TExpressionSyntax,
    TStatementSyntax,
    TConditionalExpressionSyntax,
    TBinaryExpressionSyntax,
    TInvocationExpressionSyntax,
    TMemberAccessExpressionSyntax,
    TConditionalAccessExpressionSyntax,
    TElementAccessExpressionSyntax,
    TIfStatementSyntax>
{
    private void AnalyzeIfStatement(
        SyntaxNodeAnalysisContext context,
        IMethodSymbol? referenceEqualsMethod)
    {

    }
}
