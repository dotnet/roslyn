' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageService
    Friend Class VisualBasicHeaderFacts
        Inherits AbstractHeaderFacts

        Public Shared ReadOnly Instance As IHeaderFacts = New VisualBasicHeaderFacts()

        Protected Sub New()
        End Sub

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts = VisualBasicSyntaxFacts.Instance

        Public Overrides Function IsOnTypeHeader(
                root As SyntaxNode,
                position As Integer,
                fullHeader As Boolean,
                ByRef typeDeclaration As SyntaxNode) As Boolean
            Dim typeBlock = TryGetAncestorForLocation(Of TypeBlockSyntax)(root, position, typeDeclaration)
            If typeBlock Is Nothing Then
                Return Nothing
            End If

            Dim typeStatement = typeBlock.BlockStatement

            Dim lastToken = If(typeStatement.TypeParameterList?.GetLastToken(), typeStatement.Identifier)
            If fullHeader Then
                lastToken = If(typeBlock.Implements.LastOrDefault()?.GetLastToken(),
                            If(typeBlock.Inherits.LastOrDefault()?.GetLastToken(),
                               lastToken))
            End If

            Return IsOnHeader(root, position, typeBlock, lastToken)
        End Function

        Public Overrides Function IsOnPropertyDeclarationHeader(root As SyntaxNode, position As Integer, ByRef propertyDeclaration As SyntaxNode) As Boolean
            Dim node = TryGetAncestorForLocation(Of PropertyStatementSyntax)(root, position, propertyDeclaration)

            If node?.AsClause IsNot Nothing Then
                Return IsOnHeader(root, position, node, node.AsClause)
            End If

            Return node IsNot Nothing AndAlso IsOnHeader(root, position, node, node.Identifier)
        End Function

        Public Overrides Function IsOnParameterHeader(root As SyntaxNode, position As Integer, ByRef parameter As SyntaxNode) As Boolean
            Dim node = TryGetAncestorForLocation(Of ParameterSyntax)(root, position, parameter)
            Return node IsNot Nothing AndAlso IsOnHeader(root, position, node, node)
        End Function

        Public Overrides Function IsOnMethodHeader(root As SyntaxNode, position As Integer, ByRef method As SyntaxNode) As Boolean
            Dim node = TryGetAncestorForLocation(Of MethodStatementSyntax)(root, position, method)
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

        Public Overrides Function IsOnLocalFunctionHeader(root As SyntaxNode, position As Integer, ByRef localFunction As SyntaxNode) As Boolean
            ' No local functions in VisualBasic
            Return False
        End Function

        Public Overrides Function IsOnLocalDeclarationHeader(root As SyntaxNode, position As Integer, ByRef localDeclaration As SyntaxNode) As Boolean
            Dim node = TryGetAncestorForLocation(Of LocalDeclarationStatementSyntax)(root, position, localDeclaration)
            Return node IsNot Nothing AndAlso IsOnHeader(root, position, node, node, node.Declarators.
                Where(Function(d) d.Initializer IsNot Nothing).
                SelectAsArray(Function(initialized) initialized.Initializer.Value))
        End Function

        Public Overrides Function IsOnIfStatementHeader(root As SyntaxNode, position As Integer, ByRef ifStatement As SyntaxNode) As Boolean
            Dim multipleLineNode = TryGetAncestorForLocation(Of MultiLineIfBlockSyntax)(root, position, ifStatement)
            If multipleLineNode IsNot Nothing Then
                Return IsOnHeader(root, position, multipleLineNode.IfStatement, multipleLineNode.IfStatement)
            End If

            Dim singleLineNode = TryGetAncestorForLocation(Of SingleLineIfStatementSyntax)(root, position, ifStatement)
            If singleLineNode IsNot Nothing Then
                Return IsOnHeader(root, position, singleLineNode, singleLineNode.Condition)
            End If

            Return False
        End Function

        Public Overrides Function IsOnWhileStatementHeader(root As SyntaxNode, position As Integer, ByRef whileStatement As SyntaxNode) As Boolean
            Dim whileBlock = TryGetAncestorForLocation(Of WhileBlockSyntax)(root, position, whileStatement)
            Return whileBlock IsNot Nothing AndAlso IsOnHeader(root, position, whileBlock.WhileStatement, whileBlock.WhileStatement)
        End Function

        Public Overrides Function IsOnForeachHeader(root As SyntaxNode, position As Integer, ByRef foreachStatement As SyntaxNode) As Boolean
            Dim node = TryGetAncestorForLocation(Of ForEachBlockSyntax)(root, position, foreachStatement)
            Return node IsNot Nothing AndAlso IsOnHeader(root, position, node, node.ForEachStatement)
        End Function
    End Class
End Namespace
