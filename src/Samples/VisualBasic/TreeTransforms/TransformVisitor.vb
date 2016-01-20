' *********************************************************
'
' Copyright © Microsoft Corporation
'
' Licensed under the Apache License, Version 2.0 (the
' "License"); you may not use this file except in
' compliance with the License. You may obtain a copy of
' the License at
'
' http://www.apache.org/licenses/LICENSE-2.0 
'
' THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
' OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
' INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
' OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
' PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
'
' See the Apache 2 License for the specific language
' governing permissions and limitations under the License.
'
' *********************************************************

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TreeTransformsVB.Transforms

Public Class TransformVisitor
    Inherits VisualBasicSyntaxRewriter

    Private ReadOnly tree As SyntaxTree
    Private transformKind As transformKind

    Public Sub New(tree As SyntaxTree, transformKind As transformKind)
        Me.tree = tree
        Me.transformKind = transformKind
    End Sub

    Public Overrides Function VisitLiteralExpression(node As LiteralExpressionSyntax) As SyntaxNode
        node = DirectCast(MyBase.VisitLiteralExpression(node), LiteralExpressionSyntax)
        Dim token = node.Token

        If (transformKind = transformKind.TrueToFalse) AndAlso (node.Kind = SyntaxKind.TrueLiteralExpression) Then
            Dim newToken = SyntaxFactory.Token(token.LeadingTrivia, SyntaxKind.FalseKeyword, token.TrailingTrivia)
            Return SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression, newToken)
        End If

        If (transformKind = transformKind.FalseToTrue) AndAlso (node.Kind = SyntaxKind.FalseLiteralExpression) Then
            Dim newToken = SyntaxFactory.Token(token.LeadingTrivia, SyntaxKind.TrueKeyword, token.TrailingTrivia)
            Return SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression, newToken)
        End If

        Return node
    End Function

    Public Overrides Function VisitPredefinedType(node As PredefinedTypeSyntax) As SyntaxNode
        node = DirectCast(MyBase.VisitPredefinedType(node), PredefinedTypeSyntax)
        Dim token = node.Keyword

        If (transformKind = transformKind.IntTypeToLongType) AndAlso (token.Kind = SyntaxKind.IntegerKeyword) Then
            Dim longToken = SyntaxFactory.Token(token.LeadingTrivia, SyntaxKind.LongKeyword, token.TrailingTrivia)
            Return SyntaxFactory.PredefinedType(longToken)
        End If

        Return node
    End Function

    Public Overrides Function VisitModuleBlock(ByVal node As ModuleBlockSyntax) As SyntaxNode
        Return MyBase.VisitModuleBlock(node)
    End Function

    Public Overrides Function VisitClassBlock(ByVal node As ClassBlockSyntax) As SyntaxNode
        Dim classStatement = node.ClassStatement
        Dim classKeyword = classStatement.ClassKeyword
        Dim endStatement = node.EndClassStatement
        Dim endBlockKeyword = endStatement.BlockKeyword

        If transformKind = transformKind.ClassToStructure Then
            Dim structureKeyword = SyntaxFactory.Token(classKeyword.LeadingTrivia, SyntaxKind.StructureKeyword, classKeyword.TrailingTrivia)
            Dim endStructureKeyword = SyntaxFactory.Token(endBlockKeyword.LeadingTrivia, SyntaxKind.StructureKeyword, endBlockKeyword.TrailingTrivia)
            Dim newStructureStatement = SyntaxFactory.StructureStatement(classStatement.AttributeLists, classStatement.Modifiers, structureKeyword, classStatement.Identifier, classStatement.TypeParameterList)
            Dim newEndStatement = SyntaxFactory.EndStructureStatement(endStatement.EndKeyword, endStructureKeyword)
            Return SyntaxFactory.StructureBlock(newStructureStatement, node.Inherits, node.Implements, node.Members, newEndStatement)
        End If

        Return node
    End Function

    Public Overrides Function VisitStructureBlock(ByVal node As StructureBlockSyntax) As SyntaxNode
        Dim structureStatement = node.StructureStatement
        Dim structureKeyword = structureStatement.StructureKeyword
        Dim endStatement = node.EndStructureStatement
        Dim endBlockKeyword = endStatement.BlockKeyword

        If transformKind = transformKind.StructureToClass Then
            Dim classKeyword = SyntaxFactory.Token(structureKeyword.LeadingTrivia, SyntaxKind.ClassKeyword, structureKeyword.TrailingTrivia)
            Dim endClassKeyword = SyntaxFactory.Token(endBlockKeyword.LeadingTrivia, SyntaxKind.ClassKeyword, endBlockKeyword.TrailingTrivia)
            Dim newClassStatement = SyntaxFactory.ClassStatement(structureStatement.AttributeLists, structureStatement.Modifiers, classKeyword, structureStatement.Identifier, structureStatement.TypeParameterList)
            Dim newEndStatement = SyntaxFactory.EndClassStatement(endStatement.EndKeyword, endClassKeyword)
            Return SyntaxFactory.ClassBlock(newClassStatement, node.Inherits, node.Implements, node.Members, newEndStatement)
        End If

        Return node
    End Function

    Public Overrides Function VisitInterfaceBlock(ByVal node As InterfaceBlockSyntax) As SyntaxNode
        Return MyBase.VisitInterfaceBlock(node)
    End Function

    Public Overrides Function VisitOrdering(node As OrderingSyntax) As SyntaxNode
        node = DirectCast(MyBase.VisitOrdering(node), OrderingSyntax)
        Dim orderingKind = node.AscendingOrDescendingKeyword

        If (transformKind = transformKind.OrderByAscToOrderByDesc) AndAlso (orderingKind.Kind = SyntaxKind.AscendingKeyword) Then
            Dim descToken = SyntaxFactory.Token(orderingKind.LeadingTrivia, SyntaxKind.DescendingKeyword, orderingKind.TrailingTrivia)
            Return SyntaxFactory.Ordering(SyntaxKind.DescendingOrdering, node.Expression, descToken)
        End If

        If (transformKind = transformKind.OrderByDescToOrderByAsc) AndAlso (orderingKind.Kind = SyntaxKind.DescendingKeyword) Then
            Dim ascToken = SyntaxFactory.Token(orderingKind.LeadingTrivia, SyntaxKind.AscendingKeyword, orderingKind.TrailingTrivia)
            Return SyntaxFactory.Ordering(SyntaxKind.AscendingOrdering, node.Expression, ascToken)
        End If

        Return node
    End Function

    Public Overrides Function VisitAssignmentStatement(node As AssignmentStatementSyntax) As SyntaxNode
        node = DirectCast(MyBase.VisitAssignmentStatement(node), AssignmentStatementSyntax)
        Dim left = node.Left
        Dim right = node.Right
        Dim operatorToken = node.OperatorToken

        If (transformKind = transformKind.AddAssignmentToAssignment) AndAlso (node.Kind = SyntaxKind.AddAssignmentStatement) Then
            Dim equalsToken = SyntaxFactory.Token(operatorToken.LeadingTrivia, SyntaxKind.EqualsToken, operatorToken.TrailingTrivia)
            Dim newLeft = left.WithLeadingTrivia(SyntaxTriviaList.Empty)
            Dim plusToken = SyntaxFactory.Token(operatorToken.LeadingTrivia, SyntaxKind.PlusToken, operatorToken.TrailingTrivia)
            Dim addExpression = SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression, newLeft, plusToken, right)

            Return SyntaxFactory.SimpleAssignmentStatement(left, equalsToken, addExpression)
        End If

        Return node
    End Function

    Public Overrides Function VisitDirectCastExpression(node As DirectCastExpressionSyntax) As SyntaxNode
        node = DirectCast(MyBase.VisitDirectCastExpression(node), DirectCastExpressionSyntax)
        Dim keyword = node.Keyword

        If (transformKind = transformKind.DirectCastToTryCast) AndAlso (node.Kind = SyntaxKind.DirectCastExpression) Then
            Dim tryCastKeyword = SyntaxFactory.Token(keyword.LeadingTrivia, SyntaxKind.TryCastKeyword, keyword.TrailingTrivia)

            Return SyntaxFactory.TryCastExpression(tryCastKeyword, node.OpenParenToken, node.Expression, node.CommaToken, node.Type, node.CloseParenToken)
        End If

        Return node
    End Function

    Public Overrides Function VisitTryCastExpression(node As TryCastExpressionSyntax) As SyntaxNode
        node = DirectCast(MyBase.VisitTryCastExpression(node), TryCastExpressionSyntax)
        Dim keyword = node.Keyword

        If (transformKind = transformKind.TryCastToDirectCast) AndAlso (node.Kind = SyntaxKind.TryCastExpression) Then
            Dim directCastKeyword = SyntaxFactory.Token(keyword.LeadingTrivia, SyntaxKind.DirectCastKeyword, keyword.TrailingTrivia)

            Return SyntaxFactory.DirectCastExpression(directCastKeyword, node.OpenParenToken, node.Expression, node.CommaToken, node.Type, node.CloseParenToken)
        End If

        Return node
    End Function

    Public Overrides Function VisitVariableDeclarator(node As VariableDeclaratorSyntax) As SyntaxNode
        node = DirectCast(MyBase.VisitVariableDeclarator(node), VariableDeclaratorSyntax)
        Dim names = node.Names
        Dim asClause = node.AsClause
        Dim initializer = node.Initializer

        If (transformKind = transformKind.InitVariablesToNothing) AndAlso (initializer Is Nothing) AndAlso (names.Count = 1) Then
            Dim newEqualsToken = SyntaxFactory.Token(SyntaxFactory.TriviaList(SyntaxFactory.WhitespaceTrivia(" ")), SyntaxKind.EqualsToken, SyntaxFactory.TriviaList(SyntaxFactory.WhitespaceTrivia(" ")))
            Dim newNothingToken = SyntaxFactory.Token(SyntaxKind.NothingKeyword)
            Dim newNothingExpression = SyntaxFactory.NothingLiteralExpression(newNothingToken)
            Dim newInitializer = SyntaxFactory.EqualsValue(newEqualsToken, newNothingExpression)

            Return node.Update(node.Names, node.AsClause, newInitializer)
        End If

        Return node
    End Function

    Public Overrides Function VisitParameter(node As ParameterSyntax) As SyntaxNode
        node = DirectCast(MyBase.VisitParameter(node), ParameterSyntax)

        If (transformKind = transformKind.ByRefParamToByValParam) OrElse (transformKind = transformKind.ByValParamToByRefParam) Then
            Dim listOfModifiers = New List(Of SyntaxToken)

            For Each modifier In node.Modifiers
                Dim modifierToken = modifier

                If (modifier.Kind = SyntaxKind.ByValKeyword) AndAlso (transformKind = transformKind.ByValParamToByRefParam) Then
                    modifierToken = SyntaxFactory.Token(modifierToken.LeadingTrivia, SyntaxKind.ByRefKeyword, modifierToken.TrailingTrivia)
                ElseIf (modifier.Kind = SyntaxKind.ByRefKeyword) AndAlso (transformKind = TransformKind.ByRefParamToByValParam) Then
                    modifierToken = SyntaxFactory.Token(modifierToken.LeadingTrivia, SyntaxKind.ByValKeyword, modifierToken.TrailingTrivia)
                End If

                listOfModifiers.Add(modifierToken)
            Next

            Dim newModifiers = SyntaxFactory.TokenList(listOfModifiers)
            Return SyntaxFactory.Parameter(node.AttributeLists, newModifiers, node.Identifier, node.AsClause, node.Default)
        End If

        Return node
    End Function

    Public Overrides Function VisitDoLoopBlock(node As DoLoopBlockSyntax) As SyntaxNode
        node = DirectCast(MyBase.VisitDoLoopBlock(node), DoLoopBlockSyntax)
        Dim beginLoop = node.DoStatement
        Dim endLoop = node.LoopStatement

        If (transformKind = transformKind.DoBottomTestToDoTopTest) AndAlso (node.Kind = SyntaxKind.DoLoopWhileBlock OrElse node.Kind = SyntaxKind.DoLoopUntilBlock) Then
            Dim newDoKeyword = SyntaxFactory.Token(beginLoop.DoKeyword.LeadingTrivia, SyntaxKind.DoKeyword, endLoop.LoopKeyword.TrailingTrivia)
            Dim newLoopKeyword = SyntaxFactory.Token(endLoop.LoopKeyword.LeadingTrivia, endLoop.LoopKeyword.Kind, beginLoop.DoKeyword.TrailingTrivia)
            Dim newBegin = SyntaxFactory.DoStatement(If(endLoop.Kind = SyntaxKind.LoopWhileStatement, SyntaxKind.DoWhileStatement, SyntaxKind.DoUntilStatement), newDoKeyword, endLoop.WhileOrUntilClause)
            Dim newEnd = SyntaxFactory.SimpleLoopStatement().WithLoopKeyword(newLoopKeyword)
            Return SyntaxFactory.DoLoopBlock(If(endLoop.Kind = SyntaxKind.LoopWhileStatement, SyntaxKind.DoWhileLoopBlock, SyntaxKind.DoUntilLoopBlock), newBegin, node.Statements, newEnd)
        End If

        If (transformKind = transformKind.DoTopTestToDoBottomTest) AndAlso (node.Kind = SyntaxKind.DoWhileLoopBlock OrElse node.Kind = SyntaxKind.DoUntilLoopBlock) Then
            Dim newDoKeyword = SyntaxFactory.Token(beginLoop.DoKeyword.LeadingTrivia, SyntaxKind.DoKeyword, endLoop.LoopKeyword.TrailingTrivia)
            Dim newLoopKeyword = SyntaxFactory.Token(endLoop.LoopKeyword.LeadingTrivia, endLoop.LoopKeyword.Kind, beginLoop.DoKeyword.TrailingTrivia)
            Dim newBegin = SyntaxFactory.SimpleDoStatement().WithDoKeyword(newDoKeyword)
            Dim newEnd = SyntaxFactory.LoopStatement(If(beginLoop.Kind = SyntaxKind.DoWhileStatement, SyntaxKind.LoopWhileStatement, SyntaxKind.LoopUntilStatement), newLoopKeyword, beginLoop.WhileOrUntilClause)
            Return SyntaxFactory.DoLoopBlock(If(beginLoop.Kind = SyntaxKind.DoWhileStatement, SyntaxKind.DoLoopWhileBlock, SyntaxKind.DoLoopUntilBlock), newBegin, node.Statements, newEnd)
        End If

        If (transformKind = transformKind.DoWhileTopTestToWhile) AndAlso (node.Kind = SyntaxKind.DoWhileLoopBlock OrElse node.Kind = SyntaxKind.DoUntilLoopBlock) Then
            Dim endKeyword = SyntaxFactory.Token(endLoop.LoopKeyword.LeadingTrivia, SyntaxKind.EndKeyword)
            Dim endWhileKeyword = SyntaxFactory.Token(SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" ")), SyntaxKind.WhileKeyword, endLoop.LoopKeyword.TrailingTrivia)
            Dim endWhile = SyntaxFactory.EndWhileStatement(endKeyword, endWhileKeyword)
            Dim beginWhileKeyword = SyntaxFactory.Token(beginLoop.DoKeyword.LeadingTrivia, SyntaxKind.WhileKeyword, beginLoop.DoKeyword.TrailingTrivia)
            Dim whileStatement = SyntaxFactory.WhileStatement(beginWhileKeyword, beginLoop.WhileOrUntilClause.Condition)
            Return SyntaxFactory.WhileBlock(whileStatement, node.Statements, endWhile)
        End If

        Return node
    End Function

    Public Overrides Function VisitWhileBlock(node As WhileBlockSyntax) As SyntaxNode
        node = DirectCast(MyBase.VisitWhileBlock(node), WhileBlockSyntax)
        Dim beginWhile = node.WhileStatement
        Dim endWhile = node.EndWhileStatement

        If transformKind = transformKind.WhileToDoWhileTopTest Then
            Dim doKeyword = SyntaxFactory.Token(beginWhile.WhileKeyword.LeadingTrivia, SyntaxKind.DoKeyword, beginWhile.WhileKeyword.TrailingTrivia)
            Dim whileKeyword = SyntaxFactory.Token(SyntaxKind.WhileKeyword, SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" ")))
            Dim whileClause = SyntaxFactory.WhileOrUntilClause(SyntaxKind.WhileClause, whileKeyword, beginWhile.Condition)
            Dim loopKeyword = SyntaxFactory.Token(endWhile.GetLeadingTrivia(), SyntaxKind.LoopKeyword, endWhile.GetTrailingTrivia())
            Dim endLoop = SyntaxFactory.SimpleLoopStatement().WithLoopKeyword(loopKeyword)
            Dim doStatement = SyntaxFactory.DoWhileStatement(doKeyword, whileClause)

            Return SyntaxFactory.DoWhileLoopBlock(doStatement, node.Statements, endLoop)
        End If

        Return node
    End Function

    Public Overrides Function VisitExitStatement(node As ExitStatementSyntax) As SyntaxNode
        node = DirectCast(MyBase.VisitExitStatement(node), ExitStatementSyntax)
        Dim blockKeyword = node.BlockKeyword
        Dim exitKeyword = node.ExitKeyword

        If (transformKind = transformKind.WhileToDoWhileTopTest) AndAlso (node.Kind = SyntaxKind.ExitWhileStatement) Then
            Dim doKeyword = SyntaxFactory.Token(blockKeyword.LeadingTrivia, SyntaxKind.DoKeyword, blockKeyword.TrailingTrivia)
            Return SyntaxFactory.ExitDoStatement(exitKeyword, doKeyword)
        End If

        If (transformKind = transformKind.DoWhileTopTestToWhile) AndAlso (node.Kind = SyntaxKind.ExitDoStatement) Then
            Dim parent = node.Parent

            'Update Exit Do to Exit While only for Do-While and NOT for Do-Until
            While (parent IsNot Nothing) AndAlso (TryCast(parent, DoLoopBlockSyntax) Is Nothing)
                parent = parent.Parent
            End While

            Dim doBlock = TryCast(parent, DoLoopBlockSyntax)

            If doBlock IsNot Nothing Then
                If (doBlock.DoStatement.WhileOrUntilClause IsNot Nothing) AndAlso (doBlock.DoStatement.WhileOrUntilClause.Kind = SyntaxKind.WhileClause) Then
                    Dim whileKeyword = SyntaxFactory.Token(blockKeyword.LeadingTrivia, SyntaxKind.WhileKeyword, blockKeyword.TrailingTrivia)
                    Return SyntaxFactory.ExitWhileStatement(exitKeyword, whileKeyword)
                End If
            End If
        End If

        Return node
    End Function

    Public Overrides Function VisitSingleLineIfStatement(node As SingleLineIfStatementSyntax) As SyntaxNode
        node = DirectCast(MyBase.VisitSingleLineIfStatement(node), SingleLineIfStatementSyntax)
        Dim elseClause = node.ElseClause

        If transformKind = transformKind.SingleLineIfToMultiLineIf Then
            Dim leadingTriviaList = SyntaxFactory.TriviaList(node.GetLeadingTrivia().LastOrDefault(), SyntaxFactory.WhitespaceTrivia("    "))
            Dim newIfStatement = SyntaxFactory.IfStatement(node.IfKeyword, node.Condition, node.ThenKeyword)
            Dim newIfStatements = GetSequentialListOfStatements(node.Statements, leadingTriviaList)

            Dim newElseBlock As ElseBlockSyntax = Nothing
            If elseClause IsNot Nothing Then
                Dim newElseKeyword = SyntaxFactory.Token(node.GetLeadingTrivia(), SyntaxKind.ElseKeyword)
                Dim newElseStmt = SyntaxFactory.ElseStatement(newElseKeyword)
                Dim newStatementsElsePart = GetSequentialListOfStatements(elseClause.Statements, leadingTriviaList)
                newElseBlock = SyntaxFactory.ElseBlock(newElseStmt, newStatementsElsePart)
            End If

            Dim whiteSpaceTrivia = SyntaxFactory.WhitespaceTrivia(" ")
            Dim endKeyword = SyntaxFactory.Token(node.GetLeadingTrivia(), SyntaxKind.EndKeyword)
            Dim blockKeyword = SyntaxFactory.Token(SyntaxFactory.TriviaList(whiteSpaceTrivia), SyntaxKind.IfKeyword)
            Dim newEndIf = SyntaxFactory.EndIfStatement(endKeyword, blockKeyword)

            Return SyntaxFactory.MultiLineIfBlock(newIfStatement, newIfStatements, Nothing, newElseBlock, newEndIf)
        End If

        Return node
    End Function

    Private Function GetSequentialListOfStatements(statements As SyntaxList(Of StatementSyntax), stmtLeadingTrivia As SyntaxTriviaList) As SyntaxList(Of StatementSyntax)
        Dim newStatementList = New List(Of StatementSyntax)

        For Each statement In statements
            Dim oldFirst = statement.GetFirstToken(includeZeroWidth:=True)
            Dim newFirst = oldFirst.WithLeadingTrivia(stmtLeadingTrivia)
            newStatementList.Add(statement.ReplaceToken(oldFirst, newFirst))
        Next

        Dim listOfSeparators = New List(Of SyntaxToken)

        For i = 0 To statements.Count - 1
            listOfSeparators.Add(SyntaxFactory.Token(SyntaxKind.StatementTerminatorToken))
        Next

        Return SyntaxFactory.List(newStatementList)
    End Function

End Class
