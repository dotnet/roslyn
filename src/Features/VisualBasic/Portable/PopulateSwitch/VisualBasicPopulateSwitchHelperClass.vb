Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.PopulateSwitch
    Friend Module VisualBasicPopulateSwitchHelperClass
        Public Function GetCaseLabels(selectBlock As SelectBlockSyntax, <Out> ByRef containsDefaultLabel As Boolean) As List(Of SyntaxNode)
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
    End Module
End Namespace