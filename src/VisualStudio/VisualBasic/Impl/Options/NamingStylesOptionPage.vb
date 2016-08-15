' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    <Guid(Guids.VisualBasicOptionPageNamingStyleIdString)>
    Friend Class NamingStylesOptionPage
        Inherits AbstractOptionPage

        Protected Overrides Function CreateOptionPage(serviceProvider As IServiceProvider) As AbstractOptionPageControl
            Return New NamingStylesOptionPageControl(serviceProvider, LanguageNames.VisualBasic)
        End Function
    End Class
End Namespace
