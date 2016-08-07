Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory

<ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ExtractMethod), [Shared]>
Partial Friend Class ConvertToInterpolatedStringRefactoringProvider
    Inherits AbstractConvertToInterpolatedStringRefactoringProvider(Of InvocationExpressionSyntax, ExpressionSyntax, ArgumentSyntax, LiteralExpressionSyntax)

    Protected Overrides Function GetInterpolatedString(text As String) As SyntaxNode
        Return TryCast(ParseExpression("$" + text), InterpolatedStringExpressionSyntax)
    End Function
End Class
