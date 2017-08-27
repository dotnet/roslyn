Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.GoToDefinition
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.GoToDefinition
    <ExportLanguageService(GetType(IGoToSymbolService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicGoToSymbolService
        Inherits AbstractGoToSymbolService

    End Class
End Namespace
