' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    <Export(GetType(IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession)))>
    <PartNotDiscoverable>
    <Name("Test Signature Help Presenter")>
    <ContentType(ContentTypeNames.RoslynContentType)>
    Friend Class TestSignatureHelpPresenter
        Implements IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession)

        Private ReadOnly _testState As IIntelliSenseTestState

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(testState As IIntelliSenseTestState)
            Me._testState = testState
        End Sub

        Public Function CreateSession(textView As ITextView, subjectBuffer As ITextBuffer, sessionOpt As ISignatureHelpSession) As ISignatureHelpPresenterSession _
            Implements IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession).CreateSession
            Return New TestSignatureHelpPresenterSession(_testState)
        End Function
    End Class
End Namespace
