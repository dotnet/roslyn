﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.SplitOrMergeIfStatements
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.MergeNestedIfStatements), [Shared]>
    Friend NotInheritable Class VisualBasicMergeNestedIfStatementsCodeRefactoringProvider
        Inherits AbstractMergeNestedIfStatementsCodeRefactoringProvider

        Protected Overrides Function IsApplicableSpan(node As SyntaxNode, span As TextSpan, ByRef ifOrElseIf As SyntaxNode) As Boolean
            If TypeOf node Is IfStatementSyntax And TypeOf node.Parent Is MultiLineIfBlockSyntax Then
                Dim ifStatement = DirectCast(node, IfStatementSyntax)
                ' Cases:
                ' 1. Position is at a child token of an if statement with no selection (e.g. 'If' keyword, 'Then' keyword)
                ' 2. Selection around the 'If' keyword
                ' 3. Selection around the if statement - from 'If' keyword to 'Then' keyword
                If span.Length = 0 OrElse
                   span.IsAround(ifStatement.IfKeyword) OrElse
                   span.IsAround(ifStatement) Then
                    ifOrElseIf = node.Parent
                    Return True
                End If
            End If

            If TypeOf node Is MultiLineIfBlockSyntax Then
                Dim ifBlock = DirectCast(node, MultiLineIfBlockSyntax)
                ' 4. Selection around the whole if block
                If span.IsAround(node) Then
                    ifOrElseIf = node
                    Return True
                End If

                ' 5. Selection from an else if block to the end of its multiline if block
                For Each elseIfBlock In ifBlock.ElseIfBlocks
                    If span.IsAround(elseIfBlock, ifBlock) Then
                        ifOrElseIf = elseIfBlock
                        Return True
                    End If
                Next
            End If

            If TypeOf node Is ElseIfStatementSyntax AndAlso TypeOf node.Parent Is ElseIfBlockSyntax Then
                Dim elseIfStatement = DirectCast(node, ElseIfStatementSyntax)
                ' 6. Position is at a child token of an else if statement with no selection (e.g. 'ElseIf' keyword, 'Then' keyword)
                ' 7. Selection around the 'ElseIf' keyword
                ' 8. Selection around the else if statement - from 'ElseIf' keyword to 'Then' keyword
                If span.Length = 0 OrElse
                   span.IsAround(elseIfStatement.ElseIfKeyword) OrElse
                   span.IsAround(elseIfStatement) Then
                    ifOrElseIf = node.Parent
                    Return True
                End If
            End If

            ifOrElseIf = Nothing
            Return False
        End Function
    End Class
End Namespace
