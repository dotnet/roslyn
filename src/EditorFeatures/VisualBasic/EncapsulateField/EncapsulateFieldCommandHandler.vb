' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.EncapsulateField
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.EncapsulateField
    <Export(GetType(ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name(PredefinedCommandHandlerNames.EncapsulateField)>
    <Order(After:=PredefinedCommandHandlerNames.DocumentationComments)>
    Friend Class EncapsulateFieldCommandHandler
        Inherits AbstractEncapsulateFieldCommandHandler

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New(threadingContext As IThreadingContext,
                       undoManager As ITextBufferUndoManagerProvider,
                       listenerProvider As IAsynchronousOperationListenerProvider)
            MyBase.New(threadingContext, undoManager, listenerProvider)
        End Sub
    End Class
End Namespace
