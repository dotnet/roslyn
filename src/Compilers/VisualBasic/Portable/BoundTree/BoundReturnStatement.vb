' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundReturnStatement

        Friend Function IsEndOfMethodReturn() As Boolean
            Return Me.ExitLabelOpt is nothing
        End Function

#If DEBUG Then
        Private Sub Validate()
            If ExpressionOpt IsNot Nothing AndAlso Not HasErrors Then
                If FunctionLocalOpt Is Nothing OrElse FunctionLocalOpt.Type IsNot LambdaSymbol.ReturnTypeIsBeingInferred Then
                    ExpressionOpt.AssertRValue()
                End If
            End If
        End Sub
#End If

    End Class

End Namespace
