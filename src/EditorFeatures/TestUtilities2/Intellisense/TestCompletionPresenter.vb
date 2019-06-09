' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    <Export(GetType(IIntelliSensePresenter(Of ICompletionPresenterSession, ICompletionSession)))>
    <PartNotDiscoverable>
    <Name("Presenter")>
    <ContentType(ContentTypeNames.RoslynContentType)>
    Friend Class TestCompletionPresenter
        Implements IIntelliSensePresenter(Of ICompletionPresenterSession, ICompletionSession)

        Private ReadOnly _testState As IIntelliSenseTestState

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(testState As IIntelliSenseTestState)
            Me._testState = testState
        End Sub

        Public Function CreateSession(textView As ITextView, subjectBuffer As ITextBuffer, sessionOpt As ICompletionSession) As ICompletionPresenterSession _
            Implements IIntelliSensePresenter(Of ICompletionPresenterSession, ICompletionSession).CreateSession
            Return New TestCompletionPresenterSession(_testState)
        End Function
    End Class
End Namespace
