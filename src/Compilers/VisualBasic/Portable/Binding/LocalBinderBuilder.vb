' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' The <see cref="LocalBinderBuilder"/> is used to build up the map of all <see cref="Binder"/>s within a method body, and the associated
    ''' <see cref="VisualBasicSyntaxNode"/>. To do so it traverses all the statements, handling blocks and other
    ''' statements that create scopes. For efficiency reasons, it does not traverse into
    ''' expressions. This means that blocks within lambdas and queries are not created. 
    ''' Blocks within lambdas are bound by their own <see cref="LocalBinderBuilder"/> when they are 
    ''' analyzed.
    '''
    ''' For reasons of lifetime management, this type is distinct from the <see
    ''' cref="BinderFactory"/> 
    ''' which also creates a map from <see cref="VisualBasicSyntaxNode"/> to <see cref="Binder"/>. That type owns it's binders
    ''' and that type's lifetime is that of the compilation. Therefore we do not store
    ''' binders local to method bodies in that type's cache. 
    ''' </summary>
    Friend Class LocalBinderBuilder
        Inherits VisualBasicSyntaxVisitor

        Private _nodeMap As ImmutableDictionary(Of SyntaxNode, BlockBaseBinder)
        Private _listMap As ImmutableDictionary(Of SyntaxList(Of StatementSyntax), BlockBaseBinder)
        Private ReadOnly _enclosingMethod As MethodSymbol
        Private _containingBinder As Binder

        Public Sub New(enclosingMethod As MethodSymbol)
            _enclosingMethod = enclosingMethod
            _nodeMap = ImmutableDictionary.Create(Of SyntaxNode, BlockBaseBinder)()
            _listMap = ImmutableDictionary.Create(Of SyntaxList(Of StatementSyntax), BlockBaseBinder)()
        End Sub

        Public Sub New(enclosingMethod As MethodSymbol, nodeMap As ImmutableDictionary(Of SyntaxNode, BlockBaseBinder), listMap As ImmutableDictionary(Of SyntaxList(Of StatementSyntax), BlockBaseBinder))
            _enclosingMethod = enclosingMethod
            _nodeMap = nodeMap
            _listMap = listMap
        End Sub

        Public Sub MakeBinder(node As SyntaxNode, containingBinder As Binder)
            Dim oldContainingBinder As Binder = Me._containingBinder
            Me._containingBinder = containingBinder
            MyBase.Visit(node)
            Me._containingBinder = oldContainingBinder
        End Sub

        Public ReadOnly Property NodeToBinderMap As ImmutableDictionary(Of SyntaxNode, BlockBaseBinder)
            Get
                Return _nodeMap
            End Get
        End Property

        Public ReadOnly Property StmtListToBinderMap As ImmutableDictionary(Of SyntaxList(Of StatementSyntax), BlockBaseBinder)
            Get
                Return _listMap
            End Get
        End Property

        ' Visit the statements in a statement list. Does not create a containing binder.
        Private Sub VisitStatementsInList(list As IEnumerable(Of StatementSyntax), currentBinder As BlockBaseBinder)
            For Each n In list
                MakeBinder(n, currentBinder)
            Next
        End Sub

        ' Create a binder using the given statement list.
        Private Sub CreateBinderFromStatementList(list As SyntaxList(Of StatementSyntax), outerBinder As Binder)
            Dim newBinder = New StatementListBinder(outerBinder, list)
            _listMap = _listMap.SetItem(list, newBinder)
            VisitStatementsInList(list, newBinder)
        End Sub

        ' Add a binder to the map.
        Private Sub RememberBinder(node As VisualBasicSyntaxNode, binder As Binder)
            _nodeMap = _nodeMap.SetItem(node, DirectCast(binder, BlockBaseBinder))
        End Sub

        ''' <summary>
        ''' Creates binders for top-level executable statements.
        ''' </summary>
        Public Overrides Sub VisitCompilationUnit(node As CompilationUnitSyntax)
            For Each member In node.Members
                ' Visit methods for non-executable statements are no-ops:
                MakeBinder(member, _containingBinder)
            Next
        End Sub

        Public Overrides Sub VisitMethodBlock(node As MethodBlockSyntax)
            VisitMethodBlockBase(node)
        End Sub

        Public Overrides Sub VisitConstructorBlock(node As ConstructorBlockSyntax)
            VisitMethodBlockBase(node)
        End Sub

        Public Overrides Sub VisitOperatorBlock(node As OperatorBlockSyntax)
            VisitMethodBlockBase(node)
        End Sub

        Public Overrides Sub VisitAccessorBlock(node As AccessorBlockSyntax)
            VisitMethodBlockBase(node)
        End Sub

        Private Sub VisitMethodBlockBase(methodBlock As MethodBlockBaseSyntax)
            ' Get the right kind of exit kind for this method.
            Dim exitKind As SyntaxKind
            Select Case methodBlock.BlockStatement.Kind
                Case SyntaxKind.SubStatement, SyntaxKind.SubNewStatement
                    exitKind = SyntaxKind.ExitSubStatement

                Case SyntaxKind.FunctionStatement
                    exitKind = SyntaxKind.ExitFunctionStatement

                Case SyntaxKind.GetAccessorStatement, SyntaxKind.SetAccessorStatement
                    exitKind = SyntaxKind.ExitPropertyStatement

                Case SyntaxKind.AddHandlerAccessorStatement, SyntaxKind.RemoveHandlerAccessorStatement,
                     SyntaxKind.RaiseEventAccessorStatement

                    ' this is not an Exit kind (there is no such thing as "Exit AddHandler")
                    ' We just use this so that we get a return label.
                    exitKind = SyntaxKind.EventStatement

                Case SyntaxKind.OperatorStatement
                    ' Exit Operator does not exist
                    'exitKind = SyntaxKind.ExitOperatorStatement
                    exitKind = SyntaxKind.OperatorStatement

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(methodBlock.BlockStatement.Kind)
            End Select

            _containingBinder = New ExitableStatementBinder(_containingBinder,
                                                           continueKind:=SyntaxKind.None, exitKind:=exitKind)
            RememberBinder(methodBlock, _containingBinder)

            CreateBinderFromStatementList(methodBlock.Statements, _containingBinder)
        End Sub

        Public Overrides Sub VisitSingleLineLambdaExpression(node As SingleLineLambdaExpressionSyntax)
            If node Is _enclosingMethod.Syntax Then
                Dim exitKind As SyntaxKind

                Select Case node.Kind
                    Case SyntaxKind.SingleLineSubLambdaExpression
                        exitKind = SyntaxKind.ExitSubStatement
                    Case SyntaxKind.SingleLineFunctionLambdaExpression
                        exitKind = SyntaxKind.ExitFunctionStatement
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(node.Kind)
                End Select

                _containingBinder = New ExitableStatementBinder(_containingBinder,
                                                               continueKind:=SyntaxKind.None, exitKind:=exitKind)
                RememberBinder(node, _containingBinder)

                If node.Kind = SyntaxKind.SingleLineSubLambdaExpression Then
                    ' Even though single line sub lambdas only have a single statement.  Create a binder for
                    ' a statement list so that locals can be bound. Note, while locals are not allowed at the top
                    ' level it is useful in the semantic model to bind them.
                    CreateBinderFromStatementList(node.Statements, _containingBinder)
                End If

                MakeBinder(node.Body, _containingBinder)

                Return
            Else
                MyBase.VisitSingleLineLambdaExpression(node) : Return
            End If
        End Sub

        Public Overrides Sub VisitMultiLineLambdaExpression(node As MultiLineLambdaExpressionSyntax)
            If node Is _enclosingMethod.Syntax Then
                Dim exitKind As SyntaxKind

                Select Case node.Kind
                    Case SyntaxKind.MultiLineSubLambdaExpression
                        exitKind = SyntaxKind.ExitSubStatement
                    Case SyntaxKind.MultiLineFunctionLambdaExpression
                        exitKind = SyntaxKind.ExitFunctionStatement
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(node.Kind)
                End Select

                _containingBinder = New ExitableStatementBinder(_containingBinder,
                                                               continueKind:=SyntaxKind.None, exitKind:=exitKind)
                RememberBinder(node, _containingBinder)

                CreateBinderFromStatementList(node.Statements, _containingBinder)

                Return
            Else
                MyBase.VisitMultiLineLambdaExpression(node) : Return
            End If
        End Sub

        Public Overrides Sub VisitWhileBlock(node As WhileBlockSyntax)
            _containingBinder = New ExitableStatementBinder(_containingBinder,
                                                           continueKind:=SyntaxKind.ContinueWhileStatement, exitKind:=SyntaxKind.ExitWhileStatement)
            RememberBinder(node, _containingBinder)

            CreateBinderFromStatementList(node.Statements, _containingBinder)
        End Sub

        Public Overrides Sub VisitUsingBlock(node As UsingBlockSyntax)
            _containingBinder = New UsingBlockBinder(_containingBinder, node)

            RememberBinder(node, _containingBinder)

            CreateBinderFromStatementList(node.Statements, _containingBinder)
        End Sub

        Public Overrides Sub VisitSyncLockBlock(node As SyncLockBlockSyntax)
            CreateBinderFromStatementList(node.Statements, _containingBinder)
        End Sub

        Public Overrides Sub VisitWithBlock(node As WithBlockSyntax)
            _containingBinder = New WithBlockBinder(_containingBinder, node)

            RememberBinder(node, _containingBinder)

            CreateBinderFromStatementList(node.Statements, _containingBinder)
        End Sub

        Public Overrides Sub VisitSingleLineIfStatement(node As SingleLineIfStatementSyntax)
            CreateBinderFromStatementList(node.Statements, _containingBinder)
            MakeBinder(node.ElseClause, _containingBinder)
        End Sub

        Public Overrides Sub VisitSingleLineElseClause(node As SingleLineElseClauseSyntax)
            CreateBinderFromStatementList(node.Statements, _containingBinder)
        End Sub

        Public Overrides Sub VisitMultiLineIfBlock(node As MultiLineIfBlockSyntax)
            CreateBinderFromStatementList(node.Statements, _containingBinder)

            For Each elseifBlock In node.ElseIfBlocks
                MakeBinder(elseifBlock, _containingBinder)
            Next

            MakeBinder(node.ElseBlock, _containingBinder)
        End Sub

        Public Overrides Sub VisitElseBlock(node As ElseBlockSyntax)
            CreateBinderFromStatementList(node.Statements, _containingBinder)
        End Sub

        Public Overrides Sub VisitElseIfBlock(node As ElseIfBlockSyntax)
            CreateBinderFromStatementList(node.Statements, _containingBinder)
        End Sub

        Public Overrides Sub VisitTryBlock(node As TryBlockSyntax)
            _containingBinder = New ExitableStatementBinder(_containingBinder,
                                                           continueKind:=SyntaxKind.None, exitKind:=SyntaxKind.ExitTryStatement)
            RememberBinder(node, _containingBinder)

            CreateBinderFromStatementList(node.Statements, _containingBinder)
            For Each catchBlock In node.CatchBlocks
                MakeBinder(catchBlock, _containingBinder)
            Next
            MakeBinder(node.FinallyBlock, _containingBinder)
        End Sub

        Public Overrides Sub VisitCatchBlock(node As CatchBlockSyntax)
            _containingBinder = New CatchBlockBinder(_containingBinder, node)

            RememberBinder(node, _containingBinder)

            CreateBinderFromStatementList(node.Statements, _containingBinder)
        End Sub

        Public Overrides Sub VisitFinallyBlock(node As FinallyBlockSyntax)
            _containingBinder = New FinallyBlockBinder(_containingBinder)

            RememberBinder(node, _containingBinder)

            CreateBinderFromStatementList(node.Statements, _containingBinder)
        End Sub

        Public Overrides Sub VisitSelectBlock(node As SelectBlockSyntax)
            _containingBinder = New ExitableStatementBinder(_containingBinder,
                                                           continueKind:=SyntaxKind.None, exitKind:=SyntaxKind.ExitSelectStatement)
            RememberBinder(node, _containingBinder)

            For Each caseBlock In node.CaseBlocks
                MakeBinder(caseBlock, _containingBinder)
            Next
        End Sub

        Public Overrides Sub VisitCaseBlock(node As CaseBlockSyntax)
            CreateBinderFromStatementList(node.Statements, _containingBinder)
        End Sub

        Public Overrides Sub VisitDoLoopBlock(node As DoLoopBlockSyntax)
            _containingBinder = New ExitableStatementBinder(_containingBinder,
                                                           continueKind:=SyntaxKind.ContinueDoStatement, exitKind:=SyntaxKind.ExitDoStatement)
            RememberBinder(node, _containingBinder)

            CreateBinderFromStatementList(node.Statements, _containingBinder)
        End Sub

        Public Overrides Sub VisitForBlock(node As ForBlockSyntax)
            _containingBinder = New ForOrForEachBlockBinder(_containingBinder, node)

            RememberBinder(node, _containingBinder)

            CreateBinderFromStatementList(node.Statements, _containingBinder)
        End Sub

        Public Overrides Sub VisitForEachBlock(node As ForEachBlockSyntax)
            _containingBinder = New ForOrForEachBlockBinder(_containingBinder, node)

            RememberBinder(node, _containingBinder)

            CreateBinderFromStatementList(node.Statements, _containingBinder)
        End Sub

    End Class

End Namespace
