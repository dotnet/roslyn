' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class DiagnosticsPass

        Private ReadOnly _expressionTreePlaceholders As New HashSet(Of BoundNode)(ReferenceEqualityComparer.Instance)

        Public Overrides Function VisitObjectCreationExpression(node As BoundObjectCreationExpression) As BoundNode
            If Me.IsInExpressionLambda Then
                Dim initializer As BoundObjectInitializerExpressionBase = node.InitializerOpt
                If initializer IsNot Nothing AndAlso initializer.Kind = BoundKind.ObjectInitializerExpression AndAlso node.ConstantValueOpt Is Nothing Then
                    ' report an error for the cases where ExpressionLambdaRewriter is going to emit
                    ' a call to a value type constructor with arguments, it would require creating 
                    ' a temp and calling the constructor on this temp
                    If initializer.Type.IsValueType AndAlso node.ConstructorOpt IsNot Nothing AndAlso node.Arguments.Length > 0 Then
                        GenerateExpressionTreeNotSupportedDiagnostic(initializer)
                    End If
                End If
            End If

            Return MyBase.VisitObjectCreationExpression(node)
        End Function

        Public Overrides Function VisitUserDefinedUnaryOperator(node As BoundUserDefinedUnaryOperator) As BoundNode
            If Me.IsInExpressionLambda Then
                Dim opKind As UnaryOperatorKind = node.OperatorKind And UnaryOperatorKind.OpMask
                Dim isLifted As Boolean = (node.OperatorKind And UnaryOperatorKind.Lifted) <> 0

                Select Case opKind
                    Case UnaryOperatorKind.Minus,
                         UnaryOperatorKind.Plus,
                         UnaryOperatorKind.Not

                        If isLifted Then
                            Dim method As MethodSymbol = node.Call.Method
                            If method.ReturnType.IsNullableType Then
                                ' TODO: There is a bug in Dev11 when the resulting expression tree fails to build in 
                                '       case the binary operator is lifted, but the method has nullable return type.
                                ' MORE: bug#18100
                                GenerateExpressionTreeNotSupportedDiagnostic(node)
                            End If
                        End If
                End Select
            End If

            Return MyBase.VisitUserDefinedUnaryOperator(node)
        End Function

        Public Overrides Function VisitAnonymousTypePropertyAccess(node As BoundAnonymousTypePropertyAccess) As BoundNode
            If Me.IsInExpressionLambda Then
                ' we do not allow anonymous objects which use one field to initialize another one
                GenerateDiagnostic(ERRID.ERR_BadAnonymousTypeForExprTree, node)
            End If

            Return MyBase.VisitAnonymousTypePropertyAccess(node)
        End Function

        Public Overrides Function VisitAnonymousTypeCreationExpression(node As BoundAnonymousTypeCreationExpression) As BoundNode
            ' Don't really care for declarations
            'Me.VisitList(node.Declarations)
            Debug.Assert(node.Declarations.All(Function(d) d.Kind = BoundKind.AnonymousTypePropertyAccess))

            Me.VisitList(node.Arguments)
            Return Nothing
        End Function

        Public Overrides Function VisitSequence(node As BoundSequence) As BoundNode
            If Not node.Locals.IsEmpty AndAlso Me.IsInExpressionLambda Then
                ' All such cases are not supported, note that some cases of invalid
                ' sequences are handled in DiagnosticsPass, but we still want to catch
                ' here those sequences created in lowering
                GenerateExpressionTreeNotSupportedDiagnostic(node)
            End If

            Return MyBase.VisitSequence(node)
        End Function

        Public Overrides Function VisitUserDefinedBinaryOperator(node As BoundUserDefinedBinaryOperator) As BoundNode
            If Me.IsInExpressionLambda Then
                Dim opKind As BinaryOperatorKind = node.OperatorKind And BinaryOperatorKind.OpMask

                Select Case opKind
                    Case BinaryOperatorKind.Like,
                         BinaryOperatorKind.Concatenate
                        'Do Nothing

                    Case Else
                        If (node.OperatorKind And BinaryOperatorKind.Lifted) <> 0 Then
                            Dim method As MethodSymbol = node.Call.Method
                            If method.ReturnType.IsNullableType Then
                                ' TODO: There is a bug in Dev11 when the resulting expression tree fails to build in 
                                '       case the binary operator is lifted, but the method has nullable return type.
                                ' MORE: bug#18096
                                GenerateExpressionTreeNotSupportedDiagnostic(node)
                            End If
                        End If
                End Select
            End If

            Return MyBase.VisitUserDefinedBinaryOperator(node)
        End Function

        Public Overrides Function VisitObjectInitializerExpression(node As BoundObjectInitializerExpression) As BoundNode
            If Not Me.IsInExpressionLambda Then
                Return MyBase.VisitObjectInitializerExpression(node)
            End If

            Dim placeholder As BoundWithLValueExpressionPlaceholder = node.PlaceholderOpt
            Debug.Assert(placeholder IsNot Nothing)
            Me.Visit(placeholder)

            ' Initializers cannot reference placeholder
            Me._expressionTreePlaceholders.Add(placeholder)

            For Each initializer In node.Initializers
                ' Ignore assignments in object initializers, only reference the value
                Dim boundExpression As BoundExpression = initializer
                If boundExpression.Kind = BoundKind.AssignmentOperator Then
                    Dim assignment = DirectCast(initializer, BoundAssignmentOperator)
                    Debug.Assert(assignment.LeftOnTheRightOpt Is Nothing)

                    boundExpression = assignment.Right
                    Dim propertyAccess = TryCast(assignment.Left, BoundPropertyAccess)
                    If propertyAccess IsNot Nothing Then
                        CheckRefReturningPropertyAccess(propertyAccess)
                    End If
                End If

                Me.Visit(boundExpression)
            Next

            Me._expressionTreePlaceholders.Remove(placeholder)

            Return Nothing
        End Function

        Public Overrides Function VisitWithLValueExpressionPlaceholder(node As BoundWithLValueExpressionPlaceholder) As BoundNode
            If Me._expressionTreePlaceholders.Contains(node) Then
                GenerateExpressionTreeNotSupportedDiagnostic(node)
            End If

            CheckMeAccessInWithExpression(node)

            Return MyBase.VisitWithLValueExpressionPlaceholder(node)
        End Function

        Public Overrides Function VisitAssignmentOperator(node As BoundAssignmentOperator) As BoundNode
            'COMPAT: old compiler used to allow assignments to properties
            '        we will continue allowing that too
            'NOTE:   native vbc also allows compound assignments like += but generates incorrect code.
            '        we are not going to support += assuming that it is not likely to be used in real scenarios.
            If Me.IsInExpressionLambda AndAlso
                    Not (node.Left.Kind = BoundKind.PropertyAccess AndAlso node.LeftOnTheRightOpt Is Nothing) Then

                ' Do not support explicit assignments
                GenerateExpressionTreeNotSupportedDiagnostic(node)
            End If

            Return MyBase.VisitAssignmentOperator(node)
        End Function

        Public Overrides Function VisitFieldAccess(node As BoundFieldAccess) As BoundNode
            Dim field As FieldSymbol = node.FieldSymbol
            If Not field.IsShared Then
                Me.Visit(node.ReceiverOpt)
            End If
            Return Nothing
        End Function

        Public Overrides Function VisitArrayCreation(node As BoundArrayCreation) As BoundNode
            If Me.IsInExpressionLambda Then
                If Not DirectCast(node.Type, ArrayTypeSymbol).IsSZArray Then
                    Dim initializer As BoundArrayInitialization = node.InitializerOpt
                    If initializer IsNot Nothing AndAlso Not initializer.Initializers.IsEmpty Then
                        GenerateDiagnostic(ERRID.ERR_ExprTreeNoMultiDimArrayCreation, node)
                    End If
                End If
            End If

            Return MyBase.VisitArrayCreation(node)
        End Function

        Public Overrides Function VisitLambda(node As BoundLambda) As BoundNode
            If Me.IsInExpressionLambda Then
                Dim lambda As LambdaSymbol = node.LambdaSymbol

                If lambda.IsAsync OrElse lambda.IsIterator Then
                    GenerateDiagnostic(ERRID.ERR_ResumableLambdaInExpressionTree, node)

                ElseIf Not node.WasCompilerGenerated AndAlso Not node.IsSingleLine Then
                    GenerateDiagnostic(ERRID.ERR_StatementLambdaInExpressionTree, node)

                Else
                    Select Case lambda.Syntax.Kind
                        Case SyntaxKind.MultiLineFunctionLambdaExpression,
                             SyntaxKind.MultiLineSubLambdaExpression
                            GenerateDiagnostic(ERRID.ERR_StatementLambdaInExpressionTree, node)

                        Case SyntaxKind.SingleLineSubLambdaExpression,
                             SyntaxKind.SingleLineFunctionLambdaExpression

                            Dim needDiagnostics As Boolean = True
                            Dim block As BoundBlock = node.Body
                            If block.Statements.Length = 1 OrElse
                                    (block.Statements.Length = 2 AndAlso
                                     block.Statements(1).Kind = BoundKind.ReturnStatement AndAlso
                                     DirectCast(block.Statements(1), BoundReturnStatement).ExpressionOpt Is Nothing) OrElse
                                    (block.Statements.Length = 3 AndAlso
                                     block.Statements(1).Kind = BoundKind.LabelStatement AndAlso
                                     block.Statements(2).Kind = BoundKind.ReturnStatement) Then

                                Dim stmt = block.Statements(0)
lSelect:
                                Select Case stmt.Kind
                                    Case BoundKind.ReturnStatement
                                        If (DirectCast(stmt, BoundReturnStatement)).ExpressionOpt IsNot Nothing Then
                                            needDiagnostics = False
                                        End If

                                    Case BoundKind.ExpressionStatement,
                                         BoundKind.AddHandlerStatement,
                                         BoundKind.RemoveHandlerStatement
                                        needDiagnostics = False

                                    Case BoundKind.Block
                                        Dim innerBlock = DirectCast(stmt, BoundBlock)
                                        If innerBlock.Locals.IsEmpty AndAlso innerBlock.Statements.Length = 1 Then
                                            stmt = innerBlock.Statements(0)
                                            GoTo lSelect
                                        End If
                                End Select
                            End If

                            If needDiagnostics Then
                                GenerateDiagnostic(ERRID.ERR_StatementLambdaInExpressionTree, node)
                            End If
                    End Select
                End If
            End If

            Dim save_containingSymbol = Me._containingSymbol
            Me._containingSymbol = node.LambdaSymbol
            Me.Visit(node.Body)
            Me._containingSymbol = save_containingSymbol
            Return Nothing
        End Function

        Public Overrides Function VisitCall(node As BoundCall) As BoundNode
            Dim method As MethodSymbol = node.Method
            If Not method.IsShared Then
                Me.Visit(node.ReceiverOpt)
            End If

            If IsInExpressionLambda And method.ReturnsByRef Then
                GenerateDiagnostic(ERRID.ERR_RefReturningCallInExpressionTree, node)
            End If

            Me.VisitList(node.Arguments)
            Return Nothing
        End Function

        Public Overrides Function VisitPropertyAccess(node As BoundPropertyAccess) As BoundNode
            Dim [property] As PropertySymbol = node.PropertySymbol
            If Not [property].IsShared Then
                Me.Visit(node.ReceiverOpt)
            End If

            CheckRefReturningPropertyAccess(node)

            Me.VisitList(node.Arguments)
            Return Nothing
        End Function

        Private Sub CheckRefReturningPropertyAccess(node As BoundPropertyAccess)
            If IsInExpressionLambda AndAlso node.PropertySymbol.ReturnsByRef Then
                GenerateDiagnostic(ERRID.ERR_RefReturningCallInExpressionTree, node)
            End If
        End Sub

        Public Overrides Function VisitEventAccess(node As BoundEventAccess) As BoundNode
            Dim [event] As EventSymbol = node.EventSymbol
            If Not [event].IsShared Then
                Me.Visit(node.ReceiverOpt)
            End If
            Return Nothing
        End Function

        Private Sub VisitLambdaConversion(operand As BoundExpression, relaxationLambda As BoundLambda)
            Debug.Assert(operand IsNot Nothing AndAlso
                         (operand.Kind = BoundKind.Lambda OrElse operand.Kind = BoundKind.QueryLambda))

            If operand.Kind = BoundKind.Lambda AndAlso Not CheckLambdaForByRefParameters(DirectCast(operand, BoundLambda)) AndAlso relaxationLambda IsNot Nothing Then
                CheckLambdaForByRefParameters(relaxationLambda)
            End If

            Me.Visit(operand)
        End Sub

        Private Function CheckLambdaForByRefParameters(lambda As BoundLambda) As Boolean
            Debug.Assert(Me.IsInExpressionLambda)
            Debug.Assert(lambda IsNot Nothing)

            Dim hasByRefParameters As Boolean = False
            For Each p In lambda.LambdaSymbol.Parameters
                If p.IsByRef Then
                    GenerateDiagnostic(ERRID.ERR_ByRefParamInExpressionTree, lambda)
                    Return True
                End If
            Next

            Return False
        End Function

        Public Overrides Function VisitConversion(node As BoundConversion) As BoundNode
            Dim savedInExpressionLambda As Boolean = Me._inExpressionLambda
            If (node.ConversionKind And ConversionKind.ConvertedToExpressionTree) <> 0 Then
                Me._inExpressionLambda = True
            End If

            If Me.IsInExpressionLambda AndAlso (node.ConversionKind And ConversionKind.Lambda) <> 0 Then
                VisitLambdaConversion(node.Operand, DirectCast(node.ExtendedInfoOpt, BoundRelaxationLambda)?.Lambda)
            Else
                MyBase.VisitConversion(node)
            End If

            Me._inExpressionLambda = savedInExpressionLambda
            Return Nothing
        End Function

        Public Overrides Function VisitTryCast(node As BoundTryCast) As BoundNode
            Dim savedInExpressionLambda As Boolean = Me._inExpressionLambda
            If (node.ConversionKind And ConversionKind.ConvertedToExpressionTree) <> 0 Then
                Me._inExpressionLambda = True
            End If

            If Me.IsInExpressionLambda AndAlso (node.ConversionKind And ConversionKind.Lambda) <> 0 Then
                VisitLambdaConversion(node.Operand, node.RelaxationLambdaOpt)
            Else
                MyBase.VisitTryCast(node)
            End If

            Me._inExpressionLambda = savedInExpressionLambda
            Return Nothing
        End Function

        Public Overrides Function VisitDirectCast(node As BoundDirectCast) As BoundNode
            Dim savedInExpressionLambda As Boolean = Me._inExpressionLambda
            If (node.ConversionKind And ConversionKind.ConvertedToExpressionTree) <> 0 Then
                Me._inExpressionLambda = True
            End If

            If Me.IsInExpressionLambda AndAlso (node.ConversionKind And ConversionKind.Lambda) <> 0 Then
                VisitLambdaConversion(node.Operand, node.RelaxationLambdaOpt)
            Else
                MyBase.VisitDirectCast(node)
            End If

            Me._inExpressionLambda = savedInExpressionLambda
            Return Nothing
        End Function

        Public Overrides Function VisitLateInvocation(node As BoundLateInvocation) As BoundNode
            If Not Me.IsInExpressionLambda Then
                Return MyBase.VisitLateInvocation(node)
            End If

            GenerateDiagnostic(ERRID.ERR_ExprTreeNoLateBind, node)

            If node.Member.Kind <> BoundKind.LateMemberAccess Then
                Me.Visit(node.Member)
            End If
            Me.VisitList(node.ArgumentsOpt)
            Return Nothing
        End Function

        Public Overrides Function VisitLateMemberAccess(node As BoundLateMemberAccess) As BoundNode
            If Me.IsInExpressionLambda Then
                GenerateDiagnostic(ERRID.ERR_ExprTreeNoLateBind, node)
            End If

            Return MyBase.VisitLateMemberAccess(node)
        End Function

        Public Overrides Function VisitConditionalAccess(node As BoundConditionalAccess) As BoundNode
            If Me.IsInExpressionLambda Then
                GenerateDiagnostic(ERRID.ERR_NullPropagatingOpInExpressionTree, node)
            End If

            Return MyBase.VisitConditionalAccess(node)
        End Function

        Private Sub GenerateExpressionTreeNotSupportedDiagnostic(node As BoundNode)
            GenerateDiagnostic(ERRID.ERR_ExpressionTreeNotSupported, node)
        End Sub

        Private Sub GenerateDiagnostic(code As ERRID, node As BoundNode)
            Me._diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(code), node.Syntax.GetLocation()))
        End Sub

    End Class

End Namespace
