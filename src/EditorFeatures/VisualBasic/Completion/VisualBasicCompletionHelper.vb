Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Completion

    <ExportLanguageServiceFactory(GetType(CompletionHelperFactory), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicCompletionHelperFactoryFactory
        Implements ILanguageServiceFactory

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicCompletionHelperFactory()
        End Function

        Private Class VisualBasicCompletionHelperFactory
            Inherits CompletionHelperFactory

            Public Sub New()
            End Sub

            Public Overrides Function CreateCompletionHelper() As CompletionHelper
                Return New VisualBasicCompletionHelper()
            End Function
        End Class
    End Class

    Friend Class VisualBasicCompletionHelper
        Inherits CompletionHelper

        Public Sub New()
            MyBase.New(isCaseSensitive:=False)
        End Sub
    End Class
End Namespace