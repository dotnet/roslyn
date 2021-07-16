' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.SplitOrMergeIfStatements
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.MergeConsecutiveIfStatements), [Shared]>
    Friend NotInheritable Class VisualBasicMergeConsecutiveIfStatementsCodeRefactoringProvider
        Inherits AbstractMergeConsecutiveIfStatementsCodeRefactoringProvider

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function IsApplicableSpan(node As SyntaxNode, span As TextSpan, ByRef ifOrElseIf As SyntaxNode) As Boolean
            If TypeOf node Is IfStatementSyntax AndAlso TypeOf node.Parent Is MultiLineIfBlockSyntax Then
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
                ' 4. Selection around the if block *excluding* its else if and else clauses - from 'if' keyword to the end of its statements
                If ifBlock.Statements.Count > 0 AndAlso span.IsAround(ifBlock, ifBlock.Statements.Last()) Then
                    ifOrElseIf = node
                    Return True
                End If
            End If

            If TypeOf node Is ElseIfStatementSyntax AndAlso TypeOf node.Parent Is ElseIfBlockSyntax Then
                Dim elseIfStatement = DirectCast(node, ElseIfStatementSyntax)
                ' 5. Position is at a child token of an else if statement with no selection (e.g. 'ElseIf' keyword, 'Then' keyword)
                ' 6. Selection around the 'ElseIf' keyword
                ' 7. Selection around the else if statement - from 'ElseIf' keyword to 'Then' keyword
                If span.Length = 0 OrElse
                   span.IsAround(elseIfStatement.ElseIfKeyword) OrElse
                   span.IsAround(elseIfStatement) Then
                    ifOrElseIf = node.Parent
                    Return True
                End If
            End If

            If TypeOf node Is ElseIfBlockSyntax Then
                ' 8. Selection around the else if block.
                If span.IsAround(node) Then
                    ifOrElseIf = node
                    Return True
                End If
            End If

            ifOrElseIf = Nothing
            Return False
        End Function
    End Class
End Namespace
