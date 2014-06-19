' ==++==
'
' Copyright (c) Microsoft Corporation. All rights reserved.
'
' ==--==

Namespace Roslyn.Compilers.VisualBasic

    Friend NotInheritable Class DelegateRewriter
        Inherits BoundTreeRewriter

        Private Shared ReadOnly Instance As New DelegateRewriter()

        Private Sub New()
        End Sub

        Public Shared Function Rewrite(node As BoundBlock) As BoundBlock
            Debug.Assert(node IsNot Nothing)
            Dim result = Instance.Visit(node)
            Return DirectCast(result, BoundBlock)
        End Function


        Public Overrides Function VisitDelegateCreationExpression(node As BoundDelegateCreationExpression) As BoundNode

            ' if there is a stub needed because of a delegate relaxation, the DelegateCreationNode has
            ' it stored in RelaxationLambdaOpt.
            ' The lambda rewriter will then take care of the code generation later on.

            Dim relaxationLambda = node.RelaxationLambdaOpt
            If relaxationLambda Is Nothing Then
                Return node
            Else
                Return New BoundConversion(relaxationLambda.Syntax,
                                           relaxationLambda.SyntaxTree,
                                           relaxationLambda,
                                           relaxationLambda.ConversionKind,
                                           checked:=False,
                                           explicitCastInCode:=False,
                                           Type:=node.Type,
                                           hasErrors:=node.HasErrors)
            End If
        End Function
    End Class

End Namespace
