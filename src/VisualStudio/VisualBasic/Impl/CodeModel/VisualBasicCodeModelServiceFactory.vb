' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel
    <ExportLanguageServiceFactory(GetType(ICodeModelService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicCodeModelServiceFactory
        Implements ILanguageServiceFactory

        Private ReadOnly _editorOptionsFactoryService As IEditorOptionsFactoryService
        Private ReadOnly _refactorNotifyServices As IEnumerable(Of IRefactorNotifyService)
        Private ReadOnly _commitBufferManagerFactory As CommitBufferManagerFactory

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(editorOptionsFactoryService As IEditorOptionsFactoryService,
                        <ImportMany> refactorNotifyServices As IEnumerable(Of IRefactorNotifyService),
                        commitBufferManagerFactory As CommitBufferManagerFactory)
            Me._editorOptionsFactoryService = editorOptionsFactoryService
            Me._refactorNotifyServices = refactorNotifyServices
            Me._commitBufferManagerFactory = commitBufferManagerFactory
        End Sub

        Public Function CreateLanguageService(provider As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicCodeModelService(provider, Me._editorOptionsFactoryService, _refactorNotifyServices, _commitBufferManagerFactory)
        End Function
    End Class
End Namespace
