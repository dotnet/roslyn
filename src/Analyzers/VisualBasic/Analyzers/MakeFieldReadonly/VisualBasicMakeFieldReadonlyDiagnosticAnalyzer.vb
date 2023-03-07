' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.MakeFieldReadonly
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.MakeFieldReadonly
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicMakeFieldReadonlyDiagnosticAnalyzer
        Inherits AbstractMakeFieldReadonlyDiagnosticAnalyzer(Of SyntaxKind, MeExpressionSyntax)

        Protected Overrides ReadOnly Property SyntaxKinds As ISyntaxKinds = VisualBasicSyntaxKinds.Instance

        Protected Overrides Function IsWrittenTo(semanticModel As SemanticModel, expression As MeExpressionSyntax, cancellationToken As CancellationToken) As Boolean
            Return expression.IsWrittenTo(semanticModel, cancellationToken)
        End Function
    End Class
End Namespace
