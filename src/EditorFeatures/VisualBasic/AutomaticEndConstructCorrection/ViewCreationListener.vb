' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
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

        Private ReadOnly _uiThreadOperationExecutor As IUIThreadOperationExecutor
        Private ReadOnly _globalOptions As IGlobalOptionService

        <ImportingConstructor()>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(uiThreadOperationExecutor As IUIThreadOperationExecutor,
                       globalOptions As IGlobalOptionService)
            _uiThreadOperationExecutor = uiThreadOperationExecutor
            _globalOptions = globalOptions
        End Sub

        Public Sub SubjectBuffersConnected(
            textView As ITextView,
            reason As ConnectionReason,
            subjectBuffers As IReadOnlyCollection(Of ITextBuffer)) Implements ITextViewConnectionListener.SubjectBuffersConnected

            If Not _globalOptions.GetOption(EndConstructGenerationOptionsStorage.EndConstruct, LanguageNames.VisualBasic) Then
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
            buffers.Do(Sub(b) b.Properties.GetOrCreateSingletonProperty(Function() New AutomaticEndConstructCorrector(b, _uiThreadOperationExecutor)).Connect())
        End Sub

        Private Sub RemoveConstructPairFrom(buffers As IEnumerable(Of ITextBuffer))
            buffers.Do(Sub(b) b.Properties.GetOrCreateSingletonProperty(Function() New AutomaticEndConstructCorrector(b, _uiThreadOperationExecutor)).Disconnect())

            buffers.Where(
                Function(b) b.Properties.GetProperty(Of AutomaticEndConstructCorrector)(GetType(AutomaticEndConstructCorrector)).IsDisconnected).Do(
                    Function(b) b.Properties.RemoveProperty(GetType(AutomaticEndConstructCorrector)))
        End Sub
    End Class
End Namespace
