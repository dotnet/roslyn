Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

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
        Implements Semantics.IExpression

        Private ReadOnly Property IConstantValue As Object Implements Semantics.IExpression.ConstantValue
            Get
                Dim value As ConstantValue = Me.ConstantValueOpt
                If value Is Nothing Then
                    Return Nothing
                End If

                Return value.Value
            End Get
        End Property

        Private ReadOnly Property IKind As Semantics.OperationKind Implements Semantics.IOperation.Kind
            Get
                Return Me.ExpressionKind()
            End Get
        End Property

        Private ReadOnly Property IResultType As ITypeSymbol Implements Semantics.IExpression.ResultType
            Get
                Return Me.Type
            End Get
        End Property

        Private ReadOnly Property ISyntax As SyntaxNode Implements Semantics.IOperation.Syntax
            Get
                Return Me.Syntax
            End Get
        End Property

        'Protected MustOverride Function ExpressionKind() As Unified.ExpressionKind

        Protected Overridable Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.None
        End Function
    End Class

    Partial Class BoundAssignmentOperator
        Implements Semantics.IAssignment
        Implements Semantics.ICompoundAssignment

        Private ReadOnly Property ITarget As Semantics.IReference Implements Semantics.IAssignment.Target
            Get
                Return TryCast(Me.Left, Semantics.IReference)
            End Get
        End Property

        Private ReadOnly Property IValue As Semantics.IExpression Implements Semantics.IAssignment.Value
            Get
                If ExpressionKind() = Semantics.OperationKind.CompoundAssignment Then
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

        Private ReadOnly Property IOperation As Semantics.BinaryOperatorCode Implements Semantics.ICompoundAssignment.Operation
            Get
                If ExpressionKind() = Semantics.OperationKind.CompoundAssignment Then
                    Dim rightBinary As BoundBinaryOperator = TryCast(Me.Right, BoundBinaryOperator)
                    If rightBinary IsNot Nothing Then
                        Return Expression.DeriveBinaryOperatorCode(rightBinary.OperatorKind, Me.Left)
                    End If

                    Dim rightOperatorBinary As BoundUserDefinedBinaryOperator = TryCast(Me.Right, BoundUserDefinedBinaryOperator)
                    If rightOperatorBinary IsNot Nothing Then
                        Return Expression.DeriveBinaryOperatorCode(rightOperatorBinary.OperatorKind, Me.Left)
                    End If
                End If

                Return Semantics.BinaryOperatorCode.None
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements Semantics.IOperator.Operator
            Get
                If Me.IUsesOperatorMethod Then
                    Return DirectCast(Me.Right, BoundUserDefinedBinaryOperator).Call.Method
                End If

                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements Semantics.IOperator.UsesOperatorMethod
            Get
                If ExpressionKind() = Semantics.OperationKind.CompoundAssignment Then
                    Return TypeOf Me.Right Is BoundUserDefinedBinaryOperator
                End If

                Return False
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Dim rightBinary As BoundBinaryOperator = TryCast(Me.Right, BoundBinaryOperator)
            If rightBinary IsNot Nothing Then
                If TypeOf rightBinary.Left Is BoundCompoundAssignmentTargetPlaceholder Then
                    Return Semantics.OperationKind.CompoundAssignment
                End If
            End If

            Dim rightOperatorBinary As BoundUserDefinedBinaryOperator = TryCast(Me.Right, BoundUserDefinedBinaryOperator)
            If rightOperatorBinary IsNot Nothing Then
                If TypeOf rightOperatorBinary.Left Is BoundCompoundAssignmentTargetPlaceholder Then
                    Return Semantics.OperationKind.CompoundAssignment
                End If
            End If

            Return Semantics.OperationKind.Assignment
        End Function
    End Class

    Partial Class BoundMeReference
        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.Instance
        End Function
    End Class

    Partial Class BoundMyBaseReference
        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.BaseClassInstance
        End Function
    End Class

    Partial Class BoundMyClassReference
        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.ClassInstance
        End Function
    End Class

    Partial Class BoundLiteral
        Implements Semantics.ILiteral

        Private ReadOnly Property ILiteralClass As Semantics.LiteralKind Implements Semantics.ILiteral.LiteralClass
            Get
                Return Semantics.Expression.DeriveLiteralKind(Me.Type)
            End Get
        End Property

        Private ReadOnly Property ISpelling As String Implements Semantics.ILiteral.Spelling
            Get
                Return Me.Syntax.ToString()
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.Literal
        End Function
    End Class

    Partial Class BoundAwaitOperator
        Implements Semantics.IAwait

        Private ReadOnly Property IUpon As Semantics.IExpression Implements Semantics.IAwait.Upon
            Get
                Return Me.Operand
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.Await
        End Function
    End Class

    Partial Class BoundLambda
        Implements Semantics.ILambda

        Private ReadOnly Property IBody As Semantics.IBlock Implements Semantics.ILambda.Body
            Get
                Return Me.Body
            End Get
        End Property

        Private ReadOnly Property ISignature As IMethodSymbol Implements Semantics.ILambda.Signature
            Get
                Return Me.LambdaSymbol
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.Lambda
        End Function
    End Class

    Partial Class BoundCall
        Implements Semantics.IInvocation

        Private Function IArgumentMatchingParameter(parameter As IParameterSymbol) As Semantics.IArgument Implements Semantics.IInvocation.ArgumentMatchingParameter
            Return ArgumentMatchingParameter(Me.Arguments, parameter)
        End Function

        Private ReadOnly Property IArguments As ImmutableArray(Of Semantics.IArgument) Implements Semantics.IInvocation.Arguments
            Get
                Return DeriveArguments(Me.Arguments)
            End Get
        End Property

        Private ReadOnly Property IInvocationClass As Semantics.InvocationKind Implements Semantics.IInvocation.InvocationClass
            Get
                If Me.Method.IsShared Then
                    Return Semantics.InvocationKind.Static
                End If

                If Me.Method.IsOverridable AndAlso Me.ReceiverOpt.Kind <> BoundKind.MyBaseReference AndAlso Me.ReceiverOpt.Kind <> BoundKind.MyClassReference Then
                    Return Semantics.InvocationKind.Virtual
                End If

                Return Semantics.InvocationKind.NonVirtualInstance
            End Get
        End Property

        Private ReadOnly Property TargetMethod As IMethodSymbol Implements Semantics.IInvocation.TargetMethod
            Get
                Return Me.Method
            End Get
        End Property

        Private ReadOnly Property IInstance As Semantics.IExpression Implements Semantics.IInvocation.Instance
            Get
                Return Me.ReceiverOpt
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.Invocation
        End Function

        Friend Shared Function ArgumentMatchingParameter(arguments As ImmutableArray(Of BoundExpression), parameter As IParameterSymbol) As Semantics.IArgument
            Dim index As Integer = parameter.Ordinal
            If index <= arguments.Length Then
                Return DeriveArgument(arguments(index))
            End If

            Return Nothing
        End Function

        Friend Shared Function DeriveArguments(boundArguments As ImmutableArray(Of BoundExpression)) As ImmutableArray(Of Semantics.IArgument)
            Dim argumentsLength As Integer = boundArguments.Length
            Dim arguments As ArrayBuilder(Of Semantics.IArgument) = ArrayBuilder(Of Semantics.IArgument).GetInstance(argumentsLength)
            For index As Integer = 0 To argumentsLength - 1 Step 1
                arguments(index) = DeriveArgument(boundArguments(index))
            Next

            Return arguments.ToImmutableAndFree()
        End Function

        Private Shared Function DeriveArgument(argument As BoundExpression) As Semantics.IArgument
            Select Case argument.Kind
                Case BoundKind.ByRefArgumentWithCopyBack
                    Return DirectCast(argument, BoundByRefArgumentWithCopyBack)
                Case Else
                    Return New Argument(argument)
            End Select
        End Function

        Private Class Argument
            Implements Semantics.IArgument

            Private ReadOnly _Value As Semantics.IExpression

            Public Sub New(value As Semantics.IExpression)
                Me._Value = value
            End Sub

            Public ReadOnly Property ArgumentClass As Semantics.ArgumentKind Implements Semantics.IArgument.ArgumentClass
                Get
                    ' Apparently the VB bound trees don't encode named arguments, which seems unnecesarily lossy.
                    Return Semantics.ArgumentKind.Positional
                End Get
            End Property

            Public ReadOnly Property Mode As Semantics.ArgumentMode Implements Semantics.IArgument.Mode
                Get
                    Return Semantics.ArgumentMode.In
                End Get
            End Property

            Public ReadOnly Property Value As Semantics.IExpression Implements Semantics.IArgument.Value
                Get
                    Return Me._Value
                End Get
            End Property

            Public ReadOnly Property InConversion As Semantics.IExpression Implements Semantics.IArgument.InConversion
                Get
                    Return Nothing
                End Get
            End Property

            Public ReadOnly Property OutConversion As Semantics.IExpression Implements Semantics.IArgument.OutConversion
                Get
                    Return Nothing
                End Get
            End Property
        End Class
    End Class

    Partial Class BoundByRefArgumentWithCopyBack
        Implements Semantics.IArgument

        Private ReadOnly Property IArgumentClass As Semantics.ArgumentKind Implements Semantics.IArgument.ArgumentClass
            Get
                ' Do the VB bound trees encode named arguments?
                Return Semantics.ArgumentKind.Positional
            End Get
        End Property

        Private ReadOnly Property IMode As Semantics.ArgumentMode Implements Semantics.IArgument.Mode
            Get
                If Me.InPlaceholder IsNot Nothing AndAlso Me.InPlaceholder.IsOut Then
                    Return Semantics.ArgumentMode.Out
                End If

                Return Semantics.ArgumentMode.Reference
            End Get
        End Property

        Private ReadOnly Property IValue As Semantics.IExpression Implements Semantics.IArgument.Value
            Get
                Return Me.OriginalArgument
            End Get
        End Property

        Private ReadOnly Property IInConversion As Semantics.IExpression Implements Semantics.IArgument.InConversion
            Get
                Return Me.InConversion
            End Get
        End Property

        Private ReadOnly Property IOutConversion As Semantics.IExpression Implements Semantics.IArgument.OutConversion
            Get
                Return Me.OutConversion
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.None
        End Function
    End Class

    Partial Class BoundOmittedArgument
        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.Omitted
        End Function
    End Class

    Partial Class BoundParenthesized
        Implements Semantics.IParenthesized

        Private ReadOnly Property IOperand As Semantics.IExpression Implements Semantics.IParenthesized.Operand
            Get
                Return Me.Expression
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.Parenthesized
        End Function
    End Class

    Partial Class BoundArrayAccess
        Implements Semantics.IArrayElementReference

        Private ReadOnly Property IArrayReference As Semantics.IExpression Implements Semantics.IArrayElementReference.ArrayReference
            Get
                Return Me.Expression
            End Get
        End Property

        Private ReadOnly Property IIndices As ImmutableArray(Of Semantics.IExpression) Implements Semantics.IArrayElementReference.Indices
            Get
                Return Me.Indices.As(Of Semantics.IExpression)()
            End Get
        End Property

        Private ReadOnly Property IReferenceClass As Semantics.ReferenceKind Implements Semantics.IReference.ReferenceClass
            Get
                Return Semantics.ReferenceKind.ArrayElement
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.ArrayElementReference
        End Function
    End Class

    Partial Class BoundUnaryOperator
        Implements Semantics.IUnary

        Private ReadOnly Property IOperator As IMethodSymbol Implements Semantics.IOperator.Operator
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements Semantics.IOperator.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property IOperand As Semantics.IExpression Implements Semantics.IUnary.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IOperation As Semantics.UnaryOperatorCode Implements Semantics.IUnary.Operation
            Get
                Return DeriveUnaryOperatorCode(Me.OperatorKind, Me.Operand)
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.UnaryOperator
        End Function
    End Class

    Partial Class BoundUserDefinedUnaryOperator
        Implements Semantics.IUnary

        Private ReadOnly Property IOperator As IMethodSymbol Implements Semantics.IOperator.Operator
            Get
                Return Me.Call.Method
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements Semantics.IOperator.UsesOperatorMethod
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IOperand As Semantics.IExpression Implements Semantics.IUnary.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IOperation As Semantics.UnaryOperatorCode Implements Semantics.IUnary.Operation
            Get
                Select Case OperatorKind And UnaryOperatorKind.OpMask
                    Case UnaryOperatorKind.Plus
                        Return Semantics.UnaryOperatorCode.OperatorPlus
                    Case UnaryOperatorKind.Minus
                        Return Semantics.UnaryOperatorCode.OperatorMinus
                    Case UnaryOperatorKind.Not
                        Return Semantics.UnaryOperatorCode.OperatorBitwiseNegation
                End Select

                Return Semantics.UnaryOperatorCode.None
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.UnaryOperator
        End Function
    End Class

    Partial Class BoundBinaryOperator
        Implements Semantics.IBinary
        Implements Semantics.IRelational

        Private ReadOnly Property ILeft As Semantics.IExpression Implements Semantics.IBinary.Left, Semantics.IRelational.Left
            Get
                Return Me.Left
            End Get
        End Property

        Private ReadOnly Property IOperation As Semantics.BinaryOperatorCode Implements Semantics.IBinary.Operation
            Get
                Return DeriveBinaryOperatorCode(Me.OperatorKind, Me.Left)
            End Get
        End Property

        Private ReadOnly Property IRight As Semantics.IExpression Implements Semantics.IBinary.Right, Semantics.IRelational.Right
            Get
                Return Me.Right
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements Semantics.IOperator.Operator
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements Semantics.IOperator.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property RelationalCode As Semantics.RelationalOperatorCode Implements Semantics.IRelational.RelationalCode
            Get
                Return DeriveRelationalOperatorCode(Me.OperatorKind, Me.Left)
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.BinaryOperator
        End Function

    End Class

    Partial Class BoundUserDefinedBinaryOperator
        Implements Semantics.IBinary
        Implements Semantics.IRelational

        Private ReadOnly Property ILeft As Semantics.IExpression Implements Semantics.IBinary.Left, Semantics.IRelational.Left
            Get
                Return Me.Left
            End Get
        End Property

        Private ReadOnly Property IOperation As Semantics.BinaryOperatorCode Implements Semantics.IBinary.Operation
            Get
                Select Case OperatorKind And BinaryOperatorKind.OpMask
                    Case BinaryOperatorKind.Add
                        Return Semantics.BinaryOperatorCode.OperatorAdd
                    Case BinaryOperatorKind.Subtract
                        Return Semantics.BinaryOperatorCode.OperatorSubtract
                    Case BinaryOperatorKind.Multiply
                        Return Semantics.BinaryOperatorCode.OperatorMultiply
                    Case BinaryOperatorKind.Divide
                        Return Semantics.BinaryOperatorCode.OperatorDivide
                    Case BinaryOperatorKind.Modulo
                        Return Semantics.BinaryOperatorCode.OperatorRemainder
                    Case BinaryOperatorKind.And
                        Return Semantics.BinaryOperatorCode.OperatorAnd
                    Case BinaryOperatorKind.Or
                        Return Semantics.BinaryOperatorCode.OperatorOr
                    Case BinaryOperatorKind.Xor
                        Return Semantics.BinaryOperatorCode.OperatorXor
                    Case BinaryOperatorKind.AndAlso
                        Return Semantics.BinaryOperatorCode.OperatorConditionalAnd
                    Case BinaryOperatorKind.OrElse
                        Return Semantics.BinaryOperatorCode.OperatorConditionalOr
                    Case BinaryOperatorKind.LeftShift
                        Return Semantics.BinaryOperatorCode.OperatorLeftShift
                    Case BinaryOperatorKind.RightShift
                        Return Semantics.BinaryOperatorCode.OperatorRightShift
                End Select

                Return Semantics.BinaryOperatorCode.None
            End Get
        End Property

        Private ReadOnly Property IRelationalCode As Semantics.RelationalOperatorCode Implements Semantics.IRelational.RelationalCode
            Get
                Select Case OperatorKind And BinaryOperatorKind.OpMask
                    Case BinaryOperatorKind.LessThan
                        Return Semantics.RelationalOperatorCode.OperatorLess
                    Case BinaryOperatorKind.LessThanOrEqual
                        Return Semantics.RelationalOperatorCode.OperatorLessEqual
                    Case BinaryOperatorKind.Equals
                        Return Semantics.RelationalOperatorCode.OperatorEqual
                    Case BinaryOperatorKind.NotEquals
                        Return Semantics.RelationalOperatorCode.OperatorNotEqual
                    Case BinaryOperatorKind.GreaterThanOrEqual
                        Return Semantics.RelationalOperatorCode.OperatorGreaterEqual
                    Case BinaryOperatorKind.GreaterThan
                        Return Semantics.RelationalOperatorCode.OperatorGreater
                End Select

                Return Semantics.RelationalOperatorCode.None
            End Get
        End Property

        Private ReadOnly Property IRight As Semantics.IExpression Implements Semantics.IBinary.Right, Semantics.IRelational.Right
            Get
                Return Me.Right
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements Semantics.IOperator.Operator
            Get
                Return Me.Call.Method
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements Semantics.IOperator.UsesOperatorMethod
            Get
                Return True
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Select Case Me.OperatorKind And BinaryOperatorKind.OpMask
                Case BinaryOperatorKind.Add, BinaryOperatorKind.Concatenate, BinaryOperatorKind.Subtract, BinaryOperatorKind.Multiply, BinaryOperatorKind.Divide,
                    BinaryOperatorKind.IntegerDivide, BinaryOperatorKind.Modulo, BinaryOperatorKind.Power, BinaryOperatorKind.LeftShift, BinaryOperatorKind.RightShift,
                    BinaryOperatorKind.And, BinaryOperatorKind.Or, BinaryOperatorKind.Xor, BinaryOperatorKind.AndAlso, BinaryOperatorKind.OrElse

                    Return Semantics.OperationKind.BinaryOperator

                Case BinaryOperatorKind.LessThan, BinaryOperatorKind.LessThanOrEqual, BinaryOperatorKind.Equals, BinaryOperatorKind.NotEquals,
                    BinaryOperatorKind.Is, BinaryOperatorKind.IsNot, BinaryOperatorKind.Like, BinaryOperatorKind.GreaterThanOrEqual, BinaryOperatorKind.GreaterThan

                    Return Semantics.OperationKind.RelationalOperator
            End Select

            Return Semantics.OperationKind.None
        End Function
    End Class

    Partial Class BoundBinaryConditionalExpression
        Implements Semantics.INullCoalescing

        Private ReadOnly Property IPrimary As Semantics.IExpression Implements Semantics.INullCoalescing.Primary
            Get
                Return Me.TestExpression
            End Get
        End Property

        Private ReadOnly Property ISecondary As Semantics.IExpression Implements Semantics.INullCoalescing.Secondary
            Get
                Return Me.ElseExpression
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.NullCoalescing
        End Function
    End Class

    Partial Class BoundUserDefinedShortCircuitingOperator
        Implements Semantics.IBinary

        Private ReadOnly Property ILeft As Semantics.IExpression Implements Semantics.IBinary.Left
            Get
                Return Me.LeftOperand
            End Get
        End Property

        Private ReadOnly Property IOperation As Semantics.BinaryOperatorCode Implements Semantics.IBinary.Operation
            Get
                Return If((Me.BitwiseOperator.OperatorKind And BinaryOperatorKind.And) <> 0, Semantics.BinaryOperatorCode.OperatorConditionalAnd, Semantics.BinaryOperatorCode.OperatorConditionalOr)
            End Get
        End Property

        Private ReadOnly Property IRight As Semantics.IExpression Implements Semantics.IBinary.Right
            Get
                Return Me.BitwiseOperator.Right
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements Semantics.IOperator.Operator
            Get
                Return Me.BitwiseOperator.Call.Method
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements Semantics.IOperator.UsesOperatorMethod
            Get
                Return True
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.BinaryOperator
        End Function
    End Class

    Partial Class BoundBadExpression
        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.None
        End Function
    End Class

    Partial Class BoundTryCast
        Implements Semantics.IConversion

        Private ReadOnly Property IConversion As Semantics.ConversionKind Implements Semantics.IConversion.Conversion
            Get
                Return Semantics.ConversionKind.AsCast
            End Get
        End Property

        Private ReadOnly Property IIsExplicit As Boolean Implements Semantics.IConversion.IsExplicit
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IOperand As Semantics.IExpression Implements Semantics.IConversion.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements Semantics.IOperator.Operator
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements Semantics.IOperator.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.Conversion
        End Function
    End Class

    Partial Class BoundDirectCast
        Implements Semantics.IConversion

        Private ReadOnly Property IConversion As Semantics.ConversionKind Implements Semantics.IConversion.Conversion
            Get
                Return Semantics.ConversionKind.Cast
            End Get
        End Property

        Private ReadOnly Property IIsExplicit As Boolean Implements Semantics.IConversion.IsExplicit
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IOperand As Semantics.IExpression Implements Semantics.IConversion.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements Semantics.IOperator.Operator
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements Semantics.IOperator.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.Conversion
        End Function
    End Class

    Partial Class BoundConversion
        Implements Semantics.IConversion

        Private ReadOnly Property IConversion As Semantics.ConversionKind Implements Semantics.IConversion.Conversion
            Get
                Return Semantics.ConversionKind.Basic
            End Get
        End Property

        Private ReadOnly Property IIsExplicit As Boolean Implements Semantics.IConversion.IsExplicit
            Get
                Return Me.ExplicitCastInCode
            End Get
        End Property

        Private ReadOnly Property IOperand As Semantics.IExpression Implements Semantics.IConversion.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements Semantics.IOperator.Operator
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements Semantics.IOperator.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.Conversion
        End Function
    End Class

    Partial Class BoundUserDefinedConversion
        Implements Semantics.IConversion

        Private ReadOnly Property IConversion As Semantics.ConversionKind Implements Semantics.IConversion.Conversion
            Get
                Return Semantics.ConversionKind.Operator
            End Get
        End Property

        Private ReadOnly Property IIsExplicit As Boolean Implements Semantics.IConversion.IsExplicit
            Get
                Return Not Me.WasCompilerGenerated
            End Get
        End Property

        Private ReadOnly Property IOperand As Semantics.IExpression Implements Semantics.IConversion.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements Semantics.IOperator.Operator
            Get
                Return Me.Call.Method
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements Semantics.IOperator.UsesOperatorMethod
            Get
                Return True
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.Conversion
        End Function
    End Class

    Partial Class BoundTernaryConditionalExpression
        Implements Semantics.IConditionalChoice

        Private ReadOnly Property ICondition As Semantics.IExpression Implements Semantics.IConditionalChoice.Condition
            Get
                Return Me.Condition
            End Get
        End Property

        Private ReadOnly Property IIfFalse As Semantics.IExpression Implements Semantics.IConditionalChoice.IfFalse
            Get
                Return Me.WhenFalse
            End Get
        End Property

        Private ReadOnly Property IIfTrue As Semantics.IExpression Implements Semantics.IConditionalChoice.IfTrue
            Get
                Return Me.WhenTrue
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.ConditionalChoice
        End Function
    End Class

    Partial Class BoundTypeOf
        Implements Semantics.IIs

        Private ReadOnly Property IIsType As ITypeSymbol Implements Semantics.IIs.IsType
            Get
                Return Me.TargetType
            End Get
        End Property

        Private ReadOnly Property IOperand As Semantics.IExpression Implements Semantics.IIs.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.Is
        End Function
    End Class

    Partial Class BoundObjectCreationExpression
        Implements Semantics.IObjectCreation

        Private Function IArgumentMatchingParameter(parameter As IParameterSymbol) As Semantics.IArgument Implements Semantics.IObjectCreation.ArgumentMatchingParameter
            Return BoundCall.ArgumentMatchingParameter(Me.Arguments, parameter)
        End Function

        Private ReadOnly Property IConstructor As IMethodSymbol Implements Semantics.IObjectCreation.Constructor
            Get
                Return Me.ConstructorOpt
            End Get
        End Property

        Private ReadOnly Property IConstructorArguments As ImmutableArray(Of Semantics.IArgument) Implements Semantics.IObjectCreation.ConstructorArguments
            Get
                Return BoundCall.DeriveArguments(Me.Arguments)
            End Get
        End Property

        Private ReadOnly Property IMemberInitializers As ImmutableArray(Of Semantics.IMemberInitializer) Implements Semantics.IObjectCreation.MemberInitializers
            Get
                Dim initializer As BoundObjectInitializerExpressionBase = Me.InitializerOpt
                If initializer IsNot Nothing Then
                    ' ZZZ What's the representation in bound trees?
                End If

                Return ImmutableArray.Create(Of Semantics.IMemberInitializer)()
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.ObjectCreation
        End Function
    End Class

    Partial Class BoundNewT
        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.TypeParameterObjectCreation
        End Function
    End Class

    Partial Class BoundArrayCreation
        Implements Semantics.IArrayCreation

        Private ReadOnly Property IDimensionSizes As ImmutableArray(Of Semantics.IExpression) Implements Semantics.IArrayCreation.DimensionSizes
            Get
                Return Me.Bounds.As(Of Semantics.IExpression)()
            End Get
        End Property

        Private ReadOnly Property IElementType As ITypeSymbol Implements Semantics.IArrayCreation.ElementType
            Get
                Dim arrayType As IArrayTypeSymbol = TryCast(Me.Type, IArrayTypeSymbol)
                If arrayType IsNot Nothing Then
                    Return arrayType.ElementType
                End If

                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IElementValues As Semantics.IArrayInitializer Implements Semantics.IArrayCreation.ElementValues
            Get
                Dim initializer As BoundArrayInitialization = Me.InitializerOpt
                If initializer IsNot Nothing Then
                    Return MakeInitializer(initializer)
                End If

                Return Nothing
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.ArrayCreation
        End Function

        Private Function MakeInitializer(initializer As BoundArrayInitialization) As Semantics.IArrayInitializer
            Dim dimension As ArrayBuilder(Of Semantics.IArrayInitializer) = ArrayBuilder(Of Semantics.IArrayInitializer).GetInstance(initializer.Initializers.Length)

            For index As Integer = 0 To initializer.Initializers.Length - 1
                Dim elementInitializer As BoundExpression = initializer.Initializers(index)
                Dim elementArray As BoundArrayInitialization = TryCast(elementInitializer, BoundArrayInitialization)
                dimension(index) = If(elementArray IsNot Nothing, MakeInitializer(elementArray), New ElementInitializer(elementInitializer))
            Next

            Return New DimensionInitializer(dimension.ToImmutableAndFree())
        End Function

        Private Class ElementInitializer
            Implements Semantics.IExpressionArrayInitializer

            ReadOnly _element As BoundExpression

            Public Sub New(element As BoundExpression)
                Me._element = element
            End Sub

            ReadOnly Property ElementValue As Semantics.IExpression Implements Semantics.IExpressionArrayInitializer.ElementValue
                Get
                    Return Me._element
                End Get
            End Property

            ReadOnly Property ArrayClass As Semantics.ArrayInitializerKind Implements Semantics.IExpressionArrayInitializer.ArrayClass
                Get
                    Return Semantics.ArrayInitializerKind.Expression
                End Get
            End Property
        End Class

        Private Class DimensionInitializer
            Implements Semantics.IDimensionArrayInitializer

            ReadOnly _dimension As ImmutableArray(Of Semantics.IArrayInitializer)

            Public Sub New(dimension As ImmutableArray(Of Semantics.IArrayInitializer))
                Me._dimension = dimension
            End Sub

            ReadOnly Property ElementValues As ImmutableArray(Of Semantics.IArrayInitializer) Implements Semantics.IDimensionArrayInitializer.ElementValues
                Get
                    Return Me._dimension
                End Get
            End Property

            ReadOnly Property ArrayClass As Semantics.ArrayInitializerKind Implements Semantics.IDimensionArrayInitializer.ArrayClass
                Get
                    Return Semantics.ArrayInitializerKind.Dimension
                End Get
            End Property
        End Class
    End Class

    Partial Class BoundPropertyAccess
        Implements Semantics.IPropertyReference

        Private ReadOnly Property IInstance As Semantics.IExpression Implements Semantics.IMemberReference.Instance
            Get
                Return Me.ReceiverOpt
            End Get
        End Property

        Private ReadOnly Property IProperty As IPropertySymbol Implements Semantics.IPropertyReference.Property
            Get
                Return Me.PropertySymbol
            End Get
        End Property

        Private ReadOnly Property IReferenceClass As Semantics.ReferenceKind Implements Semantics.IReference.ReferenceClass
            Get
                Return If(Me.ReceiverOpt IsNot Nothing, Semantics.ReferenceKind.InstanceProperty, Semantics.ReferenceKind.StaticProperty)
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.PropertyReference
        End Function
    End Class

    Partial Class BoundFieldAccess
        Implements Semantics.IFieldReference

        Private ReadOnly Property IField As IFieldSymbol Implements Semantics.IFieldReference.Field
            Get
                Return Me.FieldSymbol
            End Get
        End Property

        Private ReadOnly Property IInstance As Semantics.IExpression Implements Semantics.IMemberReference.Instance
            Get
                Return Me.ReceiverOpt
            End Get
        End Property

        Private ReadOnly Property IReferenceClass As Semantics.ReferenceKind Implements Semantics.IReference.ReferenceClass
            Get
                Return If(Me.ReceiverOpt IsNot Nothing, Semantics.ReferenceKind.InstanceField, Semantics.ReferenceKind.StaticField)
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.FieldReference
        End Function
    End Class

    Partial Class BoundParameter
        Implements Semantics.IParameterReference

        Private ReadOnly Property IParameter As IParameterSymbol Implements Semantics.IParameterReference.Parameter
            Get
                Return Me.ParameterSymbol
            End Get
        End Property

        Private ReadOnly Property IReferenceClass As Semantics.ReferenceKind Implements Semantics.IReference.ReferenceClass
            Get
                Return Semantics.ReferenceKind.Parameter
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.ParameterReference
        End Function
    End Class

    Partial Class BoundLocal
        Implements Semantics.ILocalReference

        Private ReadOnly Property ILocal As ILocalSymbol Implements Semantics.ILocalReference.Local
            Get
                Return Me.LocalSymbol
            End Get
        End Property

        Private ReadOnly Property IReferenceClass As Semantics.ReferenceKind Implements Semantics.IReference.ReferenceClass
            Get
                Return Semantics.ReferenceKind.Local
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.LocalReference
        End Function
    End Class

    Partial Class BoundLateMemberAccess
        Implements Semantics.ILateBoundMemberReference

        Private ReadOnly Property IInstance As Semantics.IExpression Implements Semantics.ILateBoundMemberReference.Instance
            Get
                Return Me.ReceiverOpt
            End Get
        End Property

        Private ReadOnly Property IMemberName As String Implements Semantics.ILateBoundMemberReference.MemberName
            Get
                Return Me.NameOpt
            End Get
        End Property

        Private ReadOnly Property IReferenceClass As Semantics.ReferenceKind Implements Semantics.IReference.ReferenceClass
            Get
                Return Semantics.ReferenceKind.LateBoundMember
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As Semantics.OperationKind
            Return Semantics.OperationKind.LateBoundMemberReference
        End Function
    End Class

    Module Expression
        Friend Function DeriveUnaryOperatorCode(operatorKind As UnaryOperatorKind, operand As BoundExpression) As Semantics.UnaryOperatorCode
            Select Case operand.Type.SpecialType
                Case SpecialType.System_Byte, SpecialType.System_Int16, SpecialType.System_Int32, SpecialType.System_Int64, SpecialType.System_SByte, SpecialType.System_UInt16, SpecialType.System_UInt32, SpecialType.System_UInt64
                    Select Case operatorKind And UnaryOperatorKind.OpMask
                        Case UnaryOperatorKind.Plus
                            Return Semantics.UnaryOperatorCode.IntegerPlus
                        Case UnaryOperatorKind.Minus
                            Return Semantics.UnaryOperatorCode.IntegerMinus
                        Case UnaryOperatorKind.Not
                            Return Semantics.UnaryOperatorCode.IntegerBitwiseNegation
                    End Select
                Case SpecialType.System_Single, SpecialType.System_Double
                    Select Case operatorKind And UnaryOperatorKind.OpMask
                        Case UnaryOperatorKind.Plus
                            Return Semantics.UnaryOperatorCode.FloatingPlus
                        Case UnaryOperatorKind.Minus
                            Return Semantics.UnaryOperatorCode.FloatingMinus
                    End Select
                Case SpecialType.System_Decimal
                    Select Case operatorKind And UnaryOperatorKind.OpMask
                        Case UnaryOperatorKind.Plus
                            Return Semantics.UnaryOperatorCode.DecimalPlus
                        Case UnaryOperatorKind.Minus
                            Return Semantics.UnaryOperatorCode.DecimalMinus
                    End Select
                Case SpecialType.System_Boolean
                    Select Case operatorKind And UnaryOperatorKind.OpMask
                        Case UnaryOperatorKind.Not
                            Return Semantics.UnaryOperatorCode.BooleanBitwiseNegation
                    End Select
                Case SpecialType.System_Object
                    Select Case operatorKind And UnaryOperatorKind.OpMask
                        Case UnaryOperatorKind.Plus
                            Return Semantics.UnaryOperatorCode.ObjectPlus
                        Case UnaryOperatorKind.Minus
                            Return Semantics.UnaryOperatorCode.ObjectMinus
                        Case UnaryOperatorKind.Not
                            Return Semantics.UnaryOperatorCode.ObjectNot
                    End Select
            End Select

            Return Semantics.UnaryOperatorCode.None
        End Function

        Friend Function DeriveBinaryOperatorCode(operatorKind As BinaryOperatorKind, left As BoundExpression) As Semantics.BinaryOperatorCode
            Select Case left.Type.SpecialType
                Case SpecialType.System_Byte, SpecialType.System_UInt16, SpecialType.System_UInt32, SpecialType.System_UInt64
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Add
                            Return Semantics.BinaryOperatorCode.IntegerAdd
                        Case BinaryOperatorKind.Subtract
                            Return Semantics.BinaryOperatorCode.IntegerSubtract
                        Case BinaryOperatorKind.Multiply
                            Return Semantics.BinaryOperatorCode.IntegerMultiply
                        Case BinaryOperatorKind.IntegerDivide
                            Return Semantics.BinaryOperatorCode.IntegerDivide
                        Case BinaryOperatorKind.Modulo
                            Return Semantics.BinaryOperatorCode.IntegerRemainder
                        Case BinaryOperatorKind.And
                            Return Semantics.BinaryOperatorCode.IntegerAnd
                        Case BinaryOperatorKind.Or
                            Return Semantics.BinaryOperatorCode.IntegerOr
                        Case BinaryOperatorKind.Xor
                            Return Semantics.BinaryOperatorCode.IntegerXor
                        Case BinaryOperatorKind.LeftShift
                            Return Semantics.BinaryOperatorCode.IntegerLeftShift
                        Case BinaryOperatorKind.RightShift
                            Return Semantics.BinaryOperatorCode.IntegerRightShift
                    End Select
                Case SpecialType.System_SByte, SpecialType.System_Int16, SpecialType.System_Int32, SpecialType.System_Int64
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Add
                            Return Semantics.BinaryOperatorCode.UnsignedAdd
                        Case BinaryOperatorKind.Subtract
                            Return Semantics.BinaryOperatorCode.UnsignedSubtract
                        Case BinaryOperatorKind.Multiply
                            Return Semantics.BinaryOperatorCode.UnsignedMultiply
                        Case BinaryOperatorKind.IntegerDivide
                            Return Semantics.BinaryOperatorCode.UnsignedDivide
                        Case BinaryOperatorKind.Modulo
                            Return Semantics.BinaryOperatorCode.UnsignedRemainder
                        Case BinaryOperatorKind.And
                            Return Semantics.BinaryOperatorCode.UnsignedAnd
                        Case BinaryOperatorKind.Or
                            Return Semantics.BinaryOperatorCode.UnsignedOr
                        Case BinaryOperatorKind.Xor
                            Return Semantics.BinaryOperatorCode.UnsignedXor
                        Case BinaryOperatorKind.LeftShift
                            Return Semantics.BinaryOperatorCode.UnsignedLeftShift
                        Case BinaryOperatorKind.RightShift
                            Return Semantics.BinaryOperatorCode.UnsignedRightShift
                    End Select
                Case SpecialType.System_Single, SpecialType.System_Double
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Add
                            Return Semantics.BinaryOperatorCode.FloatingAdd
                        Case BinaryOperatorKind.Subtract
                            Return Semantics.BinaryOperatorCode.FloatingSubtract
                        Case BinaryOperatorKind.Multiply
                            Return Semantics.BinaryOperatorCode.FloatingMultiply
                        Case BinaryOperatorKind.Divide
                            Return Semantics.BinaryOperatorCode.FloatingDivide
                        Case BinaryOperatorKind.Modulo
                            Return Semantics.BinaryOperatorCode.FloatingRemainder
                        Case BinaryOperatorKind.Power
                            Return Semantics.BinaryOperatorCode.FloatingPower
                    End Select
                Case SpecialType.System_Decimal
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Add
                            Return Semantics.BinaryOperatorCode.DecimalAdd
                        Case BinaryOperatorKind.Subtract
                            Return Semantics.BinaryOperatorCode.DecimalSubtract
                        Case BinaryOperatorKind.Multiply
                            Return Semantics.BinaryOperatorCode.DecimalMultiply
                        Case BinaryOperatorKind.Divide
                            Return Semantics.BinaryOperatorCode.DecimalDivide
                    End Select
                Case SpecialType.System_Boolean
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.And
                            Return Semantics.BinaryOperatorCode.BooleanAnd
                        Case BinaryOperatorKind.Or
                            Return Semantics.BinaryOperatorCode.BooleanOr
                        Case BinaryOperatorKind.Xor
                            Return Semantics.BinaryOperatorCode.BooleanXor
                        Case BinaryOperatorKind.AndAlso
                            Return Semantics.BinaryOperatorCode.BooleanConditionalAnd
                        Case BinaryOperatorKind.OrElse
                            Return Semantics.BinaryOperatorCode.BooleanConditionalOr
                    End Select
                Case SpecialType.System_String
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Concatenate
                            Return Semantics.BinaryOperatorCode.StringConcatenation
                    End Select
                Case SpecialType.System_Object
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Add
                            Return Semantics.BinaryOperatorCode.ObjectAdd
                        Case BinaryOperatorKind.Subtract
                            Return Semantics.BinaryOperatorCode.ObjectSubtract
                        Case BinaryOperatorKind.Multiply
                            Return Semantics.BinaryOperatorCode.ObjectMultiply
                        Case BinaryOperatorKind.Power
                            Return Semantics.BinaryOperatorCode.ObjectPower
                        Case BinaryOperatorKind.IntegerDivide
                            Return Semantics.BinaryOperatorCode.ObjectIntegerDivide
                        Case BinaryOperatorKind.Divide
                            Return Semantics.BinaryOperatorCode.ObjectDivide
                        Case BinaryOperatorKind.Modulo
                            Return Semantics.BinaryOperatorCode.ObjectRemainder
                        Case BinaryOperatorKind.Concatenate
                            Return Semantics.BinaryOperatorCode.ObjectConcatenation
                        Case BinaryOperatorKind.And
                            Return Semantics.BinaryOperatorCode.ObjectAnd
                        Case BinaryOperatorKind.Or
                            Return Semantics.BinaryOperatorCode.ObjectOr
                        Case BinaryOperatorKind.Xor
                            Return Semantics.BinaryOperatorCode.ObjectXor
                        Case BinaryOperatorKind.AndAlso
                            Return Semantics.BinaryOperatorCode.ObjectConditionalAnd
                        Case BinaryOperatorKind.OrElse
                            Return Semantics.BinaryOperatorCode.ObjectConditionalOr
                        Case BinaryOperatorKind.LeftShift
                            Return Semantics.BinaryOperatorCode.ObjectLeftShift
                        Case BinaryOperatorKind.RightShift
                            Return Semantics.BinaryOperatorCode.ObjectRightShift
                    End Select
            End Select

            If left.Type.TypeKind = TypeKind.Enum Then
                Select Case operatorKind And BinaryOperatorKind.OpMask
                    Case BinaryOperatorKind.Add
                        Return Semantics.BinaryOperatorCode.EnumAdd
                    Case BinaryOperatorKind.Subtract
                        Return Semantics.BinaryOperatorCode.EnumSubtract
                    Case BinaryOperatorKind.And
                        Return Semantics.BinaryOperatorCode.EnumAnd
                    Case BinaryOperatorKind.Or
                        Return Semantics.BinaryOperatorCode.EnumOr
                    Case BinaryOperatorKind.Xor
                        Return Semantics.BinaryOperatorCode.EnumXor
                End Select
            End If

            Return Semantics.BinaryOperatorCode.None
        End Function

        Friend Function DeriveRelationalOperatorCode(operatorKind As BinaryOperatorKind, left As BoundExpression) As Semantics.RelationalOperatorCode
            Select Case left.Type.SpecialType
                Case SpecialType.System_Byte, SpecialType.System_UInt16, SpecialType.System_UInt32, SpecialType.System_UInt64, SpecialType.System_Char
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.LessThan
                            Return Semantics.RelationalOperatorCode.UnsignedLess
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return Semantics.RelationalOperatorCode.UnsignedLessEqual
                        Case BinaryOperatorKind.Equals
                            Return Semantics.RelationalOperatorCode.IntegerEqual
                        Case BinaryOperatorKind.NotEquals
                            Return Semantics.RelationalOperatorCode.IntegerNotEqual
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return Semantics.RelationalOperatorCode.UnsignedGreaterEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return Semantics.RelationalOperatorCode.UnsignedGreater
                    End Select
                Case SpecialType.System_SByte, SpecialType.System_Int16, SpecialType.System_Int32, SpecialType.System_Int64
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.LessThan
                            Return Semantics.RelationalOperatorCode.IntegerLess
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return Semantics.RelationalOperatorCode.IntegerLessEqual
                        Case BinaryOperatorKind.Equals
                            Return Semantics.RelationalOperatorCode.IntegerEqual
                        Case BinaryOperatorKind.NotEquals
                            Return Semantics.RelationalOperatorCode.IntegerNotEqual
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return Semantics.RelationalOperatorCode.IntegerGreaterEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return Semantics.RelationalOperatorCode.IntegerGreater
                    End Select
                Case SpecialType.System_Single, SpecialType.System_Double
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.LessThan
                            Return Semantics.RelationalOperatorCode.FloatingLess
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return Semantics.RelationalOperatorCode.FloatingLessEqual
                        Case BinaryOperatorKind.Equals
                            Return Semantics.RelationalOperatorCode.FloatingEqual
                        Case BinaryOperatorKind.NotEquals
                            Return Semantics.RelationalOperatorCode.FloatingNotEqual
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return Semantics.RelationalOperatorCode.FloatingGreaterEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return Semantics.RelationalOperatorCode.FloatingGreater
                    End Select
                Case SpecialType.System_Decimal
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.LessThan
                            Return Semantics.RelationalOperatorCode.DecimalLess
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return Semantics.RelationalOperatorCode.DecimalLessEqual
                        Case BinaryOperatorKind.Equals
                            Return Semantics.RelationalOperatorCode.DecimalEqual
                        Case BinaryOperatorKind.NotEquals
                            Return Semantics.RelationalOperatorCode.DecimalNotEqual
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return Semantics.RelationalOperatorCode.DecimalGreaterEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return Semantics.RelationalOperatorCode.DecimalGreater
                    End Select
                Case SpecialType.System_Boolean
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Equals
                            Return Semantics.RelationalOperatorCode.BooleanEqual
                        Case BinaryOperatorKind.NotEquals
                            Return Semantics.RelationalOperatorCode.BooleanNotEqual
                    End Select
                Case SpecialType.System_String
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Equals
                            Return Semantics.RelationalOperatorCode.StringEqual
                        Case BinaryOperatorKind.NotEquals
                            Return Semantics.RelationalOperatorCode.StringNotEqual
                        Case BinaryOperatorKind.Like
                            Return Semantics.RelationalOperatorCode.StringLike
                    End Select
                Case SpecialType.System_Object
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.LessThan
                            Return Semantics.RelationalOperatorCode.ObjectLess
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return Semantics.RelationalOperatorCode.ObjectLessEqual
                        Case BinaryOperatorKind.Equals
                            Return Semantics.RelationalOperatorCode.ObjectVBEqual
                        Case BinaryOperatorKind.Is
                            Return Semantics.RelationalOperatorCode.ObjectEqual
                        Case BinaryOperatorKind.IsNot
                            Return Semantics.RelationalOperatorCode.ObjectNotEqual
                        Case BinaryOperatorKind.NotEquals
                            Return Semantics.RelationalOperatorCode.ObjectVBNotEqual
                        Case BinaryOperatorKind.Like
                            Return Semantics.RelationalOperatorCode.ObjectLike
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return Semantics.RelationalOperatorCode.ObjectGreaterEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return Semantics.RelationalOperatorCode.ObjectGreater
                    End Select
            End Select

            If left.Type.TypeKind = TypeKind.Enum Then
                Select Case operatorKind And BinaryOperatorKind.OpMask
                    Case BinaryOperatorKind.LessThan
                        Return Semantics.RelationalOperatorCode.EnumLess
                    Case BinaryOperatorKind.LessThanOrEqual
                        Return Semantics.RelationalOperatorCode.EnumLessEqual
                    Case BinaryOperatorKind.Equals
                        Return Semantics.RelationalOperatorCode.EnumEqual
                    Case BinaryOperatorKind.NotEquals
                        Return Semantics.RelationalOperatorCode.EnumNotEqual
                    Case BinaryOperatorKind.GreaterThanOrEqual
                        Return Semantics.RelationalOperatorCode.EnumGreaterEqual
                    Case BinaryOperatorKind.GreaterThan
                        Return Semantics.RelationalOperatorCode.EnumGreater
                End Select
            End If

            Return Semantics.RelationalOperatorCode.None
        End Function
    End Module
End Namespace
