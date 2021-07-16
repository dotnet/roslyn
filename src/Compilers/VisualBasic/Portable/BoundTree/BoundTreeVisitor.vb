' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend MustInherit Class BoundTreeVisitor(Of A, R)
        Protected Sub New()
        End Sub

        Public Overridable Function Visit(node As BoundNode, arg As A) As R
            If node Is Nothing Then
                Return Nothing
            End If
            ' around 50 cases inlined here to allow the JIT to generate good code.
            Select Case node.Kind
                Case BoundKind.OmittedArgument
                    Return VisitOmittedArgument(CType(node, BoundOmittedArgument), arg)
                Case BoundKind.Parenthesized
                    Return VisitParenthesized(CType(node, BoundParenthesized), arg)
                Case BoundKind.ArrayAccess
                    Return VisitArrayAccess(CType(node, BoundArrayAccess), arg)
                Case BoundKind.TypeExpression
                    Return VisitTypeExpression(CType(node, BoundTypeExpression), arg)
                Case BoundKind.NamespaceExpression
                    Return VisitNamespaceExpression(CType(node, BoundNamespaceExpression), arg)
                Case BoundKind.UnaryOperator
                    Return VisitUnaryOperator(CType(node, BoundUnaryOperator), arg)
                Case BoundKind.BinaryOperator
                    Return VisitBinaryOperator(CType(node, BoundBinaryOperator), arg)
                Case BoundKind.AssignmentOperator
                    Return VisitAssignmentOperator(CType(node, BoundAssignmentOperator), arg)
                Case BoundKind.TernaryConditionalExpression
                    Return VisitTernaryConditionalExpression(CType(node, BoundTernaryConditionalExpression), arg)
                Case BoundKind.BinaryConditionalExpression
                    Return VisitBinaryConditionalExpression(CType(node, BoundBinaryConditionalExpression), arg)
                Case BoundKind.Conversion
                    Return VisitConversion(CType(node, BoundConversion), arg)
                Case BoundKind.DirectCast
                    Return VisitDirectCast(CType(node, BoundDirectCast), arg)
                Case BoundKind.TryCast
                    Return VisitTryCast(CType(node, BoundTryCast), arg)
                Case BoundKind.TypeOf
                    Return VisitTypeOf(CType(node, BoundTypeOf), arg)
                Case BoundKind.SequencePoint
                    Return VisitSequencePoint(CType(node, BoundSequencePoint), arg)
                Case BoundKind.SequencePointWithSpan
                    Return VisitSequencePointWithSpan(CType(node, BoundSequencePointWithSpan), arg)
                Case BoundKind.NoOpStatement
                    Return VisitNoOpStatement(CType(node, BoundNoOpStatement), arg)
                Case BoundKind.MethodGroup
                    Return VisitMethodGroup(CType(node, BoundMethodGroup), arg)
                Case BoundKind.PropertyGroup
                    Return VisitPropertyGroup(CType(node, BoundPropertyGroup), arg)
                Case BoundKind.ReturnStatement
                    Return VisitReturnStatement(CType(node, BoundReturnStatement), arg)
                Case BoundKind.Call
                    Return VisitCall(CType(node, BoundCall), arg)
                Case BoundKind.ObjectCreationExpression
                    Return VisitObjectCreationExpression(CType(node, BoundObjectCreationExpression), arg)
                Case BoundKind.DelegateCreationExpression
                    Return VisitDelegateCreationExpression(CType(node, BoundDelegateCreationExpression), arg)
                Case BoundKind.FieldAccess
                    Return VisitFieldAccess(CType(node, BoundFieldAccess), arg)
                Case BoundKind.PropertyAccess
                    Return VisitPropertyAccess(CType(node, BoundPropertyAccess), arg)
                Case BoundKind.Block
                    Return VisitBlock(CType(node, BoundBlock), arg)
                Case BoundKind.LocalDeclaration
                    Return VisitLocalDeclaration(CType(node, BoundLocalDeclaration), arg)
                Case BoundKind.FieldInitializer
                    Return VisitFieldInitializer(CType(node, BoundFieldInitializer), arg)
                Case BoundKind.PropertyInitializer
                    Return VisitPropertyInitializer(CType(node, BoundPropertyInitializer), arg)
                Case BoundKind.Sequence
                    Return VisitSequence(CType(node, BoundSequence), arg)
                Case BoundKind.ExpressionStatement
                    Return VisitExpressionStatement(CType(node, BoundExpressionStatement), arg)
                Case BoundKind.IfStatement
                    Return VisitIfStatement(CType(node, BoundIfStatement), arg)
                Case BoundKind.ForToStatement
                    Return VisitForToStatement(CType(node, BoundForToStatement), arg)
                Case BoundKind.ExitStatement
                    Return VisitExitStatement(CType(node, BoundExitStatement), arg)
                Case BoundKind.ContinueStatement
                    Return VisitContinueStatement(CType(node, BoundContinueStatement), arg)
                Case BoundKind.TryStatement
                    Return VisitTryStatement(CType(node, BoundTryStatement), arg)
                Case BoundKind.CatchBlock
                    Return VisitCatchBlock(CType(node, BoundCatchBlock), arg)
                Case BoundKind.Literal
                    Return VisitLiteral(CType(node, BoundLiteral), arg)
                Case BoundKind.MeReference
                    Return VisitMeReference(CType(node, BoundMeReference), arg)
                Case BoundKind.Local
                    Return VisitLocal(CType(node, BoundLocal), arg)
                Case BoundKind.Parameter
                    Return VisitParameter(CType(node, BoundParameter), arg)
                Case BoundKind.ByRefArgumentPlaceholder
                    Return VisitByRefArgumentPlaceholder(CType(node, BoundByRefArgumentPlaceholder), arg)
                Case BoundKind.ByRefArgumentWithCopyBack
                    Return VisitByRefArgumentWithCopyBack(CType(node, BoundByRefArgumentWithCopyBack), arg)
                Case BoundKind.LabelStatement
                    Return VisitLabelStatement(CType(node, BoundLabelStatement), arg)
                Case BoundKind.GotoStatement
                    Return VisitGotoStatement(CType(node, BoundGotoStatement), arg)
                Case BoundKind.StatementList
                    Return VisitStatementList(CType(node, BoundStatementList), arg)
                Case BoundKind.ConditionalGoto
                    Return VisitConditionalGoto(CType(node, BoundConditionalGoto), arg)
                Case BoundKind.Lambda
                    Return VisitLambda(CType(node, BoundLambda), arg)
            End Select
            Return VisitInternal(node, arg)
        End Function

        Public Overridable Function DefaultVisit(node As BoundNode, arg As A) As R
            Return Nothing
        End Function
    End Class

    Partial Friend MustInherit Class BoundTreeVisitor
        Protected Sub New()
        End Sub

        <DebuggerHidden>
        Public Overridable Function Visit(node As BoundNode) As BoundNode
            If node IsNot Nothing Then
                Return node.Accept(Me)
            End If

            Return Nothing
        End Function

        <DebuggerHidden>
        Public Overridable Function DefaultVisit(node As BoundNode) As BoundNode
            Return Nothing
        End Function

        Public Class CancelledByStackGuardException
            Inherits Exception

            Public ReadOnly Node As BoundNode

            Public Sub New(inner As Exception, node As BoundNode)
                MyBase.New(inner.Message, inner)

                Me.Node = node
            End Sub

            Public Sub AddAnError(diagnostics As DiagnosticBag)
                diagnostics.Add(ERRID.ERR_TooLongOrComplexExpression, GetTooLongOrComplexExpressionErrorLocation(Node))
            End Sub

            Public Sub AddAnError(diagnostics As BindingDiagnosticBag)
                diagnostics.Add(ERRID.ERR_TooLongOrComplexExpression, GetTooLongOrComplexExpressionErrorLocation(Node))
            End Sub

            Public Shared Function GetTooLongOrComplexExpressionErrorLocation(node As BoundNode) As Location
                Dim syntax As SyntaxNode = node.Syntax

                If TypeOf syntax IsNot ExpressionSyntax Then
                    syntax = If(syntax.DescendantNodes(Function(n) TypeOf n IsNot ExpressionSyntax).OfType(Of ExpressionSyntax)().FirstOrDefault(), syntax)
                End If

                Return syntax.GetFirstToken().GetLocation()
            End Function
        End Class

        ''' <summary>
        ''' Consumers must provide implementation for <see cref="VisitExpressionWithoutStackGuard"/>.
        ''' </summary>
        <DebuggerStepThrough>
        Protected Function VisitExpressionWithStackGuard(ByRef recursionDepth As Integer, node As BoundExpression) As BoundExpression
            Dim result As BoundExpression
            recursionDepth += 1

#If DEBUG Then
            Dim saveRecursionDepth = recursionDepth
#End If

            If recursionDepth > 1 OrElse Not ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException() Then
                StackGuard.EnsureSufficientExecutionStack(recursionDepth)
                result = VisitExpressionWithoutStackGuard(node)
            Else
                result = VisitExpressionWithStackGuard(node)
            End If

#If DEBUG Then
            Debug.Assert(saveRecursionDepth = recursionDepth)
#End If
            recursionDepth -= 1
            Return result
        End Function

        Protected Overridable Function ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException() As Boolean
            Return True
        End Function

        <DebuggerStepThrough>
        Private Function VisitExpressionWithStackGuard(node As BoundExpression) As BoundExpression
            Try
                Return VisitExpressionWithoutStackGuard(node)
            Catch ex As InsufficientExecutionStackException
                Throw New CancelledByStackGuardException(ex, node)
            End Try
        End Function

        ''' <summary>
        ''' We should be intentional about behavior of derived classes regarding guarding against stack overflow. 
        ''' </summary>
        Protected MustOverride Function VisitExpressionWithoutStackGuard(node As BoundExpression) As BoundExpression
    End Class
End Namespace
