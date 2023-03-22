' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.UseConditionalExpression
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseConditionalExpression
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseConditionalExpressionForReturnDiagnosticAnalyzer
        Inherits AbstractUseConditionalExpressionForReturnDiagnosticAnalyzer(Of MultiLineIfBlockSyntax)

        Public Sub New()
            MyBase.New(New LocalizableResourceString(NameOf(VisualBasicAnalyzersResources.If_statement_can_be_simplified), VisualBasicAnalyzersResources.ResourceManager, GetType(VisualBasicAnalyzersResources)))
        End Sub

        Protected Overrides Function GetSyntaxFacts() As ISyntaxFacts
            Return VisualBasicSyntaxFacts.Instance
        End Function
    End Class
End Namespace
