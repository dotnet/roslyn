' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind
Imports ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Binding info for expressions and statements that are part of a member declaration.
    ''' Instances of this class should not be exposed to external consumers.
    ''' </summary>
    Partial Friend MustInherit Class MemberSemanticModel
        Inherits VBSemanticModel

        Private ReadOnly _root As SyntaxNode
        Private ReadOnly _rootBinder As Binder
        Private ReadOnly _containingPublicSemanticModel As PublicSemanticModel

        Private ReadOnly _operationFactory As Lazy(Of VisualBasicOperationFactory)

        Friend Sub New(root As SyntaxNode,
                       rootBinder As Binder,
                       containingPublicSemanticModel As PublicSemanticModel)

            Debug.Assert(containingPublicSemanticModel IsNot Nothing)

            _root = root
            _rootBinder = SemanticModelBinder.Mark(rootBinder, containingPublicSemanticModel.IgnoresAccessibility)
            _containingPublicSemanticModel = containingPublicSemanticModel

            _operationFactory = New Lazy(Of VisualBasicOperationFactory)(Function() New VisualBasicOperationFactory(Me))
        End Sub

        Friend ReadOnly Property RootBinder As Binder
            Get
                Return _rootBinder
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property Root As SyntaxNode
            Get
                Return _root
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsSpeculativeSemanticModel As Boolean
            Get
                Return _containingPublicSemanticModel.IsSpeculativeSemanticModel
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property OriginalPositionForSpeculation As Integer
            Get
                ' This property is not meaningful for member semantic models.
                ' An external consumer should never be able to access them directly.
                Throw ExceptionUtilities.Unreachable()
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property ParentModel As SemanticModel
            Get
                ' This property is not meaningful for member semantic models.
                ' An external consumer should never be able to access them directly.
                Throw ExceptionUtilities.Unreachable()
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ContainingPublicModelOrSelf As SemanticModel
            Get
                Return _containingPublicSemanticModel
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IgnoresAccessibility As Boolean
            Get
                Return _containingPublicSemanticModel.IgnoresAccessibility
            End Get
        End Property

        Friend NotOverridable Overloads Overrides Function GetEnclosingBinder(position As Integer) As Binder
            Dim binder = GetEnclosingBinderInternal(Me.RootBinder, Me.Root, FindInitialNodeFromPosition(position), position)
            Debug.Assert(binder IsNot Nothing)
            Return SemanticModelBinder.Mark(binder, IgnoresAccessibility)
        End Function

        Private Overloads Function GetEnclosingBinder(node As SyntaxNode) As Binder
            Dim binder = GetEnclosingBinderInternal(Me.RootBinder, Me.Root, node, node.SpanStart)
            Debug.Assert(binder IsNot Nothing)
            Return SemanticModelBinder.Mark(binder, IgnoresAccessibility)
        End Function

        ' Get the bound node corresponding to the root.
        Friend Overridable Function GetBoundRoot() As BoundNode
            Return GetUpperBoundNode(Me.Root)
        End Function

        Public Overrides Function ClassifyConversion(expression As ExpressionSyntax, destination As ITypeSymbol) As Conversion
            CheckSyntaxNode(expression)
            expression = SyntaxFactory.GetStandaloneExpression(expression)

            If destination Is Nothing Then
                Throw New ArgumentNullException(NameOf(destination))
            End If

            Dim vbDestination = destination.EnsureVbSymbolOrNothing(Of TypeSymbol)(NameOf(destination))

            Dim boundExpression = TryCast(Me.GetLowerBoundNode(expression), BoundExpression)

            If boundExpression Is Nothing OrElse vbDestination.IsErrorType() Then
                Return New Conversion(Nothing)  ' NoConversion
            End If

            Select Case boundExpression.Kind
                Case BoundKind.Lambda
                    ' Switch back to the unbound lambda node since bound lambda represents a lambda already
                    ' converted to whatever target type was provided by the context within the statement.
                    ' NOTE: Using the UnboundLambda in this way might result in new entries in its trial-binding
                    ' cache, but that won't affect future semantic model queries because it will already have
                    ' been bound (possibly for error recovery) on insertion into the syntax-to-bound-node map
                    ' and the result of that binding is also cached.  That is, even though the list of trial-bindings
                    ' may change, it will never again be consumed for error recovery (only as a cache), so there
                    ' should be no problems.
                    Dim sourceLambda = TryCast(DirectCast(boundExpression, BoundLambda).LambdaSymbol, SourceLambdaSymbol)

                    ' Can we get a synthesized lambda here? Handle the case that we do just in case.
                    Debug.Assert(sourceLambda IsNot Nothing)
                    If sourceLambda IsNot Nothing Then
                        boundExpression = sourceLambda.UnboundLambda
                    End If

                Case BoundKind.ArrayCreation
                    ' Switch back to the array literal node when we have it
                    Dim arrayLiteral = DirectCast(boundExpression, BoundArrayCreation).ArrayLiteralOpt

                    If arrayLiteral IsNot Nothing Then
                        boundExpression = arrayLiteral
                    End If
            End Select

            Return New Conversion(Conversions.ClassifyConversion(boundExpression, vbDestination, GetEnclosingBinder(boundExpression.Syntax), CompoundUseSiteInfo(Of AssemblySymbol).Discarded))
        End Function

        ''' <summary>
        ''' Get the highest bound node in the tree associated with a particular syntax node.
        ''' </summary>
        Friend Function GetUpperBoundNode(node As SyntaxNode) As BoundNode
            ' The bound nodes are stored in the map from highest to lowest, so the first bound node is the highest.
            Dim boundNodes = GetBoundNodes(node)

            If boundNodes.Length = 0 Then
                Return Nothing
            Else
                Return boundNodes(0)
            End If
        End Function

        ''' <summary>
        ''' Get the lowest bound node in the tree associated with a particular syntax node. Lowest is defined as last
        ''' in a pre-order traversal of the bound tree.
        ''' </summary>
        Friend Function GetLowerBoundNode(node As VisualBasicSyntaxNode) As BoundNode
            ' The bound nodes are stored in the map from highest to lowest, so the last bound node is the lowest.
            Dim boundNodes = GetBoundNodes(node)
            If boundNodes.Length = 0 Then
                Return Nothing
            Else
                Return boundNodes(boundNodes.Length - 1)
            End If
        End Function

        ''' <summary>
        ''' If node has an immediate parent that is an expression or statement or attribute, return
        ''' that (making sure it can be bound on its own). Otherwise return Nothing.
        ''' </summary>
        Protected Function GetBindableParent(node As VisualBasicSyntaxNode) As VisualBasicSyntaxNode
            Dim parent As VisualBasicSyntaxNode = node.Parent
            If parent Is Nothing OrElse node Is Me.Root Then
                Return Nothing
            End If

            Dim expressionSyntax = TryCast(parent, ExpressionSyntax)
            If expressionSyntax IsNot Nothing Then
                Return SyntaxFactory.GetStandaloneExpression(expressionSyntax)
            End If

            Dim statementSyntax = TryCast(parent, StatementSyntax)
            If statementSyntax IsNot Nothing AndAlso IsStandaloneStatement(statementSyntax) Then
                Return statementSyntax
            End If

            Dim attributeSyntax = TryCast(parent, AttributeSyntax)
            If attributeSyntax IsNot Nothing Then
                Return attributeSyntax
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Get a summary of the bound nodes associated with a particular syntax nodes,
        ''' and its parent. This is what the rest of the semantic model uses to determine
        ''' what to return back.
        ''' </summary>
        Friend Function GetBoundNodeSummary(node As VisualBasicSyntaxNode) As BoundNodeSummary
            ' TODO: Should these two call be merged into a single one for efficiency?
            Dim upperBound = GetUpperBoundNode(node)
            Dim lowerBound = GetLowerBoundNode(node)
            Dim parentSyntax As VisualBasicSyntaxNode = GetBindableParent(node)
            Dim lowerBoundOfParent = If(parentSyntax Is Nothing, Nothing, GetLowerBoundNode(parentSyntax))

            Return New BoundNodeSummary(lowerBound, upperBound, lowerBoundOfParent)
        End Function

        ''' <summary>
        ''' Gets a summary of the bound nodes associated with an underlying
        ''' bound call node for a raiseevent statement.
        ''' </summary>
        Friend Overrides Function GetInvokeSummaryForRaiseEvent(node As RaiseEventStatementSyntax) As BoundNodeSummary
            Dim upperBound = UnwrapRaiseEvent(GetUpperBoundNode(node))
            Dim lowerBound = UnwrapRaiseEvent(GetLowerBoundNode(node))

            Dim parentSyntax As VisualBasicSyntaxNode = GetBindableParent(node)
            Dim lowerBoundOfParent = If(parentSyntax Is Nothing, Nothing, UnwrapRaiseEvent(GetLowerBoundNode(parentSyntax)))

            Return New BoundNodeSummary(lowerBound, upperBound, lowerBoundOfParent)
        End Function

        ''' <summary>
        ''' if "node" argument is a BoundRaiseEvent, returns its underlying boundcall instead.
        ''' Otherwise returns "node" unchanged.
        ''' </summary>
        Private Shared Function UnwrapRaiseEvent(node As BoundNode) As BoundNode
            Dim asRaiseEvent = TryCast(node, BoundRaiseEventStatement)
            If asRaiseEvent IsNot Nothing Then
                Return asRaiseEvent.EventInvocation
            End If

            Return node
        End Function

        ''' <summary>
        ''' Return True if the statement can be bound by a Binder on its own.
        ''' For example Catch statement cannot be bound on its own, only
        ''' as part of Try block. Similarly, Next statement cannot be bound on its own,
        ''' only as part of For statement.
        '''
        ''' Only handles statements that are in executable code.
        ''' </summary>
        Private Shared Function IsStandaloneStatement(node As StatementSyntax) As Boolean
            Select Case node.Kind
                Case SyntaxKind.EmptyStatement,
                     SyntaxKind.SimpleAssignmentStatement,
                     SyntaxKind.AddAssignmentStatement,
                     SyntaxKind.SubtractAssignmentStatement,
                     SyntaxKind.MultiplyAssignmentStatement,
                     SyntaxKind.DivideAssignmentStatement,
                     SyntaxKind.IntegerDivideAssignmentStatement,
                     SyntaxKind.ExponentiateAssignmentStatement,
                     SyntaxKind.LeftShiftAssignmentStatement,
                     SyntaxKind.RightShiftAssignmentStatement,
                     SyntaxKind.ConcatenateAssignmentStatement,
                     SyntaxKind.CallStatement,
                     SyntaxKind.GoToStatement,
                     SyntaxKind.LabelStatement,
                     SyntaxKind.SingleLineIfStatement,
                     SyntaxKind.MidAssignmentStatement,
                     SyntaxKind.MultiLineIfBlock,
                     SyntaxKind.SelectBlock,
                     SyntaxKind.UsingBlock,
                     SyntaxKind.SyncLockBlock,
                     SyntaxKind.LocalDeclarationStatement,
                     SyntaxKind.SimpleDoLoopBlock,
                     SyntaxKind.DoWhileLoopBlock,
                     SyntaxKind.DoUntilLoopBlock,
                     SyntaxKind.DoLoopWhileBlock,
                     SyntaxKind.DoLoopUntilBlock,
                     SyntaxKind.WhileBlock,
                     SyntaxKind.ForBlock,
                     SyntaxKind.ForEachBlock,
                     SyntaxKind.TryBlock,
                     SyntaxKind.WithBlock,
                     SyntaxKind.ExitDoStatement,
                     SyntaxKind.ExitForStatement,
                     SyntaxKind.ExitSelectStatement,
                     SyntaxKind.ExitTryStatement,
                     SyntaxKind.ExitWhileStatement,
                     SyntaxKind.ExitFunctionStatement,
                     SyntaxKind.ExitSubStatement,
                     SyntaxKind.ExitOperatorStatement,
                     SyntaxKind.ExitPropertyStatement,
                     SyntaxKind.ContinueDoStatement,
                     SyntaxKind.ContinueForStatement,
                     SyntaxKind.ContinueWhileStatement,
                     SyntaxKind.ReturnStatement,
                     SyntaxKind.ThrowStatement,
                     SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.ConstructorBlock,
                     SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.OperatorBlock,
                     SyntaxKind.AddHandlerAccessorBlock, SyntaxKind.RemoveHandlerAccessorBlock, SyntaxKind.RaiseEventAccessorBlock,
                     SyntaxKind.ReDimStatement,
                     SyntaxKind.ReDimPreserveStatement,
                     SyntaxKind.EraseStatement,
                     SyntaxKind.ErrorStatement,
                     SyntaxKind.OnErrorGoToZeroStatement,
                     SyntaxKind.OnErrorGoToMinusOneStatement,
                     SyntaxKind.OnErrorGoToLabelStatement,
                     SyntaxKind.OnErrorResumeNextStatement,
                     SyntaxKind.ResumeStatement,
                     SyntaxKind.ResumeLabelStatement,
                     SyntaxKind.ResumeNextStatement,
                     SyntaxKind.EndStatement,
                     SyntaxKind.StopStatement,
                     SyntaxKind.AddHandlerStatement,
                     SyntaxKind.RemoveHandlerStatement,
                     SyntaxKind.RaiseEventStatement,
                     SyntaxKind.ExpressionStatement,
                     SyntaxKind.YieldStatement,
                     SyntaxKind.PrintStatement,
                     SyntaxKind.OptionStatement
                    Return True

                Case SyntaxKind.IfStatement,
                     SyntaxKind.ElseStatement,
                     SyntaxKind.ElseIfStatement,
                     SyntaxKind.EndIfStatement,
                     SyntaxKind.WithStatement,
                     SyntaxKind.EndWithStatement,
                     SyntaxKind.SelectStatement,
                     SyntaxKind.CaseElseStatement,
                     SyntaxKind.CaseStatement,
                     SyntaxKind.EndSelectStatement,
                     SyntaxKind.EndSubStatement,
                     SyntaxKind.EndFunctionStatement,
                     SyntaxKind.EndOperatorStatement,
                     SyntaxKind.WhileStatement,
                     SyntaxKind.EndWhileStatement,
                     SyntaxKind.TryStatement,
                     SyntaxKind.CatchStatement,
                     SyntaxKind.FinallyStatement,
                     SyntaxKind.EndTryStatement,
                     SyntaxKind.SyncLockStatement,
                     SyntaxKind.EndSyncLockStatement,
                     SyntaxKind.ForStatement,
                     SyntaxKind.ForEachStatement,
                     SyntaxKind.NextStatement,
                     SyntaxKind.SimpleDoStatement, SyntaxKind.DoWhileStatement, SyntaxKind.DoUntilStatement,
                     SyntaxKind.SimpleLoopStatement, SyntaxKind.LoopWhileStatement, SyntaxKind.LoopUntilStatement,
                     SyntaxKind.UsingStatement,
                     SyntaxKind.EndUsingStatement,
                     SyntaxKind.SubLambdaHeader,
                     SyntaxKind.FunctionLambdaHeader,
                     SyntaxKind.SubStatement, SyntaxKind.FunctionStatement,
                     SyntaxKind.FieldDeclaration,
                     SyntaxKind.SubNewStatement,
                     SyntaxKind.DeclareSubStatement, SyntaxKind.DeclareFunctionStatement,
                     SyntaxKind.DelegateFunctionStatement, SyntaxKind.DelegateSubStatement,
                     SyntaxKind.EventStatement,
                     SyntaxKind.OperatorStatement,
                     SyntaxKind.PropertyStatement,
                     SyntaxKind.GetAccessorStatement, SyntaxKind.SetAccessorStatement,
                     SyntaxKind.AddHandlerAccessorStatement, SyntaxKind.RemoveHandlerAccessorStatement, SyntaxKind.RaiseEventAccessorStatement,
                     SyntaxKind.EndNamespaceStatement,
                     SyntaxKind.EndModuleStatement,
                     SyntaxKind.EndClassStatement,
                     SyntaxKind.EndStructureStatement,
                     SyntaxKind.EndInterfaceStatement,
                     SyntaxKind.EndEnumStatement,
                     SyntaxKind.EndSubStatement,
                     SyntaxKind.EndFunctionStatement,
                     SyntaxKind.EndOperatorStatement,
                     SyntaxKind.EndPropertyStatement,
                     SyntaxKind.EndGetStatement,
                     SyntaxKind.EndSetStatement,
                     SyntaxKind.EndEventStatement,
                     SyntaxKind.EndAddHandlerStatement,
                     SyntaxKind.EndRemoveHandlerStatement,
                     SyntaxKind.EndRaiseEventStatement,
                     SyntaxKind.IncompleteMember,
                     SyntaxKind.InheritsStatement,
                     SyntaxKind.ImplementsStatement,
                     SyntaxKind.ImportsStatement,
                     SyntaxKind.EnumMemberDeclaration
                    Return False

                Case Else
                    ' Unexpected statement kind; add to either stand-alone or non-standalone list.
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind)
            End Select
            Return True
        End Function

        ''' <summary>
        ''' Get all of the syntax errors within the syntax tree associated with this
        ''' object. Does not get errors involving declarations or compiling method bodies or initializers.
        ''' </summary>
        ''' <param name="span">Optional span within the syntax tree for which to get diagnostics.
        ''' If no argument is specified, then diagnostics for the entire tree are returned.</param>
        ''' <param name="cancellationToken">A cancellation token that can be used to cancel the
        ''' process of obtaining the diagnostics.</param>
        Public NotOverridable Overrides Function GetSyntaxDiagnostics(Optional span As TextSpan? = Nothing, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            Throw New NotSupportedException()
        End Function

        ''' <summary>
        ''' Get all the syntax and declaration errors within the syntax tree associated with this object. Does not get
        ''' errors involving compiling method bodies or initializers.
        ''' </summary>
        ''' <param name="span">Optional span within the syntax tree for which to get diagnostics.
        ''' If no argument is specified, then diagnostics for the entire tree are returned.</param>
        ''' <param name="cancellationToken">A cancellation token that can be used to cancel the process of obtaining the
        ''' diagnostics.</param>
        ''' <remarks>The declaration errors for a syntax tree are cached. The first time this method is called, a ll
        ''' declarations are analyzed for diagnostics. Calling this a second time will return the cached diagnostics.
        ''' </remarks>
        Public NotOverridable Overrides Function GetDeclarationDiagnostics(Optional span As TextSpan? = Nothing, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            Throw New NotSupportedException()
        End Function

        ''' <summary>
        ''' Get all the syntax and declaration errors within the syntax tree associated with this object. Does not get
        ''' errors involving compiling method bodies or initializers.
        ''' </summary>
        ''' <param name="span">Optional span within the syntax tree for which to get diagnostics.
        ''' If no argument is specified, then diagnostics for the entire tree are returned.</param>
        ''' <param name="cancellationToken">A cancellation token that can be used to cancel the process of obtaining the
        ''' diagnostics.</param>
        ''' <remarks>The declaration errors for a syntax tree are cached. The first time this method is called, a ll
        ''' declarations are analyzed for diagnostics. Calling this a second time will return the cached diagnostics.
        ''' </remarks>
        Public NotOverridable Overrides Function GetMethodBodyDiagnostics(Optional span As TextSpan? = Nothing, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            Throw New NotSupportedException()
        End Function

        ''' <summary>
        ''' Get all the errors within the syntax tree associated with this object. Includes errors involving compiling
        ''' method bodies or initializers, in addition to the errors returned by GetDeclarationDiagnostics.
        ''' </summary>
        ''' <param name="span">Optional span within the syntax tree for which to get diagnostics.
        ''' If no argument is specified, then diagnostics for the entire tree are returned.</param>
        ''' <param name="cancellationToken">A cancellation token that can be used to cancel the process of obtaining the
        ''' diagnostics.</param>
        ''' <remarks>
        ''' Because this method must semantically all method bodies and initializers to check for diagnostics, it may
        ''' take a significant amount of time. Unlike GetDeclarationDiagnostics, diagnostics for method bodies and
        ''' initializers are not cached, the any semantic information used to obtain the diagnostics is discarded.
        ''' </remarks>
        Public NotOverridable Overrides Function GetDiagnostics(Optional span As TextSpan? = Nothing, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            Throw New NotSupportedException()
        End Function

        ''' <summary>
        ''' Given a type declaration, get the corresponding type symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares a type.</param>
        ''' <returns>The type symbol that was declared.</returns>
        Public Overloads Overrides Function GetDeclaredSymbol(declarationSyntax As TypeStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As INamedTypeSymbol
            ' Can't define type inside member
            Return Nothing
        End Function

        ''' <summary>
        ''' Given a enum declaration, get the corresponding type symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares an enum.</param>
        ''' <returns>The type symbol that was declared.</returns>
        Public Overloads Overrides Function GetDeclaredSymbol(declarationSyntax As EnumStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As INamedTypeSymbol
            ' Can't define enum inside member
            Return Nothing
        End Function

        ''' <summary>
        ''' Given a namespace declaration, get the corresponding type symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares a namespace.</param>
        ''' <returns>The namespace symbol that was declared.</returns>
        Public Overloads Overrides Function GetDeclaredSymbol(declarationSyntax As NamespaceStatementSyntax, Optional cancellationToken As CancellationToken = Nothing) As INamespaceSymbol
            ' Can't define namespace inside member
            Return Nothing
        End Function

        ''' <summary>
        ''' Given a method, property, or event declaration, get the corresponding symbol.
        ''' </summary>
        ''' <param name="declarationSyntax">The syntax node that declares a method, property, or event.</param>
        ''' <returns>The method, property, or event symbol that was declared.</returns>
        Friend Overloads Overrides Function GetDeclaredSymbol(declarationSyntax As MethodBaseSyntax, Optional cancellationToken As CancellationToken = Nothing) As ISymbol
            ' Can't define method inside member
            Return Nothing
        End Function

        ''' <summary>
        ''' Given a parameter declaration, get the corresponding parameter symbol.
        ''' </summary>
        ''' <param name="parameter">The syntax node that declares a parameter.</param>
        ''' <returns>The parameter symbol that was declared.</returns>
        Public Overloads Overrides Function GetDeclaredSymbol(parameter As ParameterSyntax, Optional cancellationToken As CancellationToken = Nothing) As IParameterSymbol
            ' This could be a lambda parameter.
            Dim parent As VisualBasicSyntaxNode = parameter.Parent

            Dim paramList As ParameterListSyntax = TryCast(parameter.Parent, ParameterListSyntax)
            If parent IsNot Nothing AndAlso parent.Kind = SyntaxKind.ParameterList Then
                Dim lambdaHeader = TryCast(parent.Parent, LambdaHeaderSyntax)

                If lambdaHeader IsNot Nothing Then
                    Dim lambdaSyntax = TryCast(lambdaHeader.Parent, LambdaExpressionSyntax)

                    If lambdaSyntax IsNot Nothing Then
                        ' We should always be able to get at least an error binding for a lambda, so assert
                        ' if this isn't true.

                        Dim boundlambda = TryCast(GetLowerBoundNode(lambdaSyntax), BoundLambda)
                        Debug.Assert(boundlambda IsNot Nothing)

                        If boundlambda IsNot Nothing Then
                            For Each symbol In boundlambda.LambdaSymbol.Parameters
                                For Each location In symbol.Locations
                                    If parameter.Span.Contains(location.SourceSpan) Then
                                        Return symbol
                                    End If
                                Next
                            Next
                        End If
                    End If
                End If
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Given an import clause get the corresponding symbol for the import alias that was introduced.
        ''' </summary>
        ''' <param name="declarationSyntax">The import statement syntax node.</param>
        ''' <returns>The alias symbol that was declared or Nothing if no alias symbol was declared.</returns>
        Public Overloads Overrides Function GetDeclaredSymbol(declarationSyntax As SimpleImportsClauseSyntax, Optional cancellationToken As CancellationToken = Nothing) As IAliasSymbol
            ' Can't define alias inside member
            Return Nothing
        End Function

        ''' <summary>
        ''' Given a type parameter declaration, get the corresponding type parameter symbol.
        ''' </summary>
        ''' <param name="typeParameter">The syntax node that declares a type parameter.</param>
        ''' <returns>The type parameter symbol that was declared.</returns>
        Public Overloads Overrides Function GetDeclaredSymbol(typeParameter As TypeParameterSyntax, Optional cancellationToken As CancellationToken = Nothing) As ITypeParameterSymbol
            ' Can't define type parameter inside member
            Return Nothing
        End Function

        Public Overrides Function GetDeclaredSymbol(declarationSyntax As EnumMemberDeclarationSyntax, Optional cancellationToken As CancellationToken = Nothing) As IFieldSymbol
            ' Can't define enum member inside member
            Return Nothing
        End Function

        Public Overrides Function GetDeclaredSymbol(identifierSyntax As ModifiedIdentifierSyntax, Optional cancellationToken As CancellationToken = Nothing) As ISymbol
            If identifierSyntax Is Nothing Then
                Throw New ArgumentNullException(NameOf(identifierSyntax))
            End If
            If Not IsInTree(identifierSyntax) Then
                Throw New ArgumentException(VBResources.IdentifierSyntaxNotWithinSyntaxTree)
            End If

            Dim parent As VisualBasicSyntaxNode = identifierSyntax.Parent

            If parent IsNot Nothing Then
                Select Case parent.Kind
                    Case SyntaxKind.CollectionRangeVariable

                        Return GetDeclaredSymbol(DirectCast(parent, CollectionRangeVariableSyntax), cancellationToken)

                    Case SyntaxKind.VariableNameEquals
                        parent = parent.Parent

                        If parent IsNot Nothing Then
                            Select Case parent.Kind
                                Case SyntaxKind.ExpressionRangeVariable
                                    Return GetDeclaredSymbol(DirectCast(parent, ExpressionRangeVariableSyntax), cancellationToken)

                                Case SyntaxKind.AggregationRangeVariable
                                    Return GetDeclaredSymbol(DirectCast(parent, AggregationRangeVariableSyntax), cancellationToken)
                            End Select
                        End If
                End Select
            End If

            Return MyBase.GetDeclaredSymbol(identifierSyntax, cancellationToken)
        End Function

        Public Overrides Function GetDeclaredSymbol(anonymousObjectCreationExpressionSyntax As AnonymousObjectCreationExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As INamedTypeSymbol
            If anonymousObjectCreationExpressionSyntax Is Nothing Then
                Throw New ArgumentNullException(NameOf(anonymousObjectCreationExpressionSyntax))
            End If
            If Not IsInTree(anonymousObjectCreationExpressionSyntax) Then
                Throw New ArgumentException(VBResources.AnonymousObjectCreationExpressionSyntaxNotWithinTree)
            End If

            Dim boundExpression = TryCast(GetLowerBoundNode(anonymousObjectCreationExpressionSyntax), BoundExpression)
            If boundExpression Is Nothing Then
                Return Nothing
            End If

            Return TryCast(boundExpression.Type, AnonymousTypeManager.AnonymousTypePublicSymbol)
        End Function

        Public Overrides Function GetDeclaredSymbol(fieldInitializerSyntax As FieldInitializerSyntax, Optional cancellationToken As System.Threading.CancellationToken = Nothing) As IPropertySymbol
            If fieldInitializerSyntax Is Nothing Then
                Throw New ArgumentNullException(NameOf(fieldInitializerSyntax))
            End If
            If Not IsInTree(fieldInitializerSyntax) Then
                Throw New ArgumentException(VBResources.FieldInitializerSyntaxNotWithinSyntaxTree)
            End If

            Dim parentInitializer = TryCast(fieldInitializerSyntax.Parent, ObjectMemberInitializerSyntax)
            If parentInitializer Is Nothing Then
                Return Nothing
            End If

            Dim anonymousObjectCreation = TryCast(parentInitializer.Parent, AnonymousObjectCreationExpressionSyntax)
            If anonymousObjectCreation Is Nothing Then
                Return Nothing
            End If

            Dim boundExpression = TryCast(GetLowerBoundNode(anonymousObjectCreation), BoundExpression)
            If boundExpression Is Nothing Then
                Return Nothing
            End If

            Dim anonymousType = TryCast(boundExpression.Type, AnonymousTypeManager.AnonymousTypePublicSymbol)
            If anonymousType Is Nothing Then
                Return Nothing
            End If

            Dim index = parentInitializer.Initializers.IndexOf(fieldInitializerSyntax)
            Debug.Assert(index >= 0)
            Debug.Assert(index < parentInitializer.Initializers.Count)
            Debug.Assert(index < anonymousType.Properties.Length)
            Return anonymousType.Properties(index)
        End Function

        Public Overrides Function GetDeclaredSymbol(rangeVariableSyntax As CollectionRangeVariableSyntax, Optional cancellationToken As CancellationToken = Nothing) As IRangeVariableSymbol
            If rangeVariableSyntax Is Nothing Then
                Throw New ArgumentNullException(NameOf(rangeVariableSyntax))
            End If
            If Not IsInTree(rangeVariableSyntax) Then
                Throw New ArgumentException(VBResources.IdentifierSyntaxNotWithinSyntaxTree)
            End If

            Dim bound As BoundNode = GetLowerBoundNode(rangeVariableSyntax)

            If bound IsNot Nothing AndAlso bound.Kind = BoundKind.QueryableSource Then
                Dim queryableSource = DirectCast(bound, BoundQueryableSource)

                If queryableSource.RangeVariableOpt IsNot Nothing Then
                    Return queryableSource.RangeVariableOpt
                End If
            End If

            Return MyBase.GetDeclaredSymbol(rangeVariableSyntax, cancellationToken)
        End Function

        Public Overrides Function GetDeclaredSymbol(rangeVariableSyntax As ExpressionRangeVariableSyntax, Optional cancellationToken As CancellationToken = Nothing) As IRangeVariableSymbol
            If rangeVariableSyntax Is Nothing Then
                Throw New ArgumentNullException(NameOf(rangeVariableSyntax))
            End If
            If Not IsInTree(rangeVariableSyntax) Then
                Throw New ArgumentException(VBResources.IdentifierSyntaxNotWithinSyntaxTree)
            End If

            Dim bound As BoundNode = GetLowerBoundNode(rangeVariableSyntax)

            If bound IsNot Nothing AndAlso bound.Kind = BoundKind.RangeVariableAssignment Then
                Return DirectCast(bound, BoundRangeVariableAssignment).RangeVariable
            End If

            Return MyBase.GetDeclaredSymbol(rangeVariableSyntax, cancellationToken)
        End Function

        Public Overrides Function GetDeclaredSymbol(rangeVariableSyntax As AggregationRangeVariableSyntax, Optional cancellationToken As CancellationToken = Nothing) As IRangeVariableSymbol
            If rangeVariableSyntax Is Nothing Then
                Throw New ArgumentNullException(NameOf(rangeVariableSyntax))
            End If
            If Not IsInTree(rangeVariableSyntax) Then
                Throw New ArgumentException(VBResources.IdentifierSyntaxNotWithinSyntaxTree)
            End If

            Dim bound As BoundNode = GetLowerBoundNode(rangeVariableSyntax)

            If bound IsNot Nothing AndAlso bound.Kind = BoundKind.RangeVariableAssignment Then
                Return DirectCast(bound, BoundRangeVariableAssignment).RangeVariable
            End If

            Return MyBase.GetDeclaredSymbol(rangeVariableSyntax, cancellationToken)
        End Function

        Friend Overrides Function GetDeclaredSymbols(declarationSyntax As FieldDeclarationSyntax, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of ISymbol)
            ' Can't define field inside member
            Return ImmutableArray.Create(Of ISymbol)()
        End Function

        ''' <summary>
        ''' Gets the semantic information of a for each statement.
        ''' </summary>
        ''' <param name="node">The for each syntax node.</param>
        Friend Overrides Function GetForEachStatementInfoWorker(node As ForEachBlockSyntax) As ForEachStatementInfo
            Dim boundForEach = DirectCast(GetUpperBoundNode(node), BoundForEachStatement)

            If boundForEach IsNot Nothing Then
                Return GetForEachStatementInfo(boundForEach, Compilation,
                                               getEnumeratorArguments:=Nothing,
                                               getEnumeratorDefaultArguments:=Nothing,
                                               moveNextArguments:=Nothing,
                                               moveNextDefaultArguments:=Nothing,
                                               currentArguments:=Nothing,
                                               currentDefaultArguments:=Nothing)
            Else
                Return Nothing
            End If
        End Function

        Friend Overloads Shared Function GetForEachStatementInfo(
            boundForEach As BoundForEachStatement,
            compilation As VisualBasicCompilation,
            <Out> ByRef getEnumeratorArguments As ImmutableArray(Of BoundExpression),
            <Out> ByRef getEnumeratorDefaultArguments As BitVector,
            <Out> ByRef moveNextArguments As ImmutableArray(Of BoundExpression),
            <Out> ByRef moveNextDefaultArguments As BitVector,
            <Out> ByRef currentArguments As ImmutableArray(Of BoundExpression),
            <Out> ByRef currentDefaultArguments As BitVector
        ) As ForEachStatementInfo
            getEnumeratorArguments = Nothing
            moveNextArguments = Nothing
            currentArguments = Nothing

            Dim enumeratorInfo = boundForEach.EnumeratorInfo

            Dim getEnumerator As MethodSymbol = Nothing
            If enumeratorInfo.GetEnumerator IsNot Nothing AndAlso enumeratorInfo.GetEnumerator.Kind = BoundKind.Call Then
                Dim getEnumeratorCall As BoundCall = DirectCast(enumeratorInfo.GetEnumerator, BoundCall)
                getEnumerator = getEnumeratorCall.Method
                getEnumeratorArguments = getEnumeratorCall.Arguments
                getEnumeratorDefaultArguments = getEnumeratorCall.DefaultArguments
            End If

            Dim moveNext As MethodSymbol = Nothing
            If enumeratorInfo.MoveNext IsNot Nothing AndAlso enumeratorInfo.MoveNext.Kind = BoundKind.Call Then
                Dim moveNextCall As BoundCall = DirectCast(enumeratorInfo.MoveNext, BoundCall)
                moveNext = moveNextCall.Method
                moveNextArguments = moveNextCall.Arguments
                moveNextDefaultArguments = moveNextCall.DefaultArguments
            End If

            Dim current As PropertySymbol = Nothing
            If enumeratorInfo.Current IsNot Nothing AndAlso enumeratorInfo.Current.Kind = BoundKind.PropertyAccess Then
                Dim currentProperty As BoundPropertyAccess = DirectCast(enumeratorInfo.Current, BoundPropertyAccess)
                current = currentProperty.PropertySymbol
                currentArguments = currentProperty.Arguments
                currentDefaultArguments = currentProperty.DefaultArguments
            End If

            ' The batch compiler doesn't actually use this conversion, so we'll just compute it here.
            ' It will usually be an identity conversion.
            Dim currentConversion As Conversion = Nothing
            Dim elementConversion As Conversion = Nothing
            Dim elementType As TypeSymbol = enumeratorInfo.ElementType

            If elementType IsNot Nothing AndAlso Not elementType.IsErrorType() Then
                If current IsNot Nothing AndAlso Not current.Type.IsErrorType() Then
                    currentConversion = New Conversion(Conversions.ClassifyConversion(current.Type, elementType, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded))
                End If

                Dim boundCurrentConversion As BoundExpression = enumeratorInfo.CurrentConversion
                If boundCurrentConversion IsNot Nothing AndAlso Not boundCurrentConversion.Type.IsErrorType() Then
                    ' NOTE: What VB calls the current conversion is used to convert the current placeholder to the iteration
                    ' variable type.  In the terminology of the public API, this is a conversion from the element type to the
                    ' iteration variable type, and is referred to as the element conversion.
                    elementConversion = New Conversion(Conversions.ClassifyConversion(elementType, boundCurrentConversion.Type, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded))
                End If
            End If

            Dim originalCollection As BoundExpression = boundForEach.Collection
            If originalCollection.Kind = BoundKind.Conversion Then
                Dim conversion = DirectCast(originalCollection, BoundConversion)
                If Not conversion.ExplicitCastInCode Then
                    originalCollection = conversion.Operand
                End If
            End If

            Return New ForEachStatementInfo(getEnumerator,
                                            moveNext,
                                            current,
                                            If(enumeratorInfo.NeedToDispose OrElse (originalCollection.Type IsNot Nothing AndAlso originalCollection.Type.IsArrayType()),
                                               DirectCast(compilation.GetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose), MethodSymbol),
                                               Nothing),
                                            elementType,
                                            elementConversion,
                                            currentConversion)
        End Function

        Friend Overrides Function GetAttributeSymbolInfo(attribute As AttributeSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            Return GetSymbolInfoForNode(SymbolInfoOptions.DefaultOptions, GetBoundNodeSummary(attribute), binderOpt:=Nothing)
        End Function

        Friend Overrides Function GetAttributeTypeInfo(attribute As AttributeSyntax, Optional cancellationToken As CancellationToken = Nothing) As VisualBasicTypeInfo
            Return GetTypeInfoForNode(GetBoundNodeSummary(attribute))
        End Function

        Friend Overrides Function GetAttributeMemberGroup(attribute As AttributeSyntax, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Symbol)
            Return GetMemberGroupForNode(GetBoundNodeSummary(attribute), binderOpt:=Nothing)
        End Function

        Friend Overrides Function GetExpressionSymbolInfo(node As ExpressionSyntax, options As SymbolInfoOptions, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            ValidateSymbolInfoOptions(options)

            If Me.IsSpeculativeSemanticModel Then
                ' For speculative model, we need to get the standalone expression when this is invoked via public GetSymbolInfo API
                node = SyntaxFactory.GetStandaloneExpression(node)
            End If

            If node.EnclosingStructuredTrivia IsNot Nothing Then
                Return SymbolInfo.None
            End If

            Return GetSymbolInfoForNode(options, GetBoundNodeSummary(node), binderOpt:=Nothing)
        End Function

        Friend Overrides Function GetOperationWorker(node As VisualBasicSyntaxNode, cancellationToken As CancellationToken) As IOperation

            Dim result As IOperation = Nothing
            Try
                _rwLock.EnterReadLock()

                If _guardedIOperationNodeMap.Count > 0 Then
                    Return If(_guardedIOperationNodeMap.TryGetValue(node, result), result, Nothing)
                End If
            Finally
                _rwLock.ExitReadLock()
            End Try

            Dim rootNode As BoundNode = GetBoundRoot()
            Dim rootOperation As IOperation = _operationFactory.Value.Create(rootNode)

            Try
                _rwLock.EnterWriteLock()

                If _guardedIOperationNodeMap.Count > 0 Then
                    Return If(_guardedIOperationNodeMap.TryGetValue(node, result), result, Nothing)
                End If

                Operation.SetParentOperation(rootOperation, Nothing)
                OperationMapBuilder.AddToMap(rootOperation, _guardedIOperationNodeMap)

                Return If(_guardedIOperationNodeMap.TryGetValue(node, result), result, Nothing)
            Finally
                _rwLock.ExitWriteLock()
            End Try
        End Function

        Friend Overrides Function GetExpressionTypeInfo(node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As VisualBasicTypeInfo
            If Me.IsSpeculativeSemanticModel Then
                ' For speculative model, we need to get the standalone expression when this is invoked via public GetTypeInfo API
                node = SyntaxFactory.GetStandaloneExpression(node)
            End If

            If node.EnclosingStructuredTrivia IsNot Nothing Then
                Return VisualBasicTypeInfo.None
            End If

            Return GetTypeInfoForNode(GetBoundNodeSummary(node))
        End Function

        Friend Overrides Function GetExpressionMemberGroup(node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Symbol)
            If Me.IsSpeculativeSemanticModel Then
                ' For speculative model, we need to get the standalone expression when this is invoked via public GetMemberGroup API
                node = SyntaxFactory.GetStandaloneExpression(node)
            End If

            If node.EnclosingStructuredTrivia IsNot Nothing Then
                Return ImmutableArray(Of Symbol).Empty
            End If

            Return GetMemberGroupForNode(GetBoundNodeSummary(node), binderOpt:=Nothing)
        End Function

        Friend Overrides Function GetExpressionConstantValue(node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As ConstantValue
            If Me.IsSpeculativeSemanticModel Then
                ' For speculative model, we need to get the standalone expression when this is invoked via public GetConstantValue API
                node = SyntaxFactory.GetStandaloneExpression(node)
            End If

            If node.EnclosingStructuredTrivia IsNot Nothing Then
                Return Nothing
            End If

            Return GetConstantValueForNode(GetBoundNodeSummary(node))
        End Function

        Friend Overrides Function GetCollectionInitializerAddSymbolInfo(collectionInitializer As ObjectCreationExpressionSyntax, node As ExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            Dim boundCollectionInitializer = TryCast(GetLowerBoundNode(collectionInitializer.Initializer), BoundCollectionInitializerExpression)

            If boundCollectionInitializer IsNot Nothing Then
                Dim boundAdd = boundCollectionInitializer.Initializers(DirectCast(collectionInitializer.Initializer, ObjectCollectionInitializerSyntax).Initializer.Initializers.IndexOf(node))

                If boundAdd.WasCompilerGenerated Then
                    Return GetSymbolInfoForNode(SymbolInfoOptions.DefaultOptions, New BoundNodeSummary(boundAdd, boundAdd, Nothing), binderOpt:=Nothing)
                End If
            End If

            Return SymbolInfo.None
        End Function

        Friend Overrides Function GetCrefReferenceSymbolInfo(crefReference As CrefReferenceSyntax, options As VBSemanticModel.SymbolInfoOptions, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            Return SymbolInfo.None
        End Function

        Friend Overrides Function GetQueryClauseSymbolInfo(node As QueryClauseSyntax, Optional cancellationToken As System.Threading.CancellationToken = Nothing) As SymbolInfo
            Dim nodeKind As SyntaxKind = node.Kind
            Debug.Assert(nodeKind <> SyntaxKind.LetClause AndAlso nodeKind <> SyntaxKind.OrderByClause AndAlso nodeKind <> SyntaxKind.AggregateClause)

            Dim bound As BoundNode

            If nodeKind = SyntaxKind.FromClause Then
                If DirectCast(node, FromClauseSyntax).Variables.Count < 2 AndAlso
                   node.Parent IsNot Nothing AndAlso node.Parent.Kind = SyntaxKind.QueryExpression Then
                    Dim query = DirectCast(node.Parent, QueryExpressionSyntax)

                    If query.Clauses.Count = 1 AndAlso query.Clauses(0) Is node Then
                        ' From needs an implicit Select call.
                        bound = GetLowerBoundNode(query)

                        Debug.Assert(bound Is Nothing OrElse bound.Kind = BoundKind.QueryExpression)
                        If bound IsNot Nothing AndAlso bound.Kind = BoundKind.QueryExpression Then
                            Dim boundQuery = DirectCast(bound, BoundQueryExpression)

                            If boundQuery.LastOperator.Syntax Is node Then
                                Debug.Assert(boundQuery.LastOperator.WasCompilerGenerated)
                                Return GetSymbolInfoForNode(SymbolInfoOptions.DefaultOptions,
                                                            New BoundNodeSummary(boundQuery.LastOperator, boundQuery.LastOperator, Nothing),
                                                            binderOpt:=Nothing)
                            End If
                        End If
                    End If
                End If

                Return SymbolInfo.None
            End If

            bound = GetLowerBoundNode(node)

            If bound IsNot Nothing AndAlso bound.Kind = BoundKind.QueryClause Then
                If nodeKind = SyntaxKind.SelectClause AndAlso
                   DirectCast(bound, BoundQueryClause).UnderlyingExpression.Kind = BoundKind.QueryClause Then
                    ' Select was absorbed by the previous operator.
                    Return SymbolInfo.None
                End If

                Return GetSymbolInfoForNode(SymbolInfoOptions.DefaultOptions, New BoundNodeSummary(bound, bound, Nothing), binderOpt:=Nothing)
            End If

            Return SymbolInfo.None
        End Function

        Friend Overrides Function GetLetClauseSymbolInfo(node As ExpressionRangeVariableSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            Debug.Assert(node.Parent IsNot Nothing AndAlso node.Parent.Kind = SyntaxKind.LetClause)

            Dim bound As BoundNode = GetUpperBoundNode(node)

            If bound IsNot Nothing AndAlso bound.Kind = BoundKind.QueryClause Then
                If DirectCast(bound, BoundQueryClause).UnderlyingExpression.Kind = BoundKind.QueryClause Then
                    ' Let was absorbed by the previous operator.
                    Return SymbolInfo.None
                End If

                Return GetSymbolInfoForNode(SymbolInfoOptions.DefaultOptions, New BoundNodeSummary(bound, bound, Nothing), binderOpt:=Nothing)
            End If

            Return SymbolInfo.None
        End Function

        Friend Overrides Function GetOrderingSymbolInfo(node As OrderingSyntax, Optional cancellationToken As CancellationToken = Nothing) As SymbolInfo
            Dim bound As BoundNode = GetLowerBoundNode(node)

            If bound IsNot Nothing AndAlso bound.Kind = BoundKind.Ordering Then
                Return GetSymbolInfoForNode(SymbolInfoOptions.DefaultOptions, New BoundNodeSummary(bound, bound, Nothing), binderOpt:=Nothing)
            End If

            Return SymbolInfo.None
        End Function

        Friend Overrides Function GetAggregateClauseSymbolInfoWorker(node As AggregateClauseSyntax, Optional cancellationToken As CancellationToken = Nothing) As AggregateClauseSymbolInfo
            Dim bound As BoundNode = GetLowerBoundNode(node)

            If bound IsNot Nothing AndAlso bound.Kind = BoundKind.AggregateClause Then
                Dim aggregateClause = DirectCast(bound, BoundAggregateClause)

                If TypeOf aggregateClause.UnderlyingExpression Is BoundQueryClauseBase Then
                    ' The Aggregate clause was completely absorbed by the previous join.
                    Return New AggregateClauseSymbolInfo(SymbolInfo.None, SymbolInfo.None)
                End If

                Dim select2 As SymbolInfo = GetSymbolInfoForNode(SymbolInfoOptions.DefaultOptions, New BoundNodeSummary(bound, bound, Nothing), binderOpt:=Nothing)

                ' Now let's check if there is another Select call preceding this one.
                Dim select1Node = DirectCast(CompilerGeneratedNodeFinder.FindIn(bound, node, BoundKind.QueryClause), BoundQueryClause)

                If select1Node IsNot Nothing Then

                    If TypeOf select1Node.UnderlyingExpression Is BoundQueryClauseBase Then
                        ' The first Select was absorbed by the previous join.
                        Return New AggregateClauseSymbolInfo(SymbolInfo.None, select2)
                    End If

                    Return New AggregateClauseSymbolInfo(GetSymbolInfoForNode(SymbolInfoOptions.DefaultOptions, New BoundNodeSummary(select1Node, select1Node, Nothing), binderOpt:=Nothing),
                                                         select2)
                End If

                Return New AggregateClauseSymbolInfo(select2)
            End If

            Return New AggregateClauseSymbolInfo(SymbolInfo.None, SymbolInfo.None)
        End Function

        Friend Overrides Function GetCollectionRangeVariableSymbolInfoWorker(node As CollectionRangeVariableSyntax, Optional cancellationToken As CancellationToken = Nothing) As CollectionRangeVariableSymbolInfo
            ' Find the upper most bound node that is a QueryClause or QueryableSource
            Dim boundNodes As ImmutableArray(Of BoundNode) = GetBoundNodes(node)
            Dim bound As BoundNode = Nothing
            For i = 0 To boundNodes.Length - 1
                If boundNodes(i).Kind = BoundKind.QueryClause OrElse boundNodes(i).Kind = BoundKind.QueryableSource Then
                    bound = boundNodes(i)
                    Exit For
                End If
            Next

            If bound Is Nothing Then
                Return CollectionRangeVariableSymbolInfo.None
            End If

            Dim toQueryableCollectionConversion As SymbolInfo = SymbolInfo.None
            Dim asClauseConversion As SymbolInfo = SymbolInfo.None
            Dim selectMany As SymbolInfo = SymbolInfo.None

            If bound.Kind = BoundKind.QueryClause Then
                ' This must be SelectMany operator.
                selectMany = GetSymbolInfoForNode(SymbolInfoOptions.DefaultOptions, New BoundNodeSummary(bound, bound, Nothing), binderOpt:=Nothing)

                ' Look for queryable source.
                bound = GetLowerBoundNode(node)
            End If

            If bound IsNot Nothing AndAlso bound.Kind = BoundKind.QueryableSource Then
                Dim queryableSource = DirectCast(bound, BoundQueryableSource)

                Select Case queryableSource.Source.Kind
                    Case BoundKind.QueryClause
                        ' This must be an implicit Select for an AsClause conversion.
                        asClauseConversion = GetSymbolInfoForNode(SymbolInfoOptions.DefaultOptions, New BoundNodeSummary(queryableSource.Source, queryableSource.Source, Nothing), binderOpt:=Nothing)

                        ' See if there is also ToQueryableSource conversion.
                        Dim toQueryable = DirectCast(CompilerGeneratedNodeFinder.FindIn(DirectCast(queryableSource.Source, BoundQueryClause).UnderlyingExpression,
                                                                                        node.Expression, BoundKind.ToQueryableCollectionConversion),
                                                     BoundToQueryableCollectionConversion)

                        If toQueryable Is Nothing Then
                            toQueryableCollectionConversion = SymbolInfo.None
                        Else
                            toQueryableCollectionConversion = GetSymbolInfoForNode(SymbolInfoOptions.DefaultOptions, New BoundNodeSummary(toQueryable, toQueryable, Nothing), binderOpt:=Nothing)
                        End If

                    Case BoundKind.ToQueryableCollectionConversion
                        asClauseConversion = SymbolInfo.None
                        toQueryableCollectionConversion = GetSymbolInfoForNode(SymbolInfoOptions.DefaultOptions, New BoundNodeSummary(queryableSource.Source, queryableSource.Source, Nothing), binderOpt:=Nothing)

                    Case BoundKind.QuerySource
                        asClauseConversion = SymbolInfo.None
                        toQueryableCollectionConversion = SymbolInfo.None

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(queryableSource.Source.Kind)
                End Select
            Else
                ' Failed to locate corresponding QueryableSource, something went wrong.
                selectMany = SymbolInfo.None
            End If

            Return New CollectionRangeVariableSymbolInfo(toQueryableCollectionConversion, asClauseConversion, selectMany)
        End Function

        Friend NotOverridable Overrides Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, type As TypeSyntax, bindingOption As SpeculativeBindingOption, <Out> ByRef speculativeModel As PublicSemanticModel) As Boolean
            Dim binder As Binder = Me.GetSpeculativeBinderForExpression(position, type, bindingOption)
            If binder Is Nothing Then
                speculativeModel = Nothing
                Return False
            End If

            speculativeModel = New SpeculativeSemanticModelWithMemberModel(parentModel, position, type, binder)
            Return True
        End Function

        Friend NotOverridable Overrides Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, rangeArgument As RangeArgumentSyntax, <Out> ByRef speculativeModel As PublicSemanticModel) As Boolean
            Dim binder = Me.GetEnclosingBinder(position)
            If binder Is Nothing Then
                speculativeModel = Nothing
                Return False
            End If

            ' Add speculative binder to bind speculatively.
            binder = SpeculativeBinder.Create(binder)

            speculativeModel = New SpeculativeSemanticModelWithMemberModel(parentModel, position, rangeArgument, binder)
            Return True
        End Function

        Friend Sub CacheBoundNodes(boundNode As BoundNode, Optional thisSyntaxNodeOnly As SyntaxNode = Nothing)
            _rwLock.EnterWriteLock()
            Try
                SemanticModelMapsBuilder.GuardedCacheBoundNodes(boundNode, Me, Me._guardedBoundNodeMap, thisSyntaxNodeOnly)
            Finally
                _rwLock.ExitWriteLock()
            End Try
        End Sub

        Private Class CompilerGeneratedNodeFinder
            Inherits BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator

            Private ReadOnly _targetSyntax As VisualBasicSyntaxNode
            Private ReadOnly _targetBoundKind As BoundKind
            Private _found As BoundNode

            Private Sub New(targetSyntax As VisualBasicSyntaxNode, targetBoundKind As BoundKind)
                _targetSyntax = targetSyntax
                _targetBoundKind = targetBoundKind
            End Sub

            Public Shared Function FindIn(context As BoundNode, targetSyntax As VisualBasicSyntaxNode, targetBoundKind As BoundKind) As BoundNode
                Debug.Assert(targetBoundKind <> BoundKind.BinaryOperator) ' Otherwise VisitBinaryOperator should be adjusted

                Dim finder As New CompilerGeneratedNodeFinder(targetSyntax, targetBoundKind)
                finder.Visit(context)
                Return finder._found
            End Function

            Public Overrides Function Visit(node As BoundNode) As BoundNode
                If node Is Nothing OrElse _found IsNot Nothing Then
                    Return Nothing
                End If

                If node.WasCompilerGenerated AndAlso
                   node.Syntax Is _targetSyntax AndAlso
                   node.Kind = _targetBoundKind Then

                    _found = node
                    Return Nothing
                End If

                Return MyBase.Visit(node)
            End Function

            Protected Overrides Function ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException() As Boolean
                Return False
            End Function
        End Class

        Public Overrides ReadOnly Property Compilation As VisualBasicCompilation
            Get
                Return RootBinder.Compilation
            End Get
        End Property

        Friend ReadOnly Property MemberSymbol As Symbol
            Get
                Return RootBinder.ContainingMember
            End Get
        End Property

        ''' <summary>
        ''' The SyntaxTree that is bound
        ''' </summary>
        Public Overrides ReadOnly Property SyntaxTree As SyntaxTree
            Get
                Return Root.SyntaxTree
            End Get
        End Property

        Private ReadOnly _rwLock As ReaderWriterLockSlim = New ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion)

        '' This class manages a cache of bound nodes and binders for all the executable code under the root SyntaxNode
        '' of this SemanticModel.
        ''
        '' The basic strategy is that a mapping from SyntaxNode -> ImmutableArray(Of BoundNode) is maintained, where
        '' the bound nodes are in top-down order. If we need to find the bound nodes associated with a syntax node, we
        '' first check the cache. If its not there, then we bind the enclosing statement which is NOT inside a lambda
        '' (statements inside lambda may not have type information inferred for them). We then do a walk over the resulting
        '' bound statement, placing all bound nodes into the mapping. We also place binders for lambda and queries into a
        '' map, so that we can answer GetEnclosingBinder questions.

        ' The bound nodes associated with syntaxnode, from highest in the tree to lowest.
        Private ReadOnly _guardedBoundNodeMap As New SmallDictionary(Of SyntaxNode, ImmutableArray(Of BoundNode))(ReferenceEqualityComparer.Instance)
        Private ReadOnly _guardedIOperationNodeMap As New Dictionary(Of SyntaxNode, IOperation)

        Private ReadOnly _guardedQueryBindersMap As New Dictionary(Of SyntaxNode, ImmutableArray(Of Binder))()
        Private ReadOnly _guardedAnonymousTypeBinderMap As New Dictionary(Of FieldInitializerSyntax, Binder.AnonymousTypeFieldInitializerBinder)()

        ' If implicit variable declaration is in play, then we must bind everything
        ' up front in order to get all implicit local variables declared.
        ' Because order of declaration is important, and any expression could declare
        ' an implicit local, we have to bind the whole method body from start to finish.
        Private Sub EnsureFullyBoundIfImplicitVariablesAllowed()
            If Me.RootBinder.ImplicitVariableDeclarationAllowed AndAlso Not Me.RootBinder.AllImplicitVariableDeclarationsAreHandled Then
                _rwLock.EnterWriteLock()
                Try
                    ' To prevent races, we must check again under the lock.
                    If Not Me.RootBinder.AllImplicitVariableDeclarationsAreHandled Then
                        ' bind everything, so all implicit variables are declared, and processed in the right order.
                        Me.GuardedIncrementalBind(Me.Root, Me.RootBinder)

                        ' after this call, RootBinder.AllImplicitVariableDeclarationsAreHandled = True
                        Me.RootBinder.DisallowFurtherImplicitVariableDeclaration(BindingDiagnosticBag.Discarded)
                    End If
                Finally
                    _rwLock.ExitWriteLock()
                End Try
            End If
        End Sub

        Private Function GuardedGetBoundNodesFromMap(node As SyntaxNode) As ImmutableArray(Of BoundNode)
            Debug.Assert(_rwLock.IsReadLockHeld OrElse _rwLock.IsWriteLockHeld)
            Dim result As ImmutableArray(Of BoundNode) = Nothing
            Return If(Me._guardedBoundNodeMap.TryGetValue(node, result), result, Nothing)
        End Function

        ''' <summary>
        ''' Get the correct enclosing binder for the given position, taking into account
        ''' block constructs and lambdas.
        ''' </summary>
        ''' <param name="memberBinder">Binder for the method body, lambda body, or field initializer. The
        ''' returned binder will be nested inside the binder, or be this binder.</param>
        ''' <param name="binderRoot">Syntax node that is the root of the construct associated with "memberBinder".</param>
        ''' <param name="node">Syntax node that position is in.</param>
        ''' <param name="position">Position we are finding the enclosing binder for.</param>
        ''' <returns>The enclosing binder within "memberBinder" for the given position.</returns>
        ''' <remarks>
        ''' WARN WARN WARN: The result is not guaranteed to have IsSemanticModelBinder set.
        ''' </remarks>
        Private Function GetEnclosingBinderInternal(
            memberBinder As Binder,
            binderRoot As SyntaxNode,
            node As SyntaxNode,
            position As Integer
        ) As Binder
            Dim binder As Binder = Nothing

            EnsureFullyBoundIfImplicitVariablesAllowed()

            Dim current As SyntaxNode = node
            Do
                Dim body As SyntaxList(Of StatementSyntax) = Nothing

                If current.Kind = SyntaxKind.DocumentationCommentTrivia Then
                    Dim trivia As SyntaxTrivia = DirectCast(current, DocumentationCommentTriviaSyntax).ParentTrivia
                    Debug.Assert(trivia.Kind <> SyntaxKind.None)
                    Debug.Assert(trivia.Token.Kind <> SyntaxKind.None)
                    Return GetEnclosingBinderInternal(memberBinder, binderRoot, DirectCast(trivia.Token.Parent, VisualBasicSyntaxNode), position)

                ElseIf SyntaxFacts.InBlockInterior(current, position, body) Then
                    ' We are in the interior of a block statement.
                    binder = memberBinder.GetBinder(body)
                    If binder IsNot Nothing Then
                        Return binder
                    End If

                ElseIf SyntaxFacts.InLambdaInterior(current, position) Then
                    If current IsNot binderRoot Then
                        Dim lambdaBinder As LambdaBodyBinder =
                                            Me.GetLambdaBodyBinder(DirectCast(current, LambdaExpressionSyntax))

                        If lambdaBinder IsNot Nothing Then
                            Debug.Assert(lambdaBinder.Root Is current)

                            If current.Kind = SyntaxKind.MultiLineFunctionLambdaExpression OrElse current.Kind = SyntaxKind.MultiLineSubLambdaExpression Then
                                Dim multiLineLambda = DirectCast(current, MultiLineLambdaExpressionSyntax)

                                If multiLineLambda.SubOrFunctionHeader.FullSpan.Contains(position) Then
                                    Return lambdaBinder
                                End If
                            End If

                            binder = GetEnclosingBinderInternal(lambdaBinder, lambdaBinder.Root, node, position)

                            If binder IsNot Nothing Then
                                Return binder
                            End If
                        End If

                    ElseIf current.Kind = SyntaxKind.MultiLineFunctionLambdaExpression OrElse current.Kind = SyntaxKind.MultiLineSubLambdaExpression Then
                        ' We reached the lambda node, get binder for the whole body.
                        binder = memberBinder.GetBinder(DirectCast(current, MultiLineLambdaExpressionSyntax).Statements)
                        If binder IsNot Nothing Then
                            Return binder
                        End If
                    ElseIf current.Kind = SyntaxKind.SingleLineSubLambdaExpression Then
                        ' Even though single line sub lambdas only have a single statement.  Get a binder for
                        ' a statement list so that locals can be bound. Note, while locals are not allowed at the top
                        ' level it is useful in the semantic model to bind them.
                        binder = memberBinder.GetBinder(DirectCast(current, SingleLineLambdaExpressionSyntax).Statements)
                        If binder IsNot Nothing Then
                            Return binder
                        End If
                    End If

                ElseIf InQueryInterior(current, position, binder) Then
                    ' We are in context of a query expression binder.
                    Debug.Assert(binder IsNot Nothing)
                    Return binder

                ElseIf InAnonymousTypeInitializerInterior(current, position, binder) Then
                    ' We are in context of an initializer expression binder.
                    Debug.Assert(binder IsNot Nothing)
                    Return binder

                ElseIf InWithStatementExpressionInterior(current) Then
                    ' Expression from With statement is supposed to be bound using
                    ' the binder for the syntax node enclosing With statement
                    Debug.Assert(current.Parent.Kind = SyntaxKind.WithStatement)
                    Debug.Assert(current.Parent.Parent.Kind = SyntaxKind.WithBlock)

                    current = current.Parent.Parent

                    ' If we are speculating on the With block, we might have reached our root,
                    ' return memberBinder in this case.
                    If current Is binderRoot Then
                        Return memberBinder
                    End If

                    current = current.Parent
                    ' Proceed to the end of If statement

                End If

                binder = memberBinder.GetBinder(current)
                If binder IsNot Nothing Then
                    Return binder
                End If

                If current Is binderRoot Then
                    Return memberBinder
                End If

                current = current.Parent
            Loop
        End Function

        ''' <summary>
        ''' If answer is True, the binder is returned via [binder] parameter.
        ''' </summary>
        Private Function InQueryInterior(
            node As SyntaxNode,
            position As Integer,
            <Out()> ByRef binder As Binder
        ) As Boolean
            binder = Nothing

            Select Case node.Kind
                Case SyntaxKind.WhereClause
                    Dim where = DirectCast(node, WhereClauseSyntax)
                    binder = GetSingleLambdaClauseLambdaBinder(where, where.WhereKeyword, position)

                Case SyntaxKind.SkipWhileClause, SyntaxKind.TakeWhileClause
                    Dim partitionWhile = DirectCast(node, PartitionWhileClauseSyntax)
                    binder = GetSingleLambdaClauseLambdaBinder(partitionWhile, partitionWhile.WhileKeyword, position)

                Case SyntaxKind.SelectClause
                    Dim [select] = DirectCast(node, SelectClauseSyntax)
                    binder = GetSingleLambdaClauseLambdaBinder([select], [select].SelectKeyword, position)

                Case SyntaxKind.LetClause
                    binder = GetLetClauseLambdaBinder(DirectCast(node, LetClauseSyntax), position)

                Case SyntaxKind.FromClause
                    binder = GetFromClauseLambdaBinder(DirectCast(node, FromClauseSyntax), position)

                Case SyntaxKind.GroupByClause
                    binder = GetGroupByClauseLambdaBinder(DirectCast(node, GroupByClauseSyntax), position)

                Case SyntaxKind.OrderByClause
                    Dim orderBy = DirectCast(node, OrderByClauseSyntax)
                    binder = GetSingleLambdaClauseLambdaBinder(orderBy, If(orderBy.ByKeyword.IsMissing, orderBy.OrderKeyword, orderBy.ByKeyword), position)

                Case SyntaxKind.SimpleJoinClause
                    binder = GetJoinClauseLambdaBinder(DirectCast(node, SimpleJoinClauseSyntax), position)

                Case SyntaxKind.GroupJoinClause
                    binder = GetGroupJoinClauseLambdaBinder(DirectCast(node, GroupJoinClauseSyntax), position)

                Case SyntaxKind.AggregateClause
                    binder = GetAggregateClauseLambdaBinder(DirectCast(node, AggregateClauseSyntax), position)

                Case SyntaxKind.FunctionAggregation
                    binder = GetFunctionAggregationLambdaBinder(DirectCast(node, FunctionAggregationSyntax), position)

            End Select

            Return binder IsNot Nothing
        End Function

        Private Function GetAggregateClauseLambdaBinder(aggregate As AggregateClauseSyntax, position As Integer) As Binder
            Dim binder As Binder = Nothing

            ' If position were in context of an additional query operator that operator would have handled it, unless there were
            ' no need for a special binder.
            ' We only need to worry about Variables and the Into clause.

            If SyntaxFacts.InSpanOrEffectiveTrailingOfNode(aggregate, position) Then
                If Not aggregate.IntoKeyword.IsMissing AndAlso aggregate.IntoKeyword.SpanStart <= position Then
                    ' Should return binder for the Into clause - the last one associated with the node.
                    Dim binders As ImmutableArray(Of Binder) = GetQueryClauseLambdaBinders(aggregate)
#If DEBUG Then
                    Debug.Assert(Not binders.IsDefault OrElse Not ShouldHaveFound(aggregate, guard:=True))
                    Debug.Assert(binders.IsDefault OrElse (binders.Length > 0 AndAlso binders.Length < 3))
#End If
                    If Not binders.IsEmpty Then
                        binder = binders.Last
                        Debug.Assert(binder IsNot Nothing)
                    End If

                ElseIf aggregate.AggregateKeyword.SpanStart <= position Then
                    binder = GetCollectionRangeVariablesLambdaBinder(aggregate.Variables, position)

                    If binder Is Nothing Then
                        ' Must be in context of the very first collection variable or an additional operator
                        ' that inherits the binder from parent context.
                        ' If this Aggregate clause doesn't begin the query, it has two binders:
                        '   - parent context binder, the one we should return;
                        '   - Into clause binder.
                        ' If this Aggregate begins the query, it has only one binder - the Into clause binder.

                        Dim binders As ImmutableArray(Of Binder) = GetQueryClauseLambdaBinders(aggregate)
#If DEBUG Then
                        Debug.Assert(Not binders.IsDefault OrElse Not ShouldHaveFound(aggregate, guard:=True))
                        Debug.Assert(binders.IsDefault OrElse (binders.Length > 0 AndAlso binders.Length < 3 AndAlso binders(0) IsNot Nothing))
#End If
                        If Not binders.IsDefault AndAlso binders.Length = 2 Then
                            binder = binders(0)
                        End If
                    End If
                End If
            End If

            Return binder
        End Function

        Private Function GetGroupJoinClauseLambdaBinder(join As GroupJoinClauseSyntax, position As Integer) As Binder
            Dim binder As Binder = Nothing

            If SyntaxFacts.InSpanOrEffectiveTrailingOfNode(join, position) Then
                If Not join.IntoKeyword.IsMissing AndAlso join.IntoKeyword.SpanStart <= position Then
                    ' Should return binder to lookup aggregate functions.
                    Dim binders As ImmutableArray(Of Binder) = GetQueryClauseLambdaBinders(join)
#If DEBUG Then
                    Debug.Assert(Not binders.IsDefault OrElse Not ShouldHaveFound(join, guard:=True))
                    Debug.Assert(binders.IsDefault OrElse binders.Length = 3)
#End If
                    If Not binders.IsDefault AndAlso binders.Length = 3 Then
                        binder = binders(2)
                    End If

                Else
                    ' Handle all parts, but [Into] clause.
                    binder = GetJoinClauseLambdaBinder(join, position)
                End If
            End If

            Return binder
        End Function

        Private Function GetJoinClauseLambdaBinder(join As JoinClauseSyntax, position As Integer) As Binder
            Dim binder As Binder = Nothing

            ' If position were in context of an additional join that join would have handled it, unless there were
            ' no need for a special binder.
            ' If position is in context of the collection range variable, we don't need a special binder.
            ' If position is in context of an 'On' clause, there is a binder that we need to return.

            If Not join.OnKeyword.IsMissing AndAlso join.OnKeyword.SpanStart <= position AndAlso SyntaxFacts.InSpanOrEffectiveTrailingOfNode(join, position) Then
                Dim binders As ImmutableArray(Of Binder) = GetQueryClauseLambdaBinders(join)
#If DEBUG Then
                Debug.Assert(Not binders.IsDefault OrElse Not ShouldHaveFound(join, guard:=True))
                Debug.Assert(binders.IsDefault OrElse (binders.Length > 1 AndAlso binders.Length < 4 AndAlso binders(0) IsNot Nothing))
#End If
                If Not binders.IsEmpty Then
                    ' The first two binders are outerkey and innerkey binders. Both have the same symbols in scope.
                    ' It is safe to always use the outerkey binder.
                    binder = binders(0)
                End If
            End If

            Return binder
        End Function

        Private Function GetFromClauseLambdaBinder(from As FromClauseSyntax, position As Integer) As Binder
            Dim binder As Binder = Nothing

            If SyntaxFacts.InSpanOrEffectiveTrailingOfNode(from, position) Then
                binder = GetCollectionRangeVariablesLambdaBinder(from.Variables, position)
            End If

            Return binder
        End Function

        Private Function GetCollectionRangeVariablesLambdaBinder(variables As SeparatedSyntaxList(Of CollectionRangeVariableSyntax), position As Integer) As Binder
            Dim binder As Binder = Nothing

            For i As Integer = 0 To variables.Count - 1
                Dim item As CollectionRangeVariableSyntax = variables(i)

                If SyntaxFacts.InSpanOrEffectiveTrailingOfNode(item, position) OrElse position < item.SpanStart Then

                    ' The first collection variable in a query or in an Aggregate clause doesn't have special binder
                    ' stored for it in the bound tree, the binder is inherited from outer context in that case.
                    If i > 0 OrElse
                      (item.Parent.Kind <> SyntaxKind.AggregateClause AndAlso
                       item.Parent.Parent IsNot Nothing AndAlso
                       Not (item.Parent.Parent.Kind = SyntaxKind.QueryExpression AndAlso
                                DirectCast(item.Parent.Parent, QueryExpressionSyntax).Clauses.FirstOrDefault Is item.Parent)) Then

                        Dim binders As ImmutableArray(Of Binder) = GetQueryClauseLambdaBinders(item)
#If DEBUG Then
                        Debug.Assert(Not binders.IsDefault OrElse Not ShouldHaveFound(item, guard:=True))
                        Debug.Assert(binders.IsDefault OrElse (binders.Length > 0 AndAlso binders.Length < 3 AndAlso binders(0) IsNot Nothing))
#End If
                        If Not binders.IsEmpty Then
                            ' Return manySelector binder.
                            binder = binders(0)
                        End If
                    End If

                    Exit For
                End If
            Next

            Return binder
        End Function

        Private Function GetLetClauseLambdaBinder([let] As LetClauseSyntax, position As Integer) As Binder
            Dim binder As Binder = Nothing

            If SyntaxFacts.InSpanOrEffectiveTrailingOfNode([let], position) Then

                For Each item As ExpressionRangeVariableSyntax In [let].Variables
                    If SyntaxFacts.InSpanOrEffectiveTrailingOfNode(item, position) OrElse position < item.SpanStart Then
                        Dim binders As ImmutableArray(Of Binder) = GetQueryClauseLambdaBinders(item)
#If DEBUG Then
                        Debug.Assert(Not binders.IsDefault OrElse Not ShouldHaveFound([let], guard:=True))
                        Debug.Assert(binders.IsDefault OrElse (binders.Length = 1 AndAlso binders(0) IsNot Nothing))
#End If
                        If Not binders.IsEmpty Then
                            binder = binders(0)
                        End If

                        Exit For
                    End If
                Next

                Debug.Assert(binder IsNot Nothing)
            End If

            Return binder
        End Function

        Private Function GetGroupByClauseLambdaBinder(groupBy As GroupByClauseSyntax, position As Integer) As Binder
            Dim binder As Binder = Nothing

            If SyntaxFacts.InSpanOrEffectiveTrailingOfNode(groupBy, position) Then
                Dim binders As ImmutableArray(Of Binder) = GetQueryClauseLambdaBinders(groupBy)
#If DEBUG Then
                Debug.Assert(Not binders.IsDefault OrElse Not ShouldHaveFound(groupBy, guard:=True))
                Debug.Assert(binders.IsDefault OrElse (binders.Length = 2 OrElse binders.Length = 3))
#End If
                If Not binders.IsEmpty Then

                    If position < groupBy.ByKeyword.SpanStart Then
                        If binders.Length <= 2 Then
                            ' If we didn't create a binder for items, which is the case if there were no items,
                            ' it is safe to grab the keys binder, because the scope is the same for both.
                            binder = binders(0)
                        Else
                            binder = binders(1)
                        End If

                    ElseIf position < groupBy.IntoKeyword.SpanStart Then
                        ' Binder for keys.
                        binder = binders(0)

                    Else
                        ' Binder for Into.
                        binder = binders.Last
                    End If

                    Debug.Assert(binder IsNot Nothing)
                End If
            End If

            Return binder
        End Function

        Private Function GetFunctionAggregationLambdaBinder(func As FunctionAggregationSyntax, position As Integer) As Binder
            Dim binder As Binder = Nothing

            If Not func.OpenParenToken.IsMissing AndAlso func.OpenParenToken.SpanStart <= position AndAlso
               ((func.CloseParenToken.IsMissing AndAlso SyntaxFacts.InSpanOrEffectiveTrailingOfNode(func, position)) OrElse position < func.CloseParenToken.SpanStart) Then

                Dim binders As ImmutableArray(Of Binder) = GetQueryClauseLambdaBinders(func)
#If DEBUG Then
                Debug.Assert(Not binders.IsDefault OrElse Not ShouldHaveFound(func, guard:=True))
                Debug.Assert(binders.IsDefault OrElse (binders.Length = 1 AndAlso binders(0) IsNot Nothing))
#End If
                If Not binders.IsDefaultOrEmpty Then
                    binder = binders(0)
                End If
            End If

            Return binder
        End Function

        Private Function GetSingleLambdaClauseLambdaBinder(
            operatorSyntax As QueryClauseSyntax,
            operatorKeyWord As SyntaxToken,
            position As Integer
        ) As Binder
            If operatorKeyWord.SpanStart <= position AndAlso SyntaxFacts.InSpanOrEffectiveTrailingOfNode(operatorSyntax, position) Then
                Dim binders As ImmutableArray(Of Binder) = GetQueryClauseLambdaBinders(operatorSyntax)
#If DEBUG Then
                Debug.Assert(Not binders.IsDefault OrElse Not ShouldHaveFound(operatorSyntax, guard:=True))
                Debug.Assert(binders.IsDefault OrElse (binders.Length = 1 AndAlso binders(0) IsNot Nothing))
#End If
                If Not binders.IsDefaultOrEmpty Then
                    Return binders(0)
                End If
            End If

            Return Nothing
        End Function

        Private Function GetQueryClauseLambdaBinders(node As VisualBasicSyntaxNode) As ImmutableArray(Of Binder)
            Debug.Assert(TypeOf node Is QueryClauseSyntax OrElse node.Kind = SyntaxKind.FunctionAggregation OrElse
                         (node.Kind = SyntaxKind.ExpressionRangeVariable AndAlso node.Parent.Kind = SyntaxKind.LetClause) OrElse
                         node.Kind = SyntaxKind.CollectionRangeVariable)

            Dim binders As ImmutableArray(Of Binder) = Nothing

            _rwLock.EnterReadLock()
            Try
                If Me._guardedQueryBindersMap.TryGetValue(node, binders) Then
                    Return binders
                End If
            Finally
                _rwLock.ExitReadLock()
            End Try

            ' Calling GetUpperBoundNode for the expression will force the
            ' query binders map for the whole immediate query expression
            ' to be generated.
            Dim boundNode = GetUpperBoundNode(node)

            _rwLock.EnterWriteLock()
            Try
                If Me._guardedQueryBindersMap.TryGetValue(node, binders) Then
                    Return binders
                End If

                ' NOTE: this is a fix for the case when we cannot find a bound node
                '       because the syntax is under unsupported construction
                If boundNode Is Nothing OrElse boundNode.Kind <> BoundKind.NoOpStatement OrElse Not boundNode.HasErrors Then
                    AssertIfShouldHaveFound(node)
                End If

                Me._guardedQueryBindersMap.Add(node, Nothing)
                Return Nothing
            Finally
                _rwLock.ExitWriteLock()
            End Try
        End Function

        ''' <summary>
        ''' If answer is True, the binder is returned via [binder] parameter.
        ''' </summary>
        Private Function InAnonymousTypeInitializerInterior(
            node As SyntaxNode,
            position As Integer,
            <Out()> ByRef binder As Binder
        ) As Boolean
            binder = Nothing

            If (node.Kind = SyntaxKind.InferredFieldInitializer OrElse node.Kind = SyntaxKind.NamedFieldInitializer) AndAlso
               node.Parent IsNot Nothing AndAlso node.Parent.Kind = SyntaxKind.ObjectMemberInitializer AndAlso
               node.Parent.Parent IsNot Nothing AndAlso node.Parent.Parent.Kind = SyntaxKind.AnonymousObjectCreationExpression Then

                Dim initialization = DirectCast(node, FieldInitializerSyntax)

                If SyntaxFacts.InSpanOrEffectiveTrailingOfNode(initialization, position) Then

                    Dim cachedBinder As Binder.AnonymousTypeFieldInitializerBinder = Nothing

                    _rwLock.EnterReadLock()
                    Try
                        If Me._guardedAnonymousTypeBinderMap.TryGetValue(initialization, cachedBinder) Then
                            binder = cachedBinder
                            Return binder IsNot Nothing
                        End If
                    Finally
                        _rwLock.ExitReadLock()
                    End Try

                    ' Get bound node for the whole AnonymousType initializer expression.
                    ' This will build required maps for it.
                    Dim boundNode As BoundNode = GetUpperBoundNode(initialization.Parent.Parent)

                    _rwLock.EnterReadLock()
                    Try
                        If Me._guardedAnonymousTypeBinderMap.TryGetValue(initialization, cachedBinder) Then
                            binder = cachedBinder
                            Return binder IsNot Nothing
                        End If

                        ' NOTE: this is a fix for the case when we cannot find a bound node
                        '       because the syntax is under unsupported construction
                        If boundNode Is Nothing OrElse boundNode.Kind <> BoundKind.NoOpStatement OrElse Not boundNode.HasErrors Then
                            AssertIfShouldHaveFound(initialization)
                        End If
                    Finally
                        _rwLock.ExitReadLock()
                    End Try
                End If
            End If

            Return False
        End Function

        Private Shared Function InWithStatementExpressionInterior(node As SyntaxNode) As Boolean

            Dim expression = TryCast(node, ExpressionSyntax)
            If expression IsNot Nothing Then
                Dim parent As VisualBasicSyntaxNode = expression.Parent
                If parent IsNot Nothing AndAlso parent.Kind = SyntaxKind.WithStatement Then
                    parent = parent.Parent
                    Return parent IsNot Nothing AndAlso parent.Kind = SyntaxKind.WithBlock AndAlso parent.Parent IsNot Nothing
                End If
            End If

            Return False
        End Function

        Private Function GetLambdaBodyBinder(lambda As LambdaExpressionSyntax) As LambdaBodyBinder
            Dim boundLambda As BoundLambda = GetBoundLambda(lambda)

            If boundLambda IsNot Nothing Then
                Return boundLambda.LambdaBinderOpt
            End If

            Return Nothing
        End Function

        Private Function GetBoundLambda(lambda As LambdaExpressionSyntax) As BoundLambda
            Dim boundNode = GetLowerBoundNode(lambda)
            Debug.Assert(boundNode Is Nothing OrElse boundNode.Kind = BoundKind.Lambda, "all lambdas should be converted to bound lambdas now")
            Return DirectCast(boundNode, BoundLambda)
        End Function

        <Conditional("DEBUG")>
        Private Sub AssertIfShouldHaveFound(node As VisualBasicSyntaxNode)
#If DEBUG Then
            Debug.Assert(Not ShouldHaveFound(node))
#End If
        End Sub

#If DEBUG Then
        Private Function ShouldHaveFound(node As VisualBasicSyntaxNode, Optional guard As Boolean = False) As Boolean
            If guard Then
                _rwLock.EnterReadLock()
                Try
                    Return ShouldHaveFound(node, guard:=False)
                Finally
                    _rwLock.ExitReadLock()
                End Try
            End If

            Dim child As VisualBasicSyntaxNode = node
            Dim parent As VisualBasicSyntaxNode = node.Parent

            ' We will not be able to find an expression used in an array bounds within a parameter declaration.
            ' We will not be able to find an expression used as a lower array bound.
            While parent IsNot Nothing
                Select Case parent.Kind
                    Case SyntaxKind.RangeArgument
                        If DirectCast(parent, RangeArgumentSyntax).LowerBound Is child Then
                            Exit While
                        End If

                    Case SyntaxKind.ModifiedIdentifier
                        If parent.Parent IsNot Nothing AndAlso parent.Parent.Kind = SyntaxKind.Parameter Then
                            Exit While
                        End If
                End Select

                child = parent
                parent = parent.Parent
            End While

            Return (parent Is Nothing)
        End Function
#End If

        ''' <summary>
        ''' Get all bound nodes associated with a node, ordered from highest to lowest in the bound tree.
        ''' Strictly speaking, the order is that of a pre-order traversal of the bound tree.
        ''' As a side effect, caches nodes and binders.
        ''' </summary>
        Friend Function GetBoundNodes(node As SyntaxNode) As ImmutableArray(Of BoundNode)
            Dim bound As ImmutableArray(Of BoundNode) = Nothing

            EnsureFullyBoundIfImplicitVariablesAllowed()

            ' First, look in the cached bounds nodes.
            _rwLock.EnterReadLock()
            Try
                bound = GuardedGetBoundNodesFromMap(node)
            Finally
                _rwLock.ExitReadLock()
            End Try

            If Not bound.IsDefault Then
                Return bound
            End If

            If IsNonExpressionCollectionInitializer(node) Then
                Return ImmutableArray(Of BoundNode).Empty
            End If

            ' If we didn't find in the cached bound nodes, find a binding root and bind it.
            ' This will cache bound nodes under the binding root.
            Dim bindingRoot = Me.GetBindingRoot(node)
            Dim bindingRootBinder = GetEnclosingBinder(bindingRoot)

            _rwLock.EnterWriteLock()
            Try
                bound = GuardedGetBoundNodesFromMap(node)
                If bound.IsDefault Then
                    GuardedIncrementalBind(bindingRoot, bindingRootBinder)
                End If
                bound = GuardedGetBoundNodesFromMap(node)

                If Not bound.IsDefault Then
                    Return bound
                End If
            Finally
                _rwLock.ExitWriteLock()
            End Try

            ' If we still didn't find it, its still possible we could bind it directly.
            ' For example, types are usually not represented by bound nodes, and some error conditions and
            ' not yet implemented features do not create bound nodes for everything underneath them.
            '
            ' In this case, however, we only add the single bound node we found to the map, not any child bound nodes,
            ' to avoid duplicates in the map if a parent of this node comes through this code path also.
            If TypeOf node Is ExpressionSyntax OrElse TypeOf node Is StatementSyntax Then
                Dim binder = New IncrementalBinder(Me, GetEnclosingBinder(node))

                _rwLock.EnterWriteLock()
                Try
                    bound = GuardedGetBoundNodesFromMap(node)

                    If bound.IsDefault Then
                        ' Bind the node and cache any associated bound nodes we find.
                        Dim boundNode = Me.Bind(binder, node, BindingDiagnosticBag.Discarded)
                        SemanticModelMapsBuilder.GuardedCacheBoundNodes(boundNode, Me, _guardedBoundNodeMap, node)
                    End If

                    bound = GuardedGetBoundNodesFromMap(node)

                    If Not bound.IsDefault Then
                        Return bound
                    End If
                Finally
                    _rwLock.ExitWriteLock()
                End Try
            End If

            ' Nothing to return.
            Return ImmutableArray(Of BoundNode).Empty
        End Function

        ''' <summary>
        ''' A collection initializer syntax node is not always treated as a VB expression syntax node
        ''' in case it's part of a CollectionInitializer (outer most or top level initializer).
        ''' </summary>
        ''' <param name="syntax">The syntax node to check.</param>
        ''' <returns><c>True</c> if the syntax node represents an expression syntax, but it's not
        ''' an expression from the VB language point of view; otherwise <c>False</c>.</returns>
        Private Shared Function IsNonExpressionCollectionInitializer(syntax As SyntaxNode) As Boolean
            Dim parent As SyntaxNode = syntax.Parent
            If syntax.Kind = SyntaxKind.CollectionInitializer AndAlso parent IsNot Nothing Then
                If parent.Kind = SyntaxKind.ObjectCollectionInitializer Then
                    Return True
                ElseIf parent.Kind = SyntaxKind.CollectionInitializer Then
                    parent = parent.Parent
                    Return parent IsNot Nothing AndAlso parent.Kind = SyntaxKind.ObjectCollectionInitializer
                End If
            End If

            Return False
        End Function

        ''' <summary>
        ''' Incrementally bind bindingRoot (which is always a non-lambda enclosed statement, or the
        ''' root of this model). Side effect is to store nodes into the guarded node map.
        ''' </summary>
        Private Sub GuardedIncrementalBind(bindingRoot As SyntaxNode, enclosingBinder As Binder)
            Debug.Assert(_rwLock.IsWriteLockHeld)

            If _guardedBoundNodeMap.ContainsKey(bindingRoot) Then
                ' We've already bound this. No need to bind it again (saves a bit of
                ' work below).
                Return
            End If

            Debug.Assert(enclosingBinder.IsSemanticModelBinder)
            Dim binder = New IncrementalBinder(Me, enclosingBinder)
            Dim boundRoot As BoundNode = Me.Bind(binder, bindingRoot, BindingDiagnosticBag.Discarded)

            ' if the node could not be bound, there's nothing more to do.
            If boundRoot Is Nothing Then
                Return
            End If

            SemanticModelMapsBuilder.GuardedCacheBoundNodes(boundRoot, Me, _guardedBoundNodeMap)

            If Not _guardedBoundNodeMap.ContainsKey(bindingRoot) Then
                ' Generally 'bindingRoot' is supposed to be found in node map at this point,
                ' but it will not happen in some scenarios such as for field or property
                ' initializers, let's add it to prevent re-binding

                Debug.Assert(bindingRoot.Kind = SyntaxKind.FieldDeclaration OrElse
                             bindingRoot.Kind = SyntaxKind.PropertyStatement OrElse
                             bindingRoot.Kind = SyntaxKind.Parameter OrElse
                             bindingRoot.Kind = SyntaxKind.EnumMemberDeclaration OrElse
                             bindingRoot Is Me.Root AndAlso Me.IsSpeculativeSemanticModel)

                _guardedBoundNodeMap.Add(bindingRoot, ImmutableArray.Create(Of BoundNode)(boundRoot))
            End If
        End Sub

        ''' <summary>
        ''' In order that any expression level special binders are used, lambdas are fully resolved,
        ''' and that any other binding context is correctly handled, we only use the binder to create bound
        ''' nodes for:
        '''   a) The root syntax of this semantic model (because there's nothing more outer to bind)
        '''   b) A stand-alone statement is that is not inside a lambda.
        ''' </summary>
        Private Function GetBindingRoot(node As SyntaxNode) As SyntaxNode
            Dim enclosingStatement As StatementSyntax = Nothing

            ' Walk all the way up to the root syntax, so that we see any enclosing lambdas.
            While node IsNot Me.Root
                If enclosingStatement Is Nothing Then
                    Dim statementNode = TryCast(node, StatementSyntax)
                    If statementNode IsNot Nothing AndAlso IsStandaloneStatement(statementNode) Then
                        enclosingStatement = statementNode
                    End If
                End If

                If node.Kind = SyntaxKind.DocumentationCommentTrivia Then
                    Dim trivia As SyntaxTrivia = DirectCast(node, DocumentationCommentTriviaSyntax).ParentTrivia
                    Debug.Assert(trivia.Kind <> SyntaxKind.None)
                    Debug.Assert(trivia.Token.Kind <> SyntaxKind.None)
                    node = DirectCast(trivia.Token.Parent, VisualBasicSyntaxNode)
                    Continue While

                ElseIf node.IsLambdaExpressionSyntax Then
                    ' We can't use a statement that is inside a lambda.
                    enclosingStatement = Nothing
                End If

                node = node.Parent
            End While

            If enclosingStatement IsNot Nothing Then
                Return enclosingStatement
            Else
                Return Me.Root
            End If
        End Function

        ''' <summary>
        ''' The incremental binder is used when binding statements. Whenever a statement
        ''' is bound, it checks the bound node cache to see if that statement was bound,
        ''' and returns it instead of rebinding it.
        '''
        ''' FOr example, we might have:
        '''    While x > goo()
        '''      y = y * x
        '''      z = z + y
        '''    End While
        '''
        ''' We might first get semantic info about "z", and thus bind just the statement
        ''' "z = z + y". Later, we might bind the entire While block. While binding the while
        ''' block, we can reuse the binding we did of "z = z + y".
        ''' </summary>
        Friend Class IncrementalBinder
            Inherits Binder

            Private ReadOnly _binding As MemberSemanticModel

            Friend Sub New(binding As MemberSemanticModel, [next] As Binder)
                MyBase.New([next])
                _binding = binding
            End Sub

            ''' <summary>
            ''' We override GetBinder so that the BindStatement override is still
            ''' in effect on nested binders.
            ''' </summary>
            Public Overrides Function GetBinder(node As SyntaxNode) As Binder
                Dim binder As Binder = Me.ContainingBinder.GetBinder(node)

                If binder IsNot Nothing Then
                    Debug.Assert(Not (TypeOf binder Is IncrementalBinder))
                    Return New IncrementalBinder(_binding, binder)
                End If

                Return Nothing
            End Function

            ''' <summary>
            ''' We override GetBinder so that the BindStatement override is still
            ''' in effect on nested binders.
            ''' </summary>
            Public Overrides Function GetBinder(list As SyntaxList(Of StatementSyntax)) As Binder
                Dim binder As Binder = Me.ContainingBinder.GetBinder(list)

                If binder IsNot Nothing Then
                    Debug.Assert(Not (TypeOf binder Is IncrementalBinder))
                    Return New IncrementalBinder(_binding, binder)
                End If

                Return Nothing
            End Function

            Public Overrides Function BindStatement(node As StatementSyntax, diagnostics As BindingDiagnosticBag) As BoundStatement
                ' Check the bound node cache to see if the statement was already bound.
                Dim boundNodes As ImmutableArray(Of BoundNode) = _binding.GuardedGetBoundNodesFromMap(node)

                If boundNodes.IsDefault Then
                    ' Not bound already. Bind it. It will get added to the cache later by the SemanticModelMapsBuilder.
                    Dim boundStmt = MyBase.BindStatement(node, diagnostics)
                    Debug.Assert((TypeOf boundStmt Is BoundStatement))
                    Return boundStmt
                Else
                    ' Already bound. Return the top-most bound node associated with the statement.
                    Return DirectCast(boundNodes.First, BoundStatement)
                End If
            End Function
        End Class

        Friend Overrides Function GetAwaitExpressionInfoWorker(awaitExpression As AwaitExpressionSyntax, Optional cancellationToken As CancellationToken = Nothing) As AwaitExpressionInfo
            Dim bound As BoundNode = GetLowerBoundNode(awaitExpression)

            If bound IsNot Nothing AndAlso bound.Kind = BoundKind.AwaitOperator Then
                Dim boundAwait = DirectCast(bound, BoundAwaitOperator)

                Return New AwaitExpressionInfo(TryCast(boundAwait.GetAwaiter.ExpressionSymbol, MethodSymbol),
                                               TryCast(boundAwait.IsCompleted.ExpressionSymbol, PropertySymbol),
                                               TryCast(boundAwait.GetResult.ExpressionSymbol, MethodSymbol))
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Traverse a tree of bound nodes, and update the following maps inside the SemanticModel:
        '''
        '''     guardedNodeMap  - a map from syntax node to bound nodes. Bound nodes are added in the order they are bound
        '''                       traversing the tree, so they will be in order from upper to lower node.
        '''
        '''     guardedQueryBindersMap - a map from query-specific syntax node to an array of binders used to
        '''                              bind various children of the node.
        '''
        '''     guardedAnonymousTypeBinderMap - a map from Anonymous Type initializer's FieldInitializerSyntax to
        '''                                     Binder.AnonymousTypeFieldInitializerBinder used to bind its expression.
        '''</summary>
        Private Class SemanticModelMapsBuilder
            Inherits BoundTreeWalkerWithStackGuard

            Private ReadOnly _semanticModel As MemberSemanticModel
            Private ReadOnly _thisSyntaxNodeOnly As SyntaxNode ' If not Nothing, record nodes for this syntax node only.
            Private _placeholderReplacementMap As Dictionary(Of BoundValuePlaceholderBase, BoundExpression)
            Private ReadOnly _nodeCache As OrderPreservingMultiDictionary(Of SyntaxNode, BoundNode)

            Private Sub New(semanticModel As MemberSemanticModel, thisSyntaxNodeOnly As SyntaxNode, nodeCache As OrderPreservingMultiDictionary(Of SyntaxNode, BoundNode))
                _semanticModel = semanticModel
                _thisSyntaxNodeOnly = thisSyntaxNodeOnly
                _nodeCache = nodeCache
            End Sub

            Public Shared Sub GuardedCacheBoundNodes(
                root As BoundNode,
                semanticModel As MemberSemanticModel,
                nodeCache As SmallDictionary(Of SyntaxNode, ImmutableArray(Of BoundNode)),
                Optional thisSyntaxNodeOnly As SyntaxNode = Nothing
            )
                Debug.Assert(semanticModel._rwLock.IsWriteLockHeld)

                Dim additionalNodes = OrderPreservingMultiDictionary(Of SyntaxNode, BoundNode).GetInstance()

                Dim walker = New SemanticModelMapsBuilder(semanticModel, thisSyntaxNodeOnly, additionalNodes)
                walker.Visit(root)

                For Each key In additionalNodes.Keys
                    If Not nodeCache.ContainsKey(key) Then
                        nodeCache(key) = additionalNodes(key)
                    Else
#If DEBUG Then
                        ' It's possible that GuardedIncrementalBind was previously called with a subtree of bindingRoot. If
                        ' this is the case, then we'll see an entry in the map. Since the incremental binder should also have seen the
                        ' pre-existing map entry, the entry in addition map should be identical.
                        ' Another, more unfortunate, possibility is that we've had to re-bind the syntax and the new bound
                        ' nodes are equivalent, but not identical, to the existing ones. In such cases, we prefer the
                        ' existing nodes so that the cache will always return the same bound node for a given syntax node.

                        ' EXAMPLE: Suppose we have the statement P.M(1)
                        ' First, we ask for semantic info about "P".  We'll walk up to the statement level and bind that.
                        ' We'll end up with map entries for "1", "P" and "P.M(1)".
                        ' Next, we ask for semantic info about "P.M".  That isn't in our map, so we walk up to the statement
                        ' level - again - and bind that - again.
                        ' Once again, we'll end up with map entries for "1", "P" and "P.M(1)". They will
                        ' have the same structure as the original map entries, but will not be ReferenceEquals.

                        Dim existing = nodeCache(key)
                        Dim added = additionalNodes(key)
                        Debug.Assert(existing.Length = added.Length)

                        For i = 0 To existing.Length - 1
                            Debug.Assert(existing(i).Kind = added(i).Kind, "New bound node does not match existing bound node")
                        Next
#End If
                    End If
                Next

                additionalNodes.Free()
            End Sub

            ''' <summary>
            ''' Should we record bound node mapping for this node? Generally, we ignore compiler generated, but optionally can
            ''' allow.
            ''' </summary>
            Public Function RecordNode(node As BoundNode, Optional allowCompilerGenerated As Boolean = False) As Boolean

                If Not allowCompilerGenerated AndAlso node.WasCompilerGenerated Then
                    ' Don't cache compiler generated nodes
                    Return False
                End If

                Select Case node.Kind
                    Case BoundKind.UnboundLambda
                        ' Don't cache unbound lambdas (unbound lambda are converted into bound lambdas in VisitUnboundLambda.)
                        Return False

                    Case BoundKind.Conversion
                        If Not allowCompilerGenerated Then
                            Dim conversion = DirectCast(node, BoundConversion)
                            If Not conversion.ExplicitCastInCode AndAlso conversion.Operand.WasCompilerGenerated Then
                                Select Case conversion.Operand.Kind
                                    Case BoundKind.RValuePlaceholder,
                                         BoundKind.LValuePlaceholder,
                                         BoundKind.WithLValueExpressionPlaceholder,
                                         BoundKind.WithRValueExpressionPlaceholder
                                        ' Don't cache compiler generated nodes
                                        Return False
                                End Select
                            End If
                        End If
                End Select

                If _thisSyntaxNodeOnly IsNot Nothing AndAlso node.Syntax IsNot _thisSyntaxNodeOnly Then
                    ' Didn't match the syntax node we're trying to handle
                    Return False
                End If

                Return True
            End Function

            Public Overrides Function Visit(node As BoundNode) As BoundNode
                If node Is Nothing Then
                    Return Nothing
                End If

                If RecordNode(node) Then
                    _nodeCache.Add(node.Syntax, node)
                End If

                Return MyBase.Visit(node)
            End Function

            Protected Overrides Function ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException() As Boolean
                Return False
            End Function

            Public Overrides Function VisitBinaryOperator(node As BoundBinaryOperator) As BoundNode
                If node.Left.Kind <> BoundKind.BinaryOperator Then
                    Return MyBase.VisitBinaryOperator(node)
                End If

                Dim rightOperands = ArrayBuilder(Of BoundExpression).GetInstance()

                rightOperands.Push(node.Right)

                Dim binary = DirectCast(node.Left, BoundBinaryOperator)

                If RecordNode(binary) Then
                    _nodeCache.Add(binary.Syntax, binary)
                End If

                rightOperands.Push(binary.Right)

                Dim current As BoundExpression = binary.Left

                While current.Kind = BoundKind.BinaryOperator
                    binary = DirectCast(current, BoundBinaryOperator)

                    If RecordNode(binary) Then
                        _nodeCache.Add(binary.Syntax, binary)
                    End If

                    rightOperands.Push(binary.Right)
                    current = binary.Left
                End While

                Me.Visit(current)

                While rightOperands.Count > 0
                    Me.Visit(rightOperands.Pop())
                End While

                rightOperands.Free()
                Return Nothing
            End Function

            Public Overrides Function VisitUnboundLambda(node As UnboundLambda) As BoundNode
                Return Visit(node.BindForErrorRecovery())
            End Function

            Public Overrides Function VisitCall(node As BoundCall) As BoundNode
                Dim receiver As BoundExpression = node.ReceiverOpt
                Debug.Assert(receiver Is Nothing OrElse Not node.Method.IsShared OrElse receiver.HasErrors)
                Me.Visit(receiver)

                Dim boundGroup As BoundMethodGroup = node.MethodGroupOpt
                If boundGroup IsNot Nothing Then
                    If boundGroup.Syntax IsNot node.Syntax Then

                        Debug.Assert(boundGroup.ReceiverOpt Is Nothing OrElse receiver Is Nothing)
                        Me.Visit(boundGroup)

                    ElseIf node.Method.IsShared Then
                        ' NOTE: in this case the receiver is nothing, but we still
                        '       want to visit it if we find it in the method group
                        Me.Visit(boundGroup.ReceiverOpt)
                    End If
                End If

                Me.VisitList(node.Arguments)
                Return Nothing
            End Function

            Public Overrides Function VisitPropertyAccess(node As BoundPropertyAccess) As BoundNode
                Dim receiver As BoundExpression = node.ReceiverOpt
                Debug.Assert(receiver Is Nothing OrElse Not node.PropertySymbol.IsShared OrElse receiver.HasErrors)
                Me.Visit(receiver)

                Dim boundGroup As BoundPropertyGroup = node.PropertyGroupOpt
                If boundGroup IsNot Nothing Then
                    If boundGroup.Syntax IsNot node.Syntax Then

                        Debug.Assert(boundGroup.ReceiverOpt Is Nothing OrElse receiver Is Nothing)
                        Me.Visit(node.PropertyGroupOpt)

                    ElseIf node.PropertySymbol.IsShared Then
                        ' NOTE: in this case the receiver is nothing but we still
                        '       want to visit it if we find it in the property group
                        Me.Visit(boundGroup.ReceiverOpt)
                    End If
                End If

                Me.VisitList(node.Arguments)
                Return Nothing
            End Function

            Public Overrides Function VisitTypeExpression(node As BoundTypeExpression) As BoundNode
                Me.Visit(node.UnevaluatedReceiverOpt)
                Return MyBase.VisitTypeExpression(node)
            End Function

            Public Overrides Function VisitAttribute(node As BoundAttribute) As BoundNode
                Me.VisitList(node.ConstructorArguments)
                For Each namedArg In node.NamedArguments
                    Me.Visit(namedArg)
                Next
                Return Nothing
            End Function

            Public Overrides Function VisitQueryClause(node As BoundQueryClause) As BoundNode
                If RecordNode(node) Then
#If DEBUG Then
                    Dim haveBindersInTheMap As ImmutableArray(Of Binder) = Nothing
                    Debug.Assert(Not _semanticModel._guardedQueryBindersMap.TryGetValue(node.Syntax, haveBindersInTheMap) OrElse haveBindersInTheMap.Equals(node.Binders))
#End If

                    _semanticModel._guardedQueryBindersMap(node.Syntax) = node.Binders
                End If

                Return MyBase.VisitQueryClause(node)
            End Function

            Public Overrides Function VisitAggregateClause(node As BoundAggregateClause) As BoundNode
                If RecordNode(node) Then
#If DEBUG Then
                    Dim haveBindersInTheMap As ImmutableArray(Of Binder) = Nothing
                    Debug.Assert(Not _semanticModel._guardedQueryBindersMap.TryGetValue(node.Syntax, haveBindersInTheMap) OrElse haveBindersInTheMap.Equals(node.Binders))
#End If
                    _semanticModel._guardedQueryBindersMap(node.Syntax) = node.Binders
                End If

                Return MyBase.VisitAggregateClause(node)
            End Function

            Public Overrides Function VisitAnonymousTypeFieldInitializer(node As BoundAnonymousTypeFieldInitializer) As BoundNode
                If RecordNode(node, allowCompilerGenerated:=True) Then
                    Dim initialization = TryCast(node.Syntax, FieldInitializerSyntax)

                    If initialization IsNot Nothing Then
#If DEBUG Then
                        Dim haveBindersInTheMap As Binder.AnonymousTypeFieldInitializerBinder = Nothing
                        ' The assert below is disabled due to https://github.com/dotnet/roslyn/issues/27533, need to follow up
                        'Debug.Assert(Not _semanticModel._guardedAnonymousTypeBinderMap.TryGetValue(initialization, haveBindersInTheMap) OrElse haveBindersInTheMap Is node.Binder)
#End If
                        _semanticModel._guardedAnonymousTypeBinderMap(initialization) = node.Binder
                    End If
                End If

                Return MyBase.VisitAnonymousTypeFieldInitializer(node)
            End Function

            Public Overrides Function VisitConversion(node As BoundConversion) As BoundNode
                ' Shouldn't visit RelaxationLambda here.
                Return Visit(node.Operand)
            End Function

            Public Overrides Function VisitDirectCast(node As BoundDirectCast) As BoundNode
                ' Shouldn't visit RelaxationLambda here.
                Return Visit(node.Operand)
            End Function

            Public Overrides Function VisitTryCast(node As BoundTryCast) As BoundNode
                ' Shouldn't visit RelaxationLambda here.
                Return Visit(node.Operand)
            End Function

            Public Overrides Function VisitDelegateCreationExpression(node As BoundDelegateCreationExpression) As BoundNode
                ' Shouldn't visit RelaxationLambda here.

                Dim receiver As BoundExpression = node.ReceiverOpt
                Me.Visit(receiver)

                Dim boundGroup As BoundMethodGroup = node.MethodGroupOpt
                If boundGroup IsNot Nothing Then
                    If boundGroup.Syntax IsNot node.Syntax Then

                        Debug.Assert(boundGroup.ReceiverOpt Is Nothing OrElse receiver Is Nothing)
                        Me.Visit(boundGroup)

                    ElseIf node.Method.IsShared Then
                        ' NOTE: in this case the receiver is nothing, but we still
                        '       want to visit it if we find it in the method group
                        Me.Visit(boundGroup.ReceiverOpt)
                    End If
                End If

                Return Nothing
            End Function

            Public Overrides Function VisitAssignmentOperator(node As BoundAssignmentOperator) As BoundNode
                If node.LeftOnTheRightOpt Is Nothing Then
                    Return MyBase.VisitAssignmentOperator(node)
                End If

                ' This is a compound assignment.
                ' Don't cache the left node now, in order to provide accurate type information,
                ' it should be cached when we visit its placeholder instead.
                ' Visiting the right side should take care of this.
                If _placeholderReplacementMap Is Nothing Then
                    _placeholderReplacementMap = New Dictionary(Of BoundValuePlaceholderBase, BoundExpression)()
                End If

                _placeholderReplacementMap.Add(node.LeftOnTheRightOpt, node.Left)
                Visit(node.Right)
                _placeholderReplacementMap.Remove(node.LeftOnTheRightOpt)
                Return Nothing
            End Function

            Public Overrides Function VisitCompoundAssignmentTargetPlaceholder(node As BoundCompoundAssignmentTargetPlaceholder) As BoundNode
                Dim replacement As BoundExpression = Nothing

                If _placeholderReplacementMap IsNot Nothing AndAlso _placeholderReplacementMap.TryGetValue(node, replacement) Then
                    Return Visit(replacement)
                End If

                Return MyBase.VisitCompoundAssignmentTargetPlaceholder(node)
            End Function

            Public Overrides Function VisitByRefArgumentPlaceholder(node As BoundByRefArgumentPlaceholder) As BoundNode
                Dim replacement As BoundExpression = Nothing

                If _placeholderReplacementMap IsNot Nothing AndAlso _placeholderReplacementMap.TryGetValue(node, replacement) Then
                    Return Visit(replacement)
                End If

                Return MyBase.VisitByRefArgumentPlaceholder(node)
            End Function

            Public Overrides Function VisitByRefArgumentWithCopyBack(node As BoundByRefArgumentWithCopyBack) As BoundNode
                ' Don't cache the OriginalArgument node now, in order to provide accurate type information,
                ' it should be cached when we visit its InPlaceholder instead.
                ' Visiting the InConversion should take care of this.
                If _placeholderReplacementMap Is Nothing Then
                    _placeholderReplacementMap = New Dictionary(Of BoundValuePlaceholderBase, BoundExpression)()
                End If

                _placeholderReplacementMap.Add(node.InPlaceholder, node.OriginalArgument)
                Visit(node.InConversion)
                _placeholderReplacementMap.Remove(node.InPlaceholder)
                Return Nothing
            End Function

            Private Function VisitObjectInitializerExpressionBase(node As BoundObjectInitializerExpressionBase) As BoundNode
                Me.VisitList(node.Initializers)

                Return Nothing
            End Function

            Public Overrides Function VisitCollectionInitializerExpression(node As BoundCollectionInitializerExpression) As BoundNode
                Return Me.VisitObjectInitializerExpressionBase(node)
            End Function

            Public Overrides Function VisitObjectInitializerExpression(node As BoundObjectInitializerExpression) As BoundNode
                Return Me.VisitObjectInitializerExpressionBase(node)
            End Function

            Public Overrides Function VisitLateInvocation(node As BoundLateInvocation) As BoundNode
                MyBase.VisitLateInvocation(node)

                Dim member = TryCast(node.Member, BoundLateMemberAccess)
                If member IsNot Nothing AndAlso member.ReceiverOpt Is Nothing AndAlso node.MethodOrPropertyGroupOpt IsNot Nothing Then
                    ' The semantic model needs to see the method or property group's receiver if its member's receiver is Nothing.
                    Visit(node.MethodOrPropertyGroupOpt.ReceiverOpt)
                End If

                Return Nothing
            End Function
        End Class
    End Class
End Namespace
