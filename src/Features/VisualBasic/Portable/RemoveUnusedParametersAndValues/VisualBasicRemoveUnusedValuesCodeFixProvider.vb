' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.RemoveUnusedParametersAndValues
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedParametersAndValues
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.RemoveUnusedValues), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.AddImport)>
    Friend Class VisualBasicRemoveUnusedValuesCodeFixProvider
        Inherits AbstractRemoveUnusedValuesCodeFixProvider(Of ExpressionSyntax, StatementSyntax, StatementSyntax,
            ExpressionStatementSyntax, LocalDeclarationStatementSyntax, VariableDeclaratorSyntax, ForEachBlockSyntax,
            CaseBlockSyntax, CaseClauseSyntax, CatchStatementSyntax, CatchBlockSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function WrapWithBlockIfNecessary(statements As IEnumerable(Of StatementSyntax)) As StatementSyntax
            ' Unreachable code path as VB statements don't need to be wrapped in special BlockSyntax.
            Throw ExceptionUtilities.Unreachable
        End Function

        Protected Overrides Sub InsertAtStartOfSwitchCaseBlockForDeclarationInCaseLabelOrClause(switchCaseBlock As CaseBlockSyntax, editor As SyntaxEditor, declarationStatement As LocalDeclarationStatementSyntax)
            ' VB does not support declarations in select case clause.
            Throw ExceptionUtilities.Unreachable
        End Sub

        Protected Overrides Function TryUpdateNameForFlaggedNode(node As SyntaxNode, newName As SyntaxToken) As SyntaxNode
            Dim modifiedIdentifier = TryCast(node, ModifiedIdentifierSyntax)
            If modifiedIdentifier IsNot Nothing Then
                Return modifiedIdentifier.WithIdentifier(newName).WithTriviaFrom(node)
            End If

            Dim identifier = TryCast(node, IdentifierNameSyntax)
            If identifier IsNot Nothing Then
                Return identifier.WithIdentifier(newName).WithTriviaFrom(node)
            End If

            Debug.Fail($"Unexpected node kind for local/parameter declaration or reference: '{node.Kind()}'")
            Return Nothing
        End Function

        Protected Overrides Function GetForEachStatementIdentifier(node As ForEachBlockSyntax) As SyntaxToken
            Throw ExceptionUtilities.Unreachable
        End Function

        Protected Overrides Function GetReplacementNodeForCompoundAssignment(originalCompoundAssignment As SyntaxNode, newAssignmentTarget As SyntaxNode, editor As SyntaxEditor, syntaxFacts As ISyntaxFactsService) As SyntaxNode
            ' VB does not support compound assignments.
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
