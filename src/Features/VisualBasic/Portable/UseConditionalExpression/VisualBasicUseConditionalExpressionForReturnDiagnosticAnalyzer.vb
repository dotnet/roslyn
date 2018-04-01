' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.UseConditionalExpression

Namespace Microsoft.CodeAnalysis.VisualBasic.UseConditionalExpression
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseConditionalExpressionForReturnDiagnosticAnalyzer
        Inherits AbstractUseConditionalExpressionForReturnDiagnosticAnalyzer(Of SyntaxKind)

        Public Sub New()
            MyBase.New(New LocalizableResourceString(NameOf(VBFeaturesResources.If_statement_can_be_simplified), VBFeaturesResources.ResourceManager, GetType(VBFeaturesResources.VBFeaturesResources)))
        End Sub

        Protected Overrides Function GetIfStatementKinds() As ImmutableArray(Of SyntaxKind)
            Return ImmutableArray.Create(SyntaxKind.MultiLineIfBlock)
        End Function
    End Class
End Namespace
