' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.EncapsulateField
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.EncapsulateField
    <ExportCommandHandler(PredefinedCommandHandlerNames.EncapsulateField, ContentTypeNames.VisualBasicContentType)>
    <Order(After:=PredefinedCommandHandlerNames.DocumentationComments)>
    Friend Class EncapsulateFieldCommandHandler
        Inherits AbstractEncapsulateFieldCommandHandler

        <ImportingConstructor>
        Public Sub New(waitIndicator As IWaitIndicator,
                       undoManager As ITextBufferUndoManagerProvider,
                       <ImportMany> asyncListeners As IEnumerable(Of Lazy(Of IAsynchronousOperationListener, FeatureMetadata)))
            MyBase.New(waitIndicator, undoManager, asyncListeners)
        End Sub
    End Class
End Namespace
