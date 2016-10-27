' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Options

Namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting
    <Guid(Guids.VisualBasicOptionPageCodeStyleIdString)>
    Friend Class CodeStylePage
        Inherits AbstractOptionPage

        Protected Overrides Function CreateOptionPage(serviceProvider As IServiceProvider) As AbstractOptionPageControl
            Return New GridOptionPreviewControl(serviceProvider, Function(o, s) New StyleViewModel(o, s))
        End Function
    End Class
End Namespace
