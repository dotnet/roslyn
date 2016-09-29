' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Interactive
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Interactive
Imports Microsoft.VisualStudio.InteractiveWindow
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Interactive

    <ExportCommandHandler("Interactive Command Handler")>
    Friend NotInheritable Class VisualBasicInteractiveCommandHandler
        Inherits InteractiveCommandHandler

        Private ReadOnly _interactiveWindowProvider As VisualBasicVsInteractiveWindowProvider

        Private ReadOnly _sendToInteractiveSubmissionProvider As ISendToInteractiveSubmissionProvider

        <ImportingConstructor>
        Public Sub New(
            interactiveWindowProvider As VisualBasicVsInteractiveWindowProvider,
            contentTypeRegistryService As IContentTypeRegistryService,
            editorOptionsFactoryService As IEditorOptionsFactoryService,
            editorOperationsFactoryService As IEditorOperationsFactoryService,
            waitIndicator As IWaitIndicator)

            MyBase.New(contentTypeRegistryService, editorOptionsFactoryService, editorOperationsFactoryService, waitIndicator)
            _interactiveWindowProvider = interactiveWindowProvider
            _sendToInteractiveSubmissionProvider = New VisualBasicSendToInteractiveSubmissionProvider()
        End Sub

        Protected Overrides ReadOnly Property SendToInteractiveSubmissionProvider As ISendToInteractiveSubmissionProvider
            Get
                Return _sendToInteractiveSubmissionProvider
            End Get
        End Property

        Protected Overrides Function OpenInteractiveWindow(focus As Boolean) As IInteractiveWindow
            Return _interactiveWindowProvider.Open(instanceId:=0, focus:=focus).InteractiveWindow
        End Function
    End Class
End Namespace

