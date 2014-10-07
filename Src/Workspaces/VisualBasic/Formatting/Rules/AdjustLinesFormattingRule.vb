Imports System.ComponentModel.Composition
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services
Imports Roslyn.Services.Formatting

Namespace Roslyn.Services.VisualBasic.Formatting
    <ExportFormattingRule(AdjustSpaceFormattingRule.Name, LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=ElasticTriviaFormattingRule.Name)>
    Friend Class AdjustLinesFormattingRule
        Inherits BaseFormattingRule
        Friend Const Name As String = "VisualBasic Adjust Lines Formatting Rule"

        <ImportingConstructor()>
        Public Sub New()
        End Sub

        Public Overrides Function GetAdjustNewLinesOperation(previousToken As CommonSyntaxToken, currentToken As CommonSyntaxToken, nextOperation As NextOperation(Of AdjustNewLinesOperation)) As AdjustNewLinesOperation
            Return GetAdjustNewLinesOperation(CType(previousToken, SyntaxToken), CType(currentToken, SyntaxToken), nextOperation)
        End Function

        Private Overloads Function GetAdjustNewLinesOperation(previousToken As SyntaxToken, currentToken As SyntaxToken, nextOperation As NextOperation(Of AdjustNewLinesOperation)) As AdjustNewLinesOperation
            ' Attribute case:
            ' > *
            ' Create an adjustment so that the attributes will be lined up.
            If previousToken.Kind = SyntaxKind.GreaterThanToken AndAlso
               TypeOf previousToken.Parent Is AttributeListSyntax Then

                Return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines)
            End If

            Return nextOperation.Invoke()
        End Function
    End Class
End Namespace
