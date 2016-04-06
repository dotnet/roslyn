Imports System.Composition
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CSharp.CodeFixes.PopulateSwitch
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.PopulateSwitch
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.PopulateSwitch
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.PopulateSwitch), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.AddOverloads)>
    Partial Friend Class VisualBasicPopulateSwitchCodeFixProvider
        Inherits AbstractPopulateSwitchCodeFixProvider(Of SelectBlockSyntax, ExpressionSyntax, CaseBlockSyntax)

        Protected Overrides Function GetSwitchExpression(selectBlock As SelectBlockSyntax) As ExpressionSyntax
            Return selectBlock.SelectStatement.Expression
        End Function

        Protected Overrides Function InsertPosition(sections As SyntaxList(Of CaseBlockSyntax)) As Integer
            Dim cases = sections.OfType(Of CaseBlockSyntax).ToList()
            Dim numOfBlocksWithNoStatementsWithElse = 0

            ' skip the `Else` block
            For i = cases.Count - 2 To 0 Step -1
                If Not cases.ElementAt(i).Statements.Count = 0

                    ' insert the values immediately below the last item with statements
                    numOfBlocksWithNoStatementsWithElse = i + 1
                    Exit For
                End If
            Next

            Return numOfBlocksWithNoStatementsWithElse
        End Function

        Protected Overrides Function GetSwitchSections(selectBlock As SelectBlockSyntax) As SyntaxList(Of CaseBlockSyntax)
            Return selectBlock.CaseBlocks
        End Function

        Protected Overrides Function NewSwitchNode(selectBlock As SelectBlockSyntax, sections As SyntaxList(Of CaseBlockSyntax)) As SelectBlockSyntax
            Return selectBlock.WithCaseBlocks(SyntaxFactory.List(sections))
        End Function

        Protected Overrides Function GetSwitchStatementNode(root As SyntaxNode, span As TextSpan) As SelectBlockSyntax
            Dim selectExpression = DirectCast(root.FindNode(span), ExpressionSyntax)
            Return DirectCast(selectExpression.Parent.Parent, SelectBlockSyntax)
        End Function

        Protected Overrides Function GetCaseLabels(selectBlock As SelectBlockSyntax, <Out> ByRef containsDefaultLabel As Boolean) As List(Of ExpressionSyntax)
            Return VisualBasicPopulateSwitchHelperClass.GetCaseLabels(selectBlock, containsDefaultLabel)
        End Function
    End Class
End Namespace