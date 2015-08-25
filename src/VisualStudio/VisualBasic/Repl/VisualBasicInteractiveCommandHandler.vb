' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Interactive
Imports Microsoft.VisualStudio.InteractiveWindow
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Interactive

    <ExportCommandHandler("Interactive Command Handler")>
    Friend NotInheritable Class VisualBasicInteractiveCommandHandler
        Inherits InteractiveCommandHandler

        Private ReadOnly _interactiveWindowProvider As VisualBasicVsInteractiveWindowProvider

        <ImportingConstructor>
        Public Sub New(
            interactiveWindowProvider As VisualBasicVsInteractiveWindowProvider,
            contentTypeRegistryService As IContentTypeRegistryService,
            editorOptionsFactoryService As IEditorOptionsFactoryService,
            editorOperationsFactoryService As IEditorOperationsFactoryService)

            MyBase.New(contentTypeRegistryService, editorOptionsFactoryService, editorOperationsFactoryService)
            _interactiveWindowProvider = interactiveWindowProvider
        End Sub

        Protected Overrides Function OpenInteractiveWindow(focus As Boolean) As IInteractiveWindow
            Return _interactiveWindowProvider.Open(instanceId:=0, focus:=focus).InteractiveWindow
        End Function
    End Class
End Namespace

