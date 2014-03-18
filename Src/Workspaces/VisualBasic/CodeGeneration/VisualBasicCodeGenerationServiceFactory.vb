' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.LanguageServices

#If MEF Then
Imports Microsoft.CodeAnalysis.CodeGeneration
#End If

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
#If MEF Then
    <ExportLanguageServiceFactory(GetType(ICodeGenerationService), LanguageNames.VisualBasic)>
    Friend Class VisualBasicCodeGenerationServiceFactory
#Else
    Friend Class VisualBasicCodeGenerationServiceFactory
#End If
        Implements ILanguageServiceFactory

        Public Function CreateLanguageService(provider As ILanguageServiceProvider) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicCodeGenerationService(provider)
        End Function
    End Class
End Namespace
