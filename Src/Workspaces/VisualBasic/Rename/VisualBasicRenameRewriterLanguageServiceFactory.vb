' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.LanguageServices

#If MEF Then
Imports Microsoft.CodeAnalysis.Rename
#End If

Namespace Microsoft.CodeAnalysis.VisualBasic.Rename
#If MEF Then
    <ExportLanguageServiceFactory(GetType(IRenameRewriterLanguageService), LanguageNames.VisualBasic)>
    Friend Class VisualBasicRenameRewriterLanguageServiceFactory
#Else
    Friend Class VisualBasicRenameRewriterLanguageServiceFactory
#End If
        Implements ILanguageServiceFactory

        Public Function CreateLanguageService(provider As ILanguageServiceProvider) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicRenameRewriterLanguageService(provider)
        End Function
    End Class
End Namespace
