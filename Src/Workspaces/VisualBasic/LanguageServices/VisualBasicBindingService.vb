Imports System.Threading
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services.Shared.LanguageServices

Namespace Roslyn.Services.Editor.VisualBasic.LanguageServices
    <ExportLanguageService(GetType(ISemanticModelService), LanguageNames.VisualBasic)>
    Class VisualBasicSemanticModelService
        Implements ISemanticModelService

        Public Function TryGetDefinition(binding As ISemanticModel, token As CommonSyntaxToken, cancellationToken As CancellationToken, ByRef definition As ISymbol) As Boolean Implements ISemanticModelService.TryGetDefinition
            Dim symbol As Symbol = Nothing
            If DirectCast(binding, SemanticModel).TryGetDefinition(CType(token, SyntaxToken), cancellationToken, symbol) Then
                definition = symbol
                Return True
            Else
                Return False
            End If
        End Function
    End Class
End Namespace