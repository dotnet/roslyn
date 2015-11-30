' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class AsyncRewriter
        Inherits StateMachineRewriter(Of CapturedSymbolOrExpression)

        Partial Friend Class AsyncMethodToClassRewriter
            Inherits StateMachineMethodToClassRewriter

            Public Overrides Function VisitReturnStatement(node As BoundReturnStatement) As BoundNode
                Dim rewritten = DirectCast(MyBase.VisitReturnStatement(node), BoundReturnStatement)
                Dim expression As BoundExpression = rewritten.ExpressionOpt

                If expression IsNot Nothing Then
                    Debug.Assert(_asyncMethodKind = AsyncMethodKind.GenericTaskFunction)

                    If expression.Kind = BoundKind.SpillSequence Then
                        Dim spill = DirectCast(expression, BoundSpillSequence)
                        Debug.Assert(spill.ValueOpt IsNot Nothing)
                        Return F.Block(
                                    RewriteSpillSequenceIntoBlock(
                                        spill,
                                        False,
                                        F.Assignment(F.Local(_exprRetValue, True), spill.ValueOpt)),
                                    F.Goto(_exprReturnLabel))
                    Else
                        Return F.Block(
                                    F.Assignment(
                                        F.Local(_exprRetValue, True), expression),
                                    F.Goto(_exprReturnLabel))
                    End If
                End If

                Return F.Goto(_exprReturnLabel)
            End Function

            Public Overrides Function VisitExpressionStatement(node As BoundExpressionStatement) As BoundNode
                Dim rewritten = DirectCast(MyBase.VisitExpressionStatement(node), BoundExpressionStatement)
                Dim expression As BoundExpression = rewritten.Expression

                If expression.Kind <> BoundKind.SpillSequence Then
                    Return rewritten
                End If

                Return RewriteSpillSequenceIntoBlock(DirectCast(expression, BoundSpillSequence), True)
            End Function

            Public Overrides Function VisitThrowStatement(node As BoundThrowStatement) As BoundNode
                Dim rewritten = DirectCast(MyBase.VisitThrowStatement(node), BoundThrowStatement)
                Dim expression As BoundExpression = rewritten.ExpressionOpt

                If expression Is Nothing OrElse expression.Kind <> BoundKind.SpillSequence Then
                    Return rewritten
                End If

                Debug.Assert(expression.Kind = BoundKind.SpillSequence)
                Dim spill = DirectCast(expression, BoundSpillSequence)
                Debug.Assert(spill.ValueOpt IsNot Nothing)

                Return RewriteSpillSequenceIntoBlock(spill,
                                                     False,
                                                     rewritten.Update(spill.ValueOpt))
            End Function

            Public Overrides Function VisitConditionalGoto(node As BoundConditionalGoto) As BoundNode
                Dim rewritten = DirectCast(MyBase.VisitConditionalGoto(node), BoundConditionalGoto)
                Dim condition As BoundExpression = rewritten.Condition

                If condition.Kind <> BoundKind.SpillSequence Then
                    Return rewritten
                End If

                Dim spill = DirectCast(condition, BoundSpillSequence)
                Debug.Assert(spill.ValueOpt IsNot Nothing)

                Return RewriteSpillSequenceIntoBlock(spill,
                                                     False,
                                                     node.Update(spill.ValueOpt,
                                                                 node.JumpIfTrue,
                                                                 node.Label))
            End Function

        End Class
    End Class

End Namespace
