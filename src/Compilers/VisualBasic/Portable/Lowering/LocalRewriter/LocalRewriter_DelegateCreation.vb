' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter

        Public Overrides Function VisitDelegateCreationExpression(node As BoundDelegateCreationExpression) As BoundNode

            ' if there is a stub needed because of a delegate relaxation, the DelegateCreationNode has
            ' it stored in RelaxationLambdaOpt.
            ' The lambda rewriter will then take care of the code generation later on.

            If node.RelaxationLambdaOpt Is Nothing Then
                Debug.Assert(node.RelaxationReceiverPlaceholderOpt Is Nothing OrElse Me._inExpressionLambda)
                Return MyBase.VisitDelegateCreationExpression(node)

            Else

                Dim placeholderOpt As BoundRValuePlaceholder = node.RelaxationReceiverPlaceholderOpt
                Dim captureTemp As SynthesizedLocal = Nothing

                If placeholderOpt IsNot Nothing Then
                    If Me._inExpressionLambda Then
                        Me.AddPlaceholderReplacement(placeholderOpt, VisitExpression(node.ReceiverOpt))
                    Else
                        captureTemp = New SynthesizedLocal(Me._currentMethodOrLambda, placeholderOpt.Type, SynthesizedLocalKind.DelegateRelaxationReceiver, syntaxOpt:=placeholderOpt.Syntax)
                        Dim actualReceiver = New BoundLocal(placeholderOpt.Syntax, captureTemp, captureTemp.Type).MakeRValue
                        Me.AddPlaceholderReplacement(placeholderOpt, actualReceiver)
                    End If
                End If

                Dim relaxationLambda = DirectCast(Me.Visit(node.RelaxationLambdaOpt), BoundLambda)

                If placeholderOpt IsNot Nothing Then
                    Me.RemovePlaceholderReplacement(placeholderOpt)
                End If

                Dim result As BoundExpression = New BoundConversion(
                                           relaxationLambda.Syntax,
                                           relaxationLambda,
                                           ConversionKind.Lambda Or ConversionKind.Widening,
                                           checked:=False,
                                           explicitCastInCode:=False,
                                           Type:=node.Type,
                                           hasErrors:=node.HasErrors)

                If captureTemp IsNot Nothing Then
                    Dim receiverToCapture As BoundExpression = VisitExpressionNode(node.ReceiverOpt)

                    Dim capture = New BoundAssignmentOperator(receiverToCapture.Syntax,
                                        New BoundLocal(receiverToCapture.Syntax,
                                                       captureTemp, captureTemp.Type),
                                        receiverToCapture.MakeRValue(),
                                        suppressObjectClone:=True,
                                        Type:=captureTemp.Type)

                    result = New BoundSequence(
                                         result.Syntax,
                                         ImmutableArray.Create(Of LocalSymbol)(captureTemp),
                                         ImmutableArray.Create(Of BoundExpression)(capture),
                                         result,
                                         result.Type)
                End If

                Return result
            End If
        End Function
    End Class
End Namespace
