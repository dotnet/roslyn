' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.UseConditionalExpression
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseConditionalExpression
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseConditionalExpressionForReturnDiagnosticAnalyzer
        Inherits AbstractUseConditionalExpressionForReturnDiagnosticAnalyzer(Of MultiLineIfBlockSyntax)

        Public Sub New()
            MyBase.New(New LocalizableResourceString(NameOf(VBFeaturesResources.If_statement_can_be_simplified), VBFeaturesResources.ResourceManager, GetType(VBFeaturesResources.VBFeaturesResources)))
        End Sub

        Protected Overrides Function GetSyntaxFactsService() As ISyntaxFactsService
            Return VisualBasicSyntaxFactsService.Instance
        End Function
    End Class
End Namespace
