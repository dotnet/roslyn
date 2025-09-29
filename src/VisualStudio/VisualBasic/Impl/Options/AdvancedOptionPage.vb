﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    <Guid(Guids.VisualBasicOptionPageVBSpecificIdString)>
    Friend Class AdvancedOptionPage
        Inherits AbstractOptionPage

        Protected Overrides Function CreateOptionPage(serviceProvider As IServiceProvider, optionStore As OptionStore) As AbstractOptionPageControl
            Return New AdvancedOptionPageControl(optionStore)
        End Function
    End Class
End Namespace
