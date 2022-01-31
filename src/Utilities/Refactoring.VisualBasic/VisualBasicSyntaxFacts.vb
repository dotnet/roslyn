' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Analyzer.Utilities.Extensions
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Global.Analyzer.Utilities

    Friend NotInheritable Class VisualBasicSyntaxFacts
        Inherits AbstractSyntaxFacts
        Implements ISyntaxFacts

        Public Shared ReadOnly Property Instance As New VisualBasicSyntaxFacts()

        Private Sub New()
        End Sub

        Public Overrides ReadOnly Property SyntaxKinds As ISyntaxKinds Implements ISyntaxFacts.SyntaxKinds
            Get
                Return VisualBasicSyntaxKinds.Instance
            End Get
        End Property

        Public Sub GetPartsOfAssignmentExpressionOrStatement(statement As SyntaxNode, ByRef left As SyntaxNode, ByRef operatorToken As SyntaxToken, ByRef right As SyntaxNode) Implements ISyntaxFacts.GetPartsOfAssignmentExpressionOrStatement
            Dim assignment = DirectCast(statement, AssignmentStatementSyntax)
            left = assignment.Left
            operatorToken = assignment.OperatorToken
            right = assignment.Right
        End Sub

        Public Overrides Function GetAttributeLists(node As SyntaxNode) As SyntaxList(Of SyntaxNode) Implements ISyntaxFacts.GetAttributeLists
            Return node.GetAttributeLists()
        End Function

        Public Function GetExpressionOfExpressionStatement(node As SyntaxNode) As SyntaxNode Implements ISyntaxFacts.GetExpressionOfExpressionStatement
            Return DirectCast(node, ExpressionStatementSyntax).Expression
        End Function

        Public Function IsSimpleAssignmentStatement(statement As SyntaxNode) As Boolean Implements ISyntaxFacts.IsSimpleAssignmentStatement
            Return statement.IsKind(SyntaxKind.SimpleAssignmentStatement)
        End Function

        Public Function GetVariablesOfLocalDeclarationStatement(node As SyntaxNode) As SeparatedSyntaxList(Of SyntaxNode) Implements ISyntaxFacts.GetVariablesOfLocalDeclarationStatement
            Return DirectCast(node, LocalDeclarationStatementSyntax).Declarators
        End Function

        Public Function GetInitializerOfVariableDeclarator(node As SyntaxNode) As SyntaxNode Implements ISyntaxFacts.GetInitializerOfVariableDeclarator
            Return DirectCast(node, VariableDeclaratorSyntax).Initializer
        End Function

        Public Function GetValueOfEqualsValueClause(node As SyntaxNode) As SyntaxNode Implements ISyntaxFacts.GetValueOfEqualsValueClause
            Return DirectCast(node, EqualsValueSyntax).Value
        End Function

        Public Function IsOnTypeHeader(
                root As SyntaxNode,
                position As Integer,
                fullHeader As Boolean,
                ByRef typeDeclaration As SyntaxNode) As Boolean Implements ISyntaxFacts.IsOnTypeHeader
            Dim typeBlock = TryGetAncestorForLocation(Of TypeBlockSyntax)(root, position)
            If typeBlock Is Nothing Then
                Return Nothing
            End If

            Dim typeStatement = typeBlock.BlockStatement
            typeDeclaration = typeStatement

            Dim lastToken = If(typeStatement.TypeParameterList?.GetLastToken(), typeStatement.Identifier)
            If fullHeader Then
                lastToken = If(typeBlock.Implements.LastOrDefault()?.GetLastToken(),
                            If(typeBlock.Inherits.LastOrDefault()?.GetLastToken(),
                               lastToken))
            End If

            Return IsOnHeader(root, position, typeBlock, lastToken)
        End Function

        Public Function IsOnPropertyDeclarationHeader(root As SyntaxNode, position As Integer, ByRef propertyDeclaration As SyntaxNode) As Boolean Implements ISyntaxFacts.IsOnPropertyDeclarationHeader
            Dim node = TryGetAncestorForLocation(Of PropertyStatementSyntax)(root, position)
            propertyDeclaration = node

            If propertyDeclaration Is Nothing Then
                Return False
            End If

            If node.AsClause IsNot Nothing Then
                Return IsOnHeader(root, position, node, node.AsClause)
            End If

            Return IsOnHeader(root, position, node, node.Identifier)
        End Function

        Public Function IsOnParameterHeader(root As SyntaxNode, position As Integer, ByRef parameter As SyntaxNode) As Boolean Implements ISyntaxFacts.IsOnParameterHeader
            Dim node = TryGetAncestorForLocation(Of ParameterSyntax)(root, position)
            parameter = node

            If parameter Is Nothing Then
                Return False
            End If

            Return IsOnHeader(root, position, node, node)
        End Function

        Public Function IsOnMethodHeader(root As SyntaxNode, position As Integer, ByRef method As SyntaxNode) As Boolean Implements ISyntaxFacts.IsOnMethodHeader
            Dim node = TryGetAncestorForLocation(Of MethodStatementSyntax)(root, position)
            method = node

            If method Is Nothing Then
                Return False
            End If

            If node.HasReturnType() Then
                Return IsOnHeader(root, position, method, node.GetReturnType())
            End If

            If node.ParameterList IsNot Nothing Then
                Return IsOnHeader(root, position, method, node.ParameterList)
            End If

            Return IsOnHeader(root, position, node, node)
        End Function

        Public Function IsOnLocalFunctionHeader(root As SyntaxNode, position As Integer, ByRef localFunction As SyntaxNode) As Boolean Implements ISyntaxFacts.IsOnLocalFunctionHeader
            ' No local functions in VisualBasic
            Return False
        End Function

        Public Function IsOnLocalDeclarationHeader(root As SyntaxNode, position As Integer, ByRef localDeclaration As SyntaxNode) As Boolean Implements ISyntaxFacts.IsOnLocalDeclarationHeader
            Dim node = TryGetAncestorForLocation(Of LocalDeclarationStatementSyntax)(root, position)
            localDeclaration = node

            If localDeclaration Is Nothing Then
                Return False
            End If

            Dim initializersExpressions = node.Declarators.
                Where(Function(d) d.Initializer IsNot Nothing).
                Select(Function(initialized) initialized.Initializer.Value).
                ToImmutableArray()
            Return IsOnHeader(root, position, node, node, initializersExpressions)
        End Function

        Public Function IsOnIfStatementHeader(root As SyntaxNode, position As Integer, ByRef ifStatement As SyntaxNode) As Boolean Implements ISyntaxFacts.IsOnIfStatementHeader
            ifStatement = Nothing

            Dim multipleLineNode = TryGetAncestorForLocation(Of MultiLineIfBlockSyntax)(root, position)
            If multipleLineNode IsNot Nothing Then
                ifStatement = multipleLineNode
                Return IsOnHeader(root, position, multipleLineNode.IfStatement, multipleLineNode.IfStatement)
            End If

            Dim singleLineNode = TryGetAncestorForLocation(Of SingleLineIfStatementSyntax)(root, position)
            If singleLineNode IsNot Nothing Then
                ifStatement = singleLineNode
                Return IsOnHeader(root, position, singleLineNode, singleLineNode.Condition)
            End If

            Return False
        End Function

        Public Function IsOnForeachHeader(root As SyntaxNode, position As Integer, ByRef foreachStatement As SyntaxNode) As Boolean Implements ISyntaxFacts.IsOnForeachHeader
            Dim node = TryGetAncestorForLocation(Of ForEachBlockSyntax)(root, position)
            foreachStatement = node

            If foreachStatement Is Nothing Then
                Return False
            End If

            Return IsOnHeader(root, position, node, node.ForEachStatement)
        End Function

    End Class

End Namespace
