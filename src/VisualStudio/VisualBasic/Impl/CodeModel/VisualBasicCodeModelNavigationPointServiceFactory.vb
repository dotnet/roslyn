﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.LanguageServices.Implementation
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel
    <ExportLanguageServiceFactory(GetType(ICodeModelNavigationPointService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicCodeModelNavigationPointServiceFactory
        Implements ILanguageServiceFactory

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Function CreateLanguageService(provider As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            ' This interface is implemented by the ICodeModelService as well, so just grab the other one and return it
            Return provider.GetService(Of ICodeModelService)
        End Function
    End Class
End Namespace
