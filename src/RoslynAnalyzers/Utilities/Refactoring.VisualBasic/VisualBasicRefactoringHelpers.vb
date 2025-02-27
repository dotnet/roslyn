' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Global.Analyzer.Utilities

    Friend NotInheritable Class VisualBasicRefactoringHelpers
        Inherits AbstractRefactoringHelpers(Of ExpressionSyntax, ArgumentSyntax, ExpressionStatementSyntax)

        Public Shared ReadOnly Property Instance As New VisualBasicRefactoringHelpers()

        Private Sub New()
        End Sub

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts
            Get
                Return VisualBasicSyntaxFacts.Instance
            End Get
        End Property

        Protected Overrides Iterator Function ExtractNodesSimple(node As SyntaxNode, syntaxFacts As ISyntaxFacts) As IEnumerable(Of SyntaxNode)
            For Each baseExtraction In MyBase.ExtractNodesSimple(node, syntaxFacts)
                Yield baseExtraction
            Next

            ' VB's arguments can have identifiers nested in ModifiedArgument -> we want
            ' identifiers to represent parent node -> need to extract.
            If IsIdentifierOfParameter(node) Then
                Yield node.Parent
            End If

            ' In VB Statement both for/foreach are split into Statement (header) and the rest
            ' selecting the header should still count for the whole blockSyntax
            If TypeOf node Is ForEachStatementSyntax And TypeOf node.Parent Is ForEachBlockSyntax Then
                Dim foreachStatement = CType(node, ForEachStatementSyntax)
                Yield foreachStatement.Parent
            End If

            If TypeOf node Is ForStatementSyntax And TypeOf node.Parent Is ForBlockSyntax Then
                Dim forStatement = CType(node, ForStatementSyntax)
                Yield forStatement.Parent
            End If

            If TypeOf node Is VariableDeclaratorSyntax Then
                Dim declarator = CType(node, VariableDeclaratorSyntax)
                If TypeOf declarator.Parent Is LocalDeclarationStatementSyntax Then
                    Dim localDeclarationStatement = CType(declarator.Parent, LocalDeclarationStatementSyntax)
                    ' Only return the whole localDeclarationStatement if there's just one declarator with just one name
                    If localDeclarationStatement.Declarators.Count = 1 And localDeclarationStatement.Declarators.First().Names.Count = 1 Then
                        Yield localDeclarationStatement
                    End If
                End If
            End If

        End Function

        Private Shared Function IsIdentifierOfParameter(node As SyntaxNode) As Boolean
            Return (TypeOf node Is ModifiedIdentifierSyntax) AndAlso (TypeOf node.Parent Is ParameterSyntax) AndAlso (CType(node.Parent, ParameterSyntax).Identifier Is node)
        End Function

    End Class

End Namespace
