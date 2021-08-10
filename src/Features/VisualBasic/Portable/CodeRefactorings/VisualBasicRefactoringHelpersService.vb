' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports System.Diagnostics.CodeAnalysis

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings
    <ExportLanguageService(GetType(IRefactoringHelpersService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicRefactoringHelpersService
        Inherits AbstractRefactoringHelpersService(Of ExpressionSyntax, ArgumentSyntax, ExpressionStatementSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Iterator Function ExtractNodesSimple(node As SyntaxNode, syntaxFacts As ISyntaxFactsService) As IEnumerable(Of SyntaxNode)
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

        Public Shared Function IsIdentifierOfParameter(node As SyntaxNode) As Boolean
            Return (TypeOf node Is ModifiedIdentifierSyntax) AndAlso (TypeOf node.Parent Is ParameterSyntax) AndAlso (CType(node.Parent, ParameterSyntax).Identifier Is node)
        End Function

        Protected Overrides Function TryGetVariableDeclaratorInSingleFieldDeclaration(node As SyntaxNode, ByRef singleVariableDeclarator As SyntaxNode) As Boolean
            Dim fieldDeclarationNode = TryCast(node, FieldDeclarationSyntax)
            If fieldDeclarationNode IsNot Nothing Then
                Dim declarators = fieldDeclarationNode.Declarators
                If declarators.Count = 1 AndAlso declarators(0).Names.Count = 1 Then
                    singleVariableDeclarator = declarators(0).Names(0)
                    Return True
                End If
            End If

            Dim declaratorNode = TryCast(node, VariableDeclaratorSyntax)
            If declaratorNode IsNot Nothing AndAlso TypeOf node.Parent Is FieldDeclarationSyntax AndAlso declaratorNode.Names.Count = 1 Then
                singleVariableDeclarator = declaratorNode.Names(0)
                Return True
            End If

            singleVariableDeclarator = Nothing
            Return False
        End Function
    End Class
End Namespace
