' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.TestHooks

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportLanguageServiceFactory(GetType(ITypeImportCompletionService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class TypeImportCompletionServiceFactory
        Implements ILanguageServiceFactory

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
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

            Protected Overrides ReadOnly Property Language As String
                Get
                    Return LanguageNames.VisualBasic
                End Get
            End Property
        End Class
    End Class
End Namespace
