' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.MetadataAsSource

Namespace Microsoft.CodeAnalysis.VisualBasic.MetadataAsSource
    <ExportLanguageServiceFactory(GetType(IMetadataAsSourceService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicMetadataAsSourceServiceFactory
        Implements ILanguageServiceFactory

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function CreateLanguageService(provider As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return VisualBasicMetadataAsSourceService.Instance
        End Function
    End Class
End Namespace
