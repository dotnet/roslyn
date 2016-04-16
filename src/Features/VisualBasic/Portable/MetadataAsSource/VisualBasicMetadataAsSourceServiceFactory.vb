' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.MetadataAsSource

Namespace Microsoft.CodeAnalysis.VisualBasic.MetadataAsSource
    <ExportLanguageServiceFactory(GetType(IMetadataAsSourceService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicMetadataAsSourceServiceFactory
        Implements ILanguageServiceFactory

        Public Function CreateLanguageService(provider As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicMetadataAsSourceService(provider)
        End Function
    End Class
End Namespace
