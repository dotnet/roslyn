' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Enum ResumeStatementKind As Byte
        Plain ' Resume
        [Next] ' Resume Next
        Label  ' Resume Label
    End Enum

    Friend Partial Class BoundResumeStatement

        Public Sub New(syntax As VisualBasicSyntaxNode, Optional isNext As Boolean = False)
            Me.New(syntax, If(isNext, ResumeStatementKind.Next, ResumeStatementKind.Plain), Nothing, Nothing)
        End Sub

        Public Sub New(syntax As VisualBasicSyntaxNode, label As LabelSymbol, labelExpressionOpt As BoundExpression, Optional hasErrors As Boolean = False)
            Me.New(syntax, ResumeStatementKind.Label, label, labelExpressionOpt, hasErrors)
            Debug.Assert(labelExpressionOpt IsNot Nothing)
        End Sub

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert((Me.ResumeKind = ResumeStatementKind.Label) = Not (Me.LabelOpt Is Nothing AndAlso Me.LabelExpressionOpt Is Nothing))
            Debug.Assert(Me.LabelExpressionOpt Is Nothing OrElse Me.LabelOpt IsNot Nothing OrElse Me.HasErrors)
        End Sub
#End If

    End Class

End Namespace
