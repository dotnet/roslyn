' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Completion.Providers.ImportCompletion
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportLanguageServiceFactory(GetType(ITypeImportCompletionService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class BasicTypeImportCompletionServiceFactory
        Implements ILanguageServiceFactory

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New BasicTypeImportCompletionService(languageServices.WorkspaceServices.Workspace)
        End Function

        Private Class BasicTypeImportCompletionService
            Inherits AbstractTypeImportCompletionService

            Public Sub New(workspace As Workspace)
                MyBase.New(workspace)
            End Sub

            Protected Overrides ReadOnly Property GenericTypeSuffix As String
                Get
                    Return "(Of ...)"
                End Get
            End Property

            Protected Overrides ReadOnly Property IsCaseSensitive As Boolean
                Get
                    Return False
                End Get
            End Property
        End Class
    End Class
End Namespace
