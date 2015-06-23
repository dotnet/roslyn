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

        'Protected MustOverride Function ExpressionKind() As Unified.ExpressionKind

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

        Private ReadOnly Property IOperation As BinaryOperatorCode Implements ICompoundAssignment.Operation
            Get
                If ExpressionKind() = OperationKind.CompoundAssignment Then
                    Dim rightBinary As BoundBinaryOperator = TryCast(Me.Right, BoundBinaryOperator)
                    If rightBinary IsNot Nothing Then
                        Return Expression.DeriveBinaryOperatorCode(rightBinary.OperatorKind, Me.Left)
                    End If

                    Dim rightOperatorBinary As BoundUserDefinedBinaryOperator = TryCast(Me.Right, BoundUserDefinedBinaryOperator)
                    If rightOperatorBinary IsNot Nothing Then
                        Return Expression.DeriveBinaryOperatorCode(rightOperatorBinary.OperatorKind, Me.Left)
                    End If
                End If

                Return BinaryOperatorCode.None
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements IOperator.Operator
            Get
                If Me.IUsesOperatorMethod Then
                    Return DirectCast(Me.Right, BoundUserDefinedBinaryOperator).Call.Method
                End If

                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IOperator.UsesOperatorMethod
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

        Private ReadOnly Property ILiteralClass As LiteralKind Implements ILiteral.LiteralClass
            Get
                Return Semantics.Expression.DeriveLiteralKind(Me.Type)
            End Get
        End Property

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
            Return ArgumentMatchingParameter(Me.Arguments, parameter)
        End Function

        Private ReadOnly Property IArguments As ImmutableArray(Of IArgument) Implements IInvocation.Arguments
            Get
                Return DeriveArguments(Me.Arguments)
            End Get
        End Property

        Private ReadOnly Property IInvocationClass As InvocationKind Implements IInvocation.InvocationClass
            Get
                If Me.Method.IsShared Then
                    Return InvocationKind.Static
                End If

                If Me.Method.IsOverridable AndAlso Me.ReceiverOpt.Kind <> BoundKind.MyBaseReference AndAlso Me.ReceiverOpt.Kind <> BoundKind.MyClassReference Then
                    Return InvocationKind.Virtual
                End If

                Return InvocationKind.NonVirtualInstance
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

        Friend Shared Function ArgumentMatchingParameter(arguments As ImmutableArray(Of BoundExpression), parameter As IParameterSymbol) As IArgument
            Dim index As Integer = parameter.Ordinal
            If index <= arguments.Length Then
                Return DeriveArgument(arguments(index))
            End If

            Return Nothing
        End Function

        Friend Shared Function DeriveArguments(boundArguments As ImmutableArray(Of BoundExpression)) As ImmutableArray(Of IArgument)
            Dim argumentsLength As Integer = boundArguments.Length
            Dim arguments As ArrayBuilder(Of IArgument) = ArrayBuilder(Of IArgument).GetInstance(argumentsLength)
            For index As Integer = 0 To argumentsLength - 1 Step 1
                arguments(index) = DeriveArgument(boundArguments(index))
            Next

            Return arguments.ToImmutableAndFree()
        End Function

        Private Shared ArgumentMappings As New System.Runtime.CompilerServices.ConditionalWeakTable(Of BoundExpression, IArgument)

        Private Shared Function DeriveArgument(argument As BoundExpression) As IArgument
            Select Case argument.Kind
                Case BoundKind.ByRefArgumentWithCopyBack
                    Return DirectCast(argument, BoundByRefArgumentWithCopyBack)
                Case Else
                    Return ArgumentMappings.GetValue(argument, Function(a) New Argument(a))
            End Select
        End Function

        Private Class Argument
            Implements IArgument

            Private ReadOnly _Value As IExpression

            Public Sub New(value As IExpression)
                Me._Value = value
            End Sub

            Public ReadOnly Property ArgumentClass As ArgumentKind Implements IArgument.ArgumentClass
                Get
                    ' Apparently the VB bound trees don't encode named arguments, which seems unnecesarily lossy.
                    Return ArgumentKind.Positional
                End Get
            End Property

            Public ReadOnly Property Mode As ArgumentMode Implements IArgument.Mode
                Get
                    Return ArgumentMode.In
                End Get
            End Property

            Public ReadOnly Property Value As IExpression Implements IArgument.Value
                Get
                    Return Me._Value
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
    End Class

    Partial Class BoundByRefArgumentWithCopyBack
        Implements IArgument

        Private ReadOnly Property IArgumentClass As ArgumentKind Implements IArgument.ArgumentClass
            Get
                ' Do the VB bound trees encode named arguments?
                Return ArgumentKind.Positional
            End Get
        End Property

        Private ReadOnly Property IMode As ArgumentMode Implements IArgument.Mode
            Get
                If Me.InPlaceholder IsNot Nothing AndAlso Me.InPlaceholder.IsOut Then
                    Return ArgumentMode.Out
                End If

                Return ArgumentMode.Reference
            End Get
        End Property

        Private ReadOnly Property IValue As IExpression Implements IArgument.Value
            Get
                Return Me.OriginalArgument
            End Get
        End Property

        Private ReadOnly Property IInConversion As IExpression Implements IArgument.InConversion
            Get
                Return Me.InConversion
            End Get
        End Property

        Private ReadOnly Property IOutConversion As IExpression Implements IArgument.OutConversion
            Get
                Return Me.OutConversion
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.None
        End Function
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

        Private ReadOnly Property IReferenceClass As ReferenceKind Implements IReference.ReferenceClass
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

        Private ReadOnly Property IOperator As IMethodSymbol Implements IOperator.Operator
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IOperator.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property IOperand As IExpression Implements IUnary.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IOperation As UnaryOperatorCode Implements IUnary.Operation
            Get
                Return DeriveUnaryOperatorCode(Me.OperatorKind, Me.Operand)
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.UnaryOperator
        End Function
    End Class

    Partial Class BoundUserDefinedUnaryOperator
        Implements IUnary

        Private ReadOnly Property IOperator As IMethodSymbol Implements IOperator.Operator
            Get
                Return Me.Call.Method
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IOperator.UsesOperatorMethod
            Get
                Return True
            End Get
        End Property

        Private ReadOnly Property IOperand As IExpression Implements IUnary.Operand
            Get
                Return Me.Operand
            End Get
        End Property

        Private ReadOnly Property IOperation As UnaryOperatorCode Implements IUnary.Operation
            Get
                Select Case OperatorKind And UnaryOperatorKind.OpMask
                    Case UnaryOperatorKind.Plus
                        Return UnaryOperatorCode.OperatorPlus
                    Case UnaryOperatorKind.Minus
                        Return UnaryOperatorCode.OperatorMinus
                    Case UnaryOperatorKind.Not
                        Return UnaryOperatorCode.OperatorBitwiseNegation
                End Select

                Return UnaryOperatorCode.None
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

        Private ReadOnly Property IOperation As BinaryOperatorCode Implements IBinary.Operation
            Get
                Return DeriveBinaryOperatorCode(Me.OperatorKind, Me.Left)
            End Get
        End Property

        Private ReadOnly Property IRight As IExpression Implements IBinary.Right, IRelational.Right
            Get
                Return Me.Right
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements IOperator.Operator
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IOperator.UsesOperatorMethod
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property RelationalCode As RelationalOperatorCode Implements IRelational.RelationalCode
            Get
                Return DeriveRelationalOperatorCode(Me.OperatorKind, Me.Left)
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

        Private ReadOnly Property IOperation As BinaryOperatorCode Implements IBinary.Operation
            Get
                Select Case OperatorKind And BinaryOperatorKind.OpMask
                    Case BinaryOperatorKind.Add
                        Return BinaryOperatorCode.OperatorAdd
                    Case BinaryOperatorKind.Subtract
                        Return BinaryOperatorCode.OperatorSubtract
                    Case BinaryOperatorKind.Multiply
                        Return BinaryOperatorCode.OperatorMultiply
                    Case BinaryOperatorKind.Divide
                        Return BinaryOperatorCode.OperatorDivide
                    Case BinaryOperatorKind.Modulo
                        Return BinaryOperatorCode.OperatorRemainder
                    Case BinaryOperatorKind.And
                        Return BinaryOperatorCode.OperatorAnd
                    Case BinaryOperatorKind.Or
                        Return BinaryOperatorCode.OperatorOr
                    Case BinaryOperatorKind.Xor
                        Return BinaryOperatorCode.OperatorXor
                    Case BinaryOperatorKind.AndAlso
                        Return BinaryOperatorCode.OperatorConditionalAnd
                    Case BinaryOperatorKind.OrElse
                        Return BinaryOperatorCode.OperatorConditionalOr
                    Case BinaryOperatorKind.LeftShift
                        Return BinaryOperatorCode.OperatorLeftShift
                    Case BinaryOperatorKind.RightShift
                        Return BinaryOperatorCode.OperatorRightShift
                End Select

                Return BinaryOperatorCode.None
            End Get
        End Property

        Private ReadOnly Property IRelationalCode As RelationalOperatorCode Implements IRelational.RelationalCode
            Get
                Select Case OperatorKind And BinaryOperatorKind.OpMask
                    Case BinaryOperatorKind.LessThan
                        Return RelationalOperatorCode.OperatorLess
                    Case BinaryOperatorKind.LessThanOrEqual
                        Return RelationalOperatorCode.OperatorLessEqual
                    Case BinaryOperatorKind.Equals
                        Return RelationalOperatorCode.OperatorEqual
                    Case BinaryOperatorKind.NotEquals
                        Return RelationalOperatorCode.OperatorNotEqual
                    Case BinaryOperatorKind.GreaterThanOrEqual
                        Return RelationalOperatorCode.OperatorGreaterEqual
                    Case BinaryOperatorKind.GreaterThan
                        Return RelationalOperatorCode.OperatorGreater
                End Select

                Return RelationalOperatorCode.None
            End Get
        End Property

        Private ReadOnly Property IRight As IExpression Implements IBinary.Right, IRelational.Right
            Get
                Return Me.Right
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements IOperator.Operator
            Get
                Return Me.Call.Method
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IOperator.UsesOperatorMethod
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

        Private ReadOnly Property IOperation As BinaryOperatorCode Implements IBinary.Operation
            Get
                Return If((Me.BitwiseOperator.OperatorKind And BinaryOperatorKind.And) <> 0, BinaryOperatorCode.OperatorConditionalAnd, BinaryOperatorCode.OperatorConditionalOr)
            End Get
        End Property

        Private ReadOnly Property IRight As IExpression Implements IBinary.Right
            Get
                Return Me.BitwiseOperator.Right
            End Get
        End Property

        Private ReadOnly Property IOperator As IMethodSymbol Implements IOperator.Operator
            Get
                Return Me.BitwiseOperator.Call.Method
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IOperator.UsesOperatorMethod
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

        Private ReadOnly Property IOperator As IMethodSymbol Implements IOperator.Operator
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IOperator.UsesOperatorMethod
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

        Private ReadOnly Property IOperator As IMethodSymbol Implements IOperator.Operator
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IOperator.UsesOperatorMethod
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

        Private ReadOnly Property IOperator As IMethodSymbol Implements IOperator.Operator
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IOperator.UsesOperatorMethod
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

        Private ReadOnly Property IOperator As IMethodSymbol Implements IOperator.Operator
            Get
                Return Me.Call.Method
            End Get
        End Property

        Private ReadOnly Property IUsesOperatorMethod As Boolean Implements IOperator.UsesOperatorMethod
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
            Return BoundCall.ArgumentMatchingParameter(Me.Arguments, parameter)
        End Function

        Private ReadOnly Property IConstructor As IMethodSymbol Implements IObjectCreation.Constructor
            Get
                Return Me.ConstructorOpt
            End Get
        End Property

        Private ReadOnly Property IConstructorArguments As ImmutableArray(Of IArgument) Implements IObjectCreation.ConstructorArguments
            Get
                Return BoundCall.DeriveArguments(Me.Arguments)
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
                    Dim dimension As ArrayBuilder(Of IArrayInitializer) = ArrayBuilder(Of IArrayInitializer).GetInstance(arrayInitalizer.Initializers.Length)

                    For index As Integer = 0 To arrayInitalizer.Initializers.Length - 1
                        Dim elementInitializer As BoundExpression = arrayInitalizer.Initializers(index)
                        Dim elementArray As BoundArrayInitialization = TryCast(elementInitializer, BoundArrayInitialization)
                        dimension(index) = If(elementArray IsNot Nothing, MakeInitializer(elementArray), New ElementInitializer(elementInitializer))
                    Next

                    Return New DimensionInitializer(dimension.ToImmutableAndFree())
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

        Private ReadOnly Property IReferenceClass As ReferenceKind Implements IReference.ReferenceClass
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

        Private ReadOnly Property IReferenceClass As ReferenceKind Implements IReference.ReferenceClass
            Get
                Return If(Me.ReceiverOpt IsNot Nothing, ReferenceKind.InstanceField, ReferenceKind.StaticField)
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.FieldReference
        End Function
    End Class

    Partial Class BoundParameter
        Implements IParameterReference

        Private ReadOnly Property IParameter As IParameterSymbol Implements IParameterReference.Parameter
            Get
                Return Me.ParameterSymbol
            End Get
        End Property

        Private ReadOnly Property IReferenceClass As ReferenceKind Implements IReference.ReferenceClass
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

        Private ReadOnly Property IReferenceClass As ReferenceKind Implements IReference.ReferenceClass
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

        Private ReadOnly Property IReferenceClass As ReferenceKind Implements IReference.ReferenceClass
            Get
                Return ReferenceKind.LateBoundMember
            End Get
        End Property

        Protected Overrides Function ExpressionKind() As OperationKind
            Return OperationKind.LateBoundMemberReference
        End Function
    End Class

    Module Expression
        Friend Function DeriveUnaryOperatorCode(operatorKind As UnaryOperatorKind, operand As BoundExpression) As UnaryOperatorCode
            Select Case operand.Type.SpecialType
                Case SpecialType.System_Byte, SpecialType.System_Int16, SpecialType.System_Int32, SpecialType.System_Int64, SpecialType.System_SByte, SpecialType.System_UInt16, SpecialType.System_UInt32, SpecialType.System_UInt64
                    Select Case operatorKind And UnaryOperatorKind.OpMask
                        Case UnaryOperatorKind.Plus
                            Return UnaryOperatorCode.IntegerPlus
                        Case UnaryOperatorKind.Minus
                            Return UnaryOperatorCode.IntegerMinus
                        Case UnaryOperatorKind.Not
                            Return UnaryOperatorCode.IntegerBitwiseNegation
                    End Select
                Case SpecialType.System_Single, SpecialType.System_Double
                    Select Case operatorKind And UnaryOperatorKind.OpMask
                        Case UnaryOperatorKind.Plus
                            Return UnaryOperatorCode.FloatingPlus
                        Case UnaryOperatorKind.Minus
                            Return UnaryOperatorCode.FloatingMinus
                    End Select
                Case SpecialType.System_Decimal
                    Select Case operatorKind And UnaryOperatorKind.OpMask
                        Case UnaryOperatorKind.Plus
                            Return UnaryOperatorCode.DecimalPlus
                        Case UnaryOperatorKind.Minus
                            Return UnaryOperatorCode.DecimalMinus
                    End Select
                Case SpecialType.System_Boolean
                    Select Case operatorKind And UnaryOperatorKind.OpMask
                        Case UnaryOperatorKind.Not
                            Return UnaryOperatorCode.BooleanBitwiseNegation
                    End Select
                Case SpecialType.System_Object
                    Select Case operatorKind And UnaryOperatorKind.OpMask
                        Case UnaryOperatorKind.Plus
                            Return UnaryOperatorCode.ObjectPlus
                        Case UnaryOperatorKind.Minus
                            Return UnaryOperatorCode.ObjectMinus
                        Case UnaryOperatorKind.Not
                            Return UnaryOperatorCode.ObjectNot
                    End Select
            End Select

            Return UnaryOperatorCode.None
        End Function

        Friend Function DeriveBinaryOperatorCode(operatorKind As BinaryOperatorKind, left As BoundExpression) As BinaryOperatorCode
            Select Case left.Type.SpecialType
                Case SpecialType.System_Byte, SpecialType.System_UInt16, SpecialType.System_UInt32, SpecialType.System_UInt64
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Add
                            Return BinaryOperatorCode.IntegerAdd
                        Case BinaryOperatorKind.Subtract
                            Return BinaryOperatorCode.IntegerSubtract
                        Case BinaryOperatorKind.Multiply
                            Return BinaryOperatorCode.IntegerMultiply
                        Case BinaryOperatorKind.IntegerDivide
                            Return BinaryOperatorCode.IntegerDivide
                        Case BinaryOperatorKind.Modulo
                            Return BinaryOperatorCode.IntegerRemainder
                        Case BinaryOperatorKind.And
                            Return BinaryOperatorCode.IntegerAnd
                        Case BinaryOperatorKind.Or
                            Return BinaryOperatorCode.IntegerOr
                        Case BinaryOperatorKind.Xor
                            Return BinaryOperatorCode.IntegerXor
                        Case BinaryOperatorKind.LeftShift
                            Return BinaryOperatorCode.IntegerLeftShift
                        Case BinaryOperatorKind.RightShift
                            Return BinaryOperatorCode.IntegerRightShift
                    End Select
                Case SpecialType.System_SByte, SpecialType.System_Int16, SpecialType.System_Int32, SpecialType.System_Int64
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Add
                            Return BinaryOperatorCode.UnsignedAdd
                        Case BinaryOperatorKind.Subtract
                            Return BinaryOperatorCode.UnsignedSubtract
                        Case BinaryOperatorKind.Multiply
                            Return BinaryOperatorCode.UnsignedMultiply
                        Case BinaryOperatorKind.IntegerDivide
                            Return BinaryOperatorCode.UnsignedDivide
                        Case BinaryOperatorKind.Modulo
                            Return BinaryOperatorCode.UnsignedRemainder
                        Case BinaryOperatorKind.And
                            Return BinaryOperatorCode.UnsignedAnd
                        Case BinaryOperatorKind.Or
                            Return BinaryOperatorCode.UnsignedOr
                        Case BinaryOperatorKind.Xor
                            Return BinaryOperatorCode.UnsignedXor
                        Case BinaryOperatorKind.LeftShift
                            Return BinaryOperatorCode.UnsignedLeftShift
                        Case BinaryOperatorKind.RightShift
                            Return BinaryOperatorCode.UnsignedRightShift
                    End Select
                Case SpecialType.System_Single, SpecialType.System_Double
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Add
                            Return BinaryOperatorCode.FloatingAdd
                        Case BinaryOperatorKind.Subtract
                            Return BinaryOperatorCode.FloatingSubtract
                        Case BinaryOperatorKind.Multiply
                            Return BinaryOperatorCode.FloatingMultiply
                        Case BinaryOperatorKind.Divide
                            Return BinaryOperatorCode.FloatingDivide
                        Case BinaryOperatorKind.Modulo
                            Return BinaryOperatorCode.FloatingRemainder
                        Case BinaryOperatorKind.Power
                            Return BinaryOperatorCode.FloatingPower
                    End Select
                Case SpecialType.System_Decimal
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Add
                            Return BinaryOperatorCode.DecimalAdd
                        Case BinaryOperatorKind.Subtract
                            Return BinaryOperatorCode.DecimalSubtract
                        Case BinaryOperatorKind.Multiply
                            Return BinaryOperatorCode.DecimalMultiply
                        Case BinaryOperatorKind.Divide
                            Return BinaryOperatorCode.DecimalDivide
                    End Select
                Case SpecialType.System_Boolean
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.And
                            Return BinaryOperatorCode.BooleanAnd
                        Case BinaryOperatorKind.Or
                            Return BinaryOperatorCode.BooleanOr
                        Case BinaryOperatorKind.Xor
                            Return BinaryOperatorCode.BooleanXor
                        Case BinaryOperatorKind.AndAlso
                            Return BinaryOperatorCode.BooleanConditionalAnd
                        Case BinaryOperatorKind.OrElse
                            Return BinaryOperatorCode.BooleanConditionalOr
                    End Select
                Case SpecialType.System_String
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Concatenate
                            Return BinaryOperatorCode.StringConcatenation
                    End Select
                Case SpecialType.System_Object
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Add
                            Return BinaryOperatorCode.ObjectAdd
                        Case BinaryOperatorKind.Subtract
                            Return BinaryOperatorCode.ObjectSubtract
                        Case BinaryOperatorKind.Multiply
                            Return BinaryOperatorCode.ObjectMultiply
                        Case BinaryOperatorKind.Power
                            Return BinaryOperatorCode.ObjectPower
                        Case BinaryOperatorKind.IntegerDivide
                            Return BinaryOperatorCode.ObjectIntegerDivide
                        Case BinaryOperatorKind.Divide
                            Return BinaryOperatorCode.ObjectDivide
                        Case BinaryOperatorKind.Modulo
                            Return BinaryOperatorCode.ObjectRemainder
                        Case BinaryOperatorKind.Concatenate
                            Return BinaryOperatorCode.ObjectConcatenation
                        Case BinaryOperatorKind.And
                            Return BinaryOperatorCode.ObjectAnd
                        Case BinaryOperatorKind.Or
                            Return BinaryOperatorCode.ObjectOr
                        Case BinaryOperatorKind.Xor
                            Return BinaryOperatorCode.ObjectXor
                        Case BinaryOperatorKind.AndAlso
                            Return BinaryOperatorCode.ObjectConditionalAnd
                        Case BinaryOperatorKind.OrElse
                            Return BinaryOperatorCode.ObjectConditionalOr
                        Case BinaryOperatorKind.LeftShift
                            Return BinaryOperatorCode.ObjectLeftShift
                        Case BinaryOperatorKind.RightShift
                            Return BinaryOperatorCode.ObjectRightShift
                    End Select
            End Select

            If left.Type.TypeKind = TypeKind.Enum Then
                Select Case operatorKind And BinaryOperatorKind.OpMask
                    Case BinaryOperatorKind.Add
                        Return BinaryOperatorCode.EnumAdd
                    Case BinaryOperatorKind.Subtract
                        Return BinaryOperatorCode.EnumSubtract
                    Case BinaryOperatorKind.And
                        Return BinaryOperatorCode.EnumAnd
                    Case BinaryOperatorKind.Or
                        Return BinaryOperatorCode.EnumOr
                    Case BinaryOperatorKind.Xor
                        Return BinaryOperatorCode.EnumXor
                End Select
            End If

            Return BinaryOperatorCode.None
        End Function

        Friend Function DeriveRelationalOperatorCode(operatorKind As BinaryOperatorKind, left As BoundExpression) As RelationalOperatorCode
            Select Case left.Type.SpecialType
                Case SpecialType.System_Byte, SpecialType.System_UInt16, SpecialType.System_UInt32, SpecialType.System_UInt64, SpecialType.System_Char
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.LessThan
                            Return RelationalOperatorCode.UnsignedLess
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return RelationalOperatorCode.UnsignedLessEqual
                        Case BinaryOperatorKind.Equals
                            Return RelationalOperatorCode.IntegerEqual
                        Case BinaryOperatorKind.NotEquals
                            Return RelationalOperatorCode.IntegerNotEqual
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return RelationalOperatorCode.UnsignedGreaterEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return RelationalOperatorCode.UnsignedGreater
                    End Select
                Case SpecialType.System_SByte, SpecialType.System_Int16, SpecialType.System_Int32, SpecialType.System_Int64
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.LessThan
                            Return RelationalOperatorCode.IntegerLess
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return RelationalOperatorCode.IntegerLessEqual
                        Case BinaryOperatorKind.Equals
                            Return RelationalOperatorCode.IntegerEqual
                        Case BinaryOperatorKind.NotEquals
                            Return RelationalOperatorCode.IntegerNotEqual
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return RelationalOperatorCode.IntegerGreaterEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return RelationalOperatorCode.IntegerGreater
                    End Select
                Case SpecialType.System_Single, SpecialType.System_Double
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.LessThan
                            Return RelationalOperatorCode.FloatingLess
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return RelationalOperatorCode.FloatingLessEqual
                        Case BinaryOperatorKind.Equals
                            Return RelationalOperatorCode.FloatingEqual
                        Case BinaryOperatorKind.NotEquals
                            Return RelationalOperatorCode.FloatingNotEqual
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return RelationalOperatorCode.FloatingGreaterEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return RelationalOperatorCode.FloatingGreater
                    End Select
                Case SpecialType.System_Decimal
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.LessThan
                            Return RelationalOperatorCode.DecimalLess
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return RelationalOperatorCode.DecimalLessEqual
                        Case BinaryOperatorKind.Equals
                            Return RelationalOperatorCode.DecimalEqual
                        Case BinaryOperatorKind.NotEquals
                            Return RelationalOperatorCode.DecimalNotEqual
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return RelationalOperatorCode.DecimalGreaterEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return RelationalOperatorCode.DecimalGreater
                    End Select
                Case SpecialType.System_Boolean
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Equals
                            Return RelationalOperatorCode.BooleanEqual
                        Case BinaryOperatorKind.NotEquals
                            Return RelationalOperatorCode.BooleanNotEqual
                    End Select
                Case SpecialType.System_String
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.Equals
                            Return RelationalOperatorCode.StringEqual
                        Case BinaryOperatorKind.NotEquals
                            Return RelationalOperatorCode.StringNotEqual
                        Case BinaryOperatorKind.Like
                            Return RelationalOperatorCode.StringLike
                    End Select
                Case SpecialType.System_Object
                    Select Case operatorKind And BinaryOperatorKind.OpMask
                        Case BinaryOperatorKind.LessThan
                            Return RelationalOperatorCode.ObjectLess
                        Case BinaryOperatorKind.LessThanOrEqual
                            Return RelationalOperatorCode.ObjectLessEqual
                        Case BinaryOperatorKind.Equals
                            Return RelationalOperatorCode.ObjectVBEqual
                        Case BinaryOperatorKind.Is
                            Return RelationalOperatorCode.ObjectEqual
                        Case BinaryOperatorKind.IsNot
                            Return RelationalOperatorCode.ObjectNotEqual
                        Case BinaryOperatorKind.NotEquals
                            Return RelationalOperatorCode.ObjectVBNotEqual
                        Case BinaryOperatorKind.Like
                            Return RelationalOperatorCode.ObjectLike
                        Case BinaryOperatorKind.GreaterThanOrEqual
                            Return RelationalOperatorCode.ObjectGreaterEqual
                        Case BinaryOperatorKind.GreaterThan
                            Return RelationalOperatorCode.ObjectGreater
                    End Select
            End Select

            If left.Type.TypeKind = TypeKind.Enum Then
                Select Case operatorKind And BinaryOperatorKind.OpMask
                    Case BinaryOperatorKind.LessThan
                        Return RelationalOperatorCode.EnumLess
                    Case BinaryOperatorKind.LessThanOrEqual
                        Return RelationalOperatorCode.EnumLessEqual
                    Case BinaryOperatorKind.Equals
                        Return RelationalOperatorCode.EnumEqual
                    Case BinaryOperatorKind.NotEquals
                        Return RelationalOperatorCode.EnumNotEqual
                    Case BinaryOperatorKind.GreaterThanOrEqual
                        Return RelationalOperatorCode.EnumGreaterEqual
                    Case BinaryOperatorKind.GreaterThan
                        Return RelationalOperatorCode.EnumGreater
                End Select
            End If

            Return RelationalOperatorCode.None
        End Function
    End Module
End Namespace
