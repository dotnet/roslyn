' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.UseCoalesceExpression
Imports Microsoft.CodeAnalysis.VisualBasic.UseCoalesceExpression

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.UseCoalesceExpression.VisualBasicUseCoalesceExpressionForIfNullStatementCheckDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.UseCoalesceExpression.UseCoalesceExpressionForIfNullStatementCheckCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UseCoalesceExpression
    <Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)>
    Public Class UseCoalesceExpressionForIfNullStatementCheckTests

    End Class
End Namespace
