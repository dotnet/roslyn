' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Interactive
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities
Imports Microsoft.VisualStudio.InteractiveWindow

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Interactive

    <ExportCommandHandler("Interactive Command Handler", ContentTypeNames.VisualBasicContentType)>
    Friend NotInheritable Class VisualBasicInteractiveCommandHandler
        Inherits InteractiveCommandHandler

        <ImportingConstructor>
        Public Sub New(
            contentTypeRegistryService As IContentTypeRegistryService,
            editorOptionsFactoryService As IEditorOptionsFactoryService,
            editorOperationsFactoryService As IEditorOperationsFactoryService)

            MyBase.New(contentTypeRegistryService, editorOptionsFactoryService, editorOperationsFactoryService)
        End Sub

        Protected Overrides Function OpenInteractiveWindow(focus As Boolean) As IInteractiveWindow
            ' TODO:
            'Return VisualBasicReplPackage.OpenVisualBasicInteractiveWindow(engineFactories, InteractiveWindowProvider, ContentTypeRegistryService, focus)
            Return Nothing
        End Function
    End Class
End Namespace

