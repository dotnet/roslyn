' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Friend Class TestCompletionPresenter
        Implements IIntelliSensePresenter(Of ICompletionPresenterSession, ICompletionSession)

        Private ReadOnly _completionBroker As ICompletionBroker
        Private ReadOnly _completionSetFactory As ICompletionSetFactory
        Private ReadOnly _glyphService As IGlyphService
        Private ReadOnly _testState As IIntelliSenseTestState

        Public Sub New(testState As IIntelliSenseTestState,
                       completionSetFactory As ICompletionSetFactory,
                       completionBroker As ICompletionBroker,
                       glyphService As IGlyphService)
            Me._testState = testState
            _completionSetFactory = completionSetFactory
            _completionBroker = completionBroker
            _glyphService = glyphService
        End Sub

        Public Function CreateSession(textView As ITextView,
                                      subjectBuffer As ITextBuffer,
                                      sessionOpt As ICompletionSession) As ICompletionPresenterSession _
            Implements IIntelliSensePresenter(Of ICompletionPresenterSession, ICompletionSession).CreateSession
            Dim session = New CompletionPresenterSession(
                _completionSetFactory, _completionBroker,
                _glyphService, textView, subjectBuffer)

            _testState.CurrentCompletionPresenterSession = session

            AddHandler session.Dismissed, Sub()
                                              _testState.CurrentCompletionPresenterSession = Nothing
                                          End Sub

            Return session
        End Function
    End Class
End Namespace
