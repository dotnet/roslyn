' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.ObjectModel
Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticEndConstructCorrection
    ''' <summary>
    ''' Tracks user's interaction with editor
    ''' </summary>
    <Export(GetType(IWpfTextViewConnectionListener))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <TextViewRole(PredefinedTextViewRoles.Interactive)>
    Friend Class ViewCreationListener
        Implements IWpfTextViewConnectionListener

        Private ReadOnly _waitIndicator As IWaitIndicator

        <ImportingConstructor()>
        Public Sub New(waitIndicator As IWaitIndicator)
            Me._waitIndicator = waitIndicator
        End Sub

        Public Sub SubjectBuffersConnected(
            textView As IWpfTextView,
            reason As ConnectionReason,
            subjectBuffers As Collection(Of ITextBuffer)) Implements IWpfTextViewConnectionListener.SubjectBuffersConnected

            If Not subjectBuffers(0).GetOption(FeatureOnOffOptions.EndConstruct) Then
                Return
            End If

            Dim vbBuffers = GetVisualBasicBuffers(subjectBuffers)
            AddCorrectors(vbBuffers)
        End Sub

        Private Shared Function GetVisualBasicBuffers(subjectBuffers As Collection(Of ITextBuffer)) As IEnumerable(Of ITextBuffer)
            Return subjectBuffers.Where(Function(b) b.ContentType.IsOfType(ContentTypeNames.VisualBasicContentType))
        End Function

        Public Sub SubjectBuffersDisconnected(textView As IWpfTextView, reason As ConnectionReason, subjectBuffers As Collection(Of ITextBuffer)) Implements IWpfTextViewConnectionListener.SubjectBuffersDisconnected
            Dim vbBuffers = GetVisualBasicBuffers(subjectBuffers)
            RemoveCorrectors(vbBuffers)
        End Sub

        Private ReadOnly createEndConstructCorrector As Func(Of ITextBuffer, AutomaticEndConstructCorrector) = Function(b As ITextBuffer) New AutomaticEndConstructCorrector(b, _waitIndicator)
        Private ReadOnly createXmlElementTagCorrector As Func(Of ITextBuffer, XmlElementTagCorrector) = Function(b As ITextBuffer) New XmlElementTagCorrector(b, _waitIndicator)

        Private Sub AddCorrectors(buffers As IEnumerable(Of ITextBuffer))
            AddCorrectors(buffers, createEndConstructCorrector)
            AddCorrectors(buffers, createXmlElementTagCorrector)
        End Sub

        Private Sub AddCorrectors(Of T As {Class, ICorrector})(buffers As IEnumerable(Of ITextBuffer), createCorrector As Func(Of ITextBuffer, T))
            buffers.Do(Sub(b) b.Properties.GetOrCreateSingletonProperty(Function() createCorrector(b)).Connect())
        End Sub

        Private Sub RemoveCorrectors(buffers As IEnumerable(Of ITextBuffer))
            RemoveCorrectors(buffers, createEndConstructCorrector)
            RemoveCorrectors(buffers, createXmlElementTagCorrector)
        End Sub

        Private Sub RemoveCorrectors(Of T As {Class, ICorrector})(buffers As IEnumerable(Of ITextBuffer), createCorrector As Func(Of ITextBuffer, T))
            buffers.Do(Sub(b) b.Properties.GetOrCreateSingletonProperty(Function() createCorrector(b)).Disconnect())

            buffers.Where(
                Function(b) b.Properties.GetProperty(Of T)(GetType(T)).IsDisconnected).Do(
                    Function(b) b.Properties.RemoveProperty(GetType(T)))
        End Sub
    End Class

    Interface ICorrector
        Sub Connect()
        Sub Disconnect()
        ReadOnly Property IsDisconnected As Boolean
    End Interface
End Namespace
