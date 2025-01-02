' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options.Formatting
    <Guid(Guids.VisualBasicOptionPageCodeStyleIdString)>
    Friend Class CodeStylePage
        Inherits AbstractOptionPage

        Protected Overrides Function CreateOptionPage(serviceProvider As IServiceProvider, optionStore As OptionStore) As AbstractOptionPageControl
            Dim enumerator = serviceProvider.GetMefService(Of EditorConfigOptionsEnumerator)()
            Return New GridOptionPreviewControl(serviceProvider,
                                                optionStore,
                                                Function(o, s) New StyleViewModel(o, s),
                                                enumerator.GetOptions(LanguageNames.VisualBasic),
                                                LanguageNames.VisualBasic)
        End Function
    End Class
End Namespace
