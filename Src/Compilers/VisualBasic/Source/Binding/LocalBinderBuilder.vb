' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Private _nodeMap As ImmutableDictionary(Of VisualBasicSyntaxNode, BlockBaseBinder)
        Private _listMap As ImmutableDictionary(Of SyntaxList(Of StatementSyntax), BlockBaseBinder)
        Private _enclosingMethod As MethodSymbol
        Private containingBinder As Binder

        Public Sub New(enclosingMethod As MethodSymbol)
            _enclosingMethod = enclosingMethod
            _nodeMap = ImmutableDictionary.Create(Of VisualBasicSyntaxNode, BlockBaseBinder)()
            _listMap = ImmutableDictionary.Create(Of SyntaxList(Of StatementSyntax), BlockBaseBinder)()
        End Sub

        Public Sub New(enclosingMethod As MethodSymbol, nodeMap As ImmutableDictionary(Of VisualBasicSyntaxNode, BlockBaseBinder), listMap As ImmutableDictionary(Of SyntaxList(Of StatementSyntax), BlockBaseBinder))
            _enclosingMethod = enclosingMethod
            _nodeMap = nodeMap
            _listMap = listMap
        End Sub

        Public Sub MakeBinder(node As VisualBasicSyntaxNode, containingBinder As Binder)
            Dim oldContainingBinder As Binder = Me.containingBinder
            Me.containingBinder = containingBinder
            MyBase.Visit(node)
            Me.containingBinder = oldContainingBinder
        End Sub

        Public ReadOnly Property NodeToBinderMap As ImmutableDictionary(Of VisualBasicSyntaxNode, BlockBaseBinder)
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
                MakeBinder(member, containingBinder)
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
            Select Case methodBlock.Begin.Kind
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
                    Throw ExceptionUtilities.UnexpectedValue(methodBlock.Begin.Kind)
            End Select

            containingBinder = New ExitableStatementBinder(containingBinder,
                                                           continueKind:=SyntaxKind.None, exitKind:=exitKind)
            RememberBinder(methodBlock, containingBinder)

            CreateBinderFromStatementList(methodBlock.Statements, containingBinder)
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

                containingBinder = New ExitableStatementBinder(containingBinder,
                                                               continueKind:=SyntaxKind.None, exitKind:=exitKind)
                RememberBinder(node, containingBinder)

                If node.Kind = SyntaxKind.SingleLineSubLambdaExpression Then
                    ' Even though single line sub lambdas only have a single statement.  Create a binder for
                    ' a statement list so that locals can be bound. Note, while locals are not allowed at the top
                    ' level it is useful in the semantic model to bind them.
                    CreateBinderFromStatementList(node.Statements, containingBinder)
                End If

                MakeBinder(node.Body, containingBinder)

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

                containingBinder = New ExitableStatementBinder(containingBinder,
                                                               continueKind:=SyntaxKind.None, exitKind:=exitKind)
                RememberBinder(node, containingBinder)

                CreateBinderFromStatementList(node.Statements, containingBinder)

                Return
            Else
                MyBase.VisitMultiLineLambdaExpression(node) : Return
            End If
        End Sub

        Public Overrides Sub VisitWhileBlock(node As WhileBlockSyntax)
            containingBinder = New ExitableStatementBinder(containingBinder,
                                                           continueKind:=SyntaxKind.ContinueWhileStatement, exitKind:=SyntaxKind.ExitWhileStatement)
            RememberBinder(node, containingBinder)

            CreateBinderFromStatementList(node.Statements, containingBinder)
        End Sub

        Public Overrides Sub VisitUsingBlock(node As UsingBlockSyntax)
            containingBinder = New UsingBlockBinder(containingBinder, node)

            RememberBinder(node, containingBinder)

            CreateBinderFromStatementList(node.Statements, containingBinder)
        End Sub

        Public Overrides Sub VisitSyncLockBlock(node As SyncLockBlockSyntax)
            CreateBinderFromStatementList(node.Statements, containingBinder)
        End Sub

        Public Overrides Sub VisitWithBlock(node As WithBlockSyntax)
            containingBinder = New WithBlockBinder(containingBinder, node)

            RememberBinder(node, containingBinder)

            CreateBinderFromStatementList(node.Statements, containingBinder)
        End Sub

        Public Overrides Sub VisitSingleLineIfStatement(node As SingleLineIfStatementSyntax)
            MakeBinder(node.IfPart, containingBinder)
            MakeBinder(node.ElsePart, containingBinder)
        End Sub

        Public Overrides Sub VisitSingleLineIfPart(node As SingleLineIfPartSyntax)
            CreateBinderFromStatementList(node.Statements, containingBinder)
        End Sub

        Public Overrides Sub VisitSingleLineElsePart(node As SingleLineElsePartSyntax)
            CreateBinderFromStatementList(node.Statements, containingBinder)
        End Sub

        Public Overrides Sub VisitMultiLineIfBlock(node As MultiLineIfBlockSyntax)
            MakeBinder(node.IfPart, containingBinder)
            For Each elseifPart In node.ElseIfParts
                MakeBinder(elseifPart, containingBinder)
            Next
            MakeBinder(node.ElsePart, containingBinder)
        End Sub

        Public Overrides Sub VisitIfPart(node As IfPartSyntax)
            CreateBinderFromStatementList(node.Statements, containingBinder)
        End Sub

        Public Overrides Sub VisitElsePart(node As ElsePartSyntax)
            CreateBinderFromStatementList(node.Statements, containingBinder)
        End Sub

        Public Overrides Sub VisitTryBlock(node As TryBlockSyntax)
            containingBinder = New ExitableStatementBinder(containingBinder,
                                                           continueKind:=SyntaxKind.NothingKeyword, exitKind:=SyntaxKind.ExitTryStatement)
            RememberBinder(node, containingBinder)

            MakeBinder(node.TryPart, containingBinder)
            For Each catchPart In node.CatchParts
                MakeBinder(catchPart, containingBinder)
            Next
            MakeBinder(node.FinallyPart, containingBinder)
        End Sub

        Public Overrides Sub VisitTryPart(node As TryPartSyntax)
            CreateBinderFromStatementList(node.Statements, containingBinder)
        End Sub

        Public Overrides Sub VisitCatchPart(node As CatchPartSyntax)
            containingBinder = New CatchBlockBinder(containingBinder, node)

            RememberBinder(node, containingBinder)

            CreateBinderFromStatementList(node.Statements, containingBinder)
        End Sub

        Public Overrides Sub VisitFinallyPart(node As FinallyPartSyntax)
            containingBinder = New FinallyBlockBinder(containingBinder)

            RememberBinder(node, containingBinder)

            CreateBinderFromStatementList(node.Statements, containingBinder)
        End Sub

        Public Overrides Sub VisitSelectBlock(node As SelectBlockSyntax)
            containingBinder = New ExitableStatementBinder(containingBinder,
                                                           continueKind:=SyntaxKind.None, exitKind:=SyntaxKind.ExitSelectStatement)
            RememberBinder(node, containingBinder)

            For Each caseBlock In node.CaseBlocks
                MakeBinder(caseBlock, containingBinder)
            Next
        End Sub

        Public Overrides Sub VisitCaseBlock(node As CaseBlockSyntax)
            CreateBinderFromStatementList(node.Statements, containingBinder)
        End Sub

        Public Overrides Sub VisitDoLoopBlock(node As DoLoopBlockSyntax)
            containingBinder = New ExitableStatementBinder(containingBinder,
                                                           continueKind:=SyntaxKind.ContinueDoStatement, exitKind:=SyntaxKind.ExitDoStatement)
            RememberBinder(node, containingBinder)

            CreateBinderFromStatementList(node.Statements, containingBinder)
        End Sub

        Public Overrides Sub VisitForBlock(node As ForBlockSyntax)
            containingBinder = New ForBlockBinder(containingBinder, node)

            RememberBinder(node, containingBinder)

            CreateBinderFromStatementList(node.Statements, containingBinder)
        End Sub

    End Class

End Namespace
