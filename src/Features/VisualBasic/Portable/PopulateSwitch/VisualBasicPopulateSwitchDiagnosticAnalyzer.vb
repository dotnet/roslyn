Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.PopulateSwitch
Imports Microsoft.CodeAnalysis.VisualBasic.PopulateSwitch
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics.PopulateSwitch

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicPopulateSwitchDiagnosticAnalyzer
        Inherits AbstractPopulateSwitchDiagnosticAnalyzerBase(Of SyntaxKind, SelectBlockSyntax)

        Protected Overrides Function GetCaseLabels(selectBlock As SelectBlockSyntax, <Out> ByRef hasDefaultCase As Boolean) As List(Of SyntaxNode)
            Return VisualBasicPopulateSwitchHelperClass.GetCaseLabels(selectBlock, hasDefaultCase)
        End Function
        
        Protected Overrides ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(SyntaxKind.SelectBlock)

        Protected Overrides Function GetExpression(selectBlock As SelectBlockSyntax) As SyntaxNode
            Return selectBlock.SelectStatement.Expression
        End Function
    End Class
End Namespace
