﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundExpression
        Implements IOperation

        Private ReadOnly Property IOperation_ConstantValue As [Optional](Of Object) Implements IOperation.ConstantValue
            Get
                Dim value As ConstantValue = Me.ConstantValueOpt
                If value Is Nothing Then
                    Return New [Optional](Of Object)()
                End If

                Return New [Optional](Of Object)(value.Value)
            End Get
        End Property

        Private ReadOnly Property IOperation_Kind As OperationKind Implements IOperation.Kind
            Get
                Return Me.ExpressionKind()
            End Get
        End Property

        Private ReadOnly Property IOperation_IsInvalid As Boolean Implements IOperation.IsInvalid
            Get
                Return Me.HasErrors
            End Get
        End Property

        Private ReadOnly Property IOperation_Type As ITypeSymbol Implements IOperation.Type
            Get
                Return Me.Type
            End Get
        End Property

        Private ReadOnly Property IOperation_Syntax As SyntaxNode Implements IOperation.Syntax
            Get
                Return Me.Syntax
            End Get
        End Property

        Protected MustOverride Function ExpressionKind() As OperationKind

        Public MustOverride Overloads Sub Accept(visitor As OperationVisitor) Implements IOperation.Accept

        Public MustOverride Overloads Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult Implements IOperation.Accept

        Protected Shared Function GetChildOfBadExpression(parent As BoundNode, index As Integer) As IOperation
            Dim badParent As BoundBadExpression = TryCast(parent, BoundBadExpression)
            If badParent IsNot Nothing AndAlso badParent.ChildBoundNodes.Length > index Then
                Dim child As IOperation = TryCast(badParent.ChildBoundNodes(index), IOperation)
                If child IsNot Nothing Then
                    Return child
                End If
            End If

            Return New InvalidExpression(parent.Syntax, ImmutableArray(Of IOperation).Empty)
        End Function

    End Class

    Partial Friend Class BoundAssignmentOperator
        Implements IAssignmentExpression
        Implements ICompoundAssignmentExpression

        Private ReadOnly Property IAssignmentExpression_Target As IOperation Implements IAssignmentExpression.Target
            Get
                Return Me.Left
            End Get
        End Property

        Private ReadOnly Property IAssignmentExpression_Value As IOperation Implements IAssignmentExpression.Value
            Get
                If ExpressionKind() = OperationKind.CompoundAssignmentExpression Then
                    Return DirectCast(Me.Right, IBinaryOperatorExpression).RightOperand
                End If

                Return Me.Right
            End Get
        End Property

        Private ReadOnly Property ICompoundAssignmentExpression_BinaryOperationKind As BinaryOperationKind Implements ICompoundAssignmentExpression.BinaryOperationKind
            Get
                If ExpressionKind() = OperationKind.CompoundAssignmentExpression Then
                    Return DirectCast(Me.Right, IBinaryOperatorExpression).BinaryOperationKind
                End If

                Return BinaryOperationKind.Invalid
            End Get
        End Property

        Private ReadOnly Property IHasOperatorMethodExpression_OperatorMethod As IMethodSymbol Implements IHasOperatorMethodExpression.OperatorMethod
            Get
                If Me.IHasOperatorMethodExpression_UsesOperatorMethod Then
                    Return DirectCast(Me.Right, IBinaryOperatorExpression).OperatorMethod
                End If

                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IHasOperatorMethodExpression_UsesOperatorMethod As Boolean Implements IHasOperatorMethodExpression.UsesOperatorMethod
            Get
                If ExpressionKind() = OperationKind.CompoundAssignmentExpression Then
                    Return DirectCast(Me.Right, IBinaryOperatorExpression).UsesOperatorMethod
                End If

                Return False
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            If Me.LeftOnTheRightOpt IsNot Nothing Then
                Select Case Me.Right.Kind
                    Case BoundKind.BinaryOperator
                        Dim rightBinary As BoundBinaryOperator = DirectCast(Me.Right, BoundBinaryOperator)
                        If rightBinary.Left Is Me.LeftOnTheRightOpt Then
                            Return OperationKind.CompoundAssignmentExpression
                        End If
                    Case BoundKind.UserDefinedBinaryOperator
                        Dim rightOperatorBinary As IBinaryOperatorExpression = DirectCast(Me.Right, BoundUserDefinedBinaryOperator)
                        ' It is not permissible to access the Left property of a BoundUserDefinedBinaryOperator unconditionally,
                        ' because that property can throw an exception if the operator expression is semantically invalid.
                        ' Fetching the left operand through IBinaryOperatorExpression is safe.
                        If rightOperatorBinary.LeftOperand Is Me.LeftOnTheRightOpt Then
                            Return OperationKind.CompoundAssignmentExpression
                        End If
                End Select
            End If

            Return OperationKind.AssignmentExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            If Me.ExpressionKind() = OperationKind.CompoundAssignmentExpression Then
                visitor.VisitCompoundAssignmentExpression(Me)
            Else
                visitor.VisitAssignmentExpression(Me)
            End If
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            If Me.ExpressionKind() = OperationKind.CompoundAssignmentExpression Then
                Return visitor.VisitCompoundAssignmentExpression(Me, argument)
            Else
                Return visitor.VisitAssignmentExpression(Me, argument)
            End If
        End Function
    End Class

    Partial Friend Class BoundMeReference
        Implements IInstanceReferenceExpression

        Private ReadOnly Property IInstanceReferenceExpression_InstanceReferenceKind As InstanceReferenceKind Implements IInstanceReferenceExpression.InstanceReferenceKind
            Get
                Return If(Me.WasCompilerGenerated, InstanceReferenceKind.Implicit, InstanceReferenceKind.Explicit)
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.InstanceReferenceExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitInstanceReferenceExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitInstanceReferenceExpression(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundMyBaseReference
        Implements IInstanceReferenceExpression

        Private ReadOnly Property IInstanceReferenceExpression_InstanceReferenceKind As InstanceReferenceKind Implements IInstanceReferenceExpression.InstanceReferenceKind
            Get
                Return InstanceReferenceKind.BaseClass
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.InstanceReferenceExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitInstanceReferenceExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitInstanceReferenceExpression(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundMyClassReference
        Implements IInstanceReferenceExpression

        Private ReadOnly Property IInstanceReferenceExpression_InstanceReferenceKind As InstanceReferenceKind Implements IInstanceReferenceExpression.InstanceReferenceKind
            Get
                Return InstanceReferenceKind.ThisClass
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.InstanceReferenceExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitInstanceReferenceExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitInstanceReferenceExpression(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundLiteral
        Implements ILiteralExpression

        Private ReadOnly Property ILiteralExpression_Text As String Implements ILiteralExpression.Text
            Get
                Return Me.Syntax.ToString()
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.LiteralExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitLiteralExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitLiteralExpression(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundAwaitOperator
        Implements IAwaitExpression

        Private ReadOnly Property IAwaitExpression_AwaitedValue As IOperation Implements IAwaitExpression.AwaitedValue
            Get
                Return Me.Operand
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.AwaitExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitAwaitExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitAwaitExpression(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundLambda
        Implements ILambdaExpression

        Private ReadOnly Property ILambdaExpression_Body As IBlockStatement Implements ILambdaExpression.Body
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property ILambdaExpression_Signature As IMethodSymbol Implements ILambdaExpression.Signature
            Get
                Return Me.LambdaSymbol
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.LambdaExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitLambdaExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitLambdaExpression(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundCall
        Implements IInvocationExpression

        Private ReadOnly Property IHasArgumentsExpression_ArgumentsInEvaluationOrder As ImmutableArray(Of IArgument) Implements IHasArgumentsExpression.ArgumentsInEvaluationOrder
            Get
                Return DeriveArguments(Me.Arguments, Me.Method.Parameters)
            End Get
        End Property

        Private ReadOnly Property IInvocationExpression_IsVirtual As Boolean Implements IInvocationExpression.IsVirtual
            Get
                Dim method As IMethodSymbol = Me.Method
                Dim instance As IOperation = Me.ReceiverOpt

                Return method IsNot Nothing AndAlso instance IsNot Nothing AndAlso (method.IsVirtual OrElse method.IsAbstract OrElse method.IsOverride) AndAlso instance.Kind <> BoundKind.MyBaseReference AndAlso instance.Kind <> BoundKind.MyClassReference
            End Get
        End Property

        Private ReadOnly Property IInvocationExpression_TargetMethod As IMethodSymbol Implements IInvocationExpression.TargetMethod
            Get
                Return Me.Method
            End Get
        End Property

        Private ReadOnly Property IInvocationExpression_Instance As IOperation Implements IInvocationExpression.Instance
            Get
                If Me.Method.IsShared Then
                    Return Nothing
                Else
                    Return Me.ReceiverOpt
                End If
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.InvocationExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitInvocationExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitInvocationExpression(Me, argument)
        End Function

        Friend Shared Function DeriveArguments(boundArguments As ImmutableArray(Of BoundExpression), parameters As ImmutableArray(Of Symbols.ParameterSymbol)) As ImmutableArray(Of IArgument)
            Dim argumentsLength As Integer = boundArguments.Length
            Debug.Assert(argumentsLength = parameters.Length)

            Dim arguments As ArrayBuilder(Of IArgument) = ArrayBuilder(Of IArgument).GetInstance(argumentsLength)
            For index As Integer = 0 To argumentsLength - 1 Step 1
                arguments.Add(DeriveArgument(index, boundArguments(index), parameters))
            Next

            Return arguments.ToImmutableAndFree()
        End Function

        Private Shared ReadOnly s_argumentMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundExpression, IArgument)

        Private Shared Function DeriveArgument(index As Integer, argument As BoundExpression, parameters As ImmutableArray(Of Symbols.ParameterSymbol)) As IArgument
            Select Case argument.Kind
                Case BoundKind.ByRefArgumentWithCopyBack
                    Return s_argumentMappings.GetValue(
                        argument,
                        Function(argumentValue) New ByRefArgument(parameters(index), DirectCast(argumentValue, BoundByRefArgumentWithCopyBack)))
                Case Else
                    Return s_argumentMappings.GetValue(
                        argument,
                        Function(argumentValue)
                            Dim lastParameterIndex = parameters.Length - 1
                            If index = lastParameterIndex AndAlso ParameterIsParamArray(parameters(lastParameterIndex)) Then
                                ' TODO: figure out if this is true:
                                '       a compiler generated argument for a ParamArray parameter is created iff 
                                '       a list of arguments (including 0 argument) is provided for ParamArray parameter in source
                                '       https://github.com/dotnet/roslyn/issues/18550
                                Dim kind = If(argument.WasCompilerGenerated AndAlso argument.Kind = BoundKind.ArrayCreation, ArgumentKind.ParamArray, ArgumentKind.Explicit)
                                Return New Argument(kind, parameters(lastParameterIndex), argumentValue)
                            Else
                                ' TODO: figure our if this is true:
                                '       a compiler generated argument for an Optional parameter is created iff
                                '       the argument is omitted from the source
                                '       https://github.com/dotnet/roslyn/issues/18550
                                Dim kind = If(argument.WasCompilerGenerated, ArgumentKind.DefaultValue, ArgumentKind.Explicit)
                                Dim parameter = parameters(index)
                                Return New Argument(kind, parameter, argumentValue)
                            End If
                        End Function)
            End Select
        End Function

        Private Shared Function ParameterIsParamArray(parameter As ParameterSymbol) As Boolean
            Return If(parameter.IsParamArray AndAlso parameter.Type.Kind = SymbolKind.ArrayType, DirectCast(parameter.Type, ArrayTypeSymbol).IsSZArray, False)
        End Function

        Private MustInherit Class ArgumentBase
            Implements IArgument

            Private ReadOnly _parameter As IParameterSymbol

            Public Sub New(parameter As IParameterSymbol)
                _parameter = parameter
            End Sub

            Public ReadOnly Property Parameter As IParameterSymbol Implements IArgument.Parameter
                Get
                    Return _parameter
                End Get
            End Property

            Public ReadOnly Property IsInvalid As Boolean Implements IOperation.IsInvalid
                Get
                    Return Me.Parameter Is Nothing OrElse Me.Value.IsInvalid
                End Get
            End Property

            Public ReadOnly Property Kind As OperationKind Implements IOperation.Kind
                Get
                    Return OperationKind.Argument
                End Get
            End Property

            Public ReadOnly Property Syntax As SyntaxNode Implements IOperation.Syntax
                Get
                    Return Me.Value?.Syntax
                End Get
            End Property

            Public MustOverride ReadOnly Property ArgumentKind As ArgumentKind Implements IArgument.ArgumentKind
            Public MustOverride ReadOnly Property Value As IOperation Implements IArgument.Value
            Public MustOverride ReadOnly Property InConversion As IOperation Implements IArgument.InConversion
            Public MustOverride ReadOnly Property OutConversion As IOperation Implements IArgument.OutConversion

            Public Sub Accept(visitor As OperationVisitor) Implements IOperation.Accept
                visitor.VisitArgument(Me)
            End Sub

            Public Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult Implements IOperation.Accept
                Return visitor.VisitArgument(Me, argument)
            End Function

            Private ReadOnly Property IOperation_Type As ITypeSymbol Implements IOperation.Type
                Get
                    Return Nothing
                End Get
            End Property

            Private ReadOnly Property IOperation_ConstantValue As [Optional](Of Object) Implements IOperation.ConstantValue
                Get
                    Return New [Optional](Of Object)()
                End Get
            End Property
        End Class

        Private NotInheritable Class Argument
            Inherits ArgumentBase

            Private ReadOnly _value As IOperation
            Private ReadOnly _kind As ArgumentKind

            Public Sub New(kind As ArgumentKind, parameter As IParameterSymbol, value As IOperation)
                MyBase.New(parameter)
                _value = value
                _kind = kind
            End Sub

            Public Overrides ReadOnly Property Value As IOperation
                Get
                    Return Me._value
                End Get
            End Property

            Public Overrides ReadOnly Property InConversion As IOperation
                Get
                    Return Nothing
                End Get
            End Property

            Public Overrides ReadOnly Property OutConversion As IOperation
                Get
                    Return Nothing
                End Get
            End Property

            Public Overrides ReadOnly Property ArgumentKind As ArgumentKind
                Get
                    Return _kind
                End Get
            End Property
        End Class

        Private NotInheritable Class ByRefArgument
            Inherits ArgumentBase

            Private ReadOnly _argument As BoundByRefArgumentWithCopyBack

            Public Sub New(parameter As IParameterSymbol, argument As BoundByRefArgumentWithCopyBack)
                MyBase.New(parameter)
                _argument = argument
            End Sub

            Public Overrides ReadOnly Property ArgumentKind As ArgumentKind
                Get
                    Return ArgumentKind.Explicit
                End Get
            End Property

            Public Overrides ReadOnly Property InConversion As IOperation
                Get
                    Return _argument.InConversion
                End Get
            End Property

            Public Overrides ReadOnly Property OutConversion As IOperation
                Get
                    Return _argument.OutConversion
                End Get
            End Property

            Public Overrides ReadOnly Property Value As IOperation
                Get
                    Return _argument.OriginalArgument
                End Get
            End Property
        End Class
    End Class

    Partial Friend Class BoundOmittedArgument
        Implements IOmittedArgumentExpression

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.OmittedArgumentExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitOmittedArgumentExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitOmittedArgumentExpression(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundParenthesized
        Implements IParenthesizedExpression

        Private ReadOnly Property IParenthesizedExpression_Operand As IOperation Implements IParenthesizedExpression.Operand
            Get
                Return Me.Expression
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ParenthesizedExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitParenthesizedExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitParenthesizedExpression(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundArrayAccess
        Implements IArrayElementReferenceExpression

        Private ReadOnly Property IArrayElementReferenceExpression_ArrayReference As IOperation Implements IArrayElementReferenceExpression.ArrayReference
            Get
                Return Me.Expression
            End Get
        End Property

        Private ReadOnly Property IArrayElementReferenceExpression_Indices As ImmutableArray(Of IOperation) Implements IArrayElementReferenceExpression.Indices
            Get
                Return Me.Indices.As(Of IOperation)()
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ArrayElementReferenceExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitArrayElementReferenceExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitArrayElementReferenceExpression(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundUnaryOperator
        Implements IUnaryOperatorExpression

        Private ReadOnly Property IHasOperatorMethodExpression_OperatorMethod As IMethodSymbol Implements IHasOperatorMethodExpression.OperatorMethod
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IHasOperatorMethodExpression_UsesOperatorMethod As Boolean Implements IHasOperatorMethodExpression.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property IUnaryOperatorExpression_Operand As IOperation Implements IUnaryOperatorExpression.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IUnaryOperatorExpression_UnaryOperationKind As UnaryOperationKind Implements IUnaryOperatorExpression.UnaryOperationKind
            Get
                Return DeriveUnaryOperationKind(Me.OperatorKind, Me.Operand)
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.UnaryOperatorExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitUnaryOperatorExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitUnaryOperatorExpression(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundUserDefinedUnaryOperator
        Implements IUnaryOperatorExpression

        Private ReadOnly Property IHasOperatorMethodExpression_OperatorMethod As IMethodSymbol Implements IHasOperatorMethodExpression.OperatorMethod
            Get
                Return If(Me.UnderlyingExpression.Kind = BoundKind.Call, Me.Call.Method, Nothing)
            End Get
        End Property

        Private ReadOnly Property IHasOperatorMethodExpression_UsesOperatorMethod As Boolean Implements IHasOperatorMethodExpression.UsesOperatorMethod
            Get
                Return IHasOperatorMethodExpression_OperatorMethod IsNot Nothing
            End Get
        End Property

        Private ReadOnly Property IUnaryOperatorExpression_Operand As IOperation Implements IUnaryOperatorExpression.Operand
            Get
                If Me.UnderlyingExpression.Kind = BoundKind.Call Then
                    Return Me.Operand
                Else
                    Return GetChildOfBadExpression(Me.UnderlyingExpression, 0)
                End If
            End Get
        End Property

        Private ReadOnly Property IUnaryOperatorExpression_UnaryOperationKind As UnaryOperationKind Implements IUnaryOperatorExpression.UnaryOperationKind
            Get
                Select Case OperatorKind And UnaryOperatorKind.OpMask
                    Case UnaryOperatorKind.Plus
                        Return UnaryOperationKind.OperatorMethodPlus
                    Case UnaryOperatorKind.Minus
                        Return UnaryOperationKind.OperatorMethodMinus
                    Case UnaryOperatorKind.Not
                        Return UnaryOperationKind.OperatorMethodBitwiseNegation
                    Case UnaryOperatorKind.IsTrue
                        Return UnaryOperationKind.OperatorMethodTrue
                    Case UnaryOperatorKind.IsFalse
                        Return UnaryOperationKind.OperatorMethodFalse
                    Case Else
                        Return UnaryOperationKind.Invalid
                End Select
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.UnaryOperatorExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitUnaryOperatorExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitUnaryOperatorExpression(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundBinaryOperator
        Implements IBinaryOperatorExpression

        Private ReadOnly Property IBinaryOperatorExpression_LeftOperand As IOperation Implements IBinaryOperatorExpression.LeftOperand
            Get
                Return Me.Left
            End Get
        End Property

        Private ReadOnly Property IBinaryOperatorExpression_BinaryOperationKind As BinaryOperationKind Implements IBinaryOperatorExpression.BinaryOperationKind
            Get
                Return DeriveBinaryOperationKind(Me.OperatorKind, Me.Left)
            End Get
        End Property

        Private ReadOnly Property IBinaryOperatorExpression_RightOperand As IOperation Implements IBinaryOperatorExpression.RightOperand
            Get
                Return Me.Right
            End Get
        End Property

        Private ReadOnly Property IHasOperatorMethodExpression_OperatorMethod As IMethodSymbol Implements IHasOperatorMethodExpression.OperatorMethod
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IHasOperatorMethodExpression_UsesOperatorMethod As Boolean Implements IHasOperatorMethodExpression.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.BinaryOperatorExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitBinaryOperatorExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitBinaryOperatorExpression(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundUserDefinedBinaryOperator
        Implements IBinaryOperatorExpression

        Private ReadOnly Property IBinaryOperatorExpression_LeftOperand As IOperation Implements IBinaryOperatorExpression.LeftOperand
            Get
                If Me.UnderlyingExpression.Kind = BoundKind.Call Then
                    Return Me.Left
                Else
                    Return GetChildOfBadExpression(Me.UnderlyingExpression, 0)
                End If
            End Get
        End Property

        Private ReadOnly Property IBinaryOperatorExpression_BinaryOperationKind As BinaryOperationKind Implements IBinaryOperatorExpression.BinaryOperationKind
            Get
                Select Case OperatorKind And BinaryOperatorKind.OpMask
                    Case BinaryOperatorKind.Add
                        Return BinaryOperationKind.OperatorMethodAdd
                    Case BinaryOperatorKind.Subtract
                        Return BinaryOperationKind.OperatorMethodSubtract
                    Case BinaryOperatorKind.Multiply
                        Return BinaryOperationKind.OperatorMethodMultiply
                    Case BinaryOperatorKind.Divide
                        Return BinaryOperationKind.OperatorMethodDivide
                    Case BinaryOperatorKind.IntegerDivide
                        Return BinaryOperationKind.OperatorMethodIntegerDivide
                    Case BinaryOperatorKind.Modulo
                        Return BinaryOperationKind.OperatorMethodRemainder
                    Case BinaryOperatorKind.And
                        Return BinaryOperationKind.OperatorMethodAnd
                    Case BinaryOperatorKind.Or
                        Return BinaryOperationKind.OperatorMethodOr
                    Case BinaryOperatorKind.Xor
                        Return BinaryOperationKind.OperatorMethodExclusiveOr
                    Case BinaryOperatorKind.AndAlso
                        Return BinaryOperationKind.OperatorMethodConditionalAnd
                    Case BinaryOperatorKind.OrElse
                        Return BinaryOperationKind.OperatorMethodConditionalOr
                    Case BinaryOperatorKind.LeftShift
                        Return BinaryOperationKind.OperatorMethodLeftShift
                    Case BinaryOperatorKind.RightShift
                        Return BinaryOperationKind.OperatorMethodRightShift
                    Case BinaryOperatorKind.LessThan
                        Return BinaryOperationKind.OperatorMethodLessThan
                    Case BinaryOperatorKind.LessThanOrEqual
                        Return BinaryOperationKind.OperatorMethodLessThanOrEqual
                    Case BinaryOperatorKind.Equals
                        Return BinaryOperationKind.OperatorMethodEquals
                    Case BinaryOperatorKind.NotEquals
                        Return BinaryOperationKind.OperatorMethodNotEquals
                    Case BinaryOperatorKind.GreaterThanOrEqual
                        Return BinaryOperationKind.OperatorMethodGreaterThanOrEqual
                    Case BinaryOperatorKind.GreaterThan
                        Return BinaryOperationKind.OperatorMethodGreaterThan
                    Case BinaryOperatorKind.Power
                        Return BinaryOperationKind.OperatorMethodPower
                    Case Else
                        Return BinaryOperationKind.Invalid
                End Select
            End Get
        End Property

        Private ReadOnly Property IBinaryOperatorExpression_RightOperand As IOperation Implements IBinaryOperatorExpression.RightOperand
            Get

                If Me.UnderlyingExpression.Kind = BoundKind.Call Then
                    Return Me.Right
                Else
                    Return GetChildOfBadExpression(Me.UnderlyingExpression, 1)
                End If
            End Get
        End Property

        Private ReadOnly Property IHasOperatorMethodExpression_OperatorMethod As IMethodSymbol Implements IHasOperatorMethodExpression.OperatorMethod
            Get
                Return If(Me.UnderlyingExpression.Kind = BoundKind.Call, Me.Call.Method, Nothing)
            End Get
        End Property

        Private ReadOnly Property IHasOperatorMethodExpression_UsesOperatorMethod As Boolean Implements IHasOperatorMethodExpression.UsesOperatorMethod
            Get
                Return IHasOperatorMethodExpression_OperatorMethod IsNot Nothing
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.BinaryOperatorExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitBinaryOperatorExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitBinaryOperatorExpression(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundBinaryConditionalExpression
        Implements INullCoalescingExpression

        Private ReadOnly Property INullCoalescingExpression_PrimaryOperand As IOperation Implements INullCoalescingExpression.PrimaryOperand
            Get
                Return Me.TestExpression
            End Get
        End Property

        Private ReadOnly Property INullCoalescingExpression_SecondaryOperand As IOperation Implements INullCoalescingExpression.SecondaryOperand
            Get
                Return Me.ElseExpression
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.NullCoalescingExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNullCoalescingExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNullCoalescingExpression(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundUserDefinedShortCircuitingOperator
        Implements IBinaryOperatorExpression

        Private ReadOnly Property IBinaryOperatorExpression_LeftOperand As IOperation Implements IBinaryOperatorExpression.LeftOperand
            Get
                Return Me.LeftOperand
            End Get
        End Property

        Private ReadOnly Property IBinaryOperatorExpression_BinaryOperationKind As BinaryOperationKind Implements IBinaryOperatorExpression.BinaryOperationKind
            Get
                Return If((Me.BitwiseOperator.OperatorKind And BinaryOperatorKind.And) <> 0, BinaryOperationKind.OperatorMethodConditionalAnd, BinaryOperationKind.OperatorMethodConditionalOr)
            End Get
        End Property

        Private ReadOnly Property IBinaryOperatorExpression_RightOperand As IOperation Implements IBinaryOperatorExpression.RightOperand
            Get
                Return Me.BitwiseOperator.Right
            End Get
        End Property

        Private ReadOnly Property IHasOperatorMethodExpression_OperatorMethod As IMethodSymbol Implements IHasOperatorMethodExpression.OperatorMethod
            Get
                Return Me.BitwiseOperator.Call.Method
            End Get
        End Property

        Private ReadOnly Property IHasOperatorMethodExpression_UsesOperatorMethod As Boolean Implements IHasOperatorMethodExpression.UsesOperatorMethod
            Get
                Return True
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.BinaryOperatorExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitBinaryOperatorExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitBinaryOperatorExpression(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundBadExpression
        Implements IInvalidExpression

        Public ReadOnly Property Children As ImmutableArray(Of IOperation) Implements IInvalidExpression.Children
            Get
                Return StaticCast(Of IOperation).From(Me.ChildBoundNodes)
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.InvalidExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitInvalidExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitInvalidExpression(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundTryCast
        Implements IConversionExpression

        Private ReadOnly Property IConversionExpression_ConversionKind As Semantics.ConversionKind Implements IConversionExpression.ConversionKind
            Get
                Return Semantics.ConversionKind.TryCast
            End Get
        End Property

        Private ReadOnly Property IConversionExpresson_IsExplicit As Boolean Implements IConversionExpression.IsExplicit
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IConversionExpression_Operand As IOperation Implements IConversionExpression.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IHasOperatorMethodExpression_OperatorMethod As IMethodSymbol Implements IHasOperatorMethodExpression.OperatorMethod
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IHasOperatorMethodExpression_UsesOperatorMethod As Boolean Implements IHasOperatorMethodExpression.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ConversionExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitConversionExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitConversionExpression(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundDirectCast
        Implements IConversionExpression

        Private ReadOnly Property IConversionExpression_ConversionKind As Semantics.ConversionKind Implements IConversionExpression.ConversionKind
            Get
                Return Semantics.ConversionKind.Cast
            End Get
        End Property

        Private ReadOnly Property IConversionExpression_IsExplicit As Boolean Implements IConversionExpression.IsExplicit
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IConversionExpression_Operand As IOperation Implements IConversionExpression.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IHasOperatorMethodExpression_OperatorMethod As IMethodSymbol Implements IHasOperatorMethodExpression.OperatorMethod
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IHasOperatorMethodExpression_UsesOperatorMethod As Boolean Implements IHasOperatorMethodExpression.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ConversionExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitConversionExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitConversionExpression(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundConversion
        Implements IConversionExpression

        Private ReadOnly Property IConversionExpression_ConversionKind As Semantics.ConversionKind Implements IConversionExpression.ConversionKind
            Get
                Return Semantics.ConversionKind.Basic
            End Get
        End Property

        Private ReadOnly Property IConversionExpression_IsExplicit As Boolean Implements IConversionExpression.IsExplicit
            Get
                Return Me.ExplicitCastInCode
            End Get
        End Property

        Private ReadOnly Property IConversinExpression_Operand As IOperation Implements IConversionExpression.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IHasOperatorMethodExpression_OperatorMethod As IMethodSymbol Implements IHasOperatorMethodExpression.OperatorMethod
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IHasOperatorMethodExpression_UsesOperatorMethod As Boolean Implements IHasOperatorMethodExpression.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ConversionExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitConversionExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitConversionExpression(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundUserDefinedConversion
        Implements IConversionExpression

        Private ReadOnly Property IConversionExpression_ConversionKind As Semantics.ConversionKind Implements IConversionExpression.ConversionKind
            Get
                Return Semantics.ConversionKind.OperatorMethod
            End Get
        End Property

        Private ReadOnly Property IConversionExpression_IsExplicit As Boolean Implements IConversionExpression.IsExplicit
            Get
                Return Not Me.WasCompilerGenerated
            End Get
        End Property

        Private ReadOnly Property IConversionExpression_Operand As IOperation Implements IConversionExpression.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IHasOperatorMethodExpression_OperatorMethod As IMethodSymbol Implements IHasOperatorMethodExpression.OperatorMethod
            Get
                Return Me.Call.Method
            End Get
        End Property

        Private ReadOnly Property IHasOperatorMethodExpression_UsesOperatorMethod As Boolean Implements IHasOperatorMethodExpression.UsesOperatorMethod
            Get
                Return True
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ConversionExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitConversionExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitConversionExpression(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundTernaryConditionalExpression
        Implements IConditionalChoiceExpression

        Private ReadOnly Property IConditionalChoiceExpression_Condition As IOperation Implements IConditionalChoiceExpression.Condition
            Get
                Return Me.Condition
            End Get
        End Property

        Private ReadOnly Property IConditionalChoiceExpression_IfFalseValue As IOperation Implements IConditionalChoiceExpression.IfFalseValue
            Get
                Return Me.WhenFalse
            End Get
        End Property

        Private ReadOnly Property IConditionalChoiceExpression_IfTrueValue As IOperation Implements IConditionalChoiceExpression.IfTrueValue
            Get
                Return Me.WhenTrue
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ConditionalChoiceExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitConditionalChoiceExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitConditionalChoiceExpression(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundTypeOf
        Implements IIsTypeExpression

        Private ReadOnly Property IIsTypeExpression_IsType As ITypeSymbol Implements IIsTypeExpression.IsType
            Get
                Return Me.TargetType
            End Get
        End Property

        Private ReadOnly Property IIsTypeExpression_Operand As IOperation Implements IIsTypeExpression.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.IsTypeExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitIsTypeExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitIsTypeExpression(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundObjectCreationExpression
        Implements IObjectCreationExpression

        Private Shared ReadOnly s_memberInitializersMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundObjectCreationExpression, Object)

        Private ReadOnly Property IObjectCreationExpression_Constructor As IMethodSymbol Implements IObjectCreationExpression.Constructor
            Get
                Return Me.ConstructorOpt
            End Get
        End Property

        Private ReadOnly Property IHasArgumentsExpression_ArgumentsInEvaluationOrder As ImmutableArray(Of IArgument) Implements IHasArgumentsExpression.ArgumentsInEvaluationOrder
            Get
                Debug.Assert(Me.ConstructorOpt IsNot Nothing OrElse Me.Arguments.IsEmpty())
                Return If(Me.ConstructorOpt Is Nothing, ImmutableArray(Of IArgument).Empty, BoundCall.DeriveArguments(Me.Arguments, Me.ConstructorOpt.Parameters))
            End Get
        End Property

        Private ReadOnly Property IObjectCreationExpression_MemberInitializers As ImmutableArray(Of ISymbolInitializer) Implements IObjectCreationExpression.MemberInitializers
            Get
                Dim initializer = s_memberInitializersMappings.GetValue(Me, Function(objectCreationStatement)
                                                                                Dim objectInitializerExpression As BoundObjectInitializerExpressionBase = Me.InitializerOpt
                                                                                If objectInitializerExpression IsNot Nothing Then
                                                                                    Dim builder = ArrayBuilder(Of ISymbolInitializer).GetInstance(objectInitializerExpression.Initializers.Length)
                                                                                    For Each memberAssignment In objectInitializerExpression.Initializers
                                                                                        Dim assignment = TryCast(memberAssignment, BoundAssignmentOperator)
                                                                                        Dim left = assignment?.Left
                                                                                        If left IsNot Nothing Then
                                                                                            Select Case left.Kind
                                                                                                Case BoundKind.FieldAccess
                                                                                                    builder.Add(New FieldInitializer(assignment.Syntax, DirectCast(left, BoundFieldAccess).FieldSymbol, assignment.Right))
                                                                                                Case BoundKind.PropertyAccess
                                                                                                    builder.Add(New PropertyInitializer(assignment.Syntax, DirectCast(left, BoundPropertyAccess).PropertySymbol, assignment.Right))
                                                                                            End Select
                                                                                        End If
                                                                                    Next
                                                                                    Return builder.ToImmutableAndFree()
                                                                                End If

                                                                                Return ImmutableArray(Of ISymbolInitializer).Empty
                                                                            End Function)

                Return DirectCast(initializer, ImmutableArray(Of ISymbolInitializer))
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ObjectCreationExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitObjectCreationExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitObjectCreationExpression(Me, argument)
        End Function

        Private NotInheritable Class FieldInitializer
            Implements IFieldInitializer

            Private _field As IFieldSymbol
            Private _syntax As SyntaxNode
            Private _value As IOperation

            Public Sub New(syntax As SyntaxNode, initializedField As IFieldSymbol, value As IOperation)
                _field = initializedField
                _syntax = syntax
                _value = value
            End Sub

            Public Sub Accept(visitor As OperationVisitor) Implements IOperation.Accept
                visitor.VisitFieldInitializer(Me)
            End Sub

            Public Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult Implements IOperation.Accept
                Return visitor.VisitFieldInitializer(Me, argument)
            End Function

            Public ReadOnly Property InitializedFields As ImmutableArray(Of IFieldSymbol) Implements IFieldInitializer.InitializedFields
                Get
                    Return ImmutableArray.Create(_field)
                End Get
            End Property

            Public ReadOnly Property Kind As OperationKind Implements IOperation.Kind
                Get
                    Return OperationKind.FieldInitializerInCreation
                End Get
            End Property

            Public ReadOnly Property IsInvalid As Boolean Implements IOperation.IsInvalid
                Get
                    Return Me.Value.IsInvalid OrElse _field Is Nothing
                End Get
            End Property

            Public ReadOnly Property Syntax As SyntaxNode Implements IOperation.Syntax
                Get
                    Return _syntax
                End Get
            End Property

            Public ReadOnly Property Value As IOperation Implements ISymbolInitializer.Value
                Get
                    Return _value
                End Get
            End Property

            Private ReadOnly Property IOperation_Type As ITypeSymbol Implements IOperation.Type
                Get
                    Return Nothing
                End Get
            End Property

            Private ReadOnly Property IOperation_ConstantValue As [Optional](Of Object) Implements IOperation.ConstantValue
                Get
                    Return New [Optional](Of Object)()
                End Get
            End Property
        End Class

        Private NotInheritable Class PropertyInitializer
            Implements IPropertyInitializer

            Private _property As IPropertySymbol
            Private _syntax As SyntaxNode
            Private _value As IOperation

            Public Sub New(syntax As SyntaxNode, initializedProperty As IPropertySymbol, value As IOperation)
                _property = initializedProperty
                _syntax = syntax
                _value = value
            End Sub

            Public Sub Accept(visitor As OperationVisitor) Implements IOperation.Accept
                visitor.VisitPropertyInitializer(Me)
            End Sub

            Public Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult Implements IOperation.Accept
                Return visitor.VisitPropertyInitializer(Me, argument)
            End Function

            Public ReadOnly Property Kind As OperationKind Implements IOperation.Kind
                Get
                    Return OperationKind.PropertyInitializerInCreation
                End Get
            End Property

            Public ReadOnly Property InitializedProperty As IPropertySymbol Implements IPropertyInitializer.InitializedProperty
                Get
                    Return _property
                End Get
            End Property

            Public ReadOnly Property IsInvalid As Boolean Implements IOperation.IsInvalid
                Get
                    Return Me.Value.IsInvalid OrElse Me.InitializedProperty Is Nothing OrElse Me.InitializedProperty.SetMethod Is Nothing
                End Get
            End Property

            Public ReadOnly Property Syntax As SyntaxNode Implements IOperation.Syntax
                Get
                    Return _syntax
                End Get
            End Property

            Public ReadOnly Property Value As IOperation Implements ISymbolInitializer.Value
                Get
                    Return _value
                End Get
            End Property

            Private ReadOnly Property IOperation_Type As ITypeSymbol Implements IOperation.Type
                Get
                    Return Nothing
                End Get
            End Property

            Private ReadOnly Property IOperation_ConstantValue As [Optional](Of Object) Implements IOperation.ConstantValue
                Get
                    Return New [Optional](Of Object)()
                End Get
            End Property
        End Class

    End Class

    Partial Friend Class BoundNewT
        Implements ITypeParameterObjectCreationExpression

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.TypeParameterObjectCreationExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitTypeParameterObjectCreationExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitTypeParameterObjectCreationExpression(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundArrayCreation
        Implements IArrayCreationExpression

        Private ReadOnly Property IArrayCreationExpression_DimensionSizes As ImmutableArray(Of IOperation) Implements IArrayCreationExpression.DimensionSizes
            Get
                Return Me.Bounds.As(Of IOperation)()
            End Get
        End Property

        Private ReadOnly Property IArrayCreationExpression_ElementType As ITypeSymbol Implements IArrayCreationExpression.ElementType
            Get
                Dim arrayType As IArrayTypeSymbol = TryCast(Me.Type, IArrayTypeSymbol)
                If arrayType IsNot Nothing Then
                    Return arrayType.ElementType
                End If

                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IArrayCreationExpression_Initializer As IArrayInitializer Implements IArrayCreationExpression.Initializer
            Get
                Dim initializer As BoundArrayInitialization = Me.InitializerOpt
                Return initializer
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ArrayCreationExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitArrayCreationExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitArrayCreationExpression(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundArrayInitialization
        Implements IArrayInitializer

        Private ReadOnly Property IArrayInitializer_ElementValues As ImmutableArray(Of IOperation) Implements IArrayInitializer.ElementValues
            Get
                Return Me.Initializers.As(Of IOperation)()
            End Get
        End Property
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ArrayInitializer
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitArrayInitializer(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitArrayInitializer(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundPropertyAccess
        Implements IIndexedPropertyReferenceExpression

        Private ReadOnly Property IMemberReferenceExpression_Instance As IOperation Implements IMemberReferenceExpression.Instance
            Get
                If Me.PropertySymbol.IsShared Then
                    Return Nothing
                Else
                    Return Me.ReceiverOpt
                End If
            End Get
        End Property

        Private ReadOnly Property IMemberReferenceExpression_Member As ISymbol Implements IMemberReferenceExpression.Member
            Get
                Return Me.PropertySymbol
            End Get
        End Property

        Private ReadOnly Property IPropertyReferenceExpression_Property As IPropertySymbol Implements IPropertyReferenceExpression.Property
            Get
                Return Me.PropertySymbol
            End Get
        End Property

        Private ReadOnly Property IHasArgumentsExpression_ArgumentsInEvaluationOrder As ImmutableArray(Of IArgument) Implements IHasArgumentsExpression.ArgumentsInEvaluationOrder
            Get
                Return BoundCall.DeriveArguments(Me.Arguments, Me.PropertySymbol.Parameters)
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return If(Me.Arguments.Length > 0, OperationKind.IndexedPropertyReferenceExpression, OperationKind.PropertyReferenceExpression)
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitIndexedPropertyReferenceExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitIndexedPropertyReferenceExpression(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundEventAccess
        Implements IEventReferenceExpression

        Private ReadOnly Property IMemberReferenceExpression_Instance As IOperation Implements IMemberReferenceExpression.Instance
            Get
                If Me.EventSymbol.IsShared Then
                    Return Nothing
                Else
                    Return Me.ReceiverOpt
                End If
            End Get
        End Property

        Private ReadOnly Property IMemberReferenceExpression_Member As ISymbol Implements IMemberReferenceExpression.Member
            Get
                Return Me.EventSymbol
            End Get
        End Property

        Private ReadOnly Property IEventReferenceExpression_Event As IEventSymbol Implements IEventReferenceExpression.Event
            Get
                Return Me.EventSymbol
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.EventReferenceExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitEventReferenceExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitEventReferenceExpression(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundDelegateCreationExpression
        Implements IMethodBindingExpression

        Private ReadOnly Property IMemberReferenceExpression_Instance As IOperation Implements IMemberReferenceExpression.Instance
            Get
                If Me.Method.IsShared Then
                    Return Nothing
                Else
                    Return Me.ReceiverOpt
                End If
            End Get
        End Property

        Private ReadOnly Property IMethodBindingExpression_IsVirtual As Boolean Implements IMethodBindingExpression.IsVirtual
            Get
                Return Me.Method IsNot Nothing AndAlso (Me.Method.IsOverridable OrElse Me.Method.IsOverrides OrElse Me.Method.IsMustOverride) AndAlso Not Me.SuppressVirtualCalls
            End Get
        End Property

        Private ReadOnly Property IMemberReferenceExpression_Member As ISymbol Implements IMemberReferenceExpression.Member
            Get
                Return Me.Method
            End Get
        End Property

        Private ReadOnly Property IMethodBindingExpression_Method As IMethodSymbol Implements IMethodBindingExpression.Method
            Get
                Return Me.Method
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.MethodBindingExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitMethodBindingExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitMethodBindingExpression(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundFieldAccess
        Implements IFieldReferenceExpression

        Private ReadOnly Property IFieldReferenceExpression_Field As IFieldSymbol Implements IFieldReferenceExpression.Field
            Get
                Return Me.FieldSymbol
            End Get
        End Property

        Private ReadOnly Property IMemberReferenceExpression_Instance As IOperation Implements IMemberReferenceExpression.Instance
            Get
                If Me.FieldSymbol.IsShared Then
                    Return Nothing
                Else
                    Return Me.ReceiverOpt
                End If
            End Get
        End Property

        Private ReadOnly Property IMemberReferenceExpression_Member As ISymbol Implements IMemberReferenceExpression.Member
            Get
                Return Me.FieldSymbol
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.FieldReferenceExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitFieldReferenceExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitFieldReferenceExpression(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundConditionalAccess
        Implements IConditionalAccessExpression

        Private ReadOnly Property IConditionalAccessExpression_ConditionalValue As IOperation Implements IConditionalAccessExpression.ConditionalValue
            Get
                Return Me.AccessExpression
            End Get
        End Property

        Private ReadOnly Property IConditionalAccessExpression_ConditionalInstance As IOperation Implements IConditionalAccessExpression.ConditionalInstance
            Get
                Return Me.Receiver
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ConditionalAccessExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitConditionalAccessExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitConditionalAccessExpression(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundConditionalAccessReceiverPlaceholder
        Implements IConditionalAccessInstanceExpression

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ConditionalAccessInstanceExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitConditionalAccessInstanceExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitConditionalAccessInstanceExpression(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundParameter
        Implements IParameterReferenceExpression

        Private ReadOnly Property IParameterReferenceExpression_Parameter As IParameterSymbol Implements IParameterReferenceExpression.Parameter
            Get
                Return Me.ParameterSymbol
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ParameterReferenceExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitParameterReferenceExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitParameterReferenceExpression(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundLocal
        Implements ILocalReferenceExpression

        Private ReadOnly Property ILocalReferenceExpression_Local As ILocalSymbol Implements ILocalReferenceExpression.Local
            Get
                Return Me.LocalSymbol
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.LocalReferenceExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitLocalReferenceExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitLocalReferenceExpression(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundLateMemberAccess
        Implements ILateBoundMemberReferenceExpression

        Private ReadOnly Property ILateBoundMemberReferenceExpression_Instance As IOperation Implements ILateBoundMemberReferenceExpression.Instance
            Get
                Return Me.ReceiverOpt
            End Get
        End Property

        Private ReadOnly Property ILateBoundMemberReferenceExpression_MemberName As String Implements ILateBoundMemberReferenceExpression.MemberName
            Get
                Return Me.NameOpt
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.LateBoundMemberReferenceExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitLateBoundMemberReferenceExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitLateBoundMemberReferenceExpression(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundFieldInitializer
        Implements IFieldInitializer

        Private ReadOnly Property IFieldInitializer_InitializedFields As ImmutableArray(Of IFieldSymbol) Implements IFieldInitializer.InitializedFields
            Get
                Return ImmutableArray(Of IFieldSymbol).CastUp(Me.InitializedFields)
            End Get
        End Property

        Private ReadOnly Property ISymbolInitializer_Value As IOperation Implements ISymbolInitializer.Value
            Get
                Return Me.InitialValue
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.FieldInitializerAtDeclaration
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitFieldInitializer(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitFieldInitializer(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundPropertyInitializer
        Implements IPropertyInitializer

        Private ReadOnly Property IPropertyInitializer_InitializedProperty As IPropertySymbol Implements IPropertyInitializer.InitializedProperty
            Get
                Return Me.InitializedProperties.FirstOrDefault()
            End Get
        End Property

        Private ReadOnly Property ISymbolInitializer_Value As IOperation Implements ISymbolInitializer.Value
            Get
                Return Me.InitialValue
            End Get
        End Property

        Protected Overrides Function StatementKind() As OperationKind
            Return OperationKind.PropertyInitializerAtDeclaration
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitPropertyInitializer(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitPropertyInitializer(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundParameterEqualsValue
        Implements IParameterInitializer

        Private ReadOnly Property IOperation_IsInvalid As Boolean Implements IOperation.IsInvalid
            Get
                Return DirectCast(Me.Value, IOperation).IsInvalid
            End Get
        End Property

        Private ReadOnly Property IOperation_Kind As OperationKind Implements IOperation.Kind
            Get
                Return OperationKind.ParameterInitializerAtDeclaration
            End Get
        End Property

        Private ReadOnly Property IOperation_Syntax As SyntaxNode Implements IOperation.Syntax
            Get
                Return Me.Syntax
            End Get
        End Property

        Private ReadOnly Property ISymbolInitializer_Value As IOperation Implements ISymbolInitializer.Value
            Get
                Return Me.Value
            End Get
        End Property

        Private ReadOnly Property IParameterInitializer_Parameter As IParameterSymbol Implements IParameterInitializer.Parameter
            Get
                Return Me._Parameter
            End Get
        End Property

        Private ReadOnly Property IOperation_Type As ITypeSymbol Implements IOperation.Type
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IOperation_ConstantValue As [Optional](Of Object) Implements IOperation.ConstantValue
            Get
                Return New [Optional](Of Object)()
            End Get
        End Property

        Public Overloads Sub Accept(visitor As OperationVisitor) Implements IOperation.Accept
            visitor.VisitParameterInitializer(Me)
        End Sub

        Public Overloads Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult Implements IOperation.Accept
            Return visitor.VisitParameterInitializer(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundTypeArguments
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundLValueToRValueWrapper
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundWithLValueExpressionPlaceholder
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundWithRValueExpressionPlaceholder
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundRValuePlaceholder
        Implements IPlaceholderExpression

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.PlaceholderExpression
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitPlaceholderExpression(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitPlaceholderExpression(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundLValuePlaceholder
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundDup
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundBadVariable
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundArrayLength
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundGetType
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundFieldInfo
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundMethodInfo
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundTypeExpression
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundTypeOrValueExpression
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundNamespaceExpression
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundNullableIsTrueOperator
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundCompoundAssignmentTargetPlaceholder
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundReferenceAssignment
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundAddressOfOperator
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundSequencePointExpression
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundMethodGroup
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundPropertyGroup
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundAttribute
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Partial Friend MustInherit Class BoundTupleExpression
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundLateInvocation
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundLateAddressOfOperator
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundNoPiaObjectCreationExpression
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundAnonymousTypeCreationExpression
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundAnonymousTypePropertyAccess
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundAnonymousTypeFieldInitializer
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundObjectInitializerExpression
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundCollectionInitializerExpression
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundArrayLiteral
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundSequence
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundValueTypeMeReference
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundPreviousSubmissionReference
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundHostObjectMemberReference
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundPseudoVariable
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundByRefArgumentPlaceholder
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundByRefArgumentWithCopyBack
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundLateBoundArgumentSupportingAssignmentWithCapture
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundLabel
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class UnboundLambda
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundQueryExpression
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundQuerySource
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundToQueryableCollectionConversion
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundQueryableSource
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundQueryClause
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundOrdering
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundQueryLambda
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundRangeVariableAssignment
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class GroupTypeInferenceLambda
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundAggregateClause
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundGroupAggregation
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundRangeVariable
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundXmlName
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundXmlNamespace
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundXmlDocument
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundXmlDeclaration
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundXmlProcessingInstruction
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundXmlComment
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundXmlAttribute
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundXmlElement
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundXmlMemberAccess
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundXmlEmbeddedExpression
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundXmlCData
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundUnstructuredExceptionHandlingCatchFilter
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundSpillSequence
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundMidResult
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundLoweredConditionalAccess
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundComplexConditionalAccessReceiver
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundNameOfOperator
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundTypeAsValueExpression
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Partial Class BoundInterpolatedStringExpression
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundModuleVersionId
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundModuleVersionIdString
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundMethodDefIndex
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundMaximumMethodDefIndex
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundInstrumentationPayloadRoot
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Partial Friend Class BoundSourceDocumentIndex
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function

        Public Overrides Sub Accept(visitor As OperationVisitor)
            visitor.VisitNoneOperation(Me)
        End Sub

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As OperationVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitNoneOperation(Me, argument)
        End Function
    End Class

    Friend Module Expression
        Friend Function DeriveUnaryOperationKind(operatorKind As UnaryOperatorKind, operand As BoundExpression) As UnaryOperationKind
            Select Case operand.Type.SpecialType
                Case SpecialType.System_Byte, SpecialType.System_Int16, SpecialType.System_Int32, SpecialType.System_Int64, SpecialType.System_SByte, SpecialType.System_UInt16, SpecialType.System_UInt32, SpecialType.System_UInt64
                    Select Case operatorKind And UnaryOperatorKind.OpMask
                        Case UnaryOperatorKind.Plus
                            Return UnaryOperationKind.IntegerPlus
                        Case UnaryOperatorKind.Minus
                            Return UnaryOperationKind.IntegerMinus
                        Case UnaryOperatorKind.Not
                            Return UnaryOperationKind.IntegerBitwiseNegation
                    End Select
                Case SpecialType.System_Single, SpecialType.System_Double
                    Select Case operatorKind And UnaryOperatorKind.OpMask
                        Case UnaryOperatorKind.Plus
                            Return UnaryOperationKind.FloatingPlus
                        Case UnaryOperatorKind.Minus
                            Return UnaryOperationKind.FloatingMinus
                    End Select
                Case SpecialType.System_Decimal
                    Select Case operatorKind And UnaryOperatorKind.OpMask
                        Case UnaryOperatorKind.Plus
                            Return UnaryOperationKind.DecimalPlus
                        Case UnaryOperatorKind.Minus
                            Return UnaryOperationKind.DecimalMinus
                    End Select
                Case SpecialType.System_Boolean
                    Select Case operatorKind And UnaryOperatorKind.OpMask
                        Case UnaryOperatorKind.Not
                            Return UnaryOperationKind.BooleanBitwiseNegation
                    End Select
                Case SpecialType.System_Object
                    Select Case operatorKind And UnaryOperatorKind.OpMask
                        Case UnaryOperatorKind.Plus
                            Return UnaryOperationKind.ObjectPlus
                        Case UnaryOperatorKind.Minus
                            Return UnaryOperationKind.ObjectMinus
                        Case UnaryOperatorKind.Not
                            Return UnaryOperationKind.ObjectNot
                    End Select
            End Select

            Return UnaryOperationKind.Invalid
        End Function

        Friend Function DeriveBinaryOperationKind(operatorKind As BinaryOperatorKind, left As BoundExpression) As BinaryOperationKind
            Select Case left.Type.SpecialType

                Case SpecialType.System_SByte, SpecialType.System_Int16, SpecialType.System_Int32, SpecialType.System_Int64
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Add
                            Return BinaryOperationKind.IntegerAdd
                        Case BinaryOperatorKind.Subtract
                            Return BinaryOperationKind.IntegerSubtract
                        Case BinaryOperatorKind.Multiply
                            Return BinaryOperationKind.IntegerMultiply
                        Case BinaryOperatorKind.IntegerDivide
                            Return BinaryOperationKind.IntegerDivide
                        Case BinaryOperatorKind.Modulo
                            Return BinaryOperationKind.IntegerRemainder
                        Case BinaryOperatorKind.And
                            Return BinaryOperationKind.IntegerAnd
                        Case BinaryOperatorKind.Or
                            Return BinaryOperationKind.IntegerOr
                        Case BinaryOperatorKind.Xor
                            Return BinaryOperationKind.IntegerExclusiveOr
                        Case BinaryOperatorKind.LeftShift
                            Return BinaryOperationKind.IntegerLeftShift
                        Case BinaryOperatorKind.RightShift
                            Return BinaryOperationKind.IntegerRightShift
                        Case BinaryOperatorKind.LessThan
                            Return BinaryOperationKind.IntegerLessThan
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return BinaryOperationKind.IntegerLessThanOrEqual
                        Case BinaryOperatorKind.Equals
                            Return BinaryOperationKind.IntegerEquals
                        Case BinaryOperatorKind.NotEquals
                            Return BinaryOperationKind.IntegerNotEquals
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return BinaryOperationKind.IntegerGreaterThanOrEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return BinaryOperationKind.IntegerGreaterThan
                    End Select
                Case SpecialType.System_Byte, SpecialType.System_UInt16, SpecialType.System_UInt32, SpecialType.System_UInt64, SpecialType.System_Char
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Add
                            Return BinaryOperationKind.UnsignedAdd
                        Case BinaryOperatorKind.Subtract
                            Return BinaryOperationKind.UnsignedSubtract
                        Case BinaryOperatorKind.Multiply
                            Return BinaryOperationKind.UnsignedMultiply
                        Case BinaryOperatorKind.IntegerDivide
                            Return BinaryOperationKind.UnsignedDivide
                        Case BinaryOperatorKind.Modulo
                            Return BinaryOperationKind.UnsignedRemainder
                        Case BinaryOperatorKind.And
                            Return BinaryOperationKind.UnsignedAnd
                        Case BinaryOperatorKind.Or
                            Return BinaryOperationKind.UnsignedOr
                        Case BinaryOperatorKind.Xor
                            Return BinaryOperationKind.UnsignedExclusiveOr
                        Case BinaryOperatorKind.LeftShift
                            Return BinaryOperationKind.UnsignedLeftShift
                        Case BinaryOperatorKind.RightShift
                            Return BinaryOperationKind.UnsignedRightShift
                        Case BinaryOperatorKind.LessThan
                            Return BinaryOperationKind.UnsignedLessThan
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return BinaryOperationKind.UnsignedLessThanOrEqual
                        Case BinaryOperatorKind.Equals
                            Return BinaryOperationKind.IntegerEquals
                        Case BinaryOperatorKind.NotEquals
                            Return BinaryOperationKind.IntegerNotEquals
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return BinaryOperationKind.UnsignedGreaterThanOrEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return BinaryOperationKind.UnsignedGreaterThan
                    End Select
                Case SpecialType.System_Single, SpecialType.System_Double
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Add
                            Return BinaryOperationKind.FloatingAdd
                        Case BinaryOperatorKind.Subtract
                            Return BinaryOperationKind.FloatingSubtract
                        Case BinaryOperatorKind.Multiply
                            Return BinaryOperationKind.FloatingMultiply
                        Case BinaryOperatorKind.Divide
                            Return BinaryOperationKind.FloatingDivide
                        Case BinaryOperatorKind.Modulo
                            Return BinaryOperationKind.FloatingRemainder
                        Case BinaryOperatorKind.Power
                            Return BinaryOperationKind.FloatingPower
                        Case BinaryOperatorKind.LessThan
                            Return BinaryOperationKind.FloatingLessThan
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return BinaryOperationKind.FloatingLessThanOrEqual
                        Case BinaryOperatorKind.Equals
                            Return BinaryOperationKind.FloatingEquals
                        Case BinaryOperatorKind.NotEquals
                            Return BinaryOperationKind.FloatingNotEquals
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return BinaryOperationKind.FloatingGreaterThanOrEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return BinaryOperationKind.FloatingGreaterThan
                    End Select
                Case SpecialType.System_Decimal
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Add
                            Return BinaryOperationKind.DecimalAdd
                        Case BinaryOperatorKind.Subtract
                            Return BinaryOperationKind.DecimalSubtract
                        Case BinaryOperatorKind.Multiply
                            Return BinaryOperationKind.DecimalMultiply
                        Case BinaryOperatorKind.Divide
                            Return BinaryOperationKind.DecimalDivide
                        Case BinaryOperatorKind.LessThan
                            Return BinaryOperationKind.DecimalLessThan
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return BinaryOperationKind.DecimalLessThanOrEqual
                        Case BinaryOperatorKind.Equals
                            Return BinaryOperationKind.DecimalEquals
                        Case BinaryOperatorKind.NotEquals
                            Return BinaryOperationKind.DecimalNotEquals
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return BinaryOperationKind.DecimalGreaterThanOrEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return BinaryOperationKind.DecimalGreaterThan
                    End Select
                Case SpecialType.System_Boolean
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.And
                            Return BinaryOperationKind.BooleanAnd
                        Case BinaryOperatorKind.Or
                            Return BinaryOperationKind.BooleanOr
                        Case BinaryOperatorKind.Xor
                            Return BinaryOperationKind.BooleanExclusiveOr
                        Case BinaryOperatorKind.AndAlso
                            Return BinaryOperationKind.BooleanConditionalAnd
                        Case BinaryOperatorKind.OrElse
                            Return BinaryOperationKind.BooleanConditionalOr
                        Case BinaryOperatorKind.Equals
                            Return BinaryOperationKind.BooleanEquals
                        Case BinaryOperatorKind.NotEquals
                            Return BinaryOperationKind.BooleanNotEquals
                    End Select
                Case SpecialType.System_String
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Concatenate
                            Return BinaryOperationKind.StringConcatenate
                        Case BinaryOperatorKind.Equals
                            Return BinaryOperationKind.StringEquals
                        Case BinaryOperatorKind.NotEquals
                            Return BinaryOperationKind.StringNotEquals
                        Case BinaryOperatorKind.Like
                            Return BinaryOperationKind.StringLike
                    End Select
                Case SpecialType.System_Object
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Add
                            Return BinaryOperationKind.ObjectAdd
                        Case BinaryOperatorKind.Subtract
                            Return BinaryOperationKind.ObjectSubtract
                        Case BinaryOperatorKind.Multiply
                            Return BinaryOperationKind.ObjectMultiply
                        Case BinaryOperatorKind.Power
                            Return BinaryOperationKind.ObjectPower
                        Case BinaryOperatorKind.IntegerDivide
                            Return BinaryOperationKind.ObjectIntegerDivide
                        Case BinaryOperatorKind.Divide
                            Return BinaryOperationKind.ObjectDivide
                        Case BinaryOperatorKind.Modulo
                            Return BinaryOperationKind.ObjectRemainder
                        Case BinaryOperatorKind.Concatenate
                            Return BinaryOperationKind.ObjectConcatenate
                        Case BinaryOperatorKind.And
                            Return BinaryOperationKind.ObjectAnd
                        Case BinaryOperatorKind.Or
                            Return BinaryOperationKind.ObjectOr
                        Case BinaryOperatorKind.Xor
                            Return BinaryOperationKind.ObjectExclusiveOr
                        Case BinaryOperatorKind.AndAlso
                            Return BinaryOperationKind.ObjectConditionalAnd
                        Case BinaryOperatorKind.OrElse
                            Return BinaryOperationKind.ObjectConditionalOr
                        Case BinaryOperatorKind.LeftShift
                            Return BinaryOperationKind.ObjectLeftShift
                        Case BinaryOperatorKind.RightShift
                            Return BinaryOperationKind.ObjectRightShift
                        Case BinaryOperatorKind.LessThan
                            Return BinaryOperationKind.ObjectLessThan
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return BinaryOperationKind.ObjectLessThanOrEqual
                        Case BinaryOperatorKind.Equals
                            Return BinaryOperationKind.ObjectVBEquals
                        Case BinaryOperatorKind.Is
                            Return BinaryOperationKind.ObjectEquals
                        Case BinaryOperatorKind.IsNot
                            Return BinaryOperationKind.ObjectNotEquals
                        Case BinaryOperatorKind.NotEquals
                            Return BinaryOperationKind.ObjectVBNotEquals
                        Case BinaryOperatorKind.Like
                            Return BinaryOperationKind.ObjectLike
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return BinaryOperationKind.ObjectGreaterThanOrEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return BinaryOperationKind.ObjectGreaterThan
                    End Select
            End Select

            If left.Type.TypeKind = TypeKind.Enum Then
                Select Case operatorKind And BinaryOperatorKind.OpMask
                    Case BinaryOperatorKind.Add
                        Return BinaryOperationKind.EnumAdd
                    Case BinaryOperatorKind.Subtract
                        Return BinaryOperationKind.EnumSubtract
                    Case BinaryOperatorKind.And
                        Return BinaryOperationKind.EnumAnd
                    Case BinaryOperatorKind.Or
                        Return BinaryOperationKind.EnumOr
                    Case BinaryOperatorKind.Xor
                        Return BinaryOperationKind.EnumExclusiveOr
                    Case BinaryOperatorKind.LessThan
                        Return BinaryOperationKind.EnumLessThan
                    Case BinaryOperatorKind.LessThanOrEqual
                        Return BinaryOperationKind.EnumLessThanOrEqual
                    Case BinaryOperatorKind.Equals
                        Return BinaryOperationKind.EnumEquals
                    Case BinaryOperatorKind.NotEquals
                        Return BinaryOperationKind.EnumNotEquals
                    Case BinaryOperatorKind.GreaterThanOrEqual
                        Return BinaryOperationKind.EnumGreaterThanOrEqual
                    Case BinaryOperatorKind.GreaterThan
                        Return BinaryOperationKind.EnumGreaterThan
                End Select
            End If

            Return BinaryOperationKind.Invalid
        End Function
    End Module
End Namespace
