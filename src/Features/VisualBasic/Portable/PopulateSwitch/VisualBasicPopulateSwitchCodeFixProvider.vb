Imports System.Composition
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.PopulateSwitch
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.PopulateSwitch
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.PopulateSwitch), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.AddOverloads)>
    Partial Friend Class VisualBasicPopulateSwitchCodeFixProvider
        Inherits AbstractPopulateSwitchCodeFixProvider(Of CaseBlockSyntax)

        Protected Overrides Function InsertPosition(sections As IReadOnlyList(Of CaseBlockSyntax)) As Integer
            Dim cases = sections.OfType(Of CaseBlockSyntax).ToList()
            Dim numOfBlocksWithNoStatementsWithElse = 0

            ' skip the `Else` block
            For i = cases.Count - 2 To 0 Step -1
                If Not cases.ElementAt(i).Statements.Count = 0 Then

                    ' insert the values immediately below the last item with statements
                    numOfBlocksWithNoStatementsWithElse = i + 1
                    Exit For
                End If
            Next

            Return numOfBlocksWithNoStatementsWithElse
        End Function
    End Class
End Namespace