' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class LocalRewriter

        ' The strategy of this rewrite is to do rewrite "locally".
        ' We analyze arguments of the concat in a shallow fashion assuming that 
        ' lowering and optimizations (including this one) is already done for the arguments.
        ' Based on the arguments we select the most appropriate pattern for the current node.
        ' 
        ' NOTE: it is not guaranteed that the node that we chose will be the most optimal since we have only 
        '       local information - i.e. we look at the arguments, but we do not know about siblings.
        '       When we move to the parent, the node may be rewritten by this or some another optimization.
        '       
        ' Example:
        '     result = if( "abc" & "def" & Nothing , expr1 & "moo" & "baz" ) & expr2
        ' 
        ' Will rewrite into:
        '     result = Concat("abcdef", expr2)
        '     
        ' However there will be transient nodes like  Concat(expr1 + "moo")  that will not be present in the
        ' resulting tree.
        Private Function RewriteConcatenateOperator(node As BoundBinaryOperator) As BoundExpression
            Debug.Assert(node.Type.IsStringType() AndAlso
               node.Left.Type.IsStringType() AndAlso
               node.Right.Type.IsStringType(), "concat args should be strings here")

            Dim syntax = node.Syntax
            Dim loweredLeft = node.Left
            Dim loweredRight = node.Right
            Dim factory = New SyntheticBoundNodeFactory(Me._topMethod, Me._currentMethodOrLambda, syntax, Me._compilationState, Me._diagnostics)

            ' try fold two args without flattening.
            Dim folded As BoundExpression = TryFoldTwoConcatOperands(factory, loweredLeft, loweredRight)
            If folded IsNot Nothing Then
                Return folded
            End If

            ' flatten and merge -  ( expr1 + "A" ) + ("B" + expr2) ===> (expr1 + "AB" + expr2)
            Dim leftFlattened = ArrayBuilder(Of BoundExpression).GetInstance()
            Dim rightFlattened = ArrayBuilder(Of BoundExpression).GetInstance()

            FlattenConcatArg(loweredLeft, leftFlattened)
            FlattenConcatArg(loweredRight, rightFlattened)

            If leftFlattened.Any AndAlso rightFlattened.Any Then
                folded = TryFoldTwoConcatOperands(factory, leftFlattened.Last(), rightFlattened.First())
                If folded IsNot Nothing Then
                    rightFlattened(0) = folded
                    leftFlattened.RemoveLast()
                End If
            End If

            leftFlattened.AddRange(rightFlattened)

            rightFlattened.Free()
            Select Case leftFlattened.Count
                Case 0
                    leftFlattened.Free()
                    Return factory.StringLiteral(ConstantValue.Create(""))

                Case 1
                    Dim result = leftFlattened(0)
                    leftFlattened.Free()
                    Return result

                Case 2
                    Dim left As BoundExpression = leftFlattened(0)
                    Dim right As BoundExpression = leftFlattened(1)
                    leftFlattened.Free()

                    Return RewriteStringConcatenationTwoExprs(node, factory, left, right)

                Case 3
                    Dim first As BoundExpression = leftFlattened(0)
                    Dim second As BoundExpression = leftFlattened(1)
                    Dim third As BoundExpression = leftFlattened(2)
                    leftFlattened.Free()

                    Return RewriteStringConcatenationThreeExprs(node, factory, first, second, third)

                Case 4
                    Dim first As BoundExpression = leftFlattened(0)
                    Dim second As BoundExpression = leftFlattened(1)
                    Dim third As BoundExpression = leftFlattened(2)
                    Dim fourth As BoundExpression = leftFlattened(3)
                    leftFlattened.Free()

                    Return RewriteStringConcatenationFourExprs(node, factory, first, second, third, fourth)

                Case Else
                    Return RewriteStringConcatenationManyExprs(node, factory, leftFlattened.ToImmutableAndFree())
            End Select
        End Function

        ''' <summary>
        ''' digs into known concat operators and unwraps their arguments
        ''' otherwise returns the expression as-is
        ''' 
        ''' Generally we only need to recognize same node patterns that we create as a result of concatenation rewrite.
        ''' We could recognize some other nodes and unwrap to arguments 
        ''' </summary>
        Private Sub FlattenConcatArg(lowered As BoundExpression, flattened As ArrayBuilder(Of BoundExpression))
            Select Case lowered.Kind
                Case BoundKind.Call
                    Dim boundCall As BoundCall = DirectCast(lowered, BoundCall)

                    Dim method As MethodSymbol = boundCall.Method
                    If method.IsShared AndAlso method.ContainingType.SpecialType = SpecialType.System_String Then
                        If method Is Me.Compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringString) OrElse
                            method Is Me.Compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringStringString) OrElse
                            method Is Me.Compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringStringStringString) Then

                            flattened.AddRange(boundCall.Arguments)
                            Return

                        End If

                        If method Is Me.Compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringArray) Then
                            Dim args As BoundArrayCreation = TryCast(boundCall.Arguments(0), BoundArrayCreation)
                            If args IsNot Nothing Then
                                Dim initializer As BoundArrayInitialization = args.InitializerOpt
                                If initializer IsNot Nothing Then

                                    flattened.AddRange(initializer.Initializers)
                                    Return

                                End If
                            End If
                        End If
                    End If

                Case BoundKind.BinaryConditionalExpression
                    Dim boundCoalesce = DirectCast(lowered, BoundBinaryConditionalExpression)
                    If boundCoalesce.ConvertedTestExpression Is Nothing Then
                        Dim elseExpr = boundCoalesce.ElseExpression
                        If elseExpr.ConstantValueOpt IsNot Nothing AndAlso elseExpr.ConstantValueOpt.StringValue = "" Then
                            flattened.AddRange(boundCoalesce.TestExpression)
                            Return
                        End If
                    End If
            End Select

            ' fallback - if nothing above worked, leave arg as-is
            flattened.Add(lowered)
        End Sub

        ''' <summary>
        ''' folds two concat operands into one expression if possible
        ''' otherwise returns null
        ''' </summary>
        Private Function TryFoldTwoConcatOperands(factory As SyntheticBoundNodeFactory,
                                                  loweredLeft As BoundExpression,
                                                  loweredRight As BoundExpression) As BoundExpression

            Dim leftConst As ConstantValue = loweredLeft.ConstantValueOpt
            Dim rightConst As ConstantValue = loweredRight.ConstantValueOpt

            If leftConst IsNot Nothing AndAlso rightConst IsNot Nothing Then
                ' const concat may fail to fold if strings are huge. 
                ' This would be unusual.
                Dim concatenated As ConstantValue = TryFoldTwoConcatConsts(leftConst, rightConst)
                If concatenated IsNot Nothing Then
                    Return factory.StringLiteral(concatenated)
                End If
            End If

            If IsNullOrEmptyStringConstant(loweredLeft) Then
                If IsNullOrEmptyStringConstant(loweredRight) Then
                    Return factory.Literal("")
                ElseIf Not _inExpressionLambda Then
                    Return RewriteStringConcatenationOneExpr(factory, loweredRight)
                End If
            ElseIf Not _inExpressionLambda AndAlso IsNullOrEmptyStringConstant(loweredRight) Then
                Return RewriteStringConcatenationOneExpr(factory, loweredLeft)
            End If

            Return Nothing
        End Function

        Private Shared Function IsNullOrEmptyStringConstant(operand As BoundExpression) As Boolean
            Return (operand.ConstantValueOpt IsNot Nothing AndAlso String.IsNullOrEmpty(operand.ConstantValueOpt.StringValue)) OrElse
                operand.IsDefaultValueConstant

        End Function

        ''' <summary>
        ''' folds two concat constants into one if possible
        ''' otherwise returns null.
        ''' It is generally always possible to concat constants, unless resulting string would be too large.
        ''' </summary>
        Private Shared Function TryFoldTwoConcatConsts(leftConst As ConstantValue, rightConst As ConstantValue) As ConstantValue
            Dim leftVal As String = leftConst.StringValue
            Dim rightVal As String = rightConst.StringValue

            If Not leftConst.IsDefaultValue AndAlso
                Not rightConst.IsDefaultValue AndAlso
                leftVal.Length + rightVal.Length < 0 Then

                Return Nothing
            End If

            ' TODO: if transient string allocations are an issue, consider introducing constants that contain builders.
            '       it may be not so easy to even get here though, since typical
            '       "A" + "B" + "C" + ... cases should be folded in the binder as spec requires so.
            '       we would be mostly picking here edge cases like "A" + (object)null + "B" + (object)null + ...
            Return ConstantValue.Create(leftVal + rightVal)
        End Function

        ''' <summary>
        ''' Strangely enough there is such a thing as unary concatenation and it must be rewritten.
        ''' </summary>
        Private Shared Function RewriteStringConcatenationOneExpr(factory As SyntheticBoundNodeFactory,
                                                           loweredOperand As BoundExpression) As BoundExpression

            Return factory.BinaryConditional(loweredOperand, factory.Literal(""))
        End Function

        Private Function RewriteStringConcatenationTwoExprs(node As BoundExpression,
                                                            factory As SyntheticBoundNodeFactory,
                                                            loweredLeft As BoundExpression,
                                                            loweredRight As BoundExpression) As BoundExpression
            '    ' Call String.Concat(left, right)
            Const memberId As SpecialMember = SpecialMember.System_String__ConcatStringString
            Dim memberSymbol = DirectCast(GetSpecialTypeMember(memberId), MethodSymbol)

            If Not ReportMissingOrBadRuntimeHelper(node, memberId, memberSymbol) Then
                Return factory.Call(Nothing, memberSymbol, loweredLeft, loweredRight)
            End If

            Return node
        End Function

        Private Function RewriteStringConcatenationThreeExprs(node As BoundExpression,
                                                              factory As SyntheticBoundNodeFactory,
                                                              loweredFirst As BoundExpression,
                                                              loweredSecond As BoundExpression,
                                                              loweredThird As BoundExpression) As BoundExpression
            '    ' Call String.Concat(first, second, third)
            Const memberId As SpecialMember = SpecialMember.System_String__ConcatStringStringString
            Dim memberSymbol = DirectCast(GetSpecialTypeMember(memberId), MethodSymbol)

            If Not ReportMissingOrBadRuntimeHelper(node, memberId, memberSymbol) Then
                Return factory.Call(Nothing, memberSymbol, loweredFirst, loweredSecond, loweredThird)
            End If

            Return node
        End Function

        Private Function RewriteStringConcatenationFourExprs(node As BoundExpression,
                                                              factory As SyntheticBoundNodeFactory,
                                                              loweredFirst As BoundExpression,
                                                              loweredSecond As BoundExpression,
                                                              loweredThird As BoundExpression,
                                                              loweredFourth As BoundExpression) As BoundExpression
            '    ' Call String.Concat(first, second, third)
            Const memberId As SpecialMember = SpecialMember.System_String__ConcatStringStringStringString
            Dim memberSymbol = DirectCast(GetSpecialTypeMember(memberId), MethodSymbol)

            If Not ReportMissingOrBadRuntimeHelper(node, memberId, memberSymbol) Then
                Return factory.Call(Nothing, memberSymbol, loweredFirst, loweredSecond, loweredThird, loweredFourth)
            End If

            Return node
        End Function

        Private Function RewriteStringConcatenationManyExprs(node As BoundExpression,
                                                             factory As SyntheticBoundNodeFactory,
                                                             loweredArgs As ImmutableArray(Of BoundExpression)) As BoundExpression

            Const memberId As SpecialMember = SpecialMember.System_String__ConcatStringArray
            Dim memberSymbol = DirectCast(GetSpecialTypeMember(memberId), MethodSymbol)

            If Not ReportMissingOrBadRuntimeHelper(node, memberId, memberSymbol) Then
                Dim argArray = factory.Array(node.Type, loweredArgs)
                Return factory.Call(Nothing, memberSymbol, ImmutableArray.Create(Of BoundExpression)(argArray))
            End If

            Return node
        End Function
    End Class
End Namespace
