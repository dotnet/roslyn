' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor.Implementation.ExtractMethod
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.ExtractMethod
    <Export(GetType(ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name(PredefinedCommandHandlerNames.ExtractMethod)>
    <Order(After:=PredefinedCommandHandlerNames.DocumentationComments)>
    Friend Class ExtractMethodCommandHandler
        Inherits AbstractExtractMethodCommandHandler

        <ImportingConstructor()>
        Public Sub New(threadingContext As IThreadingContext,
                       undoManager As ITextBufferUndoManagerProvider,
                       renameService As IInlineRenameService)
            MyBase.New(threadingContext, undoManager, renameService)
        End Sub
    End Class
End Namespace
