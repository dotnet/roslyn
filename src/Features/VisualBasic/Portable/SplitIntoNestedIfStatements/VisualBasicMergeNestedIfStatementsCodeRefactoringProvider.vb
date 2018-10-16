' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.SplitIntoNestedIfStatements
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SplitIntoNestedIfStatements
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.MergeNestedIfStatements), [Shared]>
    Friend NotInheritable Class VisualBasicMergeNestedIfStatementsCodeRefactoringProvider
        Inherits AbstractMergeNestedIfStatementsCodeRefactoringProvider(Of MultiLineIfBlockSyntax)

        Protected Overrides ReadOnly Property IfKeywordText As String = SyntaxFacts.GetText(SyntaxKind.IfKeyword)

        Protected Overrides Function IsTokenOfIfStatement(token As SyntaxToken, ByRef ifStatement As MultiLineIfBlockSyntax) As Boolean
            If TypeOf token.Parent Is IfStatementSyntax AndAlso TypeOf token.Parent.Parent Is MultiLineIfBlockSyntax Then
                ifStatement = DirectCast(token.Parent.Parent, MultiLineIfBlockSyntax)
                Return True
            End If

            ifStatement = Nothing
            Return False
        End Function

        Protected Overrides Function GetElseClauses(ifStatement As MultiLineIfBlockSyntax) As ImmutableArray(Of SyntaxNode)
            Return ImmutableArray.ToImmutableArray(Of SyntaxNode)(ifStatement.ElseIfBlocks).Add(ifStatement.ElseBlock)
        End Function

        Protected Overrides Function MergeIfStatements(outerIfBlock As MultiLineIfBlockSyntax, innerIfBlock As MultiLineIfBlockSyntax, condition As SyntaxNode) As MultiLineIfBlockSyntax
            Return outerIfBlock.WithIfStatement(outerIfBlock.IfStatement.WithCondition(DirectCast(condition, ExpressionSyntax))) _
                               .WithStatements(innerIfBlock.Statements)
        End Function
    End Class
End Namespace
