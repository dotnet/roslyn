' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.UseConditionalExpression
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseConditionalExpression
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseConditionalExpressionForAssignmentDiagnosticAnalyzer
        Inherits AbstractUseConditionalExpressionForAssignmentDiagnosticAnalyzer(Of MultiLineIfBlockSyntax)

        Public Sub New()
            MyBase.New(New LocalizableResourceString(NameOf(VBFeaturesResources.If_statement_can_be_simplified), VBFeaturesResources.ResourceManager, GetType(VBFeaturesResources.VBFeaturesResources)))
        End Sub

        Protected Overrides Function GetSyntaxFactsService() As ISyntaxFactsService
            Return VisualBasicSyntaxFactsService.Instance
        End Function
    End Class
End Namespace
