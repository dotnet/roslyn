' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Classification.Classifiers
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification
    <ExportLanguageServiceFactory(GetType(ClassificationService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicClassificationServiceFactory
        Implements ILanguageServiceFactory

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicClassificationService(languageServices.WorkspaceServices.Workspace)
        End Function

        Private Class VisualBasicClassificationService
            Inherits CommonClassificationService

            Public Sub New(workspace As Workspace)
                MyBase.New(workspace)
            End Sub

            Protected Overrides ReadOnly Property Language As String
                Get
                    Return LanguageNames.VisualBasic
                End Get
            End Property
        End Class
    End Class
End Namespace
