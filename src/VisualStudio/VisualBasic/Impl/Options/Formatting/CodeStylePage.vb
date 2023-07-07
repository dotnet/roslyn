' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Options.EditorConfig
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options.Formatting
    <Guid(Guids.VisualBasicOptionPageCodeStyleIdString)>
    Friend Class CodeStylePage
        Inherits AbstractOptionPage

        Protected Overrides Function CreateOptionPage(serviceProvider As IServiceProvider, optionStore As OptionStore) As AbstractOptionPageControl
            Dim editorService = DirectCast(serviceProvider.GetService(GetType(EditorConfigOptionsGenerator)), EditorConfigOptionsGenerator)
            Return New GridOptionPreviewControl(serviceProvider,
                                                optionStore,
                                                Function(o, s) New StyleViewModel(o, s),
                                                editorService.GetDefaultOptions(LanguageNames.VisualBasic),
                                                LanguageNames.VisualBasic)
        End Function
    End Class
End Namespace
