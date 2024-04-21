' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.UseInterpolatedString
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseInterpolatedString
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicUseInterpolatedStringDiagnosticAnalyzer
        Inherits AbstractUseInterpolatedStringDiagnosticAnalyzer (Of SyntaxKind, ExpressionSyntax, LiteralExpressionSyntax)

        Public Sub New()
            MyBase.New()
        End Sub

        Protected Overrides Function GetSyntaxFacts() As ISyntaxFacts
            Return VisualBasicSyntaxFacts.Instance
        End Function
 
        Protected Overrides Function CanConvertToInterpolatedString(literalExpression As LiteralExpressionSyntax) As Boolean
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
