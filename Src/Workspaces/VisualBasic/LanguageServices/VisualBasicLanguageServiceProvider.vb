Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.Composition
Imports System.Linq
Imports System.Text
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Services.LanguageServices

Namespace Roslyn.Services.VisualBasic
    <ExportLanguageServiceProvider(LanguageNames.VisualBasic)>
    Friend Class VisualBasicLanguageServiceProvider
        Inherits AbstractLanguageServiceProvider

        <ImportingConstructor()>
        Public Sub New(<ImportMany()> languageServices As IEnumerable(Of Lazy(Of ILanguageService, ILanguageServiceMetadata)))
            MyBase.New(LanguageNames.VisualBasic, languageServices)
        End Sub
    End Class
End Namespace