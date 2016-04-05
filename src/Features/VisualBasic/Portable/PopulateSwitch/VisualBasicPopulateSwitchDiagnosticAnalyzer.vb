Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.PopulateSwitch
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics.PopulateSwitch

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicPopulateSwitchDiagnosticAnalyzer
        Inherits AbstractPopulateSwitchDiagnosticAnalyzerBase(Of SyntaxKind, SelectBlockSyntax)

        Protected Overrides Function GetCaseLabels(selectBlock As SelectBlockSyntax, <Out> ByRef hasDefaultCase As Boolean) As List(Of SyntaxNode)
            
            Dim caseLabels As New List(Of SyntaxNode)
            hasDefaultCase = False

            For Each block In selectBlock.CaseBlocks
                For Each caseClause In block.CaseStatement.Cases
                    If caseClause.IsKind(SyntaxKind.SimpleCaseClause)
                        caseLabels.Add(DirectCast(caseClause, SimpleCaseClauseSyntax).Value)
                    End If

                    If caseClause.IsKind(SyntaxKind.ElseCaseClause)
                        hasDefaultCase = True
                    End If
                Next
            Next

            Return caseLabels
        End Function

        Private Shared ReadOnly s_kindsOfInterest As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(SyntaxKind.SelectBlock)
        Protected Overrides ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind) = s_kindsOfInterest

        Protected Overrides Function GetExpression(selectBlock As SelectBlockSyntax) As SyntaxNode
            
            Return selectBlock.SelectStatement.Expression
        End Function
    End Class
End Namespace
