' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings
    <ExportLanguageService(GetType(IRefactoringHelpersService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicRefactoringHelpersService
        Inherits AbstractRefactoringHelpersService(Of ExpressionSyntax, ArgumentSyntax)

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

        Function IsIdentifierOfParameter(node As SyntaxNode) As Boolean
            Return (TypeOf node Is ModifiedIdentifierSyntax) AndAlso (TypeOf node.Parent Is ParameterSyntax) AndAlso (CType(node.Parent, ParameterSyntax).Identifier Is node)
        End Function
    End Class
End Namespace
