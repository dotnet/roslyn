' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Formatting.Indentation
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Moq

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Formatting.Indentation
    Public Class SmartIndentProviderTests
        Private Class MockWaitIndicator
            Implements IWaitIndicator
            Public Function StartWait(title As String, message As String, allowCancel As Boolean) As IWaitContext Implements IWaitIndicator.StartWait
                Throw New NotImplementedException()
            End Function

            Public Function Wait(title As String, message As String, allowCancel As Boolean, action As Action(Of IWaitContext)) As WaitIndicatorResult Implements IWaitIndicator.Wait
                Throw New NotImplementedException()
            End Function
        End Class

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub GetSmartIndent1()
            Dim workspace = New TestWorkspace()

            Dim provider = New SmartIndentProvider()

            AssertEx.Throws(Of Exception)(
                Function() provider.CreateSmartIndent(Nothing),
                allowDerived:=True)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub GetSmartIndent2()
            Dim workspace = New TestWorkspace()
            Dim optionsService = workspace.Services.GetService(Of IOptionService)()
            Dim initialState = optionsService.GetOption(InternalFeatureOnOffOptions.SmartIndenter)
            Assert.Equal(True, initialState)

            Dim provider = New SmartIndentProvider()

            ' connect things together
            Dim textView = New Mock(Of ITextView)(MockBehavior.Strict)
            Dim subjectBuffer = workspace.ExportProvider.GetExportedValue(Of ITextBufferFactoryService)().CreateTextBuffer()
            workspace.RegisterText(subjectBuffer.AsTextContainer())

            textView.SetupGet(Function(x) x.Options).Returns(TestEditorOptions.Instance)
            textView.SetupGet(Function(x) x.TextBuffer).Returns(subjectBuffer)
            textView.SetupGet(Function(x) x.Caret).Returns(New Mock(Of ITextCaret)(MockBehavior.Strict).Object)

            Dim smartIndenter = provider.CreateSmartIndent(textView.Object)
            Assert.NotNull(smartIndenter)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub GetSmartIndent3()
            Dim workspace = New TestWorkspace()
            Dim optionsService = workspace.Services.GetService(Of IOptionService)()
            Dim initialState = optionsService.GetOption(InternalFeatureOnOffOptions.SmartIndenter)
            Assert.Equal(True, initialState)

            optionsService.SetOptions(optionsService.GetOptions().WithChangedOption(InternalFeatureOnOffOptions.SmartIndenter, False))

            Dim provider = New SmartIndentProvider()

            ' connect things together
            Dim textView = New Mock(Of ITextView)(MockBehavior.Strict)
            Dim subjectBuffer = workspace.ExportProvider.GetExportedValue(Of ITextBufferFactoryService)().CreateTextBuffer()
            workspace.RegisterText(subjectBuffer.AsTextContainer())

            textView.SetupGet(Function(x) x.Options).Returns(TestEditorOptions.Instance)
            textView.SetupGet(Function(x) x.TextBuffer).Returns(subjectBuffer)

            Dim smartIndenter = provider.CreateSmartIndent(textView.Object)

            optionsService.SetOptions(optionsService.GetOptions().WithChangedOption(InternalFeatureOnOffOptions.SmartIndenter, True))

            Assert.Null(smartIndenter)
        End Sub
    End Class
End Namespace
