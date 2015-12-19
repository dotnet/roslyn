' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Semantics
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend MustInherit Class BoundNode
        Implements IOperationSearchable

        Public Function Descendants() As IEnumerable(Of IOperation) Implements IOperationSearchable.Descendants
            Dim _list = New List(Of IOperation)
            Dim collector = New Collector(_list)
            collector.Visit(Me)
            _list.RemoveAt(0)
            Return _list
        End Function

        Public Function DescendantsAndSelf() As IEnumerable(Of IOperation) Implements IOperationSearchable.DescendantsAndSelf
            Dim _list = New List(Of IOperation)
            Dim collector = New Collector(_list)
            collector.Visit(Me)
            Return _list
        End Function

        Private Class Collector
            Inherits BoundTreeWalkerWithStackGuard

            Private nodes As List(Of IOperation)

            Public Sub New(nodes As List(Of IOperation))
                Me.nodes = nodes
            End Sub

            Public Overrides Function Visit(node As BoundNode) As BoundNode
                Dim operation = TryCast(node, IOperation)
                If operation IsNot Nothing Then
                    Me.nodes.Add(operation)
                    Select Case operation.Kind
                        Case OperationKind.InvocationExpression
                            Me.nodes.AddRange(DirectCast(operation, IInvocationExpression).ArgumentsInSourceOrder)
                        Case OperationKind.ObjectCreationExpression
                            Dim objCreationExp = DirectCast(operation, IObjectCreationExpression)
                            Me.nodes.AddRange(objCreationExp.ConstructorArguments)
                            Me.nodes.AddRange(objCreationExp.MemberInitializers)
                        Case OperationKind.SwitchSection
                            ' "Case Else" clause is not a bound node, so it needs to be added explicitly.
                            Dim caseClauses = DirectCast(operation, ICase).Clauses
                            If caseClauses.IsSingle Then
                                Dim caseClause = caseClauses(0)
                                If caseClause.CaseKind = CaseKind.Default Then
                                    Me.nodes.Add(caseClause)
                                End If
                            End If
                    End Select
                End If
                Return MyBase.Visit(node)
            End Function

        End Class
    End Class

    Partial Class BoundExpression
        Implements IExpression

        Private ReadOnly Property IConstantValue As [Optional](Of Object) Implements IExpression.ConstantValue
            Get
                Dim value As ConstantValue = Me.ConstantValueOpt
                If value Is Nothing Then
                    Return New [Optional](Of Object)()
                End If

                Return New [Optional](Of Object)(value.Value)
            End Get
        End Property

        Private ReadOnly Property IKind As OperationKind Implements IOperation.Kind
            Get
                Return Me.ExpressionKind()
            End Get
        End Property

        Private ReadOnly Property IIsInvalid As Boolean Implements IOperation.IsInvalid
            Get
                Return Me.HasErrors
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

        Protected Overridable Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function
    End Class

    Partial Class BoundAssignmentOperator
        Implements IAssignmentExpression
        Implements ICompoundAssignmentExpression

        Private ReadOnly Property ITarget As IReferenceExpression Implements IAssignmentExpression.Target
            Get
                Return TryCast(Me.Left, IReferenceExpression)
            End Get
        End Property

        Private ReadOnly Property IValue As IExpression Implements IAssignmentExpression.Value
            Get
                If ExpressionKind() = OperationKind.CompoundAssignmentExpression Then
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

        Private ReadOnly Property IBinaryKind As BinaryOperationKind Implements ICompoundAssignmentExpression.BinaryKind
            Get
                If ExpressionKind() = OperationKind.CompoundAssignmentExpression Then
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

        Private ReadOnly Property IOperator As IMethodSymbol Implements IHasOperatorExpression.Operator
            Get
                If Me.IUsesOperatorMethod Then
                    Return DirectCast(Me.Right, BoundUserDefinedBinaryOperator).Call.Method
                End If

                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IHasOperatorExpression.UsesOperatorMethod
            Get
                If ExpressionKind() = OperationKind.CompoundAssignmentExpression Then
                    Return TypeOf Me.Right Is BoundUserDefinedBinaryOperator
                End If

                Return False
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Dim rightBinary As BoundBinaryOperator = TryCast(Me.Right, BoundBinaryOperator)
            If rightBinary IsNot Nothing Then
                If TypeOf rightBinary.Left Is BoundCompoundAssignmentTargetPlaceholder Then
                    Return OperationKind.CompoundAssignmentExpression
                End If
            End If

            Dim rightOperatorBinary As BoundUserDefinedBinaryOperator = TryCast(Me.Right, BoundUserDefinedBinaryOperator)
            If rightOperatorBinary IsNot Nothing Then
                If TypeOf rightOperatorBinary.Left Is BoundCompoundAssignmentTargetPlaceholder Then
                    Return OperationKind.CompoundAssignmentExpression
                End If
            End If

            Return OperationKind.AssignmentExpression
        End Function
    End Class

    Partial Class BoundMeReference
        Implements IInstanceReferenceExpression

        Private ReadOnly Property IIsExplicit As Boolean Implements IInstanceReferenceExpression.IsExplicit
            Get
                Return Not Me.WasCompilerGenerated
            End Get
        End Property

        Private ReadOnly Property IParameter As IParameterSymbol Implements IParameterReferenceExpression.Parameter
            Get
                Return DirectCast(Me.ExpressionSymbol, IParameterSymbol)
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.InstanceReferenceExpression
        End Function
    End Class

    Partial Class BoundMyBaseReference
        Implements IInstanceReferenceExpression

        Private ReadOnly Property IIsExplicit As Boolean Implements IInstanceReferenceExpression.IsExplicit
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IParameter As IParameterSymbol Implements IParameterReferenceExpression.Parameter
            Get
                Return DirectCast(Me.ExpressionSymbol, IParameterSymbol)
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.BaseClassInstanceReferenceExpression
        End Function
    End Class

    Partial Class BoundMyClassReference
        Implements IInstanceReferenceExpression

        Private ReadOnly Property IIsExplicit As Boolean Implements IInstanceReferenceExpression.IsExplicit
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IParameter As IParameterSymbol Implements IParameterReferenceExpression.Parameter
            Get
                Return DirectCast(Me.ExpressionSymbol, IParameterSymbol)
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ClassInstanceReferenceExpression
        End Function
    End Class

    Partial Class BoundLiteral
        Implements ILiteralExpression

        Private ReadOnly Property ISpelling As String Implements ILiteralExpression.Spelling
            Get
                Return Me.Syntax.ToString()
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.LiteralExpression
        End Function
    End Class

    Partial Class BoundAwaitOperator
        Implements IAwaitExpression

        Private ReadOnly Property IUpon As IExpression Implements IAwaitExpression.Upon
            Get
                Return Me.Operand
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.AwaitExpression
        End Function
    End Class

    Partial Class BoundLambda
        Implements ILambdaExpression

        Private ReadOnly Property IBody As IBlockStatement Implements ILambdaExpression.Body
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property ISignature As IMethodSymbol Implements ILambdaExpression.Signature
            Get
                Return Me.LambdaSymbol
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.LambdaExpression
        End Function
    End Class

    Partial Class BoundCall
        Implements IInvocationExpression

        Private Function IArgumentMatchingParameter(parameter As IParameterSymbol) As IArgument Implements IInvocationExpression.ArgumentMatchingParameter
            Return ArgumentMatchingParameter(Me.Arguments, parameter, Me.Method.Parameters)
        End Function

        Private ReadOnly Property IArgumentsInSourceOrder As ImmutableArray(Of IArgument) Implements IInvocationExpression.ArgumentsInSourceOrder
            Get
                Return DeriveArguments(Me.Arguments, Me.Method.Parameters)
            End Get
        End Property

        Private ReadOnly Property IArgumentsInParameterOrder As ImmutableArray(Of IArgument) Implements IInvocationExpression.ArgumentsInParameterOrder
            Get
                Return DeriveArguments(Me.Arguments, Me.Method.Parameters)
            End Get
        End Property

        Private ReadOnly Property IIsVirtual As Boolean Implements IInvocationExpression.IsVirtual
            Get
                Dim method As IMethodSymbol = Me.Method

                Return (method.IsVirtual OrElse method.IsAbstract OrElse method.IsOverride) AndAlso Me.ReceiverOpt.Kind <> BoundKind.MyBaseReference AndAlso Me.ReceiverOpt.Kind <> BoundKind.MyClassReference
            End Get
        End Property

        Private ReadOnly Property TargetMethod As IMethodSymbol Implements IInvocationExpression.TargetMethod
            Get
                Return Me.Method
            End Get
        End Property

        Private ReadOnly Property IInstance As IExpression Implements IInvocationExpression.Instance
            Get
                Return Me.ReceiverOpt
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.InvocationExpression
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

            Public ReadOnly Property ArgumentKind As ArgumentKind Implements IArgument.ArgumentKind
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
                    Return Me.Value.Syntax
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

            Public ReadOnly Property ArgumentKind As ArgumentKind Implements IArgument.ArgumentKind
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
                    Return Me.Value.Syntax
                End Get
            End Property
        End Class
    End Class

    Partial Class BoundOmittedArgument
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.OmittedArgumentExpression
        End Function
    End Class

    Partial Class BoundParenthesized
        Implements IParenthesizedExpression

        Private ReadOnly Property IOperand As IExpression Implements IParenthesizedExpression.Operand
            Get
                Return Me.Expression
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ParenthesizedExpression
        End Function
    End Class

    Partial Class BoundArrayAccess
        Implements IArrayElementReferenceExpression

        Private ReadOnly Property IArrayReference As IExpression Implements IArrayElementReferenceExpression.ArrayReference
            Get
                Return Me.Expression
            End Get
        End Property

        Private ReadOnly Property IIndices As ImmutableArray(Of IExpression) Implements IArrayElementReferenceExpression.Indices
            Get
                Return Me.Indices.As(Of IExpression)()
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ArrayElementReferenceExpression
        End Function
    End Class

    Partial Class BoundUnaryOperator
        Implements IUnaryOperatorExpression

        Private ReadOnly Property IOperator As IMethodSymbol Implements IHasOperatorExpression.Operator
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IHasOperatorExpression.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property IOperand As IExpression Implements IUnaryOperatorExpression.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IUnaryKind As UnaryOperationKind Implements IUnaryOperatorExpression.UnaryOperationKind
            Get
                Return DeriveUnaryOperationKind(Me.OperatorKind, Me.Operand)
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.UnaryOperatorExpression
        End Function
    End Class

    Partial Class BoundUserDefinedUnaryOperator
        Implements IUnaryOperatorExpression

        Private ReadOnly Property IOperator As IMethodSymbol Implements IHasOperatorExpression.Operator
            Get
                Return Me.Call.Method
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IHasOperatorExpression.UsesOperatorMethod
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IOperand As IExpression Implements IUnaryOperatorExpression.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IUnaryKind As UnaryOperationKind Implements IUnaryOperatorExpression.UnaryOperationKind
            Get
                Select Case OperatorKind And UnaryOperatorKind.OpMask
                    Case UnaryOperatorKind.Plus
                        Return UnaryOperationKind.OperatorPlus
                    Case UnaryOperatorKind.Minus
                        Return UnaryOperationKind.OperatorMinus
                    Case UnaryOperatorKind.Not
                        Return UnaryOperationKind.OperatorBitwiseNegation
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(OperatorKind And UnaryOperatorKind.OpMask)
                End Select
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.UnaryOperatorExpression
        End Function
    End Class

    Partial Class BoundBinaryOperator
        Implements IBinaryOperatorExpression

        Private ReadOnly Property ILeft As IExpression Implements IBinaryOperatorExpression.Left
            Get
                Return Me.Left
            End Get
        End Property

        Private ReadOnly Property IBinaryKind As BinaryOperationKind Implements IBinaryOperatorExpression.BinaryOperationKind
            Get
                Return DeriveBinaryOperationKind(Me.OperatorKind, Me.Left)
            End Get
        End Property

        Private ReadOnly Property IRight As IExpression Implements IBinaryOperatorExpression.Right
            Get
                Return Me.Right
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements IHasOperatorExpression.Operator
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IHasOperatorExpression.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.BinaryOperatorExpression
        End Function

    End Class

    Partial Class BoundUserDefinedBinaryOperator
        Implements IBinaryOperatorExpression

        Private ReadOnly Property ILeft As IExpression Implements IBinaryOperatorExpression.Left
            Get
                Return Me.Left
            End Get
        End Property

        Private ReadOnly Property IBinaryKind As BinaryOperationKind Implements IBinaryOperatorExpression.BinaryOperationKind
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
                    Case BinaryOperatorKind.LessThan
                        Return BinaryOperationKind.OperatorLessThan
                    Case BinaryOperatorKind.LessThanOrEqual
                        Return BinaryOperationKind.OperatorLessThanOrEqual
                    Case BinaryOperatorKind.Equals
                        Return BinaryOperationKind.OperatorEquals
                    Case BinaryOperatorKind.NotEquals
                        Return BinaryOperationKind.OperatorNotEquals
                    Case BinaryOperatorKind.GreaterThanOrEqual
                        Return BinaryOperationKind.OperatorGreaterThanOrEqual
                    Case BinaryOperatorKind.GreaterThan
                        Return BinaryOperationKind.OperatorGreaterThan
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(OperatorKind And BinaryOperatorKind.OpMask)
                End Select
            End Get
        End Property

        Private ReadOnly Property IRight As IExpression Implements IBinaryOperatorExpression.Right
            Get
                Return Me.Right
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements IHasOperatorExpression.Operator
            Get
                Return Me.Call.Method
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IHasOperatorExpression.UsesOperatorMethod
            Get
                Return True
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Select Case Me.OperatorKind And BinaryOperatorKind.OpMask
                Case BinaryOperatorKind.Add, BinaryOperatorKind.Concatenate, BinaryOperatorKind.Subtract, BinaryOperatorKind.Multiply, BinaryOperatorKind.Divide,
                    BinaryOperatorKind.IntegerDivide, BinaryOperatorKind.Modulo, BinaryOperatorKind.Power, BinaryOperatorKind.LeftShift, BinaryOperatorKind.RightShift,
                    BinaryOperatorKind.And, BinaryOperatorKind.Or, BinaryOperatorKind.Xor, BinaryOperatorKind.AndAlso, BinaryOperatorKind.OrElse,
                    BinaryOperatorKind.LessThan, BinaryOperatorKind.LessThanOrEqual, BinaryOperatorKind.Equals, BinaryOperatorKind.NotEquals,
                    BinaryOperatorKind.Is, BinaryOperatorKind.IsNot, BinaryOperatorKind.Like, BinaryOperatorKind.GreaterThanOrEqual, BinaryOperatorKind.GreaterThan

                    Return OperationKind.BinaryOperatorExpression
            End Select

            Throw ExceptionUtilities.UnexpectedValue(Me.OperatorKind And BinaryOperatorKind.OpMask)
        End Function
    End Class

    Partial Class BoundBinaryConditionalExpression
        Implements INullCoalescingExpression

        Private ReadOnly Property IPrimary As IExpression Implements INullCoalescingExpression.Primary
            Get
                Return Me.TestExpression
            End Get
        End Property

        Private ReadOnly Property ISecondary As IExpression Implements INullCoalescingExpression.Secondary
            Get
                Return Me.ElseExpression
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.NullCoalescingExpression
        End Function
    End Class

    Partial Class BoundUserDefinedShortCircuitingOperator
        Implements IBinaryOperatorExpression

        Private ReadOnly Property ILeft As IExpression Implements IBinaryOperatorExpression.Left
            Get
                Return Me.LeftOperand
            End Get
        End Property

        Private ReadOnly Property IBinaryKind As BinaryOperationKind Implements IBinaryOperatorExpression.BinaryOperationKind
            Get
                Return If((Me.BitwiseOperator.OperatorKind And BinaryOperatorKind.And) <> 0, BinaryOperationKind.OperatorConditionalAnd, BinaryOperationKind.OperatorConditionalOr)
            End Get
        End Property

        Private ReadOnly Property IRight As IExpression Implements IBinaryOperatorExpression.Right
            Get
                Return Me.BitwiseOperator.Right
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements IHasOperatorExpression.Operator
            Get
                Return Me.BitwiseOperator.Call.Method
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IHasOperatorExpression.UsesOperatorMethod
            Get
                Return True
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.BinaryOperatorExpression
        End Function
    End Class

    Partial Class BoundBadExpression
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.InvalidExpression
        End Function
    End Class

    Partial Class BoundTryCast
        Implements IConversionExpression

        Private ReadOnly Property IConversion As Semantics.ConversionKind Implements IConversionExpression.ConversionKind
            Get
                Return Semantics.ConversionKind.AsCast
            End Get
        End Property

        Private ReadOnly Property IIsExplicit As Boolean Implements IConversionExpression.IsExplicit
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IOperand As IExpression Implements IConversionExpression.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements IHasOperatorExpression.Operator
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IHasOperatorExpression.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ConversionExpression
        End Function
    End Class

    Partial Class BoundDirectCast
        Implements IConversionExpression

        Private ReadOnly Property IConversion As Semantics.ConversionKind Implements IConversionExpression.ConversionKind
            Get
                Return Semantics.ConversionKind.Cast
            End Get
        End Property

        Private ReadOnly Property IIsExplicit As Boolean Implements IConversionExpression.IsExplicit
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IOperand As IExpression Implements IConversionExpression.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements IHasOperatorExpression.Operator
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IHasOperatorExpression.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ConversionExpression
        End Function
    End Class

    Partial Class BoundConversion
        Implements IConversionExpression

        Private ReadOnly Property IConversion As Semantics.ConversionKind Implements IConversionExpression.ConversionKind
            Get
                Return Semantics.ConversionKind.Basic
            End Get
        End Property

        Private ReadOnly Property IIsExplicit As Boolean Implements IConversionExpression.IsExplicit
            Get
                Return Me.ExplicitCastInCode
            End Get
        End Property

        Private ReadOnly Property IOperand As IExpression Implements IConversionExpression.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements IHasOperatorExpression.Operator
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IHasOperatorExpression.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ConversionExpression
        End Function
    End Class

    Partial Class BoundUserDefinedConversion
        Implements IConversionExpression

        Private ReadOnly Property IConversion As Semantics.ConversionKind Implements IConversionExpression.ConversionKind
            Get
                Return Semantics.ConversionKind.Operator
            End Get
        End Property

        Private ReadOnly Property IIsExplicit As Boolean Implements IConversionExpression.IsExplicit
            Get
                Return Not Me.WasCompilerGenerated
            End Get
        End Property

        Private ReadOnly Property IOperand As IExpression Implements IConversionExpression.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements IHasOperatorExpression.Operator
            Get
                Return Me.Call.Method
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IHasOperatorExpression.UsesOperatorMethod
            Get
                Return True
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ConversionExpression
        End Function
    End Class

    Partial Class BoundTernaryConditionalExpression
        Implements IConditionalChoiceExpression

        Private ReadOnly Property ICondition As IExpression Implements IConditionalChoiceExpression.Condition
            Get
                Return Me.Condition
            End Get
        End Property

        Private ReadOnly Property IIfFalse As IExpression Implements IConditionalChoiceExpression.IfFalse
            Get
                Return Me.WhenFalse
            End Get
        End Property

        Private ReadOnly Property IIfTrue As IExpression Implements IConditionalChoiceExpression.IfTrue
            Get
                Return Me.WhenTrue
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ConditionalChoiceExpression
        End Function
    End Class

    Partial Class BoundTypeOf
        Implements IIsExpression

        Private ReadOnly Property IIsType As ITypeSymbol Implements IIsExpression.IsType
            Get
                Return Me.TargetType
            End Get
        End Property

        Private ReadOnly Property IOperand As IExpression Implements IIsExpression.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.IsExpression
        End Function
    End Class

    Partial Class BoundObjectCreationExpression
        Implements IObjectCreationExpression

        Private Function IArgumentMatchingParameter(parameter As IParameterSymbol) As IArgument Implements IObjectCreationExpression.ArgumentMatchingParameter
            Return BoundCall.ArgumentMatchingParameter(Me.Arguments, parameter, Me.ConstructorOpt.Parameters)
        End Function

        Private ReadOnly Property IConstructor As IMethodSymbol Implements IObjectCreationExpression.Constructor
            Get
                Return Me.ConstructorOpt
            End Get
        End Property

        Private ReadOnly Property IConstructorArguments As ImmutableArray(Of IArgument) Implements IObjectCreationExpression.ConstructorArguments
            Get
                Return BoundCall.DeriveArguments(Me.Arguments, Me.ConstructorOpt.Parameters)
            End Get
        End Property

        Private ReadOnly Property IMemberInitializers As ImmutableArray(Of IMemberInitializer) Implements IObjectCreationExpression.MemberInitializers
            Get
                Dim objInitializerExp As BoundObjectInitializerExpressionBase = Me.InitializerOpt
                If objInitializerExp IsNot Nothing Then
                    Dim builder = ArrayBuilder(Of IMemberInitializer).GetInstance(objInitializerExp.Initializers.Length)
                    For Each memberAssignment In objInitializerExp.Initializers
                        Dim assignment = TryCast(memberAssignment, BoundAssignmentOperator)
                        Dim left = assignment?.Left
                        If left IsNot Nothing Then
                            Select Case left.Kind
                                Case BoundKind.FieldAccess
                                    builder.Add(New FieldInitializer(assignment.Syntax, DirectCast(left, BoundFieldAccess).FieldSymbol, assignment.Right))
                                Case BoundKind.PropertyAccess
                                    builder.Add(New PropertyInitializer(assignment.Syntax, DirectCast(left, BoundPropertyAccess).PropertySymbol.SetMethod, assignment.Right))
                            End Select
                        End If
                    Next
                    Return builder.ToImmutableAndFree()
                End If

                Return ImmutableArray.Create(Of IMemberInitializer)()
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ObjectCreationExpression
        End Function

        Private Class FieldInitializer
            Implements IFieldInitializer

            Private _field As IFieldSymbol
            Private _syntax As SyntaxNode
            Private _value As IExpression

            Public Sub New(syntax As SyntaxNode, field As IFieldSymbol, value As IExpression)
                _field = field
                _syntax = syntax
                _value = value
            End Sub

            Public ReadOnly Property Field As IFieldSymbol Implements IFieldInitializer.Field
                Get
                    Return _field
                End Get
            End Property

            Public ReadOnly Property Kind As OperationKind Implements IOperation.Kind
                Get
                    Return OperationKind.FieldInitializer
                End Get
            End Property

            Public ReadOnly Property MemberInitializerKind As MemberInitializerKind Implements IMemberInitializer.MemberInitializerKind
                Get
                    Return MemberInitializerKind.Field
                End Get
            End Property

            Public ReadOnly Property IsInvalid As Boolean Implements IOperation.IsInvalid
                Get
                    Return Me.Value.IsInvalid OrElse Me.Field Is Nothing
                End Get
            End Property

            Public ReadOnly Property Syntax As SyntaxNode Implements IOperation.Syntax
                Get
                    Return _syntax
                End Get
            End Property

            Public ReadOnly Property Value As IExpression Implements IMemberInitializer.Value
                Get
                    Return _value
                End Get
            End Property
        End Class

        Private Class PropertyInitializer
            Implements IPropertyInitializer

            Private _setter As IMethodSymbol
            Private _syntax As SyntaxNode
            Private _value As IExpression

            Public Sub New(syntax As SyntaxNode, setter As IMethodSymbol, value As IExpression)
                _setter = setter
                _syntax = syntax
                _value = value
            End Sub

            Public ReadOnly Property Kind As OperationKind Implements IOperation.Kind
                Get
                    Return OperationKind.PropertyInitializer
                End Get
            End Property

            Public ReadOnly Property MemberInitializerKind As MemberInitializerKind Implements IMemberInitializer.MemberInitializerKind
                Get
                    Return MemberInitializerKind.Property
                End Get
            End Property

            Public ReadOnly Property Setter As IMethodSymbol Implements IPropertyInitializer.Setter
                Get
                    Return _setter
                End Get
            End Property

            Public ReadOnly Property IsInvalid As Boolean Implements IOperation.IsInvalid
                Get
                    Return Me.Value.IsInvalid OrElse Me.Setter Is Nothing
                End Get
            End Property

            Public ReadOnly Property Syntax As SyntaxNode Implements IOperation.Syntax
                Get
                    Return _syntax
                End Get
            End Property

            Public ReadOnly Property Value As IExpression Implements IMemberInitializer.Value
                Get
                    Return _value
                End Get
            End Property
        End Class

    End Class

    Partial Class BoundNewT
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.TypeParameterObjectCreationExpression
        End Function
    End Class

    Partial Class BoundArrayCreation
        Implements IArrayCreationExpression

        Private ReadOnly Property IDimensionSizes As ImmutableArray(Of IExpression) Implements IArrayCreationExpression.DimensionSizes
            Get
                Return Me.Bounds.As(Of IExpression)()
            End Get
        End Property

        Private ReadOnly Property IElementType As ITypeSymbol Implements IArrayCreationExpression.ElementType
            Get
                Dim arrayType As IArrayTypeSymbol = TryCast(Me.Type, IArrayTypeSymbol)
                If arrayType IsNot Nothing Then
                    Return arrayType.ElementType
                End If

                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IInitializer As IArrayInitializer Implements IArrayCreationExpression.Initializer
            Get
                Dim initializer As BoundArrayInitialization = Me.InitializerOpt
                Return initializer
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ArrayCreationExpression
        End Function
    End Class

    Partial Class BoundArrayInitialization
        Implements IArrayInitializer

        Public ReadOnly Property ElementValues As ImmutableArray(Of IExpression) Implements IArrayInitializer.ElementValues
            Get
                Return Me.Initializers.As(Of IExpression)()
            End Get
        End Property
        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ArrayInitializer
        End Function
    End Class

    Partial Class BoundPropertyAccess
        Implements IPropertyReferenceExpression

        Private ReadOnly Property IInstance As IExpression Implements IMemberReferenceExpression.Instance
            Get
                Return Me.ReceiverOpt
            End Get
        End Property

        Private ReadOnly Property IProperty As IPropertySymbol Implements IPropertyReferenceExpression.Property
            Get
                Return Me.PropertySymbol
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.PropertyReferenceExpression
        End Function
    End Class

    Partial Class BoundFieldAccess
        Implements IFieldReferenceExpression

        Private ReadOnly Property IField As IFieldSymbol Implements IFieldReferenceExpression.Field
            Get
                Return Me.FieldSymbol
            End Get
        End Property

        Private ReadOnly Property IInstance As IExpression Implements IMemberReferenceExpression.Instance
            Get
                Return Me.ReceiverOpt
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.FieldReferenceExpression
        End Function
    End Class

    Partial Class BoundConditionalAccess
        Implements IConditionalAccessExpression

        Private ReadOnly Property IAccess As IExpression Implements IConditionalAccessExpression.Access
            Get
                Return Me.AccessExpression
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ConditionalAccessExpression
        End Function
    End Class

    Partial Class BoundParameter
        Implements IParameterReferenceExpression

        Private ReadOnly Property IParameter As IParameterSymbol Implements IParameterReferenceExpression.Parameter
            Get
                Return Me.ParameterSymbol
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.ParameterReferenceExpression
        End Function
    End Class

    Partial Class BoundLocal
        Implements ILocalReferenceExpression

        Private ReadOnly Property ILocal As ILocalSymbol Implements ILocalReferenceExpression.Local
            Get
                Return Me.LocalSymbol
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.LocalReferenceExpression
        End Function
    End Class

    Partial Class BoundLateMemberAccess
        Implements ILateBoundMemberReferenceExpression

        Private ReadOnly Property IInstance As IExpression Implements ILateBoundMemberReferenceExpression.Instance
            Get
                Return Me.ReceiverOpt
            End Get
        End Property

        Private ReadOnly Property IMemberName As String Implements ILateBoundMemberReferenceExpression.MemberName
            Get
                Return Me.NameOpt
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.LateBoundMemberReferenceExpression
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

            Throw ExceptionUtilities.UnexpectedValue(operatorKind And UnaryOperatorKind.OpMask)
        End Function

        Friend Function DeriveBinaryOperationKind(operatorKind As BinaryOperatorKind, left As BoundExpression) As BinaryOperationKind
            Select Case left.Type.SpecialType
                Case SpecialType.System_Byte, SpecialType.System_UInt16, SpecialType.System_UInt32, SpecialType.System_UInt64, SpecialType.System_Char
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
                            Return BinaryOperationKind.StringConcatenation
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


            Throw ExceptionUtilities.UnexpectedValue(operatorKind And BinaryOperatorKind.OpMask)
        End Function
    End Module
End Namespace
