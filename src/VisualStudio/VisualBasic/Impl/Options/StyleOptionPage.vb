﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    <Guid(Guids.VisualBasicOptionPageCodeStyleIdString)>
    Friend Class StyleOptionPage
        Inherits AbstractOptionPage

        Protected Overrides Function CreateOptionPage(serviceProvider As IServiceProvider, optionStore As OptionStore) As AbstractOptionPageControl
            Return New OptionPreviewControl(serviceProvider, optionStore, Function(o, s) New StyleViewModel(o, s))
        End Function
    End Class
End Namespace
