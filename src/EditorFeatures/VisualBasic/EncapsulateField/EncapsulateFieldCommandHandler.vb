' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor.Implementation.EncapsulateField
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities
Imports VSCommanding = Microsoft.VisualStudio.Commanding

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.EncapsulateField
    <Export(GetType(VSCommanding.ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name(PredefinedCommandHandlerNames.EncapsulateField)>
    <Order(After:=PredefinedCommandHandlerNames.DocumentationComments)>
    Friend Class EncapsulateFieldCommandHandler
        Inherits AbstractEncapsulateFieldCommandHandler

        <ImportingConstructor>
        Public Sub New(threadingContext As IThreadingContext,
                       undoManager As ITextBufferUndoManagerProvider,
                       listenerProvider As IAsynchronousOperationListenerProvider)
            MyBase.New(threadingContext, undoManager, listenerProvider)
        End Sub
    End Class
End Namespace
