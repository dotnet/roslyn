Imports System.Composition
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.PopulateSwitch
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.PopulateSwitch
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.PopulateSwitch), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.AddOverloads)>
    Partial Friend Class VisualBasicPopulateSwitchCodeFixProvider
        Inherits AbstractPopulateSwitchCodeFixProvider(Of CaseBlockSyntax)

        Protected Overrides Function InsertPosition(sections As IReadOnlyList(Of CaseBlockSyntax)) As Integer
            ' If the last section has a default label, then we want to be above that.
            ' Otherwise, we just get inserted at the end.
            If sections.Count <> 0 Then
                Dim lastSection = sections.Last()
                If lastSection.CaseStatement.Cases.Any(Function(c) c.Kind() = SyntaxKind.ElseCaseClause) Then
                    Return sections.Count - 1
                End If
            End If

            Return sections.Count
        End Function
    End Class
End Namespace