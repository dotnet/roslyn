' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.LanguageServices

#If MEF Then
Imports Microsoft.CodeAnalysis.CaseCorrection
#End If

Namespace Microsoft.CodeAnalysis.VisualBasic.CaseCorrection
#If MEF Then
    <ExportLanguageServiceFactory(GetType(ICaseCorrectionService), LanguageNames.VisualBasic)>
    Partial Friend Class VisualBasicCaseCorrectionServiceFactory
#Else
    Partial Friend Class VisualBasicCaseCorrectionServiceFactory
#End If
        Implements ILanguageServiceFactory

        Public Function CreateLanguageService(provider As ILanguageServiceProvider) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicCaseCorrectionService(provider)
        End Function
    End Class
End Namespace