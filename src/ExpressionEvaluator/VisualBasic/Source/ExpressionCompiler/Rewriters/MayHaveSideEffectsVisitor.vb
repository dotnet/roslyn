' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class MayHaveSideEffectsVisitor
        Inherits BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator

        Private _mayHaveSideEffects As Boolean

        Private Sub New()
        End Sub

        Friend Shared Function MayHaveSideEffects(node As BoundNode) As Boolean
            Dim visitor = New MayHaveSideEffectsVisitor()
            visitor.Visit(node)
            Return visitor._mayHaveSideEffects
        End Function

        Public Overrides Function Visit(node As BoundNode) As BoundNode
            If _mayHaveSideEffects Then
                Return Nothing
            End If

            Return MyBase.Visit(node)
        End Function

        Public Overrides Function VisitAssignmentOperator(node As BoundAssignmentOperator) As BoundNode
            Return SetMayHaveSideEffects()
        End Function

        ' Calls are treated as having side effects, but properties are
        ' not. (Since this visitor is run on the bound tree before
        ' lowering, properties are not represented as calls.)
        Public Overrides Function VisitCall(node As BoundCall) As BoundNode
            Return SetMayHaveSideEffects()
        End Function

        Public Overrides Function VisitLateInvocation(node As BoundLateInvocation) As BoundNode
            Return SetMayHaveSideEffects()
        End Function

        ' In Visual Basic, a parameterless method can be called without specifying an argument list,
        ' so we will treat every method group as a potential Call.  This is not strictly true (in the
        ' case where the parent is an AddressOf), but it doesn't seem worth any extra effort to handle
        ' that case differently (the consequence of saying that something may have side effects is
        ' simply that you may have to re-evaluate the expression manually).  The far more common case
        ' is encountered when getting the DataTip for a method invocation (the argument list is not
        ' included in the expression text).  We want to prevent automatic evaluation in that case.  
        Public Overrides Function VisitMethodGroup(node As BoundMethodGroup) As BoundNode
            Return SetMayHaveSideEffects()
        End Function

        Public Overrides Function VisitObjectInitializerExpression(node As BoundObjectInitializerExpression) As BoundNode
            For Each initializer In node.Initializers
                ' Do not treat initializer assignment as a side effect since it is
                ' part of an object creation. In short, visit the RHS only.
                Dim expr = If(initializer.Kind = BoundKind.AssignmentOperator,
                    DirectCast(initializer, BoundAssignmentOperator).Right,
                    initializer)
                Visit(expr)
            Next
            Return Nothing
        End Function

        Private Function SetMayHaveSideEffects() As BoundNode
            _mayHaveSideEffects = True
            Return Nothing
        End Function

    End Class

End Namespace
