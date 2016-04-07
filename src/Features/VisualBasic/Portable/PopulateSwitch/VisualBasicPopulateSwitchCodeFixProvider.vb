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

    End Class
End Namespace