' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Friend Class TestCompletionPresenter
        Implements IIntelliSensePresenter(Of ICompletionPresenterSession)

        Private ReadOnly _testState As IIntelliSenseTestState

        Public Sub New(testState As IIntelliSenseTestState)
            Me._testState = testState
        End Sub

        Public Function CreateSession(textView As ITextView, subjectBuffer As ITextBuffer) As ICompletionPresenterSession _
            Implements IIntelliSensePresenter(Of ICompletionPresenterSession).CreateSession
            Return New TestCompletionPresenterSession(_testState)
        End Function
    End Class
End Namespace
