Imports System.Composition
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CSharp.CodeFixes.PopulateSwitch
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.PopulateSwitch
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.PopulateSwitch), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.AddOverloads)>
    Partial Friend Class VisualBasicPopulateSwitchCodeFixProvider
        Inherits AbstractPopulateSwitchCodeFixProvider(Of SelectBlockSyntax)

        Protected Overrides Function GetSwitchExpression(selectBlock As SelectBlockSyntax) As SyntaxNode
            
            Return selectBlock.SelectStatement.Expression
        End Function

        Protected Overrides Function InsertPosition(sections As List(Of SyntaxNode)) As Integer

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

        Protected Overrides Function GetSwitchSections(selectBlock As SelectBlockSyntax) As List(Of SyntaxNode)
            
            Return New List(Of SyntaxNode)(selectBlock.CaseBlocks)
        End Function

        Protected Overrides Function NewSwitchNode(selectBlock As SelectBlockSyntax, sections As List(Of SyntaxNode)) As SyntaxNode
            
            Return selectBlock.WithCaseBlocks(SyntaxFactory.List(sections)).WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation)
        End Function

        Protected Overrides Function GetSwitchStatementNode(root As SyntaxNode, span As TextSpan) As SyntaxNode
            
            Dim selectExpression = DirectCast(root.FindNode(span), ExpressionSyntax)
            Return DirectCast(selectExpression.Parent.Parent, SelectBlockSyntax)
        End Function

        Protected Overrides Function GetCaseLabels(selectBlock As SelectBlockSyntax, <Out> ByRef containsDefaultLabel As Boolean) As List(Of SyntaxNode)

            containsDefaultLabel = False

            Dim caseLabels = New List(Of SyntaxNode)
            For Each block In selectBlock.CaseBlocks
                For Each caseSyntax In block.CaseStatement.Cases

                    Dim simpleCaseClause = TryCast(caseSyntax, SimpleCaseClauseSyntax)
                    If Not simpleCaseClause Is Nothing
                        caseLabels.Add(simpleCaseClause.Value)
                        Continue For
                    End If

                    If caseSyntax.IsKind(SyntaxKind.ElseCaseClause)
                        containsDefaultLabel = True
                    End If
                Next
            Next

            Return caseLabels
        End Function
    End Class
End Namespace