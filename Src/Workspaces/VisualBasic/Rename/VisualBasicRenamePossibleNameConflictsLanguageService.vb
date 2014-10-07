Imports Microsoft.CodeAnalysis.Common
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Rename

Namespace Microsoft.CodeAnalysis.VisualBasic.Rename
    <ExportLanguageService(GetType(IRenamePossibleNameConflictsLanguageService), LanguageNames.VisualBasic)>
    Friend Class VisualBasicRenamePossibleNameConflictsLanguageService
        Implements IRenamePossibleNameConflictsLanguageService

        Public Sub TryAddPossibleNameConflicts(symbol As ISymbol, replacementText As String, possibleNameConflicts As List(Of String)) Implements IRenamePossibleNameConflictsLanguageService.TryAddPossibleNameConflicts
            Dim halfWidthReplacementText = SyntaxFacts.MakeHalfWidthIdentifier(replacementText)

            Const AttributeSuffix As String = "Attribute"
            Const AttributeSuffixLength As Integer = 9
            Debug.Assert(AttributeSuffixLength = AttributeSuffix.Length, "Assert (AttributeSuffixLength = AttributeSuffix.Length) failed.")

            If replacementText.Length > AttributeSuffixLength AndAlso IdentifierComparison.Compare(halfWidthReplacementText.Substring(halfWidthReplacementText.Length - AttributeSuffixLength), AttributeSuffix) = 0 Then
                Dim conflict = replacementText.Substring(0, replacementText.Length - AttributeSuffixLength)
                If Not possibleNameConflicts.Contains(conflict) Then
                    possibleNameConflicts.Add(conflict)
                End If
            End If

            If symbol.Kind = CommonSymbolKind.Property Then
                For Each conflict In {"_" + replacementText, "get_" + replacementText, "set_" + replacementText}
                    If Not possibleNameConflicts.Contains(conflict) Then
                        possibleNameConflicts.Add(conflict)
                    End If
                Next
            End If

            ' consider both versions of the identifier (escaped and unescaped)
            Dim valueText = replacementText
            Dim kind = SyntaxFacts.GetKeywordKind(replacementText)
            If kind <> SyntaxKind.None Then
                valueText = SyntaxFacts.GetText(kind)
            Else
                Dim name = SyntaxFactory.ParseName(replacementText)
                If name.Kind = SyntaxKind.IdentifierName Then
                    valueText = DirectCast(name, Syntax.IdentifierNameSyntax).Identifier.ValueText
                End If
            End If

            If IdentifierComparison.Compare(valueText, replacementText) <> 0 Then
                possibleNameConflicts.Add(valueText)
            End If
        End Sub
    End Class
End Namespace