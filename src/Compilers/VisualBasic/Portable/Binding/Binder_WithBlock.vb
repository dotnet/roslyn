' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Binder used to bind statements inside With blocks. 
    ''' </summary>
    Friend NotInheritable Class WithBlockBinder
        Inherits BlockBaseBinder

        ''' <summary> Reference to a With statement syntax this binder is created for </summary>
        Private ReadOnly _withBlockSyntax As WithBlockSyntax

        ''' <summary> Reference to an expression from With statement </summary>
        Private ReadOnly Property Expression As ExpressionSyntax
            Get
                Return Me._withBlockSyntax.WithStatement.Expression
            End Get
        End Property

        ''' <summary> 
        ''' Holds information needed by With block to properly bind 
        ''' references to With block expression placeholder
        ''' </summary>
        Private _withBlockInfo As WithBlockInfo = Nothing

        Friend ReadOnly Property Info As WithBlockInfo
            Get
                Return _withBlockInfo
            End Get
        End Property

        ''' <summary> 
        ''' True if there were references to the With statement expression 
        ''' placeholder which prevent ByRef local from being used 
        ''' </summary>
        Friend ReadOnly Property ExpressionIsAccessedFromNestedLambda As Boolean
            Get
                Debug.Assert(Me._withBlockInfo IsNot Nothing)
                Return Me._withBlockInfo.ExpressionIsAccessedFromNestedLambda
            End Get
        End Property

        ''' <summary>
        ''' With statement expression placeholder is a bound node being used in initial binding
        ''' to represent with statement expression. In lowering it is to be replaced with
        ''' the lowered expression which will actually be emitted.
        ''' </summary>
        Friend ReadOnly Property ExpressionPlaceholder As BoundValuePlaceholderBase
            Get
                Debug.Assert(Me._withBlockInfo IsNot Nothing)
                Return Me._withBlockInfo.ExpressionPlaceholder
            End Get
        End Property

        ''' <summary>
        ''' A draft version of initializers which will be used in this With statement. 
        ''' Initializers are expressions which are used to capture expression in the current
        ''' With statement; they can be empty in some cases like if the expression is a local 
        ''' variable of value type.
        ''' 
        ''' Note, the initializers returned by this property are 'draft' because they are 
        ''' generated based on initial bound tree, the real initializers will be generated 
        ''' in lowering based on lowered expression form.
        ''' </summary>
        Friend ReadOnly Property DraftInitializers As ImmutableArray(Of BoundExpression)
            Get
                Debug.Assert(Me._withBlockInfo IsNot Nothing)
                Return Me._withBlockInfo.DraftInitializers
            End Get
        End Property

        ''' <summary>
        ''' A draft version of placeholder substitute which will be used in this With statement. 
        ''' 
        ''' Note, the placeholder substitute returned by this property is 'draft' because it is
        ''' generated based on initial bound tree, the real substitute will be generated in lowering 
        ''' based on lowered expression form.
        ''' </summary>
        Friend ReadOnly Property DraftPlaceholderSubstitute As BoundExpression
            Get
                Debug.Assert(Me._withBlockInfo IsNot Nothing)
                Return Me._withBlockInfo.DraftSubstitute
            End Get
        End Property

        ''' <summary> Holds information needed by With block to properly bind 
        ''' references to With block expression, placeholder, etc... </summary>
        Friend Class WithBlockInfo

            Public Sub New(originalExpression As BoundExpression,
                           expressionPlaceholder As BoundValuePlaceholderBase,
                           draftSubstitute As BoundExpression,
                           draftInitializers As ImmutableArray(Of BoundExpression),
                           diagnostics As ImmutableBindingDiagnostic(Of AssemblySymbol))

                Debug.Assert(originalExpression IsNot Nothing)
                Debug.Assert(expressionPlaceholder IsNot Nothing AndAlso (expressionPlaceholder.Kind = BoundKind.WithLValueExpressionPlaceholder OrElse expressionPlaceholder.Kind = BoundKind.WithRValueExpressionPlaceholder))
                Debug.Assert(draftSubstitute IsNot Nothing)
                Debug.Assert(Not draftInitializers.IsDefault)

                Me.OriginalExpression = originalExpression
                Me.ExpressionPlaceholder = expressionPlaceholder
                Me.DraftSubstitute = draftSubstitute
                Me.DraftInitializers = draftInitializers
                Me.Diagnostics = diagnostics
            End Sub

            ''' <summary> Original bound expression from With statement </summary>
            Public ReadOnly OriginalExpression As BoundExpression

            ''' <summary> Bound placeholder expression if used, otherwise Nothing </summary>
            Public ReadOnly ExpressionPlaceholder As BoundValuePlaceholderBase

            ''' <summary> Diagnostics produced while binding the expression </summary>
            Public ReadOnly Diagnostics As ImmutableBindingDiagnostic(Of AssemblySymbol)

            ''' <summary> 
            ''' Draft initializers for With statement, is based on initial binding tree 
            ''' and is only to be used for warnings generation as well as for flow analysis 
            ''' and semantic API; real initializers will be re-calculated in lowering
            ''' </summary>
            Public ReadOnly DraftInitializers As ImmutableArray(Of BoundExpression)

            ''' <summary> 
            ''' Draft substitute for With expression placeholder, is based on initial 
            ''' binding tree and is only to be used for warnings generation as well as 
            ''' for flow analysis and semantic API; real substitute will be re-calculated 
            ''' in lowering
            ''' </summary>
            Public ReadOnly DraftSubstitute As BoundExpression

            Public ReadOnly Property ExpressionIsAccessedFromNestedLambda As Boolean
                Get
                    Return Me._exprAccessedFromNestedLambda = ThreeState.True
                End Get
            End Property

            Public Sub RegisterAccessFromNestedLambda()
                If Me._exprAccessedFromNestedLambda <> ThreeState.True Then
                    Dim oldValue = Interlocked.CompareExchange(Me._exprAccessedFromNestedLambda, ThreeState.True, ThreeState.Unknown)
                    Debug.Assert(oldValue = ThreeState.Unknown OrElse oldValue = ThreeState.True)
                End If
            End Sub

            Private _exprAccessedFromNestedLambda As Integer = ThreeState.Unknown

            ''' <summary>
            ''' If With statement expression is being used from nested lambda there are some restrictions
            ''' to the usage of Me reference in this expression. As these restrictions are only to be checked 
            ''' in few scenarios, this flag is being calculated lazily.
            ''' </summary>
            Public Function ExpressionHasByRefMeReference(recursionDepth As Integer) As Boolean
                If Me._exprHasByRefMeReference = ThreeState.Unknown Then
                    ' Analyze the expression which will be used instead of placeholder
                    Dim value As Boolean = ValueTypedMeReferenceFinder.HasByRefMeReference(Me.DraftSubstitute, recursionDepth)
                    Dim newValue As Integer = If(value, ThreeState.True, ThreeState.False)
                    Dim oldValue = Interlocked.CompareExchange(Me._exprHasByRefMeReference, newValue, ThreeState.Unknown)
                    Debug.Assert(newValue = oldValue OrElse oldValue = ThreeState.Unknown)
                End If

                Debug.Assert(Me._exprHasByRefMeReference <> ThreeState.Unknown)
                Return Me._exprHasByRefMeReference = ThreeState.True
            End Function

            Private _exprHasByRefMeReference As Integer = ThreeState.Unknown

        End Class

        ''' <summary> Create a new instance of With statement binder for a statement syntax provided </summary>
        Public Sub New(enclosing As Binder, syntax As WithBlockSyntax)
            MyBase.New(enclosing)

            Debug.Assert(syntax IsNot Nothing)
            Debug.Assert(syntax.WithStatement IsNot Nothing)
            Debug.Assert(syntax.WithStatement.Expression IsNot Nothing)
            Me._withBlockSyntax = syntax
        End Sub

#Region "Implementation"

        Friend Overrides Function GetWithStatementPlaceholderSubstitute(placeholder As BoundValuePlaceholderBase) As BoundExpression
            Me.EnsureExpressionAndPlaceholder()
            If placeholder Is Me.ExpressionPlaceholder Then
                Return Me.DraftPlaceholderSubstitute
            End If
            Return MyBase.GetWithStatementPlaceholderSubstitute(placeholder)
        End Function

        Private Sub EnsureExpressionAndPlaceholder()

            If Me._withBlockInfo Is Nothing Then
                ' Because we cannot guarantee that diagnostics will be freed we 
                ' don't allocate this diagnostics bag from a pool
                Dim diagnostics = BindingDiagnosticBag.GetInstance()

                ' Bind the expression as a value
                Dim boundExpression As BoundExpression = Me.ContainingBinder.BindValue(Me.Expression, diagnostics)

                ' NOTE: If the expression is not an l-value we should make an r-value of it
                If Not boundExpression.IsLValue Then
                    boundExpression = Me.MakeRValue(boundExpression, diagnostics)
                End If

                ' Prepare draft substitute/initializers for expression placeholder;
                ' note that those substitute/initializers will be based on initial bound 
                ' form of the original expression captured without using ByRef locals
                Dim result As WithExpressionRewriter.Result =
                    (New WithExpressionRewriter(Me._withBlockSyntax.WithStatement)).AnalyzeWithExpression(Me.ContainingMember, boundExpression,
                                                                 doNotUseByRefLocal:=True,
                                                                 binder:=Me.ContainingBinder,
                                                                 preserveIdentityOfLValues:=True)

                ' Create a placeholder if needed
                Dim placeholder As BoundValuePlaceholderBase = Nothing
                If boundExpression.IsLValue OrElse boundExpression.IsMeReference Then
                    placeholder = New BoundWithLValueExpressionPlaceholder(Me.Expression, boundExpression.Type)
                Else
                    placeholder = New BoundWithRValueExpressionPlaceholder(Me.Expression, boundExpression.Type)
                End If
                placeholder.SetWasCompilerGenerated()

                ' It is assumed that the binding result in case of race should still be the same in all racing threads, 
                ' so if the following call fails we can just drop the bound node and diagnostics on the floor
                Interlocked.CompareExchange(Me._withBlockInfo,
                                            New WithBlockInfo(boundExpression, placeholder,
                                                              result.Expression, result.Initializers, diagnostics.ToReadOnlyAndFree()),
                                            Nothing)
            End If

            Debug.Assert(Me._withBlockInfo IsNot Nothing)
        End Sub

        ''' <summary>
        ''' A bound tree walker which search for a bound Me and MyClass references of value type. 
        ''' Is being only used for calculating the value of 'ExpressionHasByRefMeReference'
        ''' </summary>
        Private Class ValueTypedMeReferenceFinder
            Inherits BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator

            Private Sub New(recursionDepth As Integer)
                MyBase.New(recursionDepth)
            End Sub

            Private _found As Boolean = False

            Public Shared Function HasByRefMeReference(expression As BoundExpression, recursionDepth As Integer) As Boolean
                Dim walker As New ValueTypedMeReferenceFinder(recursionDepth)
                walker.Visit(expression)
                Return walker._found
            End Function

            Public Overrides Function Visit(node As BoundNode) As BoundNode
                If Not _found Then
                    Return MyBase.Visit(node)
                End If

                Return Nothing
            End Function

            Public Overrides Function VisitMeReference(node As BoundMeReference) As BoundNode
                Dim type As TypeSymbol = node.Type
                Debug.Assert(Not type.IsTypeParameter)
                Debug.Assert(type.IsValueType)
                Me._found = True
                Return Nothing
            End Function

            Public Overrides Function VisitMyClassReference(node As BoundMyClassReference) As BoundNode
                Dim type As TypeSymbol = node.Type
                Debug.Assert(Not type.IsTypeParameter)
                Debug.Assert(type.IsValueType)
                Me._found = True
                Return Nothing
            End Function

        End Class

#End Region

#Region "With block binding"

        Protected Overrides Function CreateBoundWithBlock(node As WithBlockSyntax, boundBlockBinder As Binder, diagnostics As BindingDiagnosticBag) As BoundStatement
            Debug.Assert(node Is Me._withBlockSyntax)

            ' Bind With statement expression
            Me.EnsureExpressionAndPlaceholder()

            ' We need to take care of possible diagnostics that might be produced 
            ' by EnsureExpressionAndPlaceholder call, note that this call might have
            ' been before in which case the diagnostics were stored in '_withBlockInfo'
            ' See also comment in PrepareBindingOfOmittedLeft(...)
            diagnostics.AddRange(Me._withBlockInfo.Diagnostics, allowMismatchInDependencyAccumulation:=True)

            Return New BoundWithStatement(node,
                                          Me._withBlockInfo.OriginalExpression,
                                          boundBlockBinder.BindBlock(node, node.Statements, diagnostics).MakeCompilerGenerated(),
                                          Me)
        End Function

#End Region

#Region "Other Overrides"

        ''' <summary> Asserts that the node is NOT from With statement expression </summary>
        <Conditional("DEBUG")>
        Private Sub AssertExpressionIsNotFromStatementExpression(node As SyntaxNode)
            While node IsNot Nothing
                Debug.Assert(node IsNot Me.Expression)
                node = node.Parent
            End While
        End Sub

#If DEBUG Then

        Public Overrides Function BindStatement(node As StatementSyntax, diagnostics As BindingDiagnosticBag) As BoundStatement
            AssertExpressionIsNotFromStatementExpression(node)
            Return MyBase.BindStatement(node, diagnostics)
        End Function

        Public Overrides Function GetBinder(node As SyntaxNode) As Binder
            AssertExpressionIsNotFromStatementExpression(node)
            Return MyBase.GetBinder(node)
        End Function

#End If

        Private Sub PrepareBindingOfOmittedLeft(node As VisualBasicSyntaxNode, diagnostics As BindingDiagnosticBag, accessingBinder As Binder)
            AssertExpressionIsNotFromStatementExpression(node)
            Debug.Assert((node.Kind = SyntaxKind.SimpleMemberAccessExpression) OrElse
                         (node.Kind = SyntaxKind.DictionaryAccessExpression) OrElse
                         (node.Kind = SyntaxKind.XmlAttributeAccessExpression) OrElse
                         (node.Kind = SyntaxKind.XmlElementAccessExpression) OrElse
                         (node.Kind = SyntaxKind.XmlDescendantAccessExpression) OrElse
                         (node.Kind = SyntaxKind.ConditionalAccessExpression))

            Me.EnsureExpressionAndPlaceholder()
            ' NOTE: In case the call above produced diagnostics they were stored in 
            '       '_withBlockInfo' and will be reported in later call to CreateBoundWithBlock(...)

            Dim info As WithBlockInfo = Me._withBlockInfo

            If Me.ContainingMember IsNot accessingBinder.ContainingMember Then
                ' The expression placeholder from With statement may be captured
                info.RegisterAccessFromNestedLambda()
            End If

        End Sub

        Protected Friend Overrides Function TryBindOmittedLeftForMemberAccess(node As MemberAccessExpressionSyntax,
                                                                              diagnostics As BindingDiagnosticBag,
                                                                              accessingBinder As Binder,
                                                                              <Out> ByRef wholeMemberAccessExpressionBound As Boolean) As BoundExpression
            PrepareBindingOfOmittedLeft(node, diagnostics, accessingBinder)

            wholeMemberAccessExpressionBound = False
            Return Me._withBlockInfo.ExpressionPlaceholder
        End Function

        Protected Overrides Function TryBindOmittedLeftForDictionaryAccess(node As MemberAccessExpressionSyntax,
                                                                           accessingBinder As Binder,
                                                                           diagnostics As BindingDiagnosticBag) As BoundExpression
            PrepareBindingOfOmittedLeft(node, diagnostics, accessingBinder)
            Return Me._withBlockInfo.ExpressionPlaceholder
        End Function

        Protected Overrides Function TryBindOmittedLeftForConditionalAccess(node As ConditionalAccessExpressionSyntax, accessingBinder As Binder, diagnostics As BindingDiagnosticBag) As BoundExpression
            PrepareBindingOfOmittedLeft(node, diagnostics, accessingBinder)
            Return Me._withBlockInfo.ExpressionPlaceholder
        End Function

        Protected Friend Overrides Function TryBindOmittedLeftForXmlMemberAccess(node As XmlMemberAccessExpressionSyntax,
                                                                                 diagnostics As BindingDiagnosticBag,
                                                                                 accessingBinder As Binder) As BoundExpression
            PrepareBindingOfOmittedLeft(node, diagnostics, accessingBinder)
            Return Me._withBlockInfo.ExpressionPlaceholder
        End Function

        Friend Overrides ReadOnly Property Locals As ImmutableArray(Of LocalSymbol)
            Get
                Return ImmutableArray(Of LocalSymbol).Empty
            End Get
        End Property

#End Region

    End Class

End Namespace

