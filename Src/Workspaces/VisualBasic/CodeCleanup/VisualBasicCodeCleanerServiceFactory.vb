' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.LanguageServices

#If MEF Then
Imports Microsoft.CodeAnalysis.CodeCleanup
#End If

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeCleanup
#If MEF Then
    <ExportLanguageServiceFactory(GetType(ICodeCleanerService), LanguageNames.VisualBasic)>
    Partial Friend Class VisualBasicCodeCleanerServiceFactory
#Else
    Partial Friend Class VisualBasicCodeCleanerServiceFactory
#End If
        Implements ILanguageServiceFactory

        Public Function CreateLanguageService(provider As ILanguageServiceProvider) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicCodeCleanerService()
        End Function
    End Class
End Namespace
