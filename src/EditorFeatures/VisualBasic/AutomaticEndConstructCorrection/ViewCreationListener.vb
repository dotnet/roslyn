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
    <Export(GetType(ITextViewConnectionListener))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <TextViewRole(PredefinedTextViewRoles.Interactive)>
    Friend Class ViewCreationListener
        Implements ITextViewConnectionListener

        Private ReadOnly _waitIndicator As IWaitIndicator

        <ImportingConstructor()>
        Public Sub New(waitIndicator As IWaitIndicator)
            Me._waitIndicator = waitIndicator
        End Sub

        Public Sub SubjectBuffersConnected(
            textView As ITextView,
            reason As ConnectionReason,
            subjectBuffers As IReadOnlyCollection(Of ITextBuffer)) Implements ITextViewConnectionListener.SubjectBuffersConnected

            If Not subjectBuffers(0).GetFeatureOnOffOption(FeatureOnOffOptions.EndConstruct) Then
                Return
            End If

            Dim vbBuffers = subjectBuffers.Where(Function(b) b.ContentType.IsOfType(ContentTypeNames.VisualBasicContentType))
            AddConstructPairTo(vbBuffers)
        End Sub

        Public Sub SubjectBuffersDisconnected(textView As ITextView, reason As ConnectionReason, subjectBuffers As IReadOnlyCollection(Of ITextBuffer)) Implements ITextViewConnectionListener.SubjectBuffersDisconnected
            Dim vbBuffers = subjectBuffers.Where(Function(b) b.ContentType.IsOfType(ContentTypeNames.VisualBasicContentType))
            RemoveConstructPairFrom(vbBuffers)
        End Sub

        Private Sub AddConstructPairTo(buffers As IEnumerable(Of ITextBuffer))
            buffers.Do(Sub(b) b.Properties.GetOrCreateSingletonProperty(Function() New AutomaticEndConstructCorrector(b, _waitIndicator)).Connect())
        End Sub

        Private Sub RemoveConstructPairFrom(buffers As IEnumerable(Of ITextBuffer))
            buffers.Do(Sub(b) b.Properties.GetOrCreateSingletonProperty(Function() New AutomaticEndConstructCorrector(b, _waitIndicator)).Disconnect())

            buffers.Where(
                Function(b) b.Properties.GetProperty(Of AutomaticEndConstructCorrector)(GetType(AutomaticEndConstructCorrector)).IsDisconnected).Do(
                    Function(b) b.Properties.RemoveProperty(GetType(AutomaticEndConstructCorrector)))
        End Sub
    End Class
End Namespace
