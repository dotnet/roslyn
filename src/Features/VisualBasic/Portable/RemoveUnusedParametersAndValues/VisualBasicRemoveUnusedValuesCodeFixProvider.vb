' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.RemoveUnusedParametersAndValues
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedParametersAndValues
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.RemoveUnusedValues), [Shared]>
    Friend Class VisualBasicRemoveUnusedValuesCodeFixProvider
        Inherits AbstractRemoveUnusedValuesCodeFixProvider(Of ExpressionSyntax, StatementSyntax, StatementSyntax,
            ExpressionStatementSyntax, LocalDeclarationStatementSyntax, VariableDeclaratorSyntax, ForEachBlockSyntax,
            CaseBlockSyntax, CaseClauseSyntax, CatchStatementSyntax, CatchBlockSyntax)

        Protected Overrides Function GenerateBlock(statements As IEnumerable(Of StatementSyntax)) As StatementSyntax
            Throw ExceptionUtilities.Unreachable
        End Function

        Protected Overrides Sub InsertAtStartOfSwitchCaseBlock(switchCaseBlock As CaseBlockSyntax, editor As SyntaxEditor, declarationStatement As LocalDeclarationStatementSyntax)
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

        Protected Overrides Function RemoveDiscardDeclarationsAsync(memberDeclaration As SyntaxNode, document As Document, cancellationToken As CancellationToken) As Task(Of SyntaxNode)
            Throw ExceptionUtilities.Unreachable
        End Function

        Protected Overrides Function GetForEachStatementIdentifier(node As ForEachBlockSyntax) As SyntaxToken
            Throw ExceptionUtilities.Unreachable
        End Function
    End Class
End Namespace
