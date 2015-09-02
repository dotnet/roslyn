' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Semantics

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend MustInherit Class BoundNode
        Implements IOperationSearchable

        Public Function Descendants() As IEnumerable(Of IOperation) Implements IOperationSearchable.Descendants
            Dim _list = New List(Of BoundNode)
            Dim collector = New Collector(_list)
            collector.Visit(Me)
            _list.RemoveAt(0)
            Return _list.OfType(Of IOperation)()
        End Function

        Public Function DescendantsAndSelf() As IEnumerable(Of IOperation) Implements IOperationSearchable.DescendantsAndSelf
            Dim _list = New List(Of BoundNode)
            Dim collector = New Collector(_list)
            collector.Visit(Me)
            Return _list.OfType(Of IOperation)()
        End Function

        Private Class Collector
            Inherits BoundTreeWalker

            Private nodes As List(Of BoundNode)

            Public Sub New(nodes As List(Of BoundNode))
                Me.nodes = nodes
            End Sub

            Public Overrides Function Visit(node As BoundNode) As BoundNode
                Me.nodes.Add(node)
                Return MyBase.Visit(node)
            End Function

        End Class
    End Class

    Partial Class BoundExpression
        Implements IExpression

        Private ReadOnly Property IConstantValue As Object Implements IExpression.ConstantValue
            Get
                Dim value As ConstantValue = Me.ConstantValueOpt
                If value Is Nothing Then
                    Return Nothing
                End If

                Return value.Value
            End Get
        End Property

        Private ReadOnly Property IKind As OperationKind Implements IOperation.Kind
            Get
                Return Me.ExpressionKind()
            End Get
        End Property

        Private ReadOnly Property IResultType As ITypeSymbol Implements IExpression.ResultType
            Get
                Return Me.Type
            End Get
        End Property

        Private ReadOnly Property ISyntax As SyntaxNode Implements IOperation.Syntax
            Get
                Return Me.Syntax
            End Get
        End Property

        'Protected MustOverride Function ExpressionKind() As OperationKind

        Protected Overridable Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function
    End Class

    Partial Class BoundAssignmentOperator
        Implements IAssignment
        Implements ICompoundAssignment

        Private ReadOnly Property ITarget As IReference Implements IAssignment.Target
            Get
                Return TryCast(Me.Left, IReference)
            End Get
        End Property

        Private ReadOnly Property IValue As IExpression Implements IAssignment.Value
            Get
                If ExpressionKind() = OperationKind.CompoundAssignment Then
                    Dim rightBinary As BoundBinaryOperator = TryCast(Me.Right, BoundBinaryOperator)
                    If rightBinary IsNot Nothing Then
                        Return rightBinary.Right
                    End If

                    Dim rightOperatorBinary As BoundUserDefinedBinaryOperator = TryCast(Me.Right, BoundUserDefinedBinaryOperator)
                    If rightOperatorBinary IsNot Nothing Then
                        Return rightOperatorBinary.Right
                    End If
                End If

                Return Me.Right
            End Get
        End Property

        Private ReadOnly Property IBinaryKind As BinaryOperationKind Implements ICompoundAssignment.BinaryKind
            Get
                If ExpressionKind() = OperationKind.CompoundAssignment Then
                    Dim rightBinary As BoundBinaryOperator = TryCast(Me.Right, BoundBinaryOperator)
                    If rightBinary IsNot Nothing Then
                        Return Expression.DeriveBinaryOperationKind(rightBinary.OperatorKind, Me.Left)
                    End If

                    Dim rightOperatorBinary As BoundUserDefinedBinaryOperator = TryCast(Me.Right, BoundUserDefinedBinaryOperator)
                    If rightOperatorBinary IsNot Nothing Then
                        Return Expression.DeriveBinaryOperationKind(rightOperatorBinary.OperatorKind, Me.Left)
                    End If
                End If

                Return BinaryOperationKind.None
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements IWithOperator.Operator
            Get
                If Me.IUsesOperatorMethod Then
                    Return DirectCast(Me.Right, BoundUserDefinedBinaryOperator).Call.Method
                End If

                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IWithOperator.UsesOperatorMethod
            Get
                If ExpressionKind() = OperationKind.CompoundAssignment Then
                    Return TypeOf Me.Right Is BoundUserDefinedBinaryOperator
                End If

                Return False
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Dim rightBinary As BoundBinaryOperator = TryCast(Me.Right, BoundBinaryOperator)
            If rightBinary IsNot Nothing Then
                If TypeOf rightBinary.Left Is BoundCompoundAssignmentTargetPlaceholder Then
                    Return OperationKind.CompoundAssignment
                End If
            End If

            Dim rightOperatorBinary As BoundUserDefinedBinaryOperator = TryCast(Me.Right, BoundUserDefinedBinaryOperator)
            If rightOperatorBinary IsNot Nothing Then
                If TypeOf rightOperatorBinary.Left Is BoundCompoundAssignmentTargetPlaceholder Then
                    Return OperationKind.CompoundAssignment
                End If
            End If

            Return OperationKind.Assignment
        End Function
    End Class

    Partial Class BoundMeReference
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.Instance
        End Function
    End Class

    Partial Class BoundMyBaseReference
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.BaseClassInstance
        End Function
    End Class

    Partial Class BoundMyClassReference
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ClassInstance
        End Function
    End Class

    Partial Class BoundLiteral
        Implements ILiteral

        Private ReadOnly Property ISpelling As String Implements ILiteral.Spelling
            Get
                Return Me.Syntax.ToString()
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.Literal
        End Function
    End Class

    Partial Class BoundAwaitOperator
        Implements IAwait

        Private ReadOnly Property IUpon As IExpression Implements IAwait.Upon
            Get
                Return Me.Operand
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.Await
        End Function
    End Class

    Partial Class BoundLambda
        Implements ILambda

        Private ReadOnly Property IBody As IBlock Implements ILambda.Body
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property ISignature As IMethodSymbol Implements ILambda.Signature
            Get
                Return Me.LambdaSymbol
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.Lambda
        End Function
    End Class

    Partial Class BoundCall
        Implements IInvocation

        Private Function IArgumentMatchingParameter(parameter As IParameterSymbol) As IArgument Implements IInvocation.ArgumentMatchingParameter
            Return ArgumentMatchingParameter(Me.Arguments, parameter, Me.Method.Parameters)
        End Function

        Private ReadOnly Property IArgumentsInSourceOrder As ImmutableArray(Of IArgument) Implements IInvocation.ArgumentsInSourceOrder
            Get
                Return DeriveArguments(Me.Arguments, Me.Method.Parameters)
            End Get
        End Property

        Private ReadOnly Property IArgumentsInParameterOrder As ImmutableArray(Of IArgument) Implements IInvocation.ArgumentsInParameterOrder
            Get
                Return DeriveArguments(Me.Arguments, Me.Method.Parameters)
            End Get
        End Property

        Private ReadOnly Property IIsVirtual As Boolean Implements IInvocation.IsVirtual
            Get
                Dim method As IMethodSymbol = Me.Method

                Return (method.IsVirtual OrElse method.IsAbstract OrElse method.IsOverride) AndAlso Me.ReceiverOpt.Kind <> BoundKind.MyBaseReference AndAlso Me.ReceiverOpt.Kind <> BoundKind.MyClassReference
            End Get
        End Property

        Private ReadOnly Property TargetMethod As IMethodSymbol Implements IInvocation.TargetMethod
            Get
                Return Me.Method
            End Get
        End Property

        Private ReadOnly Property IInstance As IExpression Implements IInvocation.Instance
            Get
                Return Me.ReceiverOpt
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.Invocation
        End Function

        Friend Shared Function ArgumentMatchingParameter(arguments As ImmutableArray(Of BoundExpression), parameter As IParameterSymbol, parameters As ImmutableArray(Of Symbols.ParameterSymbol)) As IArgument
            Dim index As Integer = parameter.Ordinal
            If index <= arguments.Length Then
                Return DeriveArgument(index, arguments(index), parameters)
            End If

            Return Nothing
        End Function

        Friend Shared Function DeriveArguments(boundArguments As ImmutableArray(Of BoundExpression), parameters As ImmutableArray(Of Symbols.ParameterSymbol)) As ImmutableArray(Of IArgument)
            Dim argumentsLength As Integer = boundArguments.Length
            Dim arguments As ImmutableArray(Of IArgument).Builder = ImmutableArray.CreateBuilder(Of IArgument)(argumentsLength)
            For index As Integer = 0 To argumentsLength - 1 Step 1
                arguments.Add(DeriveArgument(index, boundArguments(index), parameters))
            Next

            Return arguments.ToImmutable()
        End Function

        Private Shared ArgumentMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundExpression, IArgument)

        Private Shared Function DeriveArgument(index As Integer, argument As BoundExpression, parameters As ImmutableArray(Of Symbols.ParameterSymbol)) As IArgument
            Select Case argument.Kind
                Case BoundKind.ByRefArgumentWithCopyBack
                    Return ArgumentMappings.GetValue(argument, Function(a) New ByRefArgument(parameters(index), DirectCast(argument, BoundByRefArgumentWithCopyBack)))
                Case Else
                    ' Apparently the VB bound trees don't encode named arguments, which seems unnecesarily lossy.
                    Return ArgumentMappings.GetValue(argument, Function(a) If(index >= parameters.Length - 1 AndAlso parameters.Length > 0 AndAlso parameters(parameters.Length - 1).IsParamArray, New Argument(ArgumentKind.ParamArray, parameters(parameters.Length - 1), a), New Argument(ArgumentKind.Positional, parameters(index), a)))
            End Select
        End Function

        Private Class Argument
            Implements IArgument

            Private ReadOnly _value As IExpression
            Private ReadOnly _kind As ArgumentKind
            Private ReadOnly _parameter As IParameterSymbol

            Public Sub New(kind As ArgumentKind, parameter As IParameterSymbol, value As IExpression)
                _value = value
                _kind = kind
                _parameter = parameter
            End Sub

            Public ReadOnly Property Kind As ArgumentKind Implements IArgument.Kind
                Get
                    Return _kind
                End Get
            End Property

            Public ReadOnly Property Parameter As IParameterSymbol Implements IArgument.Parameter
                Get
                    Return _parameter
                End Get
            End Property

            Public ReadOnly Property Value As IExpression Implements IArgument.Value
                Get
                    Return Me._value
                End Get
            End Property

            Public ReadOnly Property InConversion As IExpression Implements IArgument.InConversion
                Get
                    Return Nothing
                End Get
            End Property

            Public ReadOnly Property OutConversion As IExpression Implements IArgument.OutConversion
                Get
                    Return Nothing
                End Get
            End Property
        End Class

        Private Class ByRefArgument
            Implements IArgument

            Private ReadOnly _parameter As IParameterSymbol
            Private ReadOnly _argument As BoundByRefArgumentWithCopyBack

            Public Sub New(parameter As IParameterSymbol, argument As BoundByRefArgumentWithCopyBack)
                _parameter = parameter
                _argument = argument
            End Sub

            Public ReadOnly Property InConversion As IExpression Implements IArgument.InConversion
                Get
                    Return _argument.InConversion
                End Get
            End Property

            Public ReadOnly Property Kind As ArgumentKind Implements IArgument.Kind
                Get
                    ' Do the VB bound trees encode named arguments?
                    Return ArgumentKind.Positional
                End Get
            End Property

            Public ReadOnly Property OutConversion As IExpression Implements IArgument.OutConversion
                Get
                    Return _argument.OutConversion
                End Get
            End Property

            Public ReadOnly Property Parameter As IParameterSymbol Implements IArgument.Parameter
                Get
                    Return _parameter
                End Get
            End Property

            Public ReadOnly Property Value As IExpression Implements IArgument.Value
                Get
                    Return _argument.OriginalArgument
                End Get
            End Property
        End Class
    End Class

    Partial Class BoundOmittedArgument
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.Omitted
        End Function
    End Class

    Partial Class BoundParenthesized
        Implements IParenthesized

        Private ReadOnly Property IOperand As IExpression Implements IParenthesized.Operand
            Get
                Return Me.Expression
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.Parenthesized
        End Function
    End Class

    Partial Class BoundArrayAccess
        Implements IArrayElementReference

        Private ReadOnly Property IArrayReference As IExpression Implements IArrayElementReference.ArrayReference
            Get
                Return Me.Expression
            End Get
        End Property

        Private ReadOnly Property IIndices As ImmutableArray(Of IExpression) Implements IArrayElementReference.Indices
            Get
                Return Me.Indices.As(Of IExpression)()
            End Get
        End Property

        Private ReadOnly Property IReferenceKind As ReferenceKind Implements IReference.ReferenceKind
            Get
                Return ReferenceKind.ArrayElement
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ArrayElementReference
        End Function
    End Class

    Partial Class BoundUnaryOperator
        Implements IUnary

        Private ReadOnly Property IOperator As IMethodSymbol Implements IWithOperator.Operator
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IWithOperator.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property IOperand As IExpression Implements IUnary.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IUnaryKind As UnaryOperationKind Implements IUnary.UnaryKind
            Get
                Return DeriveUnaryOperationKind(Me.OperatorKind, Me.Operand)
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.UnaryOperator
        End Function
    End Class

    Partial Class BoundUserDefinedUnaryOperator
        Implements IUnary

        Private ReadOnly Property IOperator As IMethodSymbol Implements IWithOperator.Operator
            Get
                Return Me.Call.Method
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IWithOperator.UsesOperatorMethod
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IOperand As IExpression Implements IUnary.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IUnaryKind As UnaryOperationKind Implements IUnary.UnaryKind
            Get
                Select Case OperatorKind And UnaryOperatorKind.OpMask
                    Case UnaryOperatorKind.Plus
                        Return UnaryOperationKind.OperatorPlus
                    Case UnaryOperatorKind.Minus
                        Return UnaryOperationKind.OperatorMinus
                    Case UnaryOperatorKind.Not
                        Return UnaryOperationKind.OperatorBitwiseNegation
                End Select

                Return UnaryOperationKind.None
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.UnaryOperator
        End Function
    End Class

    Partial Class BoundBinaryOperator
        Implements IBinary
        Implements IRelational

        Private ReadOnly Property ILeft As IExpression Implements IBinary.Left, IRelational.Left
            Get
                Return Me.Left
            End Get
        End Property

        Private ReadOnly Property IBinaryKind As BinaryOperationKind Implements IBinary.BinaryKind
            Get
                Return DeriveBinaryOperationKind(Me.OperatorKind, Me.Left)
            End Get
        End Property

        Private ReadOnly Property IRight As IExpression Implements IBinary.Right, IRelational.Right
            Get
                Return Me.Right
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements IWithOperator.Operator
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IWithOperator.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property RelationalKind As RelationalOperationKind Implements IRelational.RelationalKind
            Get
                Return DeriveRelationalOperationKind(Me.OperatorKind, Me.Left)
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.BinaryOperator
        End Function

    End Class

    Partial Class BoundUserDefinedBinaryOperator
        Implements IBinary
        Implements IRelational

        Private ReadOnly Property ILeft As IExpression Implements IBinary.Left, IRelational.Left
            Get
                Return Me.Left
            End Get
        End Property

        Private ReadOnly Property IBinaryKind As BinaryOperationKind Implements IBinary.BinaryKind
            Get
                Select Case OperatorKind And BinaryOperatorKind.OpMask
                    Case BinaryOperatorKind.Add
                        Return BinaryOperationKind.OperatorAdd
                    Case BinaryOperatorKind.Subtract
                        Return BinaryOperationKind.OperatorSubtract
                    Case BinaryOperatorKind.Multiply
                        Return BinaryOperationKind.OperatorMultiply
                    Case BinaryOperatorKind.Divide
                        Return BinaryOperationKind.OperatorDivide
                    Case BinaryOperatorKind.Modulo
                        Return BinaryOperationKind.OperatorRemainder
                    Case BinaryOperatorKind.And
                        Return BinaryOperationKind.OperatorAnd
                    Case BinaryOperatorKind.Or
                        Return BinaryOperationKind.OperatorOr
                    Case BinaryOperatorKind.Xor
                        Return BinaryOperationKind.OperatorExclusiveOr
                    Case BinaryOperatorKind.AndAlso
                        Return BinaryOperationKind.OperatorConditionalAnd
                    Case BinaryOperatorKind.OrElse
                        Return BinaryOperationKind.OperatorConditionalOr
                    Case BinaryOperatorKind.LeftShift
                        Return BinaryOperationKind.OperatorLeftShift
                    Case BinaryOperatorKind.RightShift
                        Return BinaryOperationKind.OperatorRightShift
                End Select

                Return BinaryOperationKind.None
            End Get
        End Property

        Private ReadOnly Property IRelationalKind As RelationalOperationKind Implements IRelational.RelationalKind
            Get
                Select Case OperatorKind And BinaryOperatorKind.OpMask
                    Case BinaryOperatorKind.LessThan
                        Return RelationalOperationKind.OperatorLess
                    Case BinaryOperatorKind.LessThanOrEqual
                        Return RelationalOperationKind.OperatorLessEqual
                    Case BinaryOperatorKind.Equals
                        Return RelationalOperationKind.OperatorEqual
                    Case BinaryOperatorKind.NotEquals
                        Return RelationalOperationKind.OperatorNotEqual
                    Case BinaryOperatorKind.GreaterThanOrEqual
                        Return RelationalOperationKind.OperatorGreaterEqual
                    Case BinaryOperatorKind.GreaterThan
                        Return RelationalOperationKind.OperatorGreater
                End Select

                Return RelationalOperationKind.None
            End Get
        End Property

        Private ReadOnly Property IRight As IExpression Implements IBinary.Right, IRelational.Right
            Get
                Return Me.Right
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements IWithOperator.Operator
            Get
                Return Me.Call.Method
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IWithOperator.UsesOperatorMethod
            Get
                Return True
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Select Case Me.OperatorKind And BinaryOperatorKind.OpMask
                Case BinaryOperatorKind.Add, BinaryOperatorKind.Concatenate, BinaryOperatorKind.Subtract, BinaryOperatorKind.Multiply, BinaryOperatorKind.Divide,
                    BinaryOperatorKind.IntegerDivide, BinaryOperatorKind.Modulo, BinaryOperatorKind.Power, BinaryOperatorKind.LeftShift, BinaryOperatorKind.RightShift,
                    BinaryOperatorKind.And, BinaryOperatorKind.Or, BinaryOperatorKind.Xor, BinaryOperatorKind.AndAlso, BinaryOperatorKind.OrElse

                    Return OperationKind.BinaryOperator

                Case BinaryOperatorKind.LessThan, BinaryOperatorKind.LessThanOrEqual, BinaryOperatorKind.Equals, BinaryOperatorKind.NotEquals,
                    BinaryOperatorKind.Is, BinaryOperatorKind.IsNot, BinaryOperatorKind.Like, BinaryOperatorKind.GreaterThanOrEqual, BinaryOperatorKind.GreaterThan

                    Return OperationKind.RelationalOperator
            End Select

            Return OperationKind.None
        End Function
    End Class

    Partial Class BoundBinaryConditionalExpression
        Implements INullCoalescing

        Private ReadOnly Property IPrimary As IExpression Implements INullCoalescing.Primary
            Get
                Return Me.TestExpression
            End Get
        End Property

        Private ReadOnly Property ISecondary As IExpression Implements INullCoalescing.Secondary
            Get
                Return Me.ElseExpression
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.NullCoalescing
        End Function
    End Class

    Partial Class BoundUserDefinedShortCircuitingOperator
        Implements IBinary

        Private ReadOnly Property ILeft As IExpression Implements IBinary.Left
            Get
                Return Me.LeftOperand
            End Get
        End Property

        Private ReadOnly Property IBinaryKind As BinaryOperationKind Implements IBinary.BinaryKind
            Get
                Return If((Me.BitwiseOperator.OperatorKind And BinaryOperatorKind.And) <> 0, BinaryOperationKind.OperatorConditionalAnd, BinaryOperationKind.OperatorConditionalOr)
            End Get
        End Property

        Private ReadOnly Property IRight As IExpression Implements IBinary.Right
            Get
                Return Me.BitwiseOperator.Right
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements IWithOperator.Operator
            Get
                Return Me.BitwiseOperator.Call.Method
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IWithOperator.UsesOperatorMethod
            Get
                Return True
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.BinaryOperator
        End Function
    End Class

    Partial Class BoundBadExpression
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function
    End Class

    Partial Class BoundTryCast
        Implements IConversion

        Private ReadOnly Property IConversion As Semantics.ConversionKind Implements IConversion.Conversion
            Get
                Return Semantics.ConversionKind.AsCast
            End Get
        End Property

        Private ReadOnly Property IIsExplicit As Boolean Implements IConversion.IsExplicit
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IOperand As IExpression Implements IConversion.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements IWithOperator.Operator
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IWithOperator.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.Conversion
        End Function
    End Class

    Partial Class BoundDirectCast
        Implements IConversion

        Private ReadOnly Property IConversion As Semantics.ConversionKind Implements IConversion.Conversion
            Get
                Return Semantics.ConversionKind.Cast
            End Get
        End Property

        Private ReadOnly Property IIsExplicit As Boolean Implements IConversion.IsExplicit
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IOperand As IExpression Implements IConversion.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements IWithOperator.Operator
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IWithOperator.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.Conversion
        End Function
    End Class

    Partial Class BoundConversion
        Implements IConversion

        Private ReadOnly Property IConversion As Semantics.ConversionKind Implements IConversion.Conversion
            Get
                Return Semantics.ConversionKind.Basic
            End Get
        End Property

        Private ReadOnly Property IIsExplicit As Boolean Implements IConversion.IsExplicit
            Get
                Return Me.ExplicitCastInCode
            End Get
        End Property

        Private ReadOnly Property IOperand As IExpression Implements IConversion.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements IWithOperator.Operator
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IWithOperator.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.Conversion
        End Function
    End Class

    Partial Class BoundUserDefinedConversion
        Implements IConversion

        Private ReadOnly Property IConversion As Semantics.ConversionKind Implements IConversion.Conversion
            Get
                Return Semantics.ConversionKind.Operator
            End Get
        End Property

        Private ReadOnly Property IIsExplicit As Boolean Implements IConversion.IsExplicit
            Get
                Return Not Me.WasCompilerGenerated
            End Get
        End Property

        Private ReadOnly Property IOperand As IExpression Implements IConversion.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements IWithOperator.Operator
            Get
                Return Me.Call.Method
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IWithOperator.UsesOperatorMethod
            Get
                Return True
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.Conversion
        End Function
    End Class

    Partial Class BoundTernaryConditionalExpression
        Implements IConditionalChoice

        Private ReadOnly Property ICondition As IExpression Implements IConditionalChoice.Condition
            Get
                Return Me.Condition
            End Get
        End Property

        Private ReadOnly Property IIfFalse As IExpression Implements IConditionalChoice.IfFalse
            Get
                Return Me.WhenFalse
            End Get
        End Property

        Private ReadOnly Property IIfTrue As IExpression Implements IConditionalChoice.IfTrue
            Get
                Return Me.WhenTrue
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ConditionalChoice
        End Function
    End Class

    Partial Class BoundTypeOf
        Implements IIs

        Private ReadOnly Property IIsType As ITypeSymbol Implements IIs.IsType
            Get
                Return Me.TargetType
            End Get
        End Property

        Private ReadOnly Property IOperand As IExpression Implements IIs.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.Is
        End Function
    End Class

    Partial Class BoundObjectCreationExpression
        Implements IObjectCreation

        Private Function IArgumentMatchingParameter(parameter As IParameterSymbol) As IArgument Implements IObjectCreation.ArgumentMatchingParameter
            Return BoundCall.ArgumentMatchingParameter(Me.Arguments, parameter, Me.ConstructorOpt.Parameters)
        End Function

        Private ReadOnly Property IConstructor As IMethodSymbol Implements IObjectCreation.Constructor
            Get
                Return Me.ConstructorOpt
            End Get
        End Property

        Private ReadOnly Property IConstructorArguments As ImmutableArray(Of IArgument) Implements IObjectCreation.ConstructorArguments
            Get
                Return BoundCall.DeriveArguments(Me.Arguments, Me.ConstructorOpt.Parameters)
            End Get
        End Property

        Private ReadOnly Property IMemberInitializers As ImmutableArray(Of IMemberInitializer) Implements IObjectCreation.MemberInitializers
            Get
                Dim initializer As BoundObjectInitializerExpressionBase = Me.InitializerOpt
                If initializer IsNot Nothing Then
                    ' ZZZ What's the representation in bound trees?
                End If

                Return ImmutableArray.Create(Of IMemberInitializer)()
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ObjectCreation
        End Function
    End Class

    Partial Class BoundNewT
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.TypeParameterObjectCreation
        End Function
    End Class

    Partial Class BoundArrayCreation
        Implements IArrayCreation

        Private ReadOnly Property IDimensionSizes As ImmutableArray(Of IExpression) Implements IArrayCreation.DimensionSizes
            Get
                Return Me.Bounds.As(Of IExpression)()
            End Get
        End Property

        Private ReadOnly Property IElementType As ITypeSymbol Implements IArrayCreation.ElementType
            Get
                Dim arrayType As IArrayTypeSymbol = TryCast(Me.Type, IArrayTypeSymbol)
                If arrayType IsNot Nothing Then
                    Return arrayType.ElementType
                End If

                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IElementValues As IArrayInitializer Implements IArrayCreation.ElementValues
            Get
                Dim initializer As BoundArrayInitialization = Me.InitializerOpt
                If initializer IsNot Nothing Then
                    Return MakeInitializer(initializer)
                End If

                Return Nothing
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ArrayCreation
        End Function

        Private Shared ArrayInitializerMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundArrayInitialization, IArrayInitializer)

        Private Function MakeInitializer(initializer As BoundArrayInitialization) As IArrayInitializer
            Return ArrayInitializerMappings.GetValue(
                initializer,
                Function(arrayInitalizer)
                    Dim dimension As ImmutableArray(Of IArrayInitializer).Builder = ImmutableArray.CreateBuilder(Of IArrayInitializer)(arrayInitalizer.Initializers.Length)

                    For index As Integer = 0 To arrayInitalizer.Initializers.Length - 1
                        Dim elementInitializer As BoundExpression = arrayInitalizer.Initializers(index)
                        Dim elementArray As BoundArrayInitialization = TryCast(elementInitializer, BoundArrayInitialization)
                        dimension.Add(If(elementArray IsNot Nothing, MakeInitializer(elementArray), New ElementInitializer(elementInitializer)))
                    Next

                    Return New DimensionInitializer(dimension.ToImmutable())
                End Function)

        End Function

        Private Class ElementInitializer
            Implements IExpressionArrayInitializer

            ReadOnly _element As BoundExpression

            Public Sub New(element As BoundExpression)
                Me._element = element
            End Sub

            ReadOnly Property ElementValue As IExpression Implements IExpressionArrayInitializer.ElementValue
                Get
                    Return Me._element
                End Get
            End Property

            ReadOnly Property ArrayClass As ArrayInitializerKind Implements IExpressionArrayInitializer.ArrayClass
                Get
                    Return ArrayInitializerKind.Expression
                End Get
            End Property
        End Class

        Private Class DimensionInitializer
            Implements IDimensionArrayInitializer

            ReadOnly _dimension As ImmutableArray(Of IArrayInitializer)

            Public Sub New(dimension As ImmutableArray(Of IArrayInitializer))
                Me._dimension = dimension
            End Sub

            ReadOnly Property ElementValues As ImmutableArray(Of IArrayInitializer) Implements IDimensionArrayInitializer.ElementValues
                Get
                    Return Me._dimension
                End Get
            End Property

            ReadOnly Property ArrayClass As ArrayInitializerKind Implements IDimensionArrayInitializer.ArrayClass
                Get
                    Return ArrayInitializerKind.Dimension
                End Get
            End Property
        End Class
    End Class

    Partial Class BoundPropertyAccess
        Implements IPropertyReference

        Private ReadOnly Property IInstance As IExpression Implements IMemberReference.Instance
            Get
                Return Me.ReceiverOpt
            End Get
        End Property

        Private ReadOnly Property IProperty As IPropertySymbol Implements IPropertyReference.Property
            Get
                Return Me.PropertySymbol
            End Get
        End Property

        Private ReadOnly Property IReferenceKind As ReferenceKind Implements IReference.ReferenceKind
            Get
                Return If(Me.ReceiverOpt IsNot Nothing, ReferenceKind.InstanceProperty, ReferenceKind.StaticProperty)
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.PropertyReference
        End Function
    End Class

    Partial Class BoundFieldAccess
        Implements IFieldReference

        Private ReadOnly Property IField As IFieldSymbol Implements IFieldReference.Field
            Get
                Return Me.FieldSymbol
            End Get
        End Property

        Private ReadOnly Property IInstance As IExpression Implements IMemberReference.Instance
            Get
                Return Me.ReceiverOpt
            End Get
        End Property

        Private ReadOnly Property IReferenceKind As ReferenceKind Implements IReference.ReferenceKind
            Get
                Return If(Me.ReceiverOpt IsNot Nothing, ReferenceKind.InstanceField, ReferenceKind.StaticField)
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.FieldReference
        End Function
    End Class

    Partial Class BoundConditionalAccess
        Implements IConditionalAccess

        Private ReadOnly Property IAccess As IExpression Implements IConditionalAccess.Access
            Get
                Return Me.AccessExpression
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ConditionalAccess
        End Function
    End Class

    Partial Class BoundParameter
        Implements IParameterReference

        Private ReadOnly Property IParameter As IParameterSymbol Implements IParameterReference.Parameter
            Get
                Return Me.ParameterSymbol
            End Get
        End Property

        Private ReadOnly Property IReferenceKind As ReferenceKind Implements IReference.ReferenceKind
            Get
                Return ReferenceKind.Parameter
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ParameterReference
        End Function
    End Class

    Partial Class BoundLocal
        Implements ILocalReference

        Private ReadOnly Property ILocal As ILocalSymbol Implements ILocalReference.Local
            Get
                Return Me.LocalSymbol
            End Get
        End Property

        Private ReadOnly Property IReferenceKind As ReferenceKind Implements IReference.ReferenceKind
            Get
                Return ReferenceKind.Local
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.LocalReference
        End Function
    End Class

    Partial Class BoundLateMemberAccess
        Implements ILateBoundMemberReference

        Private ReadOnly Property IInstance As IExpression Implements ILateBoundMemberReference.Instance
            Get
                Return Me.ReceiverOpt
            End Get
        End Property

        Private ReadOnly Property IMemberName As String Implements ILateBoundMemberReference.MemberName
            Get
                Return Me.NameOpt
            End Get
        End Property

        Private ReadOnly Property IReferenceKind As ReferenceKind Implements IReference.ReferenceKind
            Get
                Return ReferenceKind.LateBoundMember
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.LateBoundMemberReference
        End Function
    End Class

    Module Expression
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

            Return UnaryOperationKind.None
        End Function

        Friend Function DeriveBinaryOperationKind(operatorKind As BinaryOperatorKind, left As BoundExpression) As BinaryOperationKind
            Select Case left.Type.SpecialType
                Case SpecialType.System_Byte, SpecialType.System_UInt16, SpecialType.System_UInt32, SpecialType.System_UInt64
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
                    End Select
                Case SpecialType.System_SByte, SpecialType.System_Int16, SpecialType.System_Int32, SpecialType.System_Int64
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
                    End Select
                Case SpecialType.System_String
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Concatenate
                            Return BinaryOperationKind.StringConcatenation
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
                            Return BinaryOperationKind.ObjectConcatenation
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
                End Select
            End If

            Return BinaryOperationKind.None
        End Function

        Friend Function DeriveRelationalOperationKind(operatorKind As BinaryOperatorKind, left As BoundExpression) As RelationalOperationKind
            Select Case left.Type.SpecialType
                Case SpecialType.System_Byte, SpecialType.System_UInt16, SpecialType.System_UInt32, SpecialType.System_UInt64, SpecialType.System_Char
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.LessThan
                            Return RelationalOperationKind.UnsignedLess
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return RelationalOperationKind.UnsignedLessEqual
                        Case BinaryOperatorKind.Equals
                            Return RelationalOperationKind.IntegerEqual
                        Case BinaryOperatorKind.NotEquals
                            Return RelationalOperationKind.IntegerNotEqual
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return RelationalOperationKind.UnsignedGreaterEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return RelationalOperationKind.UnsignedGreater
                    End Select
                Case SpecialType.System_SByte, SpecialType.System_Int16, SpecialType.System_Int32, SpecialType.System_Int64
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.LessThan
                            Return RelationalOperationKind.IntegerLess
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return RelationalOperationKind.IntegerLessEqual
                        Case BinaryOperatorKind.Equals
                            Return RelationalOperationKind.IntegerEqual
                        Case BinaryOperatorKind.NotEquals
                            Return RelationalOperationKind.IntegerNotEqual
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return RelationalOperationKind.IntegerGreaterEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return RelationalOperationKind.IntegerGreater
                    End Select
                Case SpecialType.System_Single, SpecialType.System_Double
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.LessThan
                            Return RelationalOperationKind.FloatingLess
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return RelationalOperationKind.FloatingLessEqual
                        Case BinaryOperatorKind.Equals
                            Return RelationalOperationKind.FloatingEqual
                        Case BinaryOperatorKind.NotEquals
                            Return RelationalOperationKind.FloatingNotEqual
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return RelationalOperationKind.FloatingGreaterEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return RelationalOperationKind.FloatingGreater
                    End Select
                Case SpecialType.System_Decimal
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.LessThan
                            Return RelationalOperationKind.DecimalLess
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return RelationalOperationKind.DecimalLessEqual
                        Case BinaryOperatorKind.Equals
                            Return RelationalOperationKind.DecimalEqual
                        Case BinaryOperatorKind.NotEquals
                            Return RelationalOperationKind.DecimalNotEqual
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return RelationalOperationKind.DecimalGreaterEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return RelationalOperationKind.DecimalGreater
                    End Select
                Case SpecialType.System_Boolean
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Equals
                            Return RelationalOperationKind.BooleanEqual
                        Case BinaryOperatorKind.NotEquals
                            Return RelationalOperationKind.BooleanNotEqual
                    End Select
                Case SpecialType.System_String
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Equals
                            Return RelationalOperationKind.StringEqual
                        Case BinaryOperatorKind.NotEquals
                            Return RelationalOperationKind.StringNotEqual
                        Case BinaryOperatorKind.Like
                            Return RelationalOperationKind.StringLike
                    End Select
                Case SpecialType.System_Object
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.LessThan
                            Return RelationalOperationKind.ObjectLess
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return RelationalOperationKind.ObjectLessEqual
                        Case BinaryOperatorKind.Equals
                            Return RelationalOperationKind.ObjectVBEqual
                        Case BinaryOperatorKind.Is
                            Return RelationalOperationKind.ObjectEqual
                        Case BinaryOperatorKind.IsNot
                            Return RelationalOperationKind.ObjectNotEqual
                        Case BinaryOperatorKind.NotEquals
                            Return RelationalOperationKind.ObjectVBNotEqual
                        Case BinaryOperatorKind.Like
                            Return RelationalOperationKind.ObjectLike
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return RelationalOperationKind.ObjectGreaterEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return RelationalOperationKind.ObjectGreater
                    End Select
            End Select

            If left.Type.TypeKind = TypeKind.Enum Then
                Select Case operatorKind And BinaryOperatorKind.OpMask
                    Case BinaryOperatorKind.LessThan
                        Return RelationalOperationKind.EnumLess
                    Case BinaryOperatorKind.LessThanOrEqual
                        Return RelationalOperationKind.EnumLessEqual
                    Case BinaryOperatorKind.Equals
                        Return RelationalOperationKind.EnumEqual
                    Case BinaryOperatorKind.NotEquals
                        Return RelationalOperationKind.EnumNotEqual
                    Case BinaryOperatorKind.GreaterThanOrEqual
                        Return RelationalOperationKind.EnumGreaterEqual
                    Case BinaryOperatorKind.GreaterThan
                        Return RelationalOperationKind.EnumGreater
                End Select
            End If

            Return RelationalOperationKind.None
        End Function
    End Module
End Namespace
