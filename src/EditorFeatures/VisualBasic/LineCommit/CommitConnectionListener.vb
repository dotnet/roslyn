' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.ObjectModel
Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
    <Export(GetType(ITextViewConnectionListener))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <TextViewRole(PredefinedTextViewRoles.Editable)>
    Friend Class CommitConnectionListener
        Implements ITextViewConnectionListener

        Private ReadOnly _commitBufferManagerFactory As CommitBufferManagerFactory
        Private ReadOnly _textBufferAssociatedViewService As ITextBufferAssociatedViewService
        Private ReadOnly _textUndoHistoryRegistry As ITextUndoHistoryRegistry
        Private ReadOnly _uiThreadOperationExecutor As IUIThreadOperationExecutor

        <ImportingConstructor()>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(commitBufferManagerFactory As CommitBufferManagerFactory,
                       textBufferAssociatedViewService As ITextBufferAssociatedViewService,
                       textUndoHistoryRegistry As ITextUndoHistoryRegistry,
                       uiThreadOperationExecutor As IUIThreadOperationExecutor)
            _commitBufferManagerFactory = commitBufferManagerFactory
            _textBufferAssociatedViewService = textBufferAssociatedViewService
            _textUndoHistoryRegistry = textUndoHistoryRegistry
            _uiThreadOperationExecutor = uiThreadOperationExecutor
        End Sub

        Public Sub SubjectBuffersConnected(view As ITextView, reason As ConnectionReason, subjectBuffers As IReadOnlyCollection(Of ITextBuffer)) Implements ITextViewConnectionListener.SubjectBuffersConnected
            ' Make sure we have a view manager
            view.Properties.GetOrCreateSingletonProperty(
                Function() New CommitViewManager(view, _commitBufferManagerFactory, _textBufferAssociatedViewService, _textUndoHistoryRegistry, _uiThreadOperationExecutor))

            ' Connect to each of these buffers, and increment their ref count
            For Each buffer In subjectBuffers
                _commitBufferManagerFactory.CreateForBuffer(buffer).AddReferencingView()
            Next
        End Sub

        Public Sub SubjectBuffersDisconnected(view As ITextView, reason As ConnectionReason, subjectBuffers As IReadOnlyCollection(Of ITextBuffer)) Implements ITextViewConnectionListener.SubjectBuffersDisconnected
            For Each buffer In subjectBuffers
                _commitBufferManagerFactory.CreateForBuffer(buffer).RemoveReferencingView()
            Next

            ' If we have no subject buffers left, we can remove our view manager
            If Not view.BufferGraph.GetTextBuffers(Function(b) b.ContentType.IsOfType(ContentTypeNames.VisualBasicContentType)).Any() Then
                view.Properties.GetProperty(Of CommitViewManager)(GetType(CommitViewManager)).Disconnect()
                view.Properties.RemoveProperty(GetType(CommitViewManager))
            End If
        End Sub
    End Class
End Namespace
