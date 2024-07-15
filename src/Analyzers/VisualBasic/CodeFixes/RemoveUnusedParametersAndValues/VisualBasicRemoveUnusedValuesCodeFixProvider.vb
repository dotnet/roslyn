' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedParametersAndValues
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.RemoveUnusedValues), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.AddImport)>
    Friend Class VisualBasicRemoveUnusedValuesCodeFixProvider
        Inherits AbstractRemoveUnusedValuesCodeFixProvider(Of ExpressionSyntax, StatementSyntax, StatementSyntax,
            ExpressionStatementSyntax, LocalDeclarationStatementSyntax, VariableDeclaratorSyntax, ForEachBlockSyntax,
            CaseBlockSyntax, CaseClauseSyntax, CatchStatementSyntax, CatchBlockSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property SyntaxFormatting As ISyntaxFormatting
            Get
                Return VisualBasicSyntaxFormatting.Instance
            End Get
        End Property

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
            Throw ExceptionUtilities.Unreachable
        End Function

        Protected Overrides Function GetReplacementNodeForVarPattern(originalVarPattern As SyntaxNode, newNameNode As SyntaxNode) As SyntaxNode
            ' VB does not have var patterns
            Throw ExceptionUtilities.Unreachable
        End Function

        Protected Overrides Function ComputeReplacementNode(originalOldNode As SyntaxNode, changedOldNode As SyntaxNode, proposedReplacementNode As SyntaxNode) As SyntaxNode
            ' VB currently doesn't have recursive change scenarios
            Return proposedReplacementNode.WithAdditionalAnnotations(Formatter.Annotation)
        End Function

        Protected Overrides Function GetCandidateLocalDeclarationForRemoval(declarator As VariableDeclaratorSyntax) As LocalDeclarationStatementSyntax
            Return TryCast(declarator.Parent, LocalDeclarationStatementSyntax)
        End Function
    End Class
End Namespace
