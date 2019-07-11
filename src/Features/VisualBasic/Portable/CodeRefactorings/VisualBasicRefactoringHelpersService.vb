' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings
    <ExportLanguageService(GetType(IRefactoringHelpersService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicRefactoringHelpersService
        Inherits AbstractRefactoringHelpersService(Of PropertyStatementSyntax, ParameterSyntax, MethodStatementSyntax, LocalDeclarationStatementSyntax)

        Protected Overrides Iterator Function ExtractNodeSimple(node As SyntaxNode, syntaxFacts As ISyntaxFactsService) As IEnumerable(Of SyntaxNode)
            For Each baseExtraction In MyBase.ExtractNodeSimple(node, syntaxFacts)
                Yield baseExtraction
            Next

            ' VB's arguments can have identifiers nested in ModifiedArgument -> we want
            ' identifiers to represent parent node -> need to extract.
            If IsIdentifierOfParameter(node) Then
                Yield node.Parent
            End If
        End Function

        Function IsIdentifierOfParameter(node As SyntaxNode) As Boolean
            Return (TypeOf node Is ModifiedIdentifierSyntax) AndAlso (TypeOf node.Parent Is ParameterSyntax) AndAlso (CType(node.Parent, ParameterSyntax).Identifier Is node)
        End Function
    End Class
End Namespace
