' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.ExtractMethod
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.ExtractMethod
    <ExportCommandHandler(PredefinedCommandHandlerNames.ExtractMethod,
        ContentTypeNames.VisualBasicContentType)>
    <Order(After:=PredefinedCommandHandlerNames.DocumentationComments)>
    Friend Class ExtractMethodCommandHandler
        Inherits AbstractExtractMethodCommandHandler

        <ImportingConstructor()>
        Public Sub New(undoManager As ITextBufferUndoManagerProvider,
                       editorOperationsFactoryService As IEditorOperationsFactoryService,
                       renameService As IInlineRenameService,
                       waitIndicator As IWaitIndicator)
            MyBase.New(undoManager, editorOperationsFactoryService, renameService, waitIndicator)
        End Sub
    End Class
End Namespace
